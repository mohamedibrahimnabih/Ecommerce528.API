namespace Ecommerce.API.DTOs.Requests;

public class ProductFilterResponse
{
    public string? Query { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool LowQuantity { get; set; } = false;
}
