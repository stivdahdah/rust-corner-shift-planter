using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corner Shift Planter", "stevend", "1.2.0")]
    [Description("Allows players to plant only in the four corner slots of a Large Planter Box while holding SHIFT. Includes guide and simple corner-mode toggle. Permission: cornershiftplanter.use")]
    public class CornerShiftPlanter : RustPlugin
    {
        #region Fields

        private const string PermissionUse = "cornershiftplanter.use";

        private readonly HashSet<ulong> _cornerEnabledPlayers = new HashSet<ulong>();
        private readonly Dictionary<ulong, Oxide.Plugins.Timer> _autoDisableTimers =
            new Dictionary<ulong, Oxide.Plugins.Timer>();

        private PluginConfig _config;

        #endregion

        #region Config

        private class PluginConfig
        {
            public bool LargePlanterOnly = true;
            public bool RequireShift = true;
            public float CenterThreshold = 0.20f;
            public int AutoDisableSeconds = 120;
            public bool RefundSeeds = true;
            public bool ShowGuideOnEnable = true;
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
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                    throw new Exception("Config file was null.");
            }
            catch (Exception ex)
            {
                PrintWarning($"Configuration error detected, generating new config. Reason: {ex.Message}");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this feature.",
                ["CornerEnabled"] = "Corner planting mode enabled{0}.",
                ["CornerDisabled"] = "Corner planting mode disabled.",
                ["AutoDisabled"] = "Corner planting mode automatically disabled.",
                ["GuideHeader"] = "Genetics corner layout:",
                ["GuideLine1"] = "C  X  C",
                ["GuideLine2"] = "X  X  X",
                ["GuideLine3"] = "C  X  C",
                ["GuideFooter"] = "C = plant allowed, X = keep empty",
                ["InvalidPlant"] = "Invalid planting slot. Only the 4 corner slots are allowed in Corner Mode.",
                ["HelpToggle"] = "/corner - toggle corner planting mode",
                ["HelpGuide"] = "/corner guide - show allowed planting pattern",
                ["HelpOn"] = "/corner on - enable corner planting mode",
                ["HelpOff"] = "/corner off - disable corner planting mode",
                ["HelpHelp"] = "/corner help - show this help",
                ["NeedShift"] = "You must hold SHIFT while planting for Corner Mode to apply."
            }, this);
        }

        private string Msg(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player?.UserIDString);
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void OnServerInitialized()
        {
            Puts("Corner Shift Planter loaded successfully.");
        }

        private void Unload()
        {
            foreach (var timer in _autoDisableTimers.Values)
            {
                timer?.Destroy();
            }

            _autoDisableTimers.Clear();
            _cornerEnabledPlayers.Clear();
        }

        #endregion

        #region Commands

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

            if (args != null && args.Length > 0)
            {
                var sub = args[0].ToLowerInvariant();

                switch (sub)
                {
                    case "guide":
                    case "guid":
                        ShowGuide(player);
                        return;

                    case "on":
                        if (!_cornerEnabledPlayers.Contains(player.userID))
                            EnableCornerMode(player);
                        return;

                    case "off":
                        if (_cornerEnabledPlayers.Contains(player.userID))
                            DisableCornerMode(player, false);
                        return;

                    case "help":
                        ShowHelp(player);
                        return;
                }

                ShowHelp(player);
                return;
            }

            ToggleCornerMode(player);
        }

        private void ToggleCornerMode(BasePlayer player)
        {
            if (_cornerEnabledPlayers.Contains(player.userID))
            {
                DisableCornerMode(player, false);
                return;
            }

            EnableCornerMode(player);
        }

        private void EnableCornerMode(BasePlayer player)
        {
            _cornerEnabledPlayers.Add(player.userID);
            Subscribe(nameof(OnEntitySpawned));

            CancelAutoDisable(player.userID);

            string suffix = _config.AutoDisableSeconds > 0
                ? $" for {_config.AutoDisableSeconds} seconds"
                : string.Empty;

            player.ChatMessage(string.Format(Msg("CornerEnabled", player), suffix));

            if (_config.ShowGuideOnEnable)
            {
                ShowGuide(player);
            }

            if (_config.RequireShift)
            {
                player.ChatMessage(Msg("NeedShift", player));
            }

            if (_config.AutoDisableSeconds > 0)
            {
                _autoDisableTimers[player.userID] = timer.Once(_config.AutoDisableSeconds, () =>
                {
                    if (player != null && player.IsConnected)
                    {
                        DisableCornerMode(player, true);
                    }
                });
            }
        }

        private void DisableCornerMode(BasePlayer player, bool automatic)
        {
            _cornerEnabledPlayers.Remove(player.userID);
            CancelAutoDisable(player.userID);

            player.ChatMessage(Msg(automatic ? "AutoDisabled" : "CornerDisabled", player));

            if (_cornerEnabledPlayers.Count == 0)
            {
                Unsubscribe(nameof(OnEntitySpawned));
            }
        }

        private void CancelAutoDisable(ulong userId)
        {
            Oxide.Plugins.Timer existing;
            if (_autoDisableTimers.TryGetValue(userId, out existing))
            {
                existing?.Destroy();
                _autoDisableTimers.Remove(userId);
            }
        }

        private void ShowGuide(BasePlayer player)
        {
            player.ChatMessage(Msg("GuideHeader", player));
            player.ChatMessage(Msg("GuideLine1", player));
            player.ChatMessage(Msg("GuideLine2", player));
            player.ChatMessage(Msg("GuideLine3", player));
            player.ChatMessage(Msg("GuideFooter", player));
        }

        private void ShowHelp(BasePlayer player)
        {
            player.ChatMessage(Msg("HelpToggle", player));
            player.ChatMessage(Msg("HelpGuide", player));
            player.ChatMessage(Msg("HelpOn", player));
            player.ChatMessage(Msg("HelpOff", player));
            player.ChatMessage(Msg("HelpHelp", player));
        }

        #endregion

        #region Core Logic

        private void OnEntitySpawned(BaseEntity entity)
        {
            var plant = entity as GrowableEntity;
            if (plant == null)
                return;

            NextTick(() =>
            {
                if (plant == null || plant.IsDestroyed)
                    return;

                var planter = plant.GetParentEntity() as PlanterBox;
                if (planter == null)
                    return;

                if (_config.LargePlanterOnly && planter.ShortPrefabName != "planter.large.deployed")
                    return;

                var player = BasePlayer.FindByID(plant.OwnerID);
                if (player == null)
                    return;

                if (!_cornerEnabledPlayers.Contains(player.userID))
                    return;

                if (_config.RequireShift)
                {
                    if (player.serverInput == null || !player.serverInput.IsDown(BUTTON.SPRINT))
                        return;
                }

                var localPos = plant.transform.localPosition;

                bool isCornerAllowed = IsAllowedCorner(localPos, _config.CenterThreshold);

                if (isCornerAllowed)
                    return;

                if (_config.RefundSeeds)
                {
                    RefundSeed(player, plant.ShortPrefabName);
                }

                plant.Kill();
            });
        }

        private bool IsAllowedCorner(Vector3 localPos, float centerThreshold)
        {
            bool xIsCenter = Mathf.Abs(localPos.x) < centerThreshold;
            bool zIsCenter = Mathf.Abs(localPos.z) < centerThreshold;

            // Only 4 corners allowed:
            // reject center row or center column
            if (xIsCenter || zIsCenter)
                return false;

            return true;
        }

        #endregion

        #region Seed Refund

        private void RefundSeed(BasePlayer player, string prefabShortName)
        {
            string seedShortname = GetSeedShortname(prefabShortName);
            if (string.IsNullOrEmpty(seedShortname))
                return;

            var def = ItemManager.FindItemDefinition(seedShortname);
            if (def == null)
                return;

            var item = ItemManager.Create(def, 1);
            if (item == null)
                return;

            player.inventory.GiveItem(item);
        }

        private string GetSeedShortname(string prefabShortName)
        {
            switch (prefabShortName)
            {
                case "hemp.entity":
                    return "seed.hemp";

                case "pumpkin.entity":
                    return "seed.pumpkin";

                case "corn.entity":
                    return "seed.corn";

                case "potato.entity":
                    return "seed.potato";

                case "berry.black.entity":
                    return "seed.black.berry";

                case "berry.blue.entity":
                    return "seed.blue.berry";

                case "berry.green.entity":
                    return "seed.green.berry";

                case "berry.red.entity":
                    return "seed.red.berry";

                case "berry.white.entity":
                    return "seed.white.berry";

                case "berry.yellow.entity":
                    return "seed.yellow.berry";
            }

            return null;
        }

        #endregion
    }
}
