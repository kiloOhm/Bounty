// Requires: GUICreator

#define DEBUG

using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("bounties", "OHM & Bunsen", "2.0.0")]
    [Description("RP Bounty Hunting")]
    partial class bounties : RustPlugin
    {
        private static Plugins.bounties PluginInstance;

        public bounties()
        {
            PluginInstance = this;
        }

        #region helpers

        public string lastSeen(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder("last seen ");
            string grid = getGrid(player.transform.position);
            if (!string.IsNullOrEmpty(grid)) sb.Append($"in {grid} ");
            sb.Append("wearing: ");
            if (player.inventory.containerWear.itemList.Count == 0) sb.Append("nothing");
            else
            {
                int i = 1;
                foreach (Item item in player.inventory.containerWear.itemList)
                {
                    sb.Append($"{item.info.displayName.english}");
                    if (i != player.inventory.containerWear.itemList.Count) sb.Append(", ");
                    i++;
                }
            }
            return sb.ToString();
        }

        public string wearing(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder();
            if (player.inventory.containerWear.itemList.Count == 0) sb.Append("nothing");
            else
            {
                int i = 1;
                foreach (Item item in player.inventory.containerWear.itemList)
                {
                    sb.Append($"{item.info.displayName.english}");
                    if (i != player.inventory.containerWear.itemList.Count) sb.Append(", ");
                    i++;
                }
            }
            return sb.ToString();
        }

        public string armedWith(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder();
            List<HeldEntity> heldEntities = new List<HeldEntity>();
            foreach (Item item in player.inventory.containerBelt.itemList.Concat(player.inventory.containerMain.itemList))
            {
                if (!item.info.isHoldable || item.GetHeldEntity() == null) continue;
                heldEntities.Add(item.GetHeldEntity() as HeldEntity);
            }
            if (heldEntities.Count == 0) sb.Append("unarmed");
            else if (heldEntities.Count == 1) sb.Append($"armed with {heldEntities[0].GetItem().info.displayName.english}");
            else
            {
                heldEntities = (from x in heldEntities orderby x.hostileScore descending select x).ToList();
                sb.Append($"armed with {heldEntities[0].GetItem().info.displayName.english}");
            }
            return sb.ToString();
        }

        string getGrid(Vector3 pos)
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f) - 1) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z}";
        }

        public BasePlayer findPlayer(string name, BasePlayer player)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ulong id;
            ulong.TryParse(name, out id);
            List<BasePlayer> results = BasePlayer.allPlayerList.Where((p) => p.displayName.Contains(name, System.Globalization.CompareOptions.IgnoreCase) || p.userID == id).ToList();
            if (results.Count == 0)
            {
                if (player != null) player.ChatMessage(lang.GetMessage("noPlayersFound", this, player.UserIDString));
            }
            else if (results.Count == 1)
            {
                return results[0];
            }
            else if (player != null)
            {
                player.ChatMessage(lang.GetMessage("multiplePlayersFound", this, player.UserIDString));
                int i = 1;
                foreach (BasePlayer p in results)
                {
                    player.ChatMessage($"{i}. {p.displayName}[{p.userID}]");
                    i++;
                }
            }
            return null;
        }

        public Item findItemByUID(PlayerInventory inventory, uint uid)
        {
            Item output = null;
            output = inventory.containerBelt.FindItemByUID(uid);
            if (output != null) return output;
            output = inventory.containerMain.FindItemByUID(uid);
            if (output != null) return output;
            output = inventory.containerWear.FindItemByUID(uid);
            if (output != null) return output;
            return null;
        }

        #endregion
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using System;
    using UnityEngine;

    partial class bounties : RustPlugin
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Bounty
        {
            [JsonProperty(PropertyName = "Note Item UID")]
            public uint noteUid;

            [JsonProperty(PropertyName = "Timestamp")]
            public DateTime timestamp;

            [JsonProperty(PropertyName = "Placer name")]
            public string placerName;

            [JsonProperty(PropertyName = "Placer ID")]
            public ulong placerID;

            public BasePlayer placer => BasePlayer.FindByID(placerID);

            [JsonProperty(PropertyName = "Target name")]
            public string targetName;

            [JsonProperty(PropertyName = "Target ID")]
            public ulong targetID;

            public BasePlayer target => BasePlayer.FindByID(targetID);

            [JsonProperty(PropertyName = "Reward amount")]
            public int rewardAmount;

            public Item reward
            {
                get
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    if (itemDef == null) return null;
                    return ItemManager.Create(itemDef, rewardAmount);
                }
            }

            public string reason;

            public string text
            {
                get
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    return string.Format(PluginInstance.lang.GetMessage("bountyText", PluginInstance), targetName, reason ?? $"disrespecting {placerName}", $"{rewardAmount} {itemDef?.displayName?.english ?? "$"}", PluginInstance.getGrid(target.transform.position), PluginInstance.wearing(target), PluginInstance.armedWith(target));
                }
            }

            public Bounty(BasePlayer placer, BasePlayer target, int reward, string reason)
            {
                timestamp = DateTime.Now;
                placerID = placer.userID;
                placerName = placer.displayName;
                targetID = target.userID;
                targetName = target.displayName;
                rewardAmount = reward;
                this.reason = reason;

                noteUid = giveNote(placer);

                BountyData.AddBounty(this);
            }

            public Bounty() { }

            public uint giveNote(BasePlayer player)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition("note");
                Item item = ItemManager.Create(itemDef, 1);
                item.SetFlag(global::Item.Flag.OnFire, true);
                item.text = text;
                item.name = "Bounty";
                player.GiveItem(item);
                return item.uid;
            }

            public Hunt startHunt(BasePlayer hunter) => new Hunt(this, hunter);

        }

        private class PortableBounty : MonoBehaviour
        {
            public Bounty bounty;
        }
    }
}﻿namespace Oxide.Plugins
{
    using UnityEngine;

    partial class bounties : RustPlugin
    {
        partial void initCommands()
        {
            cmd.AddChatCommand("bounty", this, nameof(bountyCommand));
            cmd.AddChatCommand("test", this, nameof(testCommand));
            cmd.AddConsoleCommand("bounties.test", this, nameof(consoleTestCommand));
        }

        private void bountyCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IPlayer.HasPermission("bounties.use"))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 0) return;
            switch (args[0])
            {
                case "add":
                case "place":
                    //bounty add name reward reason
                    if (args.Length != 4)
                    {
                        player.ChatMessage(lang.GetMessage("usageAdd", this, player.UserIDString));
                        return;
                    }
                    BasePlayer target = findPlayer(args[1], player);
                    int reward = 0;
                    int.TryParse(args[2], out reward);
                    if (reward < config.minReward)
                    {
                        player.ChatMessage(string.Format(lang.GetMessage("minReward", this, player.UserIDString), config.minReward));
                        return;
                    }
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    if (player.inventory.GetAmount(itemDef.itemid) < reward)
                    {
                        player.ChatMessage(string.Format(lang.GetMessage("notEnough", this, player.UserIDString), itemDef.displayName.english));
                    }
                    player.inventory.Take(null, itemDef.itemid, reward);
                    Bounty bounty = new Bounty(player, target, reward, args[3]);
                    break;
            }
        }

        private void testCommand(BasePlayer player, string command, string[] args)
        {
            GetPlayerSummary(ulong.Parse(args[0]), (ps) => 
            {
                if (ps == null) return;
                player.ChatMessage(ps.personaname);
            });
        }

        private void consoleTestCommand(ConsoleSystem.Arg arg)
        {
            GetPlayerSummary(ulong.Parse(arg.Args[0]), (ps) =>
            {
                if (ps == null) return;
                SendReply(arg, ps.personaname);
            });
        }
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;

    partial class bounties : RustPlugin
    {
        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Steam API key")]
            public string steamAPIKey;

            [JsonProperty(PropertyName = "Currency Item shortname")]
            public string currency;

            [JsonProperty(PropertyName = "Minimum reward amount")]
            public int minReward;

            [JsonProperty(PropertyName = "Hunting cooldown in seconds")]
            public int targetCooldown;

            [JsonProperty(PropertyName = "Hunt Duration")]
            public int huntDuration;

            [JsonProperty(PropertyName = "Pay out reward to target if hunter dies")]
            public bool targetPayout;

            [JsonProperty(PropertyName = "Broadcast hunt conclusion in global chat")]
            public bool broadcastHunt;
        }

        private ConfigData getDefaultConfig()
        {
            return new ConfigData
            {
                steamAPIKey = "",
                currency = "scrap",
                minReward = 100,
                targetCooldown = 7200,
                huntDuration = 7200,
                targetPayout = true,
                broadcastHunt = true
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                Puts("Config data is corrupted, replacing with default");
                config = new ConfigData();
            }

            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig() => config = getDefaultConfig();
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using Oxide.Core;
    using Oxide.Core.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    /*
     Make sure that you're not saving complex classes like BasePlayer or Item. Try to stick with primitive types.
     If you're saving your own classes, make sure they have a default constructor and that all properties you're saving are public.
     Take control of which/how properties get serialized by using the Newtonsoft.Json Attributes https://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    */

    partial class bounties : RustPlugin
    {
        partial void initData()
        {
            BountyData.init();
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class BountyData
        {
            private static DynamicConfigFile bountyDataFile;
            private static BountyData instance;
            private static bool initialized = false;

            [JsonProperty(PropertyName = "Bounty list")]
            public Dictionary<uint, Bounty> bounties = new Dictionary<uint, Bounty>();

            public BountyData()
            {
            }

            public static Bounty GetBounty(uint itemID)
            {
                if (!initialized) init();
                if (!instance.bounties.ContainsKey(itemID)) return null;
                return instance.bounties[itemID];
            }

            public static void AddBounty(Bounty bounty)
            {
                if (!initialized) init();
                instance.bounties.Add(bounty.noteUid, bounty);
                save();
#if DEBUG
                PluginInstance.PrintToChat($"added Bounty {bounty.placerName} -> {bounty.targetName} {bounty.rewardAmount} to data");
#endif
            }

            public static void removeBounty(uint itemID)
            {
                if (!initialized) init();
                instance.bounties.Remove(itemID);
                save();
#if DEBUG
                PluginInstance.PrintToChat($"removed Bounty {itemID} from data");
#endif
            }

            public static void init()
            {
                if (initialized) return;
                bountyDataFile = Interface.Oxide.DataFileSystem.GetFile("bounties/Bounties");
                load();
                initialized = true;
            }

            public static void save()
            {
                if (!initialized) init();
                try
                {
                    bountyDataFile.WriteObject(instance);
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"saving {typeof(BountyData).Name} failed. Are you trying to save complex classes like BasePlayer or Item? that won't work!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }

            public static void load()
            {
                try
                {
                    instance = bountyDataFile.ReadObject<BountyData>();
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"loading {typeof(BountyData).Name} failed. Make sure that all classes you're saving have a default constructor!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class HuntData
        {
            private static DynamicConfigFile huntDataFile;
            private static HuntData instance;
            private static bool initialized = false;

            [JsonProperty(PropertyName = "Hunt list")]
            public List<Hunt> hunts = new List<Hunt>();

            public HuntData()
            {
            }

            public static Hunt getHunt(Bounty bounty)
            {
                if (!initialized) init();
                return instance.hunts.Where((h) => h.bounty == bounty).First();
            }

            public static void addHunt(Hunt hunt)
            {
                if (!initialized) init();
                instance.hunts.Add(hunt);
                save();
            }

            public static void removeHunt(Hunt hunt)
            {
                if (!initialized) init();
                instance.hunts.Remove(hunt);
                save();
            }

            public static void init()
            {
                if (initialized) return;
                huntDataFile = Interface.Oxide.DataFileSystem.GetFile("bounties/Hunts");
                load();
                initialized = true;
            }

            public static void save()
            {
                if (!initialized) init();
                try
                {
                    huntDataFile.WriteObject(instance);
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"saving {typeof(BountyData).Name} failed. Are you trying to save complex classes like BasePlayer or Item? that won't work!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }

            public static void load()
            {
                try
                {
                    instance = huntDataFile.ReadObject<HuntData>();
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"loading {typeof(BountyData).Name} failed. Make sure that all classes you're saving have a default constructor!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }
        }
    }
}﻿namespace Oxide.Plugins
{
    using UnityEngine;
    using static Oxide.Plugins.GUICreator;

    partial class bounties : RustPlugin
    {
        partial void initGUI()
        {
            guiCreator = (GUICreator)Manager.GetPlugin("GUICreator");
        }

        private GUICreator guiCreator;

        #region bounty creator

        public void sendCreator(BasePlayer player)
        {

        }

        #endregion

        public void sendBounty(BasePlayer player, Bounty bounty)
        {
#if DEBUG
            player.ChatMessage($"sendBounty: {bounty.placerName} -> {bounty.targetName}");
#endif
        }

        public void closeBounty(BasePlayer player)
        {
            //for bounty and creator

        }

        public void sendHunterIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendHunterIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
        }

        public void sendTargetIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendTargetIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
        }

        public void closeIndicators(BasePlayer player)
        {

        }
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using System;

    partial class bounties : RustPlugin
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Hunt
        {
            [JsonProperty(PropertyName = "Timestamp")]
            public DateTime timestamp;

            [JsonProperty(PropertyName = "Bounty")]
            public Bounty bounty;

            [JsonProperty(PropertyName = "Hunter ID")]
            public ulong hunterID;

            [JsonProperty(PropertyName = "Hunter name")]
            public string hunterName;

            public BasePlayer hunter => BasePlayer.FindByID(hunterID);

            public BasePlayer target => BasePlayer.FindByID(bounty.targetID);

            public TimeSpan remaining
            {
                get
                {
                    TimeSpan duration = new TimeSpan(0, 0, config.huntDuration);
                    TimeSpan elapsed = DateTime.Now.Subtract(timestamp);
                    return duration - elapsed;
                }
            }

            public Timer huntTimer;

            public Timer ticker;

            public Hunt()
            {
                TimeSpan remainingCache = remaining;
                if (remainingCache <= TimeSpan.Zero) end();
                else
                {
                    huntTimer = PluginInstance.timer.Once((float)remainingCache.TotalSeconds, () => end());
                    ticker = PluginInstance.timer.Every(1, () => tick());
                }
            }

            public Hunt(Bounty bounty, BasePlayer hunter)
            {
                timestamp = DateTime.Now;
                this.bounty = bounty;
                hunterID = hunter.userID;
                hunterName = hunter.displayName;
                huntTimer = PluginInstance.timer.Once((float)config.huntDuration, () => end());
                ticker = PluginInstance.timer.Every(1, () => tick());
            }

            public void tick()
            {
                PluginInstance.sendHunterIndicator(hunter, this);
                PluginInstance.sendTargetIndicator(target, this);
            }

            public void end(BasePlayer winner = null)
            {
#if DEBUG
                PluginInstance.PrintToChat($"ending hunt {hunterName} -> {bounty.targetName}, winner: {winner?.displayName ?? "null"}");
#endif
                if (winner == hunter)
                {
                    //msg
                    //payout
                }
                else if (winner == target)
                {
                    //msg
                    //payout/return
                }
                else
                {
                    //msg
                }

                huntTimer.Destroy();
                ticker.Destroy();
                HuntData.removeHunt(this);
            }
        }
    }
}﻿namespace Oxide.Plugins
{
    using System.Collections.Generic;

    partial class bounties : RustPlugin
    {
        partial void initLang()
        {
            lang.RegisterMessages(messages, this);
        }

        private Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"noPermission", "You don't have permission to use this command!" },
            {"noPlayersFound", "No players found!" },
            {"multiplePlayersFound", "Multiple players found!" },
            {"usageAdd", "/bounty add (target name) (reward amount) \"(reason)\""},
            {"minReward", "The reward has to be at least {0}!" },
            {"notEnough", "You don't have enough {0}!" },
            {"bountyText", "WANTED! DEAD OR ALIVE!\n{0}\n{1}\nREWARD: {2}\nlast seen in {3}, wearing {4}, {5}" },
            {"targetDeadBroadcast", "<size=30>{0} claims the {1} bounty on {2}'s head!</size>" },
            {"apiKey", "Please enter your steam API key in the config file. Get yours at https://steamcommunity.com/dev/apikey" }
        };
    }
}﻿namespace Oxide.Plugins
{
    using UnityEngine;

    partial class bounties : RustPlugin
    {
        partial void initData();

        partial void initCommands();

        partial void initLang();

        partial void initPermissions();

        partial void initGUI();

        private void Loaded()
        {
            initData();
            initCommands();
            initLang();
            initPermissions();
            initGUI();
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null) return null;
            //note.update UID Content
            BasePlayer player = arg.Player();
            if (player == null) return null;
            if (arg.cmd.FullName == "note.update")
            {
#if DEBUG
                player.ChatMessage($"note.update {arg.FullString}");
#endif
                Item note = findItemByUID(player.inventory, uint.Parse(arg.Args[0]));
                if (note == null) return null;
                Bounty bounty = BountyData.GetBounty(note.uid);
                if (bounty == null) return null;
                timer.Once(0.2f, () =>
                {
                    note.text = bounty.text;
                    note.MarkDirty();
                });
            }
            return null;
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            WorldItem wItem = item.GetWorldEntity() as WorldItem;
            if (wItem.item == null) return null;
            if (item.info.shortname == "note")
            {
                PortableBounty pBounty = wItem.gameObject.GetComponent<PortableBounty>();
                if (pBounty != null)
                {
                    Bounty bounty = pBounty.bounty;
                    if (bounty == null)
                    {
                        Puts($"pBounty.bounty on Note[{item.uid}] is null. This shouldn't happen!");
                        return null;
                    }
                    BountyData.AddBounty(bounty);
                }
            }

            return null;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.info.shortname != "note") return;
            if (!item.HasFlag(global::Item.Flag.OnFire)) return;
            Bounty bounty = BountyData.GetBounty(item.uid);
            if (bounty == null) return;

            //attach portableBounty
            WorldItem wItem = entity as WorldItem;
            PortableBounty pBounty = wItem.gameObject.AddComponent<PortableBounty>();
            pBounty.bounty = bounty;

            //remove Bounty from Data
            BountyData.removeBounty(item.uid);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            //display tooltip "put the bounty into your hotbar and select it to start hunting"
            return null;
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null) return null;
            if (item.info.shortname != "note") return null;
            if (!item.HasFlag(global::Item.Flag.OnFire)) return null;
            Bounty bounty = BountyData.GetBounty(item.uid);
            if (bounty == null) return null;

            item.text = bounty.text;
            item.MarkDirty();

            return null;
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null) return;
            if (oldItem != null)
            {
                if (oldItem?.info?.shortname == "note" && oldItem.HasFlag(global::Item.Flag.OnFire))
                {
                    Bounty bounty = BountyData.GetBounty(oldItem.uid);
                    if (bounty != null)
                    {
                        closeBounty(player);
                    }
                }
            }
            if (newItem != null)
            {
                if (newItem?.info?.shortname == "note" && newItem.HasFlag(global::Item.Flag.OnFire))
                {
                    Bounty bounty = BountyData.GetBounty(newItem.uid);
                    if (bounty != null)
                    {
                        sendBounty(player, bounty);
                    }
                }
            }
        }
    }
}﻿namespace Oxide.Plugins
{
    using System;

    partial class bounties : RustPlugin
    {
        //permissions will be (lowercase class name).(perm)
        partial void initPermissions()
        {
            foreach (string perm in Enum.GetNames(typeof(permissions)))
            {
                permission.RegisterPermission($"{PluginInstance.Name}.{perm}", this);
            }
        }

        private enum permissions
        {
            use
        }

        private bool hasPermission(BasePlayer player, permissions perm)
        {
            return player.IPlayer.HasPermission($"{PluginInstance.Name}.{Enum.GetName(typeof(permissions), perm)}");
        }

        private void grantPermission(BasePlayer player, permissions perm)
        {
            player.IPlayer.GrantPermission($"{PluginInstance.Name}.{Enum.GetName(typeof(permissions), perm)}");
        }

        private void revokePermission(BasePlayer player, permissions perm)
        {
            player.IPlayer.RevokePermission($"{PluginInstance.Name}.{Enum.GetName(typeof(permissions), perm)}");
        }
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using Oxide.Core.Libraries;
    using System;

    partial class bounties : RustPlugin
    {
        public class WebResponse
        {
            public SteamUser response;
        }
        
        public class SteamUser
        {
            public PlayerSummary[] players;
        }

        public class PlayerSummary
        {
            public string steamid;
            public string personaname;
            public string profileurl;
            public string avatarfull;
        }

        public void GetPlayerSummary(ulong steamID, Action<PlayerSummary> callback)
        {
            if(string.IsNullOrEmpty(config.steamAPIKey))
            {
                Puts(lang.GetMessage("apiKey", this));
                return;
            }
            string url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=" + config.steamAPIKey.ToString() + "&steamids=" + steamID;
            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts($"Couldn't get an answer from Steam!");
                    return;
                }
                WebResponse webResponse = JsonConvert.DeserializeObject<WebResponse>(response);
                if (webResponse?.response?.players == null)
                {
                    Puts("response is null");
                    callback(null);
                    return;
                }
                if(webResponse.response.players.Length == 0)
                {
                    Puts("response has no playerSummaries");
                    callback(null);
                    return;
                }
#if DEBUG
                Puts($"Got PlayerSummary: {webResponse?.response?.players[0]?.personaname ?? "null"} for steamID [{steamID}]");
#endif
                callback(webResponse.response.players[0]);
            }, this, RequestMethod.GET);
        }
    }
}