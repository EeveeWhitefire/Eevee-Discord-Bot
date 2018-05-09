using System;
using System.Collections.Generic;
using System.Text;

namespace EeveeBot.Classes.Json
{
    public class UrbanDictionary_Json
    {
        public class Output
        {
            public List<Definition> list { get; set; }
        }

        public class Definition
        {
            public string definition { get; set; }
            public string example { get; set; }
        }
    }
}
