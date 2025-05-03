using System.Linq;
using static AmongUs.GameOptions.RoleTypes;
using Object = UnityEngine.Object;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.UpdateButtons))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            try
            {
                if (__instance == null) return; 
                if (AmongUsClient.Instance?.AmHost == true) return; 

                for (var i = 0; i < __instance.playerStates?.Length; i++) 
                {
                    var playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea == null) continue; 

                    var playerById = GameData.Instance?.GetPlayerById(playerVoteArea.TargetPlayerId);
                    if (playerById == null)
                    {
                        playerVoteArea.SetDisabled();
                    }
                    else
                    {
                        var flag = playerById.Disconnected || playerById.IsDead;
                        if (flag == playerVoteArea.AmDead) continue;
                        var isReporter = __instance.reporterId == playerById.PlayerId; 
                        playerVoteArea.SetDead(isReporter, flag, 
                            playerById.Role?.Role == GuardianAngel);
                        __instance.SetDirtyBit(1U);
                    }
                }
            }
            catch 
            {
                /* ignored */
            }
        }
    }
 
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    [HarmonyPriority(Priority.First)]
    class VotingCompletePatch
    {
        public static void Postfix([HarmonyArgument(1)]NetworkedPlayerInfo exiled, [HarmonyArgument(2)]bool tie )
        {
            foreach (var data in XtremePlayerData.AllPlayerData.Where(data => data?.Deadbodyrend != null))
            {
                Object.Destroy(data.Deadbodyrend);
                data.Deadbodyrend = null;
            }

            if (tie || exiled == null) return;
            var player = GetPlayerById(exiled.PlayerId);
            player.SetDead();
            player.SetDeathReason(VanillaDeathReason.Exile, true);
        }
    }
}
[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}