using CvParser.Domain.CV.Parts;
using CvParser.Domain.CV.Parts.Project;

namespace CvParser.Domain.CV;

public class Cv
{
    public string Person { get; init; }

    public string PersonPosition { get; set; }  

    public SoftSkills SoftSkills { get; set; }

    public HardSkills HardSkills { get; set; }

    public IEnumerable<ProjectInfo> Projects { get; set; }
}