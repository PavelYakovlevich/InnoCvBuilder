// See https://aka.ms/new-console-template for more information

using CvParser.Docx;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
                
var logger = loggerFactory.CreateLogger<Program>();

var parser = new DocxCvParser(logger);

var cv = await parser.ParseAsync(
    @"C:\Users\User\Downloads\7799 Pavel Yakovlevich (4+, .NET, Angular, Azure, Networks administration).docx");

var t = 1;