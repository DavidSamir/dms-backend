using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DMS.Core.Interfaces;
using DMS.Core.Models;
using DMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IO; // Add this for DriveInfo

namespace DMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDocumentRepository _documentRepository;

        public ReportsController(
            IDocumentService documentService,
            UserManager<ApplicationUser> userManager,
            IDocumentRepository documentRepository)
        {
            _documentService = documentService;
            _userManager = userManager;
            _documentRepository = documentRepository;
        }

        [HttpGet("storage-statistics")]
        public async Task<IActionResult> GetStorageStatistics()
        {
            try
            {

                var documents = await _documentService.GetAllDocumentsAsync();


                // Get all document versions to calculate total storage correctly
                var documentIds = documents.Select(d => d.Id).ToList();
                var allVersions = await _documentRepository.GetVersionsByDocumentIdsAsync(documentIds);

                // Calculate total storage
                long totalStorage = 0;
                var tagBreakdown = new Dictionary<string, long>
                {
                    { "Invoice", 0 },
                    { "Report", 0 },
                    { "Contract", 0 },
                    { "Receipt", 0 },
                    { "Other", 0 }
                };

                var documentTagsMap = documents.ToDictionary(
                    d => d.Id,
                    d => d.Tags
                );


                foreach (var version in allVersions)
                {
                    totalStorage += version.FileSizeInBytes;

                    // Get the document's tags for categorization
                    if (documentTagsMap.TryGetValue(version.DocumentId, out var tags) && tags != null && tags.Any())
                    {
                        bool categorized = false;
                        // Add this before the foreach loop for versions
                        var tagMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "Invoice", "Invoice" },
                            { "Invoices", "Invoice" },
                            { "Bill", "Invoice" },
                            { "Bills", "Invoice" },
                            { "Report", "Report" },
                            { "Reports", "Report" },
                            { "Contract", "Contract" },
                            { "Contracts", "Contract" },
                            { "Agreement", "Contract" },
                            { "Agreements", "Contract" },
                            { "Receipt", "Receipt" },
                            { "Receipts", "Receipt" }
                        };

                        // Then modify the tag checking logic
                        foreach (var tag in tags)
                        {
                            if (string.IsNullOrWhiteSpace(tag)) continue;

                            var normalizedTag = tag.Trim();
                            if (tagMapping.TryGetValue(normalizedTag, out var mappedCategory))
                            {
                                tagBreakdown[mappedCategory] += version.FileSizeInBytes;
                                categorized = true;
                                break;
                            }
                        }

                        // If no matching tag found, add to "Other"
                        if (!categorized)
                        {
                            tagBreakdown["Other"] += version.FileSizeInBytes;
                        }
                    }
                    else
                    {
                        // If document not found in map or has no tags, add to "Other"
                        tagBreakdown["Other"] += version.FileSizeInBytes;
                    }
                }

                // Calculate percentages
                var percentages = new List<int>();
                foreach (var tag in new[] { "Invoice", "Report", "Contract", "Receipt", "Other" })
                {
                    if (totalStorage > 0)
                    {
                        int percentage = (int)Math.Round((double)tagBreakdown[tag] / totalStorage * 100);
                        percentages.Add(percentage);
                    }
                    else
                    {
                        percentages.Add(0);
                    }
                }

                // Ensure percentages add up to 100%
                int sum = percentages.Sum();
                if (sum != 100 && sum > 0)
                {
                    // Adjust the largest percentage to make sum 100
                    int largestIndex = percentages.IndexOf(percentages.Max());
                    percentages[largestIndex] += (100 - sum);
                }

                return Ok(new
                {
                    Total = totalStorage,
                    Split = percentages,
                    Breakdown = new
                    {
                        Invoice = tagBreakdown["Invoice"],
                        Report = tagBreakdown["Report"],
                        Contract = tagBreakdown["Contract"],
                        Receipt = tagBreakdown["Receipt"],
                        Other = tagBreakdown["Other"]
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("department-statistics")]
        public async Task<IActionResult> GetDepartmentStatistics()
        {
            try
            {
                // Get all documents
                var documents = await _documentService.GetAllDocumentsAsync();

                // Get all users
                var users = await _userManager.Users.ToListAsync();

                // Create a mapping of departments based on categories
                var departmentMapping = new Dictionary<string, string>
                {
                    { "Financial", "Finance" },
                    { "Technical", "Engineering" },
                    { "Marketing", "Marketing" },
                    { "Sales", "Sales" },
                    { "HR", "HR" },
                    { "Operations", "Operations" }
                };

                // Count documents by department
                var departmentCounts = new Dictionary<string, int>
                {
                    { "Engineering", 0 },
                    { "Marketing", 0 },
                    { "Sales", 0 },
                    { "Operations", 0 },
                    { "HR", 0 },
                    { "Finance", 0 }
                };

                foreach (var doc in documents)
                {
                    bool assigned = false;
                    foreach (var category in doc.Categories)
                    {
                        if (departmentMapping.TryGetValue(category, out string department))
                        {
                            departmentCounts[department]++;
                            assigned = true;
                            break;
                        }
                    }

                    // If no department mapping found, assign to Operations as default
                    if (!assigned)
                    {
                        departmentCounts["Operations"]++;
                    }
                }

                // Calculate total and percentages
                int totalDocuments = departmentCounts.Values.Sum();
                var result = new List<object>();

                foreach (var dept in departmentCounts)
                {
                    double percentage = totalDocuments > 0
                        ? Math.Round((double)dept.Value / totalDocuments * 100)
                        : 0;

                    result.Add(new
                    {
                        name = dept.Key,
                        value = percentage,
                        headcount = dept.Value,
                        color = GetDepartmentColor(dept.Key)
                    });
                }

                return Ok(result.OrderByDescending(d => ((dynamic)d).value));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            try
            {
                // Get all documents and users
                var documents = await _documentService.GetAllDocumentsAsync();
                var users = await _userManager.Users.ToListAsync();
                var documentIds = documents.Select(d => d.Id).ToList();
                var allVersions = await _documentRepository.GetVersionsByDocumentIdsAsync(documentIds);

                long totalStorage = allVersions.Sum(v => v.FileSizeInBytes);
                
                // Get disk space information
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var driveInfo = new DriveInfo(Path.GetPathRoot(appDirectory));
                
                // Get total disk space and used disk space
                long totalDiskSpace = driveInfo.TotalSize;
                long usedDiskSpace = totalDiskSpace - driveInfo.AvailableFreeSpace;
                
                // Calculate utilization as percentage of total disk space that is used
                double storageUtilization = totalDiskSpace > 0 
                    ? (double)usedDiskSpace / totalDiskSpace 
                    : 0;
                    
                // Format for display using our helper method
                string usedDiskSpaceFormatted = FormatFileSize(usedDiskSpace);
                string totalDiskSpaceFormatted = FormatFileSize(totalDiskSpace);
                string storageFraction = $"{usedDiskSpaceFormatted}/{totalDiskSpaceFormatted}";

                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var driveInformation = new DriveInfo(Path.GetPathRoot(appDirectory));
                long availableDiskSpace = driveInfo.AvailableFreeSpace;
                
                double diskSpaceUtilization = availableDiskSpace > 0
                    ? (double)totalStorage / availableDiskSpace 
                    : 0;
                    
                double usedStorageMB = Math.Round((double)totalStorage / (1024 * 1024), 2);
                double availableStorageMB = Math.Round((double)availableDiskSpace / (1024 * 1024), 2);
                string storageMBFraction = $"{usedStorageMB}/{availableStorageMB}MB";


                int approvedDocuments = documents.Count();

                var activeUserIds = documents.Select(d => d.UserId).Distinct().ToList();
                int activeUsers = activeUserIds.Count;
                double activeUsersRate = users.Count > 0 
                    ? (double)activeUsers / users.Count 
                    : 0;
                string activeUsersFraction = $"{activeUsers}/{users.Count}";

                return Ok(new[]
                {
                    new {
                        label = "Storage Utilization",
                        value = storageUtilization,
                        percentage = $"{Math.Round(storageUtilization * 100, 1)}%",
                        fraction = storageFraction
                    },
                    new {
                        label = "Most Active Users",
                        value = activeUsersRate,
                        percentage = $"{Math.Round(activeUsersRate * 100, 1)}%",
                        fraction = activeUsersFraction
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("report-data")]
        public async Task<IActionResult> GetReportData()
        {
            try
            {
                // Get all documents
                var documents = await _documentService.GetAllDocumentsAsync();
                var documentIds = documents.Select(d => d.Id).ToList();
                var allVersions = await _documentRepository.GetVersionsByDocumentIdsAsync(documentIds);
        
                // Calculate category data
                var categoryData = documents
                    .SelectMany(d => d.Categories)
                    .GroupBy(c => c)
                    .Select(g => new 
                    {
                        name = g.Key,
                        count = g.Count()
                    })
                    .OrderByDescending(x => x.count)
                    .Take(10)
                    .ToList();
        
                // Calculate upload activity data
                var uploadActivityData = documents
                    .GroupBy(d => new { Month = d.UploadedOn.ToString("MMM yy") })
                    .Select(g => new
                    {
                        date = g.Key.Month,
                        uploads = g.Count()
                    })
                    .OrderBy(x => DateTime.ParseExact(x.date, "MMM yy", System.Globalization.CultureInfo.InvariantCulture))
                    .Take(15)
                    .ToList();
        
                // Calculate storage usage over time
                var storageData = allVersions
                    .GroupBy(v => new { Month = v.CreatedOn.ToString("MMM yy") })
                    .Select(g => new
                    {
                        date = g.Key.Month,
                        usage = (double)g.Sum(v => v.FileSizeInBytes)
                    })
                    .OrderBy(x => DateTime.ParseExact(x.date, "MMM yy", System.Globalization.CultureInfo.InvariantCulture))
                    .Take(15)
                    .ToList();
        
                return Ok(new
                {
                    categoryData,
                    uploadActivityData,
                    storageData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private string GetDepartmentColor(string department)
        {
            return department switch
            {
                "Engineering" => "bg-cyan-500 dark:bg-cyan-500",
                "Marketing" => "bg-blue-500 dark:bg-blue-500",
                "Sales" => "bg-indigo-500 dark:bg-indigo-500",
                "Operations" => "bg-violet-500 dark:bg-violet-500",
                "HR" => "bg-fuchsia-500 dark:bg-fuchsia-500",
                "Finance" => "bg-emerald-500 dark:bg-emerald-500",
                _ => "bg-gray-500 dark:bg-gray-500"
            };
        }
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{Math.Round(len, 2)}{sizes[order]}";
        }
    }
}