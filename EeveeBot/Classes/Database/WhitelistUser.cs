using System;
using System.Collections.Generic;
using System.Text;

using LiteDB;

using EeveeBot.Interfaces;

namespace EeveeBot.Classes.Database
{
    public class WhitelistUser : IUser
    {
        [BsonId]
        public ulong Id { get; set; }
        public bool IsOwner { get; set; } = false;
        
        public WhitelistUser() { }
        public WhitelistUser(ulong id, bool isOwner = false)
        {
            Id = id;
            IsOwner = isOwner;
        }

        public override string ToString()
            => Id.ToString();
    }
}
