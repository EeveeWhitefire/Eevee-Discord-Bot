using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace EeveeBot
{
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
        E411 //can't do that on bots
    }

    public class Defined
    {
        public static Color[] Colors = typeof(Color).GetFields().Where(x => x.FieldType == typeof(Color))
            .Select( x => (Color)x.GetValue(null)).ToArray();

        public const string OWNER_ICON = ":crown:";
        public const string WHITELISTED_ICON = ":white_check_mark:";
        public const string ERROR_THUMBNAIL = @"https://cdn2.iconfinder.com/data/icons/color-svg-vector-icons-2/512/error_warning_alert_attention-512.png";
        public const string SUCCESS_THUMBNAIL = @"https://cdn2.iconfinder.com/data/icons/perfect-flat-icons-2/512/Ok_check_yes_tick_accept_success_green_correct.png";
        public const int MAX_EMOTES_IN_GUILD = 50;
        public const string EMOTE_SEPARATOR_PREFIX = ";;";
        public const int COMMANDS_COOLDOWN = 3000;
        public const string COOKIE_THUMBNAIL = @"http://static1.squarespace.com/static/57b5da73b3db2b7747f9c3a4/t/58916aaaebbd1ade326d74f2/1510756771216/";
        public const string BIG_BOSS_THUMBNAIL = @"https://cdn.discordapp.com/attachments/297913371884388353/449286701257588747/big_boss-blurple.png";
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

        public static void BuildErrorMessage(EmbedBuilder eBuilder, SocketCommandContext Context, 
            ErrorTypes err, object entity, string type, string src = null, bool overrideinfo = true)
        {
            if(overrideinfo)
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
                origin = new string(origin.Skip(n + sep.Length*2).ToArray());
                c--;
            }
            while (c >= 0);

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
            while (c >= 0);

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
    }
}
