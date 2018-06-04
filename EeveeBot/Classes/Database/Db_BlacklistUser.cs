using System;
using System.Collections.Generic;
using System.Text;

using EeveeBot.Interfaces;

using LiteDB;

namespace EeveeBot.Classes.Database
{
    public class Db_BlacklistUser : IUser
    {
        [BsonId]
        public ulong Id { get; set; }

        public Obj_BlacklistUser EncapsulateToObj()
        {
            return new Obj_BlacklistUser(Id);
        }
    }
    public class Obj_BlacklistUser : Db_BlacklistUser
    {
        public new ulong Id
        {
            get { return base.Id; }
            protected set { base.Id = value; }
        }


        public Obj_BlacklistUser(ulong id)
        {
            Id = id;
        }

        public Db_BlacklistUser EncapsulateToDb()
        {
            return this;
        }
    }
}