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

namespace EeveeBot.Modules
{
    public class RestrictionCommands : ModuleBase<SocketCommandContext>
    {
        private Config_Json _config;
        private DatabaseContext _db;
        private Color[] _pallete = Defined.Colors;

        private EmbedBuilder _eBuilder;

        public RestrictionCommands(Config_Json cnfg, DatabaseContext db)
        {
            _config = cnfg;
            _db = db;

            _eBuilder = new EmbedBuilder()
            {
                Color = _pallete[new Random().Next(0, _pallete.Length)]
            };
        }

        [Command("chown")]
        public async Task ChangeOwnerCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);

            if (currUser != null || whitelist.FirstOrDefault(x => x.IsOwner) == null)
            {
                if(whitelist.FirstOrDefault(x => x.IsOwner) == null)
                {
                    var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                    if (tUser == null)
                    {
                        tUser = new Obj_WhitelistUser(u.Id, true).EncapsulateToDb();
                        _db.GetCollection<Db_WhitelistUser>("whitelist").Insert(tUser);
                    }
                    else
                    {
                        tUser.IsOwner = true;
                        _db.GetCollection<Db_WhitelistUser>("whitelist").Update(tUser);
                    }
                }
                else if (whitelist.FirstOrDefault(x => x.IsOwner).Id == Context.User.Id)
                {
                    if (u.Id != Context.User.Id)
                    {
                        currUser.IsOwner = false;

                        var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                        if (tUser == null)
                        {
                            tUser = new Obj_WhitelistUser(u.Id, true).EncapsulateToDb();
                            _db.GetCollection<Db_WhitelistUser>("whitelist").Insert(tUser);
                        }
                        else
                        {
                            tUser.IsOwner = true;
                            _db.GetCollection<Db_WhitelistUser>("whitelist").Update(tUser);
                        }

                        _db.GetCollection<Db_WhitelistUser>("whitelist").Update(currUser);
                    }

                    await ReplyAsync("Success");
                }
                else
                {
                    await ReplyAsync("You cannot change the owner without having root privileges!");
                }
            }
            else
            {
                await ReplyAsync("You cannot change the owner without having whitelist privileges!");
            }
        }

    }

    [Group("whitelist")]
    [Alias("wl")]
    public class WhitelistCommands : ModuleBase<SocketCommandContext>
    {
        private Config_Json _config;
        private DatabaseContext _db;
        private Color[] _pallete = Defined.Colors;

        private EmbedBuilder _eBuilder;

        const string OwnerIcon = ":crown:";
        const string WhitelistedIcon = ":white_check_mark:";

        public WhitelistCommands(Config_Json cnfg, DatabaseContext db)
        {
            _config = cnfg;
            _db = db;

            _eBuilder = new EmbedBuilder()
            {
                Color = _pallete[new Random().Next(0, _pallete.Length)]
            };
        }

        [Command("add")]
        [Alias("whiten")]
        public async Task WhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);

            if(currUser != null || whitelist.Count() < 1)
            {
                var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                if (tUser == null)
                {
                    tUser = new Obj_WhitelistUser(u.Id, whitelist.Count() < 1).EncapsulateToDb();
                    _db.GetCollection<Db_WhitelistUser>("whitelist").Insert(tUser);
                }

                await ReplyAsync("Success");
            }
            else
            {
                await ReplyAsync("Only Whitelisted users can do that!");
            }
        }

        [Command("del")]
        [Alias("remove")]
        public async Task DeWhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);

            if (currUser != null)
            {
                var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                if (tUser != null)
                {
                    if (currUser.Id == tUser.Id || currUser.IsOwner)
                    {
                        _db.GetCollection<Db_WhitelistUser>("whitelist").Delete(x => x.Id == tUser.Id);
                        await ReplyAsync("Success");
                    }
                    else
                    {
                        await ReplyAsync("Only the Owner can dewhiten other members");
                    }
                }
                else
                    await ReplyAsync("Success");
            }
            else
            {
                await ReplyAsync("Only Whitelisted users can do that!");
            }
        }

        [Command("list")]
        [Alias("show", "showlist")]
        public async Task ShowWhitelistCommand()
        {
            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll().OrderByDescending( x => x.IsOwner);
            
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
                        var user = Context.Guild.Users.FirstOrDefault(x => x.Id == u.Id);
                        _eBuilder.AddField($"__{user.Nickname ?? user.Username}__", 
                            $"{u.Id.ToString()} {(u.IsOwner ? OwnerIcon : WhitelistedIcon)}", false);
                    }
                    catch (Exception)
                    {
                        var user = Context.Client.GetUser(u.Id);
                        _eBuilder.AddField($"__{user.Username}#{user.Discriminator}__",
                            $"{u.Id.ToString()} {(u.IsOwner ? OwnerIcon : WhitelistedIcon)}", false);
                    }

                }
            }

            await ReplyAsync(embed: _eBuilder.Build());
        }
    }
}
