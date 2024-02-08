using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using Discord.Commands;
using Discord;

using EeveeBot.Classes.Database;
using EeveeBot.Classes.Json;

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace EeveeBot.Classes
{
    class LanguageEmulator
    {
        private static Stopwatch _exesw = new Stopwatch();
        private static Stopwatch _compilsw = new Stopwatch();

        public static async Task EvaluateAsync(EmbedBuilder eBuilder, Languages lng, string input, 
            SocketCommandContext Context, DatabaseRepository db, Config_Json config)
        {
            switch (lng)
            {
                case Languages.CS:
                    try
                    {
                        var globals = new EmulatorGlobalVariables { Context = Context, _config = config, _db = db };

                        input = input.Replace("ReplyAsync", "Context.Channel.SendMessageAsync");

                        var scriptOptions = ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
                            .WithImports("System", "System.Math", "Discord.Commands", "Discord.Net", "Discord.WebSocket",
                            "System.Diagnostics", "System.Threading.Tasks", "System.Reflection", "System.Net", "System.Net.Http", "System.Net.Http.Headers",
                            "System.Text", "System.Collections.Generic", "System.Linq", "System.Text.RegularExpressions", "System.IO", "EeveeBot.Classes.Database", "EeveeBot.Defined");

                        var script = (CSharpScript.Create(input, scriptOptions, globalsType: typeof(EmulatorGlobalVariables)));
                        _compilsw.Start();
                        //counting compilation time
                        script.Compile();

                        _compilsw.Stop();

                        _exesw.Start();
                        //counting execution time
                        var output = (await script.RunAsync(globals: globals)).ReturnValue.ToString();

                        _exesw.Stop();

                        output = output ?? "No output";

                        MakeEmulationEmbed(eBuilder, "C#", output, _exesw.ElapsedMilliseconds, _compilsw.ElapsedMilliseconds, true);
                    }
                    catch (Exception e)
                    {
                        var output = e.Message;
                        MakeEmulationEmbed(eBuilder, "C#", output, 0, _compilsw.ElapsedMilliseconds, true);
                    }
                    break;
                case Languages.C:
                    break;
                case Languages.CPP:
                    break;
                case Languages.PY:
                    break;
                case Languages.FS:
                    break;
                default:
                    break;
            }

            _compilsw.Reset();
            _exesw.Reset();
        }


        private static void MakeEmulationEmbed(EmbedBuilder eBuilder, string lng, string output, long exe, long comp, bool success)
        {
            eBuilder.WithTitle($"{lng} | {(success ? "Evaluation Successful!" : "Evaluation Failed")}")
                .WithDescription($"```{output}```")
                .WithFooter($"Execution Time: {exe}ms | Compilation Time: {comp}ms");
        }
    }

    public class EmulatorGlobalVariables
    {
        public SocketCommandContext Context { get; set; }
        public Config_Json _config { get; set; }
        public DatabaseRepository _db { get; set; }
    }

    public enum Languages
    {
        CS,
        C,
        CPP,
        PY,
        FS
    }
}
