using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Discord;

using EeveeBot.Interfaces;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace EeveeBot.Classes.Database
{
    public class EeveeEmoteAlias
    {
        public ulong Id { get; set; }
        public ulong EmoteId { get; set; }
        public ulong OwnerId { get; set; }
        public string Alias { get; set; }
    }

    public class EeveeEmote : IEeveeEmote
    {
        [Key]
        [ConcurrencyCheck]
        public ulong Id { get; set; }
        public ulong AdderId { get; set; }
        public ulong GuildId { get; set; }
        public string Name { get; set; }
        public bool IsAnimated { get; set; } = false;
        public string RelativePath { get; set; }
        public string Url { get; set; }

        public List<EeveeEmoteAlias> Aliases { get; private set; }

        public EeveeEmote()
        {
            Aliases = new List<EeveeEmoteAlias>();
        }

        public EeveeEmote(ulong id, ulong adderId, ulong guildId, string n, bool isAnimated, string url)
        {
            Id = id;
            AdderId = adderId;
            GuildId = guildId;
            Name = n;
            IsAnimated = isAnimated;
            Url = url;
            RelativePath = $@"Emotes\{Id}{(IsAnimated ? ".gif" : ".png")}";
        }

        public EeveeEmote(GuildEmote em, ulong guildId, ulong adderId) : this(em.Id, adderId, guildId, em.Name, em.Animated, em.Url)
        { }

        public override string ToString()
            => $"<{(IsAnimated ? "a" : string.Empty)}:{Name}:{Id}>";

        public async Task<bool> TryAssociate(ulong userId, string input)
        {
            if (input.Count(x => x == ':') == 2)
                input = input.Between(':', 1);

            await Task.CompletedTask;
            return Name.EqualsCaseInsensitive(input) || Aliases.Exists(x => x.OwnerId == userId && x.Alias.EqualsCaseInsensitive(input));
        }
    }
}