#define DEBUG

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Oxide.Plugins.GUICreator;

namespace Oxide.Plugins
{
    [Info("bounties", "OHM", "2.0.0")]
    [Description("Template")]
    class bounties : RustPlugin
    {
        #region global
        private static bounties Instance = null;

        public bounties()
        {
            Instance = this;
        }

        DynamicConfigFile bountyDataFile;
        DynamicConfigFile huntDataFile;
        BountyData bountyData;
        HuntData huntData;
        #endregion

        #region references
        private GUICreator guiCreator;
        #endregion

        #region classes

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
                    return string.Format(Instance.lang.GetMessage("bountyText", Instance), targetName, reason ?? $"disrespecting {placerName}", $"{rewardAmount} {itemDef?.displayName?.english ?? "$"}");
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

                ItemDefinition itemDef = ItemManager.FindItemDefinition("note");
                Item item = ItemManager.Create(itemDef, 1);
                item.SetFlag(global::Item.Flag.OnFire, true);
                item.text = this.text;
                item.name = "Bounty";
                placer.GiveItem(item);

                noteUid = item.uid;

                Instance.bountyData.AddBounty(this);
            }

            public Bounty() { }
        }

        private class PortableBounty:MonoBehaviour
        {
            public Bounty bounty;
        }

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
                    huntTimer = Instance.timer.Once((float)remainingCache.TotalSeconds, () => end());
                    ticker = Instance.timer.Every(1, () => tick());
                }
            }

            public Hunt(Bounty bounty, BasePlayer hunter)
            {
                timestamp = DateTime.Now;
                this.bounty = bounty;
                hunterID = hunter.userID;
                hunterName = hunter.displayName;
                huntTimer = Instance.timer.Once((float)config.huntDuration, () => end());
                ticker = Instance.timer.Every(1, () => tick());
            }

            public void tick()
            {

            }

            public void end()
            {
#if DEBUG
                Instance.PrintToChat($"ending hunt {hunterName} -> {bounty.targetName}");
#endif
                //announcement
                //return item
                huntTimer.Destroy();
                ticker.Destroy();
                Instance.huntData.removeHunt(this);
            }
        }

        #endregion

        #region oxide hooks
        void Init()
        {
            permission.RegisterPermission("bounties.use", this);
            bountyDataFile = Interface.Oxide.DataFileSystem.GetFile("bounties/Bounties");
            huntDataFile = Interface.Oxide.DataFileSystem.GetFile("bounties/Hunts");
            loadBountyData();
            loadHuntData();
        }

        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            cmd.AddChatCommand("bounty", this, nameof(bountyCommand));

            guiCreator = (GUICreator)Manager.GetPlugin("GUICreator");
            if (guiCreator == null)
            {
                Puts("GUICreator missing! This shouldn't happen");
                return;
            }
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null) return null;
            //note.update UID Content
            BasePlayer player = arg.Player();
            if (player == null) return null;
            if (arg.cmd.FullName == "note.update")
            {
                player.ChatMessage($"note.update {arg.FullString}");
                Item note = findItemByUID(player.inventory, uint.Parse(arg.Args[0]));
                if (note == null) return null;
                player.ChatMessage("note found");
                Bounty bounty = bountyData.GetBounty(note.uid);
                if (bounty == null) return null;
                player.ChatMessage("bounty found");
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
                    bountyData.AddBounty(bounty);
                }
            }

            return null;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.info.shortname != "note") return;
            if (!item.HasFlag(global::Item.Flag.OnFire)) return;
            Bounty bounty = bountyData.GetBounty(item.uid);
            if (bounty == null) return;
            
            //attach portableBounty
            WorldItem wItem = entity as WorldItem;
            PortableBounty pBounty = wItem.gameObject.AddComponent<PortableBounty>();
            pBounty.bounty = bounty;

            //remove Bounty from Data
            bountyData.removeBounty(item.uid);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            //display tooltip "put the bounty into your hotbar and select it to start hunting"
            return null;
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (oldItem.info.shortname == "note" && oldItem.HasFlag(global::Item.Flag.OnFire))
            {
                Bounty bounty = bountyData.GetBounty(oldItem.uid);
                if(bounty != null)
                {
                    closeBounty(player);
                }
            }
            if (newItem.info.shortname == "note" && newItem.HasFlag(global::Item.Flag.OnFire))
            {
                Bounty bounty = bountyData.GetBounty(oldItem.uid);
                if (bounty != null)
                {
                    sendBounty(player, bounty);
                }
            }
        }
        #endregion

        #region commands
        //see Loaded() hook
        private void bountyCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IPlayer.HasPermission("bounties.use"))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 0) return;
            switch(args[0])
            {
                case "add":
                case "place":
                    //bounty add name reward reason
                    if(args.Length != 4)
                    {
                        player.ChatMessage(lang.GetMessage("usageAdd", this, player.UserIDString));
                        return;
                    }
                    BasePlayer target = findPlayer(args[1], player);
                    int reward = 0;
                    int.TryParse(args[2], out reward);
                    if(reward < config.minReward)
                    {
                        player.ChatMessage(string.Format(lang.GetMessage("minReward", this, player.UserIDString), config.minReward));
                        return;
                    }
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    if (player.inventory.GetAmount(itemDef.itemid) < reward)
                    {
                        player.ChatMessage(string.Format(lang.GetMessage("notEnough", this, player.UserIDString),itemDef.displayName.english));
                    }
                    player.inventory.Take(null, itemDef.itemid, reward);
                    Bounty bounty = new Bounty(player, target, reward, args[3]);
                    break;
            }
        }
        #endregion

        #region UI

        #region bounty creator

        public void sendCreator(BasePlayer player)
        {

        }

        #endregion

        public void sendBounty(BasePlayer player, Bounty bounty)
        {

        }

        public void closeBounty(BasePlayer player)
        {
            //for bounty and creator

        }

        public void sendHunterIndicator(BasePlayer player, Bounty bounty)
        {

        }

        public void sendTargetIndicator(BasePlayer player, Bounty bounty)
        {

        }

        public void closeIndicators(BasePlayer player)
        {
            
        }

        #endregion

        #region helpers

        public BasePlayer findPlayer(string name, BasePlayer player)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ulong id;
            ulong.TryParse(name, out id);
            List<BasePlayer> results = BasePlayer.allPlayerList.Where((p) => p.displayName.Contains(name, System.Globalization.CompareOptions.IgnoreCase) || p.userID == id).ToList();
            if(results.Count == 0)
            {
                if (player != null) player.ChatMessage(lang.GetMessage("noPlayersFound", this, player.UserIDString));
            }
            else if(results.Count == 1)
            {
                return results[0];
            }
            else if (player != null)
            {
                player.ChatMessage(lang.GetMessage("multiplePlayersFound", this, player.UserIDString));
                int i = 1;
                foreach(BasePlayer p in results)
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

        #region data management

        #region BountyData

        private class BountyData
        {
            public Dictionary<uint, Bounty> bounties = new Dictionary<uint, Bounty>();

            public BountyData() { }

            public Bounty GetBounty(uint itemID)
            {
                if (!bounties.ContainsKey(itemID)) return null;
                return bounties[itemID];
            }

            public void AddBounty(Bounty bounty)
            {
                bounties.Add(bounty.noteUid, bounty);
                Instance.saveBountyData();
#if DEBUG
                Instance.PrintToChat($"added Bounty {bounty.placerName} -> {bounty.targetName} {bounty.rewardAmount} to data");
#endif
            }

            public void removeBounty(uint itemID)
            {
                bounties.Remove(itemID);
                Instance.saveBountyData();
#if DEBUG
                Instance.PrintToChat($"removed Bounty {itemID} from data");
#endif
            }
        }

        void saveBountyData()
        {
            try
            {
                bountyDataFile.WriteObject(bountyData);
            }
            catch (Exception E)
            {
                Puts(E.ToString());
            }
        }

        void loadBountyData()
        {
            try
            {
                bountyData = bountyDataFile.ReadObject<BountyData>();
            }
            catch (Exception E)
            {
                Puts(E.ToString());
            }
        }

        #endregion

        #region HuntData

        private class HuntData
        {
            public List<Hunt> hunts = new List<Hunt>();

            public HuntData() { }

            public Hunt getHunt(Bounty bounty)
            {
                return hunts.Where((h) => h.bounty == bounty).First();
            }

            public void addHunt(Hunt hunt)
            {
                hunts.Add(hunt);
                Instance.saveHuntData();
            }

            public void removeHunt(Hunt hunt)
            {
                hunts.Remove(hunt);
                Instance.saveHuntData();
            }
        }

        void saveHuntData()
        {
            try
            {
                bountyDataFile.WriteObject(bountyData);
            }
            catch (Exception E)
            {
                Puts(E.ToString());
            }
        }

        void loadHuntData()
        {
            try
            {
                bountyData = bountyDataFile.ReadObject<BountyData>();
            }
            catch (Exception E)
            {
                Puts(E.ToString());
            }
        }

        #endregion

        #endregion

        #region Config
        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Currency Item shortname")]
            public string currency;

            [JsonProperty(PropertyName = "Minimum reward amount")]
            public int minReward;

            [JsonProperty(PropertyName = "Hunting cooldown in seconds")]
            public int targetCooldown;

            [JsonProperty(PropertyName = "Hunt Duration")]
            public int huntDuration;
        }

        private ConfigData getDefaultConfig()
        {
            return new ConfigData
            {
                currency = "scrap",
                minReward = 100,
                targetCooldown = 7200,
                huntDuration = 7200
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
        #endregion

        #region Localization
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"noPermission", "You don't have permission to use this command!" },
            {"noPlayersFound", "No players found!" },
            {"multiplePlayersFound", "Multiple players found!" },
            {"usageAdd", "/bounty add (target name) (reward amount) \"(reason)\""},
            {"minReward", "The reward has to be at least {0}!" },
            {"notEnough", "You don't have enough {0}!" },
            {"bountyText", "WANTED! DEAD OR ALIVE!\n {0}\n for {1}\n REWARD: {2}" }
        };
        #endregion
    }
}