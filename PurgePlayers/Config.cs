using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using Terraria;
using System.ComponentModel;

namespace PurgePlayers
{
    public class Config
    {
        public bool debugPP = false;
        [Description("How many days old will be purged.")]
        public int purgeBeforeDays = 20;
        [Description("Should players be archived before purging?")]
        public bool archiveDeletes = false;
        [Description("Log archive file name.")]
        public string archiveFileName = "DeletedPlayers.sql";

        [Description("List of Usernames that won't be deleted, ever!")]
        public string[] DoNotDelete = {""};

        [Description("List of Groups that won't be deleted, ever!")]
        public string[] DoNotDeleteGroups = { "admin", "trustedadmin", "newadmin", "superadmin" };

        [Description("Should purging be done automatically at the autoPurgeTime?")]
        public bool autoPurge = false;
        [Description("The time of day (hour, min, sec) when auto purge will run if set.!")]
        public TimeSpan autoPurgeTime = new TimeSpan(12, 0, 0);

        public void Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public static Config Read(string path)
		{
			return !File.Exists(path)
				? new Config()
				: JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
		}
	}
}

