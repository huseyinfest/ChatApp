namespace ChatApp.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string fileName);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
