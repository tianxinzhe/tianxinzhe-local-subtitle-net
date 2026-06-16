using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LemonSubtitleStudio.Models;

namespace LemonSubtitleStudio.Services
{
    public interface ISubtitleService
    {
        List<SubtitleItem> LoadFromSrt(string path);
        List<SubtitleItem> LoadFromVtt(string path);
        void SaveToSrt(string path, List<SubtitleItem> subtitles);
        void SaveToVtt(string path, List<SubtitleItem> subtitles);
        void SaveToBilingualSrt(string path, List<SubtitleItem> subtitles);
    }

    public class SubtitleService : ISubtitleService
    {
        public List<SubtitleItem> LoadFromSrt(string path)
        {
            var subtitles = new List<SubtitleItem>();
            var lines = File.ReadAllLines(path);
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (int.TryParse(lines[i].Trim(), out int index))
                {
                    if (i + 2 < lines.Length)
                    {
                        var timeLine = lines[i + 1].Trim();
                        
                        var timeParts = timeLine.Split(new[] { " --> " }, StringSplitOptions.None);
                        if (timeParts.Length == 2)
                        {
                            var textLines = new List<string>();
                            int textIndex = i + 2;
                            while (textIndex < lines.Length && !string.IsNullOrWhiteSpace(lines[textIndex]))
                            {
                                textLines.Add(lines[textIndex].Trim());
                                textIndex++;
                            }
                            var text = string.Join("\n", textLines);
                            
                            subtitles.Add(new SubtitleItem
                            {
                                Index = index,
                                StartTime = ParseTime(timeParts[0]),
                                EndTime = ParseTime(timeParts[1]),
                                OriginalText = text
                            });
                        }
                    }
                }
            }
            
            return subtitles;
        }

        public List<SubtitleItem> LoadFromVtt(string path)
        {
            var subtitles = new List<SubtitleItem>();
            var lines = File.ReadAllLines(path);
            int index = 1;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("-->"))
                {
                    var timeParts = line.Split(new[] { " --> " }, StringSplitOptions.None);
                    if (timeParts.Length == 2 && i + 1 < lines.Length)
                    {
                        var text = lines[i + 1].Trim();
                        subtitles.Add(new SubtitleItem
                        {
                            Index = index++,
                            StartTime = ParseTime(timeParts[0]),
                            EndTime = ParseTime(timeParts[1]),
                            OriginalText = text
                        });
                    }
                }
            }
            
            return subtitles;
        }

        public void SaveToSrt(string path, List<SubtitleItem> subtitles)
        {
            using var writer = new StreamWriter(path);
            foreach (var subtitle in subtitles)
            {
                writer.WriteLine(subtitle.Index);
                writer.WriteLine($"{FormatTime(subtitle.StartTime)} --> {FormatTime(subtitle.EndTime)}");
                writer.WriteLine(subtitle.OriginalText);
                writer.WriteLine();
            }
        }

        public void SaveToVtt(string path, List<SubtitleItem> subtitles)
        {
            using var writer = new StreamWriter(path);
            writer.WriteLine("WEBVTT");
            writer.WriteLine();
            
            foreach (var subtitle in subtitles)
            {
                writer.WriteLine($"{FormatTime(subtitle.StartTime)} --> {FormatTime(subtitle.EndTime)}");
                writer.WriteLine(subtitle.OriginalText);
                writer.WriteLine();
            }
        }

        public void SaveToBilingualSrt(string path, List<SubtitleItem> subtitles)
        {
            using var writer = new StreamWriter(path);
            foreach (var subtitle in subtitles)
            {
                writer.WriteLine(subtitle.Index);
                writer.WriteLine($"{FormatTime(subtitle.StartTime)} --> {FormatTime(subtitle.EndTime)}");
                writer.WriteLine(subtitle.OriginalText);
                if (!string.IsNullOrEmpty(subtitle.TranslatedText))
                {
                    writer.WriteLine(subtitle.TranslatedText);
                }
                writer.WriteLine();
            }
        }

        private TimeSpan ParseTime(string timeStr)
        {
            timeStr = timeStr.Replace(",", ".");
            
            if (TimeSpan.TryParse(timeStr, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            
            var pattern = @"(\d{2}):(\d{2}):(\d{2})[.,](\d{3})";
            var match = Regex.Match(timeStr, pattern);
            if (match.Success)
            {
                return new TimeSpan(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value),
                    0,
                    int.Parse(match.Groups[4].Value)
                );
            }
            
            return TimeSpan.Zero;
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
        }
    }
}