using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoMine
{
    [BepInPlugin("aedenthorn.AutoMine", "AutoMine", "0.8.3")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> intervalCheck;
        private static ConfigEntry<bool> includeGrabbables;
        private static ConfigEntry<string> checkToggleKey;
        private static ConfigEntry<string> checkKey;
        private static ConfigEntry<string> specifyKey;
        private static ConfigEntry<float> checkInterval;
        private static ConfigEntry<float> maxRange;
        private static ConfigEntry<string> allowList;
        private static ConfigEntry<string> disallowList;

        private static string specifiedID;

        private static float elapsed;

        private static InputAction action;
        private static InputAction actionM;
        private static InputAction actionS;
        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            intervalCheck = Config.Bind<bool>("Options", "IntervalCheck", true, "Enable interval checking");
            checkInterval = Config.Bind<float>("Options", "CheckInterval", 3f, "Seconds betweeen check");
            includeGrabbables = Config.Bind<bool>("Options", "IncludeGrabbables", false, "Enable grabbing grabbables");
            maxRange = Config.Bind<float>("Options", "MaxRange", 10f, "Range to check in meters");
            checkToggleKey = Config.Bind<string>("Options", "IntervalCheckKey", "<Keyboard>/v", "Key to enable / disable interval checking");
            checkKey = Config.Bind<string>("Options", "CheckKey", "<Keyboard>/c", "Key to check manually");
            specifyKey = Config.Bind<string>("Options", "SpecifyKey", "<Keyboard>/b", "Key to specify a single type (while aiming at a node of that type).");
            allowList = Config.Bind<string>("Options", "AllowList", "", "Comma-separated list of item IDs to allow mining (overrides DisallowList).");
            disallowList = Config.Bind<string>("Options", "DisallowList", "", "Comma-separated list of item IDs to disallow mining (if AllowList is empty)");

            action = new InputAction(binding: checkToggleKey.Value);
            action.Enable();
            actionM = new InputAction(binding: checkKey.Value);
            actionM.Enable();
            actionS = new InputAction(binding: specifyKey.Value);
            actionS.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        public static class PlayerInputDispatcher_Update_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                if (action.WasPressedThisFrame())
                {
                    intervalCheck.Value = !intervalCheck.Value;
                    if (Managers.GetManager<PopupsHandler>() != null)
                        AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new CustomPopupData(null, $"AutoMine {(intervalCheck.Value ? "Enabled" : "Disabled")}", 2, true, null));
                    if (intervalCheck.Value)
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
                if (actionS.WasPressedThisFrame())
                {
                    Dbgl($"Pressed specify key");
                    PlayerAimController c = FindAnyObjectByType<PlayerAimController>();
                    List<Actionnable> aimedActionnables = c.GetAimedActionnables();
                    if (aimedActionnables != null)
                    {
                        foreach (Actionnable actionnable in aimedActionnables)
                        {
                            if (actionnable is ActionMinable)
                            {
                                var wo = actionnable.GetComponent<WorldObjectAssociated>();
                                if (wo is null)
                                    continue;
                                specifiedID = wo.GetWorldObject().GetGroup().GetId();
                                AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(null, $"AutoMine Target Set To {specifiedID}", 2));
                                Dbgl($"Found minable {specifiedID}");
                                return;
                            }
                        }
                    }
                    Dbgl($"No minable, nulling specified id");
                    specifiedID = null;
                    AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(null, $"AutoMine Target Reset", 2));
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
        }

        private static void CheckForNearbyMinables()
        {
            if (!Managers.GetManager<PlayersManager>() || Managers.GetManager<WindowsHandler>().GetHasUiOpen())
                return;
            List<string> allow = allowList.Value.Split(',').ToList();
            List<string> disallow = disallowList.Value.Split(',').ToList();
            var player = Managers.GetManager<PlayersManager>().GetActivePlayerController();
            
            if (player?.GetPlayerBackpack()?.GetInventory()?.IsFull() != false)
                return;
            InformationsDisplayer informationsDisplayer = Managers.GetManager<DisplayersHandler>().GetInformationsDisplayer();
            int count = 0;
            int count2 = 0;
            var l = new List<Actionnable>();
            l.AddRange(FindObjectsByType<ActionMinable>(FindObjectsSortMode.None));
            if (includeGrabbables.Value)
            {
                l.AddRange(FindObjectsByType<ActionGrabable>(FindObjectsSortMode.None));
            }
            foreach (var m in l)
            {
                if (player.GetPlayerBackpack().GetInventory().IsFull())
                    break;
                if(m is ActionGrabable g && !g.GetCanGrab())
                    { continue; }
                if (m.GetComponentInParent<MachineAutoCrafter>() != null)
                    continue;

                Vector3 pos = player.transform.position;
                var dist = Vector3.Distance(m.transform.position, pos);
                if (dist > maxRange.Value)
                    continue;

                var worldObjectAssociated = m.GetComponent<WorldObjectAssociated>();
                if (worldObjectAssociated == null)
                    continue;

                WorldObject worldObject = worldObjectAssociated.GetWorldObject();
                if (worldObject == null)
                    continue;
                string id = worldObject.GetGroup().GetId();

                if (specifiedID != null && id != specifiedID)
                {
                    continue;
                }

                if (allowList.Value.Length > 0)
                {
                    if (!allow.Contains(id))
                        continue;
                }
                else if (disallowList.Value.Length > 0)
                {
                    if (disallow.Contains(id))
                        continue;
                }
                var name = Readable.GetGroupName(worldObject.GetGroup());
                if (m is ActionGrabable g2)
                {
                    AccessTools.Method(typeof(ActionGrabable), "Grab").Invoke(g2, new object[0]);
                    informationsDisplayer.AddInformation(2f, name, DataConfig.UiInformationsType.InInventory, worldObject.GetGroup().GetImage());
                    count2++;
                    Dbgl($"grabbed {name}");
                }
                else if (player.GetPlayerBackpack().GetInventory().AddItem(worldObject))
                {
                    Destroy(m.gameObject);
                    informationsDisplayer.AddInformation(2f, name, DataConfig.UiInformationsType.InInventory, worldObject.GetGroup().GetImage());
                    worldObject.SetDontSaveMe(false);
                    Managers.GetManager<DisplayersHandler>().GetItemWorldDisplayer()?.Hide();
                    count++;
                    Dbgl($"mined {name}");
                }
                else
                {
                    break;
                }
            }
            if (count > 0 || count2 > 0)
            {
                player.GetPlayerAudio().PlayGrab();
                Dbgl($"Mined {count} items, grabbed {count2} items");
            }
        }

    }
}
