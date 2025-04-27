namespace DMS.Shared.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public long FileSizeInBytes { get; set; }
        public DateTime UploadedOn { get; set; }
        public required string UserName { get; set; }
        public required string UserId { get; set; }
        public string[] Tags { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public int VersionCount { get; set; }
        public string? Path { get; set; }
        public List<VersionDto>? Versions { get; set; } = [];

    }

    public class CreateDocumentDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string[] Categories { get; set; } = [];
        public string[] Tags { get; set; } = [];
    }

    public class UpdateDocumentDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string[] Categories { get; set; } = [];
    }

    public class DocumentVersionDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public required string StoragePath { get; set; }
        public long FileSizeInBytes { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? Comment { get; set; }
        public Guid DocumentId { get; set; }
    }
    public class DocumentQueryParams
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? Title { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserIdFilter { get; set; } 
        public string? Tags { get; set; }
        public string? Categories { get; set; }
    }
}