using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace Ecommerce.API.Areas.Customer;

[Route("api/[area]/[controller]")]
[ApiController]
[Area(SD.CUSTOMER_AREA)]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<PromotionUserUsage> _promotionsUserUsageRepository;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ILogger<CartsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRepository<Order> _orderRepository;
    private readonly IUserService _userService;
    private readonly IRepository<ProductPromotion> _promotionPromotionRepository;

    public CartsController(UserManager<ApplicationUser> userManager,
        IRepository<Cart> cartRepository,
        IRepository<Product> productRepository,
        IRepository<ProductPromotion> promotionPromotionRepository,
        IRepository<PromotionUserUsage> promotionsUserUsageRepository,
        ApplicationDbContext applicationDbContext,
        ILogger<CartsController> logger,
        IConfiguration configuration,
        IRepository<Order> orderRepository,
        IUserService userService
        )
    {
        _userManager = userManager;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _promotionsUserUsageRepository = promotionsUserUsageRepository;
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _configuration = configuration;
        _orderRepository = orderRepository;
        _userService = userService;
        _promotionPromotionRepository = promotionPromotionRepository;
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> AddToCart(int productId, int count) // 1, 2
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var product = await _productRepository.GetOneAsync(e => e.Id == productId);
        if (product is null) return NotFound();

        var cartInDB = await _cartRepository.GetOneAsync(e => e.ApplicationuserId == user.Id && e.ProductId == productId);

        if (cartInDB is null)
        {
            Cart cart = new()
            {
                ApplicationuserId = user.Id,
                ProductId = productId,
                Quantity = count,
                ProductPrice = (double)product.Price,
                TotalPrice = (double)product.Price * count
            };
            await _cartRepository.CreateAsync(cart);
        }
        else
        {
            cartInDB.Quantity += count;
        }

        await _cartRepository.CommitAsync();

        return Ok(new Response()
        {
            Message = "Add product to your cart successfull",
            Status = 200
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get(string? code = null)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var userCart = await _cartRepository
            .GetAsync(e => e.ApplicationuserId == user.Id, includes: [e => e.Product]);

        string notification = string.Empty;
        if (code is not null)
        {
            // Code is Valid
            var promotion = await _promotionPromotionRepository.GetOneAsync(e => e.Code == code && DateTime.Now <= e.ValidTo && e.Usage > 0 && e.Status);

            if (promotion is null)
            {
                return BadRequest(new Response()
                {
                    Message = "Promotion Code is Invalid!",
                });
            }

            // Check Product List in Cart if match Product Promotion Code
            bool productMatch = false;
            foreach (var item in userCart)
            {
                if (item.ProductId == promotion.ProductId)
                {
                    var transaction = _applicationDbContext.Database.BeginTransaction();

                    try
                    {
                        // Apply Promotion:
                        // - Make sure that user does not use this promotion before,
                        var userPromotionUsage = await _promotionsUserUsageRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductPromotionId == promotion.Id);

                        if (userPromotionUsage is not null)
                        {
                            notification = "You have already used this promotion code!";
                            break;
                        }

                        // - Apply Discount
                        item.ProductPrice = item.ProductPrice * (100 - promotion.Discount) / 100;
                        item.TotalPrice = item.ProductPrice * item.Quantity;

                        // - Add New Row in Promotion User Usage
                        await _promotionsUserUsageRepository.CreateAsync(new()
                        {
                            ApplicationUserId = user.Id,
                            ProductPromotionId = promotion.Id,
                            UsedAt = DateTime.Now,
                            Code = promotion.Code
                        });
                        await _promotionsUserUsageRepository.CommitAsync();

                        // - Decrease (Usage -1)
                        promotion.Usage -= 1;

                        notification = "Promotion code applied successfully!";
                        productMatch = true;
                        await _promotionPromotionRepository.CommitAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        transaction.Rollback();
                    }

                }
            }

            if (!productMatch)
                notification = "Promotion Code Can not apply on this products!";
        }

        return Ok(userCart);
    }

    [HttpPatch("Increment/{cartId}")]
    public async Task<IActionResult> Increment(int cartId)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var cartInDb = await _cartRepository.GetOneAsync(e => e.Id == cartId && e.ApplicationuserId == user.Id);
        if (cartInDb is null) return NotFound();

        cartInDb.Quantity += 1;
        await _cartRepository.CommitAsync();

        return Ok();
    }

    [HttpPatch("Decrement/{cartId}")]
    public async Task<IActionResult> Decrement(int cartId)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var cartInDb = await _cartRepository.GetOneAsync(e => e.Id == cartId && e.ApplicationuserId == user.Id);
        if (cartInDb is null) return NotFound();

        if (cartInDb.Quantity == 1)
            _cartRepository.Delete(cartInDb);
        else
            cartInDb.Quantity -= 1;

        await _cartRepository.CommitAsync();

        return Ok();
    }

    [HttpDelete("{cartId}")]
    public async Task<IActionResult> Delete(int cartId)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var cartInDb = await _cartRepository.GetOneAsync(e => e.Id == cartId && e.ApplicationuserId == user.Id);
        if (cartInDb is null) return NotFound();

        _cartRepository.Delete(cartInDb);
        await _cartRepository.CommitAsync();

        return NoContent();
    }

    [HttpGet("Pay")]
    public async Task<IActionResult> Pay()
    {
        //StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var userCart = await _cartRepository
            .GetAsync(e => e.ApplicationuserId == user.Id, includes: [e => e.Product]);

        var orderInDb = await _orderRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.OrderStatus == OrderStatus.Pending);
        Order order = new();

        if (orderInDb == null)
        {
            order.ApplicationUserId = user.Id;
            order.TotalPrice = (decimal)userCart.Sum(e => e.ProductPrice * e.Quantity);

            await _orderRepository.CreateAsync(order);
            await _orderRepository.CommitAsync();
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
            SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success?orderId={orderInDb?.Id ?? order.Id}",
            CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel",
        };

        foreach (var item in userCart)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "egp",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Name,
                        Description = item.Product.Description,
                    },
                    UnitAmount = (long)item.ProductPrice * 100,
                },
                Quantity = item.Quantity,
            });
        }

        var service = new SessionService();
        var session = service.Create(options);
        order.SessionId = session.Id;
        await _orderRepository.CommitAsync();

        return Ok(new Response()
        {
            Body = [session.Url]
        });
    }
}
