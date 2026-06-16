using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace LemonSubtitleStudio.Services
{
    public class AudioService : IAudioService
    {
        private readonly ILoggingService _loggingService;

        public AudioService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public bool IsFfmpegAvailable()
        {
            try
            {
                return !string.IsNullOrEmpty(FFmpeg.ExecutablesPath);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("检查FFmpeg可用性失败", ex);
                return false;
            }
        }

        public async Task<string> ExtractAudioAsync(string inputPath, string outputDir, string format = "mp3", int bitrate = 192)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var extension = format.ToLower() switch
            {
                "wav" => ".wav",
                "mp3" => ".mp3",
                "flac" => ".flac",
                "ogg" => ".ogg",
                _ => ".mp3"
            };
            var outputPath = Path.Combine(outputDir, $"{fileName}{extension}");

            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
            
            var conversion = FFmpeg.Conversions.New();
            conversion.AddStream(audioStream);
            conversion.SetOutput(outputPath);
            conversion.AddParameter($"-b:a {bitrate}k");
            await conversion.Start();
            return outputPath;
        }

        public async Task<string> ConvertToWavAsync(string inputPath, string outputDir, int sampleRate = 16000)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(outputDir, $"{fileName}_converted.wav");

            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
            
            var conversion = FFmpeg.Conversions.New();
            conversion.AddStream(audioStream);
            conversion.SetOutput(outputPath);
            conversion.AddParameter($"-ar {sampleRate}");
            conversion.AddParameter("-ac 1");
            conversion.AddParameter("-sample_fmt s16");
            await conversion.Start();
            return outputPath;
        }
    }
}
