using System;
using System.Threading.Tasks;
using DMS.Core.Interfaces;
using DMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using DMS.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;

namespace DMS.Infrastructure.Repositories
{
    public class DocumentRepository(ApplicationDbContext context) : Repository<Document>(context), IDocumentRepository
    {
        public async Task AddVersionAsync(DocumentVersion version)
        {
            await _context.DocumentVersions.AddAsync(version);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DocumentExistsAsync(Guid documentId)
        {
            return await _context.Documents.AnyAsync(d => d.Id == documentId);
        }

        public async Task<IEnumerable<DocumentVersion>> GetDocumentVersionsByIdAsync(Guid documentId)
        {
            return await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<Document> GetDocumentWithVersionsAsync(Guid id)
        {
            var document = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document != null)
            {
                var versions = await _context.DocumentVersions
                    .Where(v => v.DocumentId == id)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();
            }

            return document;
        }

        public async Task<DocumentVersion> GetVersionByIdAsync(Guid versionId)
        {
            return await _context.DocumentVersions
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.Id == versionId);
        }

        public async Task<IEnumerable<DocumentVersion>> GetVersionsByDocumentIdAsync(Guid documentId)
        {
            return await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .Include(v => v.User)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetDocumentsByUserIdAsync(string userId)
        {
            return await _context.Documents
                .Include(d => d.User)
                .Where(d => d.User.Id == userId)
                .ToListAsync();
        }
        public async Task<IEnumerable<DocumentVersion>> GetVersionsByDocumentIdsAsync(IEnumerable<Guid> documentIds)
        {
            if (documentIds == null || !documentIds.Any())
            {
                return Enumerable.Empty<DocumentVersion>();
            }

            return await _context.DocumentVersions
                .Where(v => documentIds.Contains(v.DocumentId))
                .ToListAsync();
        }
    }
}
