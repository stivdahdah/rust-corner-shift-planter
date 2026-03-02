using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corner Shift Planter", "stevend", "1.1.0")]
    [Description("Allows players to plant only the four corner slots in a Large Planter Box while holding Shift. Toggleable via chat command with optional auto-disable.")]
    public class CornerShiftPlanter : RustPlugin
    {
        #region Fields

        private const string PermissionUse = "cornershiftplanter.use";

        private readonly HashSet<ulong> enabledPlayers = new HashSet<ulong>();
        private readonly Dictionary<ulong, Timer> autoDisableTimers = new Dictionary<ulong, Timer>();

        private PluginConfig config;

        #endregion

        #region Configuration

        private class PluginConfig
        {
            public bool LargePlanterOnly = true;
            public bool RequireShift = true;
            public float CenterThreshold = 0.2f;
            public int AutoDisableSeconds = 120; // 0 = disabled
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null)
                    throw new System.Exception();
            }
            catch
            {
                PrintWarning("Configuration file is invalid. Generating new default config.");
                LoadDefaultConfig();
            }

            SaveConfig();
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
                ["ModeEnabled"] = "Corner planting mode enabled{0}.",
                ["ModeDisabled"] = "Corner planting mode disabled.",
                ["AutoDisabled"] = "Corner planting mode automatically disabled."
            }, this);
        }

        private string Msg(string key, BasePlayer player)
        {
            return lang.GetMessage(key, this, player.UserIDString);
        }

        #endregion

        #region Initialization

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void Unload()
        {
            foreach (var timer in autoDisableTimers.Values)
                timer?.Destroy();

            autoDisableTimers.Clear();
            enabledPlayers.Clear();
        }

        #endregion

        #region Command

        [ChatCommand("corner")]
        private void CmdCorner(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage(Msg("NoPermission", player));
                return;
            }

            if (enabledPlayers.Contains(player.userID))
            {
                DisablePlayer(player, false);
            }
            else
            {
                EnablePlayer(player);
            }
        }

        private void EnablePlayer(BasePlayer player)
        {
            enabledPlayers.Add(player.userID);
            Subscribe(nameof(OnEntitySpawned));

            CancelAutoDisable(player.userID);

            string suffix = config.AutoDisableSeconds > 0
                ? $" for {config.AutoDisableSeconds} seconds"
                : string.Empty;

            player.ChatMessage(string.Format(Msg("ModeEnabled", player), suffix));

            if (config.AutoDisableSeconds > 0)
            {
                autoDisableTimers[player.userID] = timer.Once(config.AutoDisableSeconds, () =>
                {
                    if (player != null && player.IsConnected)
                        DisablePlayer(player, true);
                });
            }
        }

        private void DisablePlayer(BasePlayer player, bool automatic)
        {
            enabledPlayers.Remove(player.userID);
            CancelAutoDisable(player.userID);

            player.ChatMessage(Msg(automatic ? "AutoDisabled" : "ModeDisabled", player));

            if (enabledPlayers.Count == 0)
                Unsubscribe(nameof(OnEntitySpawned));
        }

        private void CancelAutoDisable(ulong userId)
        {
            if (autoDisableTimers.TryGetValue(userId, out Timer existing))
            {
                existing.Destroy();
                autoDisableTimers.Remove(userId);
            }
        }

        #endregion

        #region Core Logic

        private void OnEntitySpawned(BaseEntity entity)
        {
            GrowableEntity plant = entity as GrowableEntity;
            if (plant == null)
                return;

            NextTick(() =>
            {
                if (plant == null || plant.IsDestroyed)
                    return;

                PlanterBox planter = plant.GetParentEntity() as PlanterBox;
                if (planter == null)
                    return;

                if (config.LargePlanterOnly &&
                    planter.ShortPrefabName != "planter.large.deployed")
                    return;

                BasePlayer player = BasePlayer.FindByID(plant.OwnerID);
                if (player == null)
                    return;

                if (!enabledPlayers.Contains(player.userID))
                    return;

                if (config.RequireShift)
                {
                    if (player.serverInput == null ||
                        !player.serverInput.IsDown(BUTTON.SPRINT))
                        return;
                }

                Vector3 localPos = plant.transform.localPosition;

                if (Mathf.Abs(localPos.x) < config.CenterThreshold ||
                    Mathf.Abs(localPos.z) < config.CenterThreshold)
                {
                    RefundSeed(player, plant.ShortPrefabName);
                    plant.Kill();
                }
            });
        }

        #endregion

        #region Seed Refund

        private void RefundSeed(BasePlayer player, string prefabShortName)
        {
            string seedShortname = null;

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
            }

            if (string.IsNullOrEmpty(seedShortname))
                return;

            ItemDefinition def = ItemManager.FindItemDefinition(seedShortname);
            if (def == null)
                return;

            Item item = ItemManager.Create(def, 1);
            if (item != null)
                player.inventory.GiveItem(item);
        }

        #endregion
    }
}
