using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OriBot.Services
{
    public class MemberManipulationHandler : DiscordClientService
    {

        private readonly DiscordSocketClient _discordClient;
        private readonly ILogger _logger;
        private List<ulong> _guilds = new List<ulong>();
        private ulong GuildCount { get; set; }
        private static System.Timers.Timer wait;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Globals _globals;

        public MemberManipulationHandler(DiscordSocketClient Client, ILogger<DiscordClientService> logger, IHttpClientFactory httpClientFactory, Globals globals) : base(Client, logger)
        {
            _discordClient = Client;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _globals = globals;

            Client.GuildMemberUpdated += OnGuildMemberUpdated;
            Client.GuildMembersDownloaded += OnGuildMembersDownloaded;
            _ = AnnounceWatch();
        }

        private Task AnnounceWatch()
        {
            wait = new(2000)
            {
                AutoReset = false,
                Enabled = true
            };
            wait.Elapsed += AnnounceGuild;
            return Task.CompletedTask;
        }

        private void AnnounceGuild(object sender, ElapsedEventArgs e)
        {
            _logger.LogDebug($"Successfully retrieved members from {this.GuildCount} guilds");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        private Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            _logger.LogDebug("Downloaded guild members for guild: " + guild.Name);
            _guilds.Add(guild.Id);
            this.GuildCount = (ulong)_guilds.Count;
            wait.Interval = 2000;
            return Task.CompletedTask;
        }

        private async Task OnGuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            if (!_guilds.Contains(after.Guild.Id)) return;
            var cached = before.Value.GetGuildAvatarUrl(ImageFormat.Png);
            var embedbuilder2 = new EmbedBuilder()
            .WithAuthor(after)
            .WithTitle($"User {after.Mention} changed server profile info.")
            .WithDescription($"Event Time: <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>")
            .AddField($"Previous nickname: {before.Value.DisplayName}", $"New nickname: {after.DisplayName}")
            .AddField($"Previous guild avatar: {cached}", $"New guild avatar: {after.GetGuildAvatarUrl(ImageFormat.Png)}")
            .WithFooter($"Author ID: {after.Id}");


            if (cached != null && cached != after.GetGuildAvatarUrl())
            {
                var httpclient = _httpClientFactory.CreateClient();
                embedbuilder2.WithImageUrl($"attachment://{Path.GetFileName(cached)}");
                await _globals.LogChannel.SendFileAsync(await httpclient.GetStreamAsync(cached), Path.GetFileName(cached), embed: embedbuilder2.Build());
                return;

            }

            await _globals.LogChannel.SendMessageAsync(embed: embedbuilder2.Build());
        }
    }
}
