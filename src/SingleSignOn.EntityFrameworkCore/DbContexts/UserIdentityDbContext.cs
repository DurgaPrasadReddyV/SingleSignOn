using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SingleSignOn.EntityFrameworkCore.Constants;
using SingleSignOn.EntityFrameworkCore.Entities;

namespace SingleSignOn.EntityFrameworkCore.DbContexts
{
    public class UserIdentityDbContext : IdentityDbContext<UserIdentity, UserIdentityRole, string, UserIdentityUserClaim, UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken>
    {
        public UserIdentityDbContext(DbContextOptions<UserIdentityDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureIdentityContext(builder);
        }

        private void ConfigureIdentityContext(ModelBuilder builder)
        {
            builder.Entity<UserIdentityRole>().ToTable(TableNames.IdentityRoles);
            builder.Entity<UserIdentityRoleClaim>().ToTable(TableNames.IdentityRoleClaims);
            builder.Entity<UserIdentityUserRole>().ToTable(TableNames.IdentityUserRoles);

            builder.Entity<UserIdentity>().ToTable(TableNames.IdentityUsers);
            builder.Entity<UserIdentityUserLogin>().ToTable(TableNames.IdentityUserLogins);
            builder.Entity<UserIdentityUserClaim>().ToTable(TableNames.IdentityUserClaims);
            builder.Entity<UserIdentityUserToken>().ToTable(TableNames.IdentityUserTokens);
        }
    }
}







