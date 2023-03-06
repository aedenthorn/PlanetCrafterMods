using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ShowNextUnlockableGoal
{
    [BepInPlugin("aedenthorn.ShowNextUnlockableGoal", "Show Next Unlockable Goal", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> nextString;
        private static List<DataConfig.WorldUnitType> worldUnitsToCheck;
        private static UnlockingHandler unlockingHandler;

        public static void Dbgl(object str, LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            nextString = Config.Bind<string>("Options", "NextString", "({0})", "String to show for next unlock");

            Dbgl("Plugin awake");
            var harmony = new Harmony(MetadataHelper.GetMetadata(this).GUID);
            harmony.PatchAll();
            foreach (var type in typeof(WorldUnitsDisplayer).Assembly.GetTypes())
            {
                if (type.FullName.StartsWith("SpaceCraft.WorldUnitsDisplayer+<RefreshDisplay>"))
                {
                    Dbgl($"Found {type}");
                    harmony.Patch(
                       original: AccessTools.Method(type, "MoveNext"),
                       transpiler: new HarmonyMethod(typeof(BepInExPlugin), nameof(WorldUnitsDisplayer_RefreshDisplay_Transpiler))
                    );
                    break;
                }
            }
            worldUnitsToCheck = new List<DataConfig.WorldUnitType>
            {
                DataConfig.WorldUnitType.Heat,
                DataConfig.WorldUnitType.Oxygen,
                DataConfig.WorldUnitType.Pressure,
                DataConfig.WorldUnitType.Biomass,
                DataConfig.WorldUnitType.Terraformation
            };
        }
        [HarmonyPatch(typeof(WorldUnitsDisplayer), "OnEnable")]
        private static class WorldUnitsDisplayer_OnEnable_Patch
        {
            static void Postfix(WorldUnitsDisplayer __instance)
            {
                if (!modEnabled.Value)
                    return;
                foreach(var field in __instance.textFields)
                {
                    if(field.fontSize == 1.6f)
                    {
                        field.fontSize = 1.3f;
                        field.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, -0.035f);
                    }
                }
            }
        }
        public static IEnumerable<CodeInstruction> WorldUnitsDisplayer_RefreshDisplay_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            if (!modEnabled.Value)
                return codes.AsEnumerable();

            Dbgl("Transpiling WorldUnitsDisplayer.RefreshDisplay");
            for (int i = 0; i < codes.Count; i++)
            {
                if (i < codes.Count - 1 && codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(WorldUnit), nameof(WorldUnit.GetValueString)))
                {
                    Dbgl("Adding method to affect string");
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(GetValueString))));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_3));
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        private static string GetValueString(string str, WorldUnit unit)
        {
            var unitType = unit.GetUnitType();
            float next = float.MaxValue;
            foreach (Group group in Managers.GetManager<UnlockingHandler>().GetUnlockableGroupsOverUnit(unitType))
            {
                if (group is null)
                    continue;

                UnlockingInfos unlockingInfos = group.GetUnlockingInfos();
                if (unlockingInfos is null)
                    continue;
                if (unlockingInfos.GetUnlockingValue() < next)
                {
                    next = unlockingInfos.GetUnlockingValue();
                }
            }
            if (next < float.MaxValue)
            {
                str += " " + string.Format(nextString.Value, unit.GetDisplayStringForValue(next));
            }
            return str;
        }
    }
}
