namespace Oxide.Plugins
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

    partial class bounties : RustPlugin
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

            //button
            if (bounty.hunt != null) huntButton(player, bounty, huntErrorType.huntActive);
            else huntButton(player, bounty);

            c.display(player);
        }

        public enum huntErrorType {none ,hunterAlreadyHunting, targetAlreadyHunted, targetCooldown, huntActive, selfHunt};
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
                new GuiText("You can't hunt yourself!",guiCreator.getFontsizeByFramesize(24, ButtonPos), lightRed)
            };

            Action<BasePlayer, string[]> cb = (p, a) =>
            {
                TimeSpan targetCooldown = TimeSpan.Zero;
                TimeSpan creationCooldown = new TimeSpan(0, 0, config.creationCooldown - (int)bounty.timeSinceCreation.TotalSeconds);
                if (error == huntErrorType.huntActive) return;
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
                else if(bounty.timeSinceCreation.TotalSeconds < config.creationCooldown || CooldownData.isOnCooldown(bounty.target, out targetCooldown))
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.targetCooldown);
                    TimeSpan select = creationCooldown;
                    if (targetCooldown > creationCooldown) select = targetCooldown;
                    player.ChatMessage($"Cooldown: {select.ToString(@"hh\:mm\:ss")}");
                }
                else if(bounty.target == player)
                {
                    Effect.server.Run(errorSound, player.transform.position);
                    huntButton(player, bounty, huntErrorType.selfHunt);
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
            GuiContainer c = new GuiContainer(this, "hunterIndicator");

            //Background
            Rectangle bgPos = new Rectangle(50, 100, 350, 150, resX, resY, true);
            c.addPlainPanel("bg", bgPos, GuiContainer.Layer.hud, lightGrey, 0, 0, GuiContainer.Blur.medium);

            //Name
            Rectangle namePos = new Rectangle(50, 100, 350, 35, resX, resY, true);
            string nameTextString = $"You are hunting {hunt.target.displayName}";
            int fontsize = guiCreator.getFontsizeByFramesize(nameTextString.Length, namePos);
            GuiText nameText = new GuiText(nameTextString, fontsize, opaqueWhite);
            c.addText("name", namePos, GuiContainer.Layer.hud, nameText);

            //Last Seen
            Rectangle lastSeenPos = new Rectangle(50, 135, 350, 80, resX, resY, true);
            string lastSeenString = lastSeen(hunt.target);
            GuiText lastSeenText = new GuiText(lastSeenString, 10, opaqueWhite);
            c.addText("lastSeen", lastSeenPos, GuiContainer.Layer.hud, lastSeenText);

            //Countdown
            Rectangle countdownPos = new Rectangle(50, 215, 350, 35, resX, resY, true);
            GuiText countdownText = new GuiText(hunt.remaining.ToString(@"hh\:mm\:ss"), 20, opaqueWhite);
            c.addText("countdown", countdownPos, GuiContainer.Layer.hud, countdownText);

            c.display(player);
        }

        public void sendTargetIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendTargetIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
            GuiContainer c = new GuiContainer(this, "targetIndicator");

            //Background
            Rectangle bgPos = new Rectangle(50, 250, 350, 100, resX, resY, true);
            float distance = Vector3.Distance(hunt.hunter.transform.position, hunt.target.transform.position);
            GuiColor bgColor = gradientRedYellowGreen(Mathf.Clamp((distance / config.gradientBase), 0, 1));
            bgColor.setAlpha(0.5f);
            c.addPlainPanel("Background", bgPos, GuiContainer.Layer.hud, bgColor, 0, 0, GuiContainer.Blur.medium);

            //TopLine
            Rectangle topLinePos = new Rectangle(50, 250, 350, 50, resX, resY, true);
            string TopLineString = $"You are being hunted{(config.showHunter?$" by {hunt.hunter.displayName}":"")}!";
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
            GuiTracker.getGuiTracker(player).destroyGui(this, "hunterIndicator");
            GuiTracker.getGuiTracker(player).destroyGui(this, "targetIndicator");
        }

        public void huntExpiredMsg(Hunt hunt)
        {
            guiCreator.customGameTip(hunt.hunter, "The hunt is over. Better luck next time!", 5);
            guiCreator.customGameTip(hunt.target, "The hunt is over. You're safe... for now...", 5);

            LogToFile(logFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt expired: {hunt.hunter.displayName} -> {hunt.target.displayName}", this);
        }

        public void huntSuccessfullMsg(Hunt hunt)
        {
            guiCreator.prompt(hunt.hunter, $"You've successfully hunted down {hunt.target.displayName}!\n{hunt.bounty.reward.amount} {hunt.bounty.reward.info.displayName.english} have been transferred to your inventory!", "Hunt successful!");
            if (config.broadcastHunt) PrintToChat($"<color=#00ff33>{hunt.hunter.displayName} claims the bounty of {hunt.bounty.rewardAmount} {hunt.bounty.reward.info.displayName.english} on {hunt.target.displayName}'s head!</color>\nRIP {hunt.target.displayName}!");

            LogToFile(logFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")} Hunt successful: {hunt.hunter.displayName} -> {hunt.target.displayName} ", this);
        }

        public void huntFailedMsg(Hunt hunt)
        {
            guiCreator.prompt(hunt.target, $"You've successfully defended yourself from {hunt.hunter.displayName}!\n{hunt.bounty.reward.amount} {hunt.bounty.reward.info.displayName.english} have been transferred to your inventory!", "Hunt averted!");
            if (config.broadcastHunt) PrintToChat($"<color=#00ff33>{hunt.target.displayName} fends off his hunter {hunt.hunter.displayName} and claims {hunt.bounty.rewardAmount} {hunt.bounty.reward.info.displayName.english}</color>\nBetter luck next time {hunt.hunter.displayName}!");

            LogToFile(logFileName, $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}Hunt failed: {hunt.hunter.displayName} -> {hunt.target.displayName}", this);
        }

        public GuiColor gradientRedYellowGreen(float level)
        {
            float r = (level < 0.5f)?1:(1-level)*2;
            float g = level;
            float b = 0;
            return new GuiColor(r, g, b, 1);
        }
    }
}