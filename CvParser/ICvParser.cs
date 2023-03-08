using CvParser.Models.CV;

namespace CvParser;

public interface ICvParser
{
    Task<Cv> ParseAsync(string filePath);
}