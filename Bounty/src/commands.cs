namespace Oxide.Plugins
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

            if (args.Length == 0)
            {
                sendCreator(player);
                return;
            }
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
            GetSteamUserData(ulong.Parse(args[0]), (ps) => 
            {
                if (ps == null) return;
                player.ChatMessage(ps.personaname);
            });
        }

        private void consoleTestCommand(ConsoleSystem.Arg arg)
        {
            GetSteamUserData(ulong.Parse(arg.Args[0]), (ps) =>
            {
                if (ps == null) return;
                SendReply(arg, ps.personaname);
            });
        }
    }
}