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
        public string getJsonAliases(Dictionary<string, string> aliases)
        {
            string data = "\"aliases\":{";
            foreach (KeyValuePair<string, string> pair in aliases)
            {
                data += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + pair.Value + "\"},";
            }
            data = data.Remove(data.LastIndexOf(","));
            data += "}";
            return data;
        }
    }
}
