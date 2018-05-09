using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

using Discord;

namespace EeveeBot
{
    public class Defined
    {
        public static Color[] Colors = typeof(Color).GetFields().Where(x => x.FieldType == typeof(Color))
            .Select( x => (Color)x.GetValue(null)).ToArray();
    }
}
