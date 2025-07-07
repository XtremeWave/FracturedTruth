using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FinalSuspect.Modules.ClientActions.FeatureItems.MyMusic;

public class AudioLoader
{
    // 静态构造函数用于预热
    static AudioLoader()
    {
        Warmup();
    }

    private static void Warmup()
    {
        var dummyBytes = new byte[2];
        ConvertBytesToFloats(dummyBytes);

        var warmupClip = AudioClip.Create("Warmup", 1, 1, 44100, false);
        warmupClip.SetData(new float[] { 0 }, 0);
        Object.Destroy(warmupClip);
    }

    public static async Task<AudioClip> LoadAudioClipAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File does not exist: " + filePath);
            return null;
        }

        byte[] audioData;

        try
        {
            audioData = await ReadAllBytesAsync(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to read file: " + filePath + "\n" + e.Message);
            return null;
        }

        var floatData = await Task.Run(() => ConvertBytesToFloats(audioData));
        // ReSharper disable once RedundantAssignment
        audioData = null; // 及时释放内存

        var audioClip = AudioClip.Create("LoadedAudioClip", floatData.Length, 2, 44100, false);
        audioClip.SetData(floatData, 0);

        return audioClip;
    }

    private static async Task<byte[]> ReadAllBytesAsync(string filePath)
    {
        await using var sourceStream =
            new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var buffer = new byte[sourceStream.Length];
        _ = await sourceStream.ReadAsync(buffer.AsMemory(0, (int)sourceStream.Length)).ConfigureAwait(false);
        return buffer;
    }

    private static float[] ConvertBytesToFloats(byte[] audioBytes)
    {
        var floatCount = audioBytes.Length / 2;
        var floatData = new float[floatCount];

        unsafe
        {
            fixed (byte* bytePtr = audioBytes)
            fixed (float* floatPtr = floatData)
            {
                var src = (short*)bytePtr;
                for (var i = 0; i < floatCount; i++) floatPtr[i] = src[i] / 32768.0f;
            }
        }

        return floatData;
    }
}