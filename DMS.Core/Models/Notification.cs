using System;
using DMS.Shared.DTOs;

namespace DMS.Core.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
        public Guid? DocumentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}