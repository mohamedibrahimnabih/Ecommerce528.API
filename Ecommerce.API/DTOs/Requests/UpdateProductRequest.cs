using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.DTOs.Requests;

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool Status { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public IFormFile? MainImg { get; set; } = null!;
    public List<IFormFile> SubImgs { get; set; } = [];
}
