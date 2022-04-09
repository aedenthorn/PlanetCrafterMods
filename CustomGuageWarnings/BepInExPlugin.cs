using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using BepInEx.Logging;

namespace CustomGuageWarnings
{
    [BepInPlugin("aedenthorn.CustomGuageWarnings", "Custom Guage Warnings", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> thirstCaution;
        public static ConfigEntry<float> thirstWarning;
        public static ConfigEntry<float> hungerCaution;
        public static ConfigEntry<float> hungerWarning;
        public static ConfigEntry<float> oxygenCaution;
        public static ConfigEntry<float> oxygenWarning;


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
            thirstCaution = Config.Bind<float>("Options", "ThirstCaution", 25, "Thirst caution level.");
            thirstWarning = Config.Bind<float>("Options", "ThirstWarning", 10, "Thirst warning level.");
            hungerCaution = Config.Bind<float>("Options", "HungerCaution", 25, "hunger caution level.");
            hungerWarning = Config.Bind<float>("Options", "HungerWarning", 10, "hunger warning level.");
            oxygenCaution = Config.Bind<float>("Options", "OxygenCaution", 30, "oxygen caution level.");
            oxygenWarning = Config.Bind<float>("Options", "OxygenWarning", 16, "oxygen warning level.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(PlayerGaugeThirst), "GaugeVerifications")]
        static class PlayerGaugeThirst_GaugeVerifications_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!modEnabled.Value)
                    return codes.AsEnumerable();

                Dbgl("Transpiling PlayerGaugeThirst.GaugeVerifications");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 25)
                    {
                        Dbgl($"Switching 25 to {thirstCaution.Value}");
                    }
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 10)
                    {
                        Dbgl($"Switching 10 to {thirstWarning.Value}");
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(PlayerGaugeHealth), "GaugeVerifications")]
        static class PlayerGaugeHealth_GaugeVerifications_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!modEnabled.Value)
                    return codes.AsEnumerable();

                Dbgl("Transpiling PlayerGaugeHealth.GaugeVerifications");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 25)
                    {
                        Dbgl($"Switching 25 to {hungerCaution.Value}");
                    }
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 10)
                    {
                        Dbgl($"Switching 10 to {hungerWarning.Value}");
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(PlayerGaugeOxygen), "GaugeVerifications")]
        static class PlayerGaugeOxygen_GaugeVerifications_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!modEnabled.Value)
                    return codes.AsEnumerable();

                Dbgl("Transpiling PlayerGaugeOxygen.GaugeVerifications");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 30)
                    {
                        Dbgl($"Switching 30 to {oxygenCaution.Value}");
                    }
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 16)
                    {
                        Dbgl($"Switching 16 to {oxygenWarning.Value}");
                    }
                }
                return codes.AsEnumerable();
            }
        }
    }
}
