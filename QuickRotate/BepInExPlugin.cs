using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace QuickRotate
{
    [BepInPlugin("aedenthorn.QuickRotate", "Quick Rotate", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> modKeyOne;
        private static ConfigEntry<string> modKeyTwo;
        private static ConfigEntry<float> rotateOne;
        private static ConfigEntry<float> rotateTwo;
        private static ConfigEntry<float> rotateThree;

        private static InputAction action1;
        private static InputAction action2;

        private static bool skip;

        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            modKeyOne = Config.Bind<string>("Options", "ModKeyOne", "<Keyboard>/leftShift", "First mod key");
            modKeyTwo = Config.Bind<string>("Options", "ModKeyTwo", "<Keyboard>/leftCtrl", "Second mod key");
            rotateOne = Config.Bind<float>("Options", "RotateOne", 45, "Rotation while ModKeyOne is held");
            rotateTwo = Config.Bind<float>("Options", "RotateTwo", 90, "Rotation while ModKeyTwo is held");
            rotateThree = Config.Bind<float>("Options", "RotateThree", 180, "Rotation while both mod keys are held");

            action1 = new InputAction(binding: modKeyOne.Value);
            action1.Enable();
            action2 = new InputAction(binding: modKeyTwo.Value);
            action2.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(PlayerBuilder), nameof(PlayerBuilder.InputOnRotateObject))]
        private static class PlayerBuilder_InputOnRotateObject_Patch
        {
            static bool Prefix(Inventory __instance, Vector2 rotate, ConstructibleGhost ___ghost, GroupConstructible ___ghostGroupConstructible)
            {
                if (!modEnabled.Value || rotate.y == 0 || ___ghost is null || ___ghostGroupConstructible.GetRotationFixed())
                    return true;
                float oneDegree = rotate.y < 0 ? -1 : 1;
                if (action1.IsPressed() && action2.IsPressed())
                {
                    //Dbgl($"Rotating {oneDegree * rotateOne.Value} degrees");
                    ___ghost.transform.Rotate(0f, oneDegree * rotateThree.Value, 0f);
                    return false;
                }
                if (action1.IsPressed())
                {
                    //Dbgl($"Rotating {oneDegree * rotateOne.Value} degrees");
                    ___ghost.transform.Rotate(0f, oneDegree * rotateOne.Value, 0f);
                    return false;
                }
                if (action2.IsPressed())
                {
                    //Dbgl($"Rotating {oneDegree * rotateTwo.Value} degrees");
                    ___ghost.transform.Rotate(0f, oneDegree * rotateTwo.Value, 0f);
                    return false;
                }
                return true;
            }
        }
    }
}
