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

using ImageSharp;

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
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E405, em.ToString(), "Emote", "the Emote Database");
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

                await ReplyAsync(embed: _eBuilder.Build());
                if(nick != null)
                    await RenameEmoteCommand(nEmote.Name, nick);
            }
        }
        public async Task AddEmoteToDatabase(GuildEmote em, string nick, ulong userId)
        {
            await AddEmoteToDatabase((Emote)em, nick, userId);
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

            foreach (var id in _config.Private_Guilds)
            {
                guild = Context.Client.GetGuild(id);
                if ((em.Animated && guild.Emotes.Count(x => x.Animated) < Defined.MAX_EMOTES_IN_GUILD) || 
                    (!em.Animated && guild.Emotes.Count(x => !x.Animated) < Defined.MAX_EMOTES_IN_GUILD))
                {
                    break;
                }
            }
            var addedEmote = await guild.CreateEmoteAsync(em.Name, new Discord.Image(emoteStream));

            emoteStream.Dispose();

            return new EmoteAssociation(addedEmote, guild.Id);
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
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteCode, "Emote", "Discord");
            }
        }

        [Command("add")]
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
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }
            catch (Exception)
            {
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }
        }
        
        [Command("send")]
        [Summary("Sends the requested emote.")]
        [Alias("show")]
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
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
        }

        [Command("info")]
        [Summary("Sends the requested emote's info (ID, Aliases etc)")]
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

                await ReplyAsync(string.Empty, embed: _eBuilder.Build());
            }
            else
            {
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
        }

        [Command("rename")]
        [Summary("Adds an Alias to the requested emote.")]
        [Alias("nick", "ren")]
        public async Task RenameEmoteCommand(string name, [Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty).ToLower();
            name = name.Replace(":", string.Empty).ToLower();

            EeveeEmote emote = _db.TryEmoteAssociation(Context.User.Id, name);

            if (nick.Length < Defined.NICKNAME_LENGTH_MIN)
            {
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E406, Defined.NICKNAME_LENGTH_MIN, "Nickname (Alias)");
                await Task.CompletedTask;
            }
            else if (!_db.Exists<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Name.ToLower() == nick || x.Aliases.Where(y => y.OwnerId == Context.User.Id).Count(y => y.Alias == nick) > 0)))
            {
                if (emote != null)
                {
                    if (emote.Aliases.Count(x => x.OwnerId == Context.User.Id) > Defined.NICKNAME_COUNT_MAX)
                    {
                        await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E408, null, $"You have exceeded the Aliases capacity limit for this Emote!" +
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

                        await Defined.SendSuccessMessage(_eBuilder, Context, $"Added the Alias **{nick}** to the Emote {emote}.");
                    }
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E405, name, "Alias or Emote", "the Emote Database");
        }

        [Command("deleterename")]
        [Summary("Unassociates the given Alias from its currently associated emote.")]
        [Alias("deletenick", "deleteren", "delrename", "delnick", "delren")]
        public async Task DeleteEmoteNicknameCommand([Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty).ToLower();

            EeveeEmote emote = _db.TryEmoteAssociation(Context.User.Id, nick);

            if (emote != null)
            {
                emote.Aliases.RemoveAll(x => x.Alias.ToLower() == nick);

                _db.UpdateEntity(Defined.EEVEE_EMOTES_TABLE_NAME, emote);

                await Defined.SendSuccessMessage(_eBuilder, Context, $"Deleted the Nickname **{nick}** from the Emote {emote}.");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, nick, "Alias", "the Emote Database");
        }

        [Command("list")]
        [Summary("Lists all the emotes currently in the databases. Paged.")]
        [Alias("all")]
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

            await ReplyAsync(string.Empty, embed: _eBuilder.Build());
        }

        [Command("findemotes")]
        [Alias("find", "findemote")]
        [Summary("Lists all the Emotes that their names start with the given input")]
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
        public async Task DeleteEmoteCommand([Remainder] string name)
        {
            if(_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id)))
            {
                var emote = _db.FirstOrDefault<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.TryAssociation(name, Context.User.Id)));

                if(emote != null)
                {
                    _db.DeleteEntity<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, (x => x.Id == emote.Id));
                    var guild = Context.Client.GetGuild(emote.GuildId);
                    await guild.DeleteEmoteAsync(await guild.GetEmoteAsync(emote.Id));

                    if(guild.Emotes.Count(x => x.Id == emote.Id) < 1)
                    {
                        await Defined.SendSuccessMessage(_eBuilder, Context, $"Successfully deleted the Emote **{name}** from Discord and the Database.");

                        await ReplyAsync(string.Empty, embed: _eBuilder.Build());
                    }
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "delete an Emote from the Database");
        }
    }
}