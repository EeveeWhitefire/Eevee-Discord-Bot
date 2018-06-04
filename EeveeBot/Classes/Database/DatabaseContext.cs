using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;

using LiteDB;

using EeveeBot.Classes.Json;

namespace EeveeBot.Classes.Database
{
    public class DatabaseContext : LiteDatabase
    {
        public DatabaseContext(Config_Json _cnfg) : base($"{_cnfg.Project_Path}\\{_cnfg.Bot_Name.Trim(' ')}.db")
        {
            GetCollection<Db_EeveeEmote>("emotes");
            GetCollection<Db_WhitelistUser>("whitelist");
            GetCollection<Db_BlacklistUser>("blacklist");
        }

        public void ClearTable<T>(string name) where T : class
        {
            int k = GetCollection<T>(name).Delete(x => true);
        }

        public void AddEntity<T>(string tableName, T entity) where T : class
        {
            GetCollection<T>(tableName).Insert(entity);
        }

        public void AddEntity<T>(string tableName, IEnumerable<T> entities) where T : class
        {
            GetCollection<T>(tableName).InsertBulk(entities);
        }

        public void UpdateEntity<T>(string tableName, T entity) where T : class
        {
            GetCollection<T>(tableName).Update(entity);
        }

        public void DeleteEntity<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
        {
            GetCollection<T>(tableName).Delete(pred);
        }

        public IEnumerable<T> GetAll<T>(string name) where T : class
            => GetCollection<T>(name).FindAll();
    }
}
