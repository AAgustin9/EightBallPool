using Microsoft.AspNetCore.Mvc;
using _8_ball_pool.Services;
using _8_ball_pool.DTOs.Player;

namespace _8_ball_pool.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;

        public RankingController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> GetRankings()
        {
            var rankedPlayers = await _rankingService.GetPlayerRankingsAsync();
            
            var playerDtos = rankedPlayers.Select(p => new PlayerDto
            {
                Id = p.Id,
                Name = p.Name,
                Ranking = p.Ranking,
                Wins = p.Wins,
                Losses = p.Losses,
                PreferredCue = p.PreferredCue,
                ProfilePictureUrl = p.ProfilePictureUrl
            });
            
            return Ok(playerDtos);
        }

        [HttpPost("recalculate")]
        public async Task<ActionResult> RecalculateRankings()
        {
            await _rankingService.UpdatePlayerRankingsAsync();
            return Ok(new { message = "Rankings recalculated successfully" });
        }
    }
} 