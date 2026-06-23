namespace Ecommerce.API.DTOs.Requests
{
    public record ProductFilterRequest(string? query = null, decimal? minPrice = null, decimal? maxPrice = null, int? categoryId = null, int? brandId = null, bool lowQuantity = false);
}
