using System;
using System.Linq;
using System.Threading.Tasks;
using _8_ball_pool.Data;
using _8_ball_pool.Models;
using _8_ball_pool.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using PoolMatch = _8_ball_pool.Models.Match;

namespace EightBallPool.Tests.Services
{
    public class RankingServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RankingService _service;
        private readonly Player _player1;
        private readonly Player _player2;
        private readonly Player _player3;
        private readonly Mock<ILogger<RankingService>> _mockLogger;

        public RankingServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<RankingService>>();
            _service = new RankingService(_context, _mockLogger.Object);

            // Seed test data
            _player1 = new Player { 
                Id = 1, 
                Name = "Player 1", 
                ProfilePictureUrl = "url1",
                Ranking = 0,
                Wins = 0,
                Losses = 0
            };
            
            _player2 = new Player { 
                Id = 2, 
                Name = "Player 2", 
                ProfilePictureUrl = "url2",
                Ranking = 0,
                Wins = 0,
                Losses = 0
            };
            
            _player3 = new Player { 
                Id = 3, 
                Name = "Player 3", 
                ProfilePictureUrl = "url3",
                Ranking = 0,
                Wins = 0,
                Losses = 0
            };
            
            _context.Players.AddRange(_player1, _player2, _player3);
            _context.SaveChanges();
        }

        [Fact]
        public async Task UpdateRankingForMatchAsync_IncrementsWinnerRanking()
        {
            // Arrange
            var match = new PoolMatch
            {
                Player1Id = _player1.Id,
                Player2Id = _player2.Id,
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow,
                WinnerId = _player1.Id
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdateRankingForMatchAsync(match.Id);

            // Assert
            var winner = await _context.Players.FindAsync(_player1.Id);
            var loser = await _context.Players.FindAsync(_player2.Id);

            winner!.Wins.Should().Be(1);
            winner!.Losses.Should().Be(0);
            winner!.Ranking.Should().Be(1);

            loser!.Wins.Should().Be(0);
            loser!.Losses.Should().Be(1);
            loser!.Ranking.Should().Be(0);
        }

        [Fact]
        public async Task UpdatePlayerRankingsAsync_RecalculatesAllRankings()
        {
            // Arrange
            // First match: Player1 wins
            var match1 = new PoolMatch
            {
                Player1Id = _player1.Id,
                Player2Id = _player2.Id,
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-2).AddHours(1),
                WinnerId = _player1.Id
            };

            // Second match: Player3 wins
            var match2 = new PoolMatch
            {
                Player1Id = _player2.Id,
                Player2Id = _player3.Id,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1).AddHours(1),
                WinnerId = _player3.Id
            };

            // Third match: Player1 wins again
            var match3 = new PoolMatch
            {
                Player1Id = _player1.Id,
                Player2Id = _player3.Id,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                WinnerId = _player1.Id
            };

            _context.Matches.AddRange(match1, match2, match3);
            await _context.SaveChangesAsync();

            // Manually set some stats that should be reset
            _player1.Wins = 10;
            _player1.Losses = 5;
            _player1.Ranking = 15;
            await _context.SaveChangesAsync();

            // Act
            await _service.UpdatePlayerRankingsAsync();

            // Assert
            var player1 = await _context.Players.FindAsync(_player1.Id);
            var player2 = await _context.Players.FindAsync(_player2.Id);
            var player3 = await _context.Players.FindAsync(_player3.Id);

            // Player1 wins twice
            player1!.Wins.Should().Be(2);
            player1!.Losses.Should().Be(0);
            player1!.Ranking.Should().Be(2);

            // Player2 loses twice
            player2!.Wins.Should().Be(0);
            player2!.Losses.Should().Be(2);
            player2!.Ranking.Should().Be(0);

            // Player3 wins once, loses once
            player3!.Wins.Should().Be(1);
            player3!.Losses.Should().Be(1);
            player3!.Ranking.Should().Be(1);
        }

        [Fact]
        public async Task GetPlayerRankingsAsync_ReturnsPlayersOrderedByRanking()
        {
            // Arrange
            _player1.Ranking = 100;
            _player2.Ranking = 50;
            _player3.Ranking = 75;
            await _context.SaveChangesAsync();

            // Act
            var rankings = await _service.GetPlayerRankingsAsync();

            // Assert
            var players = rankings.ToList();
            players.Should().HaveCount(3);
            players[0].Id.Should().Be(_player1.Id); // Highest ranking
            players[1].Id.Should().Be(_player3.Id); // Second highest
            players[2].Id.Should().Be(_player2.Id); // Lowest ranking
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 