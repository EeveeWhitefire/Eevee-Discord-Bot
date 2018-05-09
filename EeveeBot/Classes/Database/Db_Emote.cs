using System;
using System.Collections.Generic;
using System.Text;

using Discord;

using LiteDB;

using EeveeBot.Interfaces;

namespace EeveeBot.Classes.Database
{
    public class Db_Emote : IEeveeEmote
    {
        [BsonId]
        public ulong Id { get; set; }
        public string Name { get; set; }
        public bool IsAnimated { get; set; } = false;
        public List<UserDefined> Nicknames { get; set; }
        public string RelativePath { get; set; }
        public string Url { get; set; }

        public Obj_Emote EncapsulateToObj()
        {
            return new Obj_Emote(Id, Name, IsAnimated, Url, Nicknames);
        }
    }

    public class UserDefined
    {
        public ulong AssociatedEmoteId { get; set; }
        public string Nickname { get; set; }
        public ulong OwnerId { get; set; }
    }

    public class Obj_Emote : IEeveeEmote
    {
        [BsonId]
        public ulong Id { get; private set; }
        public string Name { get; private set; }
        public bool IsAnimated { get; private set; } = false;
        public List<UserDefined> Nicknames { get; private set; }
        public string RelativePath { get; private set; }
        public string Url { get; private set; }

        public Obj_Emote(ulong id, string n, bool isAnimated, string url, List<UserDefined> names = null)
        {
            Name = n;
            Id = id;
            IsAnimated = isAnimated;
            RelativePath = $@"Emotes\{Id}{(IsAnimated ? ".gif" : ".png")}";
            Url = url;
            Nicknames = names ?? new List<UserDefined>();
        }

        public Db_Emote EncapsulateToDb()
        {
            return new Db_Emote()
            {
                Id = Id,
                Name = Name,
                IsAnimated = IsAnimated,
                Nicknames = Nicknames,
                RelativePath = RelativePath,
                Url = Url
            };
        }
    }
}
