using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmoteRouter
{
    public class CommandHandler : BaseCommandModule
    {
        [Command("listen"), RequireOwner]
        public async Task Listen(CommandContext context)
        {
            if(!Program.Settings.ListenedGuilds.Contains(context.Guild.Id))
            {
                Program.Settings.ListenedGuilds.Add(context.Guild.Id);
                await EmoteHandler.ReloadEmojiAll();
            }
            await Task.Yield();
        }

        [Command("emote")]
        public async Task RespondEmote(CommandContext context, string emojiName)
        {
            if(Program.Settings.EmojiData.TryGetValue(emojiName, out DiscordEmoji emoji))
            {
                await context.RespondAsync(emoji.ToString());
            }
            await Task.Yield();
        }
    }
}