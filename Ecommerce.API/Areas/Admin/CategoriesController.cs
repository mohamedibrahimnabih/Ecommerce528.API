using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Ecommerce.API.Areas.Admin;

[Route("api/[area]/[controller]")]
[Area(SD.ADMIN_AREA)]
[ApiController]
[Authorize(Roles = $"{RoleNames.SUPER_ADMIN},{RoleNames.ADMIN}")]
public class CategoriesController(IRepository<Category> categoryRepository, IStringLocalizer<Localization> localizer) : ControllerBase
{
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, string? query = null, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAsync(cancellationToken: cancellationToken);

        // Filter
        if (query is not null)
            categories = categories.Where(e => e.Name.ToLower().Contains(query.ToLower().Trim()));

        // Pagination
        int totalPages = (int)Math.Ceiling(categories.Count() / 5.0);
        categories = categories.Skip((page - 1) * 5).Take(5);

        return Ok(new CategoryWithRelatedResponse()
        {
            Categories = categories.AsEnumerable(),
            TotalPages = totalPages,
            CurrentPage = page,
            Query = query
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);

        if (category is null) return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Category category, CancellationToken cancellationToken = default) // string name, string Description, bool status
    {
        //_context.Categories.Add(category);
        //_context.SaveChanges();

        await _categoryRepository.CreateAsync(category, cancellationToken);
        await _categoryRepository.CommitAsync(cancellationToken);

        //Response.Cookies.Append("success_notification", "Add Category Successfully", new()
        //{
        //    Expires = ,
        //    Domain =
        //});

        var url = Url.Action(nameof(GetAll), "Categories", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = _localizer["AddCategory"].Value,
            Status = 201
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Category category, CancellationToken cancellationToken = default) // string name, string Description, bool status
    {
        var categoryInDb = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);

        if (categoryInDb is null) return NotFound();

        categoryInDb.Name = category.Name;
        categoryInDb.Description = category.Description;
        categoryInDb.Status = category.Status;
        await _categoryRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Categories", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = _localizer["UpdateCategory"].Value,
            Status = 201
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetOneAsync(e => e.Id == id, cancellationToken: cancellationToken);

        if (category is null) return NotFound();

        _categoryRepository.Delete(category);
        await _categoryRepository.CommitAsync(cancellationToken);

        var url = Url.Action(nameof(GetAll), "Categories", new { area = SD.ADMIN_AREA });
        return Created(url, new SuccessResponse()
        {
            Message = _localizer["DeleteCategory"].Value,
            Status = 201
        });
    }
}
