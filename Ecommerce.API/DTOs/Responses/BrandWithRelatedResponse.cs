namespace Ecommerce.API.DTOs.Responses;

public class BrandWithRelatedResponse
{
    public IEnumerable<Brand> Brands { get; set; } = [];

    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public string? Query { get; set; }
}
