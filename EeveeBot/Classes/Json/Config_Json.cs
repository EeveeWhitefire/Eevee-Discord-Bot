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
        private string _projectPath { get; set; }
        public string Project_Path
        {
            get => _projectPath;
            set => _projectPath = value.Replace("%Sync%", Environment.GetEnvironmentVariable("Sync"));
        }
    }
}