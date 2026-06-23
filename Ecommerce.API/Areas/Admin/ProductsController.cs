using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Areas.Admin;

[Route("api/[area]/[controller]")]
[Area(SD.ADMIN_AREA)]
[ApiController]
//[Authorize(Roles = $"{RoleNames.SUPER_ADMIN},{RoleNames.ADMIN}")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Brand> _brandRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IProductSubImgRepository _productSubImgRepository;

    public ProductsController(IRepository<Category> categoryRepository, IRepository<Brand> brandRepository, IRepository<Product> productRepository, IProductSubImgRepository productSubImgRepository, IProductService productService)
    {
        _productService = productService;

        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _productRepository = productRepository;
        _productSubImgRepository = productSubImgRepository;
    }

    [HttpPost]
    public async Task<IActionResult> GetAll(ProductFilterRequest productFilterRequest, int page = 1, CancellationToken cancellationToken = default)
    {
        //var products = _context.Products
        //                        .Include(e=>e.Category)
        //                        .Include(e=>e.Brand)
        //                        .AsQueryable();

        var products = await _productRepository.GetAsync(includes: [e => e.Category, e => e.Brand]);

        ProductFilterResponse productFilter = new();

        // Filter
        if (productFilterRequest.query is not null)
            products = products.Where(e => e.Name.ToLower().Contains(productFilterRequest.query.ToLower().Trim()));

        if (productFilterRequest.minPrice is not null)
        {
            products = products.Where(e => e.Price >= productFilterRequest.minPrice);
            productFilter.MinPrice = productFilterRequest.minPrice;
        }

        if (productFilterRequest.maxPrice is not null)
        {
            products = products.Where(e => e.Price <= productFilterRequest.maxPrice);
            productFilter.MaxPrice = productFilterRequest.maxPrice;
        }

        if (productFilterRequest.categoryId is not null)
        {
            products = products.Where(e => e.CategoryId == productFilterRequest.categoryId);
            productFilter.CategoryId = productFilterRequest.categoryId;
        }

        if (productFilterRequest.brandId is not null)
        {
            products = products.Where(e => e.BrandId == productFilterRequest.brandId);
            productFilter.BrandId = productFilterRequest.brandId;
        }

        if (productFilterRequest.lowQuantity)
        {
            products = products.OrderBy(e => e.Quantity);
            productFilter.LowQuantity = productFilterRequest.lowQuantity;
        }

        // Pagination
        int totalPages = (int)Math.Ceiling(products.Count() / 5.0);
        products = products.Skip((page - 1) * 5).Take(5);

        return Ok(new ProductWithRelatedResponse()
        {
            Products = products.AsEnumerable(),
            TotalPages = totalPages,
            CurrentPage = page,
            Query = productFilterRequest.query,
            Categories = await _categoryRepository.GetAsync(cancellationToken: cancellationToken),
            Brands = await _brandRepository.GetAsync(cancellationToken: cancellationToken),
            ProductFilterResponse = productFilter
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken = default)
    {
        //var product = _context.Products.SingleOrDefault(e => e.Id == id);
        var product = await _productRepository.GetOneAsync(e => e.Id == id);

        if (product is null) return NotFound();

        return Ok(new ProductResponse()
        {
            Product = product,
            ProductSubImgs = await _productSubImgRepository.GetAsync(e => e.ProductId == id),
            Categories = await _categoryRepository.GetAsync(cancellationToken: cancellationToken),
            Brands = await _brandRepository.GetAsync(cancellationToken: cancellationToken),
        });
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromForm] CreateProductRequest createProductRequest, CancellationToken cancellationToken = default)
    {
        Product product = createProductRequest.Adapt<Product>();

        if (createProductRequest.MainImg is not null && createProductRequest.MainImg.Length > 0)
        {
            // Save Main Img in wwwroot
            var fileName = _productService.SaveImg(createProductRequest.MainImg);

            if (fileName is not null)
            {
                // Save Main Img name in DB
                product.MainImg = fileName;
            }
        }
        
        await _productRepository.CreateAsync(product, cancellationToken);
        await _productRepository.CommitAsync(cancellationToken);

        if (createProductRequest.SubImgs.Any())
        {
            foreach (var item in createProductRequest.SubImgs)
            {
                if (item is not null && item.Length > 0)
                {
                    var fileName = _productService.SaveImg(item, ProductImgType.SubImg);

                    if (fileName is not null)
                    {
                        //_context.ProductSubImgs.Add(new()
                        //{
                        //    SubImg = fileName,
                        //    ProductId = product.Id
                        //});
                        await _productSubImgRepository.CreateAsync(new ProductSubImg()
                        {
                            SubImg = fileName,
                            ProductId = product.Id
                        }, cancellationToken: cancellationToken);
                    }
                }
            }
        }
        await _productSubImgRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Products", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = "Create Product Successfully",
            Status = 201
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromForm] UpdateProductRequest updateProductRequest, CancellationToken cancellationToken = default) // string name, string Description, bool status
    {
        //var productInDb = _context.Products.AsNoTracking().SingleOrDefault(e => e.Id == product.Id);
        Product? productInDb = await _productRepository.GetOneAsync(e => e.Id == updateProductRequest.Id);

        if (productInDb is null) return NotFound();

        if (updateProductRequest.MainImg is not null && updateProductRequest.MainImg.Length > 0)
        {
            // Save new Main Img in wwwroot
            var fileName = _productService.SaveImg(updateProductRequest.MainImg);

            // Remove old Main Img from wwwroot
            _productService.RemoveImg(productInDb.MainImg);

            // Save new Main Img in DB
            if (fileName is not null) productInDb.MainImg = fileName;
        }

        productInDb.Name = updateProductRequest.Name;
        productInDb.Description = updateProductRequest.Description;
        productInDb.Price = updateProductRequest.Price;
        productInDb.Status = updateProductRequest.Status;
        productInDb.Discount = updateProductRequest.Discount;
        productInDb.Quantity = updateProductRequest.Quantity;
        productInDb.CategoryId = updateProductRequest.CategoryId;
        productInDb.BrandId = updateProductRequest.BrandId;

        _productRepository.Update(productInDb);
        await _productRepository.CommitAsync(cancellationToken);

        if (updateProductRequest.SubImgs is not null && updateProductRequest.SubImgs.Any())
        {
            var productSubImgs = await _productSubImgRepository.GetAsync(e => e.ProductId == updateProductRequest.Id);

            foreach (var item in productSubImgs)
                _productService.RemoveImg(item.SubImg, ProductImgType.SubImg);

            _productSubImgRepository.DeleteRange(productSubImgs);

            foreach (var item in updateProductRequest.SubImgs)
            {
                if (item is not null && item.Length > 0)
                {
                    var fileName = _productService.SaveImg(item, ProductImgType.SubImg);

                    if (fileName is not null)
                    {
                        await _productSubImgRepository.CreateAsync(new ProductSubImg()
                        {
                            SubImg = fileName,
                            ProductId = updateProductRequest.Id
                        }, cancellationToken: cancellationToken);
                    }
                }
            }
            await _productSubImgRepository.CommitAsync(cancellationToken);
        }

        var url = Url.Action(nameof(GetAll), "Products", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = "Update Product Successfully",
            Status = 201
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        //var product = _context.Products.SingleOrDefault(e => e.Id == id);
        var product = await _productRepository.GetOneAsync(e => e.Id == id);

        if (product is null) return NotFound();

        var productSubImgs = await _productSubImgRepository.GetAsync(e => e.ProductId == product.Id);

        foreach (var item in productSubImgs)
            _productService.RemoveImg(item.SubImg, ProductImgType.SubImg);

        // Remove old Main Img from wwwroot
        _productService.RemoveImg(product.MainImg);

        _productRepository.Delete(product);
        await _productRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Products", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = "Remove Product Successfully",
            Status = 201
        });
    }
}
