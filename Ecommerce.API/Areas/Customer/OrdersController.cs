using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Ecommerce.API.Areas.Customer;

[Route("api/[area]/[controller]")]
[ApiController]
[Area(SD.CUSTOMER_AREA)]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IUserService _userService;
    private readonly IRepository<ECommerce.API.Models.Review> _reviewRepository;
    private readonly IOrderService _orderService;

    public OrdersController(
        UserManager<ApplicationUser> userManager,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IUserService userService,
        IRepository<ECommerce.API.Models.Review> reviewRepository,
        IOrderService orderService)
    {
        _userManager = userManager;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _userService = userService;
        _reviewRepository = reviewRepository;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, string? query = null)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var orders = await _orderRepository.GetAsync(e => e.ApplicationUserId == user.Id);

        // Filter
        if (query is not null)
            orders = orders.Where(e => e.CarrierName != null && e.CarrierName.ToLower().Contains(query.ToLower().Trim()));

        // Pagination
        int totalPages = (int)Math.Ceiling(orders.Count() / 5.0);
        orders = orders.Skip((page - 1) * 5).Take(5);

        return Ok(new OrderWithRelatedResponse()
        {
            Orders = orders.AsEnumerable(),
            TotalPages = totalPages,
            CurrentPage = page,
            Query = query
        });
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(int orderId)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var order = await _orderRepository.GetOneAsync(e => e.Id == orderId 
        && e.ApplicationUserId == userId);

        if(order is null) return NotFound();

        var orderItems = await _orderItemRepository
            .GetAsync(e => e.OrderId == orderId);

        return Ok(new OrderWithItemsResponse()
        {
            Order = order,
            OrderItems = orderItems
        });
    }

    [HttpPost("Rate")]
    public async Task<IActionResult> Rate([FromForm] CreateReviewRequest createReviewRequest)
    {
        var userId = _userService.GetUserId(User);
        if (userId is null) return NotFound();

        var order = await _orderRepository.GetOneAsync(e => e.Id == createReviewRequest.OrderId
        && e.ApplicationUserId == userId);

        if (order is null) return NotFound();

        var orderItem = await _orderItemRepository
            .GetOneAsync(e => createReviewRequest.ProductId == e.ProductId && e.OrderId == createReviewRequest.OrderId,
            includes: [e => e.Order]);

        if(orderItem is null || orderItem.Order.OrderStatus != OrderStatus.Completed) return NotFound();

        // TODO: if u are already give your rate
        /////////////////////////////////////////

        ECommerce.API.Models.Review review = new ECommerce.API.Models.Review()
        {
            ApplicationUserId = userId,
            Comment = createReviewRequest.Comment,
            Rate = createReviewRequest.Rate,
            ProductId = createReviewRequest.ProductId,
        };

        string? img = _orderService.SaveImg(createReviewRequest.Img);

        if (img is null)
            return BadRequest();

        review.Img = img;
        await _reviewRepository.CreateAsync(review);
        await _reviewRepository.CommitAsync();

        return Created();
    }

    [HttpGet("{id}/Refund")]
    public async Task<IActionResult> Refund(int id)
    {
        var order = await _orderRepository.GetOneAsync(e => e.Id == id);
        if (order is null) return NotFound();

        var options = new RefundCreateOptions()
        {
            PaymentIntent = order.TransactionId,
            Amount = ((long)order.TotalPrice * 100) - (5 * 100),
            Reason = RefundReasons.Unknown,
        };

        var service = new RefundService();
        var session = service.Create(options);

        return Ok();
    }
}
