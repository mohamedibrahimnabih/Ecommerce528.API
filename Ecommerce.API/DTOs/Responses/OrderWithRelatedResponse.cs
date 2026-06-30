namespace Ecommerce.API.DTOs.Responses;

public class OrderWithRelatedResponse
{
    public IEnumerable<Order> Orders { get; set; } = [];

    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public string? Query { get; set; }
}
