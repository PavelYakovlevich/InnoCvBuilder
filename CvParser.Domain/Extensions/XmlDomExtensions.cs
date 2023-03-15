using System.Xml;

namespace CvParser.Domain.Extensions;

public static class XmlDomExtensions
{
    public static IEnumerable<XmlNode>? Where(this XmlNodeList? list, Func<XmlNode, bool> filter)
    {
        return list is null ? null : WhereCore();

        IEnumerable<XmlNode> WhereCore()
        {
            foreach (XmlNode node in list)
            {
                if (filter(node))
                {
                    yield return node;
                }
            }
        }
    }

    public static bool IsEmpty(this XmlNode node)
    {
        return string.IsNullOrWhiteSpace(node.InnerText) || string.IsNullOrEmpty(node.InnerText);
    }
}