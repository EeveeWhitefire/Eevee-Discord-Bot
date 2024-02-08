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
using Microsoft.EntityFrameworkCore;

namespace EeveeBot.Modules
{
    [Name("Emote")]
    [Group(Defined.EEVEE_EMOTES_TABLE_NAME)]
    [Alias("em", "ems", "emote")]
    [Summary("The Emote System")]
    public class EmoteCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EeveeEmbed _eBuilder;
        private readonly Config_Json _config;
        private readonly DatabaseRepository _db;

        public EmoteCommands(EeveeEmbed eBuilder, Config_Json config, DatabaseRepository db)
        {
            _eBuilder = eBuilder;
            _config = config;
            _db = db;
        }

        private class EmoteAssociation
        {
            public readonly GuildEmote emote;
            public readonly ulong guildId;

            public EmoteAssociation(GuildEmote em, ulong id)
            {
                emote = em;
                guildId = id;
            }
        }

        private async Task PrepareEmoteListEmbedAsync(IEnumerable<EeveeEmote> emotes, int page)
        {
            int NumOfEmotes = emotes.Count();
            int NumOfPages = NumOfEmotes / Defined.MAX_EMOTES_PER_PAGE + (NumOfEmotes % Defined.MAX_EMOTES_PER_PAGE != 0 ? 1 : 0);

            if (NumOfEmotes > 0)
            {
                page = page < 1 || page > NumOfPages ? 1 : page;

                _eBuilder.WithFooter($"Page {page} out of {NumOfPages}");

                if (page < 1 || page > NumOfPages)
                {
                    page = 1;
                }

                var pagedEmotes = emotes.Skip((page - 1) * Defined.MAX_EMOTES_PER_PAGE).Take(Defined.MAX_EMOTES_PER_PAGE);

                foreach (EeveeEmote em in pagedEmotes)
                {
                    var userDefined = em.Aliases.Where(x => x.OwnerId == Context.User.Id);
                    _eBuilder.AddField(x =>
                    {
                        x.Name = $"{em} {em.Name}";
                        x.Value = $"Aliases : {userDefined.Count()}" + (em.IsAnimated ? "\nAnimated" : string.Empty);
                        x.IsInline = true;
                    });
                }
            }
            else
            {
                _eBuilder.WithFooter("No results.")
                    .WithDescription("No **Emotes** were found!");
            }

            await ReplyAsync(embed: _eBuilder.Build());
        }

        public async Task AddEmoteToDatabaseAsync(ulong userId, Emote em, string nick = null)
        {
            if (!await _db.EmoteExistsAsync(userId, em.Id, em.Name))
            {
                var result = await AddEmoteToEmoteGuildsAsync(userId, em);
                if (result != null)
                {
                    EeveeEmote nEmote = new EeveeEmote(result.emote, result.guildId, userId);
                    await _db.AddEmoteAsync(nEmote);

                    _eBuilder.WithTitle(nEmote.Name)
                            .WithImageUrl(nEmote.Url)
                            .WithUrl(nEmote.Url);

                    if (nick != null)
                    {
                        await ReplyAsync(embed: _eBuilder.Build()); //send previous message
                        await AddAliasEmoteAsync(userId, nick, nEmote);
                    }
                }
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E405, em, "Alias or Emote", "the Emote Database");
        }

        private async Task<EmoteAssociation> AddEmoteToEmoteGuildsAsync(ulong userId, Emote em)
        {
            var guilds = Context.Client.Guilds.Where(x => _config.Private_Guilds.Contains(x.Id));
            var existingEmoteGuild = guilds.FirstOrDefault(x => x.Emotes.Exists(y => y.Id == em.Id));

            if (existingEmoteGuild != null)
            {
                var emote = existingEmoteGuild.Emotes.FirstOrDefault(x => x.Id == em.Id);
                return new EmoteAssociation(emote, existingEmoteGuild.Id);
            }

            if (!await _db.IsEmoteAssociatedAsync(userId, em.Name))
            {
                foreach (var guild in guilds)
                {
                    if ((em.Animated && guild.Emotes.Count(x => x.Animated) < Defined.MAX_EMOTES_IN_GUILD) ||
                        (!em.Animated && guild.Emotes.Count(x => !x.Animated) < Defined.MAX_EMOTES_IN_GUILD))
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            using (Stream emoteStream = await client.GetStreamAsync(em.Url))
                            {
                                var addedEmote = await guild.CreateEmoteAsync(em.Name, new Image(emoteStream));
                                return new EmoteAssociation(addedEmote, guild.Id);
                            }
                        }
                    }
                }
            }

            Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E412); //can't add the new emote fam

            return default;
        }

        public async Task AddAliasEmoteAsync(ulong userId, string nick, EeveeEmote emote)
        {
            if (nick.Length < Defined.NICKNAME_LENGTH_MIN)
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E406, Defined.NICKNAME_LENGTH_MIN, "Nickname (Alias)");
            else if (!await _db.IsEmoteAssociatedAsync(userId, nick))
            {
                if(emote.Aliases.Count( y => y.OwnerId == userId) > Defined.NICKNAME_COUNT_MAX)
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E408, type: $"You have exceeded the Aliases capacity limit for this Emote!" +
                        $"\nIf you'd really like to add this one, please use {_config.Prefixes[0] }em delren <nick> on " +
                        $"one of the following:\n{string.Join("\n", emote.Aliases.Where(x => x.OwnerId == Context.User.Id).Select(x => x.Alias))}");
                else
                {
                    EeveeEmoteAlias alias = new EeveeEmoteAlias() { Alias = nick.ToLower(), OwnerId = userId, EmoteId = emote.Id};
                    await _db.AddAliasAsync(alias);

                    Defined.BuildSuccessMessage(_eBuilder, Context, $"Added the Alias **{nick}** to the Emote {emote}.");
                }
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E405, nick, "Alias or Emote", "the Emote Database");
        }

        [Command("add")]
        [Summary("Adds an emote to the Database.")]
        [Alias("create")]
        public async Task AddEmoteCommand(string emoteCode, [Remainder] string nick = null)
        {
            if (Emote.TryParse(emoteCode, out Emote em))
                await AddEmoteToDatabaseAsync(Context.User.Id, em, nick);
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteCode, "Emote", "Discord");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
        

        [Command("add")]
        [Permission]
        public async Task AddEmoteCommand(ulong emoteId, [Remainder] string nick = null)
        {
            try
            {
                GuildEmote em = Context.Client.Guilds.SelectMany( x => x.Emotes).FirstOrDefault(x => x.Id == emoteId);
                if (em != null)
                    await AddEmoteToDatabaseAsync(Context.User.Id, em, nick);
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }
            catch (Exception)
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, emoteId, "Emote associated with the ID", "Discord");
            }

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("fetchguild")]
        [Alias("fetch")]
        public async Task FetchEmotesFromGuild()
        {
            foreach (var em in Context.Guild.Emotes)
                await AddEmoteToDatabaseAsync(Context.User.Id, em);

            _eBuilder.WithTitle($"Emotes Fetching")
                .WithDescription($"Successfully fetched all Emotes from the Guild **{Context.Guild.Name}**")
                .WithThumbnailUrl(string.Empty)
                .WithImageUrl(string.Empty);

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
        
        [Command("send")]
        [Summary("Sends the requested emote.")]
        [Alias("show")]
        [Permission]
        public async Task SendEmoteCommand([Remainder] string name)
        {
            EeveeEmote emote = await _db.AssociateEmoteAsync(Context.User.Id, name);

            if (emote != null)
                await ReplyAsync(emote.ToString());
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
                await ReplyAsync(embed: _eBuilder.Build());
            }

            _db.Dispose();
        }

        [Command("info")]
        [Summary("Sends the requested emote's info (ID, Aliases etc)")]
        [Permission]
        public async Task SendEmoteInfo([Remainder] string name)
        {
            EeveeEmote emote = await _db.AssociateEmoteAsync(Context.User.Id, name);

            if (emote != null)
            {
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
                    .AddField( x =>
                    {
                        x.Name = "__Available Aliases__";
                        x.Value = emote.Aliases.Count() > 0 ? string.Join("\n", emote.Aliases.Select( y => y.Alias)) : "None.";
                        x.IsInline = true;
                    })
                    .WithUrl(emote.Url)
                    .WithThumbnailUrl(emote.Url);
            }
            else
            {
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("rename")]
        [Summary("Adds an Alias to the requested emote.")]
        [Alias("nick", "ren")]
        [Permission]
        public async Task RenameEmoteCommand(string name, [Remainder] string nick)
        {
            EeveeEmote emote = await _db.AssociateEmoteAsync(Context.User.Id, name);

            if (emote != null)
                await AddAliasEmoteAsync(Context.User.Id, nick, emote);
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("deleterename")]
        [Summary("Unassociates the given Alias from its currently associated emote.")]
        [Alias("deletenick", "deleteren", "delrename", "delnick", "delren")]
        [Permission]
        public async Task DeleteEmoteNicknameCommand([Remainder] string nick)
        {
            var emote = await _db.AssociateEmoteAsync(Context.User.Id, nick);

            if (emote != null)
            {
                var alias = emote.Aliases.FirstOrDefault(x => x.Alias.EqualsCaseInsensitive(nick));
                if (alias != null)
                {
                    await _db.DeleteEmoteAliasAsync(alias);

                    Defined.BuildSuccessMessage(_eBuilder, Context, $"Deleted the Nickname **{nick}** from the Emote {emote}.");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, nick, "Alias", "the Emote Database");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, nick, "Alias", "the Emote Database");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("list")]
        [Summary("Lists all the emotes currently in the databases. Paged.")]
        [Alias("all")]
        [Permission]
        public async Task SendEmoteListCommand(int page = 1)
        {
            await PrepareEmoteListEmbedAsync(await _db.GetEmotesAsync(Context.User.Id), page);
        }

        [Command("findemotes")]
        [Alias("find", "findemote")]
        [Summary("Lists all the Emotes that their names start with the given input")]
        [Permission]
        public async Task FindEmotesCommand(string arg, int page = 1)
        {
            var emotes = await _db.GetEmotesAsync(Context.User.Id, arg);

            _eBuilder.WithTitle($"Results")
                .WithDescription($"These are the Results for the user {Context.User.Mention}\nInput - **[{arg}]**");

            await PrepareEmoteListEmbedAsync(emotes, page);

            _db.Dispose();
        }

        [Command("deleteemote")]
        [Alias("del", "delete")]
        [Summary("Deletes the Emote from the Database! Whitelisted Users only!")]
        [Permission(Permissions.Whitelisted)]
        public async Task DeleteEmoteCommand([Remainder] string name)
        {
            if (_db.IsWhitelisted(Context.User.Id))
            {
                var emote = await _db.AssociateEmoteAsync(Context.User.Id, name);

                if (emote != null)
                {
                    await _db.DeleteEmoteAsync(emote);

                    var guild = Context.Client.GetGuild(emote.GuildId);
                    await Context.Client.GetGuild(emote.GuildId).DeleteEmoteAsync(await guild.GetEmoteAsync(emote.Id));

                    if(!Context.Client.GetGuild(emote.GuildId).Emotes.Exists(x => x.Id == emote.Id))
                        Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully deleted the Emote **{name}** from Discord and the Database.");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, name, "Emote", "the Emote Database");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, type: "delete an Emote from the Database");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
    }
}
