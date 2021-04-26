using System;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace JGRAN_Plugin
{
    [ApiVersion(2, 1)]
    public class JGRAN_Plugin : TerrariaPlugin
    {
        public override string Author => "Rondeo Balos";
        public override string Description => "A random giveaway reward plugin";
        public override string Name => "JGRAN Reward Plugin";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public JGRAN_Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInit);
            ServerApi.Hooks.ServerJoin.Register(this, onJoin);
            ServerApi.Hooks.ServerLeave.Register(this, onLeave);
            ServerApi.Hooks.GamePostInitialize.Register(this, onPostInit);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, onInit);
                ServerApi.Hooks.ServerJoin.Deregister(this, onJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, onPostInit);
            }
            base.Dispose(disposing);
        }

        private ArrayList online = new ArrayList();
        private Config config;
        private Random random = new Random(DateTime.Now.Millisecond);
        private XmlSerializer serializer = new XmlSerializer(typeof(Config));
        private StreamReader file;
        private StreamWriter wfile;

        private void loadConfig()
        {
            file = new StreamReader("JGRAN_Plugin.xml");
            config = (Config)serializer.Deserialize(file);
            file.Close();
        }

        private void saveConfig()
        {
            wfile = new StreamWriter("JGRAN_Plugin.xml");
            serializer.Serialize(wfile,config);
            wfile.Close();
        }

        void onJoin(JoinEventArgs args) => online.Add(args.Who);

        void onLeave(LeaveEventArgs args) => online.Remove(args.Who);

        void onPostInit(EventArgs args)
        {
            
            
            TSPlayer player;
            int item;
            Console.WriteLine("Starting Giving Random Items...");
            Task.Run(async () => {
                for (; ; )
                {
                    await Task.Delay(config.interval);
                    //Console.WriteLine($"{online.Count} Online Players...");
                    TShock.Utils.Broadcast("Giving random Item to online players. Let's Go!", Color.Green);
                    foreach (int index in online)
                    {
                        player = new TSPlayer(index);
                        item = config.items[random.Next(0, config.items.Length)];
                        player.GiveItem(item, random.Next(1, 10));
                        //Console.WriteLine($"Item Given to {player.Name}...");
                        player.SendSuccessMessage($"You have recieved a random Item [{TShock.Utils.GetItemById(item).Name}]. Yehey!");
                    }
                }
            });

        }

        void onInit(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("tshock.admin.reward", rewardCommand, "reward") { 
                HelpText = "JGRAN Reward Plugin"
            });
            loadConfig();
        }

        void rewardCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("Invalid Command!");
                return;
            }
            var token = args.Parameters[0];
            switch (token)
            {
                case "add":
                    int item_id;
                    if (Int32.TryParse(args.Parameters[1], out item_id))
                    {
                        Item iteminfo = TShock.Utils.GetItemById(item_id);
                        if ( iteminfo != null)
                        {
                            List<int> temp_items = new List<int>();
                            for (int i = 0; i < config.items.Length; i++)
                                temp_items.Add(config.items[i]);
                            temp_items.Add(item_id);
                            config.items = temp_items.ToArray();
                            saveConfig();
                            args.Player.SendSuccessMessage($"Item {iteminfo.Name} successfully added!");
                            //Console.WriteLine($"Item {iteminfo.Name} successfully added!");
                        }
                        else
                            args.Player.SendErrorMessage("Item Not Found!");
                    }else
                        args.Player.SendErrorMessage("Invalid Item ID!");
                    break;
                case "remove":
                    break;
                case "interval":
                    int interval;
                    if (Int32.TryParse(args.Parameters[1], out interval))
                    {
                        config.interval = interval;
                        saveConfig();
                        args.Player.SendSuccessMessage($"Interval has been set to {interval}!");
                        //Console.WriteLine($"Interval has been set to {interval}!");
                    }
                    else
                        args.Player.SendErrorMessage("Please input a number");
                    break;
            }
        }

    }
}
