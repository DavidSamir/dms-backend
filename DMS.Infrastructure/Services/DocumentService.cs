using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DMS.Core.Interfaces;
using DMS.Core.Models;
using DMS.Infrastructure.Mappers;
using DMS.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DMS.Infrastructure.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly long _maxFileSizeInBytes = 10 * 1024 * 1024;

        public DocumentService(
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        public async Task<PaginatedResult<DocumentDto>> GetAllDocumentsFilterAsync(DocumentQueryParams queryParams)
        {
            var allDocuments = await _documentRepository.GetAllAsync(
                includes: [d => d.User]
            );

            // Apply filters
            var filteredDocuments = allDocuments.AsEnumerable();

            if (!string.IsNullOrEmpty(queryParams.Title))
            {
                filteredDocuments = filteredDocuments.Where(d =>
                    d.Title.Contains(queryParams.Title, StringComparison.OrdinalIgnoreCase) || 
                    d.Description.Contains(queryParams.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (queryParams.StartDate.HasValue)
            {
                filteredDocuments = filteredDocuments.Where(d =>
                    d.UploadedOn >= queryParams.StartDate.Value);
            }

            if (queryParams.EndDate.HasValue)
            {
                filteredDocuments = filteredDocuments.Where(d =>
                    d.UploadedOn <= queryParams.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Categories))
            {
                var filterCategories = queryParams.Categories.Split(',');
                filteredDocuments = filteredDocuments.Where(d =>
                    d.Categories.Any(c => filterCategories.Contains(c)));
            }

            if (!string.IsNullOrEmpty(queryParams.Tags))
            {
                var filterTags = queryParams.Tags.Split(',');
                filteredDocuments = filteredDocuments.Where(d =>
                    d.Tags.Any(t => filterTags.Contains(t)));
            }

            if (!string.IsNullOrEmpty(queryParams.UserIdFilter))
            {
                filteredDocuments = filteredDocuments.Where(d =>
                    d.UserId == queryParams.UserIdFilter);
            }

            // Calculate total count before pagination
            var totalCount = filteredDocuments.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

            // Apply pagination
            var pagedDocuments = filteredDocuments
                .OrderByDescending(d => d.UploadedOn) 
                .Skip((int)((queryParams.PageNumber - 1) * queryParams.PageSize))
                .Take((int)queryParams.PageSize)
                .ToList();

            // Batch load versions for all paginated documents
            var documentIds = pagedDocuments.Select(d => d.Id).ToList();
            var allVersions = await _documentRepository.GetVersionsByDocumentIdsAsync(documentIds);

            // Create DTOs
            var documentDtos = new List<DocumentDto>();

            foreach (var document in pagedDocuments)
            {
                var versions = allVersions
                    .Where(v => v.DocumentId == document.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToList();

                documentDtos.Add(new DocumentDto
                {
                    Id = document.Id,
                    Title = document.Title,
                    Description = document.Description,
                    FileSizeInBytes = versions.FirstOrDefault()?.FileSizeInBytes ?? 0,
                    UploadedOn = document.UploadedOn,
                    UserName = document.User?.UserName,
                    UserId = document.User?.Id,
                    Tags = document.Tags,
                    Categories = document.Categories,
                    VersionCount = versions.Count,
                    Path = versions.FirstOrDefault()?.StoragePath
                });
            }

            return new PaginatedResult<DocumentDto>
            {
                Items = documentDtos,
                TotalCount = totalCount,
                PageNumber = (int)queryParams.PageNumber,
                PageSize = (int)queryParams.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync()
        {
            var documents = await _documentRepository.GetAllAsync();
            var documentDtos = new List<DocumentDto>();

            foreach (var document in documents)
            {
                var versions = await _documentRepository.GetVersionsByDocumentIdAsync(document.Id);

                var versionCount = versions.Count();
                var latestVersion = versions.FirstOrDefault();

                documentDtos.Add(new DocumentDto
                {
                    Id = document.Id,
                    Title = document.Title,
                    Description = document.Description,
                    FileSizeInBytes = latestVersion.FileSizeInBytes,
                    UploadedOn = document.UploadedOn,
                    UserName = document.User?.UserName,
                    UserId = document.User?.Id,
                    Categories = document.Categories,
                    Tags = document.Tags,
                    VersionCount = versionCount,
                    Path = latestVersion?.StoragePath
                });
            }

            return documentDtos;
        }

        public async Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(string userId)
        {
            var documents = await _documentRepository.GetDocumentsByUserIdAsync(userId);
            return documents.Select(d => MapToDocumentDto(d));
        }

        public async Task<DocumentDto> GetDocumentByIdAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Document with ID {id} not found");

            var versions = await _documentRepository.GetVersionsByDocumentIdAsync(id);

            var documentDto = MapToDocumentDto(document);
            documentDto.Versions = versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new VersionDto
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    Comment = v.Comment,
                    UserId = v.User.Id,
                    UserName = v.User.FirstName + " " + v.User.LastName,
                    FileSizeInBytes = v.FileSizeInBytes,
                    StoragePath = v.StoragePath,
                })
                .ToList();
            documentDto.Path = documentDto.Versions.FirstOrDefault()?.StoragePath;

            return documentDto;
        }

        public async Task<IEnumerable<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId)
        {
            var documentExists = await _documentRepository.DocumentExistsAsync(documentId);

            if (!documentExists)
                throw new KeyNotFoundException($"Document with ID {documentId} not found");

            var versions = await _documentRepository.GetVersionsByDocumentIdAsync(documentId);

            return versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.ToDto());
        }

        public async Task<DocumentVersionDto> AddDocumentVersionAsync(Guid documentId, IFormFile file, string comment, string userId)
        {
            if (!_fileStorageService.IsValidFile(file, _maxFileSizeInBytes))
            {
                throw new ArgumentException("Invalid file or file size exceeds limit");
            }

            var document = await _documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document with ID {documentId} not found");
            var now = DateTime.UtcNow;

            var filePath = await _fileStorageService.SaveFileAsync(file, "documents");

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User with ID {userId} not found");

            var existingVersions = await _documentRepository.GetVersionsByDocumentIdAsync(documentId);
            var newVersionNumber = existingVersions.Any() ? existingVersions.Max(v => v.VersionNumber) + 1 : 1;

            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                VersionNumber = newVersionNumber,
                CreatedOn = now,
                StoragePath = filePath,
                User = user,
                FileSizeInBytes = file.Length,
                Comment = comment ?? $"Version {newVersionNumber}",
                DocumentId = documentId,
                Document = document
            };


            await _documentRepository.AddVersionAsync(version);

            _documentRepository.Update(document);
            await _documentRepository.SaveChangesAsync();

            return version.ToDto();
        }

        public async Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto)
        {
            var document = await _documentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Document with ID {id} not found");
            document.Title = updateDto.Title;
            document.Description = updateDto.Description;

            _documentRepository.Update(document);
            await _documentRepository.SaveChangesAsync();

            return MapToDocumentDto(document);
        }

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Document with ID {id} not found");

            var versions = await _documentRepository.GetVersionsByDocumentIdAsync(id);

            foreach (var version in versions)
            {
                await _fileStorageService.DeleteFileAsync(version.StoragePath);
            }

            _documentRepository.Delete(document);

            return await _documentRepository.SaveChangesAsync();
        }

        public async Task<byte[]> DownloadDocumentAsync(Guid id)
        {
            if (!await _documentRepository.DocumentExistsAsync(id))
                throw new KeyNotFoundException($"Document with ID {id} not found");

            var versions = await _documentRepository.GetVersionsByDocumentIdAsync(id);
            var latest = versions
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No versions found for document {id}");

            return await _fileStorageService.GetFileAsync(latest.StoragePath);
        }

        public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto, IFormFile file, string userId)
        {
            try
            {
                if (!_fileStorageService.IsValidFile(file, _maxFileSizeInBytes))
                {
                    throw new ArgumentException("Invalid file or file size exceeds limits");
                }

                var now = DateTime.UtcNow;
                var filePath = await _fileStorageService.SaveFileAsync(file, "documents");

                var user = await _userManager.FindByIdAsync(userId)
                    ?? throw new KeyNotFoundException($"User with ID {userId} not found");

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    Title = createDto.Title,
                    Description = createDto.Description,
                    Categories = createDto.Categories,
                    Tags = createDto.Tags,
                    UploadedOn = now,
                    User = user
                };

                // Create initial version
                var version = new DocumentVersion
                {
                    Id = Guid.NewGuid(),
                    VersionNumber = 1,
                    CreatedOn = now,
                    StoragePath = filePath,
                    User = user,
                    FileSizeInBytes = file.Length,
                    Comment = "Initial version",
                    DocumentId = document.Id,
                    Document = document
                };

                await _documentRepository.AddAsync(document);
                await _documentRepository.AddVersionAsync(version);
                await _documentRepository.SaveChangesAsync();

                return MapToDocumentDto(document);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public async Task<DocumentDto> RevertToVersionAsync(Guid documentId, Guid versionId)
        {
            var document = await _documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document with ID {documentId} not found");

            var version = await _documentRepository.GetVersionByIdAsync(versionId) ?? throw new KeyNotFoundException($"Version with ID {versionId} not found");

            if (version.DocumentId != documentId)
                throw new ArgumentException($"Version {versionId} does not belong to document {documentId}");

            return MapToDocumentDto(document);
        }

        public async Task<byte[]> DownloadDocumentVersionAsync(Guid documentId, Guid versionId)
        {
            var version = await _documentRepository.GetVersionByIdAsync(versionId) ?? throw new KeyNotFoundException($"Version with ID {versionId} not found");

            if (version.DocumentId != documentId)
                throw new ArgumentException($"Version {versionId} does not belong to document {documentId}");

            return await _fileStorageService.GetFileAsync(version.StoragePath);
        }

        public async Task<DocumentVersionDto> GetLatestVersionAsync(Guid documentId)
        {
            if (!await _documentRepository.DocumentExistsAsync(documentId))
                throw new KeyNotFoundException($"Document with ID {documentId} not found");

            var versions = await _documentRepository.GetVersionsByDocumentIdAsync(documentId);
            var latest = versions
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No versions found for document {documentId}");

            return latest.ToDto();
        }

        private static DocumentDto MapToDocumentDto(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                UploadedOn = document.UploadedOn,
                UserId = document.User.Id,
                UserName = document.User?.FirstName + " " + document.User?.LastName,
            };
        }

    }
}