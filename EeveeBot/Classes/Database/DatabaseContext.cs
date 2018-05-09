using System;
using System.Collections.Generic;
using System.Text;

using LiteDB;

using EeveeBot.Classes.Json;

namespace EeveeBot.Classes.Database
{
    public class DatabaseContext : LiteDatabase
    {
        public DatabaseContext(Config_Json _cnfg) : base($"{_cnfg.Bot_Name.Trim(' ')}.db")
        {
            GetCollection<Db_Emote>("emotes");
            GetCollection<Db_WhitelistUser>("whitelist");
            GetCollection<Db_BlacklistUser>("blacklist");
        }
    }
}
