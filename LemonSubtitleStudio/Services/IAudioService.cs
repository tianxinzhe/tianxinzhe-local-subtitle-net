using System;
using System.Threading.Tasks;

namespace LemonSubtitleStudio.Services
{
    public interface IAudioService
    {
        Task<string> ExtractAudioAsync(string inputPath, string outputDir, string format = "mp3", int bitrate = 192);
        Task<string> ConvertToWavAsync(string inputPath, string outputDir, int sampleRate = 16000);
        bool IsFfmpegAvailable();
    }
}