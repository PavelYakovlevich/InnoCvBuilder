using CvParser.Domain.CV.Parts.Certificate;
using CvParser.Domain.CV.Parts.Language;

namespace CvParser.Domain.CV.Parts;

public class SoftSkills
{
    public ICollection<string>? Educations { get; set; }

    public ICollection<LanguageInfo>? Languages { get; set; }

    public ICollection<CertificateInfo>? Certifications { get; set; }

    public ICollection<string>? Domains { get; set; }
}