using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace EmoteRouter
{
    public static class EmoteHandler
    {
        public async static Task ReloadEmojiAll()
        {
            Dictionary<string, DiscordEmoji> patch = new Dictionary<string, DiscordEmoji>();
            DiscordGuild guild;
            await Console.Out.WriteLineAsync($"Reloading emojis...");
            foreach(ulong id in Program.Settings.ListenedGuilds)
            {
                guild = await Program.Client.GetGuildAsync(id);
                foreach(DiscordEmoji e in guild.Emojis.Values)
                {
                    if(patch.ContainsKey(e.Name)) continue;

                    patch.Add(e.Name, e);
                }
            }

            lock(Program.Settings.EmojiData)
            {
                Program.Settings.EmojiData = new ConcurrentDictionary<string, DiscordEmoji>(patch);
            }

            await Program.RequestConfigUpdate();
            await Console.Out.WriteLineAsync("Reload Complete!");
        }
    }
}