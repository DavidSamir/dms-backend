using DMS.Core.Models;
using DMS.Shared.DTOs;

namespace DMS.Infrastructure.Mappers
{
    public static class DocumentVersionMapper
    {
        public static DocumentVersionDto ToDto(this DocumentVersion v)
        {
            return new DocumentVersionDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                StoragePath = v.StoragePath,
                FileSizeInBytes = v.FileSizeInBytes,
                CreatedOn = v.CreatedOn,
                Comment = v.Comment,
                DocumentId = v.DocumentId
            };
        }
    }
}
