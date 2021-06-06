using Microsoft.AspNetCore.Http;
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
        private int _removeUsersOlderThan;
        private int _removalPeriod;

        public RemoveInactiveUsersScheduledTask(ILogger<RemoveInactiveUsersScheduledTask> logger,
                                                IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _removeUsersOlderThan = int.Parse(_configuration["RemoveInactiveUsersRemoveUsersOlderThan"]);
            _removalPeriod = int.Parse(_configuration["RemoveInactiveUsersRunEvery"]);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"RemoveInactiveUsersScheduledTask is starting. {DateTime.UtcNow}");
            _logger.LogInformation($"Removing user that have been inactive for " +
                                   $"{_removeUsersOlderThan} hours.");
            _logger.LogInformation($"Task will run every {_removalPeriod} minutes.");

            _timer = new Timer(RemoveInactiveUsers,
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        private void RemoveInactiveUsers(object state)
        {
            _logger.LogInformation($"RemoveInactiveUsersScheduledTask is working. {DateTime.UtcNow}");

            //var applicationDbContext = (ApplicationDbContext)_httpContextAccessor
            //                                                    .HttpContext
            //                                                    .RequestServices
            //                                                    .GetService(typeof(ApplicationDbContext));
            //var listForRemoval = applicationDbContext
            //                     .ActiveUsers
            //                     .Where(u => (DateTime.Now - u.DatetimeLastUpdate).Hours > _removeUsersOlderThan)
            //                     .ToList();

            //applicationDbContext.RemoveRange(listForRemoval);

            //applicationDbContext.SaveChanges();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"RemoveInactiveUsersScheduledTask is stopping. {DateTime.UtcNow}");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
    }
}