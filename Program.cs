using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OriBot.Services;
using OriBot.Utility;
using Serilog;

namespace OriBot;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables(prefix: "DOTNET_")
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "Files"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: false, reloadOnChange: true)
            .AddJsonFile("CooldownOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("ComponentNegativeResponsesOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("UserJoinOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("PinOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("PassiveResponsesOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("MessageAmountQuerying.json", optional: false, reloadOnChange: true)
            //.AddJsonFile("RuntimeCompilationOptions.json", optional: false, reloadOnChange: true)
            .AddJsonFile("GenerativeAIOptions.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .Filter.ByExcluding("StartsWith(@m, 'Rest:')")
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "Files", "SystemLogs", "OriLog.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 60)
            .CreateLogger();

        try
        {

            Log.Information("Program starting");

            using var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureDiscordHost((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        MessageCacheSize = 100,
                        AlwaysDownloadUsers = true,
                        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.GuildPresences,
                        AuditLogCacheSize = 20
                    };
                    config.Token = context.Configuration.GetSection("BotSettings").Get<BotOptions>()?.DiscordToken ?? throw new ArgumentException("Discord token is null");
                })
                .UseCommandService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = Discord.Commands.RunMode.Async;
                    config.CaseSensitiveCommands = true;
                })
                .UseInteractionService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = Discord.Interactions.RunMode.Async;
                    config.UseCompiledLambda = true;
                })
                .ConfigureServices(services =>
                {
                    services
                        // config
                        .Configure<BotOptions>(configuration.GetSection(BotOptions.BotSettings))
                        .Configure<CooldownOptions>(configuration)
                        .Configure<ComponentNegativeResponsesOptions>(configuration)
                        .Configure<UserJoinOptions>(configuration)
                        .Configure<PinOptions>(configuration)
                        .Configure<NewPassiveResponsesOptions>(configuration)
                        .Configure<MessageAmountQuerying>(configuration)
                        //.Configure<RuntimeCompileOptions>(configuration)
                        .Configure<GenerativeAIOptions>(configuration)
                        .AddDbContextFactory<SpiritContext>(options => options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, "Files", "database.db")}").EnableThreadSafetyChecks(true))
                        .AddHttpClient()
                        // managers
                        .AddHostedService<MessageHandler>()
                        .AddHostedService<InteractionHandler>()
                        .AddHostedService<BackgroundTasks>()
                        .AddHostedService<Services.EventHandler>()
                        // singletons
                        .AddSingleton<GenAI>()
                        .AddSingleton<Globals>()
                        .AddHostedService<HostedServiceStarter<Globals>>()
                        .AddSingleton<NewPassiveResponses>()
                        .AddHostedService<HostedServiceStarter<NewPassiveResponses>>()
                        .AddSingleton<ExceptionReporter>()
                        .AddSingleton<VolatileData>()
                        .AddSingleton<MessageUtilities>()
                        .AddSingleton<PaginatorFactory>()
                        //.AddSingleton<RuntimeCompilationService>()
                        ;

                })
                .UseSerilog()
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
            return 0;
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("Program ending");
            Log.CloseAndFlush();
        }
    }
}
