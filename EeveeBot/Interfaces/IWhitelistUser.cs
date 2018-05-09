using System;
using System.Collections.Generic;
using System.Text;

namespace EeveeBot.Interfaces
{
    public interface IWhitelistUser
    {
        bool IsOwner { get; }
    }
}
