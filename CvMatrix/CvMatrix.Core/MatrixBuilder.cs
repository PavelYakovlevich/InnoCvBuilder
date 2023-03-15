using CvMatrix.Domain;
using CvParser.Domain.CV;

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
        foreach (var project in cv.Projects)
        {
            
        }

        return new Matrix();
    }
}