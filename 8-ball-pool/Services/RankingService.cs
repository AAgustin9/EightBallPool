using _8_ball_pool.Data;
using _8_ball_pool.Models;
using Microsoft.EntityFrameworkCore;

namespace _8_ball_pool.Services
{
    public class RankingService : IRankingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RankingService> _logger;

        public RankingService(AppDbContext context, ILogger<RankingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdatePlayerRankingsAsync(int? matchId = null)
        {
            try
            {
                if (matchId.HasValue)
                {
                    await UpdateRankingForMatchAsync(matchId.Value);
                }
                else
                {
                    // For full recalculation of all rankings (could be used periodically)
                    var players = await _context.Players.ToListAsync();
                    
                    foreach (var player in players)
                    {
                        // Reset statistics
                        player.Wins = 0;
                        player.Losses = 0;
                        player.Ranking = 0;
                    }

                    // Get all completed matches
                    var matches = await _context.Matches
                        .Where(m => m.WinnerId.HasValue)
                        .OrderBy(m => m.EndTime)
                        .ToListAsync();

                    // Update stats for each match
                    foreach (var match in matches)
                    {
                        if (match.WinnerId.HasValue)
                        {
                            await UpdateStatsForMatchAsync(match);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player rankings");
                throw;
            }
        }

        public async Task UpdateRankingForMatchAsync(int matchId)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId && m.WinnerId.HasValue);

            if (match == null)
            {
                _logger.LogWarning("No completed match found with ID {MatchId} for ranking update", matchId);
                return;
            }

            await UpdateStatsForMatchAsync(match);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateStatsForMatchAsync(Match match)
        {
            if (!match.WinnerId.HasValue) return;

            var winner = await _context.Players.FindAsync(match.WinnerId.Value);
            if (winner == null) return;

            // Update winner stats
            winner.Wins++;
            winner.Ranking += 1; // Increment ranking by 1 for each win

            // Update loser stats
            var loserId = match.Player1Id == match.WinnerId.Value ? match.Player2Id : match.Player1Id;
            var loser = await _context.Players.FindAsync(loserId);
            
            if (loser != null)
            {
                loser.Losses++;
                // You could also implement ranking decreases for losses if desired
                // loser.Ranking = Math.Max(0, loser.Ranking - 1); // Ensure ranking doesn't go below 0
            }
        }

        public async Task<IEnumerable<Player>> GetPlayerRankingsAsync()
        {
            return await _context.Players
                .OrderByDescending(p => p.Ranking)
                .ToListAsync();
        }
    }
} 