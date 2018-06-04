using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Scripting;

using EeveeBot.Classes;
using EeveeBot.Classes.Database;
using EeveeBot.Classes.Services;
using EeveeBot.Classes.Json;

namespace EeveeBot.Modules
{
    [Name("Utility")]
    [Summary("Utility commands like purge or memory.")]
    public class UtilityCommands : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder _eBuilder;
        private Color[] _pallete = Defined.Colors;

        private Config_Json _config;
        private DatabaseContext _db;
        private JsonManager_Service _jsonMngr;

        private Stopwatch _exesw;
        private Stopwatch _compilsw;

        public UtilityCommands(Config_Json config, DatabaseContext db, JsonManager_Service jsonParser)
        {
            _config = config;
            _db = db;
            _jsonMngr = jsonParser;

            _eBuilder = new EmbedBuilder()
            {
                Color = _pallete[new Random().Next(0, _pallete.Length)]
            };
        }

        [Command("ping")]
        public async Task SendPing()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            var msg = await ReplyAsync("Calculating ping");
            s.Stop();
            await msg.ModifyAsync(x => x.Content = $"Response time: **{s.ElapsedMilliseconds}ms**\n" +
            $"Latency: **{Context.Client.Latency}ms**");
        }

        [Command("chown")]
        [Remarks("Owner Only")]
        public async Task ChangeOwnerCommand([Remainder] SocketGuildUser u = null)
        {
            u = u ?? (SocketGuildUser)Context.User;

            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            var currUser = whitelist.FirstOrDefault(x => x.Id == Context.User.Id);

            if (currUser != null || whitelist.FirstOrDefault(x => x.IsOwner) == null)
            {
                if (whitelist.FirstOrDefault(x => x.IsOwner) == null)
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

        public class EvalGlobals
        {
            public SocketCommandContext Context { get; set; }
            public Config_Json _config { get; set; }
            public DatabaseContext _db { get; set; }
        }

        public async Task EvaluateCSharp(string code)
        {
            try
            {
                var globals = new EvalGlobals { Context = Context, _config = _config, _db = _db };

                code = code.Replace("ReplyAsync", "Context.Channel.SendMessageAsync");

                var scriptOptions = ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
                    .WithImports("System", "System.Math", "Discord.Commands", "Discord.Net", "Discord.WebSocket",
                    "System.Diagnostics", "System.Threading.Tasks", "System.Reflection", "System.Net", "System.Net.Http", "System.Net.Http.Headers",
                    "System.Text", "System.Collections.Generic", "System.Linq", "System.Text.RegularExpressions", "System.IO");

                var script = (CSharpScript.Create(code, scriptOptions, globalsType: typeof(EvalGlobals)));
                _compilsw.Start();
                //counting compilation time
                script.Compile();

                _compilsw.Stop();

                _exesw.Start();
                //counting execution time
                var output = (await script.RunAsync(globals: globals)).ReturnValue.ToString();

                _exesw.Stop();

                output = output ?? "No output";

                MakeEvaluationEmbed("C#", output, _exesw.ElapsedMilliseconds, _compilsw.ElapsedMilliseconds, true);
            }
            catch (Exception e)
            {
                var output = e.Message;
                MakeEvaluationEmbed("C#", output, 0, _compilsw.ElapsedMilliseconds, true);
            }
        }
        public async Task EvaluateVisualBasic(string code)
        {
            try
            {
                await Task.CompletedTask;
                var globals = new EvalGlobals { Context = Context, _config = _config, _db = _db };

                var scriptOptions = ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
                    .WithImports("System", "System.Math", "DSharpPlus.CommandsNext", "DSharpPlus", "DSharpPlus.Entities",
                    "System.Diagnostics", "System.Threading.Tasks", "System.Reflection", "System.Net", "System.Net.Http", "System.Net.Http.Headers",
                    "System.Text", "System.Collections.Generic", "System.Linq", "System.Text.RegularExpressions", "System.IO");
                //VisualBasicCompilation.Create()
                _compilsw.Start();

                _compilsw.Stop();

                _exesw.Start();
                //counting execution time
                _exesw.Stop();

                //output = output ?? "No output";

                //MakeEvaluationEmbed("VB", output, _exesw.ElapsedMilliseconds, _compilsw.ElapsedMilliseconds, true);
            }
            catch (Exception e)
            {
                var output = e.Message;
                MakeEvaluationEmbed("VB", output, 0, _compilsw.ElapsedMilliseconds, true);
            }
        }

        IList<Language_Json> GetLanguages()
        {
            return new List<Language_Json>()
                    {
                        new Language_Json
                        {
                            Name = "F#",
                            RequiresCompilation = true,
                            Shortcuts = new string[] {"fs", "f#"},
                            EmulatorPath = @"C:\Program Files (x86)\Microsoft SDKs\F#\4.1\Framework\v4.0\fsc.exe",
                            Extension = "fs"
                        }, //f#
                        new Language_Json
                        {
                            Name = "Python",
                            Shortcuts = new string[] {"py"},
                            EmulatorPath = @"I:\Python37\python.exe",
                            Extension = "py"
                        }, //py
                    };
        }

        void MakeEvaluationEmbed(string lng, string output, long exe, long comp, bool success)
        {
            _eBuilder.WithTitle($"{lng} | {(success ? "Evaluation Successful!" : "Evaluation Failed")}")
                .WithDescription($"```{output}```")
                .WithFooter($"Execution Time: {exe}ms | Compilation Time: {comp}ms");
        }

        public async Task EvaluateLanguage(string lng, string code)
        {
            Process process = new Process();
            ProcessStartInfo startInfo;

            LanguagesConfig_Json lngs;
            string lngsConfigPath = $"{_config.Project_Path}\\lngs-config.json";
            if (!File.Exists(lngsConfigPath))
            {
                lngs = new LanguagesConfig_Json()
                {
                    Languages = GetLanguages()
                };
                await _jsonMngr.UpdateJsonAsync(lngs, lngsConfigPath);
            }
            else
            {
                lngs = await _jsonMngr.GetJsonObjectAsync<LanguagesConfig_Json>(lngsConfigPath);
            }

            if (lng == "cs" || lng == "c#")
                await EvaluateCSharp(code);
            /*else if (lng == "vb")
                await EvaluateVB(code, Context);*/
            else if (lngs.Languages.Count(x => x.Shortcuts.Contains(lng)) > 0)
            {
                var lang = lngs.Languages.FirstOrDefault(x => x.Shortcuts.Contains(lng));

                int currId = Directory.GetFiles("Code Evaluation").Length;

                string scriptPath = $@"{_config.Project_Path}\Code Evaluation\Scripts\ev{lang.Extension}{currId}.{lang.Extension}";
                string compiledPath = $@"{_config.Project_Path}\Code Evaluation\Compiled\ev{lang.Extension}{currId}.exe";

                using (StreamWriter writer = new StreamWriter(scriptPath))
                {
                    await writer.WriteAsync(code);
                }

                startInfo = new ProcessStartInfo()
                {
                    FileName = lang.EmulatorPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = $"\"{scriptPath}\""
                };
                process.StartInfo = startInfo;

                _compilsw.Start();
                process.Start();

                var errors = await process.StandardError.ReadToEndAsync();
                var output = await process.StandardOutput.ReadToEndAsync();

                process.WaitForExit();
                _compilsw.Stop();
                process.Close();

                if (errors == string.Empty)
                {
                    if (lang.RequiresCompilation)
                    {
                        startInfo.FileName = compiledPath;
                        _exesw.Start();
                        process.Start();
                        output = await process.StandardOutput.ReadToEndAsync();
                        _exesw.Stop();
                        process.WaitForExit();
                        process.Close();

                        MakeEvaluationEmbed(lang.Name, output, _exesw.ElapsedMilliseconds, _compilsw.ElapsedMilliseconds, true);

                        try
                        {
                            File.Delete(compiledPath);
                        }
                        catch (Exception)
                        { }
                    }
                    else
                    {
                        MakeEvaluationEmbed(lang.Name, output, _compilsw.ElapsedMilliseconds, _compilsw.ElapsedMilliseconds, true);
                    }
                }
                else
                {
                    MakeEvaluationEmbed(lang.Name, errors, 0, _compilsw.ElapsedMilliseconds, false);
                }
                File.Delete(scriptPath);
            }
            else
                _eBuilder.WithTitle("Error")
                    .WithDescription($"The bot doesn't support the {lng} Language, if it even is one :frowning:")
                    .WithFooter("git gud");

            process.Dispose();
        }

        [Command("eval")]
        [Summary("This command compiles and executes given code as input.")]
        [Remarks("Whitelisted Users Only")]
        public async Task Eval([Remainder] string code)
        {
            if (_db.GetCollection<Db_WhitelistUser>("whitelist").Count(x => x.Id == Context.User.Id) > 0)
            {
                _exesw = new Stopwatch();
                _compilsw = new Stopwatch();
                string lng = string.Empty;

                if (!code.StartsWith('`'))
                {
                    await ReplyAsync("No code block found! C# Assumed");
                    lng = "cs";

                }
                else
                {
                    if (!Directory.Exists("Code Evaluation"))
                        Directory.CreateDirectory("Code Evaluation");

                    code = code.TrimStart(' ').TrimEnd(' ')
                        .TrimStart('`').TrimEnd('`');

                    lng = string.Join(string.Empty, code.Take(3)).ToLower()
                        .TrimEnd(' ').TrimStart(' ')
                        .TrimEnd('\n').TrimStart('\n'); ;

                    code = "`" + code;
                    code = code.Replace("`" + lng, string.Empty)
                        .TrimEnd('\n').TrimStart('\n');
                }

                await EvaluateLanguage(lng, code);
                await ReplyAsync(embed: _eBuilder.Build());
            }
            else
                await ReplyAsync("Only Whitelisted users can use that!");
        }

        
        [Command("clean")]
        [Alias("purge")]
        [Summary("Removes all or a number of messages that the user (this bot = def) has sent!")]
        public async Task CleanMessagesCommand(int toDelete = 50)
        {
            var messages = await Context.Channel.GetMessagesAsync(toDelete*2).FlattenAsync();
            var msgsOfUser = messages.Where(x => x.Author.Id == _config.Client_Id && (x.CreatedAt.Date - DateTime.Now).TotalDays < 15).Take(toDelete);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(msgsOfUser);
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }

        [Command("clean")]
        public async Task PurgeMessagesCommand(SocketGuildUser user, int toDelete = 50)
        {
            var messages = await Context.Channel.GetMessagesAsync(toDelete * 2).FlattenAsync();
            var msgsOfUser = messages.Where(x => x.Author.Id == user.Id && (x.CreatedAt.Date - DateTime.UtcNow).TotalDays < 15).Take(toDelete);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(msgsOfUser);
            await ReplyAsync($"{((SocketGuildUser)(Context.User)).Mention} I have succesfully deleted {msgsOfUser.Count()} messages from this channel!");
        }

        [Command("memory")]
        [Summary("Gets the number of bytes that the Bot's process has allocated.")]
        public async Task GetMemoryStateCommand()
        {
            string privateMemory;
            using (Process proc = Process.GetCurrentProcess())
            {
                privateMemory = $"{Math.Round(proc.PrivateMemorySize64 / Math.Pow(10,6), 2)}MB";
            }

            _eBuilder.AddField("__Private Memory Allocated__", privateMemory);

            await ReplyAsync("", embed: _eBuilder.Build());
        }
        
        [Command("relaunch")]
        [Alias("rel", "reset", "restart", "reboot")]
        [Summary("Restarts the bot!")]
        [Remarks("Whitelisted Users Only")]
        public async Task RelaunchBotCommand()
        {
            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();
            DirectoryInfo dInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var path = dInfo.Parent.FullName + "\\Executions\\";

            if (whitelist.FirstOrDefault(x => x.IsOwner)?.Id == (Context.User).Id)
            {
                ProcessStartInfo ProcessInfo = new ProcessStartInfo($"{path}Run {_config.Bot_Name}.bat")
                {
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                await Task.Run(() => Process.Start(ProcessInfo));
                Environment.Exit(1);
            }
            else
            {
                _eBuilder.WithTitle("ERROR")
                    .WithDescription("You don't have the required permission to relaunch the Bot!");
            }
        }

        [Command("sudo")]
        [Summary("You know what it means...")]
        [Remarks("Owner Only")]
        public async Task SudoCommand(SocketGuildUser user, [Remainder] string args)
        {
            var whitelist = _db.GetCollection<Db_WhitelistUser>("whitelist").FindAll();

            if (whitelist.FirstOrDefault(x => x.IsOwner)?.Id == (Context.User).Id)
            {
                await Task.CompletedTask;
            }
            else
            {
                _eBuilder.WithTitle("ERROR")
                    .WithDescription("You don't have the required permission to use sudo!");
            }
        }
    }
}
