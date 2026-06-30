namespace Ecommerce.API.DTOs.Requests;

public class CreateReviewRequest
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string Comment { get; set; }
    public int Rate { get; set; }
    public IFormFile Img { get; set; }
}
