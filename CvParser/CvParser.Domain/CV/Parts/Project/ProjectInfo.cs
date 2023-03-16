namespace CvParser.Domain.CV.Parts.Project;

public class ProjectInfo
{
    public string Title { get; set; }

    public string Description { get; set; }

    public string PersonRoles { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IEnumerable<string>? Responsibilities { get; set; }

    public IEnumerable<string> Environment { get; set; }
}