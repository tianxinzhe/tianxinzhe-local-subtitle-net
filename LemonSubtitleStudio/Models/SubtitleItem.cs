
using System;

namespace LemonSubtitleStudio.Models
{
    public class SubtitleItem
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }

        public string StartTimeFormatted => FormatTime(StartTime);
        public string EndTimeFormatted => FormatTime(EndTime);
        public string DurationFormatted => FormatTime(EndTime - StartTime);

        private string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }
    }
}
