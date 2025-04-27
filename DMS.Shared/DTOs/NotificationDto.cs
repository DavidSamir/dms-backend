using System;

namespace DMS.Shared.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        public Guid? DocumentId { get; set; }
    }

    public enum NotificationType
    {
        DocumentUpdate,
        NewVersion,
        AdminAction
    }
}