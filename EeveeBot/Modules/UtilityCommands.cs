using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Scripting;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Services;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Name("Utility")]
    [Summary("Utility commands like purge or memory.")]
    public class UtilityCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;
        
        private Random _rnd;

        private Config_Json _config;
        private DatabaseContext _db;
        private JsonManager_Service _jsonMngr;

        public UtilityCommands(Config_Json config, DatabaseContext db, JsonManager_Service jsonParser, Random rnd)
        {
            _config = config;
            _db = db;
            _rnd = rnd;
            _jsonMngr = jsonParser;

            _eFooter = new EmbedFooterBuilder()
            {
                IconUrl = Defined.BIG_BOSS_THUMBNAIL,
                Text = Defined.FOOTER_MESSAGE
            };

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)],
                Footer = _eFooter
            };
        }

        [Command("ping")]
        [Summary("Sends the Response Time and Latency of the Bot's connection")]
        [Permission]
        public async Task SendPing()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            var msg = await ReplyAsync("Calculating ping");
            s.Stop();
            await msg.ModifyAsync(x => x.Content = $"Response Time: **{s.ElapsedMilliseconds}ms**\n" +
            $"Latency: **{Context.Client.Latency}ms**");
        }

        [Command("chown")]
        [Permission(Permissions.Owner)]
        public async Task ChangeOwnerCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).FindAll();
            if (whitelist.FirstOrDefault(x => x.IsOwner) == null)
            {
                var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                if (tUser == null)
                {
                    tUser = new WhitelistUser(u.Id, true);
                    _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Insert(tUser);
                }
                else
                {
                    tUser.IsOwner = true;
                    _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Update(tUser);
                }
            }
            else if (whitelist.FirstOrDefault(x => x.IsOwner).Id == Context.User.Id)
            {
                if (u.Id != Context.User.Id)
                {
                    var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);
                    currUser.IsOwner = false;

                    var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                    if (tUser == null)
                    {
                        tUser = new WhitelistUser(u.Id, true);
                        _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Insert(tUser);
                    }
                    else
                    {
                        tUser.IsOwner = true;
                        _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Update(tUser);
                    }

                    _db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Update(currUser);
                }
                Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully changed the Owner of {_config.Bot_Name}");
            }
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, null, "change the Owner");
            }
            await ReplyAsync(embed: _eBuilder.Build());
        }
        
        [Group("evaluation")]
        [Alias("eval")]
        [Summary("These commands compile and execute given code as input.")]
        public class CodeEvaluationCommands : ModuleBase<SocketCommandContext>
        {
            private EmbedBuilder _eBuilder;
            private EmbedFooterBuilder _eFooter;

            private Random _rnd;

            private Config_Json _config;
            private DatabaseContext _db;
            private JsonManager_Service _jsonMngr;

            public CodeEvaluationCommands(Config_Json config, DatabaseContext db, JsonManager_Service jsonParser, Random rnd)
            {
                _config = config;
                _db = db;
                _rnd = rnd;
                _jsonMngr = jsonParser;

                _eFooter = new EmbedFooterBuilder()
                {
                    IconUrl = Defined.BIG_BOSS_THUMBNAIL,
                    Text = Defined.FOOTER_MESSAGE
                };

                _eBuilder = new EmbedBuilder
                {
                    Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)],
                    Footer = _eFooter
                };
            }

            private bool CanEvaluate()
            {
                if (!Directory.Exists("Code Evaluation"))
                    Directory.CreateDirectory("Code Evaluation");
                if (_db.GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).Count(x => x.Id == Context.User.Id) > 0)
                {
                    return true;
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "use the Code Evaluation Command");
                return false;
            }

            [Command("c#")]
            [Alias("csharp", "cs")]
            [Permission(Permissions.Whitelisted)]
            public async Task CodeEvaluationCommand([Remainder] string code)
            {
                if (CanEvaluate())
                {
                    Languages lng = Languages.CS;
                    code = code.Between("```", 0);
                    code = code.MultipleTrimStart("csharp", "cs", "c#").TrimStart(' ');
                    await LanguageEmulator.EvaluateAsync(_eBuilder, lng, code, Context, _db, _config);
                }

                await ReplyAsync(embed: _eBuilder.Build());
            }
        }
        

        
        [Command("clean")]
        [Alias("purge")]
        [Summary("Removes all or a number of messages that the user (this bot = def) has sent!")]
        [Permission]
        public async Task CleanMessagesCommand(int toDelete = 50)
        {
            var messages = await Context.Channel.GetMessagesAsync(toDelete*2).FlattenAsync();
            var msgsOfUser = messages.Where(x => x.Author.Id == _config.Client_Id && (x.CreatedAt.Date - DateTime.Now).TotalDays < 15).Take(toDelete);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(msgsOfUser);
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }

        [Command("clean")]
        public async Task PurgeMessagesCommand(SocketGuildUser user, int toDelete = 50)
        {
            var messages = await Context.Channel.GetMessagesAsync(toDelete * 2).FlattenAsync();
            var msgsOfUser = messages.Where(x => x.Author.Id == user.Id && (x.CreatedAt.Date - DateTime.UtcNow).TotalDays < 15).Take(toDelete);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(msgsOfUser);
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }

        [Command("relaunch")]
        [Alias("rel", "reset", "restart", "reboot")]
        [Summary("Restarts the bot!")]
        [Permission(Permissions.Whitelisted)]
        public async Task RelaunchBotCommand()
        {
            var whitelist = _db.GetAll<WhitelistUser>(Defined.WHITELIST_TABLE_NAME);
            DirectoryInfo dInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var path = dInfo.Parent.FullName + "\\Executions\\";

            if (whitelist.FirstOrDefault(x => x.Id == (Context.User).Id) != null)
            {
                ProcessStartInfo ProcessInfo = new ProcessStartInfo($"{path}Run {_config.Bot_Name}.bat")
                {
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                await Task.Run(() => Process.Start(ProcessInfo));
                await Context.Client.LogoutAsync();
                Environment.Exit(0);
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, $"relaunch {_config.Bot_Name}");
            await ReplyAsync(embed: _eBuilder.Build());
        }

        [Command("sudo")]
        [Summary("You know what it means...")]
        [Permission(Permissions.Owner)]
        public async Task SudoCommand(SocketGuildUser user, [Remainder] string args)
        {
            var whitelist = _db.GetAll<WhitelistUser>(Defined.WHITELIST_TABLE_NAME);

            if (!_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.IsOwner && x.Id ==  Context.User.Id)))
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, null, "use the Sudo Command");
                await ReplyAsync(embed: _eBuilder.Build());
            }
            else
            {
                var context = new SudoCommandContext(Context.Client, Context.Guild, Context.Channel, user, null);
            }
        }

        [Command("quit")]
        [Summary("The bot will quit")]
        [Permission(Permissions.Owner)]
        public async Task QuitCommand()
        {
            var whitelist = _db.GetAll<WhitelistUser>(Defined.WHITELIST_TABLE_NAME);

            if (!_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.IsOwner && x.Id == Context.User.Id)))
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, null, "use the Quit Command");
                await ReplyAsync(embed: _eBuilder.Build());
            }
            else
            {
                await Context.Client.LogoutAsync();
                Environment.Exit(0);
            }
        }


        class SudoCommandContext : ICommandContext
        {
            public IDiscordClient Client { get; protected set; }

            public IGuild Guild { get; protected set; }

            public IMessageChannel Channel { get; protected set; }

            public IUser User { get; protected set; }

            public IUserMessage Message { get; protected set; }

            public SudoCommandContext(IDiscordClient client, IGuild g, IMessageChannel ch, IUser u, IUserMessage msg)
            {
                Client = client;
                Guild = g;
                Message = msg;
                User = u;
                Channel = ch;
            }
        }
    }
}
