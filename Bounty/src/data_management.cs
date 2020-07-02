namespace Oxide.Plugins
{
    using Newtonsoft.Json;
    using Oxide.Core;
    using Oxide.Core.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    partial class bounties : RustPlugin
    {
        partial void initData()
        {
            BountyData.init();
            HuntData.init();
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
            private static HuntData instance;
            private static bool initialized = false;

            [JsonProperty(PropertyName = "Hunt list")]
            public List<Hunt> hunts = new List<Hunt>();

            public HuntData()
            {
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
                if (instance.cooldowns.ContainsKey(player.userID)) return;
                instance.cooldowns.Add(player.userID, DateTime.Now);
                save();
            }

            public static bool isOnCooldown(BasePlayer player)
            {
                if (!instance.cooldowns.ContainsKey(player.userID)) return false;
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
}