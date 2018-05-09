using System;
using System.Collections.Generic;
using System.Text;

namespace EeveeBot.Classes.Json
{
    public class LanguagesConfig_Json
    {
        public IList<Language_Json> Languages { get; set; }
    }

    public class Language_Json
    {
        public string Name { get; set; }
        public bool RequiresCompilation { get; set; } = false;
        public string Extension { get; set; }
        public string[] Shortcuts { get; set; }
        public string EmulatorPath { get; set; }
    }
}
