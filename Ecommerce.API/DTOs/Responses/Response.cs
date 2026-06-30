namespace Ecommerce.API.DTOs.Responses;

public class Response
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public string[]? Body { get; set; }
    public int Status { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
}
