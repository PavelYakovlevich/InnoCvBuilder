using CvMatrix.Domain;
using CvParser.Domain.CV;

namespace CvMatrix;

public interface IMatrixBuilder
{
    Matrix Build(Cv cv);
}