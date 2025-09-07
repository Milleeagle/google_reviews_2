namespace google_reviews.Services
{
    public class ScheduledMonitorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledMonitorBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public ScheduledMonitorBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<ScheduledMonitorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Monitor Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var scheduledMonitorService = scope.ServiceProvider.GetRequiredService<IScheduledMonitorService>();
                    
                    await scheduledMonitorService.ProcessScheduledMonitorsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled monitors");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }

            _logger.LogInformation("Scheduled Monitor Background Service stopped");
        }
    }
}