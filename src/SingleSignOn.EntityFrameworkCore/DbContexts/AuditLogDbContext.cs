using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Skoruba.AuditLogging.EntityFramework.DbContexts;
using Skoruba.AuditLogging.EntityFramework.Entities;

namespace SingleSignOn.EntityFrameworkCore.DbContexts
{
    public class AuditLogDbContext : DbContext, IAuditLoggingDbContext<AuditLog>
    {
        public AuditLogDbContext(DbContextOptions<AuditLogDbContext> dbContextOptions)
            : base(dbContextOptions)
        {

        }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        public DbSet<AuditLog> AuditLog { get; set; }
    }
}








