namespace CvMatrix.Domain;

public class Matrix
{
    public IDictionary<string, IReadOnlyCollection<SkillUsageInfo>> Skills { get; }
}