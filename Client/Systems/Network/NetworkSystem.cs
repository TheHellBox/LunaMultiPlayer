﻿using LunaClient.Base;
using LunaClient.Network;
using LunaClient.Systems.Admin;
using LunaClient.Systems.Asteroid;
using LunaClient.Systems.Chat;
using LunaClient.Systems.CraftLibrary;
using LunaClient.Systems.Facility;
using LunaClient.Systems.Flag;
using LunaClient.Systems.GameScene;
using LunaClient.Systems.Groups;
using LunaClient.Systems.Handshake;
using LunaClient.Systems.KerbalSys;
using LunaClient.Systems.Lock;
using LunaClient.Systems.Motd;
using LunaClient.Systems.PlayerColorSys;
using LunaClient.Systems.PlayerConnection;
using LunaClient.Systems.Scenario;
using LunaClient.Systems.SettingsSys;
using LunaClient.Systems.Status;
using LunaClient.Systems.TimeSyncer;
using LunaClient.Systems.Toolbar;
using LunaClient.Systems.VesselDockSys;
using LunaClient.Systems.VesselFlightStateSys;
using LunaClient.Systems.VesselImmortalSys;
using LunaClient.Systems.VesselLockSys;
using LunaClient.Systems.VesselPositionSys;
using LunaClient.Systems.VesselPrecalcSys;
using LunaClient.Systems.VesselProtoSys;
using LunaClient.Systems.VesselRangeSys;
using LunaClient.Systems.VesselRemoveSys;
using LunaClient.Systems.VesselResourceSys;
using LunaClient.Systems.VesselStateSys;
using LunaClient.Systems.VesselSwitcherSys;
using LunaClient.Systems.VesselSyncSys;
using LunaClient.Systems.VesselUpdateSys;
using LunaClient.Systems.Warp;
using LunaClient.Utilities;
using LunaCommon.Enums;
using System;

namespace LunaClient.Systems.Network
{
    public class NetworkSystem : Base.System
    {
        #region Disconnect message

        public static bool DisplayDisconnectMessage { get; set; }

        #endregion

        public override string SystemName { get; } = nameof(NetworkSystem);

        private static bool _enabled = true;

        /// <summary>
        /// This system must be ALWAYS enabled!
        /// </summary>
        public override bool Enabled
        {
            get => _enabled;
            set
            {
                base.Enabled |= value;
                _enabled |= value;
            }
        }

        #region Base overrides

        protected override void OnEnabled()
        {
            base.OnEnabled();

            SetupRoutine(new RoutineDefinition(100, RoutineExecution.Update, NetworkUpdate));
            SetupRoutine(new RoutineDefinition(1000, RoutineExecution.Update, ShowDisconnectMessage));
        }

        #endregion

        #region Update method

        private static void NetworkUpdate()
        {
            switch (MainSystem.NetworkState)
            {
                case ClientState.DisconnectRequested:
                case ClientState.Disconnected:
                case ClientState.Connecting:
                    return;
                case ClientState.Connected:
                    SystemsContainer.Get<HandshakeSystem>().Enabled = true;
                    SystemsContainer.Get<MainSystem>().Status = "Connected";
                    MainSystem.NetworkState = ClientState.Handshaking;
                    NetworkSimpleMessageSender.SendHandshakeRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.Handshaking:
                    SystemsContainer.Get<MainSystem>().Status = "Waiting for handshake challenge";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.Connected;
                    break;
                case ClientState.HandshakeChallengeReceived:
                    SystemsContainer.Get<MainSystem>().Status = "Challenge received, authenticating";
                    MainSystem.NetworkState = ClientState.Authenticating;
                    SystemsContainer.Get<HandshakeSystem>().SendHandshakeChallengeResponse();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.Authenticating:
                    SystemsContainer.Get<MainSystem>().Status = "Connection successful, authenticating";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.HandshakeChallengeReceived;
                    break;
                case ClientState.Authenticated:
                    SystemsContainer.Get<MainSystem>().Status = "Handshaking successful";

                    SystemsContainer.Get<KerbalSystem>().Enabled = true;
                    SystemsContainer.Get<VesselProtoSystem>().Enabled = true;
                    SystemsContainer.Get<VesselSyncSystem>().Enabled = true;
                    SystemsContainer.Get<VesselSyncSystem>().MessageSender.SendVesselsSyncMsg();

                    MainSystem.NetworkState = ClientState.SyncingKerbals;
                    NetworkSimpleMessageSender.SendKerbalsRequest();

                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingKerbals:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing kerbals";
                    if (ConnectionIsStuck(5000))
                        MainSystem.NetworkState = ClientState.Authenticated;
                    break;
                case ClientState.KerbalsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Kerbals synced";
                    SystemsContainer.Get<SettingsSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingSettings;
                    NetworkSimpleMessageSender.SendSettingsRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingSettings:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing settings";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.KerbalsSynced;
                    break;
                case ClientState.SettingsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Settings synced";
                    if (SettingsSystem.ValidateSettings())
                    {
                        SystemsContainer.Get<WarpSystem>().Enabled = true;
                        MainSystem.NetworkState = ClientState.SyncingWarpsubspaces;
                        NetworkSimpleMessageSender.SendWarpSubspacesRequest();
                        _lastStateTime = DateTime.Now;
                    }
                    break;
                case ClientState.SyncingWarpsubspaces:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing warp subspaces";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.SettingsSynced;
                    break;
                case ClientState.WarpsubspacesSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Warp subspaces synced";
                    SystemsContainer.Get<PlayerColorSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingColors;
                    NetworkSimpleMessageSender.SendColorsRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingColors:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing player colors";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.WarpsubspacesSynced;
                    break;
                case ClientState.ColorsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Player colors synced";
                    SystemsContainer.Get<FlagSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingFlags;
                    NetworkSimpleMessageSender.SendFlagsRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingFlags:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing flags";
                    if (ConnectionIsStuck(5000))
                        MainSystem.NetworkState = ClientState.ColorsSynced;
                    break;
                case ClientState.FlagsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Flags synced";
                    SystemsContainer.Get<StatusSystem>().Enabled = true;
                    SystemsContainer.Get<PlayerConnectionSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingPlayers;
                    NetworkSimpleMessageSender.SendPlayersRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingPlayers:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing players";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.FlagsSynced;
                    break;
                case ClientState.PlayersSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Players synced";
                    SystemsContainer.Get<ScenarioSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingScenarios;
                    NetworkSimpleMessageSender.SendScenariosRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingScenarios:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing scenarios";
                    if (ConnectionIsStuck(10000))
                        MainSystem.NetworkState = ClientState.PlayersSynced;
                    break;
                case ClientState.ScenariosSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Scenarios synced";
                    SystemsContainer.Get<CraftLibrarySystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingCraftlibrary;
                    NetworkSimpleMessageSender.SendCraftLibraryRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingCraftlibrary:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing craft library";
                    if (ConnectionIsStuck(10000))
                        MainSystem.NetworkState = ClientState.ScenariosSynced;
                    break;
                case ClientState.CraftlibrarySynced:
                    SystemsContainer.Get<MainSystem>().Status = "Craft library synced";
                    SystemsContainer.Get<ChatSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingChat;
                    NetworkSimpleMessageSender.SendChatRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingChat:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing chat";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.CraftlibrarySynced;
                    break;
                case ClientState.ChatSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Chat synced";
                    SystemsContainer.Get<LockSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingLocks;
                    SystemsContainer.Get<LockSystem>().MessageSender.SendLocksRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingLocks:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing locks";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.ChatSynced;
                    break;
                case ClientState.LocksSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Locks synced";
                    SystemsContainer.Get<AdminSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingAdmins;
                    NetworkSimpleMessageSender.SendAdminsRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingAdmins:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing admins";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.LocksSynced;
                    break;
                case ClientState.AdminsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Admins synced";
                    SystemsContainer.Get<GroupSystem>().Enabled = true;
                    MainSystem.NetworkState = ClientState.SyncingGroups;
                    NetworkSimpleMessageSender.SendGroupListRequest();
                    _lastStateTime = DateTime.Now;
                    break;
                case ClientState.SyncingGroups:
                    SystemsContainer.Get<MainSystem>().Status = "Syncing groups";
                    if (ConnectionIsStuck())
                        MainSystem.NetworkState = ClientState.AdminsSynced;
                    break;
                case ClientState.GroupsSynced:
                    SystemsContainer.Get<MainSystem>().Status = "Groups synced";
                    SystemsContainer.Get<MainSystem>().StartGame = true;
                    MainSystem.NetworkState = ClientState.Starting;
                    break;
                case ClientState.Starting:
                    SystemsContainer.Get<MainSystem>().Status = "Running";
                    CommonUtil.Reserve20Mb();
                    LunaLog.Log("[LMP]: All systems up and running. Поехали!");
                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    {
                        SystemsContainer.Get<TimeSyncerSystem>().Enabled = true;
                        SystemsContainer.Get<MotdSystem>().Enabled = true;
                        SystemsContainer.Get<VesselLockSystem>().Enabled = true;
                        SystemsContainer.Get<VesselPositionSystem>().Enabled = true;
                        SystemsContainer.Get<VesselFlightStateSystem>().Enabled = true;
                        SystemsContainer.Get<VesselRemoveSystem>().Enabled = true;
                        SystemsContainer.Get<VesselImmortalSystem>().Enabled = true;
                        SystemsContainer.Get<VesselDockSystem>().Enabled = true;
                        SystemsContainer.Get<VesselSwitcherSystem>().Enabled = true;
                        SystemsContainer.Get<VesselRangeSystem>().Enabled = true;
                        SystemsContainer.Get<VesselPrecalcSystem>().Enabled = true;
                        SystemsContainer.Get<VesselStateSystem>().Enabled = true;
                        SystemsContainer.Get<VesselResourceSystem>().Enabled = true;
                        SystemsContainer.Get<VesselUpdateSystem>().Enabled = true;
                        SystemsContainer.Get<GameSceneSystem>().Enabled = true;
                        SystemsContainer.Get<AsteroidSystem>().Enabled = true;
                        SystemsContainer.Get<ToolbarSystem>().Enabled = true;
                        SystemsContainer.Get<FacilitySystem>().Enabled = true;
                        SystemsContainer.Get<PlayerColorSystem>().MessageSender.SendPlayerColorToServer();
                        SystemsContainer.Get<StatusSystem>().MessageSender.SendOwnStatus();
                        NetworkSimpleMessageSender.SendMotdRequest();

                        MainSystem.NetworkState = ClientState.Running;
                    }
                    break;
                case ClientState.Running:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (SystemsContainer.Get<MotdSystem>().DisplayMotd && HighLogic.LoadedScene != GameScenes.LOADING)
            {
                SystemsContainer.Get<MotdSystem>().DisplayMotd = false;
                SystemsContainer.Get<ScenarioSystem>().UpgradeTheAstronautComplexSoTheGameDoesntBugOut();
                ScreenMessages.PostScreenMessage(SystemsContainer.Get<MotdSystem>().ServerMotd, 10f, ScreenMessageStyle.UPPER_CENTER);
                //Control locks will bug out the space centre sceen, so remove them before starting.
                NetworkMain.DeleteAllTheControlLocksSoTheSpaceCentreBugGoesAway();
            }
        }

        #endregion

        #region Private methods


        private static DateTime _lastStateTime = DateTime.MinValue;
        private static bool ConnectionIsStuck(int maxIdleMiliseconds = 2000)
        {
            if ((DateTime.Now - _lastStateTime).TotalMilliseconds > maxIdleMiliseconds)
            {
                LunaLog.LogWarning($"Connection got stuck while connecting after waiting {maxIdleMiliseconds} ms, resending last request!");
                return true;
            }

            return false;
        }

        private static void ShowDisconnectMessage()
        {
            if (HighLogic.LoadedScene < GameScenes.SPACECENTER)
                DisplayDisconnectMessage = false;

            if (DisplayDisconnectMessage)
            {
                ScreenMessages.PostScreenMessage("You have been disconnected!", 2f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        #endregion
    }
}