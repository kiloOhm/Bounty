namespace Oxide.Plugins
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
                if (bounty.hunt != null) PluginInstance.Puts($"Hunt Constructor: Bounty {bounty.placerName} -> {bounty.targetName} already has an ongoing hunt!");
                timestamp = DateTime.Now;
                this.bounty = bounty;
                hunterID = hunter.userID;
                hunterName = hunter.displayName;
                huntTimer = PluginInstance.timer.Once((float)config.huntDuration, () => end());
                ticker = PluginInstance.timer.Every(config.indicatorRefresh, () => tick());
                PluginInstance.sendHunterIndicator(hunter, this);
                PluginInstance.sendTargetIndicator(target, this);
                HuntData.addHunt(this);
                PluginInstance.LogToFile(logFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt started: {hunter.displayName} -> {target.displayName}", PluginInstance);
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

                huntTimer.Destroy();
                ticker.Destroy();
                bounty.hunt = null;
                HuntData.removeHunt(this);
                CooldownData.addCooldown(target);
                PluginInstance.closeIndicators(target);
                PluginInstance.closeIndicators(hunter);
            }
        }
    }
}