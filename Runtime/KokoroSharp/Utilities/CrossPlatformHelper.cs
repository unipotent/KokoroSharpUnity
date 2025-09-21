
using KokoroSharp.Core;
using KokoroSharp.Processing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using KokoroSharpUnity;

namespace KokoroSharp.Utilities
{
    /// <summary> Contains functionality regarding cross-platform compatibility, like providing the path to the appropriate binaries, and setting up the correct audio player. </summary>
    /// <remarks> All platform-specific functionality splits will go through this class. </remarks>
    public static class CrossPlatformHelper
    {

        /// <summary> Retrieves the path for the appropriate espeak-ng binaries based on the platform and architecture. </summary>
        /// <remarks> In case there was no matching platform/architecture combo found for the running system, will fallback to "espeak-ng". </remarks>
        public static string GetEspeakBinariesPath()
        {
      
            // Otherwise, build the path to the binary based on PC's specs.
            var espeak_cli_path = @$"{Tokenizer.eSpeakNGPath}/espeak-ng-";
#if UNITY_STANDALONE_WIN
            espeak_cli_path += "win-";
#elif UNITY_STANDALONE_LINUX
espeak_cli_path += "linux-";
#elif UNITY_STANDALONE_OSX
espeak_cli_path += "macos-";
#else
#error Unsupported platform
#endif

            espeak_cli_path += (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64.dll" : "amd64.dll");

            return File.Exists(espeak_cli_path) ? espeak_cli_path : "espeak-ng"; // In case developers did not include the espeak folder at all.
        }

        /// <summary> Retrieves the appropriate audio player for the running system: <b>NAudio.WaveOutEvent wrapper</b> for Windows, or <b>AL wrapper</b> for other OS. </summary>
        public static KokoroWaveOutEvent GetAudioPlayer()
        {
            
            return new KokoroWaveOutEventUnity(); // Just use Unity audio sources
        }
    }
}
