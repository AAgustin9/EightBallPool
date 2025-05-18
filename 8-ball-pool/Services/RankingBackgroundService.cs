using _8_ball_pool.Services;

namespace _8_ball_pool.Services
{
    public class RankingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<RankingBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // Update daily by default

        public RankingBackgroundService(
            IServiceProvider services,
            ILogger<RankingBackgroundService> logger,
            IConfiguration configuration)
        {
            _services = services;
            _logger = logger;
            
            // Optional: Load update interval from configuration
            if (int.TryParse(configuration["RankingUpdateIntervalHours"], out int hours) && hours > 0)
            {
                _updateInterval = TimeSpan.FromHours(hours);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ranking Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running ranking recalculation...");
                    
                    using (var scope = _services.CreateScope())
                    {
                        var rankingService = scope.ServiceProvider.GetRequiredService<IRankingService>();
                        await rankingService.UpdatePlayerRankingsAsync();
                    }
                    
                    _logger.LogInformation("Ranking recalculation completed. Next update in {Interval}", _updateInterval);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during ranking update");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
        }
    }
} 