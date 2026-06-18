using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LemonSubtitleStudio.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace LemonSubtitleStudio.Services
{
    public class TranslationService : ITranslationService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;
        private readonly List<string> _availableModels = new List<string> { "marianmt-zh-en", "marianmt-en-zh", "nllb-200-distilled-600M", "m2m100-418M" };
        private readonly Dictionary<string, OnnxModelSession> _sessions = new();
        private bool _disposed = false;

        public TranslationService(ISettingsService settingsService, ILoggingService loggingService)
        {
            _settingsService = settingsService;
            _loggingService = loggingService;
        }

        public Task<List<string>> GetAvailableModels() => Task.FromResult(_availableModels);

        public Task<bool> IsModelAvailable(string modelName)
        {
            var modelDir = GetModelPath(modelName);
            return Task.FromResult(Directory.Exists(modelDir) && File.Exists(Path.Combine(modelDir, "encoder_model.onnx")));
        }

        private string GetModelPath(string modelName)
        {
            return Path.Combine(_settingsService.ModelStoragePath, "onnx", modelName);
        }

        private OnnxModelSession GetOrCreateSession(string modelName)
        {
            if (_sessions.TryGetValue(modelName, out var existing))
                return existing;

            var modelDir = GetModelPath(modelName);
            var config = LoadModelConfig(modelDir);
            var tokenizer = LoadTokenizer(modelDir, config);

            var encoderPath = Path.Combine(modelDir, "encoder_model.onnx");
            var decoderPath = Path.Combine(modelDir, "decoder_model.onnx");

            var session = new OnnxModelSession
            {
                EncoderSession = new InferenceSession(encoderPath),
                DecoderSession = new InferenceSession(decoderPath),
                Config = config,
                Tokenizer = tokenizer
            };

            _sessions[modelName] = session;
            return session;
        }

        private ModelConfig LoadModelConfig(string modelDir)
        {
            var configPath = Path.Combine(modelDir, "config.json");
            var configJson = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = configJson.RootElement;

            return new ModelConfig
            {
                DecoderStartTokenId = root.TryGetProperty("decoder_start_token_id", out var ds) ? ds.GetInt32() : 2,
                EosTokenId = root.TryGetProperty("eos_token_id", out var eos) ? eos.GetInt32() : 2,
                PadTokenId = root.TryGetProperty("pad_token_id", out var pad) ? pad.GetInt32() : 1,
                MaxLength = root.TryGetProperty("max_length", out var ml) && ml.ValueKind != JsonValueKind.Null ? ml.GetInt32() : 200,
                VocabSize = root.TryGetProperty("vocab_size", out var vs) ? vs.GetInt32() : 0,
                DModel = root.TryGetProperty("d_model", out var dm) ? dm.GetInt32() : 0
            };
        }

        private Tokenizer LoadTokenizer(string modelDir, ModelConfig config)
        {
            var vocabPath = Path.Combine(modelDir, "vocab.json");
            var tokenizerJsonPath = Path.Combine(modelDir, "tokenizer.json");

            var vocabJson = JsonDocument.Parse(File.ReadAllText(vocabPath));
            var stringToId = new Dictionary<string, int>();
            var idToString = new Dictionary<int, string>();
            foreach (var prop in vocabJson.RootElement.EnumerateObject())
            {
                int id = prop.Value.GetInt32();
                stringToId[prop.Name] = id;
                idToString[id] = prop.Name;
            }

            if (File.Exists(tokenizerJsonPath))
            {
                var tokenizerJson = JsonDocument.Parse(File.ReadAllText(tokenizerJsonPath));
                var model = tokenizerJson.RootElement.GetProperty("model");
                var modelType = model.GetProperty("type").GetString();

                if (modelType == "Unigram")
                {
                    var unigramVocab = new Dictionary<string, double>();
                    foreach (var entry in model.GetProperty("vocab").EnumerateArray())
                        unigramVocab[entry[0].GetString()] = entry[1].GetDouble();

                    return new UnigramTokenizer(stringToId, idToString, unigramVocab);
                }
                else if (modelType == "BPE")
                {
                    var bpeVocab = new Dictionary<string, int>();
                    if (model.TryGetProperty("vocab", out var bpeVocabJson))
                    {
                        foreach (var prop in bpeVocabJson.EnumerateObject())
                            bpeVocab[prop.Name] = prop.Value.GetInt32();
                    }

                    var merges = new List<(string, string)>();
                    if (model.TryGetProperty("merges", out var mergesJson))
                    {
                        foreach (var merge in mergesJson.EnumerateArray())
                        {
                            var parts = merge.GetString().Split(' ');
                            if (parts.Length == 2)
                                merges.Add((parts[0], parts[1]));
                        }
                    }

                    return new BPETokenizer(stringToId, idToString, bpeVocab, merges);
                }
            }

            return new UnigramTokenizer(stringToId, idToString, new Dictionary<string, double>());
        }

        public async Task<string> TranslateTextAsync(string content, string sourceLang, string targetLang, string modelName)
        {
            if (!await IsModelAvailable(modelName))
                return content;

            try
            {
                var session = GetOrCreateSession(modelName);
                return await Task.Run(() => TranslateWithOnnx(content, session));
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"ONNX translation failed for {modelName}", ex);
                return content;
            }
        }

        private string TranslateWithOnnx(string text, OnnxModelSession session)
        {
            var inputIds = session.Tokenizer.Encode(text);
            if (inputIds.Count == 0) return text;

            var inputIdsArray = inputIds.Select(id => (long)id).ToArray();
            var attentionMask = inputIdsArray.Select(_ => 1L).ToArray();

            var encInputIds = new DenseTensor<long>(inputIdsArray, new[] { 1, inputIdsArray.Length });
            var encAttnMask = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });

            using var encResults = session.EncoderSession.Run(new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", encInputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", encAttnMask)
            });
            var lastHiddenState = encResults.First(r => r.Name == "last_hidden_state").AsTensor<float>();

            var generatedTokens = AutoregressiveDecode(session, lastHiddenState, attentionMask);
            return session.Tokenizer.Decode(generatedTokens);
        }

        private List<int> AutoregressiveDecode(OnnxModelSession session, Tensor<float> encoderHiddenState, long[] encoderAttentionMask)
        {
            var config = session.Config;
            var decInputIds = new List<long> { config.DecoderStartTokenId };
            var generatedTokens = new List<int>();
            var generatedTokenSet = new HashSet<int>();
            float repetitionPenalty = 1.5f;
            int maxLength = Math.Min(config.MaxLength, 200);

            for (int step = 0; step < maxLength; step++)
            {
                var decInputTensor = new DenseTensor<long>(decInputIds.ToArray(), new[] { 1, decInputIds.Count });
                var encAttnTensor = new DenseTensor<long>(encoderAttentionMask, new[] { 1, encoderAttentionMask.Length });

                using var decResults = session.DecoderSession.Run(new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", decInputTensor),
                    NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encAttnTensor),
                    NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenState)
                });

                var logits = decResults.First(r => r.Name == "logits").AsTensor<float>();
                int seqLen = decInputIds.Count;
                int vocabSize = logits.Dimensions[2];

                float maxLogit = float.MinValue;
                int nextToken = 0;
                for (int v = 0; v < vocabSize; v++)
                {
                    float logit = logits[0, seqLen - 1, v];
                    if (generatedTokenSet.Contains(v))
                        logit = logit > 0 ? logit / repetitionPenalty : logit * repetitionPenalty;
                    if (logit > maxLogit) { maxLogit = logit; nextToken = v; }
                }

                if (nextToken == config.EosTokenId) break;

                generatedTokens.Add(nextToken);
                generatedTokenSet.Add(nextToken);
                decInputIds.Add(nextToken);
            }

            return generatedTokens;
        }

        public async Task<List<SubtitleItem>> TranslateSubtitlesAsync(List<SubtitleItem> subtitles, string sourceLang, string targetLang, string modelName, IProgress<int> progress)
        {
            var translated = new List<SubtitleItem>();

            for (int i = 0; i < subtitles.Count; i++)
            {
                var subtitle = subtitles[i];
                var translatedText = await TranslateTextAsync(subtitle.OriginalText, sourceLang, targetLang, modelName);

                translated.Add(new SubtitleItem
                {
                    Index = subtitle.Index,
                    StartTime = subtitle.StartTime,
                    EndTime = subtitle.EndTime,
                    OriginalText = subtitle.OriginalText,
                    TranslatedText = translatedText
                });

                progress?.Report((int)((i + 1) * 100.0 / subtitles.Count));
            }

            return translated;
        }

        public async Task<string> TranslateTextWithWebAsync(string content, string sourceLang, string targetLang)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var langMap = new Dictionary<string, string>
                {
                    ["中文"] = "zh",
                    ["English"] = "en",
                    ["日本語"] = "ja",
                    ["한국어"] = "ko"
                };
                var src = langMap.TryGetValue(sourceLang, out var s) ? s : "auto";
                var tgt = langMap.TryGetValue(targetLang, out var t) ? t : "en";
                var encoded = Uri.EscapeDataString(content);
                var url = $"https://lingva.ml/api/v1/{src}/{tgt}/{encoded}";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var translation = doc.RootElement.GetProperty("translation").GetString();
                return translation ?? content;
            }
            catch
            {
                return content;
            }
        }

        public async Task<List<SubtitleItem>> TranslateSubtitlesWithWebAsync(List<SubtitleItem> subtitles, string sourceLang, string targetLang, IProgress<int> progress)
        {
            var translated = new List<SubtitleItem>();

            for (int i = 0; i < subtitles.Count; i++)
            {
                var subtitle = subtitles[i];
                var translatedText = await TranslateTextWithWebAsync(subtitle.OriginalText, sourceLang, targetLang);

                translated.Add(new SubtitleItem
                {
                    Index = subtitle.Index,
                    StartTime = subtitle.StartTime,
                    EndTime = subtitle.EndTime,
                    OriginalText = subtitle.OriginalText,
                    TranslatedText = translatedText
                });

                progress?.Report((int)((i + 1) * 100.0 / subtitles.Count));
            }

            return translated;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                foreach (var session in _sessions.Values)
                {
                    session.EncoderSession?.Dispose();
                    session.DecoderSession?.Dispose();
                }
                _sessions.Clear();
            }

            _disposed = true;
        }

        ~TranslationService()
        {
            Dispose(false);
        }
    }

    internal class OnnxModelSession
    {
        public InferenceSession EncoderSession { get; set; } = null!;
        public InferenceSession DecoderSession { get; set; } = null!;
        public ModelConfig Config { get; set; } = null!;
        public Tokenizer Tokenizer { get; set; } = null!;
    }

    internal class ModelConfig
    {
        public int DecoderStartTokenId { get; set; }
        public int EosTokenId { get; set; }
        public int PadTokenId { get; set; }
        public int MaxLength { get; set; } = 200;
        public int VocabSize { get; set; }
        public int DModel { get; set; }
    }

    internal abstract class Tokenizer
    {
        protected readonly Dictionary<string, int> _stringToId;
        protected readonly Dictionary<int, string> _idToString;

        protected Tokenizer(Dictionary<string, int> stringToId, Dictionary<int, string> idToString)
        {
            _stringToId = stringToId;
            _idToString = idToString;
        }

        public abstract List<int> Encode(string text);

        public string Decode(List<int> ids)
        {
            var parts = new List<string>();
            foreach (int id in ids)
            {
                if (!_idToString.TryGetValue(id, out var token)) continue;
                if (token == "</s>" || token == "<unk>" || token == "<pad>") continue;
                parts.Add(token.Replace("▁", " "));
            }
            var result = string.Join("", parts);
            if (result.StartsWith(" ")) result = result.Substring(1);
            return result;
        }

        protected string PreTokenize(string text) => "▁" + text.Replace(" ", "▁");
    }

    internal class UnigramTokenizer : Tokenizer
    {
        private readonly Dictionary<string, double> _tokenScores;

        public UnigramTokenizer(Dictionary<string, int> stringToId, Dictionary<int, string> idToString, Dictionary<string, double> tokenScores)
            : base(stringToId, idToString)
        {
            _tokenScores = tokenScores;
        }

        public override List<int> Encode(string text)
        {
            var pretokenized = PreTokenize(text);
            int n = pretokenized.Length;
            var bestScore = new double[n + 1];
            var bestTokenLen = new int[n + 1];
            for (int i = 0; i <= n; i++) bestScore[i] = double.NegativeInfinity;
            bestScore[0] = 0;

            for (int pos = 0; pos < n; pos++)
            {
                if (bestScore[pos] == double.NegativeInfinity) continue;
                for (int len = 1; len <= Math.Min(32, n - pos); len++)
                {
                    var substr = pretokenized.Substring(pos, len);
                    if (_tokenScores.TryGetValue(substr, out double score))
                    {
                        double totalScore = bestScore[pos] + score;
                        if (totalScore > bestScore[pos + len])
                        {
                            bestScore[pos + len] = totalScore;
                            bestTokenLen[pos + len] = len;
                        }
                    }
                }
            }

            var tokens = new List<int>();
            int p = n;
            while (p > 0)
            {
                int len = bestTokenLen[p];
                var substr = pretokenized.Substring(p - len, len);
                tokens.Add(_stringToId.TryGetValue(substr, out int id) ? id : _stringToId["<unk>"]);
                p -= len;
            }
            tokens.Reverse();
            return tokens;
        }
    }

    internal class BPETokenizer : Tokenizer
    {
        private readonly Dictionary<string, int> _bpeVocab;
        private readonly List<(string, string)> _merges;

        public BPETokenizer(Dictionary<string, int> stringToId, Dictionary<int, string> idToString,
            Dictionary<string, int> bpeVocab, List<(string, string)> merges)
            : base(stringToId, idToString)
        {
            _bpeVocab = bpeVocab;
            _merges = merges;
        }

        public override List<int> Encode(string text)
        {
            var pretokenized = PreTokenize(text);
            var words = pretokenized.Split(' ');
            var allTokens = new List<int>();

            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word)) continue;
                var chars = word.Select(c => c.ToString()).ToList();

                foreach (var (left, right) in _merges)
                {
                    int i = 0;
                    while (i < chars.Count - 1)
                    {
                        if (chars[i] == left && chars[i + 1] == right)
                        {
                            chars[i] = left + right;
                            chars.RemoveAt(i + 1);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }

                foreach (var token in chars)
                {
                    if (_stringToId.TryGetValue(token, out int id))
                        allTokens.Add(id);
                    else if (_bpeVocab.TryGetValue(token, out int bpeId))
                        allTokens.Add(bpeId);
                    else
                        allTokens.Add(_stringToId["<unk>"]);
                }
            }

            return allTokens;
        }
    }
}
