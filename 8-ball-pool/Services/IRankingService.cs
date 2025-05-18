using _8_ball_pool.Models;

namespace _8_ball_pool.Services
{
    public interface IRankingService
    {
        Task UpdatePlayerRankingsAsync(int? matchId = null);
        Task UpdateRankingForMatchAsync(int matchId);
        Task<IEnumerable<Player>> GetPlayerRankingsAsync();
    }
} 