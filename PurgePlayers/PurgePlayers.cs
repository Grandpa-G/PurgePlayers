using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using System.IO;
using System.Data;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;
using System.IO.Compression;

using Terraria;
using TShockAPI;
using Newtonsoft.Json;
using System.Threading;
using TerrariaApi.Server;
using Newtonsoft.Json.Linq;
using TShockAPI.DB;

namespace PurgePlayers
{
    [ApiVersion(1, 22)]
    public class PurgePlayers : TerrariaPlugin
    {
        private static Config purgePlayersConfig;
        private static int purgeBeforeDays;
        private bool verbose = false;
        private bool preview = false;
        private string archiveFileName;
        System.Timers.Timer executePurge = new System.Timers.Timer();
        public override string Name
        {
            get { return "PurgePlayers"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Deletes inactive players base upon criteria."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public PurgePlayers(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("PurgePlayers.allow", purgePlayers, "purgeplayers", "pp"));

            var path = Path.Combine(TShock.SavePath, "purgeplayers.json");
            (purgePlayersConfig = Config.Read(path)).Write(path);

            purgeBeforeDays = purgePlayersConfig.purgeBeforeDays;
            archiveFileName = purgePlayersConfig.archiveFileName;

            ServerApi.Hooks.GamePostInitialize.Register(this, OnGameInitialize);
        }

        private void OnGameInitialize(EventArgs args)
        {

            if (purgePlayersConfig.autoPurge)
            {
                TimeSpan now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));     // The current time in 24 hour format
                TimeSpan activationTime = purgePlayersConfig.autoPurgeTime;

                TimeSpan timeLeftUntilFirstRun = activationTime - now;
                if (timeLeftUntilFirstRun.TotalHours < 0)
                    timeLeftUntilFirstRun += new TimeSpan(24, 0, 0);    // adds a day from the schedule 
                if (purgePlayersConfig.debugPP)
                    Console.WriteLine(timeLeftUntilFirstRun);

                executePurge.Interval = timeLeftUntilFirstRun.TotalMilliseconds;
                executePurge.Elapsed += autoPurge;    // Event to do your tasks.
                executePurge.AutoReset = true;
                executePurge.Start();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        public void autoPurge(object sender, ElapsedEventArgs e)
        {
            // Do your stuff and recalculate the timer interval and reset the Timer.
            processPurgeCommand();
            TimeSpan day = new TimeSpan(24, 00, 00);     // 24 hours in a day.
            executePurge.Interval = day.TotalMilliseconds;
        }

        private void purgePlayers(CommandArgs args)
        {

            PurgePlayersArguments arguments = new PurgePlayersArguments(args.Parameters.ToArray());
            if (arguments.Contains("-help"))
            {
                args.Player.SendMessage("Syntax: /purgeplayers [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage(" -purge        starts purge operation", Color.LightSalmon);
                args.Player.SendMessage(" -reload/-r    reloads options from purgeplayers.json config file", Color.LightSalmon);
                args.Player.SendMessage(" -verbose/-v   show each user purge", Color.LightSalmon);
                args.Player.SendMessage(" -!verbose/-!v don't show each user purged", Color.LightSalmon);
                args.Player.SendMessage(" -days n       purges any user not accessed in the last n days before today", Color.LightSalmon);
                args.Player.SendMessage(" -!preview/!p  negates preview, thus users will be deleted", Color.LightSalmon);
                args.Player.SendMessage(" -preview/p    performs purge action without actually purging any users", Color.LightSalmon);
                args.Player.SendMessage(" -list/l       show current purge criteria", Color.LightSalmon);
                args.Player.SendMessage(" -help         this information", Color.LightSalmon);
                return;
            }

            if (arguments.Contains("-days"))
            {
                if (args.Parameters.Count > 1)
                    purgeBeforeDays = Int32.Parse(args.Parameters[1]);
                Console.WriteLine(" {0} Days before to purge is " + purgeBeforeDays.ToString(), Name);
                return;
            }

            if (arguments.Contains("-r") || arguments.Contains("-reload"))
            {
                var path = Path.Combine(TShock.SavePath, "purgeplayers.json");
                purgePlayersConfig = Config.Read(path);
                purgeBeforeDays = purgePlayersConfig.purgeBeforeDays;

                if (purgePlayersConfig.autoPurge)
                {
                    executePurge.Stop();
                    TimeSpan now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));     // The current time in 24 hour format
                    TimeSpan activationTime = purgePlayersConfig.autoPurgeTime;

                    TimeSpan timeLeftUntilFirstRun = activationTime - now;
                    if (timeLeftUntilFirstRun.TotalHours < 0)
                        timeLeftUntilFirstRun += new TimeSpan(24, 0, 0);    // adds a day from the schedule 
                    if (purgePlayersConfig.debugPP)
                        Console.WriteLine(timeLeftUntilFirstRun);

                    executePurge.Interval = timeLeftUntilFirstRun.TotalMilliseconds;
                    executePurge.Elapsed += autoPurge;    // Event to do your tasks.
                    executePurge.AutoReset = true;
                    executePurge.Start();
                }
                Console.WriteLine(" {0} Config file reloaded.", Name);
                return;
            }

            if (arguments.Contains("-v") || arguments.Contains("-verbose"))
            {
                verbose = true;
                Console.WriteLine(" {0} Verbose is " + verbose.ToString(), Name);
                return;
            }
            if (arguments.Contains("-!v") || arguments.Contains("-!verbose"))
            {
                verbose = false;
                Console.WriteLine(" {0} Verbose is " + verbose.ToString(), Name);
                return;
            }
            if (arguments.Contains("-p") || arguments.Contains("-preview"))
            {
                preview = true;
                Console.WriteLine(" {0} Preview is " + preview.ToString(), Name);
                return;
            }
            if (arguments.Contains("-!p") || arguments.Contains("-!preview"))
            {
                preview = false;
                Console.WriteLine(" {0} Preview is " + preview.ToString(), Name);
                return;
            }

            if (arguments.Contains("-l") || arguments.Contains("-list"))
            {
                Console.WriteLine("Current options for PurgePlayers version " + Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine(" keepForDays=" + purgeBeforeDays);
                Console.WriteLine(" verbose is " + verbose.ToString());
                Console.WriteLine(" preview is " + preview.ToString());
                return;
            }


            if (arguments.Contains("-purge"))
            {
                processPurgeCommand();
                return;
            }
        }

        private void processPurgeCommand()
        {
            string logFilename = TShock.Log.FileName;

            int purgeCount = 0;

            DateTime keepDate = DateTime.Today.AddDays(-purgeBeforeDays);
            string doNotDelete = String.Join("','", purgePlayersConfig.DoNotDelete);
            string DoNotDeleteGroups = String.Join("','", purgePlayersConfig.DoNotDeleteGroups);
            string sql = string.Format("SELECT * FROM users where ((LastAccessed is null) or (LastAccessed < \"{0}\"))", String.Format("{0:yyyy-MM-dd HH:mm:ss}", keepDate));
            if (purgePlayersConfig.DoNotDelete.Length > 0)
                sql = sql + " and Username not in ('" + doNotDelete + "')";
            if (purgePlayersConfig.DoNotDeleteGroups.Length > 0)
                sql = sql + " and Usergroup not in ('" + DoNotDeleteGroups + "')";

            if (purgePlayersConfig.debugPP)
                Console.WriteLine(sql);
            string archiveFileName = Path.Combine(TShock.SavePath, purgePlayersConfig.archiveFileName);

            string inventory;
            List<int> deleteIds = new List<int>();
            if (purgeBeforeDays > 0)
            {
                try
                {
                    using (var reader = TShock.DB.QueryReader(sql))
                    {
                        while (reader.Read())
                        {
                            int account = reader.Get<Int32>("ID");
                            if (verbose)
                                Console.WriteLine("Player " + reader.Get<string>("Username") + " " + reader.Get<Int32>("id") + " " + reader.Get<string>("LastAccessed"));
                            inventory = string.Format("SELECT * FROM tsCharacter where Account = {0}", account);

                            try
                            {
                                using (var deleter = TShock.DB.QueryReader(inventory))
                                {
                                    if (deleter.Read())
                                    {
                                        if (purgePlayersConfig.archiveDeletes)
                                        {
                                            try
                                            {
                                                using (StreamWriter w = File.AppendText(archiveFileName))
                                                {
                                                    string insert = string.Format("INSERT INTO Users (ID, Username, Password, UUID, Usergroup, Registered, LastAccessed, KnownIPs) VALUES ({0}, \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\", \"{6}\", \"{7}\");",
                                                        reader.Get<Int32>("ID"), reader.Get<string>("Username"), reader.Get<string>("Password"), reader.Get<string>("UUID"),
                                                        reader.Get<string>("Usergroup"), reader.Get<string>("Registered"), reader.Get<string>("LastAccessed"), reader.Get<string>("KnownIPs"));
                                                    w.WriteLine(insert);

                                                    insert = string.Format("INSERT INTO tsCharacter (Account, Health, MaxHealth, Mana, MaxMana, Inventory, spawnX, spawnY, hair, hairDye, hairColor, pantsColor, shirtColor, underShirtColor, shoeColor, hideVisuals, skinColor, eyeColor, questsCompleted) VALUES ({0},{1},{2},{3},{4},\"{5}\",{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18});",
                                                          deleter.Get<Int32>("Account"), deleter.Get<Int32>("Health"), deleter.Get<Int32>("MaxHealth"), deleter.Get<Int32>("Mana"), deleter.Get<Int32>("MaxMana"),
                                                          deleter.Get<string>("Inventory"), deleter.Get<Int32>("spawnX"), deleter.Get<Int32>("spawnY"), deleter.Get<Int32>("hair"), deleter.Get<Int32>("hairDye"),
                                                          deleter.Get<Int32>("hairColor"), deleter.Get<Int32>("pantsColor"), deleter.Get<Int32>("shirtColor"), deleter.Get<Int32>("underShirtColor"),
                                                          deleter.Get<Int32>("shoeColor"), deleter.Get<Int32>("hideVisuals"), deleter.Get<Int32>("skinColor"), deleter.Get<Int32>("eyeColor"), deleter.Get<Int32>("questsCompleted"));
                                                    w.WriteLine(insert);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                TShock.Log.Error(ex.ToString());
                                                Console.WriteLine(ex.StackTrace);
                                            }

                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                TShock.Log.Error(ex.ToString());
                                Console.WriteLine(ex.StackTrace);
                            }

                            deleteIds.Add(account);
                            TShock.Log.Info("User {1}:{0} last accessed on {2} purged.", reader.Get<Int32>("ID"), reader.Get<string>("Username"), reader.Get<string>("LastAccessed"));
                            if (verbose)
                                Console.WriteLine("User {1}:{0} last accessed on {2} purged.", reader.Get<Int32>("ID"), reader.Get<string>("Username"), reader.Get<string>("LastAccessed"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    Console.WriteLine(ex.StackTrace);
                }

                foreach (int id in deleteIds)
                {
                    inventory = string.Format("DELETE FROM tsCharacter where Account = {0}", id);
                    if (purgePlayersConfig.debugPP)
                        TShock.DB.Query(inventory);

                    sql = string.Format("DELETE FROM users where ID = {0}", id);
                    if (purgePlayersConfig.debugPP)
                        Console.WriteLine(sql);
                    TShock.DB.Query(sql);
                    purgeCount++;
                }
                Console.WriteLine(purgeCount + " players purged");
                return;
            }
        }
    }
    #region application specific commands
    public class PurgePlayersArguments : InputArguments
    {
        public string Verbose
        {
            get { return GetValue("-verbose"); }
        }
        public string VerboseShort
        {
            get { return GetValue("-v"); }
        }

        public string Help
        {
            get { return GetValue("-help"); }
        }


        public PurgePlayersArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion

}
