using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoMine
{
    [BepInPlugin("aedenthorn.AutoMine", "AutoMine", "0.2.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> intervalCheck;
        private static ConfigEntry<string> checkToggleKey;
        private static ConfigEntry<string> checkKey;
        private static ConfigEntry<float> checkInterval;
        private static ConfigEntry<float> maxRange;
        private static ConfigEntry<string> allowList;
        private static ConfigEntry<string> disallowList;

        private static float elapsed;

        private InputAction action;
        private InputAction actionM;
        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            intervalCheck = Config.Bind<bool>("Options", "IntervalCheck", true, "Enable interval checking");
            checkInterval = Config.Bind<float>("Options", "CheckInterval", 3f, "Seconds betweeen check");
            maxRange = Config.Bind<float>("Options", "MaxRange", 10f, "Range to check in meters");
            checkToggleKey = Config.Bind<string>("Options", "IntervalCheckKey", "<Keyboard>/v", "Key to enable / disable interval checking");
            checkKey = Config.Bind<string>("Options", "CheckKey", "<Keyboard>/c", "Key to check manually");
            allowList = Config.Bind<string>("Options", "AllowList", "", "Comma-separated list of item IDs to allow mining (overrides DisallowList).");
            disallowList = Config.Bind<string>("Options", "DisallowList", "", "Comma-separated list of item IDs to disallow mining (if AllowList is empty)");

            action = new InputAction(binding: checkToggleKey.Value);
            action.Enable();
            actionM = new InputAction(binding: checkKey.Value);
            actionM.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        private void Update()
        {
            if (!modEnabled.Value)
                return;
            if (action.WasPressedThisFrame())
            {
                intervalCheck.Value = !intervalCheck.Value;
                if (Managers.GetManager<PopupsHandler>() != null)
                    AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(null, $"AutoMine {(intervalCheck.Value ? "Enabled" : "Disabled")}", 2));
                if(intervalCheck.Value)
                    elapsed = checkInterval.Value;
                return;
            }
            if (actionM.WasPressedThisFrame())
            {
                Dbgl($"Pressed manual check key");
                elapsed = 0;
                CheckForNearbyMinables();
                return;
            }
            if (intervalCheck.Value)
            {
                elapsed += Time.deltaTime;
                if (elapsed > checkInterval.Value)
                {
                    elapsed = 0;
                    CheckForNearbyMinables();
                    return;
                }
            }
        }

        private void CheckForNearbyMinables()
        {
            if (!Managers.GetManager<PlayersManager>() || Managers.GetManager<WindowsHandler>().GetHasUiOpen())
                return;

            List<string> allow = allowList.Value.Split(',').ToList();
            List<string> disallow = disallowList.Value.Split(',').ToList();
            var player = Managers.GetManager<PlayersManager>().GetActivePlayerController();
            InformationsDisplayer informationsDisplayer = Managers.GetManager<DisplayersHandler>().GetInformationsDisplayer();
            int count = 0;
            foreach (var m in FindObjectsOfType<ActionMinable>())
            {
                Vector3 pos = player.transform.position;
                var dist = Vector3.Distance(m.transform.position, pos);
                if (dist > maxRange.Value)
                    continue;

                var worldObjectAssociated = m.GetComponent<WorldObjectAssociated>();
                if (worldObjectAssociated == null)
                    continue;

                WorldObject worldObject = worldObjectAssociated.GetWorldObject();

                if (allowList.Value.Length > 0)
                {
                    if (!allow.Contains(worldObject.GetGroup().GetId()))
                        continue;
                }
                else if (disallowList.Value.Length > 0)
                {
                    if (disallow.Contains(worldObject.GetGroup().GetId()))
                        continue;
                }

                if (player.GetPlayerBackpack().GetInventory().AddItem(worldObject))
                {
                    Destroy(m.gameObject);
                    informationsDisplayer.AddInformation(2f, Readable.GetGroupName(worldObject.GetGroup()), DataConfig.UiInformationsType.InInventory, worldObject.GetGroup().GetImage());
                    worldObject.SetDontSaveMe(false);
                    Managers.GetManager<DisplayersHandler>().GetItemWorldDislpayer().Hide();
                    count++;
                } 
                else
                {
                    break;
                }
            }
            if(count > 0)
            {
                player.GetPlayerAudio().PlayGrab();
            }
            Dbgl($"Mined {count} items");
        }

    }
}
