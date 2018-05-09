using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using ImageSharp;

using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;
using EeveeBot.Classes.Services;
using System.Reflection;

namespace EeveeBot.Modules
{
    [Summary("Mostly weird commands but whatever suits your fancy")]
    public class FunCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private DatabaseContext _db;
        private Random _rnd;
        private JsonManager_Service _jsonMngr;

        public FunCommands(DatabaseContext db, Random rnd, JsonManager_Service jsonM)
        {
            _db = db;
            _rnd = rnd;
            _jsonMngr = jsonM;

            _eBuilder = new EmbedBuilder
            {
                Color = Defined.Colors[_rnd.Next(Defined.Colors.Length - 1)]
            };
        }

        [Command("ud")]
        [Alias("urbandictionary")]
        [Summary("Sends the meaning of the given input from Urban Dictionary!")]
        public async Task UrbanDictionaryCommand(string input, int num = 1)
        {
            Uri url = new Uri($"http://api.urbandictionary.com/v0/define?term={input}");
            var data = await _jsonMngr.GetJsonObjectAsync<UrbanDictionary_Json.Output>(url);

            try
            {
                _eBuilder.WithTitle(input)
                    .WithDescription(string.Join("\n", data.list.Take(num).Select(x => $"**{data.list.IndexOf(x) + 1}.** {x.definition}\n**Example:**\n{x.example}\n")))
                    .WithUrl($"http://www.urbandictionary.com/define.php?term={input}");

                await ReplyAsync(embed: _eBuilder.Build());
            }
            catch (Exception)
            {
                await ReplyAsync("Definition is too long for Embed text length!");
            }
        }
    }

    [Group("Emotes")]
    [Alias("em", "ems", "emote")]
    [Summary("The Emote System")]
    public class EmoteCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private DatabaseContext _db;
        private Random _rnd;
        private JsonManager_Service _jsonMngr;
        private Config_Json _config;
        private const int NicknameCountMax = 2;
        private const int NicknameLengthMin = 2;

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
            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll();
            if (emotes.Where(x => x.Id == em?.Id).Count() > 0)
            {
                await ReplyAsync("Error : The emote is already registered!");
            }
            else
            {
                Obj_Emote nEmote = new Obj_Emote(em.Id, em.Name, em.Animated, em.Url);
                var definitions = emotes.SelectMany(x => x.Nicknames).Where(x => x.OwnerId == Context.User.Id);

                if (inNick != null && !definitions.Select(x => x.Nickname.ToLower()).Contains(inNick.ToLower()))
                {
                    if (inNick.Length < NicknameLengthMin)
                    {
                        await ReplyAsync($"Error : A nickname must be at least {NicknameLengthMin} chars long!");
                    }
                    else
                    {
                        nEmote.Nicknames.Add(new UserDefined()
                        {
                            AssociatedEmoteId = nEmote.Id,
                            Nickname = inNick,
                            OwnerId = Context.User.Id
                        });
                    }
                }
                using (WebClient webC = new WebClient())
                {
                    string fileName = $"Emotes\\{nEmote.Id}{(em.Animated ? ".gif" : ".png")}";
                    if (!File.Exists($"Emotes\\{nEmote.Id}{(em.Animated ? ".gif" : ".png")}"))
                        webC.DownloadFileAsync(new Uri(em.Url), fileName);
                }

                _db.GetCollection<Db_Emote>("emotes").Insert(nEmote.EncapsulateToDb());

                _eBuilder.WithTitle(em.Name)
                    .WithImageUrl(em.Url);

                await ReplyAsync(embed: _eBuilder.Build());
            }
        }

        [Command("add")]
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
                    await ReplyAsync($"Error! Emote {emoteCode} wasn't found!");
            }
            catch (Exception)
            {
                await ReplyAsync($"Error! Emote {emoteCode} wasn't found!");
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
                    await ReplyAsync($"Error! Emote associated with the ID {emoteId} wasn't found!");
            }
            catch (Exception)
            {
                await ReplyAsync($"Error! Emote associated with the ID {emoteId} wasn't found!");
            }
        }
        

        private async Task ResizeEmoteAndSendAsync(Db_Emote emote)
        {
            int size = 36;
            var assembly = Assembly.GetExecutingAssembly();
            FileInfo fInfo = new FileInfo(Directory.GetCurrentDirectory() + "\\" + emote.RelativePath);
            Stream resource = assembly.GetManifestResourceStream($"EeveeBot.Emotes.{fInfo.Name}");

            Image<Rgba32> img = ImageSharp.Image.Load(resource);
            resource.Dispose();

            int width = img.Width, height = img.Height;

            if(height > width)
            {
                size = size > height ? height : size;

                width = Convert.ToInt16(width * (double)(size / height));
                height = size;
            }
            else
            {
                size = size > width ? width : size;
                height = Convert.ToInt16(height * (double)(size / width));
                width = size;
            }

            if(!Directory.Exists("Temp"))
                Directory.CreateDirectory("Temp");

            string newPath = $"Temp\\{width}x{height}-{fInfo.Name}";

            if (!File.Exists(newPath))
            {
                using (_ = File.Create(newPath))
                { }
                img = img.Resize(width, height);
                img.Save(newPath);
            }
            img.Dispose();

            await Context.Channel.SendFileAsync(newPath);
            File.Delete(newPath);
        }

        [Command("send")]
        [Alias("show")]
        public async Task SendEmoteCommand([Remainder] string name)
        {
            name = name.Replace(":", string.Empty).Replace(" ", string.Empty);

            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll();
            var definitions = emotes.SelectMany(x => x.Nicknames).Where(x => x.OwnerId == Context.User.Id);
            var emote = emotes
                .FirstOrDefault(x => x.Name.Replace(" ", string.Empty).ToLower() == name.ToLower() || x.Nicknames.Count(y => y.Nickname.Replace(" ", string.Empty).ToLower() == name.ToLower()) > 0);
            if(emote != null)
            {
                try
                {
                    await ResizeEmoteAndSendAsync(emote);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                await ReplyAsync("Error : The emote doesn't exist!");
            }
        }

        [Command("info")]
        public async Task SendEmoteInfo([Remainder] string name)
        {
            name = name.Replace(":", string.Empty).Replace(" ", string.Empty);

            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll();
            var definitions = emotes.SelectMany(x => x.Nicknames).Where(x => x.OwnerId == Context.User.Id);
            var emote = emotes
                .FirstOrDefault(x => x.Name.Replace(" ", string.Empty).ToLower() == name.ToLower() || x.Nicknames.Count(y => y.Nickname.Replace(" ", string.Empty).ToLower() == name.ToLower()) > 0);
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
                        x.Value = emote.Nicknames.Count > 0 ? string.Join("\n", emote.Nicknames.Select( y => y.Nickname)) : "None.";
                        x.IsInline = false;
                    })
                    .WithUrl(emote.Url)
                    .WithThumbnailUrl(emote.Url);

                await ReplyAsync("", embed: _eBuilder.Build());
            }
            else
            {
                await ReplyAsync("Error : The emote doesn't exist!");
            }
        }

        [Command("rename")]
        [Alias("nick", "ren")]
        public async Task RenameEmoteCommand(string name, [Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty);
            name = name.Replace(":", string.Empty);

            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll();
            var definitions = emotes.SelectMany(x => x.Nicknames).Where(x => x.OwnerId == Context.User.Id);
            var emote = emotes.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());

            if (nick.Length < NicknameLengthMin)
            {
                await ReplyAsync($"Error : A nickname must be at least {NicknameLengthMin} chars long!");
            }
            else
            {

                if (emote != null)
                {
                    if (emotes.Select(x => x.Name).Count(x => x.ToLower() == nick.ToLower()) < 1
                        && !definitions.Select(x => x.Nickname.ToLower()).Contains(nick.ToLower()))
                    {
                        if (emote.Nicknames.Count(x => x.OwnerId == Context.User.Id) > NicknameCountMax)
                        {
                            await ReplyAsync("Error : You have exceeded the Nicknames capacity limit for this emote!" +
                                $"\nIf you'd really like to add this one, please use {_config.Prefixes[0] }emotes delnick <nick> on " +
                                $"one of the following:\n{string.Join("\n", emote.Nicknames.Where(x => x.OwnerId == Context.User.Id).Select(x => x.Nickname))}");
                        }
                        else
                        {
                            emote.Nicknames.Add(new UserDefined()
                            {
                                AssociatedEmoteId = emote.Id,
                                Nickname = nick,
                                OwnerId = Context.User.Id
                            });

                            _db.GetCollection<Db_Emote>("emotes").Update(emote);

                            await ReplyAsync("Success");
                        }
                    }
                }
                else
                {
                    emote = emotes.FirstOrDefault(x => definitions.Where(y => y.AssociatedEmoteId == x.Id)
                   .Count(y => y.Nickname.ToLower() == name.ToLower()) > 0);

                    if (emote != null)
                    {
                        if (!definitions.Select(x => x.Nickname.ToLower()).Contains(nick.ToLower())
                            && emotes.Select(x => x.Name).Count(x => x.ToLower() == nick.ToLower()) < 1)
                        {
                            if (emote.Nicknames.Count(x => x.OwnerId == Context.User.Id) > 3)
                            {
                                await ReplyAsync("Error : You have exceeded the Nicknames capacity limit for this emote!" +
                                    $"\nIf you'd really like to add this one, please use {_config.Prefixes[0] }emotes delnick <nick> on " +
                                    $"one of the following:\n{string.Join("\n", emote.Nicknames.Where(x => x.OwnerId == Context.User.Id).Select(x => x.Nickname))}");
                            }
                            else
                            {
                                emote.Nicknames.Add(new UserDefined()
                                {
                                    AssociatedEmoteId = emote.Id,
                                    Nickname = nick,
                                    OwnerId = Context.User.Id
                                });

                                _db.GetCollection<Db_Emote>("emotes").Update(emote);
                                await ReplyAsync("Success");
                            }
                        }
                    }
                    else
                    {
                        await ReplyAsync("Error : The emote doesn't exist!");
                    }
                }
            }
        }

        [Command("deleterename")]
        [Alias("deletenick", "deleteren", "delrename", "delnick", "delren")]
        public async Task DeleteEmoteNicknameCommand([Remainder] string nick)
        {
            nick = nick.Replace(":", string.Empty);

            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll();
            var definition = emotes.SelectMany(x => x.Nicknames).Where(x => x.OwnerId == Context.User.Id)
                .FirstOrDefault( x => x.Nickname.ToLower() == nick.ToLower());

            if (definition != null)
            {
                var emote = emotes.FirstOrDefault(x => x.Id == definition.AssociatedEmoteId);
                emote.Nicknames.RemoveAll(x => x.Nickname == definition.Nickname);

                _db.GetCollection<Db_Emote>("emotes").Update(emote);
                await ReplyAsync("Success");
            }
        }

        [Command("list")]
        [Alias("all")]
        public async Task SendEmoteListCommand(int page = 1)
        {
            const int MaxEmotesPerPage = 9;
            var emotes = _db.GetCollection<Db_Emote>("emotes").FindAll().OrderByDescending(x => string.Join("", x.Nicknames).Length)
                .OrderByDescending(x => x.IsAnimated);
            int NumOfEmotes = emotes.Count();
            int NumOfPages = NumOfEmotes / MaxEmotesPerPage + (NumOfEmotes % MaxEmotesPerPage);
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

            foreach (Db_Emote em in pagedEmotes)
            {
                var userDefined = em.Nicknames.Where(x => x.OwnerId == Context.User.Id);
                _eBuilder.AddField($"__{em.Name}__" +
                    $"{(userDefined.Count() > 0 ? " : " + string.Join(", ", userDefined.Select(x => x.Nickname)) : string.Empty)}", 
                    em.Id.ToString() + (em.IsAnimated ? "\nAnimated" : string.Empty), true);
            }

            await ReplyAsync(text, embed: _eBuilder.Build());
        }
    }
}