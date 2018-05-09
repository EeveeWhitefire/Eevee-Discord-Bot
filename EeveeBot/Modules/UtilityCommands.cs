using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Services;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Summary("Utility commands like purge or memory.")]
    public class UtilityCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private Color[] _pallete = Defined.Colors;

        private Config_Json _config;
        private DatabaseContext _db;
        private JsonManager_Service _jsonMngr;

        public UtilityCommands(Config_Json config, DatabaseContext db, JsonManager_Service jsonParser)
        {
            _config = config;
            _db = db;
            _jsonMngr = jsonParser;

            _eBuilder = new EmbedBuilder()
            {
                Color = _pallete[new Random().Next(0, _pallete.Length)]
            };
        }
        
        /*
        [Command("clean")]
        [Alias("purge")]
        [Summary("Removes all or a number of messages that the user (this bot = def) has sent!")]
        public async Task CleanMessagesCommand(int toDelete = 100)
        {
            var messages = await Context.Channel.GetMessagesBeforeAsync(Context.Message.Id, 100);
            var msgsOfUser = messages.Where(x => x.Author.Id == _config.Client_Id && (x.CreationTimestamp.Date - DateTime.Now).TotalDays < 15).Take(toDelete);

            await Context.Channel.DeleteMessagesAsync(msgsOfUser);
            await Context.Message.DeleteAsync();
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }

        [Command("clean")]
        public async Task PurgeMessagesCommand(SocketGuildUser user, int toDelete = 100)
        {
            var messages = await Context.Channel.GetMessagesBeforeAsync(Context.Message.Id, 100);
            var msgsOfUser = messages.Where(x => x.Author.Id == user.Id && (DateTime.Now - x.CreationTimestamp.Date).TotalDays < 14).Take(toDelete);

            await Context.Guild.(msgsOfUser);
            await Context.Message.DeleteAsync();
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }*/

        [Command("memory")]
        [Summary("Gets the number of bytes that the Bot's process has allocated.")]
        public async Task GetMemoryStateCommand()
        {
            string privateMemory;
            using (Process proc = Process.GetCurrentProcess())
            {
                privateMemory = $"{Math.Round(proc.PrivateMemorySize64 / Math.Pow(10,6), 2)}MB";
            }

            _eBuilder.AddField("__Private Memory Allocated__", privateMemory);

            await ReplyAsync("", embed: _eBuilder.Build());
        }
        
        [Command("relaunch")]
        [Alias("rel", "reset", "restart", "reboot")]
        [Summary("Restarts the bot!")]
        public async Task RelaunchBotCommand()
        {
            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            DirectoryInfo dInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var path = dInfo.Parent.FullName + "\\Executions\\";

            if (whitelist.FirstOrDefault(x => x.IsOwner)?.Id == (Context.User).Id)
            {
                ProcessStartInfo ProcessInfo = new ProcessStartInfo($"{path}Run {_config.Bot_Name}.bat")
                {
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                await Task.Run(() => Process.Start(ProcessInfo));
                Environment.Exit(1);
            }
            else
            {
                _eBuilder.WithTitle("ERROR")
                    .WithDescription("You don't have the required permission to relaunch the Bot!");
            }
        }

        [Command("sudo")]
        [Summary("You know what it means...")]
        public async Task SudoCommand(SocketGuildUser user, [Remainder] string args)
        {
            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();

            if (whitelist.FirstOrDefault(x => x.IsOwner)?.Id == (Context.User).Id)
            {
                await Task.CompletedTask;
            }
            else
            {
                _eBuilder.WithTitle("ERROR")
                    .WithDescription("You don't have the required permission to use sudo!");
            }
        }
    }
}
