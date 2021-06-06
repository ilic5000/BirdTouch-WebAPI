using DbUp;
using DbUp.Engine;
using DbUp.Support;
using Microsoft.Extensions.Configuration;
using System;

namespace DatabaseMigrator
{
    public class Program
    {
        static int Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder()
                                        .SetBasePath(AppContext.BaseDirectory)
                                        .AddJsonFile("appsettings.json", optional: false)
                                        .AddEnvironmentVariables();

            var config = configBuilder.Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            var upgrader = DeployChanges.To
                                        .PostgresqlDatabase(connectionString)
                                        .WithScriptsFromFileSystem(path: "migrations/",
                                                                   sqlScriptOptions: new SqlScriptOptions
                                                                   {
                                                                        ScriptType = ScriptType.RunOnce
                                                                   })
                                        .WithTransaction()
                                        .LogToConsole()
                                        .Build();

            upgrader.PerformUpgrade();

            return 0;
        }
    }
}
