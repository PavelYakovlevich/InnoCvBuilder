using CvParser.Docx;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
                
var logger = loggerFactory.CreateLogger<DocxCvParser>();

var parser = new DocxCvParser(logger);

const string filePath = @"C:\Users\User\Downloads\7986 Dechko Stanislav (.NET, Angular, PowerShell, Azure, AWS).docx";

var cv = await parser.ParseAsync(filePath);

//
// await using var stream = new FileStream(Path.Combine(Path.GetDirectoryName(filePath)!, "dump.json"), FileMode.Create);
//
// await using var streamWriter = new StreamWriter(stream);
//
// await streamWriter.WriteAsync(JsonConvert.SerializeObject(cv, Formatting.Indented));