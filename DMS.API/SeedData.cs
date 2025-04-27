using DMS.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using DMS.Infrastructure.Data;
using System.Linq;
using DMS.Shared.DTOs;
using System.Collections.Generic;
using System.IO;

namespace DMS.API
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed roles 
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Seed admin user 
            var adminUser = await userManager.FindByNameAsync("admin");

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Add random normal user
            var normalUser = await userManager.FindByNameAsync("user");

            if (normalUser == null)
            {
                normalUser = new ApplicationUser
                {
                    UserName = "user",
                    Email = "user@example.com",
                    FirstName = "Normal",
                    LastName = "User",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(normalUser, "User123!");
                await userManager.AddToRoleAsync(normalUser, "User");
            }

            // Seed documents and notifications if less than 10 documents exist
            if (await dbContext.Documents.CountAsync() < 10)
            {
                // Get all users for random assignment
                var users = await userManager.Users.ToListAsync();
                if (users.Count == 0) return;

                var random = new Random();
                var documentCount = 100;

                // Create directory for document storage if it doesn't exist
                var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                if (!Directory.Exists(storageDir))
                {
                    Directory.CreateDirectory(storageDir);
                }

                // Sample categories and tags
                string[] categories = { "Engineering", "Marketing", "Finance", "Sales", "Operations", "HR" };
                string[] tags = { "Invoice", "Report", "Contract", "Receipt" };

                // Create 100 random documents
                for (int i = 0; i < documentCount; i++)
                {
                    // Select random user
                    var user = users[random.Next(users.Count)];

                    string[] documentTypes = { "Report", "Plan", "Proposal", "Guidelines", "Analysis", "Manual", "Policy", "Brief" };
                    string[] departments = { "HR", "Finance", "Engineering", "Marketing", "Operations", "Sales" };
                    string[] projects = { "Project Alpha", "Q4 Initiative", "2023 Expansion", "Infra Upgrade", "GPDR Compliance" };
                    string[] statuses = { "Draft", "Final", "Revised", "Pending Review", "Approved" };
                    string[] verbs = { "Drafted", "Compiled", "Prepared", "Generated", "Developed" };

                    // Inside the document creation loop
                    var document = new Document
                    {
                        Id = Guid.NewGuid(),
                        Title = $"{departments[random.Next(departments.Length)]} " + $"{projects[random.Next(projects.Length)]} " + $"{documentTypes[random.Next(documentTypes.Length)]} " + $"{(random.Next(2) == 0 ? statuses[random.Next(statuses.Length)] : $"v{random.Next(1, 5)}.0")}",
                        Description = $"{verbs[random.Next(verbs.Length)]} by " + $"{user.FirstName} {user.LastName} " + $"on {DateTime.UtcNow.AddDays(-random.Next(1, 365)):MMMM yyyy}. " + $"Contains {new[] { "strategic", "technical", "financial", "operational" }[random.Next(4)]} " + $"details about {new[] { "implementation", "compliance", "optimization", "execution" }[random.Next(4)]} " + $"of {projects[random.Next(projects.Length)]}.",
                        UploadedOn = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                        User = user,
                        UserId = user.Id,
                        Categories = categories.OrderBy(x => random.Next()).Take(random.Next(1, 4)).ToArray(),
                        Tags = tags.OrderBy(x => random.Next()).Take(1).ToArray()

                    };


                    dbContext.Documents.Add(document);

                    // Create document version
                    var dummyFilePath = Path.Combine(storageDir, $"dummy_file_{document.Id}.txt");

                    // Create a dummy file if it doesn't exist
                    if (!File.Exists(dummyFilePath))
                    {
                        await File.WriteAllTextAsync(dummyFilePath, $"Content for document {document.Title}");
                    }

                    var version = new DocumentVersion
                    {
                        Id = Guid.NewGuid(),
                        VersionNumber = 1,
                        StoragePath = dummyFilePath,
                        FileSizeInBytes = new FileInfo(dummyFilePath).Length,
                        User = user,
                        CreatedOn = document.UploadedOn,
                        Comment = "Initial version",
                        DocumentId = document.Id,
                        Document = document
                    };

                    dbContext.DocumentVersions.Add(version);

                    // Create notification for document upload
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Title = "Document Created",
                        Message = $"Document '{document.Title}' has been uploaded successfully.",
                        Type = NotificationType.DocumentUpdate,
                        DocumentId = document.Id,
                        CreatedAt = document.UploadedOn,
                        IsRead = random.Next(2) == 0 // 50% chance of being read
                    };

                    dbContext.Notification.Add(notification);

                    // Add some random additional versions for some documents (20% chance)
                    if (random.Next(5) == 0)
                    {
                        var additionalVersionCount = random.Next(1, 4);
                        for (int v = 0; v < additionalVersionCount; v++)
                        {
                            var versionNumber = v + 2; // Start from version 2
                            var versionDate = document.UploadedOn.AddDays(random.Next(1, 30));

                            var additionalVersion = new DocumentVersion
                            {
                                Id = Guid.NewGuid(),
                                VersionNumber = versionNumber,
                                StoragePath = dummyFilePath, // Reuse the same file for simplicity
                                FileSizeInBytes = new FileInfo(dummyFilePath).Length,
                                User = users[random.Next(users.Count)], // Random user for the version
                                CreatedOn = versionDate,
                                Comment = $"Update version {versionNumber}",
                                DocumentId = document.Id,
                                Document = document
                            };

                            dbContext.DocumentVersions.Add(additionalVersion);

                            // Create notification for version update
                            var versionNotification = new Notification
                            {
                                Id = Guid.NewGuid(),
                                UserId = user.Id,
                                Title = "Document Updated",
                                Message = $"Document '{document.Title}' has been updated to version {versionNumber}.",
                                Type = NotificationType.NewVersion,
                                DocumentId = document.Id,
                                CreatedAt = versionDate,
                                IsRead = random.Next(2) == 0 // 50% chance of being read
                            };

                            dbContext.Notification.Add(versionNotification);
                        }
                    }
                }

                // Add some random system notifications not related to documents
                string[] systemMessages = {
                    "System maintenance scheduled for next weekend",
                    "New feature released: Advanced document search",
                    "Please update your profile information",
                    "Welcome to the Document Management System",
                    "Your account settings have been updated"
                };

                foreach (var user in users)
                {
                    var notificationCount = random.Next(1, 4);
                    for (int n = 0; n < notificationCount; n++)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            Title = "Version Created",
                            Message = systemMessages[random.Next(systemMessages.Length)],
                            Type = NotificationType.AdminAction,
                            DocumentId = null,
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                            IsRead = random.Next(2) == 0 // 50% chance of being read
                        };

                        dbContext.Notification.Add(notification);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}