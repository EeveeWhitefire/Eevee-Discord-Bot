using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;
using System.Linq.Expressions;

namespace EeveeBot.Modules
{
    [Name("Whitelist")]
    [Group(Defined.WHITELIST_TABLE_NAME)]
    [Alias("wl")]
    public class WhitelistCommands : ModuleBase<SocketCommandContext>
    {
        private Config_Json _config;
        private DatabaseContext _db;
        private Random _rnd;

        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;

        public WhitelistCommands(Config_Json cnfg, DatabaseContext db, Random rnd)
        {
            _config = cnfg;
            _db = db;
            _rnd = rnd;

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

        [Command("add")]
        [Alias("whiten")]
        public async Task WhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;
            if (u.Id == _config.Client_Id)
            {
                await ReplyAsync($"You can't whitelist {_config.Bot_Name} for itself smh");
                await Task.CompletedTask;
            }
            else
            {
                if (_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id)))
                {
                    if (!_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == u.Id)))
                    {
                        _db.AddEntity(Defined.WHITELIST_TABLE_NAME, new WhitelistUser(u.Id, _db.IsEmpty<WhitelistUser>((Defined.WHITELIST_TABLE_NAME))));
                        if (_db.Exists<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME, (x => x.Id == Context.User.Id)))
                            _db.DeleteEntity<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME, (x => x.Id == Context.User.Id));
                    }

                    await Defined.SendSuccessMessage(_eBuilder, Context, $"Successfully added the User **{u.Username}#{u.Discriminator}** to the Whitelist");
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "add a User to the Whitelist");
            }
        }

        [Command("delete")]
        [Alias("remove", "del", "rem")]
        public async Task DeWhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var caller = _db.FirstOrDefault<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id));
            var target = _db.FirstOrDefault<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == u.Id));

            if(caller != null)
            {
                if (target != null)
                {
                    if(target.Id == caller.Id || caller.IsOwner)
                    {
                        _db.DeleteEntity<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == u.Id));
                        await Defined.SendSuccessMessage(_eBuilder, Context, $"Successfully removed the User **{u.Username}#{u.Discriminator}** from the Whitelist");
                    }
                    else
                        await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E410, null, "remove a User who isn't yourself from the Whitelist");
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, $"{u.Username}#{u.Discriminator}", "User", "the Whitelist");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "remove a User from the Whitelist");
        }

        [Command("list")]
        [Alias("show", "showlist")]
        public async Task ShowWhitelistCommand()
        {
            var whitelist = _db.GetAll<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).OrderByDescending( x => x.IsOwner);
            
            _eBuilder.WithTitle("The Whitelist:");

            if(whitelist.Count() < 1)
            {
                _eBuilder.WithDescription("Empty");
            }
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
                                x.Name = $"__{guildUser.Nickname ?? guildUser.Username}__";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                        else
                        {
                            var user = Context.Client.GetUser(u.Id);
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"__{user.Username}#{user.Discriminator}__";
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
        }
    }

    [Name("Blacklist")]
    [Group(Defined.BLACKLIST_TABLE_NAME)]
    [Alias("bl")]
    public class BlacklistCommands : ModuleBase<SocketCommandContext>
    {
        private Config_Json _config;
        private DatabaseContext _db;
        private Random _rnd;

        private EmbedBuilder _eBuilder;
        private EmbedFooterBuilder _eFooter;

        public BlacklistCommands(Config_Json cnfg, DatabaseContext db, Random rnd)
        {
            _config = cnfg;
            _db = db;
            _rnd = rnd;

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

        [Command("add")]
        [Alias("blacken")]
        public async Task BlackenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;
            if (u.Id == _config.Client_Id)
            {
                await ReplyAsync($"You can't blacklist {_config.Bot_Name} for itself smh");
                await Task.CompletedTask;
            }
            else
            {
                if (_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id)))
                {
                    if (!_db.Exists<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME, (x => x.Id == u.Id)))
                    {
                        _db.AddEntity(Defined.BLACKLIST_TABLE_NAME, new BlacklistUser(u.Id));
                        if (_db.Exists<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == u.Id)))
                            _db.DeleteEntity<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == u.Id));
                    }
                    await Defined.SendSuccessMessage(_eBuilder, Context, $"Successfully added the User **{u.Username}#{u.Discriminator}** to the Blacklist");
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "add a User to the Blacklist");
            }
        }

        [Command("delete")]
        [Alias("remove", "del", "rem")]
        public async Task DeBlackenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var caller = _db.FirstOrDefault<WhitelistUser>(Defined.WHITELIST_TABLE_NAME, (x => x.Id == Context.User.Id));
            var target = _db.FirstOrDefault<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME, (x => x.Id == u.Id));

            if (caller != null)
            {
                if (target != null)
                {
                    _db.DeleteEntity<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME, (x => x.Id == u.Id));
                    await Defined.SendSuccessMessage(_eBuilder, Context, $"Successfully removed the User **{u.Username}#{u.Discriminator}** from the Blacklist");
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, $"{u.Username}#{u.Discriminator}", "User", "the Blacklist");
            }
            else
                await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "remove a User from the Blacklist");
        }

        [Command("list")]
        [Alias("show", "showlist")]
        public async Task ShowBlacklistCommand()
        {
            var blacklist = _db.GetAll<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME);

            _eBuilder.WithTitle("The Blacklist:");

            if (blacklist.Count() < 1)
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
                                x.Name = $"__{guildUser.Nickname ?? guildUser.Username}__";
                                x.Value = u.ToString();
                                x.IsInline = false;
                            });
                        }
                        else
                        {
                            var user = Context.Client.GetUser(u.Id);
                            _eBuilder.AddField(x =>
                            {
                                x.Name = $"__{user.Username}#{user.Discriminator}__";
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
        }
    }
}
