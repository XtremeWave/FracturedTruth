using System;
using System.Diagnostics;
using FinalSuspect.DataHandling.FinalAntiCheat.Interfaces;
using FinalSuspect.Patches.Game_Vanilla;
using Hazel;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

public static class FAC
{
    public static int DeNum;
    public static long _lastHandleCheater = -1;
    public static readonly List<RpcHandlers> _handlers = [];

    static FAC()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => typeof(IRpcHandler).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var handler = (IRpcHandler)Activator.CreateInstance(type);
            Debug.Assert(handler != null, nameof(handler) + " != null");
            var rpcTypes = handler.TargetRpcs;

            var activehandler = new RpcHandlers(rpcTypes);
            activehandler.Handlers.Add(handler);

            _handlers.Add(activehandler);
        }
    }

    public static void Init()
    {
        DeNum = 0;
    }

    public static void WarnHost(int denum = 1)
    {
        DeNum += denum;
        if (!ErrorText.Instance) return;
        ErrorText.Instance.CheatDetected = DeNum > 3;
        ErrorText.Instance.SBDetected = DeNum > 10;
        if (ErrorText.Instance.CheatDetected)
            ErrorText.Instance.AddError(ErrorText.Instance.SBDetected ? ErrorCode.SBDetected : ErrorCode.CheatDetected);
        else
            ErrorText.Instance.Clear();
    }

    public static bool ReceiveRpc(PlayerControl pc, byte callId, MessageReader reader, out bool notify,
        out string reason, out bool ban)
    {
        notify = true;
        reason = "Hacking";
        ban = false;

        if (Main.DisableFAC.Value || !pc || reader == null || pc.AmOwner)
            return false;

        try
        {
            if (pc.GetCheatData().HandleIncomingRpc(callId))
            {
                ban = true;
                return true;
            }

            var sr = MessageReader.Get(reader);

            var handlers = _handlers.FirstOrDefault(x => x.TargetRpcs.Contains(callId))?.Handlers;

            if (handlers != null)
                foreach (var handler in handlers)
                {
                    if (!Enum.IsDefined(typeof(RpcCalls), callId))
                    {
                        notify = false;
                        if (handler.HandleInvalidRPC(pc, sr, ref notify, ref reason, ref ban))
                        {
                            ban = true;
                            if (reason == "Hacking")
                                reason = GetString("Unknown");
                            NotificationPopperPatch.NotificationPop(
                                string.Format(GetString("FAC.CheatDetected.UsingCheat"), pc.GetColoredName(), reason));
                            return true;
                        }

                        NotificationPopperPatch.NotificationPop(
                            string.Format(GetString("FAC.CheatDetected.MayUseCheat"), pc.GetColoredName(), reason));
                        return false;
                    }

                    if (handler.HandleAll(pc, sr, ref notify, ref reason, ref ban))
                        return true;

                    if (IsLobby && handler.HandleLobby(pc, sr, ref notify, ref reason, ref ban))
                    {
                        if (AmongUsClient.Instance.AmHost) return true;
                        NotificationPopperPatch.NotificationPop(GetString("Warning.RoomBroken"));
                        notify = false;
                        return true;
                    }

                    return (IsInGame && handler.HandleGame_All(pc, sr, ref notify, ref reason, ref ban))
                           || (IsInTask && handler.HandleGame_InTask(pc, sr, ref notify, ref reason, ref ban))
                           || (IsMeeting && handler.HandleGame_InMeeting(pc, sr, ref notify, ref reason, ref ban));
                }
        }
        catch (Exception e)
        {
            Fatal(e.ToString(), "FAC");
        }

        WarnHost(-1);
        return false;
    }

    public static void HandleCheat(PlayerControl pc, string text) =>
        NotificationPopperPatch.NotificationPop(string.Format(text, pc.GetColoredName()));

    public static void Dispose(byte id)
    {
        foreach (var handler in _handlers)
        {
            handler.Handlers.Do(x => x.Dispose(id));
        }
    }
}