﻿using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OriBot;
using OriBot.Services;

namespace Discord.Interactions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ModCommandAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.User is SocketGuildUser user)
            {
                if (user.GuildPermissions.BanMembers)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("You can't use this command."));
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You can't use this command."));
            }
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CommandsChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var botOptions = services.GetRequiredService<IOptions<BotOptions>>();
            if (context.Channel.Id == botOptions.Value.CommandsChannelId || ((IGuildUser)context.User).GuildPermissions.BanMembers)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("Please use this command in the commands channel."));
        }
    }
}
namespace Discord.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ModCommandAttribute : PreconditionAttribute
    {
        public override string ErrorMessage { get; set; }

        public ModCommandAttribute(string? errorMessage = null) => ErrorMessage = errorMessage ?? "You can't use this command.";

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser user)
            {
                if (user.GuildPermissions.BanMembers)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError(ErrorMessage));
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError(ErrorMessage));
            }
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CommandsChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var botOptions = services.GetRequiredService<IOptions<BotOptions>>();
            if (context.Channel.Id == botOptions.Value.CommandsChannelId || ((IGuildUser)context.User).GuildPermissions.BanMembers)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("Please use this command in the commands channel."));
        }
    }
}