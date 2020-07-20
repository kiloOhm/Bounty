namespace Oxide.Plugins
{
    using System;
    using UnityEngine;

    partial class Bounties : RustPlugin
    {
        partial void initData();

        partial void initCommands();

        partial void initLang();

        partial void initPermissions();

        partial void initGUI();

        private void Loaded()
        {
            try
            {
                initData();
            }
            catch(Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initCommands();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initLang();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initPermissions();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            try
            {
                initGUI();
            }
            catch (Exception e)
            {
                Puts(e.Message);
            }

            if (!config.skullCrushing) Unsubscribe("OnItemAction");
        }

        void Unload()
        {
            PluginInstance = null;
        }

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null) return null;
            //note.update UID Content
            BasePlayer player = arg.Player();
            if (player == null) return null;
            if (arg.cmd.FullName == "note.update")
            {
#if DEBUG
                player.ChatMessage($"note.update {arg.FullString}");
#endif
                Item note = findItemByUID(player.inventory, uint.Parse(arg.Args[0]));
                if (note == null) return null;
                Bounty bounty = BountyData.GetBounty(note);
                if (bounty == null) return null;
                timer.Once(0.2f, () =>
                {
                    note.text = bounty.text;
                    note.MarkDirty();
                });
            }
            return null;
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            WorldItem wItem = item.GetWorldEntity() as WorldItem;
            if (wItem.item == null) return null;
            if (item.info.shortname == "note")
            {
                PortableBounty pBounty = wItem.gameObject.GetComponent<PortableBounty>();
                if (pBounty != null)
                {
                    Bounty bounty = pBounty.bounty;
                    if (bounty == null)
                    {
                        Puts($"pBounty.bounty on Note[{item.uid}] is null. This shouldn't happen!");
                        return null;
                    }
                    BountyData.AddBounty(bounty);
                }
            }

            return null;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.info.shortname != "note") return;
            if (!item.HasFlag(global::Item.Flag.OnFire)) return;
            Bounty bounty = BountyData.GetBounty(item);
            if (bounty == null) return;

            //attach portableBounty
            WorldItem wItem = entity as WorldItem;
            PortableBounty pBounty = wItem.gameObject.AddComponent<PortableBounty>();
            pBounty.bounty = bounty;

            //remove Bounty from Data
            BountyData.removeBounty(item.uid);
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            //display tooltip "put the bounty into your hotbar and select it to start hunting"
            return null;
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item == null) return null;
            if (item.info.shortname != "note") return null;
            if (!item.HasFlag(global::Item.Flag.OnFire)) return null;
            Bounty bounty = BountyData.GetBounty(item);
            if (bounty == null) return null;

            item.text = bounty.text;
            item.MarkDirty();

            return null;
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null) return;
            if (newItem != null)
            {
                if (newItem?.info?.shortname == "note" && newItem.HasFlag(global::Item.Flag.OnFire))
                {
                    Bounty bounty = BountyData.GetBounty(newItem);
                    if (bounty != null)
                    {
                        sendBounty(player, bounty);
                        return;
                    }
                }
            }
            closeBounty(player);
        }

        void OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || info == null)
                return;

            BasePlayer killer = info.InitiatorPlayer;

            if (killer == null || killer.GetComponent<NPCPlayer>())
                return;

            OnPlayerKilled(victim, killer);
            return;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            BasePlayer victim = entity.ToPlayer();
            BasePlayer killer = info.InitiatorPlayer;

            if (victim == null || killer == null || killer.GetComponent<NPCPlayer>())
                return;

            OnPlayerKilled(victim, killer);
        }

        void OnPlayerKilled(BasePlayer victim, BasePlayer killer)
        {
#if DEBUG
            PrintToChat($"{killer?.displayName ?? "null"} kills {victim?.displayName ?? "null"}");
#endif

            Hunt hunt = null;
            hunt = HuntData.getHuntByTarget(victim);
            if(hunt != null) //Hunter kills target
            {
                if (hunt.hunter == killer && hunt.target == victim) hunt.end(hunt.hunter);
            }
            else hunt = HuntData.getHuntByHunter(victim);
            if(hunt != null) //target kills hunter
            {
                if (hunt.target == killer && hunt.hunter == victim) hunt.end(hunt.target);
            }
            return;
        }

        private object OnItemAction(Item item, string action)
        {
            if (action != "crush")
                return null;
            if (item.info.shortname != "skull.human")
                return null;
            string skullName = null;
            if (item.name != null)
                skullName = item.name.Substring(10, item.name.Length - 11);
            if (string.IsNullOrEmpty(skullName)) return null;

            BasePlayer killer = item.GetOwnerPlayer();
            BasePlayer victim = BasePlayer.Find(skullName);

            if (victim == null) return null;

#if DEBUG
            PrintToChat($"{killer} crushed {victim}'s skull!");
#endif

            Hunt hunt = null;
            hunt = HuntData.getHuntByTarget(victim);
            if (hunt == null) hunt = HuntData.getHuntByHunter(victim);
            if (hunt == null) return null;
            if (hunt.hunter == killer || hunt.target == killer)
            {
                hunt.end(killer);
                return null;
            }

            return null;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null) return;
            Hunt hunt = HuntData.getHuntByTarget(player);
            if (hunt == null) hunt = HuntData.getHuntByHunter(player);
            if (hunt == null) return;
            hunt.initTicker();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            Hunt hunt = HuntData.getHuntByHunter(player);
            if (hunt == null) return;
            hunt.end();
            CooldownData.removeCooldown(hunt.target);
        }
    }
}