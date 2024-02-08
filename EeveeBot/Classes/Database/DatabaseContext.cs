using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EeveeBot.Classes.Json;
using Microsoft.EntityFrameworkCore;

namespace EeveeBot.Classes.Database
{
    public class DatabaseRepository : IDisposable
    {
        public class DatabaseContext : DbContext
        {
            public DbSet<WhitelistUser> Whitelist { get; set; }
            public DbSet<BlacklistUser> Blacklist { get; set; }

            public DbSet<EeveeEmote> Emotes { get; set; }
            public DbSet<EeveeEmoteAlias> EmoteAliases { get; set; }

            public DatabaseContext(DbContextOptions options) : base(options)
            { }
        }

        public class RuntimeDBCache
        {
            public List<EeveeEmote> Emotes { get; private set; } = new List<EeveeEmote>();
            public List<EeveeEmoteAlias> Aliases { get; private set; } = new List<EeveeEmoteAlias>();

            public async Task InitAsync(DatabaseContext dbContext)
            {
                await dbContext.Emotes.ForEachAsync(em =>
                {
                    if (!Emotes.Exists(x => x.Id == em.Id))
                        Emotes.Add(em);
                });

                await dbContext.EmoteAliases.ForEachAsync(al =>
                {
                    if (!Aliases.Exists(x => x.Id == al.Id))
                        Aliases.Add(al);

                    Emotes.FirstOrDefault(em => em.Id == al.EmoteId).Aliases.Add(al);
                });

                Emotes.RemoveAll(x => !dbContext.Emotes.Exists(y => y.Id == x.Id));

                foreach (var al in Aliases.Where(x => !dbContext.EmoteAliases.Exists(y => y.Id == x.Id)))
                {
                    Aliases.RemoveAll(x => x.Id == al.Id);
                    Emotes.ForEach(x =>
                    {
                        x.Aliases.RemoveAll(y => y.Id == al.Id);
                    });
                }
            }
        }

        private readonly DatabaseContext _dbContext;
        private readonly RuntimeDBCache _cache;

        public DatabaseRepository(DatabaseContext dbContext, RuntimeDBCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;

            _cache.InitAsync(_dbContext).GetAwaiter();
        }

        public bool IsWhitelisted(ulong userId)
            => _dbContext.Whitelist.Exists(x => x.Id == userId);

        public bool IsOwner(ulong userId)
            => _dbContext.Whitelist.Exists(x => x.Id == userId && x.IsOwner);

        public bool NoOwner()
            => !_dbContext.Whitelist.Exists(x => x.IsOwner);

        public bool CanChangeOwner(ulong userId)
            => IsOwner(userId) || NoOwner();

        public bool IsBlacklisted(ulong userId)
            => _dbContext.Blacklist.Exists(x => x.Id == userId);

        public async Task WhitelistAddAsync(ulong userId)
        {
            if(!IsWhitelisted(userId))
                _dbContext.Whitelist.Add(new WhitelistUser(userId, isOwner: _dbContext.Whitelist.IsEmpty()));

            _dbContext.Blacklist.RemoveAll(x => x.Id == userId);
            await Task.CompletedTask;
        }

        public async Task WhitelistRemoveAsync(ulong userId)
        {
            _dbContext.Whitelist.RemoveAll(x => x.Id == userId);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<WhitelistUser>> GetWhitelistAsync()
            => await _dbContext.Whitelist.OrderByDescending(x => x.IsOwner).ToListAsync();

        public async Task ChangeOwnerAsync(ulong targetId, ulong callerId)
        {
            if(targetId == callerId && NoOwner()) //can only occur if there is no owner curr
            {
                var u = await _dbContext.Whitelist.FirstOrDefaultAsync(x => x.Id == targetId) ?? new WhitelistUser(targetId, isOwner: true);
                _dbContext.Whitelist.UpdateOrInsert(u);
                _dbContext.Entry(u).State = EntityState.Detached;
            }
            else //can only occur if caller is an owner and targetId /= callerId
            {
                var caller = await _dbContext.Whitelist.FirstOrDefaultAsync(x => x.Id == callerId);
                var target = await _dbContext.Whitelist.FirstOrDefaultAsync(x => x.Id == targetId) ?? new WhitelistUser(targetId, isOwner: false);

                caller.ToggleIsOwner(); //owner to not owner
                target.ToggleIsOwner(); //not owner to owner

                _dbContext.Whitelist.Update(caller);
                _dbContext.Whitelist.UpdateOrInsert(target);
            }
        }

        public async Task BlacklistAddAsync(ulong userId)
        {
            if (!IsBlacklisted(userId))
                _dbContext.Blacklist.Add(new BlacklistUser(userId));

            _dbContext.Whitelist.RemoveAll(x => x.Id == userId);
            await Task.CompletedTask;
        }

        public async Task BlacklistRemoveAsync(ulong userId)
        {
            _dbContext.Blacklist.RemoveAll(x => x.Id == userId); //only happens if the caller is the owner
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BlacklistUser>> GetBlacklistAsync()
            => await _dbContext.Blacklist.ToListAsync();

        public async Task DeleteEmoteAliasAsync(EeveeEmoteAlias alias)
        {
            _dbContext.EmoteAliases.RemoveAll(x => x.Id == alias.Id);

            await Task.CompletedTask;
        }

        public async Task DeleteEmoteAsync(EeveeEmote emote)
        {
            _dbContext.Emotes.RemoveAll(x => x.Id == emote.Id);
            _dbContext.EmoteAliases.RemoveAll(x => x.EmoteId == emote.Id);

            await Task.CompletedTask;
        }

        public async Task AddEmoteAsync(EeveeEmote emote)
        {
            _dbContext.Emotes.Add(emote);

            await Task.CompletedTask;
        }

        public async Task AddAliasAsync(EeveeEmoteAlias alias)
        {
            _dbContext.EmoteAliases.Add(alias);

            await Task.CompletedTask;
        }

        public async Task<EeveeEmote> AssociateEmoteAsync(ulong userId, string input)
        {
            foreach (var em in _cache.Emotes)
            {
                if (await em.TryAssociate(userId, input))
                {
                    return em;
                }
            }
            return default;
        }

        public async Task<bool> IsEmoteAssociatedAsync(ulong userId, string input)
            => await AssociateEmoteAsync(userId, input) != default(EeveeEmote);

        public async Task<EeveeEmoteAlias> AssociateEmoteAliasAsync(ulong userId, string input)
        {
            await Task.CompletedTask;
            var res = _cache.Aliases.FirstOrDefault(x => x.OwnerId == userId && x.Alias.EqualsCaseInsensitive(input));

            return res;
        }

        public async Task<IEnumerable<EeveeEmote>> AssociateMultipleEmotesAsync(ulong userId, string[] input)
            => await Task.WhenAll(input.Select(x => AssociateEmoteAsync(userId, x)));

        public async Task<bool> EmoteExistsAsync(ulong userid, ulong emoteId, string input)
        {
            await Task.CompletedTask;

            return _cache.Emotes.Exists(x => x.Id == emoteId || x.TryAssociate(userid, input).GetAwaiter().GetResult());
        }

        public async Task<IEnumerable<EeveeEmote>> GetEmotesAsync(ulong userId)
        {
            await Task.CompletedTask;

            return _cache.Emotes.OrderByDescending(x => x.Aliases.Count(y => y.OwnerId == userId))
                  .OrderByDescending(x => x.IsAnimated);
        }
        public async Task<IEnumerable<EeveeEmote>> GetEmotesAsync(ulong userId, string input)
        {
            if (input.Count(x => x == ':') == 2)
                input = input.Between(':', 1);
            await Task.CompletedTask;

            return (await GetEmotesAsync(userId)).Where(x => x.Name.ContainsCaseInsensitive(input) || x.Aliases.Exists(y => y.Alias.ContainsCaseInsensitive(input)));
        }

        public void Dispose()
        {
            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }
    }
}
