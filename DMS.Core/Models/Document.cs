namespace DMS.Core.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public string[] Categories { get; set; } = [];
        public string[] Tags { get; set; } = [];
        public string UserId { get; set; }
    }
}