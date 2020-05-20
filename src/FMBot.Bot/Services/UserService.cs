using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using FMBot.Persistence.Domain.Models;
using FMBot.Persistence.EntityFrameWork;
using Microsoft.EntityFrameworkCore;

namespace FMBot.Bot.Services
{
    public class UserService
    {
        // User settings
        public async Task<User> GetUserSettingsAsync(IUser discordUser)
        {
            using var db = new FMBotDbContext();
            return await db.Users.FirstOrDefaultAsync(f => f.DiscordUserId == discordUser.Id);
        }

        // User settings
        public async Task<User> GetFullUserAsync(IUser discordUser)
        {
            using var db = new FMBotDbContext();
            return await db.Users
                .Include(i => i.Artists)
                .Include(i => i.Friends)
                .Include(i => i.FriendedByUsers)
                .FirstOrDefaultAsync(f => f.DiscordUserId == discordUser.Id);
        }

        // Discord nickname/username
        public async Task<string> GetNameAsync(ICommandContext context)
        {
            if (context.Guild == null)
            {
                return context.User.Username;
            }

            var guildUser = await context.Guild.GetUserAsync(context.User.Id);

            return guildUser.Nickname ?? context.User.Username;
        }

        // Rank
        public async Task<UserType> GetRankAsync(IUser discordUser)
        {
            using var db = new FMBotDbContext();
            var user = await db.Users.FirstOrDefaultAsync(f => f.DiscordUserId == discordUser.Id);

            if (user == null)
            {
                return UserType.User;
            }

            return user.UserType;
        }

        // Featured
        public async Task<bool?> GetFeaturedAsync(IUser discordUser)
        {
            using var db = new FMBotDbContext();
            var user = await db.Users.FirstOrDefaultAsync(f => f.DiscordUserId == discordUser.Id);

            if (user == null)
            {
                return false;
            }

            return user.Featured;
        }

        // Featured
        public async Task<User> GetFeaturedUserAsync()
        {
            using var db = new FMBotDbContext();
            return await db.Users.FirstOrDefaultAsync(f => f.Featured == true);
        }

        // Random user
        public async Task<string> GetRandomLastFMUserAsync()
        {
            using var db = new FMBotDbContext();
            var featuredUser = await db.Users.FirstOrDefaultAsync(f => f.Featured == true);
            if (featuredUser != null)
            {
                featuredUser.Featured = false;

                db.Entry(featuredUser).State = EntityState.Modified;
            }

            var users = db.Users.Where(w => w.Blacklisted != true).ToList();

            var rand = new Random();
            var user = users[rand.Next(users.Count)];

            user.Featured = true;

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            return user.UserNameLastFM;
        }


        // Server Blacklisting
        public async Task<bool> GetBlacklistedAsync(IUser discordUser)
        {
            using var db = new FMBotDbContext();
            var user = await db.Users.FirstOrDefaultAsync(f => f.DiscordUserId == discordUser.Id);

            if (user == null)
            {
                return false;
            }

            return user.Blacklisted ?? false;
        }

        // UserTitle
        public async Task<string> GetUserTitleAsync(ICommandContext context)
        {
            var name = await GetNameAsync(context);
            var rank = await GetRankAsync(context.User);
            var featured = await GetFeaturedAsync(context.User);

            var title = name;

            if (featured == true)
            {
                title = name + " - Featured";
            }

            if (rank == UserType.Owner)
            {
                title += " 👑";
            }

            if (rank == UserType.Admin)
            {
                title += " 🛡️";
            }

            if (rank == UserType.Contributor)
            {
                title += " 🔥";
            }

            return title;
        }

        // Set LastFM Name
        public void SetLastFM(IUser discordUser, User userSettings)
        {
            using var db = new FMBotDbContext();
            var user = db.Users.FirstOrDefault(f => f.DiscordUserId == discordUser.Id);

            if (user == null)
            {
                var newUser = new User
                {
                    DiscordUserId = discordUser.Id,
                    UserType = UserType.User,
                    UserNameLastFM = userSettings.UserNameLastFM,
                    TitlesEnabled = true,
                    ChartTimePeriod = ChartTimePeriod.Monthly,
                    FmEmbedType = userSettings.FmEmbedType,
                    FmCountType = userSettings.FmCountType
                };

                db.Users.Add(newUser);

                db.SaveChanges();
            }
            else
            {
                user.UserNameLastFM = userSettings.UserNameLastFM;
                user.FmEmbedType = userSettings.FmEmbedType;
                user.FmCountType = userSettings.FmCountType;

                db.Entry(user).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public User SetSettings(User userSettings, string[] extraOptions)
        {

            if (extraOptions.Contains("embedfull") || extraOptions.Contains("ef"))
            {
                userSettings.FmEmbedType = FmEmbedType.embedfull;
            }
            else if (extraOptions.Contains("textmini") || extraOptions.Contains("tm"))
            {
                userSettings.FmEmbedType = FmEmbedType.textmini;
            }
            else if (extraOptions.Contains("textfull") || extraOptions.Contains("tf"))
            {
                userSettings.FmEmbedType = FmEmbedType.textfull;
            }
            else
            {
                userSettings.FmEmbedType = FmEmbedType.embedmini;
            }


            if (extraOptions.Contains("artist"))
            {
                userSettings.FmCountType = FmCountType.Artist;
            }
            else if (extraOptions.Contains("album"))
            {
                userSettings.FmCountType = FmCountType.Album;
            }
            else if (extraOptions.Contains("track"))
            {
                userSettings.FmCountType = FmCountType.Track;
            }
            else
            {
                userSettings.FmCountType = null;
            }

            return userSettings;
        }


        public async Task ResetChartTimerAsync(User user)
        {
            using var db = new FMBotDbContext();
            user.LastGeneratedChartDateTimeUtc = DateTime.Now;

            db.Entry(user).State = EntityState.Modified;

            await db.SaveChangesAsync();
        }

        // Remove user
        public async Task DeleteUser(int userID)
        {
            using var db = new FMBotDbContext();
            var user = await db.Users.FirstOrDefaultAsync(f => f.UserId == userID);

            db.Users.Remove(user);

            await db.SaveChangesAsync();
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            using var db = new FMBotDbContext();
            return await db.Users.CountAsync();
        }
    }
}
