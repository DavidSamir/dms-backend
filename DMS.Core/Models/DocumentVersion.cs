namespace DMS.Core.Models
{
    public class DocumentVersion
    {

        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public required string StoragePath { get; set; }
        public long FileSizeInBytes { get; set; }
        public required ApplicationUser User { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? Comment { get; set; }
        public Guid DocumentId { get; set; }
        public required Document Document { get; set; }
    }
}
