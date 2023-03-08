using CvParser.Models.CV.Parts;
using CvParser.Models.CV.Parts.Project;

namespace CvParser.Models.CV;

public class Cv
{
    public string Person { get; init; }

    public string PersonPosition { get; set; }  

    public SoftSkills SoftSkills { get; set; }

    public HardSkills HardSkills { get; set; }

    public IEnumerable<ProjectInfo> Projects { get; set; }
}