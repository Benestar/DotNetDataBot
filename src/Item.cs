﻿// DotNetDataBot Framework 1.0 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

using System;
using System.Collections.Generic;
using System.Text;
using DotNetDataBot;
using System.Web;
using System.Xml;
using System.IO;
using DotNetDataBot.Exceptions;
using DotNetDataBot.Helpers;
using System.Text.RegularExpressions;

namespace DotNetDataBot
{
    /// <summary>
    /// Class for items
    /// </summary>
    public class Item
    {
        #region variables

        //http://www.mediawiki.org/wiki/Extension:Wikibase/API

        /// <summary>The site of the item</summary>
        public Site site { get; set; }

        /// <summary>The id of the item</summary>
        public int id { get; set; }

        /// <summary>The id of the item</summary>
        public Dictionary<string, string> links { get; private set; }

        /// <summary>The id of the item</summary>
        public Dictionary<string, string> labels { get; private set; }

        /// <summary>The id of the item</summary>
        public Dictionary<string, string> descriptions { get; private set; }

        private string editSessionToken;

        private string editSessionTime;

        private JsonParser JsonParser = new JsonParser();

        #endregion

        #region construction

        /// <summary>
        /// Constructor
        /// </summary>
        public Item() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The item's site</param>
        public Item(Site site)
        {
            this.site = site;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The item's site</param>
        /// <param name="id">The item's id in the format Q####</param>
        public Item(Site site, string id)
        {
            this.site = site;
            try
            {
                id = id.Substring(1);
                this.id = int.Parse(id);
            }
            catch (FormatException ex)
            {
                throw new WikiBotException("The id must be in the form Q####.", ex);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The item's site</param>
        /// <param name="id">The item's id</param>
        public Item(Site site, int id)
        {
            this.site = site;
            this.id = id;
        }

        #endregion

        #region create

        /// <summary>
        /// Creates a new item with no content.
        /// </summary>
        public void createItem()
        {
            getEditSessionData();
            string result = site.PostDataAndGetResultHTM(site.site + site.indexPath + "api.php?action=wbeditentity&format=xml",
                string.Format("token={0}&data=%7B%7D", HttpUtility.UrlEncode(editSessionToken)));

            this.id = getId(result);
        }

        /// <summary>
        /// Creates a new item with the label and description in the given language.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="label">The label</param>
        /// <param name="description">The description</param>
        public void createItem(string lang, string label, string description)
        {
            createItem();
            setLabel(lang, label);
            setDescription(lang, description);
        }

        /// <summary>
        /// Creates a new item with the list of sitelinks and the given summary.
        /// The list must have the keys as language keys and the values as titles.
        /// </summary>
        /// <param name="links">The links in a dictionary</param>
        /// <param name="summary">The summary of this edit.</param>
        public void createItem(Dictionary<string, string> links, string summary)
        {
            Dictionary<string, string> labels = new Dictionary<string, string>();
            createItem(links, labels, summary);
        }

        /// <summary>
        /// Creates a new item with the list of sitelinks, the list of labels and the given summary.
        /// The lists must have the keys as language keys and the values as titles.
        /// </summary>
        /// <param name="links"></param>
        /// <param name="labels"></param>
        /// <param name="summary"></param>
        public void createItem(Dictionary<string, string> links, Dictionary<string, string> labels, string summary)
        {
            Dictionary<string, string> descriptions = new Dictionary<string, string>();
            createItem(links, labels, descriptions, summary);
        }

        /// <summary>
        /// Creates a new item with the list of sitelinks, the list of labels, the list of descriptions and the given summary.
        /// The lists must have the keys as language keys and the values as titles.
        /// </summary>
        /// <param name="links"></param>
        /// <param name="labels"></param>
        /// <param name="descriptions"></param>
        /// <param name="summary"></param>
        public void createItem(Dictionary<string, string> links, Dictionary<string, string> labels, Dictionary<string, string> descriptions, string summary)
        {
            //http://kpaste.net/e3ac254

            try
            {
                getEditSessionData();
                if (links.Count < 1)
                {
                    throw new WikiBotException("There were no links to add.");
                }
                else
                {
                    string data = "{" + JsonParser.getJsonLinks(links);
                    if (labels.Count > 0)
                    {
                        data += ", " + JsonParser.getJsonLabels(labels);
                    }
                    data += "}";

                    string postData = string.Format(
                        "token={0}&bot=bot&data={1}&summary={2}", HttpUtility.UrlEncode(editSessionToken), HttpUtility.UrlEncode(data), HttpUtility.UrlEncode(summary));
                    string respStr = site.PostDataAndGetResultHTM(
                        site.site + site.indexPath + "api.php?action=wbeditentity&format=xml", postData);

                    this.id = getId(respStr);
                }
            }
            catch (Exception ex)
            {
                throw new WikiBotException("An Error had occured while creating the item!\n" + ex.Message, ex);
            }
        }

        #endregion

        #region loading

        /// <summary>
        /// Loads the informations about this item.
        /// </summary>
        public void Load()
        {
            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "api.php?action=wbgetentities&format=xml&ids=Q" + id, "");

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            this.links = new Dictionary<string, string>();
            this.labels = new Dictionary<string, string>();
            this.descriptions = new Dictionary<string, string>();

            StringReader stream = new StringReader(respStr);

            XmlTextReader reader = new XmlTextReader(stream);

            while (reader.Read())
            {
                string lang = null;
                switch (reader.Name)
                {
                    case "sitelink":
                        // Add sitelinks
                        string title = null;

                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "site":
                                    lang = reader.Value;
                                    break;
                                case "title":
                                    title = reader.Value;
                                    break;
                            }
                        }

                        links.Add(lang, title);
                        break;
                    case "label":
                        // Add labels
                        string label = null;

                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "language":
                                    lang = reader.Value;
                                    break;
                                case "value":
                                    label = reader.Value;
                                    break;
                            }
                        }

                        labels.Add(lang, label);
                        break;
                    case "description":
                        // Add descriptions
                        string description = null;

                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.Name)
                            {
                                case "language":
                                    lang = reader.Value;
                                    break;
                                case "value":
                                    description = reader.Value;
                                    break;
                            }
                        }

                        descriptions.Add(lang, description);
                        break;
                    case "alias":
                        // Add aliases
                        // TODO
                        break;
                }
            }
        }

        private void getEditSessionData()
        {
            string src = site.GetPageHTM(
                site.indexPath + "api.php?action=query&prop=info&format=xml&intoken=edit&titles=" + HttpUtility.UrlEncode("Q" + id));
            editSessionToken = Site.editSessionTokenRE3.Match(src).Groups[1].ToString();
            if (editSessionToken == "+\\")
                editSessionToken = "";
            editSessionTime = Site.editSessionTimeRE3.Match(src).Groups[1].ToString();
            if (!string.IsNullOrEmpty(editSessionTime))
                editSessionTime = Regex.Replace(editSessionTime, "\\D", "");
            if (string.IsNullOrEmpty(editSessionTime) && !string.IsNullOrEmpty(editSessionToken))
                editSessionTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
        }

        #endregion

        #region sitelinks

        /// <summary>
        /// Sets the sitelink of a specific language.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="title">The title</param>
        public void setSiteLink(string lang, string title)
        {
            setSiteLink(lang, title, "");
        }

        /// <summary>
        /// Sets the sitelink of a specific language with the given summary.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="title">The title</param>
        /// <param name="summary">The summary</param>
        public void setSiteLink(string lang, string title, string summary)
        {
            #region test
            /*/
             * string url = site.site + site.indexPath + "api.php?action=query&prop=info&format=xml&intoken=edit&titles=Q" + id;

            MemoryStream stream = new MemoryStream();
            byte[] buffer = new ASCIIEncoding().GetBytes(site.PostDataAndGetResultHTM(url, ""));
            stream.Write(buffer, 0, buffer.Length);

            XmlTextReader reader = new XmlTextReader(stream);

            string edittoken = "";
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "page")
                    {
                        edittoken = reader.GetAttribute("edittoken");
                    }
                }
            }
            //*/
            #endregion

            getEditSessionData();
            string postData = string.Format(
                "id={0}&token={1}&linksite={2}wiki&linktitle={3}&summary={4}", id, HttpUtility.UrlEncode(editSessionToken), lang, title, summary);

            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "api.php?action=wbsetsitelink&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            if (this.links.ContainsKey(lang))
            {
                this.links[lang] = title;
            }
            else
            {
                this.links.Add(lang, title);
            }

            Console.WriteLine("‎Changed [" + lang + "] sitelink: " + title);
        }

        /// <summary>
        /// Sets all the sitelinks in the given dictionary.
        /// </summary>
        /// <param name="links">The links in a dictionary</param>
        public void setSiteLink(Dictionary<string, string> links)
        {
            setSiteLink(links, "");
        }

        /// <summary>
        /// Sets all the sitelinks in the given dictionary with the given summary.
        /// </summary>
        /// <param name="links">The links in a dictionary</param>
        /// <param name="summary">The summary</param>
        public void setSiteLink(Dictionary<string, string> links, string summary)
        {
            getEditSessionData();

            string data = JsonParser.getJsonLinks(links);

            string postData = string.Format(
                "id={0}&token={1}&data={2}&summary={3}", id, HttpUtility.UrlEncode(editSessionToken), data, summary);
            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "/w/api.php?action=wbeditentity&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            this.links = links;
        }

        #endregion

        #region labels

        /// <summary>
        /// Sets the label of a specific language.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="label">The label</param>
        public void setLabel(string lang, string label)
        {
            setLabel(lang, label, "");
        }

        /// <summary>
        /// Sets the label of a specific language with the given summary.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="label">The label</param>
        /// <param name="summary">The summary</param>
        public void setLabel(string lang, string label, string summary)
        {
            getEditSessionData();
            string postData = string.Format(
                "id={0}&token={1}&language={2}&value={3}&summary={4}", id, HttpUtility.UrlEncode(editSessionToken), lang, label, summary);

            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "api.php?action=wbsetlabel&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            if (this.labels.ContainsKey(lang))
            {
                this.labels[lang] = label;
            }
            else
            {
                this.labels.Add(lang, label);
            }

            Console.WriteLine("‎Changed [" + lang + "] label: " + label);
        }

        /// <summary>
        /// Sets all the labels in the given dictionary.
        /// </summary>
        /// <param name="labels">The labels in a dictionary</param>
        public void setLabel(Dictionary<string, string> labels)
        {
            setLabel(labels, "");
        }

        /// <summary>
        /// Sets all the labels in the given dictionary with the given summary.
        /// </summary>
        /// <param name="labels">The labels in a dictionary</param>
        /// <param name="summary">The summary</param>
        public void setLabel(Dictionary<string, string> labels, string summary)
        {
            getEditSessionData();

            string data = JsonParser.getJsonLabels(labels);

            string postData = string.Format(
                "id={0}&token={1}&data={2}&summary={3}", id, HttpUtility.UrlEncode(editSessionToken), data, summary);
            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "/w/api.php?action=wbeditentity&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            this.labels = labels;
        }

        #endregion

        #region descriptions

        /// <summary>
        /// Sets the description of a specific language.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="description">The description</param>
        public void setDescription(string lang, string description)
        {
            setDescription(lang, description, "");
        }

        /// <summary>
        /// Sets the description of a specific language with the given summary.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="description">The description</param>
        /// <param name="summary">The summary</param>
        public void setDescription(string lang, string description, string summary)
        {
            getEditSessionData();
            string postData = string.Format(
                "id={0}&token={1}&language={2}&value={3}&summary={4}", id, HttpUtility.UrlEncode(editSessionToken), lang, description, summary);

            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "api.php?action=wbsetdescription&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            if (this.descriptions.ContainsKey(lang))
            {
                this.descriptions[lang] = description;
            }
            else
            {
                this.descriptions.Add(lang, description);
            }

            Console.WriteLine("‎Changed [" + lang + "] description: " + description);
        }

        /// <summary>
        /// Sets all the descriptions in the given dictionary.
        /// </summary>
        /// <param name="descriptions">The descriptions in a dictionary</param>
        public void setDescription(Dictionary<string, string> descriptions)
        {
            setDescription(descriptions, "");
        }

        /// <summary>
        /// Sets all the descriptions in the given dictionary with the given summary.
        /// </summary>
        /// <param name="descriptions">The descriptions in a dictionary</param>
        /// <param name="summary">The summary</param>
        public void setDescription(Dictionary<string, string> descriptions, string summary)
        {
            getEditSessionData();

            string data = JsonParser.getJsonDescriptions(descriptions);

            string postData = string.Format(
                "id={0}&token={1}&data={2}&summary={3}", id, HttpUtility.UrlEncode(editSessionToken), data, summary);
            string respStr = site.PostDataAndGetResultHTM(
                site.site + site.indexPath + "/w/api.php?action=wbeditentity&format=xml", postData);

            if (isError(respStr))
            {
                throw getAPIError(respStr);
            }

            this.descriptions = descriptions;
        }

        #endregion

        #region aliases

        // TODO

        #endregion

        #region getId

        /// <summary>
        /// Returns if the item with the sitelink in the specific language already exists.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="sitelink">The sitelink</param>
        /// <returns>Exists</returns>
        public bool itemExists(string lang, string sitelink)
        {
            try
            {
                GetIdBySitelink(lang, sitelink);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the id of an item with the sitelink in the specific language.
        /// </summary>
        /// <param name="lang">The language</param>
        /// <param name="sitelink">The sitelink</param>
        /// <returns>The id</returns>
        public int GetIdBySitelink(string lang, string sitelink)
        {
            string url = site.site + site.indexPath + string.Format("api.php?action=wbgetentities&format=xml&sites={0}wiki&titles={1}&props=info", lang, sitelink);
            string result = site.PostDataAndGetResultHTM(url, "");

            return getId(result);
        }

        /// <summary>
        /// Returns the id of an item by the result of an api request.
        /// </summary>
        /// <param name="result">The result of the api request</param>
        /// <returns>The id</returns>
        private int getId(string result)
        {
            string mark = " id=\"q"; // <entity
            string id = result;

            id = id.Remove(0, id.IndexOf(mark) + mark.Length);
            id = id.Remove(id.IndexOf("\""));
            //Console.WriteLine("Item Id: " + id);
            int resultId;
            try
            {
                resultId = int.Parse(id);
            }
            catch
            {
                throw getAPIError(result);
            }

            return resultId;
        }

        #endregion

        #region API-answer

        private bool isError(string result)
        {
            try
            {
                if (result.Contains("<api success=\"1\">"))
                    return false;
                getAPIError(result);
            }
            catch { return false; }

            return true;
        }

        private ApiException getAPIError(string result)
        {
            result = result.Substring(result.IndexOf("<error "));
            result = result.Remove(result.IndexOf(">") + 1);
            string codemark = " code=\"";
            string code = result.Remove(0, result.IndexOf(codemark) + codemark.Length);
            code = code.Remove(code.IndexOf("\""));

            string messagemark = " info=\"";
            string message = result.Remove(0, result.IndexOf(messagemark) + messagemark.Length);
            message = message.Remove(message.IndexOf("\""));

            return new ApiException(code, message);
        }

        #endregion
    }
}