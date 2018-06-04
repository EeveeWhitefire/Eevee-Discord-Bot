using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Services;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Name("General")]
    [Summary("General commands like say, send, uinfo")]
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;
        private DatabaseContext _db;
        private Random _rnd;
        private CommandService _cmdService;
        private Config_Json _config;
        private JsonManager_Service _jsonMngr;

        public GeneralCommands(DatabaseContext db, Random rnd, CommandService cmdService, Config_Json cnfg, JsonManager_Service jsonM)
        {
            _db = db;
            _rnd = rnd;
            _cmdService = cmdService;
            _eFooter = new EmbedFooterBuilder();
            _eFooter.WithText("EeveeBot made by TalH#6144. All rights reserved ©");
            _config = cnfg;
            _jsonMngr = jsonM;

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)]
            };
        }

        [Command("uinfo")]
        [Alias("userinfo")]
        [Summary("Sends the User Info of the specified user")]
        public async Task UserInfoCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            _eBuilder.WithTitle($"{u.Username}#{u.Discriminator}")
                .AddField("__ID__", u.Id.ToString(), true)
                .AddField("__Nickname__", u.Nickname ?? "None", true)
                .AddField("__Joined At__", u.JoinedAt != null ? u.JoinedAt.ToString() : "Undefined", true)
                .AddField("__Roles__", u.Roles.Count() > 0 ? string.Join(" | ", u.Roles.Select(x => x.Mention)) : "None", false)
                .AddField("__Is Owner?__", (Context.Guild.OwnerId == u.Id).ToString(), true)
                .AddField("__Is Bot?__", u.IsBot.ToString(), true)
                .WithThumbnailUrl(u.GetAvatarUrl());

            await Context.Channel.SendMessageAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("invite")]
        [Alias("invitelink", "inv", "invlink")]
        public async Task SendInvitationLinkCommand()
        {
            await ReplyAsync(@"https://discordapp.com/oauth2/authorize?client_id=337649506856468491&scope=bot");
        }

        [Command("ud")]
        [Alias("urbandictionary")]
        [Summary("Sends the meaning of the given input from Urban Dictionary!")]
        public async Task UrbanDictionaryCommand(string input, int num = 1)
        {
            Uri url = new Uri($"http://api.urbandictionary.com/v0/define?term={input}");
            var data = await _jsonMngr.GetJsonObjectAsync<UrbanDictionary_Json.Output>(url);

            try
            {
                _eBuilder.WithTitle(input)
                    .WithDescription(string.Join("\n", data.list.Take(num).Select(x => $"**{data.list.IndexOf(x) + 1}.** {x.definition}\n**Example:**\n{x.example}\n")))
                    .WithUrl($"http://www.urbandictionary.com/define.php?term={input}");

                await ReplyAsync(embed: _eBuilder.Build());
            }
            catch (Exception)
            {
                await ReplyAsync("Definition is too long for Embed text length!");
            }
        }

        [Command("say")]
        [Alias("send")]
        public async Task SendText(SocketTextChannel channel, [Remainder] string text)
        {
            await channel.SendMessageAsync(text);
        }
        [Command("say")]
        public async Task SendText([Remainder] string text)
        {
            await ReplyAsync(text);
        }


        [Command("mcavatar")]
        public async Task SendMinecraftSkin(string username)
        {
            string url = $@"https://visage.surgeplay.com/full/512/{username}";
            _eBuilder.WithTitle($"{username}'s Avatar:")
                .WithUrl(url)
                .WithImageUrl(url);
            

            await ReplyAsync("", embed: _eBuilder.Build());
        }

        [Command("date")]
        [Alias("currdate")]
        public async Task SendTimeInFuturistic()
        {
            string now = DateTimeOffset.UtcNow.ToString("ddMMyyyy");
            char[] arr = now.ToCharArray();
            await ReplyAsync("Current date is : " + new string(arr.ByOrder(0,2,4,5,1,3,6,7)));
        }


        private async Task PrepareCommandHelp(IEnumerable<CommandInfo> commands, string group = null)
        {
            var cmd = commands.FirstOrDefault();
            var parameters = string.Join("\n",
                    commands.Select(x => $"**{_config.Prefixes[0]}{(group != null ? $"{group} " : string.Empty)}{cmd.Name}** " +
                   $"{string.Join(" ", x.Parameters.Select(y => $"{(y.IsOptional ? "[" : "<")}{y.Name}{(y.IsOptional ? "]" : ">")}"))}"));
            var aliases = string.Join(" ", cmd.Aliases.Where(y => y != string.Empty).Select(y => $"`{y}`"));

            _eBuilder.WithTitle($"The {cmd.Name} Command")
                .AddField(x =>
                {
                    x.Name = "__Summary__";
                    x.Value = $"**{(cmd.Summary ?? "No Summary")}**\n{(cmd.Remarks ?? "Public Permission")}.";
                    x.IsInline = false;
                })
                .AddField(x =>
                {
                    x.Name = "__Module__";
                    x.Value = cmd.Module.Name;
                    x.IsInline = false;
                })
                .AddField(x =>
                {
                    x.Name = "__Aliases__";
                    x.Value = aliases.Length > 0 ? aliases : "No Aliases.";
                    x.IsInline = false;
                })
               .AddField(x =>
                {
                    x.Name = "__Syntax__";
                    x.Value = parameters;
                    x.IsInline = false;
                });

            await Task.CompletedTask;
        }
        private async Task PrepareModuleHelp(ModuleInfo module)
        {
            var aliases = string.Join(" ", module.Aliases.Where( y => y != string.Empty).Select(y => $"`{y}`"));
            var commands = string.Join("  ", module.Commands.GroupBy(y => y.Name).Select(y => $"`{y.FirstOrDefault().Name}`"));

            _eBuilder.WithTitle($"The {module.Name} Module")
                .AddField(x =>
               {
                   x.Name = "__Summary__";
                   x.Value = module.Summary ?? "No Summary.";
                   x.IsInline = false;
               })
                .AddField(x =>
               {
                   x.Name = "_Aliases_";
                   x.Value = aliases.Length > 0 ? aliases : "No Aliases.";
                   x.IsInline = false;
               })
                .AddField(x =>
               {
                   x.Name = "__Commands__";
                   x.Value = commands.Length > 0 ? commands : "No Commands.";
                   x.IsInline = false;
               });

            await Task.CompletedTask;
        }
        private async Task Prepare404Help(string arg)
        {
            _eBuilder.WithTitle("ERROR 404")
                .WithDescription($"Command or Module **{arg}** wasn't found!");

            await Task.CompletedTask;
        }


        [Command("help")]
        public async Task SendHelpCommand([Remainder] string arg = null)
        {
            _eFooter.WithIconUrl(@"https://cdn.discordapp.com/attachments/297913371884388353/449286701257588747/big_boss-blurple.png");
            _eBuilder.WithFooter(_eFooter)
                .WithThumbnailUrl(@"http://static1.squarespace.com/static/57b5da73b3db2b7747f9c3a4/t/58916aaaebbd1ade326d74f2/1510756771216/");
            if (arg == null)
            {
                _eBuilder.WithTitle("All Commands")
                    .WithDescription($"Use **{_config.Prefixes[0]}**help <module / command name> for help in a specific **Module** or **Command**");
                foreach (var mod in _cmdService.Modules.OrderByDescending(x => x.Commands.Count))
                {
                    _eBuilder.AddField(x =>
                    {
                        x.Name = $"{mod.Name}";
                        x.Value = string.Join("\n", mod.Commands.GroupBy(z => z.Name).Select(y => y.FirstOrDefault().Name));
                        x.IsInline = true;
                    });
                }
            }
            else
            {
                var split = arg.ToLower().Split(' ');
                var module = _cmdService.Modules
                    .FirstOrDefault(x => x.Name.ToLower() == split[0] || (x.Aliases.Select( y => y.ToLower()).Contains(split[0])));

                if (module != null)
                {
                    if (split.Length > 1)
                    {
                        var commands = module.Commands.Where(x => x.Name.ToLower() == split[1] || (x.Aliases.Select(y => y.ToLower()).Contains(split[1])));
                        if (commands.Count() > 0)
                            await PrepareCommandHelp(commands, module.Group.ToLower());
                        else
                            await PrepareModuleHelp(module);
                    }
                    else await PrepareModuleHelp(module);

                }
                else if(module == null)
                {
                    if(split.Length > 1)
                    {
                        await Prepare404Help(arg);
                    }
                    else
                    {
                        var commands = _cmdService.Commands.Where(x => x.Module.Group == null && x.Name.ToLower() == split[0] || (x.Aliases.Select(y => y.ToLower()).Contains(split[0])));
                        if (commands.Count() > 0)
                            await PrepareCommandHelp(commands);
                        else
                        {
                            await Prepare404Help(arg);
                        }
                    }
                }
            }

            await ReplyAsync("", embed: _eBuilder.Build());
        }
    }
}
