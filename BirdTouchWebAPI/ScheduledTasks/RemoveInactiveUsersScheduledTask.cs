using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BirdTouchWebAPI.ScheduledTasks
{
    public class RemoveInactiveUsersScheduledTask : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private IConfiguration _configuration;

        public RemoveInactiveUsersScheduledTask(ILogger<RemoveInactiveUsersScheduledTask> logger,
                                                IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RemoveInactiveUsersScheduledTask is starting.");
            _logger.LogInformation($"Removing user that have been inactive for " +
                                   $"{_configuration["RemoveInactiveUsersRemoveUsersOlderThan"]} hours.");
            _logger.LogInformation($"Task will run every {_configuration["RemoveInactiveUsersRunEvery"]} minutes.");

            _logger.LogWarning("test1");
            _logger.LogCritical("test1");
            _logger.LogDebug("test1");
            _logger.LogError("test1");
            _logger.LogTrace("test1");

            _timer = new Timer(RemoveInactiveUsers,
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        private void RemoveInactiveUsers(object state)
        {
            _logger.LogInformation("RemoveInactiveUsersScheduledTask is working.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RemoveInactiveUsersScheduledTask is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
    }
}