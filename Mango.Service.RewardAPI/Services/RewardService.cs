using Mango.Service.RewardAPI.Models;
using Mango.Services.RewardAPI.Data;
using Mango.Services.RewardAPI.Message;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Mango.Services.RewardAPI.Services
{
    public class RewardService : IRewardService
    {
        public DbContextOptions<AppDbContext> _dbOptions;
        public RewardService(DbContextOptions<AppDbContext> dbOptions)
        {
            _dbOptions = dbOptions;
        }
        public async Task UpdateRewards(RewardsMessage rewardsMessage)
        {
            try
            {
                Rewards rewards = new Rewards()
                {
                   OrderId = rewardsMessage.OrderId,
                   RewardsActivity = rewardsMessage.RewardsActivity,
                   UserId = rewardsMessage.UserId,
                   RewardsDate = DateTime.Now,  
                };

                await using var _db = new AppDbContext(_dbOptions);

                await _db.Rewards.AddAsync(rewards);
                await _db.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
