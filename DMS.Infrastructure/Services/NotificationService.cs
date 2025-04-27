using DMS.Core.Interfaces;
using DMS.Core.Models;
using DMS.Infrastructure.Data;
using DMS.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DMS.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;

        public NotificationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, Guid? documentId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                DocumentId = documentId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _dbContext.Notification.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int count)
        {
            return await _dbContext.Notification
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    DocumentId = n.DocumentId,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _dbContext.Notification.FindAsync(notificationId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _dbContext.Notification.FindAsync(notificationId);

            if (notification != null)
            {
                _dbContext.Notification.Remove(notification);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}