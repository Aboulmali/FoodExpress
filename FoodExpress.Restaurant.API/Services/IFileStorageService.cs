namespace FoodExpress.Restaurant.API.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "");
    Task DeleteFileAsync(string fileUrl);
}