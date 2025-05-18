using System.Collections.Generic;
using FinalSuspect.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Handlers.Valid;

// 19, 20
public class VentHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.EnterVent,
        (byte)RpcCalls.ExitVent,
    ];

//检测所有会因为树懒的延迟刀出现管道杀而被FS误判
    public bool HandleLobby(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }
}