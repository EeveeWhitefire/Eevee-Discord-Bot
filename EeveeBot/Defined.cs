using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;

using EeveeBot.Classes.Database;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace EeveeBot
{
    public class EeveeEmbed : EmbedBuilder
    {
        public EeveeEmbed()
        {
            Color = Defined.Colors.Random();
            Footer = new EmbedFooterBuilder()
            {
                IconUrl = Defined.COPYRIGHTS_THUMBNAIL,
                Text = Defined.FOOTER_MESSAGE
            };
        }
    }

    /// <summary>
    /// Error types (IDs)
    /// </summary>
    public enum ErrorTypes
    {
        E404 = 404, ///not found
        E405, ///already exists
        E406, ///invalid length - min
        E407, ///invalid length - max
        E408, ///invalid length - free text
        E409, //no whitelisted privilege
        E410, //no owner privilege
        E411, //can't do that on bots
        E412  //all emote guilds ran out of space for new emotes
    }

    public class Defined
    {
        //order for module fields
        // embed builder
        // command service
        // config
        // database
        // json wrapper
        // bot logic

        public static Color[] Colors = typeof(Color).GetFields().Where(x => x.FieldType == typeof(Color))
            .Select( x => (Color)x.GetValue(null)).ToArray();

        public const string OWNER_ICON = ":crown:";
        public const string WHITELISTED_ICON = ":white_check_mark:";
        public const string ERROR_THUMBNAIL = @"https://cdn2.iconfinder.com/data/icons/color-svg-vector-icons-2/512/error_warning_alert_attention-512.png";
        public const string SUCCESS_THUMBNAIL = @"https://cdn2.iconfinder.com/data/icons/perfect-flat-icons-2/512/Ok_check_yes_tick_accept_success_green_correct.png";
        public const int MAX_EMOTES_IN_GUILD = 50;
        public const string EMOTE_SEPARATOR_PREFIX = ";";
        public const int COMMANDS_COOLDOWN_SECONDS = 1;
        public const string COOKIE_THUMBNAIL = @"http://static1.squarespace.com/static/57b5da73b3db2b7747f9c3a4/t/58916aaaebbd1ade326d74f2/1510756771216/";
        public const string COPYRIGHTS_THUMBNAIL = @"https://cdn.discordapp.com/attachments/299710358875275265/700413705544007690/285a009b994468891c3d76d48b2df5cb.jpg";
        public const string FOOTER_MESSAGE = "EeveeBot made by TalH#6144. Licensed under MIT";
        public const string COPYRIGHTS_MESSAGE = "Copyright © 2018 Tal Hadad. Licensed under MIT";
        public const string DATE_THUMBNAIL = @"https://cdn2.iconfinder.com/data/icons/business-flatcircle/512/calendar-512.png";
        public const ulong CLIENT_ID = 337649506856468491;
        public static string INVITE_URL(ulong client_id)
            => $@"https://discordapp.com/oauth2/authorize?client_id={client_id}&scope=bot";

        public const string WHITELIST_TABLE_NAME = "whitelist";
        public const string BLACKLIST_TABLE_NAME = "blacklist";
        public const string EEVEE_EMOTES_TABLE_NAME = "emotes";

        public const int NICKNAME_COUNT_MAX = 2;
        public const int NICKNAME_LENGTH_MIN = 2;
        public const int EMOTE_RESOLUTION_SIZE = 36;

        public const int EMBED_TITLE_LIMIT = 256;
        public const int EMBED_DESCRIPTION_LIMIT = 2048;
        public const int EMBED_FIELD_COUNT_LIMIT = 25;
        public const int EMBED_FIELD_NAME_LIMIT = 256;
        public const int EMBED_FIELD_VALUE_LIMIT = 1024;
        public static char[] WORD_ENDS = new[] { ' ', ':', ';', '"', '.', ',', '}', ')', ']' , '\n', '\'', '?', '!'};
        public static char[] WORD_BEGINS = new[] { ' ', ':', ';', '"', '.', ',', '[', ')', '[', '\n', '\'', '?', '!', '-' };


        public const ConsoleColor DEFAULT_COLOR = ConsoleColor.White;

        public const string DATETIME_FORMAT = "MM/dd/yyyy hh:mm:ss.fff tt";

        public const int MAX_EMOTES_PER_PAGE = 15;

        public const bool ALLOW_DEBUG_LOG_MESSAGES = false;

        public static void BuildErrorMessage(EmbedBuilder eBuilder, SocketCommandContext Context,
            ErrorTypes err, object entity = null, string type = null, string src = null)
        {
            eBuilder.WithTitle(err.ErrorToString())
                .WithThumbnailUrl(ERROR_THUMBNAIL);

            switch (err)
            {
                case ErrorTypes.E404:
                    eBuilder.WithDescription($"{type} **[{entity}]** wasn't found in {src}.");
                    break;
                case ErrorTypes.E405:
                    eBuilder.WithDescription($"{type} **[{entity}]** already exists in {src}.");
                    break;
                case ErrorTypes.E406:
                    eBuilder.WithDescription($"A {type} must be at least **{entity}** characters long.");
                    break;
                case ErrorTypes.E407:
                    eBuilder.WithDescription($"A {type} must be less or equal to **{entity}** characters long.");
                    break;
                case ErrorTypes.E408:
                    eBuilder.WithDescription(type + "."); //type - the free text
                    break;
                case ErrorTypes.E409:
                    eBuilder.WithDescription($"You must be Whitelisted in order to {type}"); //type - free text
                    break;
                case ErrorTypes.E410:
                    eBuilder.WithDescription($"You must be the Owner in order to {type}"); //type - free text
                    break;
                case ErrorTypes.E411:
                    eBuilder.WithDescription($"Bot Users can't {type}."); //type - free text
                    break;
                case ErrorTypes.E412:
                    eBuilder.WithDescription("All Emote Guilds have run out of space for new Emotes"); //type - free text
                    break;
                default:
                    break;
            }
        }

        public static void BuildSuccessMessage(EmbedBuilder eBuilder, SocketCommandContext Context, string message, bool overrideInfo = true)
        {
            eBuilder.WithDescription(message)
                .WithThumbnailUrl(SUCCESS_THUMBNAIL);

            if (overrideInfo)
                eBuilder.WithTitle("Success");
        }
    }

    public enum Permissions
    {
        Rannick = 69,
        Blacklisted,
        Owner,
        Whitelisted,
        Regular
    }

    public class Permission : Attribute
    {
        public Permissions Access { get; protected set; } = Permissions.Regular;
        public Permission()
        {
        }
        public Permission(Permissions perm)
        {
            Access = perm;
        }
    }

    public static class ExtensionMethods
    {
        public static T[] ByOrder<T>(this T[] origin, params int[] order)
        {
            T[] on = new T[origin.Length];
            Array.Copy(origin, on, origin.Length);
            for (int i = 0; i < origin.Length; i++)
            {
                on[i] = origin[order[i]];
            }

            return on;
        }

        public static string ErrorToString(this ErrorTypes err)
            => $"ERROR {(int)err}";

        public static string[] AllBetween(this string str, string sep)
        {
            int n = (int)(str.CountString(sep) / 2);
            string[] between = new string[n];
            for (int i = 0; i < n; i++)
            {
                between[i] = str.Between(sep, i);
            }

            return between;
        }
        public static string[] AllBetween(this string str, char sep)
        {
            int n = str.Count(x => x == sep);
            n = (int)(n / 2);
            string[] between = new string[n];
            for (int i = 0; i < n; i++)
            {
                between[i] = str.Between(sep, i);
            }

            return between;
        }

        public static string Between(this string str, string sep, int c)
        {
            string origin = str;
            int n = 0;
            do
            {
                n = origin.IndexOf(sep);
                str = new string(origin.Skip(n + sep.Length).ToArray());
                n = str.IndexOf(sep);
                str = new string(str.Take(n).ToArray());
                origin = new string(origin.Skip(n + sep.Length * 2).ToArray());
                c--;
            }
            while (c > 0);

            return str;
        }

        public static string Between(this string str, char sep, int c)
        {
            string origin = str;
            int n = 0;
            do
            {
                n = origin.IndexOf(sep);
                str = new string(origin.Skip(n + 1).ToArray());
                n = str.IndexOf(sep);
                str = new string(str.Take(n).ToArray());
                origin = new string(origin.Skip(n + 2).ToArray());
                c--;
            }
            while (c > 0);

            return str;
        }

        public static string MultipleTrimStart(this string str, params string[] strs)
        {
            string before = str;
            foreach (var s in strs)
            {
                if (str.StartsWith(s))
                    str = str.Remove(0, s.Length);
            }
            return str;
        }

        public static int CountString(this string str, string input)
        {
            if (input.Length > 0)
            {
                string checkOn = input;
                int occurrences = 0;
                int chCount = 0;
                foreach (char c in str)
                {
                    if (c == checkOn[0])
                    {
                        chCount++;
                        checkOn = new string(checkOn.Skip(1).ToArray());
                        if (chCount == input.Length)
                        {
                            occurrences++;
                            chCount = 0;
                            checkOn = input;
                        }
                    }
                }
                return occurrences;
            }
            return 0;
        }

        public static int CountWords(this string str)
        {
            str = str.Trim(' ');
            return str.Split(' ').Length;
        }

        public static int CountWord(this string str, string input)
        {
            str = str.Trim(' ').ToLower();
            var words = str.Split(' ');
            return words.Count(x => x == input.ToLower());
        }

        public static bool EqualsCaseInsensitive(this string str, string otherStr)
            => str.ToLower() == otherStr.ToLower();

        public static bool ContainsCaseInsensitive(this string str, string otherStr)
            => str.ToLower().Contains(otherStr.ToLower());

        public static bool Exists<T>(this DbSet<T> set, Expression<Func<T, bool>> func) where T : class
            => set?.FirstOrDefault(func) != null;

        public static bool IsEmpty<T>(this IQueryable<T> collec)
            => collec.Count() == 0;

        public static bool IsEmpty<T>(this DbSet<T> collec) where T : class
            => collec?.Count() == 0;

        public static void RemoveAll<T>(this DbSet<T> set, Expression<Func<T, bool>> func) where T : class
        {
            var entities = set.Where(func);
            if (!entities.IsEmpty())
                set.RemoveRange(entities);
        }

        public static void UpdateOrInsert<T>(this DbSet<T> set, T entity) where T : class
        {
            if (set != null)
            {
                if (set.Contains(entity))
                    set.Update(entity);
                else
                    set.Add(entity);
            }
        }
        public static void UpdateOrInsertRange<T>(this DbSet<T> set, IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                if (set != null)
                    set.UpdateOrInsert(entity);
            }
        }

        public static async Task<IEnumerable<EeveeEmote>> AssociateMultipleAsync(this DbSet<EeveeEmote> set, ulong userId, string name)
        {
            name = name.ToLower().Trim();

            if (name.Count(x => x == ':') == 2)
                name = name.Between(':', 1);

            return (await set.ToListAsync()).Where(x => x.Name.ToLower() == name.ToLower() || x.Aliases.Exists(y => y.OwnerId == userId && y.Alias.ToLower() == name.ToLower()));
        }

        public static async Task<EeveeEmote> AssociateAsync(this DbSet<EeveeEmote> set, ulong userId, string name)
        {
            var ems = await set.AssociateMultipleAsync(userId, name);
            if (ems.Count() == 1)
                return ems.FirstOrDefault();
            else
                return ems.FirstOrDefault(x => x.AdderId == userId);
        }

        public static async Task<IEnumerable<EeveeEmote>> AssociateRangeAsync(this DbSet<EeveeEmote> set, ulong userId, IEnumerable<string> names)
            => await Task.WhenAll(names.Select(x => set.AssociateAsync(userId, x)));

        public static async Task<bool> IsAssociatedAsync(this DbSet<EeveeEmote> set, ulong userId, string name)
            => await set.AssociateAsync(userId, name) != null;

        public static bool Exists<T>(this IEnumerable<T> collec, Func<T, bool> func)
            => collec.Count(func) > 0;

        public static bool IsEmpty<T>(this List<T> collec)
            => collec.Count == 0;
        public static bool IsEmpty<T>(this IEnumerable<T> collec)
            => collec.Count() == 0;

        public static bool IsLetter(this char c)
            => (c >= 65 && c <= 90) || (c >= 97 && c <= 122);

        public static List<string> SplitByLength(this string str, int count, bool keepWords = false)
        {
            List<string> res = new List<string>();
            if (count < str.Length)
            {
                string curr = string.Empty;

                for (int i = 0; i < str.Length; i++)
                {
                    curr += str[i];
                    if (curr.Length == count)
                    {
                        if (keepWords && i != str.Length - 1 && !Defined.WORD_ENDS.Contains(str[i + 1]))
                        {
                            for (int y = i - 1; y >= 0; y--)
                            {
                                if (Defined.WORD_BEGINS.Contains(str[y]))
                                {
                                    i = y;
                                    curr = new string(curr.Take(y + 1).ToArray());
                                    break;
                                }
                            }
                        }
                        res.Add(curr.Trim());
                        curr = string.Empty;
                    }
                }

                if (curr.Length > 0)
                    res.Add(curr.Trim());
            }
            else
                res.Add(str.Trim());

            return res;
        }

        public static T Random<T>(this T[] collec)
            => collec[new Random().Next(collec.Length - 1)];
    }
}
