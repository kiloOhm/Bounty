namespace Oxide.Plugins
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
                    return "To start hunting, put this note in your hotbar and select it!";
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
}