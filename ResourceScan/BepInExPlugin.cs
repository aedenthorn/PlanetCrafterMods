using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ResourceScan
{
    [BepInPlugin("aedenthorn.ResourceScan", "Resource Scan", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> scanEnabled;
        public static ConfigEntry<Key> toggleScanKey;
        public static ConfigEntry<Key> toggleResourceKey;
        public static ConfigEntry<float> checkInterval;
        public static ConfigEntry<float> minRange;
        public static ConfigEntry<float> maxRange;
        public static ConfigEntry<string> allowList;
        public static ConfigEntry<string> disallowList;


        public static float elapsed;

        public static List<GameObject> hoveringList = new List<GameObject>();

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
            toggleScanKey = Config.Bind<Key>("Options", "ToggleScanningKey", Key.PageUp, "Key used to toggle the scan");
            toggleResourceKey = Config.Bind<Key>("Options", "ToggleSpecificResourceKey", Key.PageDown, "Key used to toggle a specific resource scan");
            scanEnabled = Config.Bind<bool>("Options", "ScanEnabled", true, "Enable this mod");
            checkInterval = Config.Bind<float>("Options", "CheckInterval", 3f, "Seconds betweeen check");
            minRange = Config.Bind<float>("Options", "MinRange", 20f, "Min range to check in meters");
            maxRange = Config.Bind<float>("Options", "MaxRange", 100f, "Max range to check in meters");
            allowList = Config.Bind<string>("Options", "AllowList", "", "Comma-separated list of item IDs to allow scanning (overrides DisallowList).");
            disallowList = Config.Bind<string>("Options", "DisallowList", "", "Comma-separated list of item IDs to disallow scanning (if AllowList is empty)");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }
        public static void ClearHovering()
        {
            foreach (var go in hoveringList)
            {
                if (go != null)
                    Destroy(go);
            }
            hoveringList.Clear();
        }

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        public static class PlayerInputDispatcher_Update_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                {
                    if(hoveringList.Count > 0)
                    {
                        ClearHovering();
                    }
                    return;
                }
                if (Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() == true)
                    return;
                if (toggleScanKey.Value != Key.None && Keyboard.current != null && Keyboard.current[toggleScanKey.Value].wasPressedThisFrame)
                {
                    scanEnabled.Value = !scanEnabled.Value;
                    Dbgl($"Scan enabled: {scanEnabled.Value}");
                    elapsed = checkInterval.Value;
                }
                else if (toggleResourceKey.Value != Key.None && Keyboard.current != null && Keyboard.current[toggleResourceKey.Value].wasPressedThisFrame)
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
                                var id = wo.GetWorldObject().GetGroup().GetId();
                                var allowed = allowList.Value.Split(',').ToList();
                                if (allowed.Contains(id))
                                {
                                    allowed.Remove(id);
                                    Dbgl($"removed {id} from list");
                                }
                                else
                                {
                                    allowed.Add(id);
                                    Dbgl($"added {id} to list");
                                }
                                allowList.Value = string.Join(",", allowed);
                                elapsed = checkInterval.Value;
                                break;
                            }
                        }
                    }
                }
                if (!scanEnabled.Value)
                {
                    if (hoveringList.Count > 0)
                    {
                        ClearHovering();
                    }
                    return;
                }
                elapsed += Time.deltaTime;
                if (elapsed >= checkInterval.Value)
                {
                    elapsed = 0;
                    CheckForNearbyMinables();
                }
            }
        }

        public static void CheckForNearbyMinables()
        {
            if (!Managers.GetManager<PlayersManager>() || Managers.GetManager<WindowsHandler>().GetHasUiOpen())
                return;
            List<string> allow = allowList.Value.Split(',').ToList();
            List<string> disallow = disallowList.Value.Split(',').ToList();
            var player = Managers.GetManager<PlayersManager>().GetActivePlayerController();
            var minables = FindObjectsByType<ActionMinable>(FindObjectsSortMode.None);
            ClearHovering();
            ItemWorldDislpayer template = Managers.GetManager<DisplayersHandler>().GetItemWorldDisplayer();
            for (int i = minables.Length - 1; i >= 0; i--)
            {
                var m = minables[i];
                if (m.GetComponentInParent<MachineAutoCrafter>() != null)
                    continue;

                Vector3 pos = player.transform.position;
                var dist = Vector3.Distance(m.transform.position, pos);
                if (dist > maxRange.Value || dist < minRange.Value)
                    continue; 

                var worldObjectAssociated = m.GetComponent<WorldObjectAssociated>();
                if (worldObjectAssociated == null)
                    continue;

                WorldObject worldObject = worldObjectAssociated.GetWorldObject();

                string id = worldObject.GetGroup().GetId();

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
                WorldObjectAssociated component = m.GetComponent<WorldObjectAssociated>();

                string groupName = Readable.GetGroupName(component.GetWorldObject().GetGroup());

                GameObject go = Instantiate(template.gameObject, template.transform.parent);
                go.GetComponentInChildren<ItemWorldDislpayer>().ShowTo(groupName, m.transform.position, Managers.GetManager<PlayersManager>().GetActivePlayerController().gameObject);
                go.transform.position += go.transform.right;
                go.transform.position += new Vector3(0,-1,0);
                hoveringList.Add(go);
            }
        }
    }
}
