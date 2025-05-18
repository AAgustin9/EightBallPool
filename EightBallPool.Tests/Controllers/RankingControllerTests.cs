using System.Collections.Generic;
using System.Threading.Tasks;
using _8_ball_pool.Controllers;
using _8_ball_pool.DTOs.Player;
using _8_ball_pool.Models;
using _8_ball_pool.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EightBallPool.Tests.Controllers
{
    public class RankingControllerTests
    {
        private readonly Mock<IRankingService> _mockRankingService;
        private readonly RankingController _controller;

        public RankingControllerTests()
        {
            _mockRankingService = new Mock<IRankingService>();
            _controller = new RankingController(_mockRankingService.Object);
        }

        [Fact]
        public async Task GetRankings_ReturnsOkWithRankedPlayers()
        {
            // Arrange
            var players = new List<Player>
            {
                new Player { 
                    Id = 1, 
                    Name = "Highly Ranked Player", 
                    Ranking = 100,
                    Wins = 10,
                    Losses = 2,
                    ProfilePictureUrl = "url1" 
                },
                new Player { 
                    Id = 2, 
                    Name = "Medium Ranked Player", 
                    Ranking = 50,
                    Wins = 5,
                    Losses = 5,
                    ProfilePictureUrl = "url2" 
                },
                new Player { 
                    Id = 3, 
                    Name = "Low Ranked Player", 
                    Ranking = 10,
                    Wins = 1,
                    Losses = 9,
                    ProfilePictureUrl = "url3" 
                }
            };

            _mockRankingService.Setup(s => s.GetPlayerRankingsAsync())
                .ReturnsAsync(players);

            // Act
            var result = await _controller.GetRankings();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedPlayers = okResult.Value.Should().BeAssignableTo<IEnumerable<PlayerDto>>().Subject;

            // Verify the returned DTOs match the source players
            var playerList = new List<PlayerDto>(returnedPlayers);
            playerList.Should().HaveCount(3);
            
            playerList[0].Id.Should().Be(1);
            playerList[0].Name.Should().Be("Highly Ranked Player");
            playerList[0].Ranking.Should().Be(100);
            playerList[0].Wins.Should().Be(10);
            playerList[0].Losses.Should().Be(2);
            
            playerList[1].Id.Should().Be(2);
            playerList[1].Ranking.Should().Be(50);
            
            playerList[2].Id.Should().Be(3);
            playerList[2].Ranking.Should().Be(10);
        }

        [Fact]
        public async Task RecalculateRankings_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            _mockRankingService.Setup(s => s.UpdatePlayerRankingsAsync(It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RecalculateRankings();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var message = okResult.Value.Should().BeAssignableTo<object>().Subject;
            message.Should().NotBeNull();
        }
    }
} 