using DMS.Core.Models;
using DMS.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DMS.Core.Interfaces
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync();
        Task<PaginatedResult<DocumentDto>> GetAllDocumentsFilterAsync(DocumentQueryParams queryParams);
        Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(string userId);
        Task<DocumentDto> GetDocumentByIdAsync(Guid id);
        Task<IEnumerable<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId);
        Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto, IFormFile file, string userId);
        Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto);
        Task<DocumentVersionDto> AddDocumentVersionAsync(Guid documentId, IFormFile file, string comment, string userId);
        Task<DocumentDto> RevertToVersionAsync(Guid documentId, Guid versionId);
        Task<bool> DeleteDocumentAsync(Guid id);
        Task<byte[]> DownloadDocumentAsync(Guid id);
        Task<byte[]> DownloadDocumentVersionAsync(Guid documentId, Guid versionId);
        Task<DocumentVersionDto?> GetLatestVersionAsync(Guid documentId);

    }
}
