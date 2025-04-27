namespace DMS.Shared.DTOs
{
    public class VersionDto
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public int VersionNumber { get; set; }
        public long FileSizeInBytes { get; set; }
        public required string Comment { get; set; }
        public required string StoragePath { get; set; }
        public required string UserName { get; set; }
        public DateTime CreatedDate { get; set; }

    }
}