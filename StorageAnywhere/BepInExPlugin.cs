using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;

namespace StorageAnywhere
{
    [BepInPlugin("aedenthorn.StorageAnywhere", "Storage Anywhere", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> ignoreSingleCell;
        private static ConfigEntry<string> toggleKey;
        private static ConfigEntry<string> leftKey;
        private static ConfigEntry<string> rightKey;
        private static ConfigEntry<float> range;

        private InputAction action;
        private InputAction actionLeft;
        private InputAction actionRight;
        private static List<InventoryAssociated> inventoryList = new List<InventoryAssociated>();
        private static int currentIndex = 0;

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
            ignoreSingleCell = Config.Bind<bool>("Options", "IgnoreSingleCell", true, "Ignore single cell inventories");
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "<Keyboard>/i", "Key to toggle inventory UI");
            leftKey = Config.Bind<string>("Options", "LeftKey", "<Keyboard>/leftArrow", "Key to switch to previous inventory");
            rightKey = Config.Bind<string>("Options", "RightKey", "<Keyboard>/rightArrow", "Key to switch to next inventory");
            range = Config.Bind<float>("Options", "Range", 20f, "Range (m)");

            action = new InputAction(binding: toggleKey.Value);
            actionLeft = new InputAction(binding: leftKey.Value);
            actionRight = new InputAction(binding: rightKey.Value);
            action.Enable();
            actionLeft.Enable();
            actionRight.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        private void Update()
        {
            if (!modEnabled.Value || Managers.GetManager<PlayersManager>() == null)
                return;
            if (action.WasPressedThisFrame())
            {
                if (Managers.GetManager<WindowsHandler>().GetHasUiOpen() && Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
                {
                    Managers.GetManager<WindowsHandler>().CloseAllWindows();

                    return;
                }
                currentIndex = 0;
                inventoryList = GetNearbyInventories();
                SetInventories();
            }
            else if (!Managers.GetManager<WindowsHandler>().GetHasUiOpen())
            {
                currentIndex = 0;
                inventoryList.Clear();
            }
            else if (actionRight.WasPressedThisFrame() && Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
            {
                inventoryList = GetNearbyInventories();
                currentIndex++;
                currentIndex %= inventoryList.Count;
                SetInventories();
            }
            else if (actionLeft.WasPressedThisFrame() && Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
            {
                inventoryList = GetNearbyInventories();
                currentIndex--;
                if(currentIndex < 0)
                    currentIndex = inventoryList.Count - 1;
                SetInventories();
            }
        }

        private void SetInventories()
        {
            Managers.GetManager<WindowsHandler>().CloseAllWindows();

            UiWindowContainer uiWindowContainer = (UiWindowContainer)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.Container);
            if (uiWindowContainer != null && inventoryList.Count > 0)
            {
                uiWindowContainer.transform.Find("Container").GetComponent<TextMeshProUGUI>().text = GetObjectName(inventoryList[currentIndex].gameObject);
                uiWindowContainer.SetInventories(Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory(), inventoryList[currentIndex].GetInventory());
            }
        }

        private string GetObjectName(GameObject go)
        {
            Transform t = go.transform;
            if (t.GetComponent<WorldObjectText>() != null)
            {
                return t.GetComponent<WorldObjectText>().GetText(); 
            }
            if (t.GetComponent<WorldObjectAssociated>() != null)
            {
                return Readable.GetGroupName(t.GetComponent<WorldObjectAssociated>().GetWorldObject().GetGroup());
            }
            while (t.parent != null && t.parent.parent != null && t.parent.name != "WorldObjectsContainer")
            {
                t = t.parent;
                if (t.GetComponent<WorldObjectText>() != null)
                {
                    return t.GetComponent<WorldObjectText>().GetText();
                }
                if (t.GetComponent<WorldObjectAssociated>() != null)
                {
                    return Readable.GetGroupName(t.GetComponent<WorldObjectAssociated>().GetWorldObject().GetGroup());
                }
            }
            return t.name.Replace("(Clone)","");
        }

        private List<InventoryAssociated> GetNearbyInventories()
        {
            List<InventoryAssociated> result = new List<InventoryAssociated>();
            InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
            Vector2 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

            Dbgl($"got {ial.Length} inventories");

            for (int i = 0; i < ial.Length; i++)
            {
                try
                {
                    ial[i].GetInventory();
                }
                catch
                {
                    continue;
                }
                var dist = Vector2.Distance(ial[i].transform.position, pos);
                if (ial[i].GetInventory() != Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() && dist <= range.Value && (!ignoreSingleCell.Value || ial[i].GetInventory().GetSize() > 1) && !ial[i].name.Contains("Golden Container"))
                    result.Add(ial[i]);
            }
            result.Sort(delegate (InventoryAssociated a, InventoryAssociated b) { return a.name.CompareTo(b.name); });
            return result;
        }

    }
}
