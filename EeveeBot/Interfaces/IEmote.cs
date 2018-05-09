using System;
using System.Collections.Generic;
using System.Text;

using EeveeBot.Classes.Database;

namespace EeveeBot.Interfaces
{
    public interface IEeveeEmote
    {
        ulong Id { get; }
        string Name { get; }
        string RelativePath { get; }
        bool IsAnimated { get; }
        List<UserDefined> Nicknames { get; }
        string Url { get; }
    }
}
