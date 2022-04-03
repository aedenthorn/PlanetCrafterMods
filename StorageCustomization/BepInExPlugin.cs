using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace StorageCustomization
{
    [BepInPlugin("aedenthorn.StorageCustomization", "Storage Customization", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> chestStorageSize;
        public static ConfigEntry<int> lockerStorageSize;
        public static ConfigEntry<int> goldenChestStorageSize;
        public static ConfigEntry<int> backpack1Adds;
        public static ConfigEntry<int> backpack2Adds;
        public static ConfigEntry<int> backpack3Adds;
        public static ConfigEntry<int> backpack4Adds;
        public static ConfigEntry<int> backpack5Adds;
        private static IEnumerator coroutine;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            chestStorageSize = Config.Bind<int>("Options", "ChestStorageSize", 15, "Chest storage size");
            lockerStorageSize = Config.Bind<int>("Options", "LockerStorageSize", 35, "Locker storage size");
            goldenChestStorageSize = Config.Bind<int>("Options", "GoldenChestStorageSize", 30, "Golden chest storage size");
            backpack1Adds = Config.Bind<int>("Options", "Backpack1Adds", 4, "Storage added by Backpack 1");
            backpack2Adds = Config.Bind<int>("Options", "Backpack2Adds", 8, "Storage added by Backpack 2");
            backpack3Adds = Config.Bind<int>("Options", "Backpack3Adds", 12, "Storage added by Backpack 3");
            backpack4Adds = Config.Bind<int>("Options", "Backpack4Adds", 16, "Storage added by Backpack 4");
            backpack5Adds = Config.Bind<int>("Options", "Backpack5Adds", 23, "Storage added by Backpack 5");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }
        [HarmonyPatch(typeof(PlayerEquipment), "UpdateAfterEquipmentChange")]
        static class PlayerEquipment_UpdateAfterEquipmentChange_Patch
        {
            static void Prefix(PlayerEquipment __instance, WorldObject _worldObject, bool _hasBeenAdded, bool _isFirstInit)
            {
                if (!modEnabled.Value)
                    return;
                GroupItem groupItem = (GroupItem)_worldObject.GetGroup();
                if (groupItem.GetEquipableType() == DataConfig.EquipableType.BackpackIncrease && !_isFirstInit)
                {
                    if (_hasBeenAdded)
                    {
                        switch (_worldObject.GetGroup().GetAssociatedGameObject().name)
                        {
                            case "Backpack1":
                                ((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value = backpack1Adds.Value;
                                break;
                            case "Backpack2":
                                ((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value = backpack2Adds.Value;
                                break;
                            case "Backpack3":
                                ((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value = backpack3Adds.Value;
                                break;
                            case "Backpack4":
                                ((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value = backpack4Adds.Value;
                                break;
                            case "Backpack5":
                                ((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value = backpack5Adds.Value;
                                break;
                        }
                        Dbgl($"Added {_worldObject.GetGroup().GetAssociatedGameObject().name}, value {((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value}");
                    }
                    if (!_hasBeenAdded)
                    {
                        Dbgl($"removed {_worldObject.GetGroup().GetAssociatedGameObject().name}, value {((GroupItem)AccessTools.FieldRefAccess<WorldObject, Group>(_worldObject, "group")).value}");
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ActionOpenable), nameof(ActionOpenable.OnAction))]
        static class ActionOpenable_OnAction_Patch
        {
            static void Prefix(ActionOpenable __instance)
            {
                if (!modEnabled.Value || !__instance.name.Contains("Container"))
                    return;
                InventoryAssociated componentOnGameObjectOrInParent = Components.GetComponentOnGameObjectOrInParent<InventoryAssociated>(__instance.gameObject);
                Inventory i = componentOnGameObjectOrInParent.GetInventory();
                if (__instance.name.StartsWith("Container1"))
                {
                    i.SetSize(chestStorageSize.Value);
                }
                else if (__instance.name.StartsWith("Container2"))
                {
                    i.SetSize(lockerStorageSize.Value);
                }
                else if (__instance.name.StartsWith("GoldenContainer"))
                {
                    i.SetSize(goldenChestStorageSize.Value);
                }
                else
                    return;
                Dbgl($"Set storage size of {__instance.name} to {i.GetSize()}");
                componentOnGameObjectOrInParent.SetInventory(i);
            }
        }
        [HarmonyPatch(typeof(InventoryDisplayer), nameof(InventoryDisplayer.TrueRefreshContent))]
        static class InventoryDisplayer_TrueRefreshContent_Patch
        {
            static void Postfix(InventoryDisplayer __instance, GridLayoutGroup ___grid)
            {
                if (!modEnabled.Value)
                    return;

                coroutine = WaitAndFixDisplay(__instance, ___grid);
                __instance.StartCoroutine(coroutine);
            }

            private static IEnumerator WaitAndFixDisplay(InventoryDisplayer displayer, GridLayoutGroup grid)
            {
                yield return new WaitForEndOfFrame();
                int childs = grid.transform.childCount;
                int height = (int)Math.Ceiling(childs / 5f);
                if (height > 7 && grid.transform.parent.name != "Mask")
                {
                    var size = new Vector2(grid.cellSize.x * 5 + grid.spacing.x * 4, grid.cellSize.y * height + grid.spacing.y * (height - 1));
                    var size2 = new Vector2(grid.cellSize.x * 5 + grid.spacing.x * 4, grid.cellSize.y * 7 + grid.spacing.y * 6) * 4 / 3;
                    Dbgl($"Adding scroll view: {size}, {size2}");

                    RectTransform rtg = grid.GetComponent<RectTransform>();

                    GameObject scrollObject = new GameObject() { name = "ScrollView" };
                    scrollObject.transform.SetParent(grid.transform.parent);
                    RectTransform rts = scrollObject.AddComponent<RectTransform>();
                    rts.sizeDelta = size2;
                    rts.anchorMax = new Vector2(1, 1);
                    rts.anchorMin = new Vector2(0, 0);
                    rts.anchoredPosition = new Vector2(0, -50);

                    GameObject mask = new GameObject { name = "Mask" };
                    mask.transform.SetParent(scrollObject.transform);
                    RectTransform rtm = mask.AddComponent<RectTransform>();
                    rtm.anchoredPosition = Vector2.zero;
                    rtm.sizeDelta = size2;

                    //rtg.sizeDelta = size;

                    //Dbgl($"Preferred height: {___grid.preferredHeight}");
                    grid.transform.SetParent(mask.transform);
                    rtg.anchorMax = new Vector2(0.5f, 0.5f);
                    rtg.anchorMin = new Vector2(0.5f, 0.5f);
                    rtg.sizeDelta = size;

                    Texture2D tex = new Texture2D((int)Mathf.Ceil(rtm.rect.width), (int)Mathf.Ceil(rtm.rect.height));
                    Image image = mask.AddComponent<Image>();
                    image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                    Mask m = mask.AddComponent<Mask>();
                    m.showMaskGraphic = false;

                    ScrollRect sr = scrollObject.AddComponent<ScrollRect>();
                    sr.movementType = ScrollRect.MovementType.Clamped;
                    sr.horizontal = false;
                    sr.viewport = mask.GetComponent<RectTransform>();
                    sr.content = grid.transform.GetComponent<RectTransform>();
                    sr.verticalNormalizedPosition = 1;
                    sr.scrollSensitivity = 10;

                    Dbgl("Added scroll view");

                    displayer.SetIconsPositionRelativeToGrid();
                }
                else if (height <= 7 && grid.transform.parent.name == "Mask")
                {
                    RectTransform rtg = grid.GetComponent<RectTransform>();
                    grid.transform.SetParent(grid.transform.parent.parent.parent);
                    rtg.anchorMax = new Vector2(1f, 1f);
                    rtg.anchorMin = new Vector2(0f, 0f);
                    rtg.offsetMax = new Vector2(0,0);
                    rtg.offsetMin = new Vector2(0,62);
                    Destroy(grid.transform.parent.Find("ScrollView").gameObject);
                    displayer.SetIconsPositionRelativeToGrid();
                }

                foreach (EventTrigger t in grid.transform.GetComponentsInChildren<EventTrigger>(true))
                {
                    t.gameObject.AddComponent<MyEventTrigger>().triggers.AddRange(t.triggers);
                    DestroyImmediate(t);
                }
                yield break;
            }
        }
    }
}
