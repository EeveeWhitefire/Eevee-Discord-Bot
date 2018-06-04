using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

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
        E408 ///invalid length - free text
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

        public static async Task SendErrorMessage(EmbedBuilder eBuilder, SocketCommandContext Context, 
            ErrorTypes err, object entity, string type, string src = null)
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
                default:
                    break;
            }

            await Context.Channel.SendMessageAsync(string.Empty, embed: eBuilder.Build());
        }

        public static async Task SendSuccessMessage(EmbedBuilder eBuilder, SocketCommandContext Context, string message)
        {
            eBuilder.WithTitle("Success")
                .WithDescription(message)
                .WithThumbnailUrl(SUCCESS_THUMBNAIL);

            await Context.Channel.SendMessageAsync(string.Empty, embed: eBuilder.Build());
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
    }
}
