using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using Fougerite.Events;
using UnityEngine;
using System.IO;

namespace PaperShopServer
{
    public class PaperShopServerClass : Fougerite.Module
    {
        public override string Name { get { return "PaperShop"; } }
        public override string Author { get { return "Salva/Juli"; } }
        public override string Description { get { return "Shop"; } }
        public override Version Version { get { return new Version("1.0"); } }

        public string red = "[color #B40404]";
        public string blue = "[color #81F7F3]";
        public string green = "[color #82FA58]";
        public string yellow = "[color #F4FA58]";
        public string orange = "[color #FF8000]";
        public string pink = "[color #FA58F4]";
        public string white = "[color #FFFFFF]";

        public bool addshopmode = false;

        public List<Vector3> EntityShops;
        public List<Fougerite.Player> PlayersInShop;

        public string PricesFile;
        public string ShopsFile;
        public string LogFile;

        public int MinutesToRefillShop = 60;

        public System.IO.StreamWriter file;
        private IniParser Settings;

        #region override
        public override void Initialize()
        {
            EntityShops = new List<Vector3>();
            PlayersInShop = new List<Fougerite.Player>();

            Hooks.OnServerLoaded += OnServerLoaded;
            Hooks.OnServerInit += OnServerInit;
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;

            Hooks.OnLootUse += OnLootUse;
            Hooks.OnItemRemoved += OnItemRemoved;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
            Hooks.OnEntityHurt += OnEntityHurt;
            Hooks.OnEntityDeployedWithPlacer += OnEntityDeployedWithPlacer;
            Hooks.OnResearch += OnResearch;
            Hooks.OnCrafting += OnCrafting;
            Hooks.OnItemPickup += OnItemPickUp;

            ReloadConfig();
        }
        public override void DeInitialize()
        {
            Hooks.OnServerLoaded -= OnServerLoaded;
            Hooks.OnServerInit -= OnServerInit;
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnLootUse -= OnLootUse;
            Hooks.OnItemRemoved -= OnItemRemoved;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;
            Hooks.OnEntityHurt -= OnEntityHurt;
            Hooks.OnEntityDeployedWithPlacer -= OnEntityDeployedWithPlacer;
            Hooks.OnResearch -= OnResearch;
            Hooks.OnCrafting -= OnCrafting;
            Hooks.OnItemPickup -= OnItemPickUp;
        }
        #endregion

        #region hooks
        public void OnServerInit()
        {
            ShopsFile = Path.Combine(ModuleFolder, "Shops.txt");
            PricesFile = Path.Combine(ModuleFolder, "Prices.txt");
            LogFile = Path.Combine(ModuleFolder, "Log.log");

            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            if (!File.Exists(ShopsFile))
            {
                File.Create(ShopsFile).Dispose();
                file = new System.IO.StreamWriter(ShopsFile, true);
                file.Close();
            }

            if (!File.Exists(PricesFile))
            {
                File.Create(PricesFile).Dispose();
                string[] linea = { "Revolver=5", 
                                     "Pipe Shotgun=5", 
                                     "9mm Pistol=10",
                                     "P250=15",
                                     "Shotgun=20",
                                     "MP5A4=25",
                                     "M4=30",
                                     "Bolt Action Rifle=50",
                                     "Explosive Charge=50",
                                     "Research Kit 1=10",
                                     "Supply Signal=100" };
                file = new System.IO.StreamWriter(PricesFile, true);
                foreach (var xx in linea)
                {
                    file.WriteLine(xx);
                }
                file.Close();
            }
        }
        public void OnServerLoaded()
        {
            ReloadShops();
            ReFillShops();
            Logger.Log("SHOP: Found (" + EntityShops.Count.ToString() + ") Shops!!");
            Timer1(MinutesToRefillShop * 60000, null).Start();
        }
        public void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (!player.Admin) { return; }
            if (cmd == "shop")
            {
                if (args.Length == 0)
                {
                    player.MessageFrom(Name, "/shop " + blue + " List of commands");
                    player.MessageFrom(Name, "/shop add " + blue + " Set new Shop");
                    player.MessageFrom(Name, "/shop reload " + blue + " Reload all shops from the config file");
                    player.MessageFrom(Name, "/shop refill " + blue + " Refill whit Items all Shops");
                    player.MessageFrom(Name, "/shop list " + blue + " List all shops locations");
                }
                else
                {
                    if (args[0] == "add")
                    {
                        addshopmode = true;
                        player.MessageFrom(Name, blue + "Hit some box to add new Shop");
                    }
                    else if (args[0] == "reload")
                    {
                        ReloadShops();
                        player.MessageFrom(Name, blue + "Shops Reloaded!");
                    }
                    else if (args[0] == "refill")
                    {
                        ReFillShops();
                        player.MessageFrom(Name, blue + "Shops Refilled!");
                    }
                    else if (args[0] == "list")
                    {
                        int count = 1;
                        foreach (var x in EntityShops)
                        {
                            player.MessageFrom(Name, count.ToString() + " - " + x.ToString());
                            count += 1;
                        }
                    }
                }
            }
        }
        public void OnPlayerConnected(Fougerite.Player pl)
        {
            PlayersInShop.RemoveAll(t => PlayersInShop.Contains(pl));
        }
        public void OnPlayerDisconnected(Fougerite.Player pl)
        {
            PlayersInShop.RemoveAll(t => PlayersInShop.Contains(pl));
        }
        public void OnItemRemoved(InventoryModEvent ir)
        {
            //ir.Player.Message("remove " + ir.Inventory.slotCount);

            if (IsPlayerInShop(ir.Player))
            {
                string ItemComprado = ir.ItemName;

                foreach (var line in File.ReadAllLines(PricesFile))
                {
                    if (ItemComprado == "Rock")
                    {
                        ir.Player.MessageFrom(Name, yellow + "You can´t buy a Rock");
                        ir.Cancel();
                        //ir.Player.Damage(20f);
                        DamagePlayer(ir.Player);
                        break;
                    }
                    string[] pares = line.ToString().Split(new char[] { '=' });
                    string item = pares[0];
                    int price = Convert.ToInt32(pares[1]);

                    if (ItemComprado == item)
                    {
                        //tiene dinero suficiente?
                        if (ir.Player.Inventory.HasItem("Paper", price))
                        {
                            ir.Player.MessageFrom(Name, green + "You have buy " + white + ItemComprado + green + " whit " + white + price + green + " Papers");
                            ir.Player.Inventory.RemoveItem("Paper", price);
                            int freeslots = ir.FInventory.FreeSlots;
                            ir.FInventory.AddItem("Rock", freeslots);

                            file = new System.IO.StreamWriter(LogFile, true);
                            file.WriteLine("[" + DateTime.Now.ToString() + "] " + ir.Player.Name + " has bought a " + ItemComprado + " for " + price + " papers");
                            file.Close();
                            Server.GetServer().BroadcastFrom(Name, ir.Player.Name + " has bought a " + ItemComprado + " for " + price + " papers");
                            break;
                        }
                        else
                        {
                            ir.Cancel();
                            //ir.Player.Notice(" ", "You don´t have enougth Papers :(", 5);
                            ir.Player.MessageFrom(Name, orange + "You don´t have enougth Papers " + white + ItemComprado + " (" + price + " Papers)");
                            //ir.Player.Damage(20f);
                            DamagePlayer(ir.Player);
                            break;
                        }
                    }
                }
            }

        }
        public void OnPlayerSpawned(Fougerite.Player pl, SpawnEvent se)
        {
            PlayersInShop.RemoveAll(t => PlayersInShop.Contains(pl));
        }
        public void OnEntityDeployedWithPlacer(Fougerite.Player pl, Fougerite.Entity e, Fougerite.Player actualplacer)
        {
            if (IsShopAround(e.Location))
            {
                actualplacer.MessageFrom(Name, red + "You cant place Storages closed to the Shop!!");
                e.Destroy();
            }
        }
        public void OnItemPickUp(ItemPickupEvent ipu)
        {
            foreach (var shoploc in EntityShops)
            {
                var dis = Util.GetUtil().GetVectorsDistance(ipu.Player.Location, shoploc);
                if (dis <= 10f)
                {

                    ipu.Player.Inventory.RemoveItem(ipu.Item.slot);
                    //ipu.Cancel();
                    ipu.Player.MessageFrom(Name, red + "You cant Pick Up Items closed to the Shop!!");
                    //ipu.Player.Damage(20f);
                    DamagePlayer(ipu.Player);
                    break;
                }
            }
        }
        public void OnLootUse(LootStartEvent le)
        {
            if (!le.IsObject)
            {
                return;
            }
            var player = (Fougerite.Player)le.Player;
            //if (le.Entity.Inventory.SlotCount == 36) //WoodBoxLarge tiene 36 slots
            if (le.Entity.Name == "WoodBoxLarge")
            {
                if (IsShop(le.Entity.Location))
                {
                    //Borrar mochilas alrededor
                    try
                    {
                        var objects = Physics.OverlapSphere(le.Entity.Location, 3f);
                        foreach (var x in objects)
                        {
                            if (x.name == "LootSack(Clone)")
                            {
                                Util.GetUtil().DestroyObject(x.gameObject);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Name + " ERROR A: " + ex.ToString());
                    }


                    ////

                    try
                    {
                        if (!PlayersInShop.Contains(player))
                        {
                            PlayersInShop.Add(player);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Name + " ERROR B: " + ex.ToString());
                        PlayersInShop.Add(player);
                    }
                    player.Notice("", "Welcome to the Shop!", 5f);

                    string mensajealcliente = "MensajeDeLaTienda-";
                    foreach (var line in File.ReadAllLines(PricesFile))
                    {
                        if (line == "")
                        {
                            continue;
                        }
                        mensajealcliente += "\n" + line;
                    }
                    player.SendConsoleMessage(mensajealcliente);
                }
                else
                {
                    try
                    {
                        PlayersInShop.RemoveAll(t => PlayersInShop.Contains(player));
                        //PlayersInShop.Remove(player);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Name + " ERROR C: " + ex.ToString());
                    }

                }
            }
        }
        public void OnEntityHurt(HurtEvent he)
        {
            var player = (Fougerite.Player)he.Attacker;
            if (player.Admin && addshopmode)
            {
                if (IsShop(he.Entity.Location))
                {
                    player.MessageFrom(Name, blue + "This Shop Already Exist!");
                    addshopmode = false;
                    return;
                }
                else
                {
                    player.MessageFrom(Name, blue + "Shop Added!");
                    AddShopMethod(he.Entity.Location);
                    SaveShopsToFile();
                    ReloadShops();
                    ReFillShops();
                    addshopmode = false;
                    return;
                }
            }
            if (IsShop(he.Entity.Location))
            {
                player.MessageFrom(Name, red + "You cant Damage the Shop!");
                he.DamageAmount = 0f;
            }
        }
        public void OnResearch(ResearchEvent re)
        {
            var item = re.ItemDataBlock;
            if (item.name == "Paper")
            {
                re.Cancel();
                re.Player.Notice("You can not Reseach the Paper");
            }
        }
        public void OnCrafting(CraftingEvent e)
        {
            var item = e.ResultItem;
            if (item.name == "Paper")
            {
                e.Cancel();
                e.Player.Notice("You can not Craft the Paper");
            }
        }
        #endregion

        #region methods
        private void ReloadConfig()
        {
            if (!File.Exists(Path.Combine(ModuleFolder, "Settings.ini")))
            {
                File.Create(Path.Combine(ModuleFolder, "Settings.ini")).Dispose();
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                Settings.AddSetting("Timer", "MinutesToRefillShop", "60");
                Logger.Log(Name + ": New Settings File Created!");
                Settings.Save();
                ReloadConfig();
            }
            else
            {
                Settings = new IniParser(Path.Combine(ModuleFolder, "Settings.ini"));
                if (Settings.ContainsSetting("Timer", "MinutesToRefillShop"))
                {
                    try
                    {
                        MinutesToRefillShop = int.Parse(Settings.GetSetting("Timer", "MinutesToRefillShop"));
                        Logger.Log(Name + ": Settings file Loaded!");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(Name + ": Detected a problem in the configuration");
                        Logger.Log("ERROR -->" + ex.Message);
                        File.Delete(Path.Combine(ModuleFolder, "Settings.ini"));
                        Logger.LogError(Name + ": Deleted the old configuration file");
                        ReloadConfig();
                    }
                }
                else
                {
                    Logger.LogError(Name + ": Detected a problem in the configuration (lost key)");
                    File.Delete(Path.Combine(ModuleFolder, "Settings.ini"));
                    Logger.LogError(Name + ": Deleted the old configuration file");
                    ReloadConfig();
                }
                return;
            }
        }
        public void SaveShopsToFile()
        {
            File.Delete(ShopsFile);
            File.Create(ShopsFile).Dispose();
            foreach (var shop in EntityShops)
            {
                file = new System.IO.StreamWriter(ShopsFile, true);
                file.WriteLine(shop);
                file.Close();
            }
            return;
        }
        public void ReloadShops()
        {
            EntityShops.Clear();
            foreach (string linea in File.ReadAllLines(ShopsFile))
            {
                if (linea == "")
                {
                    continue;
                }
                Vector3 shoploc = Util.GetUtil().ConvertStringToVector3(linea);
                foreach (Fougerite.Entity xx in Util.GetUtil().FindDeployablesAround(shoploc, 0.5f))
                {
                    AddShopMethod(xx.Location);
                }
            }
            return;
        }
        public void ReFillShops()
        {
            Loom.QueueOnMainThread(() =>
            {
                Hooks.OnItemRemoved -= OnItemRemoved;

                try
                {
                    foreach (var shoploc in EntityShops)
                    {
                        foreach (Fougerite.Entity xx in Util.GetUtil().FindDeployablesAround(shoploc, 0.5f))
                        {
                            xx.Inventory.ClearAll();
                            foreach (var line in File.ReadAllLines(PricesFile))
                            {
                                string[] pares = line.ToString().Split(new char[] { '=' });
                                string item = pares[0];
                                xx.Inventory.AddItem(item);
                            }

                            int freeslots = xx.Inventory.FreeSlots;
                            xx.Inventory.AddItem("Rock", freeslots);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("SHOP ERROR");
                    Hooks.OnItemRemoved += OnItemRemoved;

                }

                Hooks.OnItemRemoved += OnItemRemoved;
                return;
            });
        }
        public void AddShopMethod(Vector3 ent)
        {
            EntityShops.Add(ent);
            return;
        }
        public bool IsShop(Vector3 ent)
        {
            if (EntityShops.Contains(ent))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsShopAround(Vector3 ent)
        {
            bool resultado = false;
            foreach (var shoploc in EntityShops)
            {
                var dis = Util.GetUtil().GetVectorsDistance(shoploc, ent);
                if (dis <= 10f)
                {
                    resultado = true;
                    break;
                }
                else
                {
                    resultado = false;
                    break;
                }
            }
            if (resultado)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsPlayerInShop(Fougerite.Player pl)
        {
            if (PlayersInShop.Contains(pl))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void DamagePlayer(Fougerite.Player pl)
        {
            if (pl.Health <= 10f)
            {
                pl.TeleportTo(pl.Location.x + 15f, pl.Location.y + 300f, pl.Location.z + 15f);
                pl.MessageFrom(Name, blue + "========================");
                pl.MessageFrom(Name, yellow + "DO NOT SPAM in the Shop");
                pl.MessageFrom(Name, red + "You get Killed !!");
                pl.MessageFrom(Name, blue + "========================");
            }
            else
            {
                pl.Damage(10f);
                pl.MessageFrom(Name, red + "Penality " + blue + " -10 Healt");
            }
            return;

        }
        #endregion

        #region timers
        public TimedEvent Timer1(int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += CallBack;
            return timedEvent;
        }
        public void CallBack(TimedEvent e)
        {
            e.Kill();
            Server.GetServer().BroadcastFrom(Name, green + "All the SHOPS have been filled, next fill will be in " + white + MinutesToRefillShop + "mins.");
            Timer1(MinutesToRefillShop * 60000, null).Start();
            ReFillShops();

        }
        #endregion
    }
}
