using CvMatrix.Core;
using CvMatrix.Domain;
using CvParser.Docx;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
                
var logger = loggerFactory.CreateLogger<DocxCvParser>();

var parser = new DocxCvParser(logger);

const string filePath = @"C:\Users\User\Downloads\8009 Naumov Victor (.NET, Angular, Azure, DevOps).docx";

var cv = await parser.ParseAsync(filePath);

var matrixBuilder = new MatrixBuilder(new MatrixSkillsConfiguration());

var matrix = matrixBuilder.Build(cv);

await using var stream = new FileStream(Path.Combine(Path.GetDirectoryName(filePath)!, "dump.json"), FileMode.Create);

await using var streamWriter = new StreamWriter(stream);

await streamWriter.WriteAsync(JsonConvert.SerializeObject(matrix, Formatting.Indented));