using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

using LiteDB;

using EeveeBot.Classes.Json;

namespace EeveeBot.Classes.Database
{
    public class DatabaseContext : LiteDatabase
    {
        public DatabaseContext(Config_Json _cnfg) : base($"{_cnfg.Project_Path}\\{_cnfg.Bot_Name.Trim(' ')}.db4")
        {
            GetCollection<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME).EnsureIndex(x => x.Id);
            GetCollection<WhitelistUser>(Defined.WHITELIST_TABLE_NAME).EnsureIndex(x => x.Id);
            GetCollection<BlacklistUser>(Defined.BLACKLIST_TABLE_NAME).EnsureIndex(x => x.Id);
        }

        public int ClearTable<T>(string name) where T : class
            => GetCollection<T>(name).Delete(x => true);

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
        public void UpdateEntity<T>(string tableName, IEnumerable<T> entities) where T : class
        {
            GetCollection<T>(tableName).Update(entities);
        }

        public int DeleteEntity<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
            => GetCollection<T>(tableName).Delete(pred);

        public T FirstOrDefault<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
            => GetCollection<T>(tableName).FindOne(pred);

        public IEnumerable<T> GetWhere<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
            => GetCollection<T>(tableName).Find(pred);

        public IEnumerable<T> GetAll<T>(string name) where T : class
            => GetCollection<T>(name).FindAll();

        public bool Exists<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
            => GetCollection<T>(tableName).Exists(pred);

        public bool IsEmpty<T>(string tableName) where T : class
            => GetCollection<T>(tableName).Count() < 1;

        public int Count<T>(string tableName) where T : class
            => GetCollection<T>(tableName).Count();

        public int CountWhere<T>(string tableName, Expression<Func<T, bool>> pred) where T : class
            => GetCollection<T>(tableName).Find(pred).Count();

        public EeveeEmote TryEmoteAssociation(ulong userId, string name)
        {
            var ems = GetWhere<EeveeEmote>(Defined.EEVEE_EMOTES_TABLE_NAME, x => x.TryAssociation(name, userId));
            int count = ems.Count();
            if (count > 0)
            {
                if (count == 1)
                    return ems.FirstOrDefault();
                else
                    return ems.FirstOrDefault(x => x.AdderId == userId);
            }
            return null;
        }
    }
}
