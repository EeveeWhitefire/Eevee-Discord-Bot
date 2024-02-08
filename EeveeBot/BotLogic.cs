using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;
using EeveeBot.Classes.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace EeveeBot
{
    public class BotLogic : IDisposable
    {
        private readonly DiscordSocketConfig _dConfig = new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = true
        };
        private readonly CancellationTokenSource _runningTokenSource = new CancellationTokenSource();
        private readonly ConcurrentDictionary<ulong, DateTime> _userCommandCooldowns = new ConcurrentDictionary<ulong, DateTime>();
        private readonly CommandServiceConfig _cmdServiceConfig = new CommandServiceConfig()
        {
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async,
            IgnoreExtraArgs = false,
            LogLevel = LogSeverity.Info
        };

        private CommandService _cmdService;
        private Config_Json _config;
        private readonly JsonWrapper_Service _jsonWrapper;
        private IServiceProvider _serviceProvider;
        private DiscordSocketClient _dClient;

        private string _dbPath = string.Empty;
        public bool Relaunch { get; private set; } = true;

        public BotLogic()
        {
            _jsonWrapper = new JsonWrapper_Service($@"{Directory.GetCurrentDirectory()}\config.json");
        }

        public async Task StartBotAsync()
        {
            Console.WriteLine(string.Empty); //for logging visual purposes
            await ResetValues();

            try
            {
                _config = await _jsonWrapper.GetJsonObjectAsync<Config_Json>();
                Console.Title = $"{_config.Bot_Name} Bot";

                _dbPath = $"{_config.Project_Directory.Replace("%Sync%", Environment.GetEnvironmentVariable("Sync"))}\\{_config.Bot_Name.Trim()}.db";

                #region Discord Client Initialization
                using (_dClient = new DiscordSocketClient(_dConfig))
                {
                    _dClient.Log += Log;
                    _dClient.MessageReceived += HandleInputAsync;

                    #region Commands Next Initialization
                    _cmdService = new CommandService(_cmdServiceConfig);

                    _serviceProvider = BuildServiceProvider();
                    await _serviceProvider.GetRequiredService<DatabaseRepository.DatabaseContext>().Database.EnsureCreatedAsync();

                    _cmdService.Log += Log;
                    await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
                    #endregion

                    await _dClient.LoginAsync(TokenType.Bot, _config.Token);
                    await _dClient.SetGameAsync($"Bluma is cool | {_config.Prefixes[0]}help");
                    await _dClient.StartAsync();

                    await Task.Delay(-1, _runningTokenSource.Token);

                    await _dClient.StopAsync();
                }
                #endregion
            }
            catch (Exception e)
            {
                await Log(e.ToString(), false);
            }
        }

        #region Command Execution
        private async Task<bool> AllowExecution(SocketUser user, DatabaseRepository db)
        {
            bool cooldown = !_userCommandCooldowns.ContainsKey(user.Id) || // does not exist
                (DateTime.UtcNow - _userCommandCooldowns.GetValueOrDefault(user.Id)).TotalSeconds >= Defined.COMMANDS_COOLDOWN_SECONDS; //cooldown has ended

            bool blacklist = !db.IsBlacklisted(user.Id) || db.NoOwner() || db.IsWhitelisted(user.Id); //user is not blacklisted

            await Task.CompletedTask;

            return cooldown && !user.IsBot && blacklist;
        }

        private async Task HandleInputAsync(SocketMessage msg)
        {
            int paramPos = 0;
            if (!(msg is SocketUserMessage userMsg)) return;
            if (msg.Author.Id == _config.Client_Id) return;

            var context = new SocketCommandContext(_dClient, userMsg);
            var db = _serviceProvider.GetRequiredService<DatabaseRepository>();

            if (!await AllowExecution(msg.Author, db))
            {
                await Log(new LogMessage(LogSeverity.Debug, "Commands", $"User {msg.Author.Username}#{msg.Author.Discriminator} was blocked from the execution logic!"));
                return;
            }

            if (msg.Content.CountString(Defined.EMOTE_SEPARATOR_PREFIX) >= 2)
            {
                await Task.Run(async () =>
                {
                    var emoteNames = msg.Content.AllBetween(Defined.EMOTE_SEPARATOR_PREFIX);
                    var emotes = await db.AssociateMultipleEmotesAsync(msg.Author.Id, emoteNames);

                    string res = string.Join(string.Empty, emotes);

                    if (res != string.Empty)
                        await context.Channel.SendMessageAsync(res);
                });
            }

            bool foundStringPrefix = _config.Prefixes.Exists(x => userMsg.HasStringPrefix(x, ref paramPos, StringComparison.OrdinalIgnoreCase));

            if (foundStringPrefix || userMsg.HasMentionPrefix(_dClient.CurrentUser, ref paramPos))
            {
                var result = await _cmdService.ExecuteAsync(context, paramPos, _serviceProvider);
                if (!result.IsSuccess)
                {
                    await Log(result.ErrorReason, result.IsSuccess);
                    _userCommandCooldowns.TryRemove(userMsg.Author.Id, out DateTime last);
                }
                else
                    _userCommandCooldowns[userMsg.Author.Id] = DateTime.UtcNow;
            }
        }
        #endregion

        #region Preparation
        private async Task ResetValues()
        {
            _serviceProvider = null;
            _config = null;
            _cmdService = null;

            if (_dClient?.Status == UserStatus.Online)
                await _dClient.StopAsync();

            _dClient = null;

            _userCommandCooldowns.Clear();
        }

        private IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddTransient<EeveeEmbed>()
                .AddSingleton(_cmdService)
                .AddSingleton(_config)
                .AddDbContext<DatabaseRepository.DatabaseContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbPath}").EnableSensitiveDataLogging();
                }, ServiceLifetime.Transient)
                .AddSingleton<DatabaseRepository.RuntimeDBCache>()
                .AddTransient<DatabaseRepository>()
                .AddSingleton(_jsonWrapper)
                .AddSingleton(this)
                .BuildServiceProvider();
        }
        #endregion

        #region Logs
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


        private static string BuildLogMessage(LogSeverity severity, string src, string msg)
        {
            string dateTime = DateTime.Now.ToString(Defined.DATETIME_FORMAT);

            return $"{string.Empty,2}[{dateTime}]    [{severity,-8}] => {src,-8} : {msg}";
        }

        private Task Log(LogMessage msg)
        {
            if (msg.Severity == LogSeverity.Debug && !Defined.ALLOW_DEBUG_LOG_MESSAGES)
                return Task.CompletedTask;

            HandleLogSeverity(msg.Severity);
            Console.WriteLine(BuildLogMessage(msg.Severity, msg.Source, msg.Message ?? msg.Exception.InnerException.ToString()));

            Console.ForegroundColor = Defined.DEFAULT_COLOR;
            return Task.CompletedTask;
        }

        public static Task Log(string msg, bool succeeded)
        {
            LogSeverity severity = succeeded ? LogSeverity.Info : LogSeverity.Error;
            HandleLogSeverity(severity);
            Console.WriteLine(BuildLogMessage(severity, "Commands", msg));

            Console.ForegroundColor = Defined.DEFAULT_COLOR;
            return Task.CompletedTask;
        }
        #endregion

        #region Instance Control
        public void Reboot()
        {
            _runningTokenSource.Cancel();
        }

        public void StopBot()
        {
            Relaunch = false;
            _runningTokenSource.Cancel();
        }

        public void Dispose()
        {
            ResetValues().GetAwaiter().GetResult();
            _runningTokenSource.Dispose();
        }
        #endregion
    }
}
