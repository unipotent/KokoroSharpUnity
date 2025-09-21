This is an adapted version of KokoroSharp that updates the C# 9+ syntax and uses a KokoroWaveOutEvent for Unity audio sources to make it usable in Unity.
This package includes the eSpeak NG dlls from `https://github.com/Lyrcaxis/KokoroSharpBinaries`, where you can also get the voices by running the [download script](https://github.com/Lyrcaxis/KokoroSharpBinaries/blob/main/convert_kokoro_voices.py).
The voices are expected to be in Assets/StreamingAssets/Kokoro/voices.

You can install this package by:
1. Opening your Unity project
2. Opening the Package Manager (Window > Package Manager)
3. Click the "+" button in the top-left
4. Select "Add package from git URL..."
5. Enter the repository URL: `https://github.com/unipotent/KokoroSharpUnity.git`
6. Click "Add"

## Dependencies
This package requires the package [com.github.asus4.onnxruntime](https://github.com/asus4/onnxruntime-unity/tree/main)
Be sure to follow the installation instructions from their readme.

## Usage
- Add voices as mentioned above
- Add the KokoroUnity component to a GameObject, and drag in your audio source.

Example code:
```csharp
using KokoroSharp;
using KokoroSharp.Core;

void Start()
{
    KokoroTTS kokoroTTS = KokoroTTS.LoadModel();
    KokoroVoice voice = KokoroVoiceManager.GetVoice("af_heart");
    kokoroTTS.SpeakFast("Hello World!", voice);
}
```

Tested on Unity 6, Windows 11 on CPU 

## License
- This package is provided under the [MIT LICENSE](LICENSE).
- KokoroSharp is licensed under the [MIT License](https://github.com/Lyrcaxis/KokoroSharp/blob/main/LICENSE).
- The [Kokoro 82M model](https://huggingface.co/hexgrad/Kokoro-82M) and its voices are released under the [Apache License](https://huggingface.co/datasets/choosealicense/licenses/blob/main/markdown/apache-2.0.md).
- eSpeak NG is licensed under the [GPLv3 License](https://github.com/espeak-ng/espeak-ng/blob/master/COPYING).
- NumSharp is licensed under the [Apache 2.0 License](https://github.com/SciSharp/NumSharp/blob/master/LICENSE).
- NAudio is licensed under the [MIT Licnese](https://github.com/naudio/NAudio/blob/master/license.txt).

