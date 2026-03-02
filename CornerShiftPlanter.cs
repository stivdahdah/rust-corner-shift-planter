using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corner Shift Planter", "Steven D.", "1.0.0")]
    [Description("Allows players to plant only the four corner slots in a Large Planter Box while holding Shift, when enabled via command.")]

    public class CornerShiftPlanter : RustPlugin
    {
        private const string PermissionUse = "cornershiftplanter.use";

        private HashSet<ulong> enabledPlayers = new HashSet<ulong>();
        private PluginConfig config;

        #region Configuration

        private class PluginConfig
        {
            public bool LargePlanterOnly = true;
            public bool RequireShift = true;
            public float CenterThreshold = 0.2f;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>() ?? new PluginConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this feature.",
                ["ModeEnabled"] = "Corner planting mode enabled.",
                ["ModeDisabled"] = "Corner planting mode disabled."
            }, this);
        }

        private string Msg(string key, BasePlayer player) =>
            lang.GetMessage(key, this, player.UserIDString);

        #endregion

        void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
        }

        void Unload()
        {
            enabledPlayers.Clear();
        }

        #region Command

        [ChatCommand("corner")]
        private void ToggleCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage(Msg("NoPermission", player));
                return;
            }

            if (enabledPlayers.Contains(player.userID))
            {
                enabledPlayers.Remove(player.userID);
                player.ChatMessage(Msg("ModeDisabled", player));
            }
            else
            {
                enabledPlayers.Add(player.userID);
                player.ChatMessage(Msg("ModeEnabled", player));
            }
        }

        #endregion

        #region Core Logic

        private void OnEntitySpawned(BaseEntity entity)
        {
            GrowableEntity plant = entity as GrowableEntity;
            if (plant == null) return;

            NextTick(() =>
            {
                if (plant == null || plant.IsDestroyed) return;

                PlanterBox planter = plant.GetParentEntity() as PlanterBox;
                if (planter == null) return;

                if (config.LargePlanterOnly &&
                    planter.ShortPrefabName != "planter.large.deployed")
                    return;

                BasePlayer player = BasePlayer.FindByID(plant.OwnerID);
                if (player == null) return;

                if (!enabledPlayers.Contains(player.userID))
                    return;

                if (config.RequireShift &&
                    !player.serverInput.IsDown(BUTTON.SPRINT))
                    return;

                Vector3 localPos = plant.transform.localPosition;

                if (Mathf.Abs(localPos.x) < config.CenterThreshold ||
                    Mathf.Abs(localPos.z) < config.CenterThreshold)
                {
                    int seedItemId = GetSeedItemId(plant.ShortPrefabName);
                    if (seedItemId != 0)
                    {
                        player.inventory.GiveItem(
                            ItemManager.CreateByItemID(seedItemId, 1)
                        );
                    }

                    plant.Kill();
                }
            });
        }

        #endregion

        #region Seed Mapping

        private int GetSeedItemId(string prefabShortName)
        {
            string seedShortname = "";

            switch (prefabShortName)
            {
                case "hemp.entity": seedShortname = "seed.hemp"; break;
                case "pumpkin.entity": seedShortname = "seed.pumpkin"; break;
                case "corn.entity": seedShortname = "seed.corn"; break;
                case "potato.entity": seedShortname = "seed.potato"; break;
                case "berry.black.entity": seedShortname = "seed.black.berry"; break;
                case "berry.blue.entity": seedShortname = "seed.blue.berry"; break;
                case "berry.green.entity": seedShortname = "seed.green.berry"; break;
                case "berry.red.entity": seedShortname = "seed.red.berry"; break;
                case "berry.white.entity": seedShortname = "seed.white.berry"; break;
                case "berry.yellow.entity": seedShortname = "seed.yellow.berry"; break;
                default: return 0;
            }

            ItemDefinition def = ItemManager.FindItemDefinition(seedShortname);
            return def != null ? def.itemid : 0;
        }

        #endregion
    }
}