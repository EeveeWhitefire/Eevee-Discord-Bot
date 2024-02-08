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
using Microsoft.EntityFrameworkCore;

namespace EeveeBot.Modules
{
    [Name("Utility")]
    [Summary("Utility commands like purge or memory.")]
    public class UtilityCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EeveeEmbed _eBuilder;
        private readonly Config_Json _config;
        private readonly DatabaseRepository _db;
        private readonly BotLogic _instance;

        public UtilityCommands(EeveeEmbed eBuilder, Config_Json config, DatabaseRepository db, BotLogic p)
        {
            _eBuilder = eBuilder;
            _config = config;
            _db = db;
            _instance = p;
        }

        [Command("ping")]
        [Summary("Sends the Response Time and Latency of the Bot's connection")]
        [Permission]
        public async Task SendPing()
        {
            Stopwatch s = new Stopwatch();

            s.Start();
            var msg = await ReplyAsync("Calculating ping ...");
            s.Stop();

            await msg.ModifyAsync(x => x.Embed = _eBuilder.WithTitle("Ping Calculation").WithDescription($"Response Time: **{s.ElapsedMilliseconds}ms**\n" +
                $"Latency: **{Context.Client.Latency}ms**").Build());
        }

        [Command("chown")]
        [Permission(Permissions.Owner)]
        public async Task ChangeOwnerCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            if(_db.CanChangeOwner(Context.User.Id))
            {
                await _db.ChangeOwnerAsync(u.Id, Context.User.Id);

                Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully changed the Owner of {_config.Bot_Name}");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, type: "change the Owner (unless there is no Owner)");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
        
        [Group("Evaluation")]
        [Alias("eval")]
        [Summary("These commands compile and execute given code as input.")]
        public class CodeEvaluationCommands : ModuleBase<SocketCommandContext>
        {
            private readonly EeveeEmbed _eBuilder;
            private readonly Config_Json _config;
            private readonly DatabaseRepository _db;

            public CodeEvaluationCommands(EeveeEmbed eBuilder,  Config_Json config, DatabaseRepository db)
            {
                _eBuilder = eBuilder;
                _config = config;
                _db = db;
            }

            private bool CanEvaluate()
            {
                if (!Directory.Exists("Code Evaluation"))
                    Directory.CreateDirectory("Code Evaluation");
                if (_db.IsWhitelisted(Context.User.Id))
                    return true;
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, type: "use the Code Evaluation Command");

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

                _db.Dispose();
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
            if (_db.IsWhitelisted(Context.User.Id))
                _instance.Reboot();
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, type: $"relaunch {_config.Bot_Name}");
                await ReplyAsync(embed: _eBuilder.Build());
            }

            _db.Dispose();
        }


        [Command("quit")]
        [Summary("The bot will quit")]
        [Permission(Permissions.Owner)]
        public async Task QuitCommand()
        {
            if (_db.IsWhitelisted(Context.User.Id))
                _instance.StopBot();
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, type: "use the Quit Command");
                await ReplyAsync(embed: _eBuilder.Build());
            }

            _db.Dispose();
        }
    }
}
