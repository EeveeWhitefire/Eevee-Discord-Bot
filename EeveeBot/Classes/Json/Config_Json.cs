using System;
using System.Collections.Generic;
using System.Text;

namespace EeveeBot.Classes.Json
{
    public class Config_Json
    {
        public string Token { get; set; }
        public string Bot_Name { get; set; } = "Eevee";
        public string[] Prefixes { get; set; } = new string[] { "ev!" };
        public ulong Client_Id { get; set; } = 337649506856468491;
        public ulong[] Private_Guilds { get; set; } = new ulong[] { 452502455897292820, 452502303639994368, 452502558959861760 };
        public string Project_Directory { get; set; }
    }
}