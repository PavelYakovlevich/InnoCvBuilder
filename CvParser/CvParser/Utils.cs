using CvParser.Domain.CV.Parts.Language;

namespace CvParser;

public static class Utils
{
    public static LanguageLevel Parse(string value)
    {
        value = value.Trim().ToUpper();

        return value switch
        {
            "A1" => LanguageLevel.A1,
            "A2" => LanguageLevel.A2,
            "B2" => LanguageLevel.B2,
            "C1" => LanguageLevel.C1,
            "C2" => LanguageLevel.C2,
            _ => LanguageLevel.B1
        };
    }
}