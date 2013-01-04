// DotNetDataBot Framework 1.2 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetDataBot.Helpers
{
    /// <summary>
    /// Parser for Json data
    /// </summary>
    public class JsonParser
    {
        /// <summary>
        /// Returns the links in Json format.
        /// </summary>
        /// <param name="links">The links</param>
        /// <returns>Links in Json format.</returns>
        public string getJsonLinks(Dictionary<string, string> links)
        {
            string data = "\"sitelinks\":{";
            foreach (KeyValuePair<string, string> pair in links)
            {
                data += "\"" + pair.Key.Replace("-", "_") + "wiki\":{\"site\":\"" + pair.Key.Replace("-", "_") + "wiki\",\"title\":\"" + pair.Value + "\"},";
            }
            data = data.Remove(data.LastIndexOf(","));
            data += "}";
            return data;
        }

        /// <summary>
        /// Returns the labels in Json format.
        /// </summary>
        /// <param name="labels">The labels</param>
        /// <returns>Labels in Json format</returns>
        public string getJsonLabels(Dictionary<string, string> labels)
        {
            string data = "\"labels\":{";
            foreach (KeyValuePair<string, string> pair in labels)
            {
                data += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + pair.Value + "\"},";
            }
            data = data.Remove(data.LastIndexOf(","));
            data += "}";
            return data;
        }

        /// <summary>
        /// Returns the descriptions in Json format.
        /// </summary>
        /// <param name="descriptions">The descriptions</param>
        /// <returns>Descriptions in Json format</returns>
        public string getJsonDescriptions(Dictionary<string, string> descriptions)
        {
            string data = "\"descriptions\":{";
            foreach (KeyValuePair<string, string> pair in descriptions)
            {
                data += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + pair.Value + "\"},";
            }
            data = data.Remove(data.LastIndexOf(","));
            data += "}";
            return data;
        }

        /// <summary>
        /// Returns the aliases in Json format.
        /// </summary>
        /// <param name="aliases">The aliases</param>
        /// <returns>Aliases in Json format</returns>
        public string getJsonAliases(Dictionary<string, List<string>> aliases)
        {
            string data = "\"aliases\":{";
            foreach (KeyValuePair<string, List<string>> pair in aliases)
            {
                string aliasesData = "";
                foreach (string alias in pair.Value)
                {
                    aliasesData += alias + "|";
                }
                aliasesData = aliasesData.Remove(aliasesData.Length - 1);
                data += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + aliasesData + "\"},";
            }
            data = data.Remove(data.LastIndexOf(","));
            data += "}";
            return data;
        }
    }
}
