using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace KokoroSharp;

public static class Tokenizer {
    public static Dictionary<char, int> Vocab { get; } = GetVocab();

    // If any isn't present in the vocab, it'll be discarded later anyway.
    static HashSet<char> punctuation = ['.', ',', '?', '!', ';', ':', '-', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\', '&', '@', '#', '%', '$', '*', '~', '`', '<', '>', '|', '^', '_'];
    static HashSet<char> noSpacePunc = ['\'', '-', '\"', '(', ')', '[', ']', '{', '}', '/', '\\', '&', '@', '#', '%', '$', '*', '~', '`', '<', '>', '|', '^', '_'];


    /// <summary> Converts the input text to phoneme tokens, directly usable by Kokoro. </summary>
    public static int[] Tokenize(string inputText) => Phonemize(inputText).Select(x => Vocab[x]).ToArray();

    static string Phonemize(string inputText) {
        var preprocessedText = PreprocessText(inputText);
        var phonemeList = Phonemize_Internal(preprocessedText).Split('\n');
        return PostProcessPhonemes(inputText, phonemeList);
    }

    static string Phonemize_Internal(string text) {
        var process = new Process() {
            StartInfo = new ProcessStartInfo() {
                FileName = "espeak-ng",
                Arguments = $"--ipa=3 -q -stress -v en-us \"{text}\"",
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
        process.Start();
        var phonemeList = process.StandardOutput.ReadToEnd();
        process.StandardOutput.Close();

        return phonemeList.Replace("\r\n", "\n").Trim();
    }

    /// <summary> Normalizes the input text to what Kokoro would expect to see, preparing it for phonemization. </summary>
    static string PreprocessText(string text) {
        text = text.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace("«", "\u201C").Replace("»", "\u201D").Replace('\u201C', '"').Replace('\u201D', '"');
        foreach (var (a, b) in new[] { ("、", ", "), ("。", ". "), ("！", "! "), ("，", ", "), ("：", ": "), ("；", "; "), ("？", "? ") }) { text = text.Replace(a, b); }

        text = Regex.Replace(text, @"\bD[Rr]\.(?= [A-Z])", "Doctor");
        text = Regex.Replace(text, @"\b(Mr|MR)\.(?= [A-Z])", "Mister");
        text = Regex.Replace(text, @"\b(Ms|MS)\.(?= [A-Z])", "Miss");
        text = Regex.Replace(text, @"\x20{2,}", " ");

        text = Regex.Replace(text, @"(?<!\:)\b([1-9]|1[0-2]):([0-5]\d)\b(?!\:)", m => $"{m.Groups[1].Value} {m.Groups[2].Value}");
        text = Regex.Replace(text, @"[$£]\d+(?:\.\d+)?", FlipMoneyMatch);
        text = Regex.Replace(text, @"\d+\.\d+", PointNumMatch);

        return text.Trim();



        static string FlipMoneyMatch(Match m) {
            var value = m.Value[1..];
            var currency = m.Value[0] == '$' ? "dollar" : "pound";
            return value.Contains('.') ? $"{value.Replace(".", " ")} {currency}s"
                 : value.EndsWith('1') ? $"{value} {currency}"
                 : $"{value} {currency}s";
        }

        static string PointNumMatch(Match m) {
            var parts = m.Value.Split('.');
            return $"{parts[0]} point {string.Join(" ", parts[1].ToCharArray())}";
        }
    }

    /// <summary> Post-processes the phonemes to Kokoro's specs, preparing them for tokenization. </summary>
    /// <remarks> We also use the initial text to restore the punctuation. </remarks>
    static string PostProcessPhonemes(string initialText, string[] phonemesArray, string lang = "en-us") {
        var puncs = new List<char>();
        foreach (var c in initialText) { if (punctuation.Contains(c)) { puncs.Add(c); } }

        var sb = new StringBuilder();
        for (int i = 0; i < phonemesArray.Length; i++) {
            sb.Append(phonemesArray[i]);
            sb.Append(puncs[i]);
            if (!noSpacePunc.Contains(puncs[i])) { sb.Append(' '); }
        }
        var phonemes = sb.ToString().Trim();

        phonemes = phonemes.Replace("ʲ", "j").Replace("r", "ɹ").Replace("x", "k").Replace("ɬ", "l");
        if (lang == "en-us") { phonemes = Regex.Replace(phonemes, @"(?<=nˈaɪn)ti(?!ː)", "di"); }
        for (int i = 0; i < 5; i++) { phonemes = phonemes.Replace("  ", " "); }
        return new string(phonemes.Where(Vocab.ContainsKey).ToArray());
    }

    static Dictionary<char, int> GetVocab() {
        var symbols = new List<char>();
        symbols.Add('$'); // pad token
        symbols.AddRange(";:,.!?¡¿—…\"«»“” ".ToCharArray());
        symbols.AddRange("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray());
        symbols.AddRange("ɑɐɒæɓʙβɔɕçɗɖðʤəɘɚɛɜɝɞɟʄɡɠɢʛɦɧħɥʜɨɪʝɭɬɫɮʟɱɯɰŋɳɲɴøɵɸθœɶʘɹɺɾɻʀʁɽʂʃʈʧʉʊʋⱱʌɣɤʍχʎʏʑʐʒʔʡʕʢǀǁǂǃˈˌːˑʼʴʰʱʲʷˠˤ˞↓↑→↗↘'̩'ᵻ".ToCharArray());

        var dict = new Dictionary<char, int>();
        for (int i = 0; i < symbols.Count; i++) { dict[symbols[i]] = i; }
        return dict;
    }
}