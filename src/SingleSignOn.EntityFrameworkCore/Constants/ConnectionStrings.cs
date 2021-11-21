using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleSignOn.EntityFrameworkCore.Constants
{
    public static class ConnectionStrings
    {
        public const string LogDb = "LogDbConnection";
        public const string AuditLogDb = "AuditLogDbConnection";
        public const string UserIdentityDb = "UserIdentityDbConnection";
        public const string DataProtectionDb = "DataProtectionDbConnection";
        public const string IdentityServerConfigurationDb = "IdentityServerConfigurationDbConnection";
        public const string IdentityServerPersistedGrantDb = "IdentityServerPersistedGrantDbConnection";
    }
}
