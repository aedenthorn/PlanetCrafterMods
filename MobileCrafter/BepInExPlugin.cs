using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobileCrafter
{
    [BepInPlugin("MobileCrafter", "Mobile Crafter", "0.3.2")]
    public class mobileCrafter : BaseUnityPlugin
    {
        private static InputAction actionOpen;
        private static mobileCrafter context;
        public static ConfigEntry<string> mobileCrafterKey;
        public static ConfigEntry<DataConfig.CraftableIn> mobileCrafterType;
        public static ConfigEntry<DataConfig.CraftableIn> craftAtStationType;
        public static ConfigEntry<string> titleText;
        public static ConfigEntry<string> microchipText;
        
        public static ActionCrafter craftAction = new ActionCrafter();
        public static DataConfig.EquipableType mobileC = (DataConfig.EquipableType)420;
        public static bool MobileCrafterCanCraft;
        public static bool hasBeenAdded;

        public void Awake()
        {
            context = this;
            mobileCrafterKey = Config.Bind<string>("Options", "MobileCrafterKey", "<Keyboard>/p", "Key binding to open the crafter");
            mobileCrafterType = Config.Bind<DataConfig.CraftableIn>("Options", "MobileCrafterType", DataConfig.CraftableIn.CraftStationT1, "Which crafter to base the mobile crafter on");
            craftAtStationType = Config.Bind<DataConfig.CraftableIn>("Options", "CraftAtStationType", DataConfig.CraftableIn.CraftStationT3, "Which crafter to craft the mobile crafter in");
            titleText = Config.Bind<string>("Text", "TitleText", "Mobile Crafter", "UI Title text");
            microchipText = Config.Bind<string>("Text", "MicrochipText", "Microchip - Mobile Crafter", "Microchip text");

            actionOpen = new InputAction(binding: mobileCrafterKey.Value);
            actionOpen.Enable();

            craftAction.craftTime = 1f;
            craftAction.craftableIdentifier = mobileCrafterType.Value;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Logger.LogInfo("Plugin Mobile Crafter loaded");
        }

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        public static class PlayerInputDispatcher_Update_Patch
        {
            public static void Postfix()
            {
                if (Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() == true)
                    return;

                if (actionOpen.WasPressedThisFrame())
                {
                    context.Logger.LogInfo("pressed crafter key");
                    if (MobileCrafterCanCraft || Managers.GetManager<GameSettingsHandler>().GetCurrentGameSettings().GetFreeCraft())
                    {
                        UiWindowCraft uiWindowCraft = (UiWindowCraft)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.Craft);
                        uiWindowCraft.SetCrafter(craftAction, true, false);
                        uiWindowCraft.ChangeTitle(titleText.Value);
                        context.Logger.LogInfo("opening crafter");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StaticDataHandler), "LoadStaticData")]
        public static class StaticDataHandler_LoadStaticData_Patch 
        {
            public static void Prefix(ref List<GroupData> ___groupsData)
            {
                if (hasBeenAdded)
                    return;

                GroupData groupData = ___groupsData.Find((GroupData infgen) => infgen.id == "CraftStation1");
                
                GroupDataItem groupDataItem = ScriptableObject.CreateInstance(typeof(GroupDataItem)) as GroupDataItem;
                groupDataItem.name = "MobileCrafter";
                groupDataItem.id = microchipText.Value;
                groupDataItem.associatedGameObject = GetGroupDataItemById(___groupsData, "MultiToolMineSpeed4").associatedGameObject;
                groupDataItem.icon = groupData.icon;
                groupDataItem.unlockingWorldUnit = DataConfig.WorldUnitType.Terraformation;
                groupDataItem.unlockingValue = 0f;
                groupDataItem.unlockInPlanets = GetGroupDataItemById(___groupsData, "MultiToolMineSpeed4").unlockInPlanets;
                groupDataItem.planetUsageType = DataConfig.GroupPlanetUsageType.CanBeUsedOnAllPlanets;
                groupDataItem.terraformStageUnlock = null;
                groupDataItem.inventorySize = 0;
                groupDataItem.value = 50;
                groupDataItem.craftableInList = new List<DataConfig.CraftableIn>
                {
                    craftAtStationType.Value
                };
                groupDataItem.equipableType = mobileC;
                groupDataItem.usableType = 0;
                groupDataItem.itemCategory = DataConfig.ItemCategory.Equipment;
                groupDataItem.growableGroup = null;
                groupDataItem.unitMultiplierOxygen = 0f;
                groupDataItem.unitMultiplierPressure = 0f;
                groupDataItem.unitMultiplierHeat = 0f;
                groupDataItem.unitMultiplierEnergy = 0f;
                groupDataItem.unitMultiplierPlants = 0f;
                groupDataItem.unitMultiplierInsects = 0f;
                groupDataItem.unitMultiplierAnimals = 0f;
                groupDataItem.recipeIngredients = new List<GroupDataItem>
                {
                    GetGroupDataItemById(___groupsData, "Alloy"),
                    GetGroupDataItemById(___groupsData, "Aluminium"),
                    GetGroupDataItemById(___groupsData, "Iron"),
                    GetGroupDataItemById(___groupsData, "Silicon")
                };

                ___groupsData.Add(groupDataItem);
                context.Logger.LogInfo("Added crafter to data");
                hasBeenAdded = true;

            }
            private static GroupDataItem GetGroupDataItemById(List<GroupData> groupsData, string id)
            {
                return groupsData.Find((GroupData x) => x.id == id) as GroupDataItem;
            }
        }

        [HarmonyPatch(typeof(ActionCrafter), nameof(ActionCrafter.CraftAnimation))]
        public static class ActionCrafter_CraftAnimation_Patch
        {
            public static bool Prefix(ActionCrafter __instance)
            {
                return __instance != craftAction;
            }
        }

        [HarmonyPatch(typeof(PlayerEquipment), nameof(PlayerEquipment.UpdateAfterEquipmentChange))]
        public static class PlayerEquipment_UpdateAfterEquipmentChange_Patch
        {
            public static void Prefix(WorldObject worldObject, bool hasBeenAdded)
            {
                GroupItem groupItem = (GroupItem)worldObject.GetGroup();
                if (groupItem.GetEquipableType() == (DataConfig.EquipableType)420)
                {
                    MobileCrafterCanCraft = hasBeenAdded;
                    context.Logger.LogInfo($"Can craft: {MobileCrafterCanCraft}");
                }
            }
        }
	}
}
