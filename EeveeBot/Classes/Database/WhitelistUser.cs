using EeveeBot.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EeveeBot.Classes.Database
{
    public class WhitelistUser : IUser
    {
        [Key]
        [ConcurrencyCheck]
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

        public void ToggleIsOwner()
        {
            IsOwner = !IsOwner;
        }
    }
}
