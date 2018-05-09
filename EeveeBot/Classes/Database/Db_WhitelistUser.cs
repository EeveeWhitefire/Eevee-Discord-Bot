using System;
using System.Collections.Generic;
using System.Text;

using LiteDB;

using EeveeBot.Interfaces;

namespace EeveeBot.Classes.Database
{
    public class Db_WhitelistUser : IWhitelistUser, IUser
    {
        [BsonId]
        public ulong Id { get; set; }
        public bool IsOwner { get; set; }

        public Obj_WhitelistUser EncapsulateToObj()
        {
            return new Obj_WhitelistUser(Id, IsOwner);
        }
    }

    public class Obj_WhitelistUser : IWhitelistUser, IUser
    {
        public ulong Id { get; protected set; }
        public bool IsOwner { get; protected set; }


        public Obj_WhitelistUser(ulong id, bool isOwner = false)
        {
            Id = id;
            IsOwner = isOwner;
        }

        public Db_WhitelistUser EncapsulateToDb()
        {
            return new Db_WhitelistUser()
            {
                Id = Id,
                IsOwner = IsOwner
            };
        }
    }
}
