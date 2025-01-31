namespace KokoroSharp;

/// <summary> Contains all available languages as per Kokoro v1.0. </summary>
/// <remarks> Each enum entry internally maps to a language-specific *char*, so voices can be name-filtered base on their first letter. </remarks>
public enum KokoroLanguage {
    AmericanEnglish = 'a',
    BritishEnglish = 'b',
    Japanese = 'j',
    MandarinChinese = 'z',
    Spanish = 'e',
    French = 'f',
    Hindi = 'h',
    Italian = 'i',
    BrazilianPortuguese = 'p'
}
