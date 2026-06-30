namespace Ecommerce.API.DTOs.Responses;

public class OrderWithItemsResponse
{
    public Order Order { get; set; }
    public IEnumerable<OrderItem> OrderItems { get; set; }
}
