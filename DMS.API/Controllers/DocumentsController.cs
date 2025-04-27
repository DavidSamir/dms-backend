using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMS.Core.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DMS.Core.Interfaces;
using DMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.IO;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace DMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public DocumentsController(
            IDocumentService documentService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager
        )
        {
            _documentService = documentService;
            _notificationService = notificationService;
            _userManager = userManager;
        }


        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] DocumentQueryParams queryParams)
        {
            if (queryParams.PageNumber < 1) queryParams.PageNumber = 1;
            if (queryParams.PageSize < 1) queryParams.PageSize = 10;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Admin"))
            {
                // Admins can use UserIdFilter from query to filter by a user, 
                // or leave it null to get all records
            }
            else
            {
                queryParams.UserIdFilter = currentUserId;

                if (currentUserId == null)
                {
                    return Unauthorized();
                }
            }
            var result = await _documentService.GetAllDocumentsFilterAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(Guid id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound();
                }
                if (!IsOwnerOrAdmin(document.UserId))
                {
                    return Forbid();
                }

                return Ok(document);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/versions")]
        public async Task<IActionResult> GetDocumentVersions(Guid id)
        {
            try
            {
                var versions = await _documentService.GetDocumentVersionsAsync(id);
                return Ok(versions);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromForm] CreateDocumentDto createDto, IFormFile file)
        {
            if (file == null)
                return BadRequest("File is required");

            var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(fileExt))
                return BadRequest("Invalid file type");

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("File too large");

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized();
                }
                var document = await _documentService.CreateDocumentAsync(createDto, file, userId);

                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

                foreach (var adminId in adminUsers.Select(u => u.Id))
                {
                    await _notificationService.CreateNotificationAsync(
                        adminId,
                        "Document Created",
                        $"A new document '{document.Title}' has been created by {User.FindFirstValue(ClaimTypes.Name)}",
                        NotificationType.AdminAction,
                        document.Id
                    );
                }

                return CreatedAtAction(
                    nameof(GetDocument),
                    new { id = document.Id },
                    document
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDto)
        {
            try
            {
                var existingDocument = await _documentService.GetDocumentByIdAsync(id);
                if (existingDocument == null)
                {
                    return NotFound();
                }

                // if (!await IsOwnerOrAdmin(existingDocument.UserName))
                if (!IsOwnerOrAdmin(existingDocument.UserId))

                {
                    return Forbid();
                }

                var updatedDocument = await _documentService.UpdateDocumentAsync(id, updateDto);

                // Notify document owner about updates
                await _notificationService.CreateNotificationAsync(
                    existingDocument.UserName,
                    "Document Updated",
                    $"Your document '{existingDocument.Title}' has been updated",
                    NotificationType.DocumentUpdate,
                    id
                );

                return Ok(updatedDocument);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("file")]
        [Authorize]
        public async Task<IActionResult> AddDocumentVersion([FromForm] Guid id, [FromForm] string comment, IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is required");
            }

            try
            {
                var existingDocument = await _documentService.GetDocumentByIdAsync(id);
                if (existingDocument == null)
                {
                    return NotFound();
                }
                if (!IsOwnerOrAdmin(existingDocument.UserId))
                {
                    return Forbid();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in claims.");
                }

                var version = await _documentService.AddDocumentVersionAsync(id, file, comment, userId);

                // Notify document owner about new version
                await _notificationService.CreateNotificationAsync(
                    existingDocument.UserId,
                    "Document Updated",
                    $"A new version has been added to document '{existingDocument.Title}'",
                    NotificationType.NewVersion,
                    id
                );

                return Ok(version);
            }
            catch (KeyNotFoundException)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while adding the document version.");
            }
        }

        [HttpPost("{id}/revert/{versionId}")]
        public async Task<IActionResult> RevertToVersion(Guid id, Guid versionId)
        {
            try
            {
                var existingDocument = await _documentService.GetDocumentByIdAsync(id);
                if (existingDocument == null)
                {
                    return NotFound();
                }

                if (!IsOwnerOrAdmin(existingDocument.UserId))

                {
                    return Forbid();
                }

                var document = await _documentService.RevertToVersionAsync(id, versionId);
                return Ok(document);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            try
            {
                var existingDocument = await _documentService.GetDocumentByIdAsync(id);
                if (existingDocument == null)
                {
                    return NotFound();
                }

                if (!IsOwnerOrAdmin(existingDocument.UserId))
                {
                    return Forbid();
                }

                var result = await _documentService.DeleteDocumentAsync(id);
                return Ok(new { Success = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            if (!IsOwnerOrAdmin(document.UserId))
                return Forbid();

            var latestVersion = await _documentService.GetLatestVersionAsync(id);
            if (latestVersion == null)
                return NotFound();

            var fileBytes = await _documentService.DownloadDocumentAsync(id);

            var fileName = Path.GetFileName(latestVersion.StoragePath);

            return File(fileBytes, "application/octet-stream", fileName);
        }

        [HttpGet("categories/search")]
        public async Task<IActionResult> SearchCategories([FromQuery] string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return BadRequest("Search prefix is required");
            }

            var allDocuments = await _documentService.GetAllDocumentsAsync();

            var matchingCategories = allDocuments
                .SelectMany(d => d.Categories)
                .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(matchingCategories);
        }

        [HttpGet("tags/search")]
        public async Task<IActionResult> SearchTags([FromQuery] string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return BadRequest("Search prefix is required");
            }

            var allDocuments = await _documentService.GetAllDocumentsAsync();

            var matchingTags = allDocuments
                .SelectMany(d => d.Tags)
                .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(matchingTags);
        }

        private bool IsOwnerOrAdmin(string ownerUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return User.IsInRole("Admin") || currentUserId == ownerUserId;
        }
    }
}