using Stripe.Climate;

namespace Ecommerce.API.Services;

public class OrderService : IOrderService
{
    public string? SaveImg(IFormFile logo)
    {
        try
        {
            var fileName = $"{DateTime.Now.ToString("dd_MM_yyyy")}_{Guid.NewGuid()}{Path.GetExtension(logo.FileName)}";
            //14_4_2026_509ksdfjskl59034509.png

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\order_rate", fileName);

            using (var stream = System.IO.File.Create(filePath))
            {
                logo.CopyTo(stream);
            }

            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public bool RemoveImg(string img)
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\order_rate", img);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            Console.WriteLine($"Remove old img from wwwroot");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
}
