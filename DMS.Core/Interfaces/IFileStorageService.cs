using Microsoft.AspNetCore.Http;

namespace DMS.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string directory);
        Task<byte[]> GetFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
        bool IsValidFile(IFormFile file, long maxSizeInBytes);
    }
}
