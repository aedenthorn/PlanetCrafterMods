using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BetterJetPack
{
    [BepInPlugin("aedenthorn.BetterJetPack", "Better JetPack", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> removeDropEffect;
        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<float> iridiumChance;
        public static ConfigEntry<float> asteroidResourceMult;

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
            removeDropEffect = Config.Bind<bool>("Options", "RemoveDropEffect", true, "Remove the sharp drop effect when you jetpack off a cliff");
            speedMult = Config.Bind<float>("Options", "SpeedMult", 1f, "Jetpack speed multiplier");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PlayerMovable), nameof(PlayerMovable.UpdatePlayerMovement))]
        static class PlayerMovable_UpdatePlayerMovement_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl("Transpiling PlayerMovable.UpdatePlayerMovement");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (i < codes.Count - 1 && codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == -5 && codes[i + 1].opcode == OpCodes.Ldc_R4 && (float)codes[i + 1].operand == 50)
                    {
                        Dbgl("Removing fall set on jetpack");
                        codes[i + 1].operand = 20f;
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
            static void Prefix(PlayerMovable __instance, ref float ___jetpackFactor, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = ___jetpackFactor;
                ___jetpackFactor *= speedMult.Value;
            }
            static void Postfix(PlayerMovable __instance, ref float ___jetpackFactor, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                ___jetpackFactor = __state;
            }
        }
    }
}
