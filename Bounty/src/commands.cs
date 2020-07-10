namespace Oxide.Plugins
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
                    Hunt hunt = HuntData.getHuntByHunter(player);
                    if (hunt == null) hunt = HuntData.getHuntByTarget(player);
                    if (hunt == null) return;
                    hunt.end();
                    player.ChatMessage("removed hunt");
                    break;
            }
        }


    }
}