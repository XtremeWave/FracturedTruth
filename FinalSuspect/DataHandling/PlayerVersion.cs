using System;

namespace FinalSuspect.DataHandling;

public static partial class XtremeGameData
{
    public class PlayerVersion(Version ver, string tag_str, string forkId)
    {
        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        public readonly string forkId = forkId;
        public readonly string tag = tag_str;
        public readonly Version version = ver;

        public PlayerVersion(string ver, string tag_str, string forkId) : this(Version.Parse(ver), tag_str, forkId)
        {
        }
    }
}