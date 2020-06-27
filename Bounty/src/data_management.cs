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

            public static Hunt getHuntByHunter(BasePlayer player)
            {
                if (!initialized) init();
                return instance.hunts.Where((h) => h.hunterID == player.userID).First();
            }

            public static Hunt getHuntByTarget(BasePlayer player)
            {
                if (!initialized) init();
                return instance.hunts.Where((h) => h.bounty.targetID == player.userID).First();
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
}