using DbUp;
using DbUp.Engine;
using DbUp.Support;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;

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

            // Try to ensure database is created for 1 min (sometimes database service is unavailable because it's still starting up)
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    EnsureDatabase.For.PostgresqlDatabase(connectionString);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }

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
