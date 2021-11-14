using System;
using System.Reflection;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;
using SqlMigrationAssembly = IdentityServer.Admin.EntityFramework.SqlServer.Helpers.MigrationAssembly;

namespace IdentityServer.Admin.Configuration.Database
{
    public static class MigrationAssemblyConfiguration
    {
        public static string GetMigrationAssemblyByProvider(DatabaseProviderConfiguration databaseProvider)
        {
            return typeof(SqlMigrationAssembly).GetTypeInfo().Assembly.GetName().Name;
        }
    }
}







