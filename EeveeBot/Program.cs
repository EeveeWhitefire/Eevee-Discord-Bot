using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;
using EeveeBot.Classes.Services;

namespace EeveeBot
{
    class Program
    {
        private DiscordSocketClient _dClient;
        private DiscordSocketConfig _dConfig;
        private CommandService _cmdService;
        private CommandServiceConfig _cmdServiceConfig;
        private DatabaseContext _db;
        private TimeSpan _tempSpan;

        private ConcurrentDictionary<ulong, TimeSpan> _userCommandCooldowns;

        private IServiceProvider _serviceProvider;

        public static ConsoleColor defColor = ConsoleColor.White;

        private Config_Json _config;
        private JsonManager_Service _jsonMngr;

        static void Main(string[] args)
            => new Program(Directory.GetCurrentDirectory()).StartBotAsync().GetAwaiter().GetResult();

        /*[DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }*/

        public Program(string tPath)
        {
            _jsonMngr = new JsonManager_Service(tPath + "\\config.json");
            _userCommandCooldowns = new ConcurrentDictionary<ulong, TimeSpan>();
        }


        private async Task StartBotAsync()
        {
            Console.WriteLine(string.Empty); //for logging visual purposes

            #region Discord Client Initialization
            _config = await _jsonMngr.GetJsonObjectAsync<Config_Json>();
            _db = new DatabaseContext(_config);
            Console.Title = _config.Bot_Name + " Bot";

            _dConfig = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true
            };
            _dClient = new DiscordSocketClient(_dConfig);
            _dClient.Log += Log;
            #endregion


            #region Commands Next Initialization
            _cmdServiceConfig = new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                IgnoreExtraArgs = false,
                LogLevel = LogSeverity.Info
            };
            _cmdService = new CommandService(_cmdServiceConfig);

            _dClient.MessageReceived += HandleInput;

            _serviceProvider = BuildServiceProvider();

            await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            _cmdService.Log += _cmdService_Log;
            #endregion

            await _dClient.LoginAsync(TokenType.Bot, _config.Token);
            await _dClient.SetGameAsync($"Pokemon Ultra Sun & Moon | {_config.Prefixes[0]}help");
            await _dClient.StartAsync();

            await Task.Delay(-1);
        }

        private async Task _cmdService_Log(LogMessage arg)
        {
            await Log(arg);
        }

        private async Task HandleInput(SocketMessage msg)
        {
            int paramPos = 0;
            if (!(msg is SocketUserMessage userMsg)) return;
            var context = new SocketCommandContext(_dClient, userMsg);
            if (msg.Author.Id == _config.Client_Id) return;

            string content = msg.Content;
            if(content.CountString(Defined.EMOTE_SEPARATOR_PREFIX) >= 2)
            {
                await Task.Run(async () =>
               {
                   var emoteNames = content.AllBetween(Defined.EMOTE_SEPARATOR_PREFIX);
                   IList<EeveeEmote> emotes = new List<EeveeEmote>();
                   foreach (var n in emoteNames)
                   {
                       var em = _db.TryEmoteAssociation(context.User.Id, n);
                       if(em != null)
                           emotes.Add(em);
                   }
                   string formatted = string.Join(" ", emotes.Select(x => x.ToString()));
                   await Task.Run( async () => await  context.Channel.SendMessageAsync(formatted));
               });
            }

            string prefix = _config.Prefixes.FirstOrDefault(x => userMsg.HasStringPrefix(x, ref paramPos, StringComparison.OrdinalIgnoreCase));
            if (_db.GetCollection<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME).FindOne(x => x.Id == msg.Author.Id) != null) return;

            if (prefix != null || userMsg.HasMentionPrefix(_dClient.CurrentUser, ref paramPos))
            {
                if (!(_userCommandCooldowns.Count(x => x.Key == userMsg.Author.Id) > 0
                    && (DateTime.UtcNow.TimeOfDay - _userCommandCooldowns[userMsg.Author.Id])
                    .TotalMilliseconds < Defined.COMMANDS_COOLDOWN))
                {
                    var result = await _cmdService.ExecuteAsync(context, paramPos, _serviceProvider);
                    if (!result.IsSuccess)
                    {
                        await Log(result.ErrorReason, result.IsSuccess);
                        _userCommandCooldowns.TryRemove(userMsg.Author.Id, out _tempSpan);
                    }
                    else
                        _userCommandCooldowns[userMsg.Author.Id] = DateTime.UtcNow.TimeOfDay;
                }

                foreach (var item in _userCommandCooldowns
                    .Where(x => (DateTime.UtcNow.TimeOfDay - x.Value).TotalMilliseconds >= Defined.COMMANDS_COOLDOWN))
                {
                    _userCommandCooldowns.TryRemove(item.Key, out _tempSpan);
                }
            }
        }


        private IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton(_db)
                .AddScoped<Random>()
                .AddSingleton(_config)
                .AddSingleton(_jsonMngr)
                .AddSingleton(_dClient)
                .AddSingleton(_cmdService)
                .BuildServiceProvider();
        }

        public static void HandleLogSeverity(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
        }

        private const string _dateTimeFormat = "MM/dd/yyyy hh:mm:ss.fff tt";

        private static string BuildLogMessage(string severity, string src, string msg)
        {
            string dateTime = DateTime.Now.ToString(_dateTimeFormat);

            return $"{string.Empty,2}[{dateTime}]    [{severity,-8}] => {src,-8} : {msg}";
        }

        private Task Log(LogMessage msg)
        {
            HandleLogSeverity(msg.Severity);

            Console.WriteLine(BuildLogMessage(msg.Severity.ToString(), msg.Source, msg.Message ?? msg.Exception.InnerException.ToString()));

            Console.ForegroundColor = defColor;
            return Task.CompletedTask;
        }

        public static Task Log(string msg, bool succeeded)
        {
            LogSeverity severity = succeeded ? LogSeverity.Info : LogSeverity.Error;
            HandleLogSeverity(severity);

            Console.WriteLine(BuildLogMessage(severity.ToString(), "Commands", msg));

            Console.ForegroundColor = defColor;
            return Task.CompletedTask;
        }
    }
}
