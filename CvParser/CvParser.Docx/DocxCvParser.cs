using System.IO.Compression;
using System.Xml;
using CvParser.Domain.CV;
using CvParser.Domain.CV.Parts;
using CvParser.Domain.CV.Parts.Certificate;
using CvParser.Domain.CV.Parts.Language;
using CvParser.Domain.CV.Parts.Project;
using CvParser.Domain.Exceptions;
using CvParser.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace CvParser.Docx;

public class DocxCvParser : ICvParser
{
    private readonly ILogger? _logger;

    private static readonly (string sectionName, Action<SoftSkills, string> setter)[] SoftSkillsSetters =
        new (string sectionName, Action<SoftSkills, string>)[]
        {
            ("education", (skills, value) =>
            {
                skills.Educations ??= new List<string>();
                skills.Educations.Add(value);
            }),
            ("language proficiency", (skills, value) =>
            {
                var languageInfoParts = value.Split('—', '-');

                if (languageInfoParts.Length != 2)
                {
                    return;
                }
                
                skills.Languages ??= new List<LanguageInfo>();

                skills.Languages.Add(new LanguageInfo
                {
                    Name = languageInfoParts[0],
                    Level = Utils.Parse(languageInfoParts[1]),
                });

            }),
            ("certifications", (skills, value) =>
            {
                skills.Certifications ??= new List<CertificateInfo>();

                skills.Certifications.Add(new CertificateInfo
                {
                    Name = value
                });
            }),
            ("domains", (skills, value) =>
            {
                skills.Domains ??= new List<string>();

                skills.Domains.Add(value);
            }),
        };

    public DocxCvParser(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<Cv> ParseAsync(string filePath)
    {
        try
        {
            var documentXmlPath = UnzipDocxFile(filePath);

            var xmlDoc = ReadCvFile(documentXmlPath);

            var cv = ParseCvCore(xmlDoc);

            return cv;
        }
        catch (Exception e)
        {
            _logger?.LogError("Cv parsing failed with the message: {message}, {stackTrace}",
                e.Message, e.StackTrace);

            throw new CvParseException($"Cv parsing failed with the message: {e.Message}", e);
        }
    }

    private string UnzipDocxFile(string docxFilePath)
    {
        var fileName = Path.GetFileName(docxFilePath);

        var fileLocationDirectory = Path.GetDirectoryName(docxFilePath)!;

        var createdDirectory = CreateDirectory();

        ZipFile.ExtractToDirectory(docxFilePath, createdDirectory.FullName);

        return Path.Combine(createdDirectory.FullName, "word", "document.xml");

        static string RemoveExtension(string fileName)
        {
            if (!Path.HasExtension(fileName))
            {
                return fileName;
            }

            return fileName[..fileName.LastIndexOf('.')];
        }

        DirectoryInfo CreateDirectory()
        {
            var cvRootDirectoryName = Path.Combine(fileLocationDirectory, RemoveExtension(fileName));

            if (!Directory.Exists(cvRootDirectoryName))
            {
                Directory.CreateDirectory(cvRootDirectoryName);
            }

            return Directory.CreateDirectory(Path.Combine(cvRootDirectoryName,
                DateTime.Now.ToString("yy-MM-dd h_mm_ss")));
        }
    }

    private Cv ParseCvCore(XmlDocument xmlDoc)
    {
        _logger?.LogInformation("Start parsing cv xml");

        var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

        var body = xmlDoc.DocumentElement!.FirstChild;

        var personName = ParsePersonName(body!);

        var personPosition = ParsePersonPosition(body!);

        var softSkills = ParseSoftSkills(body!, namespaceManager);

        var hardSkills = ParseHardSkills(body!, namespaceManager);

        var projects = ParseProjects(body!, namespaceManager);

        return new Cv
        {
            Person = personName!,
            PersonPosition = personPosition!,
            SoftSkills = softSkills,
            HardSkills = hardSkills,
            Projects = projects
        };
    }

    private IEnumerable<ProjectInfo> ParseProjects(XmlNode body, XmlNamespaceManager namespaceManager)
    {
        var projectsHeader = FindNode(body, "Projects")
                             ?? throw new CvParseException("Missing 'Projects' header");

        var projectsTableNode = projectsHeader.NextSibling!;

        _logger?.LogInformation("Start parsing projects");

        var projectNodes = projectsTableNode.SelectNodes("w:tr", namespaceManager)!
            .Where(node => !node.IsEmpty())!
            .ToArray();
        
        _logger?.LogInformation("{count} projects were found", projectNodes.Length);

        var currentProjectIndex = 0;
        foreach (var projectNode in projectNodes)
        {
            _logger?.LogInformation("Parsing {index} project:", currentProjectIndex++);

            var project = ParseProject(projectNode, namespaceManager);

            _logger?.LogInformation("Parsed project: @{project}", project);

            yield return project;
        }
    }

    private ProjectInfo ParseProject(XmlNode tableRow, XmlNamespaceManager namespaceManager)
    {
        var title = ParseTitle();

        var description = ParseDescription();

        var projectDetailsColumn = tableRow.ChildNodes[2]!;

        var roles = ParseProjectRoles(projectDetailsColumn);

        var (startDate, endDate) = ParsePeriod(projectDetailsColumn);

        var responsibilities = ParseResponsibilities(projectDetailsColumn);

        var usedTechnologies = ParseEnvironment(projectDetailsColumn);

        return new ProjectInfo
        {
            Title = title,
            Description = description,
            PersonRoles = roles,
            StartDate = startDate,
            EndDate = endDate,
            Responsibilities = responsibilities,
            Environment = usedTechnologies,
        };

        string ParseTitle()
        {
            const int projectTitleColumnNodeIndex = 1;

            var projectTitleColumn = tableRow.ChildNodes[projectTitleColumnNodeIndex]!;

            const int projectTitleParagraphNodeIndex = 1;
            var titleParagraphNode = projectTitleColumn.ChildNodes[projectTitleParagraphNodeIndex];

            var titleValue = ReadParagraphText(titleParagraphNode);

            _logger?.LogInformation("Project title: {title}", titleValue);

            return titleValue;
        }

        string ParseDescription()
        {
            const int projectDescriptionColumnNodeIndex = 1;

            var projectTitleColumn = tableRow.ChildNodes[projectDescriptionColumnNodeIndex]!;

            const int projectDescriptionParagraphNodeIndex = 2;
            var descriptionParagraphNode = projectTitleColumn.ChildNodes[projectDescriptionParagraphNodeIndex];

            var descriptionValue = ReadParagraphText(descriptionParagraphNode);

            _logger?.LogInformation("Project description: {description}", descriptionValue);

            return descriptionValue;
        }

        string ParseProjectRoles(XmlNode detailsColumn) =>
            ParseSectionValue(detailsColumn, "Project roles");

        (DateTime startDate, DateTime endDate) ParsePeriod(XmlNode detailsColumn)
        {
            var period = ParseSectionValue(detailsColumn, "Period");

            var periodParts = period.Split('–', '-',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!DateTime.TryParse(periodParts[0], out var startDate) ||
                !DateTime.TryParse(periodParts[1], out var endDate))
            {
                throw new CvParseException($"Invalid period definition: {period}");
            }

            return (startDate, endDate);
        }

        IEnumerable<string> ParseResponsibilities(XmlNode detailsColumn)
        {
            var listStartNode = FindSectionValues(detailsColumn,
                "Responsibilities & achievements", "Responsibilities");

            return ParseList(listStartNode, namespaceManager)
                .Select(value => value.Trim(' ', ';', '.'));
        }

        string ParseSectionValue(XmlNode detailsColumn, string label)
        {
            var sectionParagraph = FindSectionValues(detailsColumn, label);

            return ReadParagraphText(sectionParagraph);
        }

        IEnumerable<string> ParseEnvironment(XmlNode detailsColumn)
        {
            var environmentTableNode = FindSectionValues(detailsColumn, "Environment");

            return GetTableContent(environmentTableNode, namespaceManager);
        }
    }

    private static XmlNode? FindSectionValues(XmlNode location, params string[] sectionNames)
    {
        foreach (var name in sectionNames)
        {
            var labelNode = FindNode(location, name);

            if (labelNode is not null)
            {
                return labelNode.NextSibling ?? throw new CvParseException($"Missing '{name}' values");
            }
        }

        return null;
    }

    private IEnumerable<string> ParseList(XmlNode listStartNode, XmlNamespaceManager namespaceManager)
    {
        var currentParagraphNode = listStartNode;

        while (!IsSectionHeader(GetFirstParagraphRow(currentParagraphNode), namespaceManager))
        {
            var responsibility = ReadParagraphText(currentParagraphNode);

            yield return responsibility;

            currentParagraphNode = currentParagraphNode.NextSibling!;
        }

        XmlNode GetFirstParagraphRow(XmlNode paragraph)
        {
            return paragraph.SelectNodes("w:r", namespaceManager)![0]!;
        }
    }

    private HardSkills ParseHardSkills(XmlNode body, XmlNamespaceManager namespaceManager)
    {
        var hardSkillsSection = GetHardSkillsSection();

        _logger?.LogInformation("Start reading hard skills section");

        var title = ParseTitle();

        var description = ParseDescription();

        var programmingLanguages = ParseProgrammingLanguages();

        var programmingTechnologies = ParseProgrammingTechnologies();

        var frontendTechnologies = ParseFrontendTechnologies();

        var databases = ParseDatabases();

        var cloudTechnologies = ParseCloudTechnologies();

        var other = ParseOther();

        return new HardSkills
        {
            Title = title,
            Description = description,
            ProgrammingLanguages = programmingLanguages,
            ProgrammingTechnologies = programmingTechnologies,
            FrontendTechnologies = frontendTechnologies,
            Databases = databases,
            CloudTechnologies = cloudTechnologies,
            Other = other
        };

        XmlNode GetHardSkillsSection()
        {
            const int personSkillsTableNodeIndex = 2;
            var skillsTable = body.ChildNodes[personSkillsTableNodeIndex]!;

            const int personSkillsTableContentNodeIndex = 2;
            var skillsTableContent = skillsTable.ChildNodes[personSkillsTableContentNodeIndex]!;

            const int personSkillsTableContentValuesNodeIndex = 2;
            var personSkillsTableContentValues = skillsTableContent.ChildNodes[personSkillsTableContentValuesNodeIndex];

            return personSkillsTableContentValues ?? throw new CvParseException("Missing hard skills column");
        }

        string ParseTitle()
        {
            const int titleNodeIndex = 1;

            var value = ParseTableParagraph(hardSkillsSection, titleNodeIndex);

            _logger?.LogInformation("Title: {title}", value);

            return value;
        }

        string ParseDescription()
        {
            const int descriptionNodeIndex = 2;

            var value = ParseTableParagraph(hardSkillsSection, descriptionNodeIndex);

            _logger?.LogInformation("Description: {title}", value);

            return value;
        }

        IEnumerable<string> ParseProgrammingLanguages()
        {
            var programmingLanguagesNode = FindSectionValues(hardSkillsSection,
                "Programming languages");

            return GetRowEnumerationValues(programmingLanguagesNode);
        }

        IEnumerable<string> ParseProgrammingTechnologies()
        {
            var programmingTechnologiesTable = FindSectionValues(hardSkillsSection,
                "Programming technologies")!;

            return GetTableContent(programmingTechnologiesTable, namespaceManager);
        }

        IEnumerable<string> ParseFrontendTechnologies()
        {
            var frontendNode = FindSectionValues(hardSkillsSection,
                "Frontend technologies", "Frontend")!;

            return GetRowEnumerationValues(frontendNode);
        }

        IEnumerable<string> ParseDatabases()
        {
            var databasesNode = FindSectionValues(hardSkillsSection,
                "Database management systems", "Databases")!;

            return GetRowEnumerationValues(databasesNode);
        }

        IEnumerable<string>? ParseCloudTechnologies()
        {
            var cloudTechnologiesTable = FindSectionValues(hardSkillsSection,
                "Cloud technologies", "Cloud");

            return cloudTechnologiesTable is null ? null : GetTableContent(cloudTechnologiesTable, namespaceManager);
        }

        IEnumerable<string>? ParseOther()
        {
            var otherTechnologiesNode = FindSectionValues(hardSkillsSection, "Other");

            return otherTechnologiesNode is null ? null : GetRowEnumerationValues(otherTechnologiesNode);
        }
    }

    private static IEnumerable<string> GetRowEnumerationValues(XmlNode node)
    {
        return (node?.InnerText ?? "").Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private IEnumerable<string> GetTableContent(XmlNode programmingTechnologiesTable,
        XmlNamespaceManager namespaceManager)
    {
        var tableContent = GetTableContentNode(programmingTechnologiesTable);

        var columns = tableContent.SelectNodes("w:tc", namespaceManager)!;

        _logger?.LogInformation("Cols in table count: {count}", columns.Count);

        var columnIndex = 0;
        foreach (XmlNode column in columns)
        {
            var paragraphs = column.SelectNodes("w:p", namespaceManager)!;

            _logger?.LogInformation("Rows in column[{index}]: {count}", columnIndex, columns.Count);

            foreach (XmlNode paragraph in paragraphs)
            {
                yield return ReadParagraphText(paragraph);
            }

            columnIndex++;
        }
    }

    private static XmlNode GetTableContentNode(XmlNode table)
    {
        const int tableContentNodeIndex = 2;

        return table.ChildNodes[tableContentNodeIndex]!;
    }

    private SoftSkills ParseSoftSkills(XmlNode body, XmlNamespaceManager xmlNamespaceManager)
    {
        var softSkillsSections = GetSoftSkillsContentTableNodes(body, xmlNamespaceManager);

        var softSkills = new SoftSkills();

        _logger?.LogInformation("Start reading soft skills section");

        Action<SoftSkills, string>? currentSetter = null;
        foreach (var currentSoftSkillNode in softSkillsSections!)
        {
            var nodeValue = ReadParagraphText(currentSoftSkillNode);

            if (IsSectionHeader(currentSoftSkillNode, xmlNamespaceManager))
            {
                var setterIndex = Array.FindIndex(SoftSkillsSetters,
                    setterInfo => setterInfo.sectionName.Equals(nodeValue.ToLower()));

                if (setterIndex == -1)
                {
                    var errorMessage = $"Setter for {nodeValue} section was not found.";
                    _logger?.LogError(errorMessage);
                    throw new CvParseException(errorMessage);
                }

                currentSetter = SoftSkillsSetters[setterIndex].setter;
                _logger?.LogInformation("Section {name}:", nodeValue);
            }
            else
            {
                currentSetter?.Invoke(softSkills, nodeValue);
                _logger?.LogInformation(" - : {value}", nodeValue);
            }
        }

        return softSkills;

        static IEnumerable<XmlNode>? GetSoftSkillsContentTableNodes(XmlNode body,
            XmlNamespaceManager xmlNamespaceManager)
        {
            const int personSkillsTableNodeIndex = 2;
            var skillsTable = body.ChildNodes[personSkillsTableNodeIndex]!;

            var skillsTableContent = GetTableContentNode(skillsTable);

            const int personSkillsTableContentValuesNodeIndex = 1;
            var personSkillsTableContentValues =
                skillsTableContent.ChildNodes[personSkillsTableContentValuesNodeIndex]!;

            foreach (XmlNode paragraph in personSkillsTableContentValues.SelectNodes("w:p", xmlNamespaceManager)!)
            {
                foreach (XmlNode paragraphNode in paragraph.SelectNodes("w:r", xmlNamespaceManager)!)
                {
                    yield return paragraphNode;
                }
            }
        }
    }

    private static bool IsSectionHeader(XmlNode paragraphRowNode, XmlNamespaceManager xmlNamespaceManager)
    {
        var boldTextStyleNode = paragraphRowNode.FirstChild?.SelectNodes("w:b", xmlNamespaceManager)?[0] ?? null;

        if (boldTextStyleNode is null)
        {
            return false;
        }

        var attribute = boldTextStyleNode?.Attributes?[0].Value ?? "";

        return attribute == "1";
    }

    private string ParsePersonPosition(XmlNode body)
    {
        const int personPositionNodeIndex = 1;
        var paragraph = body.ChildNodes[personPositionNodeIndex];

        var position = ReadParagraphText(paragraph);

        _logger?.LogInformation("Person position: {position}", position);

        return position ?? string.Empty;
    }

    private string ParsePersonName(XmlNode body)
    {
        var paragraph = body.FirstChild;

        var credentials = ReadParagraphText(paragraph);

        _logger?.LogInformation("Person: {credentials}", credentials);

        return credentials;
    }

    private XmlDocument ReadCvFile(string filePath)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);

        _logger?.LogInformation("Cv file '{file}' was opened", filePath);

        return xmlDoc;
    }

    private static string ReadParagraphText(XmlNode? paragraphNode)
    {
        return paragraphNode?.InnerText ?? string.Empty;
    }

    private static XmlNode? FindNode(XmlNode node, string name)
    {
        name = name.Trim().ToLower();

        return FindNodeIndexCore(node, name);

        static XmlNode? FindNodeIndexCore(XmlNode xmlNode, string name)
        {
            if (xmlNode.ChildNodes.Count == 0)
            {
                return null;
            }

            XmlNode? result = null;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.InnerText.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    return childNode;
                }

                result ??= FindNodeIndexCore(childNode, name);
            }

            return result;
        }
    }

    private static string ParseTableParagraph(XmlNode table, int index)
    {
        var node = table.ChildNodes[index];

        return ReadParagraphText(node);
    }
}