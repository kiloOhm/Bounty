// Requires: GUICreator

#define DEBUG

using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("bounties", "OHM & Bunsen", "2.0.0")]
    [Description("RP Bounty Hunting")]
    partial class bounties : RustPlugin
    {
        private static Plugins.bounties PluginInstance;

        public bounties()
        {
            PluginInstance = this;
        }

        #region helpers

        public string lastSeen(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder("last seen ");
            string grid = getGrid(player.transform.position);
            if (!string.IsNullOrEmpty(grid)) sb.Append($"in {grid} ");
            sb.Append(", wearing: ");
            if (player.inventory.containerWear.itemList.Count == 0) sb.Append("nothing");
            else
            {
                int i = 1;
                foreach (Item item in player.inventory.containerWear.itemList)
                {
                    sb.Append($"{item.info.displayName.english}");
                    if (i != player.inventory.containerWear.itemList.Count) sb.Append(", ");
                    i++;
                }
            }
            sb.Append($", {armedWith(player)}");
            return sb.ToString();
        }

        public string wearing(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder();
            if (player.inventory.containerWear.itemList.Count == 0) sb.Append("nothing");
            else
            {
                int i = 1;
                foreach (Item item in player.inventory.containerWear.itemList)
                {
                    sb.Append($"{item.info.displayName.english}");
                    if (i != player.inventory.containerWear.itemList.Count) sb.Append(", ");
                    i++;
                }
            }
            return sb.ToString();
        }

        public string armedWith(BasePlayer player)
        {
            StringBuilder sb = new StringBuilder();
            List<HeldEntity> heldEntities = new List<HeldEntity>();
            foreach (Item item in player.inventory.containerBelt.itemList.Concat(player.inventory.containerMain.itemList))
            {
                if (!item.info.isHoldable || item.GetHeldEntity() == null) continue;
                heldEntities.Add(item.GetHeldEntity() as HeldEntity);
            }
            if (heldEntities.Count == 0) sb.Append("unarmed");
            else if (heldEntities.Count == 1) sb.Append($"armed with {heldEntities[0].GetItem().info.displayName.english}");
            else
            {
                heldEntities = (from x in heldEntities orderby x.hostileScore descending select x).ToList();
                sb.Append($"armed with {heldEntities[0].GetItem().info.displayName.english}");
            }
            return sb.ToString();
        }

        string getGrid(Vector3 pos)
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f) - 1) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z}";
        }

        public BasePlayer findPlayer(string name, BasePlayer player)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ulong id;
            ulong.TryParse(name, out id);
            List<BasePlayer> results = BasePlayer.allPlayerList.Where((p) => p.displayName.Contains(name, System.Globalization.CompareOptions.IgnoreCase) || p.userID == id).ToList();
            if (results.Count == 0)
            {
                if (player != null) player.ChatMessage(lang.GetMessage("noPlayersFound", this, player.UserIDString));
            }
            else if (results.Count == 1)
            {
                return results[0];
            }
            else if (player != null)
            {
                player.ChatMessage(lang.GetMessage("multiplePlayersFound", this, player.UserIDString));
                int i = 1;
                foreach (BasePlayer p in results)
                {
                    player.ChatMessage($"{i}. {p.displayName}[{p.userID}]");
                    i++;
                }
            }
            return null;
        }

        public Item findItemByUID(PlayerInventory inventory, uint uid)
        {
            Item output = null;
            output = inventory.containerBelt.FindItemByUID(uid);
            if (output != null) return output;
            output = inventory.containerMain.FindItemByUID(uid);
            if (output != null) return output;
            output = inventory.containerWear.FindItemByUID(uid);
            if (output != null) return output;
            return null;
        }

        #endregion
    }
}