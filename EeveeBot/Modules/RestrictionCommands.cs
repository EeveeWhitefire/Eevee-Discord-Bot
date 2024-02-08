using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Name("Whitelist")]
    [Group(Defined.WHITELIST_TABLE_NAME)]
    [Alias("wl")]
    public class WhitelistCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EeveeEmbed _eBuilder;
        private readonly DatabaseRepository _db;

        public WhitelistCommands(EeveeEmbed eBuilder, DatabaseRepository db)
        {
            _eBuilder = eBuilder;
            _db = db;
        }

        [Command("add")]
        [Alias("whiten")]
        [Permission(Permissions.Owner)]
        public async Task WhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            if (u.IsBot)
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E411, type: "added to the Whitelist");
            else
            {
                if (_db.IsWhitelisted(Context.User.Id))
                {
                    await _db.WhitelistAddAsync(u.Id);

                    Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully added the User **{u.Username}#{u.Discriminator}** to the Whitelist");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, type: "add a User to the Whitelist");
            }

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("delete")]
        [Alias("remove", "del", "rem")]
        [Permission(Permissions.Owner)]
        public async Task DeWhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            if (_db.IsWhitelisted(Context.User.Id))
            {
                if (_db.IsWhitelisted(u.Id))
                {
                    if(Context.User.Id == u.Id || _db.IsOwner(Context.User.Id))
                    {
                        await _db.WhitelistRemoveAsync(u.Id);

                        Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully removed the User **{u.Username}#{u.Discriminator}** from the Whitelist");
                    }
                    else
                        Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, type: "remove a User who isn't yourself from the Whitelist");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, $"{u.Username}#{u.Discriminator}", "User", "the Whitelist");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, type: "remove a User from the Whitelist");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("list")]
        [Alias("show", "showlist")]
        [Permission]
        public async Task ShowWhitelistCommand()
        {
            _eBuilder.WithTitle("The Whitelist:");

            var whitelist = await _db.GetWhitelistAsync();
            
            if(whitelist.IsEmpty())
                _eBuilder.WithDescription("Empty");
            else
            {
                foreach (var u in whitelist)
                {
                    try
                    {
                        var guildUser = Context.Guild.Users.FirstOrDefault(x => x.Id == u.Id);
                        if(guildUser != null)
                        {
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"{(u.IsOwner ? Defined.OWNER_ICON : Defined.WHITELISTED_ICON)} {guildUser.Nickname ?? guildUser.Username}";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                        else
                        {
                            var user = Context.Client.GetUser(u.Id);
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"{(u.IsOwner ? Defined.OWNER_ICON : Defined.WHITELISTED_ICON)} {user.Username}#{user.Discriminator}";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                    }
                    catch (Exception)
                    {
                    }

                }
            }

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
    }

    [Name("Blacklist")]
    [Group(Defined.BLACKLIST_TABLE_NAME)]
    [Alias("bl")]
    public class BlacklistCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EeveeEmbed _eBuilder;
        private readonly DatabaseRepository _db;

        public BlacklistCommands(EeveeEmbed eBuilder, DatabaseRepository db)
        {
            _eBuilder = eBuilder;
            _db = db;
        }

        [Command("add")]
        [Alias("blacken")]
        [Permission(Permissions.Whitelisted)]
        public async Task BlackenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            if (u.IsBot)
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E411, type: "added to the Blacklist");
            else
            {
                if (_db.IsWhitelisted(Context.User.Id))
                {
                    await _db.BlacklistAddAsync(u.Id);

                    Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully added the User **{u.Username}#{u.Discriminator}** to the Blacklist");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E409, type: "add a User to the Blacklist");
            }
            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("delete")]
        [Alias("remove", "del", "rem")]
        [Permission(Permissions.Owner)]
        public async Task DeBlackenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            if (_db.IsOwner(Context.User.Id))
            {
                if (_db.IsBlacklisted(u.Id))
                {
                    await _db.BlacklistRemoveAsync(u.Id);

                    Defined.BuildSuccessMessage(_eBuilder, Context, $"Successfully removed the User **{u.Username}#{u.Discriminator}** from the Blacklist");
                }
                else
                    Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E404, $"{u.Username}#{u.Discriminator}", "User", "the Blacklist");
            }
            else
                Defined.BuildErrorMessage(_eBuilder, Context, ErrorTypes.E410, type: "remove a User from the Blacklist");

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }

        [Command("list")]
        [Alias("show", "showlist")]
        [Permission]
        public async Task ShowBlacklistCommand()
        {
            _eBuilder.WithTitle("The Blacklist:");

            var blacklist = await _db.GetBlacklistAsync();

            if (blacklist.IsEmpty())
            {
                _eBuilder.WithDescription("Empty");
            }
            else
            {
                foreach (var u in blacklist)
                {
                    try
                    {
                        var guildUser = Context.Guild.Users.FirstOrDefault(x => x.Id == u.Id);
                        if (guildUser != null)
                        {
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"{guildUser.Nickname ?? guildUser.Username}";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                        else
                        {
                            var user = Context.Client.GetUser(u.Id);
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"{user.Username}#{user.Discriminator}";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                    }
                    catch (Exception)
                    {
                    }

                }
            }

            await ReplyAsync(embed: _eBuilder.Build());

            _db.Dispose();
        }
    }
}
