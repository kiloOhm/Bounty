namespace Oxide.Plugins
{
    using Facepunch.Extend;
    using System;
    using System.Collections.Generic;
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

        GuiColor darkGreen = new GuiColor(67, 84, 37, 0.8f);
        GuiColor lightGreen = new GuiColor(134, 190, 41, 0.8f);

        GuiColor darkRed = new GuiColor(65, 33, 32, 0.8f);
        GuiColor lightRed = new GuiColor(162, 51, 46, 0.8f);
        #endregion

        #region bounty creator

        public void sendCreator(BasePlayer player)
        {

        }

        #endregion

        public void sendBounty(BasePlayer player, Bounty bounty)
        {
#if DEBUG
            player.ChatMessage($"sendBounty: {bounty.placerName} -> {bounty.targetName}");
#endif
            GuiContainer c = new GuiContainer(this, "bounty");

            //template
            Rectangle templatePos = new Rectangle(623, 26, 673, 854, resX, resY, true);
            c.addImage("template", templatePos, "bounty_template", GuiContainer.Layer.hud, FadeIn: FadeIn, FadeOut: FadeOut);
           
            //targetName
            Rectangle targetNamePos = new Rectangle(680, 250, 560, 65, resX, resY, true);
            int fontsize = guiCreator.getFontsizeByFramesize(bounty.targetName.Length, targetNamePos);
            GuiText targetNameText = new GuiText(bounty.targetName, fontsize);
            c.addText("targetName", targetNamePos, GuiContainer.Layer.hud, targetNameText, FadeIn, FadeOut);
           
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
            huntButton(player, bounty);

            c.display(player);
        }

        public enum huntErrorType {none ,hunterAlreadyHunting, hunterCooldown , targetAlreadyHunted, targetCooldown};
        public void huntButton(BasePlayer player, Bounty bounty, huntErrorType error = huntErrorType.none)
        {
            GuiContainer c = new GuiContainer(this, "huntButton", "bounty");
            Rectangle ButtonPos = new Rectangle(710, 856, 500, 100, resX, resY, true);

            List<GuiText> textOptions = new List<GuiText>
            {
                new GuiText("Start Hunting", guiCreator.getFontsizeByFramesize(13, ButtonPos), lightGreen),
                new GuiText("You are already hunting someone else", guiCreator.getFontsizeByFramesize(36, ButtonPos), lightRed),
                new GuiText("You need to wait until you can hunt again", guiCreator.getFontsizeByFramesize(41, ButtonPos), lightRed),
                new GuiText("Target is already being hunted", guiCreator.getFontsizeByFramesize(30, ButtonPos), lightRed),
                new GuiText("Target can't be hunted yet", guiCreator.getFontsizeByFramesize(26, ButtonPos), lightRed)
            };

            Action<BasePlayer, string[]> cb = (p, a) =>
            {
                if (bounty.hunt != null || HuntData.getHuntByHunter(p) != null)
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                    huntButton(player, bounty, huntErrorType.hunterAlreadyHunting);
                }
                else if(false) //hunterCooldown
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                    huntButton(player, bounty, huntErrorType.hunterCooldown);
                }
                else if(HuntData.getHuntByTarget(bounty.target) != null)
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                    huntButton(player, bounty, huntErrorType.targetAlreadyHunted);
                }
                else if(false) //targetCooldown
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                    huntButton(player, bounty, huntErrorType.targetCooldown);
                }
                else
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", player.transform.position);
                    bounty.startHunt(p);
                }
            };

            Action<BasePlayer, string[]> errorCB = (p, a) =>
            {
                PluginInstance.timer.Once(2f, () =>
                {
                    huntButton(player, bounty);
                });
            };

            c.addPlainButton("button", ButtonPos, GuiContainer.Layer.hud, (error == huntErrorType.none)?darkGreen:darkRed, FadeIn, FadeOut, textOptions[(int)error], cb);
            c.display(player);
        }

        public void closeBounty(BasePlayer player)
        {
            //for bounty and creator
#if DEBUG
            player.ChatMessage($"closeBounty");
#endif
            GuiTracker.getGuiTracker(player).destroyGui(this, "bounty");
        }

        public void sendHunterIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendHunterIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
        }

        public void sendTargetIndicator(BasePlayer player, Hunt hunt)
        {
#if DEBUG
            player.ChatMessage($"sendTargetIndicator: {hunt.hunterName} -> {hunt.bounty.targetName}");
#endif
        }

        public void closeIndicators(BasePlayer player)
        {
#if DEBUG
            player.ChatMessage($"closeIndicators: {player.displayName}");
#endif
        }
    }
}