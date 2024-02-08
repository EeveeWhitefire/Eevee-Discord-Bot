using System.ComponentModel.DataAnnotations;

using EeveeBot.Interfaces;

namespace EeveeBot.Classes.Database
{
    public class BlacklistUser : IUser
    {
        [Key]
        [ConcurrencyCheck]
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