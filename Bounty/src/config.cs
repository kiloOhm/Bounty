namespace Oxide.Plugins
{
    using Newtonsoft.Json;

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

            [JsonProperty(PropertyName = "Show hunter name to target")]
            public bool showHunter;

            [JsonProperty(PropertyName = "Broadcast hunt conclusion in global chat")]
            public bool broadcastHunt;

            [JsonProperty(PropertyName = "Show steam profile picture of target on bounty")]
            public bool showSteamImage;

            [JsonProperty(PropertyName = "Require reason for bounties")]
            public bool requireReason;

            [JsonProperty(PropertyName = "Allow skull crushing as confirmation")]
            public bool skullCrushing;
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
                showHunter = true,
                broadcastHunt = true,
                showSteamImage = true,
                requireReason = true,
                skullCrushing = true
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
}