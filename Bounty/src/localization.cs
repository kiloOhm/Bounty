namespace Oxide.Plugins
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
}