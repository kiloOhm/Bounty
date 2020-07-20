namespace Oxide.Plugins
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
}