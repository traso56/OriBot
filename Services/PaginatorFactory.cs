using Discord;

namespace OriBot.Services;

public interface IEagerPaginator
{
    Task SendPaginatorAsync(IDiscordInteraction interaction, IUser? user, int timeoutInSeconds = 60);
}
public class PaginatorFactory
{
    private readonly MessageUtilities _messageUtilities;

    public PaginatorFactory(MessageUtilities messageUtilities)
    {
        _messageUtilities = messageUtilities;
    }

    public IEagerPaginator CreateEagerPaginator(IList<Embed> embeds)
    {
        return new EagerPaginator(_messageUtilities, embeds);
    }

    private sealed class EagerPaginator : IEagerPaginator
    {
        private readonly MessageUtilities _messageUtilities;
        private readonly IList<Embed> _embeds;

        public EagerPaginator(MessageUtilities messageUtilities, IList<Embed> embeds)
        {
            _messageUtilities = messageUtilities;
            _embeds = embeds;
        }

        public async Task SendPaginatorAsync(IDiscordInteraction interaction, IUser? user, int timeoutInSeconds = 60)
        {
            IUserMessage message;
            if (_embeds.Count == 1)
            {
                await interaction.FollowupAsync(embed: _embeds[0]);
                return;
            }
            else
            {
                ComponentBuilder buttonBuilder = new ComponentBuilder()
                    .WithButton("Previous", customId: "l", disabled: true)
                    .WithButton("Next", customId: "r", disabled: false);
                message = await interaction.FollowupAsync(embed: _embeds[0], components: buttonBuilder.Build());
            }

            int index = 0;
            while (true)
            {
                var selection = await _messageUtilities.AwaitComponentAsync(message.Id, user?.Id, MessageUtilities.ComponentType.Button, timeoutInSeconds);

                if (selection is null)
                {
                    ComponentBuilder disabledButtons = new ComponentBuilder()
                        .WithButton("Previous", customId: "l", disabled: true)
                        .WithButton("Next", customId: "r", disabled: true);

                    await message.ModifyAsync(m => m.Components = disabledButtons.Build());
                    return;
                }

                if (selection.Data.CustomId == "l")
                    index--;
                else
                    index++;

                ComponentBuilder buttonBuilder = new ComponentBuilder()
                    .WithButton("Previous", customId: "l", disabled: index <= 0)
                    .WithButton("Next", customId: "r", disabled: index >= _embeds.Count - 1);

                await message.ModifyAsync(m =>
                {
                    m.Embed = _embeds[index];
                    m.Components = buttonBuilder.Build();
                });
            }
        }
    }
}
