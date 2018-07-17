using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Discord;

using LiteDB;

using EeveeBot.Interfaces;

namespace EeveeBot.Classes.Database
{
    public class EeveeEmoteAlias
    {
        public ulong AssociatedEmoteId { get; set; }
        public string Alias { get; set; }
        public ulong OwnerId { get; set; }
    }

    public class EeveeEmote : IEeveeEmote
    {
        [BsonId]
        public ulong Id { get; set; }
        public ulong AdderId { get; set; }
        public ulong GuildId { get; set; }
        public string Name { get; set; }
        public bool IsAnimated { get; set; } = false;
        public bool IsDefault { get; set; } = false;
        public List<EeveeEmoteAlias> Aliases { get; set; } = new List<EeveeEmoteAlias>();
        public string RelativePath { get; set; }
        public string Url { get; set; }

        public EeveeEmote() { }

        public EeveeEmote(ulong id, ulong adderId, ulong guildId, string n, bool isAnimated, bool isDef, string url, List<EeveeEmoteAlias> names = null)
        {
            Name = n;
            Id = id;
            AdderId = adderId;
            IsDefault = isDef;
            GuildId = guildId;
            IsAnimated = isAnimated;
            RelativePath = $@"Emotes\{Id}{(IsAnimated ? ".gif" : ".png")}";
            Url = url;
            Aliases = names ?? new List<EeveeEmoteAlias>();
        }

        public EeveeEmote(GuildEmote em, ulong guildId, ulong adderId, bool isDef) : this(em.Id, adderId, guildId, em.Name, em.Animated, isDef, em.Url)
        { }

        public override string ToString()
            => $"<{(IsAnimated ? "a" : string.Empty)}:{Name}:{Id}>";

        public bool TryAssociation(string name, ulong ownerId)
            => GetAllNamesLowered(ownerId).Contains(name.ToLower());

        public IEnumerable<string> GetAllNames(ulong ownerId)
        {
            List<string> list = new List<string>
            {
                $"{Name}#{AdderId}"
            };

            if (IsDefault || ownerId == AdderId)
                list.Add(Name);

            list.AddRange(Aliases.Where(x => x.OwnerId == ownerId).Select(x => x.Alias));
            return list;
        }

        public IEnumerable<string> GetAllNamesLowered(ulong ownerId)
            => GetAllNames(ownerId).Select(x => x.ToLower());
    }
}