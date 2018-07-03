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
    [Name("Whitelist")]
    [Group("whitelist")]
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
                Text = Defined.COPYRIGHTS_MESSAGE
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

                var whitelist = _db.GetAll<Db_WhitelistUser>("whitelist");
                var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);

                if (currUser != null || whitelist.Count() < 1)
                {
                    var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                    if (tUser == null)
                    {
                        tUser = new Obj_WhitelistUser(u.Id, whitelist.Count() < 1).EncapsulateToDb();
                        _db.AddEntity("whitelist", tUser);
                    }

                    _db.DeleteEntity<Db_EeveeEmote>("whitelist", (x => x.Id == tUser.Id));
                    _eBuilder.WithTitle("Success")
                        .WithDescription($"Successfully added the User **{u.Username}#{u.Discriminator}** to the Whitelist")
                        .WithThumbnailUrl(Defined.SUCCESS_THUMBNAIL);

                    await ReplyAsync(string.Empty, embed: _eBuilder.Build());
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E409, null, "add a User to the Whitelist");
            }
        }

        [Command("del")]
        [Alias("remove")]
        public async Task DeWhitenUserCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetAll<Db_WhitelistUser>("whitelist");
            var owner = whitelist.FirstOrDefault(x => x.IsOwner);

            if (owner != null)
            {
                var tUser = whitelist.FirstOrDefault(x => x.Id == u.Id);
                if (tUser != null)
                {
                    if (Context.User.Id == tUser.Id || Context.User.Id == owner.Id)
                    {
                        _db.DeleteEntity<Db_EeveeEmote>("whitelist", (x => x.Id == tUser.Id));
                        _eBuilder.WithTitle("Success")
                            .WithDescription($"Successfully removed the User **{u.Username}#{u.Discriminator}** from the Whitelist")
                            .WithThumbnailUrl(Defined.SUCCESS_THUMBNAIL);

                        await ReplyAsync(string.Empty, embed: _eBuilder.Build());
                    }
                    else
                    {
                        await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E410, null, "remove a User from the Whitelist");
                    }
                }
                else
                    await Defined.SendErrorMessage(_eBuilder, Context, ErrorTypes.E404, $"{u.Username}#{u.Discriminator}", "User", "Whitelist");
            }
        }

        [Command("list")]
        [Alias("show", "showlist")]
        public async Task ShowWhitelistCommand()
        {
            var whitelist = _db.GetAll<Db_WhitelistUser>("whitelist").OrderByDescending( x => x.IsOwner);
            
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
}
