using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Corner Shift Planter", "YourName", "1.0.1")]
    [Description("Allows players to plant only the four corner slots in a Large Planter Box while holding Shift, when enabled via command, with optional auto-disable.")]
    public class CornerShiftPlanter : RustPlugin
    {
        private const string PermissionUse = "cornershiftplanter.use";

        private readonly HashSet<ulong> _enabledPlayers = new HashSet<ulong>();
        private readonly Dictionary<ulong, Timer> _autoDisableTimers = new Dictionary<ulong, Timer>();

        private PluginConfig _config;

        #region Configuration

        private class PluginConfig
        {
            public bool LargePlanterOnly = true;
            public bool RequireShift = true;
            public float CenterThreshold = 0.2f;

            // Auto-disable after X seconds (0 = never auto-disable)
            public int AutoDisableSeconds = 120;
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>() ?? new PluginConfig();
            }
            catch
            {
                PrintWarning("Config is invalid/corrupt. Recreating default config.");
                _config = new PluginConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Localization (Lang API)

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

        private string Msg(string key, BasePlayer player) =>
            lang.GetMessage(key, this, player?.UserIDString);

        #endregion

        #region Lifecycle

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);

            // Only subscribe when needed
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void Unload()
        {
            foreach (var t in _autoDisableTimers.Values)
                t?.Destroy();

            _autoDisableTimers.Clear();
            _enabledPlayers.Clear();
        }

        #endregion

        #region Command

        [ChatCommand("corner")]
        private void CmdCorner(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage(Msg("NoPermission", player));
                return;
            }

            if (_enabledPlayers.Contains(player.userID))
            {
                DisablePlayer(player, auto: false);
                return;
            }

            EnablePlayer(player);
        }

        private void EnablePlayer(BasePlayer player)
        {
            _enabledPlayers.Add(player.userID);

            // Ensure hook is active while at least one player is enabled
            Subscribe(nameof(OnEntitySpawned));

            // Cancel existing timer (if any)
            CancelAutoDisableTimer(player.userID);

            string suffix = _config.AutoDisableSeconds > 0
                ? $" for {_config.AutoDisableSeconds} seconds"
                : string.Empty;

            player.ChatMessage(string.Format(Msg("ModeEnabled", player), suffix));

            if (_config.AutoDisableSeconds > 0)
            {
                Timer playerTimer = this.timer.Once(_config.AutoDisableSeconds, () =>
                {
                    if (player == null || !player.IsConnected) return;
                    DisablePlayer(player, auto: true);
                });

                _autoDisableTimers[player.userID] = playerTimer;
            }
        }

        private void DisablePlayer(BasePlayer player, bool auto)
        {
            _enabledPlayers.Remove(player.userID);
            CancelAutoDisableTimer(player.userID);

            player.ChatMessage(Msg(auto ? "AutoDisabled" : "ModeDisabled", player));

            // If no one is using it, stop listening to spawn events
            if (_enabledPlayers.Count == 0)
                Unsubscribe(nameof(OnEntitySpawned));
        }

        private void CancelAutoDisableTimer(ulong userId)
        {
            if (_autoDisableTimers.TryGetValue(userId, out var existing))
            {
                existing?.Destroy();
                _autoDisableTimers.Remove(userId);
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

                if (_config.LargePlanterOnly && planter.ShortPrefabName != "planter.large.deployed")
                    return;

                BasePlayer player = BasePlayer.FindByID(plant.OwnerID);
                if (player == null) return;

                if (!_enabledPlayers.Contains(player.userID))
                    return;

                if (_config.RequireShift)
                {
                    if (player.serverInput == null) return;
                    if (!player.serverInput.IsDown(BUTTON.SPRINT)) return;
                }

                Vector3 localPos = plant.transform.localPosition;

                // Corner slots have both x and z far from 0. Middle row/col has x or z near 0.
                if (Mathf.Abs(localPos.x) < _config.CenterThreshold || Mathf.Abs(localPos.z) < _config.CenterThreshold)
                {
                    int seedItemId = GetSeedItemId(plant.ShortPrefabName);
                    if (seedItemId != 0)
                    {
                        // Refund 1 seed for each deleted plant
                        player.inventory.GiveItem(ItemManager.CreateByItemID(seedItemId, 1));
                    }

                    plant.Kill();
                }
            });
        }

        #endregion

        #region Seed Mapping

        private int GetSeedItemId(string prefabShortName)
        {
            string seedShortname;
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
