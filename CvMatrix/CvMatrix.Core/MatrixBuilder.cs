using CvMatrix.Domain;
using CvParser.Domain.CV;
using CvParser.Domain.CV.Parts.Project;

namespace CvMatrix.Core;

public class MatrixBuilder : IMatrixBuilder
{
    private readonly MatrixSkillsConfiguration _configuration;

    public MatrixBuilder(MatrixSkillsConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public Matrix Build(Cv cv)
    {
        var skills = new Dictionary<string, SkillUsageInfo>();
        
        foreach (var project in cv.Projects)
        {
            HandleProject(project);
        }
        
        foreach (var (_, skillUsageInfo) in skills)
        {
            skillUsageInfo.Experience = (float) Math.Round(skillUsageInfo.Experience / 12, 1);
        }
        
        return new Matrix
        {
            Skills = skills
        };

        void HandleProject(ProjectInfo project)
        {
            var projectDurationInMonth = (project.EndDate.Year * 12 + project.EndDate.Month) -
                                         (project.StartDate.Year * 12 + project.StartDate.Month);

            foreach (var technology in project.Environment)
            {
                HandleTechnology(technology, projectDurationInMonth, project.EndDate.Year);
            }
        }

        void HandleTechnology(string name, float periodInMonth, int lastUsageYear)
        {
            name = name.Trim().ToLower();

            if (!skills.TryAdd(name, new SkillUsageInfo
                {
                    Name = name,
                    LastUsageYear = lastUsageYear,
                    Experience = periodInMonth
                }))
            {
                skills[name].Experience += periodInMonth;
            }
        }
    }
}