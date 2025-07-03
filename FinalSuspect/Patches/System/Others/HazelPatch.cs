using Hazel;

namespace FinalSuspect.Patches.System.Others;

[HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadUInt16))]
[HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadPackedUInt32))]
[HarmonyPriority(Priority.First)]
internal class HazelPatch
{
    public static bool Prefix(MessageReader __instance)
    {
        return __instance.Length > 0;
    }
}