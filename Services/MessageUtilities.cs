using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Options;
using OriBot.Utility;

namespace OriBot.Services;

public class MessageUtilities
{
    private readonly DiscordSocketClient _client;
    private readonly IOptionsMonitor<ComponentNegativeResponsesOptions> _componentNegativeResponsesOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public MessageUtilities(DiscordSocketClient client, IOptionsMonitor<ComponentNegativeResponsesOptions> componentNegativeResponsesOptions,
        IHttpClientFactory httpClientFactory)
    {
        _client = client;
        _componentNegativeResponsesOptions = componentNegativeResponsesOptions;
        _httpClientFactory = httpClientFactory;
    }

    public enum ComponentType
    {
        Button, SelectMenu
    }
    public async Task<IComponentInteraction?> AwaitComponentAsync(ulong messageID, ulong? userId, ComponentType type, int delayInSeconds = 15)
    {
        SocketMessageComponent? response = null;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        if (type == ComponentType.Button)
            _client.ButtonExecuted += OnComponentReceived;
        else if (type == ComponentType.SelectMenu)
            _client.SelectMenuExecuted += OnComponentReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            if (type == ComponentType.Button)
                _client.ButtonExecuted -= OnComponentReceived;
            else if (type == ComponentType.SelectMenu)
                _client.SelectMenuExecuted -= OnComponentReceived;
            canceler.Dispose();
        }

        return response;

        async Task OnComponentReceived(SocketMessageComponent component)
        {
            if (component.Message.Id == messageID)
            {
                if (userId is null || component.User.Id == userId)
                {
                    response = component;
                    await component.DeferAsync();
                    canceler.Cancel();
                }
                else
                {
                    await component.RespondAsync(_componentNegativeResponsesOptions.CurrentValue.GetRandomResponse(), ephemeral: true);
                }
            }
        }
    }
    public async Task AwaitComponentMultipleAsync(ulong messageID, Dictionary<ulong, string?> choices, ComponentType type, int delayInSeconds = 15)
    {
        int usersLeftToRespond = choices.Count(u => u.Value is null);

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        if (type == ComponentType.Button)
            _client.ButtonExecuted += OnComponentReceived;
        else if (type == ComponentType.SelectMenu)
            _client.SelectMenuExecuted += OnComponentReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            if (type == ComponentType.Button)
                _client.ButtonExecuted -= OnComponentReceived;
            else if (type == ComponentType.SelectMenu)
                _client.SelectMenuExecuted -= OnComponentReceived;
            canceler.Dispose();
        }

        async Task OnComponentReceived(SocketMessageComponent component)
        {
            if (component.Message.Id == messageID)
            {
                if (choices.ContainsKey(component.User.Id) && choices[component.User.Id] is null)
                {
                    choices[component.User.Id] = component.Data.CustomId;

                    await component.DeferAsync();
                    if (--usersLeftToRespond == 0)
                        canceler.Cancel();
                }
                else
                {
                    await component.RespondAsync(_componentNegativeResponsesOptions.CurrentValue.GetRandomResponse(), ephemeral: true);
                }
            }
        }
    }
    public async Task<IMessage?> AwaitMessageAsync(ulong channelID, ulong? userId, int delayInSeconds = 15)
    {
        SocketMessage? response = null;

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        _client.MessageReceived += OnMessageReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
            canceler.Dispose();
        }

        return response;

        async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == channelID && (userId is null || message.Author.Id == userId))
            {
                response = message;
                canceler.Cancel();
                await Task.CompletedTask;
            }
        }
    }
    public async Task AwaitMessageMultipleAsync(ulong channelID, Dictionary<ulong, string?> responses, int delayInSeconds = 15)
    {
        int usersLeftToRespond = responses.Count(u => u.Value is null);

        CancellationTokenSource canceler = new CancellationTokenSource();
        Task waiter = Task.Delay(delayInSeconds * 1000, canceler.Token);

        _client.MessageReceived += OnMessageReceived;

        try
        { await waiter; }
        catch (TaskCanceledException)
        { /* task cancelled */ }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
            canceler.Dispose();
        }

        Task OnMessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == channelID && responses.ContainsKey(message.Author.Id) && responses[message.Author.Id] is null)
            {
                responses[message.Author.Id] = message.Content;

                if (--usersLeftToRespond == 0)
                    canceler.Cancel();
            }
            return Task.CompletedTask;
        }
    }
    public async Task<bool> TrySendDmAsync(IUser user, string message, EmbedBuilder? embedToHoldError)
    {
        try
        {
            IDMChannel userDm = await user.CreateDMChannelAsync();
            await userDm.SendMessageAsync(message);
            return true;
        }
        catch (HttpException)
        {
            embedToHoldError?.AddField("Info", "DM to user notifying could not be delivered, most likely the user does not accept DMs");
            return false;
        }
    }
    public async Task<bool> ActivityConfirmationAsync(IInteractionContext context, IGuildUser challenger, IGuildUser target, string nameOfActivity,
        bool botResponse = false)
    {
        if (challenger.Id == target.Id)
        {
            await context.Interaction.FollowupAsync("You can't make a game against yourself!");
            return false;
        }

        var buttonBuilder = new ComponentBuilder()
            .WithButton("Yes", "Yes", ButtonStyle.Success)
            .WithButton("No", "No", ButtonStyle.Danger);

        string questionText = $"{target.DisplayName}, {challenger.DisplayName} has challenged you to __{nameOfActivity}__\n";
        await context.Interaction.FollowupAsync(questionText + $"Do you accept?", components: buttonBuilder.Build());
        IUserMessage question = await context.Interaction.GetOriginalResponseAsync();

        bool targetIsBot = target.Id == _client.CurrentUser.Id;
        var componentResponse = await AwaitComponentAsync(question.Id, target.Id, ComponentType.Button, targetIsBot ? 1 : 15);

        bool? response = null;
        if (targetIsBot)
            response = botResponse;
        else if (componentResponse is not null)
            response = componentResponse.Data.CustomId == "Yes";

        // default value for null (no response), it gets changed if there is a response
        string responseStatusMessage = $"{target.DisplayName} didn't respond";
        if (response is not null)
        {
            responseStatusMessage = response == true ? $"{target.DisplayName} accepted" : $"{target.DisplayName} declined";
        }

        await question.ModifyAsync(m =>
        {
            m.Content = questionText + responseStatusMessage;
            m.Components = Utilities.DisableAllButtons(buttonBuilder).Build();
        });

        return response == true;
    }
    public enum ResponseType
    {
        Respond, FollowUp, Reply
    }
    public async Task<bool?> UserConfirmation(IInteractionContext context, IUser user, string message, ResponseType responseType)
    {
        var buttonBuilder = new ComponentBuilder()
            .WithButton("Yes", "Yes", ButtonStyle.Success)
            .WithButton("No", "No", ButtonStyle.Danger);

        IUserMessage question;
        switch (responseType)
        {
            case ResponseType.Respond:
                await context.Interaction.RespondAsync(message, components: buttonBuilder.Build());
                question = await context.Interaction.GetOriginalResponseAsync();
                break;
            case ResponseType.FollowUp:
                question = await context.Interaction.FollowupAsync(message, components: buttonBuilder.Build());
                break;
            case ResponseType.Reply:
                question = await context.Channel.SendMessageAsync(message, components: buttonBuilder.Build());
                break;
            default:
                throw new NotImplementedException("Response type is not in enum.");
        }
        var response = await AwaitComponentAsync(question.Id, user.Id, ComponentType.Button);

        if (responseType == ResponseType.Reply)
            await question.ModifyAsync(m => m.Components = Utilities.DisableAllButtons(buttonBuilder).Build());
        else
            await context.Interaction.ModifyOriginalResponseAsync(m => m.Components = Utilities.DisableAllButtons(buttonBuilder).Build());

        return response is null ? null : response.Data.CustomId == "Yes";
    }
    public async Task<bool?> UserConfirmation(ICommandContext context, IUser user, string message)
    {
        var buttonBuilder = new ComponentBuilder()
            .WithButton("Yes", "Yes", ButtonStyle.Success)
            .WithButton("No", "No", ButtonStyle.Danger);

        IUserMessage question = await context.Channel.SendMessageAsync(message, components: buttonBuilder.Build());
        var response = await AwaitComponentAsync(question.Id, user.Id, ComponentType.Button);
        await question.ModifyAsync(m => m.Components = Utilities.DisableAllButtons(buttonBuilder).Build());
        return response is null ? null : response.Data.CustomId == "Yes";
    }
    public async Task SendMessageAfterDelayAsync(IMessageChannel channel, string message, int delayInSeconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delayInSeconds * 1000, cancellationToken);
            await channel.SendMessageAsync(message);
        }
        catch (TaskCanceledException)
        { /* task cancelled */ }
    }
    public async Task<IUserMessage> SendMessageWithFiles(IMessageChannel channel, EmbedBuilder embedBuilder, IUserMessage message, ComponentBuilder? components = null)
    {
        if (message.Attachments.Count == 0)
        {
            return await channel.SendMessageAsync(embed: embedBuilder.Build());
        }
        else
        {
            int size = 0;
            foreach (var attachment in message.Attachments)
                size += attachment.Size;
            if (size <= 26_214_400) // 25 MB
            {
                List<FileAttachment> files = [];
                try
                {
                    HttpClient httpClient = _httpClientFactory.CreateClient();
                    foreach (var attachment in message.Attachments)
                    {
                        files.Add(new FileAttachment(await httpClient.GetStreamAsync(attachment.Url), attachment.Filename, attachment.Description));
                    }
                    return await channel.SendFilesAsync(files, embed: embedBuilder.Build(), components: components?.Build());
                }
                finally
                {
                    foreach (var file in files)
                        file.Dispose();
                }
            }
            else
            {
                string attachmentsString = "Files too large to upload. Their links are:\n" + Utilities.MessageAttachmentsToUrls(message);
                return await channel.SendMessageAsync(attachmentsString, embed: embedBuilder.Build(), components: components?.Build());
            }
        }
    }
}

