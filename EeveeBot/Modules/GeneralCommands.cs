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

namespace EeveeBot.Modules
{
    [Summary("General commands like say, send, uinfo")]
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private DatabaseContext _db;
        private Random _rnd;

        public GeneralCommands(DatabaseContext db, Random rnd)
        {
            _db = db;
            _rnd = rnd;

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)]
            };
        }

        [Command("uinfo")]
        [Alias("userinfo")]
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

        [Command("invite")]
        [Alias("invitelink", "inv", "invlink")]
        public async Task SendInvitationLinkCommand()
        {
            await ReplyAsync(@"https://discordapp.com/oauth2/authorize?client_id=337649506856468491&scope=bot");
        }

        [Command("say")]
        [Alias("send")]
        public async Task SendText(SocketTextChannel channel, [Remainder] string text)
        {
            await channel.SendMessageAsync(text);
        }
        [Command("say")]
        public async Task SendText([Remainder] string text)
        {
            await ReplyAsync(text);
        }


        [Command("mcavatar")]
        public async Task SendMinecraftSkin(string username)
        {
            string url = $@"https://visage.surgeplay.com/full/512/{username}";
            _eBuilder.WithTitle($"{username}'s Avatar:")
                .WithUrl(url)
                .WithImageUrl(url);
            

            await ReplyAsync("", embed: _eBuilder.Build());
        }
    }
}
