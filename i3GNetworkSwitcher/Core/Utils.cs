using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace i3GNetworkSwitcher.Core
{
    internal static class Utils
    {
        public static HtmlNode GetFirstElementChild(this HtmlNode node)
        {
            return node.ChildNodes.Where(x => x.NodeType == HtmlNodeType.Element).FirstOrDefault();
        }

        public static HtmlNode[] GetElementChildren(this HtmlNode node)
        {
            return node.ChildNodes.Where(x => x.NodeType == HtmlNodeType.Element).ToArray();
        }

        public static string GetRequiredAttribute(this HtmlNode node, string name)
        {
            string value = node.GetAttributeValue(name, null);
            if (value == null)
                throw new Exception($"Required attribute \"{name}\" didn't exist on node.");
            return value;
        }
    }
}
