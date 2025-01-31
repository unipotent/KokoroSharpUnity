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

/// <summary> Used to name-filter voices based on their second letter, which represents the speaker's gender. </summary>
/// <remarks> For example a voice of an <b>AmericanEnglish Male</b> speaker will start with <b>`am_`</b> whereas a <b>Hindi Female</b>'s will start with <b>`hf_`</b>. </remarks>
public enum KokoroGender { Both, Male = 'm', Female = 'f' }
