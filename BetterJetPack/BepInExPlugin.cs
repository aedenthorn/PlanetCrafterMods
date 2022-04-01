using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Random = UnityEngine.Random;

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
                bool found = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!found && i < codes.Count - 2 && codes[i + 2].opcode == OpCodes.Callvirt && (MethodInfo)codes[i + 2].operand == AccessTools.Method(typeof(PlayerGroundRelation), nameof(PlayerGroundRelation.GetGroundDistance)))
                    {
                        Dbgl("Removing fall set on jetpack");
                        found = true;
                    }
                    if (found)
                    {
                        if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PlayerMovable), "m_Fall") && codes[i + 1].opcode == OpCodes.Stloc_3)
                        {
                            codes[i].opcode = OpCodes.Nop;
                            codes[i].operand = null;
                            codes[i + 1].opcode = OpCodes.Nop;
                            codes[i + 1].operand = null;
                            break;
                        }

                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        //[HarmonyPatch(typeof(MeteoHandler), nameof(MeteoHandler.LaunchSpecificMeteoEvent))]
        static class MeteoHandler_LaunchSpecificMeteoEvent_Patch
        {
            static void Prefix(MeteoHandler __instance, ref MeteoEventData _meteoEvent)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Getting asteroid meteo event");
                double r = Random.value;
                if (r < speedMult.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                        if (e != null)
                        {
                            _meteoEvent = e;
                            Dbgl($"Got uranium asteroid");
                            return;
                        }
                    }
                }
                else if (r < speedMult.Value + iridiumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Iridium"));
                        if (e != null)
                        {
                            _meteoEvent = e;
                            Dbgl($"Got iridium asteroid");
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(AsteroidsImpactHandler), nameof(AsteroidsImpactHandler.CreateImpact))]
        static class Asteroid_CreateImpact_Patch
        {
            static void Prefix(AsteroidsImpactHandler __instance, ref Asteroid _asteroid)
            {
                if (!modEnabled.Value || __instance.name.Contains("Uranium") || __instance.name.Contains("Iridium"))
                    return;
                double r = Random.value;
                if (r < speedMult.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                        if (e != null)
                        {
                            var a = e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>();
                            var groups = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(_asteroid, "associatedGroups");
                            foreach (GroupDataItem groupDataItem in a.groupsSelected)
                            {
                                groups.Add((GroupItem)GroupsHandler.GetGroupViaId(groupDataItem.id));
                            }
                            //Dbgl($"Got uranium asteroid");
                        }
                    }
                }
                else if (r < speedMult.Value + iridiumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Iridium"));
                        if (e != null)
                        {
                            var a = e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>();
                            var groups = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(_asteroid, "associatedGroups");
                            foreach (GroupDataItem groupDataItem in a.groupsSelected)
                            {
                                groups.Add((GroupItem)GroupsHandler.GetGroupViaId(groupDataItem.id));
                            }
                            //Dbgl($"Got iridium asteroid");
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Asteroid), nameof(Asteroid.GetNumberOfResourceInDebris))]
        static class Asteroid_GetNumberOfResourceInDebris_Patch
        {
            static void Postfix(Asteroid __instance, ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result *= asteroidResourceMult.Value;
            }
        }
    }
}
