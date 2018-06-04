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

    public class Db_EeveeEmote : IEeveeEmote
    {
        [BsonId]
        public virtual ulong Id { get; set; }
        public string Name { get; set; }
        public bool IsAnimated { get; set; } = false;
        public List<EeveeEmoteAlias> Aliases { get; set; } = new List<EeveeEmoteAlias>();
        public string RelativePath { get; set; }
        public string Url { get; set; }

        public Db_EeveeEmote() { }

        public Db_EeveeEmote(ulong id, string n, bool isAnimated, string url, List<EeveeEmoteAlias> names = null)
        {
            Name = n;
            Id = id;
            IsAnimated = isAnimated;
            RelativePath = $@"Emotes\{Id}{(IsAnimated ? ".gif" : ".png")}";
            Url = url;
            Aliases = names ?? new List<EeveeEmoteAlias>();
        }

        public Db_EeveeEmote(GuildEmote em) : this(em.Id, em.Name, em.Animated, em.Url)
        { }

        public override string ToString()
            => $"<{(IsAnimated ? "a" : string.Empty)}:{Name}:{Id}>";

        public Obj_EeveeEmote ToObject()
        {
            return new Obj_EeveeEmote(this);
        }

        public bool TryAssociation(string name, ulong ownerId)
            => GetAllNamesLowered(ownerId).Contains(name.ToLower());

        public IEnumerable<string> GetAllNames(ulong ownerId)
        {
            List<string> list = new List<string>
            {
                Name
            };

            list.AddRange(Aliases.Where(x => x.OwnerId == ownerId).Select(x => x.Alias));
            return list;
        }

        public IEnumerable<string> GetAllNamesLowered(ulong ownerId)
            => GetAllNames(ownerId).Select(x => x.ToLower());
    }

    public class Obj_EeveeEmote : Db_EeveeEmote
    {
        #region Fields
        public new ulong Id
        {
            get { return base.Id; }
            private set { base.Id = value; }
        }
        public new string Name
        {
            get { return base.Name; }
            private set { base.Name = value; }
        }
        public new bool IsAnimated
        {
            get { return base.IsAnimated; }
            private set { base.IsAnimated = IsAnimated; }
        }
        public new List<EeveeEmoteAlias> Aliases
        {
            get { return base.Aliases; }
            private set { base.Aliases = value; }
        }
        public new string RelativePath
        {
            get { return base.RelativePath; }
            private set { base.RelativePath = value; }
        }
        public new string Url
        {
            get { return base.Url; }
            private set { base.Url = value; }
        }
        #endregion

        public Obj_EeveeEmote(Db_EeveeEmote b)
        {
            Name = b.Name;
            Id = b.Id;
            IsAnimated = b.IsAnimated;
            RelativePath = $@"Emotes\{b.Id}{(b.IsAnimated ? ".gif" : ".png")}";
            Url = b.Url;
            Aliases = b.Aliases;
        }

        public Obj_EeveeEmote(ulong id, string n, bool isAnimated, string url, List<EeveeEmoteAlias> aliases = null)
            : base(id, n, isAnimated, url, aliases)
        {
        }

        public Db_EeveeEmote EncapsulateToDb()
        {
            return this;
        }
    }
}
