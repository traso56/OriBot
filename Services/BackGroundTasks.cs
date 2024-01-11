using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OriBot.Utility;

namespace OriBot.Services;

public class BackgroundTasks : DiscordClientService
{
    private readonly IOptionsMonitor<CooldownOptions> _cooldownOptions;
    private readonly IDbContextFactory<SpiritContext> _dbContextFactory;
    private readonly ExceptionReporter _exceptionReporter;
    private readonly VolatileData _volatileData;
    private readonly Globals _globals;

    private string[] _statuses = null!;
    private int _currentStatusIndex = 0;

    public BackgroundTasks(DiscordSocketClient client, ILogger<DiscordClientService> logger, IOptionsMonitor<CooldownOptions> cooldownOptions,
        IDbContextFactory<SpiritContext> dbContextFactory, ExceptionReporter exceptionReporter, VolatileData volatileData, Globals globals)
        : base(client, logger)
    {
        _cooldownOptions = cooldownOptions;
        _dbContextFactory = dbContextFactory;
        _exceptionReporter = exceptionReporter;
        _volatileData = volatileData;
        _globals = globals;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken);

        Logger.LogInformation("Background Tasks Starting");

        await StartupTask();
        Task rareTask = RareTask(stoppingToken);
        Task frequentTask = FrequentTask(stoppingToken);

        await Task.WhenAll(rareTask, frequentTask);

        Logger.LogInformation($"Background tasks stopping");
    }
    private Task StartupTask()
    {
        Logger.LogInformation("Startup task begin work");

        using var db = _dbContextFactory.CreateDbContext();

        // store the channels with active tickets if any
        var dbTickets = db.Tickets.ToArray();
        foreach (var dbTicket in dbTickets)
            _volatileData.TicketThreads.TryAdd(dbTicket.TicketId, dbTicket.TicketUserId);

        // load status messages
        _statuses = File.ReadAllLines(Utilities.GetLocalFilePath("BotStatuses.txt"));

        Logger.LogInformation("Startup task ended work");
        return Task.CompletedTask;
    }
    private async Task RareTask(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Logger.LogInformation("Rare task begin work");
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                // check tickets for timeout
                var dbTickets = db.Tickets.ToArray();
                foreach (var dbTicket in dbTickets)
                {
                    IThreadChannel? thread = await Client.GetChannelAsync(dbTicket.TicketId) as IThreadChannel;

                    bool messageTooOld = false;
                    if (thread is not null)
                    {
                        var message = (await thread.GetMessagesAsync(1).FlattenAsync()).First();
                        if (message.CreatedAt.AddDays(2) < DateTimeOffset.Now)
                        {
                            messageTooOld = true;
                            await thread.ModifyAsync(t => t.Locked = true);
                        }
                    }

                    if (thread is null || messageTooOld)
                    {
                        _volatileData.TicketThreads.TryRemove(dbTicket.TicketId, out _);
                        db.Tickets.Remove(dbTicket);
                    }
                }

                // check bans
                var expiredBans = db.Punishments.Where(
                    p => p.CheckForExpiry &&
                    p.Type == PunishmentType.Ban &&
                    p.Expiry < DateTime.Now).ToArray();
                foreach (var expiredBan in expiredBans)
                {
                    try
                    {
                        await _globals.MainGuild.RemoveBanAsync(expiredBan.PunishedId);
                    }
                    catch (Discord.Net.HttpException e)
                    {
                        if (e.DiscordCode != Discord.DiscordErrorCode.UnknownBan)
                            throw;
                    }
                    expiredBan.CheckForExpiry = false;
                }

                // check for image roles
                var imageRolesToGive = db.PendingImageRoles.Where(u => u.ImageRoleDateTime > DateTime.Now).ToArray();
                foreach (var imageRoleToGive in imageRolesToGive)
                {
                    IGuildUser? user = await _globals.MainGuild.GetUserAsync(imageRoleToGive.UserId);

                    if (user is not null)
                        await user.AddRoleAsync(_globals.ImagesRole);

                    db.Remove(imageRoleToGive);
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                var exceptionContext = new ExceptionContext();
                await _exceptionReporter.NotifyExceptionAsync(e, exceptionContext, "Exception while executing the rare task", false);
            }
            Logger.LogInformation("Rare task ended work, waiting for next run");

            try
            {
                await Task.Delay(new TimeSpan(_cooldownOptions.CurrentValue.RareTaskIntervalHours, 0, 0), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // log task cancelled here if needed
            }
        }

        Logger.LogInformation($"Rare task stopping");
    }
    private async Task FrequentTask(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Logger.LogInformation("Frequent task begin work");
            try
            {
                // change status
                await Client.SetCustomStatusAsync(_statuses[_currentStatusIndex++]);
                if (_currentStatusIndex >= _statuses.Length)
                    _currentStatusIndex = 0;
            }
            catch (Exception e)
            {
                var exceptionContext = new ExceptionContext();
                await _exceptionReporter.NotifyExceptionAsync(e, exceptionContext, "Exception while executing the frequent task", false);
            }
            Logger.LogInformation("Frequent task ended work, waiting for next run");

            try
            {
                await Task.Delay(new TimeSpan(_cooldownOptions.CurrentValue.FrequentTaskIntervalHours, 0, 0), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // log task cancelled here if needed
            }
        }

        Logger.LogInformation($"Frequent task stopping");
    }
}
