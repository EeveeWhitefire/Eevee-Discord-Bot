using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Net.Http;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;
using EeveeBot.Classes.Services;
using EeveeBot.Interfaces;

namespace EeveeBot.Modules
{
    [Name("Emote")]
    [Group(Defined.EEVEE_EMOTES_TABLE_NAME)]
    [Alias("em", "ems", "emote")]
    [Summary("The Emote System")]
    public class EmoteCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;

        private DatabaseContext _db;

        private JsonManager_Service _jsonMngr;
        private Config_Json _config;
        private Random _rnd;

        public EmoteCommands(DatabaseContext db, Random rnd, JsonManager_Service jsonM, Config_Json cnfg)
        {
            _db = db;
            _rnd = rnd;
            _jsonMngr = jsonM;
            _config = cnfg;

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

        public async Task AddEmoteToDatabase(Emote em, string nick, ulong userId)
        {
            var emotes = _db.GetAll<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME);
            if (_db.Exists<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Name.ToLower() == em.Name.ToLower() && x.AdderId == userId)))
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E405, em.ToString(), "Emote", "the Emote Database");
                await Task.CompletedTask;
            }
            else
            {

                bool isDef = !_db.Exists<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Name.ToLower() == em.Name.ToLower()));

                var result = await AddEmoteToEmoteGuilds(em);
                    EeveeEmote nEmote = new EeveeEmote(result.emote, result.guildId, userId, isDef);
                    _db.AddEntity(Defined.EEVEE_EMOTES_TABLE_NAME, nEmote);

                    _eBuilder.WithTitle(nEmote.Name)
                        .WithImageUrl(nEmote.Url)
                        .WithUrl(nEmote.Url);

                    if (nick != null)
                        AddAliasEmote(nick, nEmote, nEmote.Name, false);
            }
        }

        public void AddAliasEmote(string nick, EeveeEmote emote, string name, bool overrideinfo = true)
        {
            if (nick.Length < Defined.NICKNAME_LENGTH_MIN)
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E406, Defined.NICKNAME_LENGTH_MIN, "Nickname (Alias)");
            }
            else if (!_db.Exists<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Name.ToLower() == nick || x.Aliases.Where(y => y.OwnerId == Context.User.Id).Count(y => y.Alias == nick) > 0)))
            {
                if (emote != null)
                {
                    if (emote.Aliases.Count(x => x.OwnerId == Context.User.Id) > Defined.NICKNAME_COUNT_MAX)
                    {
                        Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E408, null, $"You have exceeded the Aliases capacity limit for this Emote!" +
                            $"\nIf you'd really like to add this one, please use {_config.Prefixes[0] }emotes delnick <nick> on " +
                            $"one of the following:\n{string.Join("\n", emote.Aliases.Where(x => x.OwnerId == Context.User.Id).Select(x => x.Alias))}");
                    }
                    else
                    {
                        emote.Aliases.Add(new EeveeEmoteAlias()
                        {
                            AssociatedEmoteId = emote.Id,
                            Alias = nick,
                            OwnerId = Context.User.Id
                        });

                        _db.UpdateEntity(Defined.EEVEE_EMOTES_TABLE_NAME, emote);
                        Defined.BuildSuccessMessage(_eBuilder, Context, $"Added the Alias **{nick}** to the Emote {emote}.", overrideinfo);
                    }
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database", overrideinfo);
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E405, name, "Alias or Emote", "the Emote Database", overrideinfo);
        }

        private struct EmoteAssociation
        {
            public readonly GuildEmote emote;
            public ulong guildId;

            public EmoteAssociation(GuildEmote em, ulong id)
            {
                guildId = id;
                emote = em;
            }
        }

        private async Task<EmoteAssociation> AddEmoteToEmoteGuilds(Emote em)
        {
            SocketGuild guild = null;
            Stream emoteStream = null;

            using (HttpClient client = new HttpClient())
            {
                emoteStream = await client.GetStreamAsync(em.Url);
            }

            int animCount = 0, staticCount = 0;

            foreach (var id in _config.Private_Guilds)
            {
                guild = Context.Client.GetGuild(id);
                animCount = guild.Emotes.Count(x => x.Animated);
                staticCount = guild.Emotes.Count(x => !x.Animated);
                if ((em.Animated && animCount < Defined.MAX_EMOTES_IN_GUILD) || 
                    (!em.Animated && staticCount < Defined.MAX_EMOTES_IN_GUILD))
                {
                    break;
                }
            }
            if ((em.Animated && animCount < Defined.MAX_EMOTES_IN_GUILD) ||
                (!em.Animated && staticCount < Defined.MAX_EMOTES_IN_GUILD))
            {
                var addedEmote = await guild.CreateEmoteAsync(em.Name, new Discord.Image(emoteStream));
                return new EmoteAssociation(addedEmote, guild.Id);
            }

            emoteStream.Dispose();
            return default(EmoteAssociation);
        }

        [Command("add")]
        [Summary("Adds an emote to the Database.")]
        [Alias("create")]
        public async Task AddEmoteCommand(string emoteCode, [Remainder] string nick = null)
        {
            if (Emote.TryParse(emoteCode, out Emote em))
            {
                await AddEmoteToDatabase(em, nick, Context.User.Id);
            }
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteCode, "Emote", "Discord");
            }
            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }
        

        [Command("add")]
        [Permission]
        public async Task AddEmoteCommand(ulong emoteId, [Remainder] string nick = null)
        {
            try
            {
                GuildEmote em = Context.Client.Guilds.SelectMany( x => x.Emotes).FirstOrDefault(x => x.Id == emoteId);
                if (em != null)
                {
                    await AddEmoteToDatabase(em, nick, Context.User.Id);
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }
            catch (Exception)
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }
            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }
        
        [Command("send")]
        [Summary("Sends the requested emote.")]
        [Alias("show")]
        [Permission]
        public async Task SendEmoteCommand([Remainder] string name)
        {
            name = name.Replace(":", string.Empty).ToLower();

            EeveeEmote emote = _db.TryEmoteAssociation(Context.User.Id, name);

            if (emote != null)
            {
                await ReplyAsync(emote.ToString());
            }
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
                await ReplyAsync(string.Empty, embed: _eBuilder.Build());
            }
        }

        [Command("info")]
        [Summary("Sends the requested emote's info (ID, Aliases etc)")]
        [Permission]
        public async Task SendEmoteInfo([Remainder] string name)
        {
            name = name.Replace(":", string.Empty).ToLower();
            EeveeEmote emote =_db.TryEmoteAssociation(Context.User.Id, name);
            if(emote != null)
            {
                var aliases = emote.Aliases.Where(x => x.OwnerId == Context.User.Id);

                _eBuilder.WithTitle($"{emote.Name} info:")
                    .WithDescription($"Emote information for the user {Context.User.Mention}.")
                    .AddField( x =>
                    {
                        x.Name = "__Emote ID__";
                        x.Value = emote.Id;
                        x.IsInline = true;
                    })
                    .AddField(x =>
                    {
                        x.Name = "__Owner ID__";
                        x.Value = emote.AdderId;
                        x.IsInline = true;
                    })
                    .AddField(x =>
                    {
                        x.Name = "__Is Default?__";
                        x.Value = emote.IsDefault;
                        x.IsInline = true;
                    })
                    .AddField( x =>
                    {
                        x.Name = "__Available Aliases__";
                        x.Value = aliases.Count() > 0 ? string.Join("\n", aliases.Select( y => y.Alias)) : "None.";
                        x.IsInline = true;
                    })
                    .WithUrl(emote.Url)
                    .WithThumbnailUrl(emote.Url);
            }
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("rename")]
        [Summary("Adds an Alias to the requested emote.")]
        [Alias("nick", "ren")]
        [Permission]
        public async Task RenameEmoteCommand(string name, [Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty).ToLower();
            name = name.Replace(":", string.Empty).ToLower();

            EeveeEmote emote = _db.TryEmoteAssociation(Context.User.Id, name);
            AddAliasEmote(nick, emote, name);
            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("deleterename")]
        [Summary("Unassociates the given Alias from its currently associated emote.")]
        [Alias("deletenick", "deleteren", "delrename", "delnick", "delren")]
        [Permission]
        public async Task DeleteEmoteNicknameCommand([Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty).ToLower();

            EeveeEmote emote = _db.TryEmoteAssociation(Context.User.Id, nick);

            if (emote != null)
            {
                emote.Aliases.RemoveAll(x => x.Alias.ToLower() == nick);

                _db.UpdateEntity(Defined.EEVEE_EMOTES_TABLE_NAME, emote);

                Defined.BuildSuccessMessage(_eBuilder, Context, $"Deleted the Nickname **{nick}** from the Emote {emote}.");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, nick, "Alias", "the Emote Database");

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("list")]
        [Summary("Lists all the emotes currently in the databases. Paged.")]
        [Alias("all")]
        [Permission]
        public async Task SendEmoteListCommand(int page = 1)
        {
            var emotes = _db.GetAll<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME).OrderByDescending(x => x.Aliases.Count)
                .OrderByDescending(x => x.IsAnimated);

            _eBuilder.WithTitle($"The Emote List")
                .WithDescription($"This is the Emote List for the user {Context.User.Mention}");

            await PrepareEmoteListEmbed(emotes, page);
        }

        private async Task PrepareEmoteListEmbed(IEnumerable<EeveeEmote> emotes, int page)
        {
            const int MaxEmotesPerPage = 12;

            int NumOfEmotes = emotes.Count();
            if (NumOfEmotes > 0)
            {
                int NumOfPages = NumOfEmotes / MaxEmotesPerPage + (NumOfEmotes % MaxEmotesPerPage != 0 ? 1 : 0);

                if (page < 1 || page > NumOfPages)
                {
                    page = 1;
                }

                _eBuilder.WithFooter($"Page {page} out of {NumOfPages}");

                if (page < 1 || page > NumOfPages)
                {
                    page = 1;
                }

                var pagedEmotes = emotes.Skip((page - 1) * MaxEmotesPerPage).Take(MaxEmotesPerPage);

                foreach (EeveeEmote em in pagedEmotes)
                {
                    var userDefined = em.Aliases.Where(x => x.OwnerId == Context.User.Id);
                    _eBuilder.AddField(x =>
                    {
                        x.Name = $"{em} {em.Name}";
                        x.Value = $"Aliases : {userDefined.Count()}" + (em.IsAnimated ? "\nAnimated" : string.Empty) + (em.IsDefault ? "\nDefault" : string.Empty);
                        x.IsInline = true;
                    });
                }
            }
            else
            {
                _eBuilder.WithFooter("No results.")
                    .WithDescription("No **Emotes** were found!");
            }

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("findemotes")]
        [Alias("find", "findemote")]
        [Summary("Lists all the Emotes that their names start with the given input")]
        [Permission]
        public async Task FindEmotesCommand(string arg, int page = 1)
        {
            arg = arg.ToLower();

            var emotes = _db.GetWhere<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, 
                (x => x.Name.ToLower().Contains(arg) || x.Aliases.Exists(y => y.Alias.ToLower().Contains(arg))))
                .OrderByDescending(x => x.Aliases.Count)
                .OrderByDescending(x => x.IsAnimated);

            _eBuilder.WithTitle($"Results")
                .WithDescription($"These are the Results for the user {Context.User.Mention}\nInput - **[{arg}]**");

            await PrepareEmoteListEmbed(emotes, page);
        }

        [Command("deleteemote")]
        [Alias("del", "delete")]
        [Summary("Deletes the Emote from the Database! Whitelisted Users only!")]
        [Permission(Permissions.Whitelisted)]
        public async Task DeleteEmoteCommand([Remainder] string name)
        {
            if(_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id)))
            {
                var emotes = _db.GetWhere<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.TryAssociation(name, Context.User.Id)));

                if(emotes.Count() > 0)
                {
                    string pickMesasge = "Please pick one of the following:\n" + string.Join("\n", emotes.Select(x => $"{emotes.ToList().IndexOf(x)} : {x} \\{x}"));
                    await ReplyAsync(pickMesasge);
                    var emote = emotes.FirstOrDefault();
                    

                    _db.DeleteEntity<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Id == emote.Id));
                    var guild = Context.Client.GetGuild(emote.GuildId);
                    await guild.DeleteEmoteAsync(await guild.GetEmoteAsync(emote.Id));

                    if(guild.Emotes.Count(x => x.Id == emote.Id) < 1)
                    {
                        Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully deleted the Emote **{name}** from Discord and the Database.");

                        await ReplyAsync(string.Empty, embed: _eBuilder.Build());
                    }
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "delete an Emote from the Database");

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }
    }
}