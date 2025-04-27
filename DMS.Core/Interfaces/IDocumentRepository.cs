using DMS.Core.Models;

namespace DMS.Core.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<IEnumerable<Document>> GetDocumentsByUserIdAsync(string userId);
        Task<IEnumerable<DocumentVersion>> GetDocumentVersionsByIdAsync(Guid documentId);
        Task<Document> GetDocumentWithVersionsAsync(Guid id);
        Task<DocumentVersion> GetVersionByIdAsync(Guid versionId);
        Task<IEnumerable<DocumentVersion>> GetVersionsByDocumentIdAsync(Guid documentId);
        Task<bool> DocumentExistsAsync(Guid documentId);
        Task AddVersionAsync(DocumentVersion version);
        Task<IEnumerable<DocumentVersion>> GetVersionsByDocumentIdsAsync(IEnumerable<Guid> documentIds);
    }
}