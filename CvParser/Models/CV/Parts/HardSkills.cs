namespace CvParser.Models.CV.Parts;

public class HardSkills
{
    public string Title { get; set; }

    public string Description { get; set; }
    
    public IEnumerable<string> ProgrammingLanguages { get; set; }

    public IEnumerable<string> ProgrammingTechnologies { get; set; }

    public IEnumerable<string> FrontendTechnologies { get; set; }
    
    public IEnumerable<string> Databases { get; set; }

    public IEnumerable<string> CloudTechnologies { get; set; }

    public IEnumerable<string> Other { get; set; }
}