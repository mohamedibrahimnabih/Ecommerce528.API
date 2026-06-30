using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Areas.Admin;

[Route("api/[area]/[controller]")]
[Area(SD.ADMIN_AREA)]
[ApiController]
//[Authorize(Roles = $"{RoleNames.SUPER_ADMIN},{RoleNames.ADMIN}")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;
    private readonly IRepository<Brand> _brandRepository;

    public BrandsController(IRepository<Brand> brandRepository, IBrandService brandService)
    {
        //_context = new();
        _brandRepository = brandRepository;
        _brandService = brandService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, string? query = null, CancellationToken cancellationToken = default)
    {
        var brands = await _brandRepository.GetAsync(cancellationToken: cancellationToken);

        // Filter
        if (query is not null)
            brands = brands.Where(e => e.Name.ToLower().Contains(query.ToLower().Trim()));

        // Pagination
        int totalPages = (int)Math.Ceiling(brands.Count() / 5.0);
        brands = brands.Skip((page - 1) * 5).Take(5);

        return Ok(new BrandWithRelatedResponse()
        {
            Brands = brands.AsEnumerable(),
            TotalPages = totalPages,
            CurrentPage = page,
            Query = query
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);

        if (brand is null) return NotFound();

        return Ok(brand);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateBrandRequest createBrandRequest, CancellationToken cancellationToken = default)
    {
        //ModelState.Remove("logo");

        Brand brand = new()
        {
            Name = createBrandRequest.Name,
            Description = createBrandRequest.Description,
            Status = createBrandRequest.Status
        };

        if (createBrandRequest.Logo is not null && createBrandRequest.Logo.Length > 0)
        {
            // Save Logo in wwwroot
            var fileName = _brandService.SaveImg(createBrandRequest.Logo);

            if (fileName is not null)
            {
                // Save Logo name in DB
                brand.Logo = fileName;
            }
        }

        await _brandRepository.CreateAsync(brand, cancellationToken);
        await _brandRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Brands", new { area = SD.ADMIN_AREA });
        return Created(url, new Response()
        {
            Message = "Create Brand Successfully",
            Status = 201
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromForm] UpdateBrandRequest updateBrandRequest, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetOneAsync(e => e.Id == updateBrandRequest.Id,
            cancellationToken: cancellationToken);

        if (brand is null) return NotFound();

        if (updateBrandRequest.Logo is not null && updateBrandRequest.Logo.Length > 0)
        {
            // Save new Logo in wwwroot
            var fileName = _brandService.SaveImg(updateBrandRequest.Logo);

            // Remove old Logo from wwwroot
            _brandService.RemoveImg(brand.Logo);

            // Save new Logo in DB
            if (fileName is not null) brand.Logo = fileName;
        }
        else
            brand.Logo = brand.Logo;

        brand.Name = updateBrandRequest.Name;
        brand.Description = updateBrandRequest.Description;
        brand.Status = updateBrandRequest.Status;

        //_brandRepository.Update(brand);
        await _brandRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Brands", new { area = SD.ADMIN_AREA });
        return Created(url, new Response()
        {
            Message = "Update Brand Successfully",
            Status = 201
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetOneAsync(e => e.Id == id,
            cancellationToken: cancellationToken);

        if (brand is null) return NotFound();

        // Remove old Logo from wwwroot
        _brandService.RemoveImg(brand.Logo);

        _brandRepository.Delete(brand);
        await _brandRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Brands", new { area = SD.ADMIN_AREA });
        return Created(url, new Response()
        {
            Message = "Delete Brand Successfully",
            Status = 201
        });
    }
}
