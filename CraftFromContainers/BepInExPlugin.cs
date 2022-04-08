using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> toggleKey;
        private static ConfigEntry<float> range;

        private InputAction action;

        private static bool skip;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "home", "Key to toggle pulling");
            range = Config.Bind<float>("Options", "Range", 20f, "Pull range (m)");

            action = new InputAction(binding: $"<Keyboard>/{toggleKey.Value}");
            action.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private void Update()
        {
            if (action.WasPressedThisFrame())
            {
                modEnabled.Value = !modEnabled.Value;
                Dbgl($"Mod enabled: {modEnabled.Value}");
                if(Managers.GetManager<PopupsHandler>() != null)
                    AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(null, $"Craft From Containers: {modEnabled.Value}", 2));
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItems))]
        private static class Inventory_RemoveItems_Patch
        {
            static void Prefix(Inventory __instance, List<Group> _groups, bool _destroyWorldObjects, bool _displayInformation)
            {
                if (!modEnabled.Value || __instance != Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory())
                    return;

                skip = true;
                List<bool> hasStatus = __instance.ItemsContainsStatus(_groups);
                if (!hasStatus.Contains(false))
                    return;
                skip = false;

                Dbgl($"Trying to remove missing items from player inventory:");
                for (int j = 0; j < hasStatus.Count; j++)
                {
                    if (!hasStatus[j])
                    {
                        Dbgl($"{_groups[j].GetId()}");
                    }
                }
                InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
                Vector2 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

                Dbgl($"got {ial.Length} inventories");

                for (int i = 0; i < ial.Length; i++)
                {
                    var dist = Vector2.Distance(ial[i].transform.position, pos);
                    if (ial[i].GetInventory() == Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() || dist > range.Value)
                        continue;
                    Dbgl($"checking close inventory {ial[i].name}: {ial[i].transform.position}, {pos}: {dist}m");
                    skip = true;
                    List<bool> hasItems = ial[i].GetInventory().ItemsContainsStatus(_groups);
                    skip = false;
                    List<Group> thisGroups = new List<Group>();
                    for (int j = 0; j < hasStatus.Count; j++)
                    {
                        if (!hasStatus[j] && hasItems[j])
                        {
                            Dbgl($"Removing item {_groups[j].GetId()} from {ial[i].name}");
                            hasStatus[j] = true;
                            thisGroups.Add(_groups[j]);
                            _groups.RemoveAt(j);
                        }
                    }
                    ial[i].GetInventory().RemoveItems(thisGroups, _destroyWorldObjects, _displayInformation);
                    if (!hasStatus.Contains(false))
                    {
                        Dbgl($"removed all missing items");
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.ItemsContainsStatus))]
        private static class Inventory_ContainsItems_Patch
        {
            static void Postfix(Inventory __instance, List<bool> __result, List<Group> _groups)
            {
                if (!modEnabled.Value || skip || __instance != Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() || !__result.Contains(false))
                    return;
                Dbgl($"checking status for missing items:");

                for (int j = 0; j < __result.Count; j++)
                {
                    if (!__result[j])
                    {
                        Dbgl($"{_groups[j].GetId()}");
                    }
                }


                InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
                Vector2 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

                Dbgl($"got {ial.Length} inventories");

                for (int i = 0; i < ial.Length; i++)
                {
                    var dist = Vector2.Distance(ial[i].transform.position, pos);
                    if (ial[i].GetInventory() == Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() || dist > range.Value)
                        continue;
                    Dbgl($"checking close inventory {ial[i].name}: {ial[i].transform.position}, {pos}: {dist}m");
                    skip = true;
                    List<bool> hasItems = ial[i].GetInventory().ItemsContainsStatus(_groups);
                    skip = false;
                    for (int j = 0; j < __result.Count; j++)
                    {
                        if (!__result[j] && hasItems[j])
                        {
                            Dbgl($"Found item {_groups[j].GetId()} in {ial[i].name}");
                            __result[j] = true;
                        }
                    }
                    if (!__result.Contains(false))
                    {
                        Dbgl($"found all items");
                        return;
                    }

                }
            }
        }
    }
}
