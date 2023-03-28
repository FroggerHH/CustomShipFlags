using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CustomShipFlags.Plugin;

namespace CustomShipFlags
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake)), HarmonyPostfix]
        private static void ShipAwakePatch(Ship __instance)
        {
            __instance.transform.Find(path)?.gameObject.AddComponent<CustomFlagComponent>();
        }
    }
}