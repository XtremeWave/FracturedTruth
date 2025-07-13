using Il2CppSystem.Reflection.Internal;
using UnityEngine;

namespace FinalSuspect.Patches.System;

[HarmonyPatch]
public class ShipStatusPatch
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    [HarmonyPostfix]
    public static void ShipStatus_Start(ShipStatus __instance)
    {
        Test(__instance.name);
        if (__instance.name.Contains("Skeld"))
        {
            var speed = HashRandom.Next(-20, -1);
            DestroyableSingleton<StarGen>.Instance.SetDirection(new Vector2(speed, 0));
        }
        else if (__instance.name.Contains("Mira"))
        {
            Retry:
            var speed_x = HashRandom.Next(-10, 10);
            var speed_y = HashRandom.Next(-10, 10);
            if (speed_x is 0 || speed_y is 0) goto Retry;
            DestroyableSingleton<CloudGenerator>.Instance.SetDirection(new Vector2(speed_x, speed_y));
        }
        else if (__instance.name.Contains("Polus"))
        {
            var speed = HashRandom.Next(1, 20);
            var size = HashRandom.Next(1, 100);
            size /= 1000;
            DestroyableSingleton<SnowManager>.Instance.particles.startSpeed = speed;
            DestroyableSingleton<SnowManager>.Instance.particles.startSize = size;
        }
        else if (__instance.name.Contains("Airship"))
        {
            var speed = HashRandom.Next(1, 10);
            var cloudGenerators = Object.FindObjectsOfType<CloudGenerator>();
            foreach (var generator in cloudGenerators)
                generator.SetDirection(new Vector2(speed, 0));
        }
    }
}