using System;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

using Discord.Commands;
using Discord.WebSocket;

using EeveeBot.Classes.Services;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Name("General")]
    [Summary("General commands like say, send, uinfo")]
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EeveeEmbed _eBuilder;
        private readonly CommandService _cmdService;
        private readonly Config_Json _config;
        private readonly JsonWrapper_Service _jsonWrapper;

        public GeneralCommands(EeveeEmbed eBuilder, CommandService cmdService, Config_Json config, JsonWrapper_Service jsonWrapper)
        {
            _eBuilder = eBuilder;
            _cmdService = cmdService;
            _config = config;
            _jsonWrapper = jsonWrapper;
        }

        [Command("botinfo")]
        [Alias("info", "binfo")]
        [Permission]
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
               })
               .WithFooter(Defined.COPYRIGHTS_MESSAGE);

            await ReplyAsync(embed: _eBuilder.Build());
        }

        [Command("userinfo")]
        [Alias("uinfo")]
        [Summary("Sends the User Info of the specified user")]
        [Permission]
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

            await ReplyAsync(embed: _eBuilder.Build());
        }

        [Command("guildinfo")]
        [Alias("ginfo")]
        [Summary("Sends the Guild Info of the specified user")]
        [Permission]
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

            await ReplyAsync(embed: _eBuilder.Build());
        }
        
        [Command("genbotinv")]
        [Alias("generatebotinv", "generatebotinvitatio", "genbotinvitation")]
        [Permission]
        public async Task GenerateBotInvitationUrlCommand(ulong client_id = Defined.CLIENT_ID)
        {
            var res = Defined.INVITE_URL(client_id);
            if (client_id == Defined.CLIENT_ID)
            {
                _eBuilder.WithTitle($"{_config.Bot_Name}'s Invite Link");
            }
            else
                _eBuilder.WithTitle("Result");

            _eBuilder.WithDescription(res)
                .WithUrl(res);

            await ReplyAsync(embed: _eBuilder.Build());
        }

        [Command("ud")]
        [Alias("urbandictionary")]
        [Summary("Sends the meaning of the given input from Urban Dictionary!")]
        [Permission]
        public async Task UrbanDictionaryCommand(string input, int num = 1)
        {
            Uri url = new Uri($"http://api.urbandictionary.com/v0/define?term={input.Replace(" ", "%20").ToLower()}");
            var data = (await _jsonWrapper.GetJsonObjectAsync<UrbanDictionary_Json.Output>(url)).list;

            _eBuilder.WithUrl(url.OriginalString);
            for (int i = 0; i < data.Take(num).Count(); i++)
            {
                _eBuilder.WithTitle($"{input} - Definition #{(i + 1)}");

                var definitionParts = $"{data[i].definition}\n\n**Examples:**\n{data[i].example}".SplitByLength(Defined.EMBED_DESCRIPTION_LIMIT, keepWords: true);
                foreach(var defPart in definitionParts)
                {
                    _eBuilder.WithDescription(defPart);
                    await ReplyAsync(embed: _eBuilder.Build());
                }
            }
        }
        
        [Command("minecraftavatar")]
        [Alias("mcavatar")]
        [Summary("Sends the Minecraft Avatar of the specified Player")]
        [Permission]
        public async Task SendMinecraftSkin(string username)
        {
            string url = $@"https://visage.surgeplay.com/full/512/{username}";
            _eBuilder.WithTitle($"{username}'s Avatar:")
                .WithUrl(url)
                .WithImageUrl(url);
            

            await ReplyAsync(embed: _eBuilder.Build());
        }

        private async Task PrepareHelp(CommandInfo command)
        {

            var parameters = string.Join(" ", command.Parameters.Select(y => $"{(y.IsOptional ? "[" : "<")}{y.Name}{(y.IsOptional ? "]" : ">")}"));
            var aliases = string.Join(" ", command.Aliases.Where(y => y != string.Empty).Select(y => $"`{y}`"));

            _eBuilder.WithTitle($"{command.Name} | Commands")
                .AddField(x =>
                {
                    x.Name = "__Summary__";
                    x.Value = $"**{(command.Summary ?? "No Summary")}**.";
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
                    x.Value = $"**{_config.Prefixes[0]}{command.Aliases.FirstOrDefault()}** {parameters}";
                    x.IsInline = false;
                });

            await ReplyAsync(embed: _eBuilder.Build());
        }
        private async Task PrepareHelp(ModuleInfo module)
        {
            var aliases = string.Join(" ", module.Aliases.Where( y => y != string.Empty).Select(y => $"`{y}`"));
            var commands = string.Join("  ", module.Commands.GroupBy(y => y.Name).Select(y => $"`{y.FirstOrDefault().Name}`"));

            _eBuilder.WithTitle($"{module.Name} | Modules")
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

            await ReplyAsync(embed: _eBuilder.Build());
        }


        [Command("help")]
        [Summary("Sends the available modules and their commands.")]
        [Permission]
        public async Task SendHelpCommand([Remainder] string arg = null)
        {
            _eBuilder.WithThumbnailUrl(Defined.COOKIE_THUMBNAIL);

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

                await ReplyAsync(embed: _eBuilder.Build());
            }
            else
            {
                arg = arg.ToLower();
                var module = _cmdService.Modules
                    .FirstOrDefault(x => x.Aliases.Select( y => y.ToLower()).Contains(arg));

                if (module != null)
                    await PrepareHelp(module);
                else
                {
                    var command = _cmdService.Commands.FirstOrDefault(x => x.Aliases.Select(y => y.ToLower()).Contains(arg));

                    if (command != null)
                        await PrepareHelp(command);
                    else
                    {
                        Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, arg, "Command", _config.Bot_Name);
                    }
                }
            }
        }
    }
}
