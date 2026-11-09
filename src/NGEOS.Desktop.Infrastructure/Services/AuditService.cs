using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using NGEOS.Desktop.Infrastructure.Database;

namespace NGEOS.Desktop.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly NgeosDbContext _context;

        public AuditService(NgeosDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string entityName, string? entityId, string details)
        {
            try
            {
                var log = new AuditLog
                {
                    UserEmail = "CURRENT_USER", // Will be enriched or default
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch { }
        }

        public async Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
