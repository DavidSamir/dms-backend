using System;
using System.IO;
using System.Threading.Tasks;
using DMS.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DMS.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;

        public LocalFileStorageService(IConfiguration configuration)
        {
            _baseStoragePath = configuration["Storage:LocalPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");

            // Ensure storage directory exists
            Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string directory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null", nameof(file));

            // Create directory if it doesn't exist
            var directoryPath = Path.Combine(_baseStoragePath, directory);
            Directory.CreateDirectory(directoryPath);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(directory, fileName);
            var fullPath = Path.Combine(_baseStoragePath, filePath);

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_baseStoragePath, filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found", filePath);

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_baseStoragePath, filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public bool IsValidFile(IFormFile file, long maxSizeInBytes)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > maxSizeInBytes)
                return false;

            // Add additional file type validation if needed
            // var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".txt" };
            // if (!allowedTypes.Contains(Path.GetExtension(file.FileName).ToLower()))
            //     return false;

            return true;
        }
    }
}