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
using System.Diagnostics;

namespace EeveeBot.Modules
{
    [Name("General")]
    [Summary("General commands like say, send, uinfo")]
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;

        private DatabaseContext _db;

        private CommandService _cmdService;
        private Config_Json _config;
        private JsonManager_Service _jsonMngr;
        private Random _rnd;

        public GeneralCommands(DatabaseContext db, Random rnd, CommandService cmdService, Config_Json cnfg, JsonManager_Service jsonM)
        {
            _db = db;
            _rnd = rnd;
            _cmdService = cmdService;
            _config = cnfg;
            _jsonMngr = jsonM;

            _eFooter = new EmbedFooterBuilder()
            {
                IconUrl = Defined.BIG_BOSS_THUMBNAIL,
                Text = Defined.COPYRIGHTS_MESSAGE
            };

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)],
                Footer = _eFooter
            };
        }

        [Command("botinfo")]
        [Alias("info", "binfo")]
        public async Task SendBotInfo()
        {
            string privateMemory;
            using (Process proc = Process.GetCurrentProcess())
            {
                privateMemory = $"{Math.Round(proc.PrivateMemorySize64 / Math.Pow(10, 6), 2)}MB";
            }

            _eBuilder.WithTitle(_config.Bot_Name + "'s Info")
                .AddField(x =>
                {
                    x.Name = "__Server Count__";
                    x.Value = Context.Client.Guilds.Count;
                    x.IsInline = true;
                })
                .AddField(x =>
               {
                   x.Name = "__Private Memory Allocated__";
                   x.Value = privateMemory;
                   x.IsInline = true;
               });

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("userinfo")]
        [Alias("uinfo")]
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

        [Command("guildinfo")]
        [Alias("ginfo")]
        [Summary("Sends the Guild Info of the specified user")]
        public async Task GuildInfoCommand()
        {
            var g = Context.Guild;

            _eBuilder.WithTitle(g.Name)
                .AddField("__ID__", g.Id.ToString(), true)
                .AddField("__Created At__", g.CreatedAt, true)
                .AddField("__Owner__", g.Owner.Mention, true)
                .AddField("__Members__", g.MemberCount, true)
                .AddField("__Roles__", g.Roles.Count() > 0 ? string.Join(" | ", g.Roles.Select(x => x.Mention)) : "None", false)
                .AddField("__Text Channels__", g.TextChannels.Count() > 0 ? string.Join(" | ", g.TextChannels.Select(x => x.Mention)) : "None", true)
                .WithThumbnailUrl(g.IconUrl);

            await Context.Channel.SendMessageAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("invite")]
        [Alias("invitelink", "inv", "invlink")]
        public async Task SendInvitationLinkCommand()
        {
            _eBuilder.WithTitle($"{_config.Bot_Name}'s Invite Link")
                .WithUrl(Defined.INVITE_URL)
                .AddField(x =>
                {
                    x.Name = "__Url__";
                    x.Value = Defined.INVITE_URL;
                });

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
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
                    .WithUrl($"http://www.urbandictionary.com/define.php?term={input}");
                for (int i = 0; i < data.list.Take(num).Count(); i++)
                {
                    _eBuilder.AddField(x =>
                    {
                        x.Name = $"__Definition #{(i + 1)}__";
                        x.Value = data.list[i].definition;
                        x.IsInline = false;

                    })
                    .AddField(x =>
                    {
                       x.Name = "__Examples__";
                       x.Value = data.list[i].example;
                       x.IsInline = false;
                    });
                }

                await ReplyAsync(embed: _eBuilder.Build());
            }
            catch (Exception)
            {
                await ReplyAsync("Definition is too long for Embed text length!");
            }
        }

        [Command("say")]
        [Alias("send")]
        [Summary("Sends the given Text to the specified Channel")]
        public async Task SendText(SocketTextChannel channel, [Remainder] string text)
        {
            await channel.SendMessageAsync(text);
        }
        [Command("say")]
        public async Task SendText([Remainder] string text)
        {
            await ReplyAsync(text);
        }
        
        [Command("minecraftavatar")]
        [Alias("mcavatar")]
        [Summary("Sends the Minecraft Avatar of the specified Player")]
        public async Task SendMinecraftSkin(string username)
        {
            string url = $@"https://visage.surgeplay.com/full/512/{username}";
            _eBuilder.WithTitle($"{username}'s Avatar:")
                .WithUrl(url)
                .WithImageUrl(url);
            

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("date")]
        [Alias("currdate")]
        public async Task SendTimeInFuturistic()
        {
            _eBuilder.WithTitle("Current Data")
                .AddField(x =>
                {
                    x.Name = "__General Format__";
                    x.Value = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy");
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "__Futuristic Format__";
                    x.Value = new string(DateTimeOffset.UtcNow.ToString("ddMMyyyy").ToCharArray().ByOrder(0, 2, 4, 5, 1, 3, 6, 7));
                    x.IsInline = true;
                })
                .WithThumbnailUrl(Defined.DATE_THUMBNAIL);

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }


        private async Task PrepareCommandHelp(CommandInfo command, string group = null)
        {

            var parameters = string.Join(" ", command.Parameters.Select(y => $"{(y.IsOptional ? "[" : "<")}{y.Name}{(y.IsOptional ? "]" : ">")}"));
            var aliases = string.Join(" ", command.Aliases.Where(y => y != string.Empty).Select(y => $"`{y}`"));

            _eBuilder.WithTitle($"The {command.Name} Command")
                .AddField(x =>
                {
                    x.Name = "__Summary__";
                    x.Value = $"**{(command.Summary ?? "No Summary")}**\n{(command.Remarks ?? "Public Permission")}.";
                    x.IsInline = false;
                })
                .AddField(x =>
                {
                    x.Name = "__Module__";
                    x.Value = command.Module.Name;
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
                    x.Value = string.Join("\n", command.Aliases.Select(y => $"**{_config.Prefixes[0]}{y}** {parameters}"));
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


        [Command("help")]
        public async Task SendHelpCommand([Remainder] string arg = null)
        {
            _eBuilder.WithThumbnailUrl(Defined.COOKIE_THUMBNAIL);
            arg = arg.ToLower();

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
                var module = _cmdService.Modules
                    .FirstOrDefault(x => x.Aliases.Select( y => y.ToLower()).Contains(arg));

                if (module != null)
                {
                    await PrepareModuleHelp(module);
                }
                else if(module == null)
                {
                    var command = _cmdService.Commands.FirstOrDefault(x => x.Aliases.Select(y => y.ToLower()).Contains(arg));

                    if (command != null)
                        await PrepareCommandHelp(command);
                    else
                    {
                        await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, arg, "Command", _config.Bot_Name);
                    }
                }
            }

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }
    }
}
