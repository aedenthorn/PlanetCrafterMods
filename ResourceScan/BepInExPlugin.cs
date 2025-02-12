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
    [BepInPlugin("aedenthorn.ResourceScan", "Resource Scan", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> intervalCheck;
        public static ConfigEntry<float> checkInterval;
        public static ConfigEntry<float> maxRange;
        public static ConfigEntry<string> allowList;
        public static ConfigEntry<string> disallowList;

        public static string specifiedID;

        public static float elapsed;

        public static InputAction action;
        public static InputAction actionM;
        public static InputAction actionS;
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
            intervalCheck = Config.Bind<bool>("Options", "IntervalCheck", true, "Enable interval checking");
            checkInterval = Config.Bind<float>("Options", "CheckInterval", 3f, "Seconds betweeen check");
            maxRange = Config.Bind<float>("Options", "MaxRange", 10f, "Range to check in meters");
            allowList = Config.Bind<string>("Options", "AllowList", "", "Comma-separated list of item IDs to allow mining (overrides DisallowList).");
            disallowList = Config.Bind<string>("Options", "DisallowList", "", "Comma-separated list of item IDs to disallow mining (if AllowList is empty)");

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
                elapsed += Time.deltaTime;
                if (elapsed > checkInterval.Value)
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
                if (dist > maxRange.Value)
                    continue;

                var worldObjectAssociated = m.GetComponent<WorldObjectAssociated>();
                if (worldObjectAssociated == null)
                    continue;

                WorldObject worldObject = worldObjectAssociated.GetWorldObject();

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
                WorldObjectAssociated component = m.GetComponent<WorldObjectAssociated>();
                Dbgl($"component: {template != null}");

                string groupName = Readable.GetGroupName(component.GetWorldObject().GetGroup());
                Dbgl($"group name: {groupName}");

                GameObject go = Instantiate(template.gameObject, template.transform.parent);
                go.GetComponentInChildren<ItemWorldDislpayer>().ShowTo(groupName, m.transform.position, Managers.GetManager<PlayersManager>().GetActivePlayerController().gameObject);
                go.transform.position += go.transform.right;
                go.transform.position += new Vector3(0,-1,0);
                hoveringList.Add(go);
            }
        }
    }
}
