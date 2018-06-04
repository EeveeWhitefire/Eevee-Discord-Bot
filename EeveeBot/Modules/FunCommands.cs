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
    [Group("emotes")]
    [Alias("em", "ems", "emote")]
    [Summary("The Emote System")]
    public class EmoteCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private DatabaseContext _db;
        private Random _rnd;
        private JsonManager_Service _jsonMngr;
        private Config_Json _config;

        private const int NICKNAME_COUNT_MAX = 2;
        private const int NICKNAME_LENGTH_MIN = 2;
        private const int EMOTE_RESOLUTION_SIZE = 36;

        private static IDictionary<ulong, string> _emotesYetToBeLoaded = new Dictionary<ulong, string>();

        public EmoteCommands(DatabaseContext db, Random rnd, JsonManager_Service jsonM, Config_Json cnfg)
        {
            _db = db;
            _rnd = rnd;
            _jsonMngr = jsonM;
            _config = cnfg;

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)]
            };
        }

        public async Task AddEmoteToDatabase(Emote em, string inNick)
        {
            var emotes = _db.GetAll<Db_EeveeEmote>("emotes");
            if (emotes.Where(x => x.Id == em?.Id).Count() > 0)
            {
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E405, em.ToString(), "Emote", "the Emote Database");
                await Task.CompletedTask;
            }
            else
            {
                Db_EeveeEmote nEmote = new Db_EeveeEmote(await AddEmoteToEmoteGuilds(em));
                var definitions = emotes.SelectMany(x => x.Aliases).Where(x => x.OwnerId == Context.User.Id);

                if (inNick != null && !definitions.Select(x => x.Alias.ToLower()).Contains(inNick.ToLower()))
                {
                    if (inNick.Length < NICKNAME_LENGTH_MIN)
                    {
                        await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E406, NICKNAME_LENGTH_MIN, "Nickname (Alias)");
                        await Task.CompletedTask;
                    }
                    else
                    {
                        nEmote.Aliases.Add(new EeveeEmoteAlias()
                        {
                            AssociatedEmoteId = nEmote.Id,
                            Alias = inNick,
                            OwnerId = Context.User.Id
                        });
                    }
                }
                _db.AddEntity("emotes", nEmote);

                _eBuilder.WithTitle(nEmote.Name)
                    .WithImageUrl(nEmote.Url);

                await ReplyAsync(embed: _eBuilder.Build());
            }
        }
        public async Task AddEmoteToDatabase(GuildEmote em, string inNick)
        {
            await AddEmoteToDatabase((Emote)em, inNick);
        }

        private async Task<GuildEmote> AddEmoteToEmoteGuilds(Emote em)
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
                if ((em.Animated && guild.Emotes.Count(x => x.Animated) < Defined.MAX_EMOTES_IN_GUILD) || (!em.Animated && guild.Emotes.Count(x => !x.Animated) < Defined.MAX_EMOTES_IN_GUILD))
                {
                    break;
                }
            }

            var addedEmote = await guild.CreateEmoteAsync(em.Name, new Discord.Image(emoteStream));

            emoteStream.Dispose();

            return addedEmote;
        }

        private async Task<GuildEmote> AddEmoteToEmoteGuilds(Db_EeveeEmote em)
        {
            SocketGuild guild = null;
            Stream emoteStream = null;
            List<SocketGuild> guilds = new List<SocketGuild>();
            bool choseGuild = false;

            using (HttpClient client = new HttpClient())
            {
                emoteStream = await client.GetStreamAsync(em.Url);
            }

            foreach (var id in _config.Private_Guilds)
            {
                var it = Context.Client.GetGuild(id);
                guilds.Add(it);
                if (((em.IsAnimated && it.Emotes?.Count(x => x.Animated) < Defined.MAX_EMOTES_IN_GUILD) || 
                    (!em.IsAnimated && it.Emotes?.Count(x => !x.Animated) < Defined.MAX_EMOTES_IN_GUILD)) && !choseGuild)
                {
                    guild = it;
                    choseGuild = true;
                }
            }
            var d = guilds.SelectMany( x => x.Emotes).FirstOrDefault(x => x.Name == em.Name);
            if (d == null)
            {
                try
                {
                    var addedEmote = await guild.CreateEmoteAsync(em.Name, new Discord.Image(emoteStream));
                    await Task.Delay(2000);

                    emoteStream.Dispose();

                    return addedEmote;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return null;
                }
            }
            return d;
        }

        [Command("add")]
        [Summary("Adds an emote to the Database.")]
        [Alias("create")]
        public async Task AddEmoteCommand(string emoteCode, [Remainder] string nick = null)
        {
            try
            {
                if (Emote.TryParse(emoteCode, out Emote em))
                {
                    await AddEmoteToDatabase(em, nick);
                }
                else
                {
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteCode, "Emote", "Discord");
                }
            }
            catch (Exception)
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
                    await AddEmoteToDatabase(em, nick);
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
            name = name.Replace(":", string.Empty);

            var emotes = _db.GetAll<Db_EeveeEmote>("emotes");
            var emote = emotes
                .FirstOrDefault(x => x.TryAssociation(name, Context.User.Id));

            if(emote != null)
            {
                try
                {
                    await ReplyAsync(emote.ToString());
                }
                catch (Exception e)
                {
                    await Program.Log(e.ToString(), false);
                }
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
            name = name.Replace(":", string.Empty);

            var emotes = _db.GetAll<Db_EeveeEmote>("emotes");
            var definitions = emotes.SelectMany(x => x.Aliases).Where(x => x.OwnerId == Context.User.Id);
            var emote = emotes
                .FirstOrDefault(x => x.Name.ToLower() == name.ToLower() || x.Aliases.Count(y => y.Alias.ToLower() == name.ToLower()) > 0);
            if (emote != null)
            {
                _eBuilder.WithTitle($"{emote.Name} info:")
                    .WithDescription($"Emote information for the user {Context.User.Mention}.")
                    .AddField( x =>
                    {
                        x.Name = "__ID__";
                        x.Value = emote.Id;
                        x.IsInline = false;
                    })
                    .AddField( x =>
                    {
                        x.Name = "__Available Nicknames__";
                        x.Value = emote.Aliases.Count > 0 ? string.Join("\n", emote.Aliases.Select( y => y.Alias)) : "None.";
                        x.IsInline = false;
                    })
                    .WithUrl(emote.Url)
                    .WithThumbnailUrl(emote.Url);

                await ReplyAsync("", embed: _eBuilder.Build());
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
            nick = nick.Replace(":", string.Empty);
            name = name.Replace(":", string.Empty);

            var emotes = _db.GetAll<Db_EeveeEmote>("emotes");

            var emote = emotes.FirstOrDefault(x => x.TryAssociation(name, Context.User.Id));
            var userAliases = emotes.SelectMany(x => x.Aliases.Where(y => y.OwnerId == Context.User.Id)).ToList();

            if (nick.Length < NICKNAME_LENGTH_MIN)
            {
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E406, NICKNAME_LENGTH_MIN, "Nickname (Alias)");
                await Task.CompletedTask;
            }
            else if (!userAliases.Exists(x => x.Alias.ToLower() == nick) &&
                emotes.Select(x => x.Name.ToLower()).Count(x => x.ToLower() == nick) < 1)
            {
                if (emote != null)
                {
                    if (emote.Aliases.Count(x => x.OwnerId == Context.User.Id) > NICKNAME_COUNT_MAX)
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

                        _db.UpdateEntity("emotes", emote);

                        await Defined.SendSuccessMessage(_eBuilder, Context, $"Added the Nickname **{nick}** to the Emote {emote}.");
                    }
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
        }

        [Command("deleterename")]
        [Summary("Unassociates the given Alias from its currently associated emote.")]
        [Alias("deletenick", "deleteren", "delrename", "delnick", "delren")]
        public async Task DeleteEmoteNicknameCommand([Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty);

            if (nick.StartsWith('\"'))
                nick = new string(nick.Skip(1).ToArray());
            if (nick.EndsWith('\"'))
                nick = new string(nick.Take(nick.Length - 1).ToArray());

            var emotes = _db.GetAll<Db_EeveeEmote>("emotes");
            var definition = emotes.SelectMany(x => x.Aliases).Where(x => x.OwnerId == Context.User.Id)
                .FirstOrDefault( x => x.Alias.ToLower() == nick.ToLower());

            if (definition != null)
            {
                var emote = emotes.FirstOrDefault(x => x.Id == definition.AssociatedEmoteId);
                emote.Aliases.RemoveAll(x => x.Alias == definition.Alias);

                _db.UpdateEntity("emotes", emote);

                await Defined.SendSuccessMessage(_eBuilder, Context, $"Deleted the Nickname {nick} from the Emote {emote}.");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, nick, "Alias", "the Emote Database");
        }

        [Command("list")]
        [Summary("Lists all the emotes currently in the databases. Paged.")]
        [Alias("all")]
        public async Task SendEmoteListCommand(int page = 1)
        {
            const int MaxEmotesPerPage = 9;
            var emotes = _db.GetAll<Db_EeveeEmote>("emotes").OrderByDescending(x => x.Aliases.Count)
                .OrderByDescending(x => x.IsAnimated);

            int NumOfEmotes = emotes.Count();
            int NumOfPages = NumOfEmotes / MaxEmotesPerPage + (NumOfEmotes % MaxEmotesPerPage != 0 ? 1 : 0);
            string text = string.Empty;

            if (page < 1 || page > NumOfPages)
            {
                text = $"Invalid page index {page} out of {NumOfPages}, showing page #1";
                page = 1;
            }

            _eBuilder.WithTitle("The Emote List:")
                .WithFooter($"Page {page} out of {NumOfPages}")
                .WithDescription($"**This is the Emote List for the user {Context.User.Mention}**");

            var pagedEmotes = emotes.Skip((page - 1) * MaxEmotesPerPage).Take(MaxEmotesPerPage);

            foreach (Db_EeveeEmote em in pagedEmotes)
            {
                var userDefined = em.Aliases.Where(x => x.OwnerId == Context.User.Id);
                _eBuilder.AddField(x =>
                {
                    x.Name = $"__{em.Name}__";
                    x.Value = $"Aliases : {userDefined.Count()}" + (em.IsAnimated ? "\nAnimated" : string.Empty);
                    x.IsInline = true;
                });
            }

            await ReplyAsync(text, embed: _eBuilder.Build());
        }
    }
}