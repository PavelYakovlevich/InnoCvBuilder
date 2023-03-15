using CvParser.Domain.CV;

namespace CvParser;

public interface ICvParser
{
    Task<Cv> ParseAsync(string filePath);
}