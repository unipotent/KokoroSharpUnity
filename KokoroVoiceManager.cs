namespace KokoroSharp;

using NumSharp;

/// <summary> Helper module responsible for holding Kokoro Voices and making them accessible for retrieval by name. </summary>
/// <remarks> Also contains methods that allows mixing voices with each other to create new voices with shared characteristics. </remarks>
public static class KokoroVoiceManager {
    public static List<KokoroVoice> Voices { get; } = new();

    public static void LoadVoicesFromPath(string voicesPath, IEnumerable<KokoroLanguage> languages = null) {
        IEnumerable<string> files = Directory.GetFiles(voicesPath);
        if (languages != null) { files = files.Where(x => languages.Any(l => x.StartsWith(l.AsString()))); }

        foreach (var filePath in files) {
            var voiceName = Path.GetFileNameWithoutExtension(filePath);
            if (Voices.Any(x => x.Name == voiceName)) { continue; }

            var voiceFeatures = np.Load<float[,,]>(filePath);
            Voices.Add(new() { Name = voiceName, Features = voiceFeatures });
        }
    }

    /// <summary> Retrieves a registered voice by name. </summary>
    /// <remarks> Customly mixed voices will not be considered unless named and added to <see cref="Voices"/>. </remarks>
    public static KokoroVoice GetVoice(string name) => Voices.FirstOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));



    /// <summary>
    /// <para> Mixes the given voices together with their weights, to produce a new voice. There is no limit on how many voices this can accept. </para>
    /// <para> The *weight* for each voice will determine its contribution to the output voice, which will be normalized based on the sum of all weights. </para>
    /// <para> The formula that's used is <b>NewVoice = (λa * A) + (λb * B) + ... + (λn * N)</b>, where <b>λ</b> are the normalized weights. </para>
    /// </summary>
    public static KokoroVoice Mix(params (KokoroVoice voice, float weight)[] voices) {
        var f = voices[0].voice.Features;
        var (w, h, d) = (f.GetLength(0), f.GetLength(1), f.GetLength(2));

        var totalWeight = voices.Sum(x => x.weight);
        var weights = voices.Select(x => x.weight / totalWeight).ToArray();

        var newArray = np.zeros_like(voices[0].voice.Features);
        for (int i = 0; i < voices.Length; i++) { newArray += np.array(voices[i].voice.Features) * weights[i]; }
        var newFeatures = newArray.reshape(new Shape(w, h, d)).ToMuliDimArray<float>() as float[,,];
        return new KokoroVoice() { Name = "", Features = newFeatures };
    }

    /// <summary> Mixes the two voices with formula <b>NewVoice = (λa * A) + (λb * B)</b>, where <b>λ</b> are the normalized weights. </summary>
    /// <remarks> For more fine-grained control and mixing multiple voices in one go, see <b>KokoroVoiceManager.Mix(..)</b>. </remarks>
    public static KokoroVoice MixWith(this KokoroVoice A, KokoroVoice B, float λa = 0.5f, float λb = 0.5f) => Mix([(A, λb), (B, λb)]);

    /// <summary> </summary>
    public static string AsString(this KokoroLanguage lang) => ((char) lang).ToString();
}