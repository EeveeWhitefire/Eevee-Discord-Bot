using System;
using System.Collections.Generic;
using System.Text;

using EeveeBot.Interfaces;

using LiteDB;

namespace EeveeBot.Classes.Database
{
    public class BlacklistUser : IUser
    {
        [BsonId]
        public ulong Id { get; set; }

        public BlacklistUser() { }
        public BlacklistUser(ulong id)
        {
            Id = id;
        }

        public override string ToString()
            => Id.ToString();
    }
}