using QRCoder;

namespace FinalSuspect.Patches.System;

[HarmonyPatch]
public class VoteBanSystemPatch
{
    // thanks: NikoCat233
    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote)), HarmonyPrefix]
    public static bool AddVote(VoteBanSystem __instance)
    {
        return false;
    }
}