using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

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

        private IServiceProvider _serviceProvider;

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

        }


        private async Task StartBotAsync()
        {
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

            _serviceProvider = BuildServiceProvider();

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
            await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            #endregion

            await _dClient.LoginAsync(TokenType.Bot, _config.Token);
            await _dClient.SetGameAsync($"Pokemon Ultra Sun & Moon | {_config.Prefixes[0]}help");
            await _dClient.StartAsync();

            await Task.Delay(-1);
        }

        private async Task HandleInput(SocketMessage msg)
        {
            var userMsg = msg as SocketUserMessage;
            int paramPos = 0;
            if (userMsg == null) return;
            string prefix = _config.Prefixes.FirstOrDefault(x => userMsg.HasStringPrefix(x, ref paramPos, StringComparison.OrdinalIgnoreCase));
            var isBlacklisted = _db.GetCollection<Db_BlacklistUser>("blacklist").FindOne(x => x.Id == msg.Author.Id) != null;

            if (prefix != null && !isBlacklisted)
            {
                var context = new SocketCommandContext(_dClient, userMsg);
                var result = await _cmdService.ExecuteAsync(context, paramPos, _serviceProvider);
                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
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
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
