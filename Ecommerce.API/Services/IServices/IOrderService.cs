namespace Ecommerce.API.Services;

public interface IOrderService
{
    /// <summary>
    /// Saves the provided image file to the server and returns the generated file name.
    /// </summary>
    /// <remarks>The image is saved in the 'wwwroot/images/order_rate' directory of the application's
    /// root. The file name is generated using the current date and a unique identifier to avoid
    /// collisions.</remarks>
    /// <param name="img">The image file to be saved. Must not be null and should contain a valid file name and content.</param>
    /// <returns>The generated file name of the saved image if the operation succeeds; otherwise, null.</returns>
    string? SaveImg(IFormFile img);

    /// <summary>
    /// Removes an image file with the specified name from the review img images directory.
    /// </summary>
    /// <remarks>If the specified file does not exist, the method returns true. Any errors encountered
    /// during file deletion will result in a return value of false.</remarks>
    /// <param name="reviewName">The name of the image file to remove. This should include the file extension. Cannot be null or empty.</param>
    /// <returns>true if the image file was successfully removed or did not exist; otherwise, false.</returns>
    bool RemoveImg(string img);
}
