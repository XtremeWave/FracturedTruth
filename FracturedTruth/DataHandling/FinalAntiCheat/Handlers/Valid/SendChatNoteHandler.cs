using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 16
public class SendChatNoteHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SendChatNote
    ];

    public bool HandleLobby(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }

    public bool HandleGame_InTask(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }
}