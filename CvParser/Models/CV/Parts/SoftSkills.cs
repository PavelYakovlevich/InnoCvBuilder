using CvParser.Models.CV.Parts.Certificate;
using CvParser.Models.CV.Parts.Language;

namespace CvParser.Models.CV.Parts;

public class SoftSkills
{
    public ICollection<string>? Educations { get; set; }

    public ICollection<LanguageInfo>? Languages { get; set; }

    public ICollection<CerfiticateInfo>? Certifications { get; set; }

    public ICollection<string>? Domains { get; set; }
}