using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using static System.Collections.Specialized.BitVector32;

namespace BeaconToggleMenu
{
    [BepInPlugin("aedenthorn.BeaconToggleMenu", "Beacon Toggle Menu", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        private static InputAction actionOpen;
        private static InputAction actionMouse;
        private static BepInExPlugin context;
        public static ConfigEntry<string> menuKey;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<bool> allDisabled;
        public static ConfigEntry<Color> windowBackgroundColor;
        public static ConfigEntry<Vector2> windowPosition;
        public static ConfigEntry<float> windowHeight;
        public static ConfigEntry<string> windowTitleText;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<Color> fontColor;
        public static ConfigEntry<int> windowWidth;
        public static ConfigEntry<int> buttonWidth;
        public static ConfigEntry<int> rowHeight;
        public static ConfigEntry<int> betweenSpace;

        public static Vector2 scrollPosition;
        public static bool finishedChecking = false;
        public static GUIStyle style;
        public static GUIStyle styleDisabled;
        public static GUIStyle toggleStyle;
        public static Rect windowRect;
        public static bool menuOpen;

        public static Dictionary<string, Dictionary<string, bool>> beaconList = new Dictionary<string, Dictionary<string, bool>>();

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "Debug", false, "Enable debug logs");

            menuKey = Config.Bind<string>("Options", "MenuKey", "<Keyboard>/o", "Key binding to open the beacon menu");
            
            windowBackgroundColor = Config.Bind<Color>("UI", "WindowBackgroundColor", new Color(0, 0, 0, 1f), "Color of the window background");
            windowHeight = Config.Bind<float>("UI", "WindowHeight", 400, "Height of the menu window");
            windowWidth = Config.Bind<int>("UI", "WindowWidth", 200, "Width of the window");
            buttonWidth = Config.Bind<int>("UI", "ButtonWidth", 30, "Width of edit buttons");
            betweenSpace = Config.Bind<int>("UI", "BetweenSpace", 10, "Vertical space between each beacon in list");
            rowHeight = Config.Bind<int>("UI", "RowHeight", 30, "Height per row");
            windowPosition = Config.Bind<Vector2>("UI", "WindowPosition", new Vector2(-1,-1), "Position of the menu on the screen");

            fontSize = Config.Bind<int>("Text", "FontSize", 20, "Size of the text in the updates list");
            fontColor = Config.Bind<Color>("Text", "FontColor", new Color(1, 1, 0.7f, 1), "Color of the text in the menu");
            
            windowTitleText = Config.Bind<string>("Text", "WindowTitleText", "<b>Active Beacons</b>", "Window title");

            allDisabled = Config.Bind<bool>("ZAuto", "AllDisabled", false, "All beacons disabled");


            actionOpen = new InputAction(binding: menuKey.Value);
            actionOpen.Enable();

            actionMouse = new InputAction(binding: "<Mouse>/leftButton");
            actionMouse.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Logger.LogInfo("Plugin Mobile Crafter loaded");
        }

        public static void LoadBeacons()
        {
            if(!modEnabled.Value)
                return;

            var filePath = Path.Combine(AedenthornUtils.GetAssetPath(context, true), Managers.GetManager<SavedDataHandler>().GetCurrentSaveFileName() + ".json");
            beaconList = File.Exists(filePath) ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(File.ReadAllText(filePath)) : new Dictionary<string, Dictionary<string, bool>>();
            Dbgl("Loaded beacons");
        }
        public static void SaveBeacons()
        {
            if(!modEnabled.Value)
                return;
            var filePath = Path.Combine(AedenthornUtils.GetAssetPath(context, true), Managers.GetManager<SavedDataHandler>().GetCurrentSaveFileName() + ".json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(beaconList));
        }

        public void OnGUI()
        {
            if (modEnabled.Value && menuOpen)
            {

                if (windowPosition.Value.x < 0 && windowPosition.Value.y < 0)
                {
                    Camera c = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController().GetComponentInChildren<Camera>();
                    windowRect = new Rect(c.pixelRect.width / 2 - windowWidth.Value / 2, c.pixelRect.height / 2 - windowHeight.Value / 2, windowWidth.Value + 50, windowHeight.Value);
                }
                else
                {
                    windowRect = new Rect(windowPosition.Value.x, windowPosition.Value.y, windowWidth.Value + 50, windowHeight.Value);
                }

                GUI.backgroundColor = windowBackgroundColor.Value;

                windowRect = GUI.Window(424242, windowRect, new GUI.WindowFunction(WindowBuilder), windowTitleText.Value);

            }
        }
        public static string whichEditing;
        public void WindowBuilder(int id)
        {
            style = new GUIStyle
            {
                richText = true,
                fontSize = fontSize.Value,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };

            style.normal.textColor = fontColor.Value;

            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.fontSize = fontSize.Value;
            toggleStyle.normal.textColor = fontColor.Value;

            GUILayout.BeginVertical();
            GUI.DragWindow(new Rect(0, 0, windowWidth.Value + 50, 20));


            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(windowWidth.Value + 40), GUILayout.Height(windowHeight.Value - 30) });

            var blist = FindObjectsByType<MachineBeaconUpdater>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);

            string currentPlanet = Managers.GetManager<PlanetLoader>().GetCurrentPlanetData().id;

            if(!beaconList.TryGetValue(currentPlanet, out var dict))
            {
                dict = new Dictionary<string, bool>();
                beaconList[currentPlanet] = dict;
            }
            bool enabled = GUILayout.Toggle(allDisabled.Value, "Disable All", toggleStyle);
            if (allDisabled.Value != enabled)
            {
                allDisabled.Value = enabled;
            }
            GUILayout.Space(betweenSpace.Value);
            if (blist?.Any() == true)
            {
                foreach (var b in blist)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(rowHeight.Value), GUILayout.Width(windowWidth.Value + 20) });
                    if (b?.canvasPosition?.transform == null)
                        continue;
                    var pos = b.ToVector3i();
                    bool editing = whichEditing == pos;
                    var oldVal = dict.TryGetValue(pos, out var on) ? on : true;
                    string label = b.GetComponentInChildren<WorldObjectText>().textContainer.text;
                    enabled = GUILayout.Toggle(oldVal, editing ? "" : label, toggleStyle, new GUILayoutOption[]{
                        GUILayout.Width(editing ? buttonWidth.Value : windowWidth.Value - buttonWidth.Value),
                        GUILayout.Height(rowHeight.Value)
                    });
                    if (oldVal != enabled)
                    {
                        dict[pos] = enabled;
                        Dbgl("Saving beacons");
                        SaveBeacons();
                    }
                    if (editing)
                    {
                        b.GetComponentInChildren<WorldObjectText>().textContainer.text = GUILayout.TextField(b.GetComponentInChildren<WorldObjectText>().textContainer.text, new GUILayoutOption[]{
                            GUILayout.Width(windowWidth.Value - buttonWidth.Value * 2),
                            GUILayout.Height(rowHeight.Value)
                        });
                    }
                    if (GUILayout.Button(editing ? "s" : "e", new GUILayoutOption[]{
                            GUILayout.Width(buttonWidth.Value),
                            GUILayout.Height(rowHeight.Value)
                        }))
                    {
                        whichEditing = editing ? null : pos;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(betweenSpace.Value);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        [HarmonyPatch(typeof(WindowsHandler), nameof(WindowsHandler.GetHasUiOpen))]
        static class WindowsHandler_GetHasUiOpen_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (modEnabled.Value && menuOpen)
                {
                    __result = true;
                    return false;
                }
                return true;

            }
        }
        
        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        public static class PlayerInputDispatcher_Update_Patch
        {
            public static void Postfix(PlayerInputDispatcher __instance)
            {
                if (!modEnabled.Value || (bool)AccessTools.Method(typeof(PlayerInputDispatcher), "IsTyping").Invoke(__instance, new object[0]))
                    return;
                if (actionOpen.WasPressedThisFrame())
                {
                    context.Logger.LogInfo("pressed menu key");
                    menuOpen = !menuOpen;
                    CursorStateManager.SetLockCursorStatus(!menuOpen);

                }
            }
        }

        [HarmonyPatch(typeof(MachineBeaconUpdater), "LateUpdate")]
        public static class MachineBeaconUpdater_LateUpdate_Patch
        {
            public static bool Prefix(MachineBeaconUpdater __instance)
            {
                if (!modEnabled.Value || __instance.canvasPosition?.transform?.position == null)
                    return true;
                string currentPlanet = Managers.GetManager<PlanetLoader>()?.GetCurrentPlanetData()?.id;
                if (currentPlanet == null)
                {
                    return true;
                }
                if (allDisabled.Value || beaconList.TryGetValue(currentPlanet, out var dict) && dict.TryGetValue(__instance.ToVector3i(), out var enabled) && !enabled)
                {
                    __instance.canvas.SetActive(false);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SaveFilesSelector), nameof(SaveFilesSelector.SelectedSaveFile))]
        public static class SaveFilesSelector_SelectedSaveFile_Patch
        {
            public static void Postfix()
            {
                LoadBeacons();
            }
        }

    }
    public static class Extensions
    {
        public static string ToVector3i(this MachineBeaconUpdater b)
        {
            var v = b.canvasPosition.transform.position;
            return $"{(int)v.x},{(int)v.y},{(int)v.z}";
        }
    }
}
