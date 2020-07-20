// Requires: GUICreator

//#define DEBUG

using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("bounties", "OHM & Bunsen", "2.0.6")]
    [Description("RP Bounty Hunting")]
    partial class Bounties : RustPlugin
    {
        private static Plugins.Bounties PluginInstance;
        const string huntLogFileName = "hunts";
        const string bountyLogFileName = "bounties";

        public Bounties()
        {
            PluginInstance = this;
        }

        #region helpers

        public void rename(BasePlayer player, string name)
        {
            if (player == null) return;
            player.displayName = name;
            player.IPlayer.Rename(name);
        }

        public string lastSeen(BasePlayer player)
        {
            if (player == null) return null;
            StringBuilder sb = new StringBuilder("last seen ");
            string grid = getGrid(player.transform.position);
            if (!string.IsNullOrEmpty(grid)) sb.Append($"in {grid} ");
            sb.Append(", wearing: ");
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
            sb.Append($", {armedWith(player)}");
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
            if (player == null) return null;
            try
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
            catch(Exception e)
            {
                Puts(e.Message);
                return null;
            }
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

    partial class Bounties : RustPlugin
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

            public Hunt hunt;

            public Item reward
            {
                get
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    if (itemDef == null) return null;
                    return ItemManager.Create(itemDef, rewardAmount);
                }
            }

            [JsonProperty(PropertyName = "Reason")]
            public string reason;

            public string text
            {
                get
                {
                    //ItemDefinition itemDef = ItemManager.FindItemDefinition(config.currency);
                    //return string.Format(PluginInstance.lang.GetMessage("bountyText", PluginInstance), targetName, reason ?? $"disrespecting {placerName}", $"{rewardAmount} {itemDef?.displayName?.english ?? "$"}", PluginInstance.getGrid(target.transform.position), PluginInstance.wearing(target), PluginInstance.armedWith(target));
                    return $"Target: {targetName}\nReward: {rewardAmount} {reward.info.displayName.english}\nReason: {reason}\nTo start hunting, put this note in your hotbar and select it!";
                }
            }

            public TimeSpan timeSinceCreation => DateTime.Now - timestamp;

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

                if(config.showSteamImage)
                {
                    PluginInstance.GetSteamUserData(targetID, (ps) =>
                        PluginInstance.guiCreator.registerImage(PluginInstance, targetID.ToString(), ps.avatarfull)
                    );
                }

                BountyData.AddBounty(this);
                PluginInstance.LogToFile(bountyLogFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} {placerName}[{placerID}] placed a bounty of {rewardAmount} {config.currency} on {targetName}[{targetID}]'s head. Reason: {reason}", PluginInstance);
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

            public Hunt startHunt(BasePlayer hunter) => hunt = new Hunt(this, hunter);

        }

        private class PortableBounty : MonoBehaviour
        {
            public Bounty bounty;
        }
    }
}﻿namespace Oxide.Plugins
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    partial class Bounties : RustPlugin
    {
        partial void initCommands()
        {
            cmd.AddChatCommand("bounty", this, nameof(bountyCommand));
            cmd.AddChatCommand("hunt", this, nameof(huntCommand));
            cmd.AddChatCommand("mask", this, nameof(maskCommand));
            cmd.AddChatCommand("unmask", this, nameof(unmaskCommand));
        }

        private void bountyCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permissions.use))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                sendCreator(player);
                return;
            }
            switch (args[0])
            {
                case "add":
                case "place":
                    if (!hasPermission(player, permissions.admin))
                    {
                        PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                        return;
                    }

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

        private void maskCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permissions.mask))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }
            Hunt hunt = HuntData.getHuntByHunter(player);
            if (hunt == null)
            {
                player.ChatMessage("You're not hunting anyone!");
                return;
            }
            if(config.maskNames.Count == 0)
            {
                player.ChatMessage("There aren't any masknames available.");
                return;
            }
            System.Random rand = new System.Random();
            string maskName = config.maskNames[rand.Next(0, config.maskNames.Count - 1)];
            rename(player, maskName);
            player.ChatMessage($"You've been renamed to <color=#00ff33><size=16>{maskName}</size></color>. Using global chat might give you away, so be careful...");
        }

        private void unmaskCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permissions.mask))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }
            Hunt hunt = HuntData.getHuntByHunter(player);
            if (hunt == null)
            {
                player.ChatMessage("You're not hunting anyone!");
                return;
            }
            if(player.displayName == hunt.hunterName)
            {
                player.ChatMessage("you're not masked!");
                return;
            }
            rename(player, hunt.hunterName);
            player.ChatMessage("You've been unmasked!");
        }

        private void huntCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permissions.admin))
            {
                PrintToChat(player, lang.GetMessage("noPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 0) return;
            switch(args[0])
            {
                case "list":
                    player.ChatMessage("Ongoing hunts:");
                    foreach(Hunt h in HuntData.instance.hunts)
                    {
                        player.ChatMessage($"{h.hunterName} hunts {h.bounty.targetName}");
                    }
                    break;
                case "end":
                case "remove":
                    if (args.Length < 2) return;
                    BasePlayer target = findPlayer(args[1], player);
                    Hunt hunt = HuntData.getHuntByHunter(target);
                    if (hunt == null) hunt = HuntData.getHuntByTarget(target);
                    if (hunt == null) return;
                    hunt.end();
                    player.ChatMessage("removed hunt");
                    break;
            }
        }


    }
}﻿namespace Oxide.Plugins
{
    using Facepunch.Extend;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    partial class Bounties : RustPlugin
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

            [JsonProperty(PropertyName = "Hunting cooldown after creating a bounty in seconds")]
            public int creationCooldown;

            [JsonProperty(PropertyName = "Hunting cooldown after being hunted in seconds")]
            public int targetCooldown;

            [JsonProperty(PropertyName = "Hunt Duration")]
            public int huntDuration;

            [JsonProperty(PropertyName = "Hunt indicator refresh interval")]
            public int indicatorRefresh;

            [JsonProperty(PropertyName = "Minimum distance for starting a hunt")]
            public int safeDistance;

            [JsonProperty(PropertyName = "Show target indicator that he/she is being hunted")]
            public bool showTargetIndicator;

            [JsonProperty(PropertyName = "Show hunter name to target")]
            public bool showHunter;

            [JsonProperty(PropertyName = "Show hunter distance to target")]
            public bool showDistance;

            [JsonProperty(PropertyName = "Show last seen info to hunter")]
            public bool showLastSeen;

            [JsonProperty(PropertyName = "Broadcast hunt conclusion in global chat")]
            public bool broadcastHunt;

            [JsonProperty(PropertyName = "Show steam profile picture of target on bounty")]
            public bool showSteamImage;

            [JsonProperty(PropertyName = "Require reason for bounties")]
            public bool requireReason;

            [JsonProperty(PropertyName = "Allow skull crushing as confirmation")]
            public bool skullCrushing;

            [JsonProperty(PropertyName = "Random Names for /mask command")]
            public List<string> maskNames;
        }

        private ConfigData getDefaultConfig()
        {
            return new ConfigData
            {
                steamAPIKey = "",
                currency = "scrap",
                minReward = 100,
                creationCooldown = 1200,
                targetCooldown = 7200,
                huntDuration = 7200,
                indicatorRefresh = 5,
                safeDistance = 500,
                showTargetIndicator = true,
                showHunter = true,
                showDistance = true,
                showLastSeen = true,
                broadcastHunt = true,
                showSteamImage = true,
                requireReason = true,
                skullCrushing = true,
                maskNames = new List<string>
                {
                    "Aischrolatry",
                    "Anglomania",
                    "AntrimIrpe",
                    "Agnizetix982",
                    "Scorbutic",
                    "ForepSutler",
                    "Ichthyic",
                    "Gladiate",
                    "Aubergine",
                    "Zelotypia",
                    "AltmousGadfly",
                    "Piceous",
                    "Synectics",
                    "Policeocracy",
                    "UgotmYeasty",
                    "Multanimous",
                    "Pulletwee12549",
                    "Sativetang",
                    "ForepSutler",
                    "Grapheme",
                    "Petulcous",
                    "Pastance",
                    "Ichorbmmlc77",
                    "Osmometer",
                    "Plurennial",
                    "Affricate",
                    "Anagogy",
                    "Apperception",
                    "Araphorostic",
                    "AsufTropic",
                    "Bailivate",
                    "Bindlestiff",
                    "Cataplasm",
                    "Cylixlboj",
                    "Demonomania",
                    "Discept",
                    "Enigmatology",
                    "Entablature",
                    "Episcopicide",
                    "Estampie",
                    "Favonian",
                    "ForepSutler",
                    "GrryDucape",
                    "Hellenomania",
                    "Horrent",
                    "Horrent",
                    "Hydroscopist",
                    "Hylophagous",
                    "Impudicity",
                    "Inurbanity",
                    "Invitatory",
                    "Iteration",
                    "Laevoduction",
                    "Lardaceous",
                    "Laudanum",
                    "Magnetograph",
                    "Matriotism",
                    "Meninges",
                    "Microanatomy",
                    "Narrischkeit",
                    "Necrophobia",
                    "Necrophobia",
                    "Necrophobia",
                    "Neophobia",
                    "Nephoscope",
                    "Nephoscope",
                    "Ophiology",
                    "Ossiferous",
                    "Padella",
                    "Papillose",
                    "Parabiosis",
                    "Pasguard",
                    "Pennyweight",
                    "Piliferous",
                    "Pillion",
                    "Pleonectic",
                    "Poliorcectic",
                    "PugbrosMurine",
                    "Pulicide",
                    "Pullulate",
                    "Quatrefoil",
                    "Quillon",
                    "Rescript",
                    "Reticello",
                    "RosyrotBulbul",
                    "Socageralvy",
                    "SoldarHoary",
                    "Soughrox218",
                    "Spiritualism",
                    "Stadial",
                    "Stirkfung123",
                    "SwagtasCrinal",
                    "Swivekth18",
                    "Tegestology",
                    "Torchier",
                    "Tricolette",
                    "Vadosemaryly",
                    "Valeyval",
                    "Vendace",
                    "Yarmulke"
                }
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

    partial class Bounties : RustPlugin
    {
        partial void initData()
        {
            BountyData.init();
            HuntData.init();
            HuntData.restartHunts();
            CooldownData.init();
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

            public static Bounty GetBounty(Item item)
            {
                if (!initialized) init();
                if (!instance.bounties.ContainsKey(item.uid)) return null;
                Bounty bounty = instance.bounties[item.uid];
                return bounty;
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
            public static HuntData instance;
            private static bool initialized = false;

            [JsonProperty(PropertyName = "Hunt list")]
            public List<Hunt> hunts = new List<Hunt>();

            public HuntData()
            {
            }

            public static void restartHunts()
            {
                foreach(Hunt hunt in instance.hunts)
                {
                    hunt.initTicker();
                }
            }

            public static Hunt getHuntByHunter(BasePlayer player)
            {
                if (!initialized) init();
                List<Hunt> results = instance.hunts.Where((h) => h.hunterID == player.userID).ToList();
                if (results.Count == 0) return null;
                return results.First();
            }

            public static Hunt getHuntByTarget(BasePlayer player)
            {
                if (!initialized) init();
                List<Hunt> results = instance.hunts.Where((h) => h.bounty.targetID == player.userID).ToList();
                if (results.Count == 0) return null;
                return results.First();
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
                    StringBuilder sb = new StringBuilder($"saving {typeof(HuntData).Name} failed. Are you trying to save complex classes like BasePlayer or Item? that won't work!\n");
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
                    StringBuilder sb = new StringBuilder($"loading {typeof(HuntData).Name} failed. Make sure that all classes you're saving have a default constructor!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class CooldownData
        {
            private static DynamicConfigFile CooldownDataFile;
            private static CooldownData instance;
            private static bool initialized = false;

            [JsonProperty(PropertyName = "Cooldowns")]
            private Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();

            public CooldownData() { }

            public static void addCooldown(BasePlayer player)
            {
                if (player == null) return;
                if (instance.cooldowns.ContainsKey(player.userID)) return;
                instance.cooldowns.Add(player.userID, DateTime.Now);
                save();
            }

            public static void removeCooldown(BasePlayer player)
            {
                if (!instance.cooldowns.ContainsKey(player.userID)) return;
                instance.cooldowns.Remove(player.userID);
                save();
            }

            public static bool isOnCooldown(BasePlayer player, out TimeSpan remaining)
            {
                remaining = TimeSpan.Zero;
                if (!instance.cooldowns.ContainsKey(player.userID)) return false;
                remaining = new TimeSpan(0,0,config.targetCooldown - (int)(DateTime.Now - instance.cooldowns[player.userID]).TotalSeconds);
                if ((DateTime.Now - instance.cooldowns[player.userID]).TotalSeconds < config.targetCooldown) return true;
                else
                {
                    instance.cooldowns.Remove(player.userID);
                    save();
                }
                return false;
            }

            public static void init()
            {
                if (initialized) return;
                CooldownDataFile = Interface.Oxide.DataFileSystem.GetFile("bounties/Cooldowns");
                load();
                initialized = true;
            }

            public static void save()
            {
                if (!initialized) init();
                try
                {
                    CooldownDataFile.WriteObject(instance);
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"saving {typeof(CooldownData).Name} failed. Are you trying to save complex classes like BasePlayer or Item? that won't work!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }

            public static void load()
            {
                try
                {
                    instance = CooldownDataFile.ReadObject<CooldownData>();
                }
                catch (Exception E)
                {
                    StringBuilder sb = new StringBuilder($"loading {typeof(CooldownData).Name} failed. Make sure that all classes you're saving have a default constructor!\n");
                    sb.Append(E.Message);
                    PluginInstance.Puts(sb.ToString());
                }
            }
        }
    }
}﻿namespace Oxide.Plugins
{
    using ConVar;
    using Facepunch.Extend;
    using Rust.Workshop.Editor;
    using Steamworks.Data;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using static Oxide.Plugins.GUICreator;

    partial class Bounties : RustPlugin
    {
        partial void initGUI()
        {
            guiCreator = (GUICreator)Manager.GetPlugin("GUICreator");
            guiCreator.registerImage(this, "bounty_template", "https://cdn.discordapp.com/attachments/684891062723936349/713845889503985714/WANTED_1.png");
        }

        private GUICreator guiCreator;

        #region UI Parameters
        float FadeIn = 0.1f;
        float FadeOut = 0.1f;
        int resX = 1920;
        int resY = 1080;

        GuiColor opaqueWhite = new GuiColor(1, 1, 1, 1);

        GuiColor lightGrey = new GuiColor(0, 0, 0, 0.5f);

        GuiColor darkGreen = new GuiColor(67, 84, 37, 0.8f);
        GuiColor lightGreen = new GuiColor(134, 190, 41, 0.8f);

        GuiColor darkRed = new GuiColor(65, 33, 32, 0.8f);
        GuiColor lightRed = new GuiColor(162, 51, 46, 0.8f);

        const string errorSound = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";
        const string successSound = "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab";

        #endregion

        #region bounty creator

        public class BountyBP
        {
            public BasePlayer target = null;
            public int reward = 0;
            public string reason = "";

            public void init(BasePlayer placer)
            {
                Bounty b = new Bounty(placer, target, reward, reason);
                placer.inventory.Take(null, b.reward.info.itemid, reward);
            }
        }

        public void sendCreator(BasePlayer player)
        {
#if DEBUG
            player.ChatMessage($"sendCreator");
#endif
            BountyBP bp = new BountyBP();
            GuiContainer c = new GuiContainer(this, "bountyCreator");

            //template
            Rectangle templatePos = new Rectangle(623, 26, 673, 854, resX, resY, true);
            c.addImage("template", templatePos, "bounty_template", GuiContainer.Layer.hud, FadeIn: FadeIn, FadeOut: FadeOut);

            //targetName
            Rectangle targetNamePos = new Rectangle(680, 250, 100, 53, resX, resY, true);
            Rectangle targetNamePosInput = new Rectangle(780, 250, 460, 53, resX, resY, true);
            c.addText("targetNameHeader", targetNamePos, GuiContainer.Layer.hud, new GuiText("Target:", 20), FadeIn, FadeOut);
            Action<BasePlayer, string[]> targetNameCB = (p, a) => 
            {
                if (a.Length < 1) return;
                BasePlayer target = findPlayer(a[0], player);
                if (target == null)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.missingTarget, bp);
                    return;
                }
                bp.target = target;
                GuiTracker.getGuiTracker(player).destroyGui(this, c, "targetName");
                int fontsize = guiCreator.getFontsizeByFramesize(target.displayName.Length, targetNamePosInput);
                GuiText targetNameText = new GuiText(target.displayName, fontsize);
                GuiContainer c2 = new GuiContainer(this, "tfound", "bountyCreator");
                c2.addText("targetName", targetNamePosInput, GuiContainer.Layer.hud, targetNameText, FadeIn, FadeOut);
                c2.display(player);
                Effect.server.Run(successSound, player.transform.position);

                //image
                if (config.showSteamImage)
                {
                    GetSteamUserData(target.userID, (ps) =>
                    {
                        guiCreator.registerImage(this, target.userID.ToString(), ps.avatarfull, () =>
                        {
                            Rectangle imagePos = new Rectangle(828, 315, 264, 264, resX, resY, true);
                            GuiContainer c3 = new GuiContainer(this, "image", "bountyCreator");
                            c3.addImage("image", imagePos, target.userID.ToString(), GuiContainer.Layer.hud, FadeIn: FadeIn, FadeOut: FadeOut);
                            c3.display(player);
                            player.ChatMessage("sent image");
                        });
                    });
                }
                
            };
            c.addInput("targetName", targetNamePosInput, targetNameCB, GuiContainer.Layer.hud, panelColor: new GuiColor("white"), text: new GuiText("", 20, new GuiColor("black")), FadeOut: FadeOut, FadeIn: FadeIn);

            //reward
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(config.currency);
            Rectangle rewardPosInput = new Rectangle(828, 579, 132, 53, resX, resY, true);
            Rectangle rewardPosCurrency = new Rectangle(970, 579, 122, 53, resX, resY, true);
            Action<BasePlayer, string[]> rewardCB = (p, a) => 
            {
                if (a.Length < 1) return;
                int reward = 0;
                if (!int.TryParse(a[0], out reward))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.badReward, bp);
                    return;
                }
                if (reward < config.minReward)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.badReward, bp);
                    return;
                }
                if(player.inventory.GetAmount(itemDefinition.itemid) < reward)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.cantAfford, bp);
                    return;
                }
                bp.reward = reward;
                GuiTracker.getGuiTracker(player).destroyGui(this, c, "reward");
                GuiContainer c2 = new GuiContainer(this, "rewardok", "bountyCreator");
                c2.addText("reward", rewardPosInput, GuiContainer.Layer.hud, new GuiText(reward.ToString(), 24, align: TextAnchor.MiddleRight), FadeIn, FadeOut);
                c2.display(player);
                Effect.server.Run(successSound, player.transform.position);
            };
            c.addInput("reward", rewardPosInput, rewardCB, GuiContainer.Layer.hud, panelColor: new GuiColor("white"), text: new GuiText("", 22, new GuiColor("black")), FadeOut: FadeOut, FadeIn: FadeIn);
            c.addText("rewardCurrency", rewardPosCurrency, GuiContainer.Layer.hud, new GuiText(itemDefinition.displayName.english, 24, new GuiColor("black"), TextAnchor.MiddleLeft), FadeIn, FadeOut);

            //reason
            Rectangle reasonPos = new Rectangle(680, 681, 100, 53, resX, resY, true);
            Rectangle reasonPosInput = new Rectangle(780, 681, 460, 53, resX, resY, true);
            c.addText("reasonHeader", reasonPos, GuiContainer.Layer.hud, new GuiText("Reason:", 20), FadeIn, FadeOut);
            Action<BasePlayer, string[]> reasonCB = (p, a) => 
            {
                if (a.Length < 1) return;
                StringBuilder sb = new StringBuilder();
                foreach(string s in a)
                {
                    sb.Append($"{s} ");
                }
                bp.reason = sb.ToString().Trim();
            };
            c.addInput("reason", reasonPosInput, reasonCB, GuiContainer.Layer.hud, panelColor: new GuiColor("white"), text: new GuiText("", 14, new GuiColor("black")), FadeOut: FadeOut, FadeIn: FadeIn);

            //placerName
            Rectangle placerNamePos = new Rectangle(680, 771, 560, 36, resX, resY, true);
            GuiText placerNameText = new GuiText(player.displayName, guiCreator.getFontsizeByFramesize(player.displayName.Length, placerNamePos));
            c.addText("placerName", placerNamePos, GuiContainer.Layer.hud, placerNameText, FadeIn, FadeOut);

            //exitButton
            Rectangle closeButtonPos = new Rectangle(1296, 52, 60, 60, resX, resY, true);
            c.addButton("close", closeButtonPos, GuiContainer.Layer.hud, darkRed, FadeIn, FadeOut, new GuiText("X", 24, lightRed), blur: GuiContainer.Blur.medium);

            c.display(player);

            //button
            creatorButton(player, bp: bp);
        }

        public enum createErrorType { none, missingTarget, badReward, cantAfford, missingReason};
        public void creatorButton(BasePlayer player, createErrorType error = createErrorType.none, BountyBP bp = null)
        {
            GuiContainer c = new GuiContainer(this, "createButton", "bountyCreator");
            Rectangle ButtonPos = new Rectangle(710, 856, 500, 100, resX, resY, true);

            List<GuiText> textOptions = new List<GuiText>
            {
                new GuiText("Create Bounty", guiCreator.getFontsizeByFramesize(13, ButtonPos), lightGreen),
                new GuiText("Target not found!", guiCreator.getFontsizeByFramesize(17, ButtonPos), lightRed),
                new GuiText("Invalid reward!", guiCreator.getFontsizeByFramesize(15, ButtonPos), lightRed),
                new GuiText("Can't afford reward!", guiCreator.getFontsizeByFramesize(20, ButtonPos), lightRed),
                new GuiText("Reason missing!", guiCreator.getFontsizeByFramesize(15, ButtonPos), lightRed)
            };

            Action<BasePlayer, string[]> cb = (p, a) =>
            {
                if (bp.target == null)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.missingTarget, bp);
                }
                else if(bp.reward == 0)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.badReward, bp);
                }
                else if (config.requireReason && string.IsNullOrEmpty(bp.reason))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    creatorButton(player, createErrorType.missingReason, bp);
                }
                else
                {
                    Effect.server.Run(successSound, player.transform.position);
                    bp.init(player);
                    GuiTracker.getGuiTracker(player).destroyGui(this, "bountyCreator");
                }
            };

            c.addPlainButton("button", ButtonPos, GuiContainer.Layer.hud, (error == createErrorType.none) ? darkGreen : darkRed, FadeIn, FadeOut, textOptions[(int)error], cb, blur: GuiContainer.Blur.medium);
            c.display(player);

            if (error != createErrorType.none)
            {
                PluginInstance.timer.Once(2f, () =>
                {
                    if (GuiTracker.getGuiTracker(player).getContainer(this, "bountyCreator") != null) creatorButton(player, bp: bp);
                });
            }
        }

        #endregion

        #region bounty
        public void sendBounty(BasePlayer player, Bounty bounty)
        {
#if DEBUG
            player.ChatMessage($"sendBounty: {bounty.placerName} -> {bounty.targetName}");
#endif
            closeBounty(player);
            GuiContainer c = new GuiContainer(this, "bountyPreview");

            //template
            Rectangle templatePos = new Rectangle(623, 26, 673, 854, resX, resY, true);
            c.addImage("template", templatePos, "bounty_template", GuiContainer.Layer.hud, FadeIn: FadeIn, FadeOut: FadeOut);
           
            //targetName
            Rectangle targetNamePos = new Rectangle(680, 250, 560, 65, resX, resY, true);
            int fontsize = guiCreator.getFontsizeByFramesize(bounty.targetName.Length, targetNamePos);
            GuiText targetNameText = new GuiText(bounty.targetName, fontsize);
            c.addText("targetName", targetNamePos, GuiContainer.Layer.hud, targetNameText, FadeIn, FadeOut);

            //image
            if(config.showSteamImage)
            {
                Rectangle imagePos = new Rectangle(828, 315, 264, 264, resX, resY, true);
                c.addImage("image", imagePos, bounty.targetID.ToString(), GuiContainer.Layer.hud, FadeIn: FadeIn, FadeOut: FadeOut);
            }

            //reward
            Rectangle rewardPos = new Rectangle(680, 579, 560, 53, resX, resY, true);
            string reward = $"{bounty.rewardAmount} {bounty.reward.info.displayName.english}";
            GuiText rewardText = new GuiText(reward, guiCreator.getFontsizeByFramesize(reward.Length, rewardPos));
            c.addText("reward", rewardPos, GuiContainer.Layer.hud, rewardText, FadeIn, FadeOut);
           
            //reason
            Rectangle reasonPos = new Rectangle(680, 681, 560, 53, resX, resY, true);
            GuiText reasonText = new GuiText(bounty.reason, 14);
            c.addText("reason", reasonPos, GuiContainer.Layer.hud, reasonText, FadeIn, FadeOut);
           
            //placerName
            Rectangle placerNamePos = new Rectangle(680, 771, 560, 36, resX, resY, true);
            GuiText placerNameText = new GuiText(bounty.placerName, guiCreator.getFontsizeByFramesize(bounty.placerName.Length, placerNamePos));
            c.addText("placerName", placerNamePos, GuiContainer.Layer.hud, placerNameText, FadeIn, FadeOut);

            //exitButton
            Rectangle closeButtonPos = new Rectangle(1296, 52, 60, 60, resX, resY, true);
            c.addButton("close", closeButtonPos, GuiContainer.Layer.hud, darkRed, FadeIn, FadeOut, new GuiText("X", 24, lightRed), blur: GuiContainer.Blur.medium);

            c.display(player);

            //button
            if (bounty.hunt != null) huntButton(player, bounty, huntErrorType.huntActive);
            else huntButton(player, bounty);

        }

        public enum huntErrorType {none ,hunterAlreadyHunting, targetAlreadyHunted, targetCooldown, huntActive, selfHunt, offline, tooClose};
        public void huntButton(BasePlayer player, Bounty bounty, huntErrorType error = huntErrorType.none)
        {
            GuiContainer c = new GuiContainer(this, "huntButton", "bountyPreview");
            Rectangle ButtonPos = new Rectangle(710, 856, 500, 100, resX, resY, true);

            List<GuiText> textOptions = new List<GuiText>
            {
                new GuiText("Start Hunting", guiCreator.getFontsizeByFramesize(13, ButtonPos), lightGreen),
                new GuiText("You are already hunting", guiCreator.getFontsizeByFramesize(23, ButtonPos), lightRed),
                new GuiText("Target is already being hunted", guiCreator.getFontsizeByFramesize(30, ButtonPos), lightRed),
                new GuiText("Target can't be hunted yet", guiCreator.getFontsizeByFramesize(26, ButtonPos), lightRed),
                new GuiText("Hunt Active", guiCreator.getFontsizeByFramesize(11, ButtonPos), lightRed),
                new GuiText("You can't hunt yourself!",guiCreator.getFontsizeByFramesize(24, ButtonPos), lightRed),
                new GuiText("Target is offline",guiCreator.getFontsizeByFramesize(17, ButtonPos), lightRed),
                new GuiText("Too close to the target!",guiCreator.getFontsizeByFramesize(24, ButtonPos), lightRed),
            };

            Action<BasePlayer, string[]> cb = (p, a) =>
            {
                TimeSpan targetCooldown = TimeSpan.Zero;
                TimeSpan creationCooldown = new TimeSpan(0, 0, config.creationCooldown - (int)bounty.timeSinceCreation.TotalSeconds);
                if (error == huntErrorType.huntActive) return;
                else if (bounty.target == null)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.offline);
                }
                else if (bounty.target.IsSleeping())
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.offline);
                }
                else if (bounty.hunt != null || HuntData.getHuntByHunter(p) != null)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.hunterAlreadyHunting);
                }
                else if(HuntData.getHuntByTarget(bounty.target) != null)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.targetAlreadyHunted);
                }
                else if((bounty.timeSinceCreation.TotalSeconds < config.creationCooldown || CooldownData.isOnCooldown(bounty.target, out targetCooldown))&&!hasPermission(player, permissions.admin))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.targetCooldown);
                    TimeSpan select = creationCooldown;
                    if (targetCooldown > creationCooldown) select = targetCooldown;
                    player.ChatMessage($"Cooldown: {select.ToString(@"hh\:mm\:ss")}");
                }
                else if(bounty.target == player && !hasPermission(player, permissions.admin))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.selfHunt);
                }
                else if(Vector3.Distance(player.transform.position, bounty.target.transform.position) < config.safeDistance && !hasPermission(player, permissions.admin))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.tooClose);
                }
                else
                {
                    Effect.server.Run(successSound, player.transform.position);
                    bounty.startHunt(p);
                    BountyData.removeBounty(bounty.noteUid);
                    player.GetActiveItem()?.Remove();       
                    closeBounty(player);
                }
            };

            c.addPlainButton("button", ButtonPos, GuiContainer.Layer.hud, (error == huntErrorType.none)?darkGreen:darkRed, FadeIn, FadeOut, textOptions[(int)error], cb, blur: GuiContainer.Blur.medium);
            c.display(player);

            if (error != huntErrorType.none && error != huntErrorType.huntActive)
            {
                PluginInstance.timer.Once(2f, () =>
                {
                    if(GuiTracker.getGuiTracker(player).getContainer(this, "bountyPreview") != null) huntButton(player, bounty);
                });
            }
        }

        #endregion

        public void closeBounty(BasePlayer player)
        {
            //for bounty and creator
#if DEBUG
            player.ChatMessage($"closeBounty");
#endif
            GuiTracker.getGuiTracker(player).destroyGui(this, "bountyPreview");
        }

        public void sendHunterIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendHunterIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
            if (player == null) return;
            if (player.IsSleeping()) return;
            GuiContainer c = new GuiContainer(this, "hunterIndicator");

            //Background
            Rectangle bgPos = new Rectangle(50, 100, 350, 150, resX, resY, true);
            c.addPlainPanel("bg", bgPos, GuiContainer.Layer.hud, lightGrey, 0, 0, GuiContainer.Blur.medium);

            //Name
            Rectangle namePos = new Rectangle(50, 100, 350, 35, resX, resY, true);
            string nameTextString = $"You are hunting {hunt.bounty.targetName}";
            int fontsize = guiCreator.getFontsizeByFramesize(nameTextString.Length, namePos);
            GuiText nameText = new GuiText(nameTextString, fontsize, opaqueWhite);
            c.addText("name", namePos, GuiContainer.Layer.hud, nameText);

            //Last Seen
            if(config.showLastSeen)
            {
                Rectangle lastSeenPos = new Rectangle(50, 135, 350, 80, resX, resY, true);
                string lastSeenString = hunt.lastSeen();
                GuiText lastSeenText = new GuiText(lastSeenString, 10, opaqueWhite);
                c.addText("lastSeen", lastSeenPos, GuiContainer.Layer.hud, lastSeenText);
            }

            //Countdown
            Rectangle countdownPos = new Rectangle(50, 215, 350, 35, resX, resY, true);
            GuiText countdownText = new GuiText(hunt.remaining.ToString(@"hh\:mm\:ss"), 20, opaqueWhite);
            c.addText("countdown", countdownPos, GuiContainer.Layer.hud, countdownText);

            c.display(player);
        }

        public void sendTargetIndicator(BasePlayer player, Hunt hunt)
        {
            if (!config.showTargetIndicator) return;
#if DEBUG
            player.ChatMessage($"sendTargetIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
            if (player == null) return;
            if (player.IsSleeping()) return;
            GuiContainer c = new GuiContainer(this, "targetIndicator");

            //Background
            Rectangle bgPos = new Rectangle(50, 250, 350, 100, resX, resY, true);
            float distance = config.safeDistance;
            if(hunt.hunter?.transform != null && hunt.target?.transform != null) distance = Vector3.Distance(hunt.hunter.transform.position, hunt.target.transform.position);
            GuiColor bgColor = config.showDistance?gradientRedYellowGreen(Mathf.Clamp((distance / config.safeDistance), 0, 1)):lightGrey;
            bgColor.setAlpha(0.5f);
            c.addPlainPanel("Background", bgPos, GuiContainer.Layer.hud, bgColor, 0, 0, GuiContainer.Blur.medium);

            //TopLine
            Rectangle topLinePos = new Rectangle(50, 250, 350, 50, resX, resY, true);
            string TopLineString = $"You are being hunted{(config.showHunter?$" by {hunt.hunterName}":"")}!";
            int topLineFontsize = guiCreator.getFontsizeByFramesize(TopLineString.Length, topLinePos);
            GuiText topLineText = new GuiText(TopLineString, topLineFontsize, opaqueWhite);
            c.addText("topline", topLinePos, GuiContainer.Layer.hud, topLineText);

            //BottomLine
            Rectangle bottomLinePos = new Rectangle(50, 300, 350, 20, resX, resY, true);
            string bottomLineString = "You can run, but you can't hide!";
            int bottomLineFontsize = guiCreator.getFontsizeByFramesize(bottomLineString.Length, bottomLinePos);
            GuiText bottomLineText = new GuiText(bottomLineString, bottomLineFontsize, opaqueWhite);
            c.addText("bottomLine", bottomLinePos, GuiContainer.Layer.hud, bottomLineText);

            //Countdown
            Rectangle CountdownPos = new Rectangle( 50, 320, 350, 30, resX, resY, true);
            string CountdownString = hunt.remaining.ToString(@"hh\:mm\:ss");
            int CountdownFontsize = guiCreator.getFontsizeByFramesize(CountdownString.Length, CountdownPos);
            GuiText CountdownText = new GuiText(CountdownString, CountdownFontsize, opaqueWhite);
            c.addText("Countdown", CountdownPos, GuiContainer.Layer.hud, CountdownText);

            c.display(player);
        }

        public void closeIndicators(BasePlayer player)
        {
#if DEBUG
            player.ChatMessage($"closeIndicators: {player.displayName}");
#endif
            try
            {
                GuiTracker.getGuiTracker(player).destroyGui(this, "hunterIndicator");
            }
            catch(Exception e)
            {
                Puts(e.Message);
            }
            try
            {
                GuiTracker.getGuiTracker(player).destroyGui(this, "targetIndicator");
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }
        }

        public void huntExpiredMsg(Hunt hunt)
        {
            try
            {
                guiCreator.customGameTip(hunt.hunter, "The hunt is over. Better luck next time!", 5);
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                guiCreator.customGameTip(hunt.target, "The hunt is over. You're safe... for now...", 5);
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            LogToFile(huntLogFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt expired: {hunt.hunterName}[{hunt.hunterID}] hunting {hunt.bounty.targetName}[{hunt.bounty.targetID}], placed by {hunt.bounty.placerName}[{hunt.bounty.placerID}]", this);
        }

        public void huntSuccessfullMsg(Hunt hunt)
        {
            try
            {
                guiCreator.prompt(hunt.hunter, $"You've successfully hunted down {hunt.hunterName}!\n{hunt.bounty.reward.amount} {hunt.bounty.reward.info.displayName.english} have been transferred to your inventory!", "Hunt successful!");
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            if (config.broadcastHunt) PrintToChat($"<color=#00ff33>{hunt.hunterName} claims the bounty of {hunt.bounty.rewardAmount} {hunt.bounty.reward.info.displayName.english} on {hunt.bounty.targetName}'s head!</color>\nReason: {hunt.bounty.reason}");

            LogToFile(huntLogFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt successful: {hunt.hunterName}[{hunt.hunterID}] hunting {hunt.bounty.targetName}[{hunt.bounty.targetID}], placed by {hunt.bounty.placerName}[{hunt.bounty.placerID}]", this);
        }

        public void huntFailedMsg(Hunt hunt)
        {
            try
            {
                guiCreator.prompt(hunt.target, $"You've successfully defended yourself from {hunt.hunterName}!\n{hunt.bounty.rewardAmount} {hunt.bounty.reward.info.displayName.english} have been transferred to your inventory!", "Hunt averted!");
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            if (config.broadcastHunt) PrintToChat($"<color=#00ff33>{hunt.bounty.targetName} fends off his hunter {hunt.hunterName} and claims {hunt.bounty.rewardAmount} {hunt.bounty.reward.info.displayName.english}</color>\nBetter luck next time {hunt.hunterName}!");

            LogToFile(huntLogFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}Hunt failed: {hunt.hunterName}[{hunt.hunterID}] hunting {hunt.bounty.targetName}[{hunt.bounty.targetID}], placed by {hunt.bounty.placerName}[{hunt.bounty.placerID}]", this);
        }

        public GuiColor gradientRedYellowGreen(float level)
        {
            float r = (level < 0.5f)?1:(1-level)*2;
            float g = level;
            float b = 0;
            return new GuiColor(r, g, b, 1);
        }
    }
}﻿namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using System;

    partial class Bounties : RustPlugin
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

            private string lastSeenString;

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
            }

            public Hunt(Bounty bounty, BasePlayer hunter)
            {
                if (bounty.hunt != null) PluginInstance.Puts($"Hunt Constructor: Bounty {bounty.placerName} -> {bounty.targetName} already has an ongoing hunt!");
                timestamp = DateTime.Now;
                this.bounty = bounty;
                hunterID = hunter.userID;
                hunterName = hunter.displayName;
                initTicker();
                PluginInstance.sendHunterIndicator(hunter, this);
                PluginInstance.sendTargetIndicator(target, this);
                HuntData.addHunt(this);
                if(PluginInstance.hasPermission(hunter, permissions.mask)) hunter.ChatMessage("<color=#00ff33>Hunt started!</color> Remember that you can use /mask and /unmask to randomize your name for some extra stealth!");
                PluginInstance.LogToFile(huntLogFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt started: {hunter.displayName}[{hunter.UserIDString}] hunting {target.displayName}[{target.UserIDString}], placed by {bounty.placerName}[{bounty.placerID}]", PluginInstance);
            }

            public string lastSeen()
            {
                string newLastSeen = PluginInstance.lastSeen(target);
                if (newLastSeen != null) lastSeenString = newLastSeen;
                return lastSeenString;
            }

            public void initTicker()
            {
                TimeSpan remainingCache = remaining;
                if (remainingCache <= TimeSpan.Zero) end();
                else
                {
                    if(huntTimer == null) huntTimer = PluginInstance.timer.Once((float)remainingCache.TotalSeconds, () => end());
                    if(ticker == null) ticker = PluginInstance.timer.Every(1, () => tick());
                }
            }

            public void tick()
            {
                try
                {
                    PluginInstance.sendHunterIndicator(hunter, this);
                }
                catch( Exception e)
                {
                    PluginInstance.Puts(e.Message);
                }
                try
                {
                    PluginInstance.sendTargetIndicator(target, this);
                }
                catch (Exception e)
                {
                    PluginInstance.Puts(e.Message);
                }
            }

            public void end(BasePlayer winner = null)
            {
#if DEBUG
                PluginInstance.PrintToChat($"ending hunt {hunterName} -> {bounty.targetName}, winner: {winner?.displayName ?? "null"}");
#endif
                if(huntTimer != null) huntTimer.Destroy();
                if(ticker != null) ticker.Destroy();

                PluginInstance.rename(hunter, hunterName);

                if (winner == hunter)
                {
                    //msg
                    PluginInstance.huntSuccessfullMsg(this);

                    //payout
                    hunter.GiveItem(bounty.reward);
                }
                else if (winner == target)
                {
                    //msg
                    PluginInstance.huntFailedMsg(this);

                    //payout
                    target.GiveItem(bounty.reward);
                }
                else
                {
                    //msg
                    PluginInstance.huntExpiredMsg(this);
                    bounty.noteUid = bounty.giveNote(hunter);
                    BountyData.AddBounty(bounty);
                }
                PluginInstance.closeIndicators(target);
                PluginInstance.closeIndicators(hunter);
                bounty.hunt = null;
                HuntData.removeHunt(this);
                CooldownData.addCooldown(target);
            }
        }
    }
}﻿namespace Oxide.Plugins
{
    using System.Collections.Generic;

    partial class Bounties : RustPlugin
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
            {"bountyText", "WANTED! DEAD OR ALIVE!\n{0}\n{1}\nREWARD: {2}" },
            {"targetDeadBroadcast", "<size=30>{0} claims the {1} bounty on {2}'s head!</size>" },
            {"apiKey", "Please enter your steam API key in the config file. Get yours at https://steamcommunity.com/dev/apikey" }
        };
    }
}﻿namespace Oxide.Plugins
{
    using System;
    using UnityEngine;

    partial class Bounties : RustPlugin
    {
        partial void initData();

        partial void initCommands();

        partial void initLang();

        partial void initPermissions();

        partial void initGUI();

        private void Loaded()
        {
            try
            {
                initData();
            }
            catch(Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initCommands();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initLang();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initPermissions();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initGUI();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            if (!config.skullCrushing) Unsubscribe("OnItemAction");
        }

        void Unload()
        {
            PluginInstance = null;
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
                Bounty bounty = BountyData.GetBounty(note);
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
            Bounty bounty = BountyData.GetBounty(item);
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
            Bounty bounty = BountyData.GetBounty(item);
            if (bounty == null) return null;

            item.text = bounty.text;
            item.MarkDirty();

            return null;
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null) return;
            if (newItem != null)
            {
                if (newItem?.info?.shortname == "note" && newItem.HasFlag(global::Item.Flag.OnFire))
                {
                    Bounty bounty = BountyData.GetBounty(newItem);
                    if (bounty != null)
                    {
                        sendBounty(player, bounty);
                        return;
                    }
                }
            }
            closeBounty(player);
        }

        void OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null)
                return;

            BasePlayer killer = info.InitiatorPlayer;

            if (killer == null || killer.GetComponent<NPCPlayer>())
                return;

            OnPlayerKilled(victim, killer);
            return;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            BasePlayer victim = entity.ToPlayer();
            BasePlayer killer = info.InitiatorPlayer;

            if (victim == null || killer == null || killer.GetComponent<NPCPlayer>())
                return;

            OnPlayerKilled(victim, killer);
        }

        void OnPlayerKilled(BasePlayer victim, BasePlayer killer)
        {
#if DEBUG
            PrintToChat($"{killer?.displayName ?? "null"} kills {victim?.displayName ?? "null"}");
#endif

            Hunt hunt = null;
            hunt = HuntData.getHuntByTarget(victim);
            if(hunt != null) //Hunter kills target
            {
                if (hunt.hunter == killer && hunt.target == victim) hunt.end(hunt.hunter);
            }
            else hunt = HuntData.getHuntByHunter(victim);
            if(hunt != null) //target kills hunter
            {
                if (hunt.target == killer && hunt.hunter == victim) hunt.end(hunt.target);
            }
            return;
        }

        private object OnItemAction(Item item, string action)
        {
            if (action != "crush")
                return null;
            if (item.info.shortname != "skull.human")
                return null;
            string skullName = null;
            if (item.name != null)
                skullName = item.name.Substring(10, item.name.Length - 11);
            if (string.IsNullOrEmpty(skullName)) return null;

            BasePlayer killer = item.GetOwnerPlayer();
            BasePlayer victim = BasePlayer.Find(skullName);

            if (victim == null) return null;

#if DEBUG
            PrintToChat($"{killer} crushed {victim}'s skull!");
#endif

            Hunt hunt = null;
            hunt = HuntData.getHuntByTarget(victim);
            if (hunt == null) hunt = HuntData.getHuntByHunter(victim);
            if (hunt == null) return null;
            if (hunt.hunter == killer || hunt.target == killer)
            {
                hunt.end(killer);
                return null;
            }

            return null;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;
            Hunt hunt = HuntData.getHuntByTarget(player);
            if (hunt == null) hunt = HuntData.getHuntByHunter(player);
            if (hunt == null) return;
            hunt.initTicker();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            Hunt hunt = HuntData.getHuntByHunter(player);
            if (hunt == null) return;
            hunt.end();
            CooldownData.removeCooldown(hunt.target);
        }
    }
}﻿namespace Oxide.Plugins
{
    using System;

    partial class Bounties : RustPlugin
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
            use,
            mask,
            admin
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

    partial class Bounties : RustPlugin
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

        public void GetSteamUserData(ulong steamID, Action<PlayerSummary> callback)
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