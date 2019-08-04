using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using Newtonsoft.Json;

namespace EmoteRouter
{
    public class Program
    {
        public static Config Settings;
        public static DiscordClient Client { get; set; }
        public CommandsNextExtension Commands { get; set; }
        private static object ConfigLock = new Object();
        public static void Main(string[] args) =>
            new Program().StartBot().GetAwaiter().GetResult();

        public async Task StartBot()
        {
            await RequestConfigReload();

            DiscordConfiguration UserConfig = new DiscordConfiguration
            {
                Token = Program.Settings.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            CommandsNextConfiguration CommandConfig = new CommandsNextConfiguration
            {
                StringPrefixes = Program.Settings.CommandPrefixes,
                EnableDefaultHelp = false,
                EnableMentionPrefix = false
            };

            Client = new DiscordClient(UserConfig);
            Client.Ready += OnReadyClient;
            Client.GuildAvailable += OnGuildAvaliable;
            Client.ClientErrored += OnErrorClient;
            Client.GuildEmojisUpdated += OnEmojiChanged;

            Commands = Client.UseCommandsNext(CommandConfig);

            Commands.RegisterCommands<CommandHandler>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        #region Config Task
        public static async Task CheckForConfig()
        {
            if(!File.Exists("Config.json"))
            {
                using(StreamWriter fileWriter = File.CreateText("Config.json"))
                {
                    string toWrite = JsonConvert.SerializeObject(new Config()
                    {
                        Token = "",
                        CommandPrefixes = new string[1]{"."},
                        Data = new Dictionary<string, DiscordEmoji>(),
                        ListenedGuilds = new ThreadSafeList<ulong>()
                    }, Formatting.Indented);

                    await fileWriter.WriteAsync(toWrite);
                }
            }
        }

        public static async Task RequestConfigReload()
        {
            await CheckForConfig();

            string JsonConfig = "";
            using (FileStream file = File.OpenRead("Config.json"))
            using (StreamReader reader = new StreamReader(file, new System.Text.UTF8Encoding(false)))
                JsonConfig = await reader.ReadToEndAsync();

            lock(ConfigLock)
            {
                Settings = JsonConvert.DeserializeObject<Config>(JsonConfig);
                Settings.EmojiData = new ConcurrentDictionary<string, DiscordEmoji>(Settings.Data);
            }
        }
        public static async Task RequestConfigUpdate()
        {
            await CheckForConfig();

            lock(ConfigLock)
            {
                Settings.Data = new Dictionary<string, DiscordEmoji>(Settings.EmojiData);
            }
 
            using (StreamWriter writer = new StreamWriter("Config.json", false, new System.Text.UTF8Encoding(false)))
                await writer.WriteAsync(JsonConvert.SerializeObject(Program.Settings, Formatting.Indented));
        }
        #endregion

        #region Client Tasks
        private async Task OnEmojiChanged(GuildEmojisUpdateEventArgs e)
        {
            if(Program.Settings.ListenedGuilds.Contains(e.Guild.Id))
            {
                await EmoteHandler.ReloadEmojiAll();
            }
            await Task.Yield();
        }

        private Task OnReadyClient(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "EmoteRouter", "Bot client is ready!", DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task OnGuildAvaliable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "EmoteRouter", $"Avaliable guild: {e.Guild.Name}", DateTime.Now);
            if(Program.Settings.ListenedGuilds.Contains(e.Guild.Id))
            {
                await EmoteHandler.ReloadEmojiAll();
            }
        }

        private Task OnErrorClient(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "EmoteRouter", $"Exception: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }
        #endregion

        #region Command Tasks
        private Task OnExecuteCommand(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "EmoteRouter", $"{e.Context.User.Username} executed '{e.Command.QualifiedName}'", DateTime.Now);
            return Task.CompletedTask;
        }
 
        private async Task OnErrorCommand(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "EmoteRouter", $"{e.Context.User.Username} failed executing '{e.Command?.QualifiedName ?? "<unknown command>"}' - error: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);
 
            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
 
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"{emoji} You have no permissions.",
                    Color = new DiscordColor(0xCC0C0C)
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }
        #endregion
    }

    public struct Config
    {
        [JsonProperty("token")]
        public string Token { get; internal set; }
        [JsonProperty("commandPrefixes")]
        public string[] CommandPrefixes { get; set; }
        [JsonProperty("emojiData")]
        internal Dictionary<string, DiscordEmoji> Data { get; set; }
        [JsonProperty("listenedGuilds")]
        public ThreadSafeList<ulong> ListenedGuilds { get; set; }
        [JsonIgnore]
        public ConcurrentDictionary<string, DiscordEmoji> EmojiData { get; set; }
    }
}
