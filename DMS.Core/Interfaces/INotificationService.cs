using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMS.Shared.DTOs;

namespace DMS.Core.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, Guid? documentId = null);
        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count);
        Task MarkNotificationAsReadAsync(Guid notificationId);
        Task DeleteNotificationAsync(Guid notificationId);
    }
}