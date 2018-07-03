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
        public bool IsOwner { get; set; } = false;

        public Obj_WhitelistUser EncapsulateToObj()
        {
            return new Obj_WhitelistUser(Id, IsOwner);
        }

        public override string ToString()
            => $"{Id} {(IsOwner ? Defined.OWNER_ICON : Defined.WHITELISTED_ICON)}";
    }

    public class Obj_WhitelistUser : Db_WhitelistUser
    {
        public new ulong Id
        {
            get { return base.Id; }
            protected set { base.Id = value; }
        }
        public new bool IsOwner
        {
            get { return base.IsOwner; }
            protected set { base.IsOwner = value; }
        }

        public Obj_WhitelistUser() { }

        public Obj_WhitelistUser(ulong id, bool isOwner = false)
        {
            Id = id;
            IsOwner = isOwner;
        }

        public Db_WhitelistUser EncapsulateToDb()
        {
            return this;
        }
    }
}
