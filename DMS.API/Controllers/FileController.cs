using System;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;



namespace DMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Add JWT authentication
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".txt" };

        public FileController(IWebHostEnvironment env, ILogger<FileController> logger)
        {
            _env = env;
        }

        [HttpGet("{*fileName}")]
        public IActionResult Download(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || ContainsInvalidChars(fileName))
                {
                    return BadRequest("Invalid file name");
                }

                // Validate file extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!Array.Exists(_allowedExtensions, x => x == extension))
                {
                    return BadRequest("File type not allowed");
                }

                var root = _env.ContentRootPath;
                var fullPath = Path.Combine(root, "Storage", fileName);

                // Prevent directory traversal
                var normalizedPath = Path.GetFullPath(fullPath);
                var normalizedRoot = Path.GetFullPath(Path.Combine(root, "Storage"));
                if (!normalizedPath.StartsWith(normalizedRoot))
                {
                    return BadRequest("Invalid file path");
                }

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new
                    {
                        Message = "File not found"
                    });
                }

                return PhysicalFile(fullPath, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        static bool ContainsInvalidChars(string fileName)
        {
            return fileName.Contains("..");
        }
    }
}
