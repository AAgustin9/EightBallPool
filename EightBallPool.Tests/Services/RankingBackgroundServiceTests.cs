using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _8_ball_pool.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EightBallPool.Tests.Services
{
    public class RankingBackgroundServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_CallsRankingServicePeriodically()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockServiceScope = new Mock<IServiceScope>();
            var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            var mockRankingService = new Mock<IRankingService>();
            var mockLogger = new Mock<ILogger<RankingBackgroundService>>();
            
            // Set up configuration for shorter interval (5ms for quick test)
            var configValues = new Dictionary<string, string?>
            {
                {"RankingUpdateIntervalHours", "0.001"}
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
            
            // Configure service scope
            mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
            mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(mockServiceScopeFactory.Object);
            mockServiceProvider.Setup(x => x.GetService(typeof(IRankingService))).Returns(mockRankingService.Object);
            
            // Configure service scope provider to return our mock ranking service
            var scopeServiceProvider = new Mock<IServiceProvider>();
            scopeServiceProvider.Setup(x => x.GetService(typeof(IRankingService))).Returns(mockRankingService.Object);
            mockServiceScope.Setup(x => x.ServiceProvider).Returns(scopeServiceProvider.Object);
            
            // Set up ranking service mock to return completed task
            mockRankingService.Setup(s => s.UpdatePlayerRankingsAsync(null))
                .Returns(Task.CompletedTask);
            
            // Create the service with our mocks
            var service = new RankingBackgroundService(
                mockServiceProvider.Object,
                mockLogger.Object,
                configuration);
            
            // Use a cancellation token that will cancel after a short period
            var cts = new CancellationTokenSource();
            
            // Act
            // Start the background task
            var task = Task.Run(async () => {
                await service.StartAsync(cts.Token);
                
                // Wait just enough time for at least one update
                await Task.Delay(50);
                
                // Cancel the operation
                cts.Cancel();
                
                // Wait for graceful shutdown
                await service.StopAsync(CancellationToken.None);
            });
            
            // Allow the task to complete
            await task;
            
            // Assert
            // Verify that the ranking service was called at least once
            mockRankingService.Verify(
                s => s.UpdatePlayerRankingsAsync(It.IsAny<int?>()), 
                Times.AtLeastOnce);
        }
    }
} 