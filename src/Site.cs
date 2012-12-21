// DotNetWikiBot Framework 2.101 - bot framework based on Microsoft .NET Framework 2.0 for wiki projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright (c) Iaroslav Vassiliev (2006-2012) codedriller@gmail.com

using System;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Xml.XPath;
using System.Web;
using DotNetDataBot.Exceptions;
using DotNetDataBot.Helpers;

namespace DotNetDataBot
{
    /// <summary>Class defines wiki site object.</summary>
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [Serializable]
    public class Site
    {
        #region variables

        /// <summary>Wiki site URL.</summary>
        public string site;
        /// <summary>User's account to login with.</summary>
        public string userName;
        /// <summary>User's password to login with.</summary>
        private string userPass;
        /// <summary>Default domain for LDAP authentication. Additional information can
        /// be found at http://www.mediawiki.org/wiki/Extension:LDAP_Authentication.</summary>
        public string userDomain = "";
        /// <summary>Site title.</summary>
        public string name;
        /// <summary>MediaWiki version as string.</summary>
        public string generator;
        /// <summary>MediaWiki version as number.</summary>
        public float version;
        /// <summary>MediaWiki version as Version object.</summary>
        public Version ver;
        /// <summary>Rule of page title capitalization.</summary>
        public string capitalization;
        /// <summary>Short relative path to wiki pages (if such alias is set on the server).
        /// See "http://www.mediawiki.org/wiki/Manual:Short URL" for details.</summary>
        public string wikiPath;		// = "/wiki/";
        /// <summary>Relative path to "index.php" file on server.</summary>
        public string indexPath;	// = "/w/";
        /// <summary>User's watchlist. Should be loaded manually with FillFromWatchList function,
        /// if it is necessary.</summary>
        public PageList watchList;
        /// <summary>MediaWiki interface messages. Should be loaded manually with
        /// GetMediaWikiMessagesEx function, if it is necessary.</summary>
        public PageList messages;
        /// <summary>Regular expression to find redirection target.</summary>
        public Regex redirectRE;
        /// <summary>Regular expression to find links to pages in list in HTML source.</summary>
        public static Regex linkToPageRE1 =
            new Regex("<li><a href=\"[^\"]*?\" (?:class=\"mw-redirect\" )?title=\"([^\"]+?)\">");
        /// <summary>Alternative regular expression to find links to pages in HTML source.</summary>
        public static Regex linkToPageRE2 =
            new Regex("<a href=\"[^\"]*?\" title=\"([^\"]+?)\">\\1</a>");
        /// <summary>Alternative regular expression to find links to pages (mostly image and file
        /// pages) in HTML source.</summary>
        public Regex linkToPageRE3;
        /// <summary>Regular expression to find links to subcategories in HTML source
        /// of category page on sites that use "CategoryTree" MediaWiki extension.</summary>
        public static Regex linkToSubCategoryRE =
            new Regex(">([^<]+)</a></div>\\s*<div class=\"CategoryTreeChildren\"");
        /// <summary>Regular expression to find links to image pages in galleries
        /// in HTML source.</summary>
        public static Regex linkToImageRE =
            new Regex("<div class=\"gallerytext\">\n<a href=\"[^\"]*?\" title=\"([^\"]+?)\">");
        /// <summary>Regular expression to find titles in markup.</summary>
        public static Regex pageTitleTagRE = new Regex("<title>(.+?)</title>");
        /// <summary>Regular expression to find internal wiki links in markup.</summary>
        public static Regex wikiLinkRE = new Regex(@"\[\[(.+?)(\|.+?)?]]");
        /// <summary>Regular expression to find wiki category links.</summary>
        public Regex wikiCategoryRE;
        /// <summary>Regular expression to find wiki templates in markup.</summary>
        public static Regex wikiTemplateRE = new Regex(@"(?s)\{\{(.+?)((\|.*?)*?)}}");
        /// <summary>Regular expression to find embedded images and files in wiki markup.</summary>
        public Regex wikiImageRE;
        /// <summary>Regular expression to find links to sister wiki projects in markup.</summary>
        public static Regex sisterWikiLinkRE;
        /// <summary>Regular expression to find interwiki links in wiki markup.</summary>
        public static Regex iwikiLinkRE;
        /// <summary>Regular expression to find displayed interwiki links in wiki markup,
        /// like "[[:de:...]]".</summary>
        public static Regex iwikiDispLinkRE;
        /// <summary>Regular expression to find external web links in wiki markup.</summary>
        public static Regex webLinkRE =
            new Regex("(https?|t?ftp|news|nntp|telnet|irc|gopher)://([^\\s'\"<>]+)");
        /// <summary>Regular expression to find sections of text, that are explicitly
        /// marked as non-wiki with special tag.</summary>
        public static Regex noWikiMarkupRE = new Regex("(?is)<nowiki>(.*?)</nowiki>");
        /// <summary>A template for disambiguation page. If some unusual template is used in your
        /// wiki for disambiguation, then it must be set in this variable. Use "|" as a delimiter
        /// when enumerating several templates here.</summary>
        public string disambigStr;
        /// <summary>Regular expression to extract language code from site URL.</summary>
        public static Regex siteLangRE = new Regex(@"https?://(.*?)\.(.+?\..+)");
        /// <summary>Regular expression to extract edit session time attribute.</summary>
        public static Regex editSessionTimeRE1 =
            new Regex("value=\"([^\"]*?)\" name=['\"]wpEdittime['\"]");
        /// <summary>Regular expression to extract edit session time attribute.</summary>
        public static Regex editSessionTimeRE3 = new Regex(" touched=\"(.+?)\"");
        /// <summary>Regular expression to extract edit session token attribute.</summary>
        public static Regex editSessionTokenRE1 =
            new Regex("value=\"([^\"]*?)\" name=['\"]wpEditToken['\"]");
        /// <summary>Regular expression to extract edit session token attribute.</summary>
        public static Regex editSessionTokenRE2 =
            new Regex("name=['\"]wpEditToken['\"](?: type=\"hidden\")? value=\"([^\"]*?)\"");
        /// <summary>Regular expression to extract edit session token attribute.</summary>
        public static Regex editSessionTokenRE3 = new Regex(" edittoken=\"(.+?)\"");
        /// <summary>Site cookies.</summary>
        public CookieContainer cookies = new CookieContainer();
        /// <summary>XML name table for parsing XHTML documents from wiki site.</summary>
        public NameTable xhtmlNameTable = new NameTable();
        /// <summary>XML namespace URI of wiki site's XHTML version.</summary>
        public string xhtmlNSUri = "http://www.w3.org/1999/xhtml";
        /// <summary>XML namespace manager for parsing XHTML documents from wiki site.</summary>
        public XmlNamespaceManager xmlNS;
        /// <summary>Local namespaces.</summary>
        public Hashtable namespaces = new Hashtable();
        /// <summary>Default namespaces.</summary>
        public static Hashtable wikiNSpaces = new Hashtable();
        /// <summary>List of Wikimedia Foundation sites and according prefixes.</summary>
        public static Hashtable WMSites = new Hashtable();
        /// <summary>Built-in variables of MediaWiki software, used in brackets {{...}}.
        /// To be distinguished from templates.
        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>
        public static string[] mediaWikiVars;
        /// <summary>Built-in parser functions (and similar prefixes) of MediaWiki software, used
        /// like first ... in {{...:...}}. To be distinguished from templates.
        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>
        public static string[] parserFunctions;
        /// <summary>Built-in template modifiers of MediaWiki software
        /// (see http://meta.wikimedia.org/wiki/Help:Magic_words).</summary>
        public static string[] templateModifiers;
        /// <summary>Interwiki links sorting order, based on local language by first word.
        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>
        public static string[] iwikiLinksOrderByLocalFW;
        /// <summary>Interwiki links sorting order, based on local language.
        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>
        public static string[] iwikiLinksOrderByLocal;
        /// <summary>Interwiki links sorting order, based on latin alphabet by first word.
        /// See http://meta.wikimedia.org/wiki/Interwiki_sorting_order for details.</summary>
        public static string[] iwikiLinksOrderByLatinFW;
        /// <summary>Wikimedia Foundation sites and prefixes in one regex-escaped string
        /// with "|" as separator.</summary>
        public static string WMSitesStr;
        /// <summary>ISO 639-1 language codes, used as prefixes to identify Wikimedia
        /// Foundation sites, gathered in one regex-escaped string with "|" as separator.</summary>
        public static string WMLangsStr;
        /// <summary>Availability of "api.php" MediaWiki extension (bot interface).</summary>
        public bool botQuery;
        /// <summary>Versions of "api.php" MediaWiki extension (bot interface) modules.</summary>
        public Hashtable botQueryVersions = new Hashtable();
        /// <summary>Set of lists of pages, produced by bot interface.</summary>
        public static Hashtable botQueryLists = new Hashtable();
        /// <summary>Set of lists of parsed data, produced by bot interface.</summary>
        public static Hashtable botQueryProps = new Hashtable();
        /// <summary>Site language.</summary>
        public string language;
        /// <summary>Site language text direction.</summary>
        public string langDirection;
        /// <summary>Site's neutral (language) culture.</summary>
        public CultureInfo langCulture;
        /// <summary>Randomly chosen regional (non-neutral) culture for site's language.</summary>
        public CultureInfo regCulture;
        /// <summary>Site encoding.</summary>
        public Encoding encoding = Encoding.UTF8;

        #endregion

        #region construction

        /// <summary>This constructor is used to generate most Site objects.</summary>
        /// <param name="site">Wiki site's URI. It must point to the main page of the wiki, e.g.
        /// "http://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>
        /// <param name="userName">User name to log in.</param>
        /// <param name="userPass">Password.</param>
        /// <returns>Returns Site object.</returns>
        public Site(string site, string userName, string userPass)
            : this(site, userName, userPass, "") { }

        /// <summary>This constructor is used for LDAP authentication. Additional information can
        /// be found at "http://www.mediawiki.org/wiki/Extension:LDAP_Authentication".</summary>
        /// <param name="site">Wiki site's URI. It must point to the main page of the wiki, e.g.
        /// "http://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>
        /// <param name="userName">User name to log in.</param>
        /// <param name="userPass">Password.</param>
        /// <param name="userDomain">Domain for LDAP authentication.</param>
        /// <returns>Returns Site object.</returns>
        public Site(string site, string userName, string userPass, string userDomain)
        {
            this.site = site;
            this.userName = userName;
            this.userPass = userPass;
            this.userDomain = userDomain;
            Initialize();
        }

        /// <summary>This constructor uses default site, userName and password. The site URL and
        /// account data can be stored in UTF8-encoded "Defaults.dat" file in bot's "Cache"
        /// subdirectory.</summary>
        /// <returns>Returns Site object.</returns>
        public Site()
        {
            if (File.Exists("Cache" + Path.DirectorySeparatorChar + "Defaults.dat") == true)
            {
                string[] lines = File.ReadAllLines(
                    "Cache" + Path.DirectorySeparatorChar + "Defaults.dat", Encoding.UTF8);
                if (lines.GetUpperBound(0) >= 2)
                {
                    this.site = lines[0];
                    this.userName = lines[1];
                    this.userPass = lines[2];
                    if (lines.GetUpperBound(0) >= 3)
                        this.userDomain = lines[3];
                    else
                        this.userDomain = "";
                }
                else
                    throw new WikiBotException(
                        Bot.Msg("\"\\Cache\\Defaults.dat\" file is invalid."));
            }
            else
                throw new WikiBotException(Bot.Msg("\"\\Cache\\Defaults.dat\" file not found."));
            Initialize();
        }

        /// <summary>This internal function establishes connection to site and loads general site
        /// info by the use of other functions. Function is called from the constructors.</summary>
        public void Initialize()
        {
            xmlNS = new XmlNamespaceManager(xhtmlNameTable);
            if (site.Contains("sourceforge"))
            {
                site = site.Replace("https://", "http://");
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogInSourceForge();
                site = site.Replace("http://", "https://");
            }
            else if (site.Contains("wikia.com"))
            {
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogInViaApi();
            }
            else
            {
                GetPaths();
                xmlNS.AddNamespace("ns", xhtmlNSUri);
                LoadDefaults();
                LogIn();
            }
            GetInfo();
            if (!Bot.isRunningOnMono)
                Bot.DisableCanonicalizingUriAsFilePath();	// .NET bug evasion
        }

        #endregion

        #region get

        /// <summary>Gets path to "index.php", short path to pages (if present), and then
        /// saves paths to file.</summary>
        public void GetPaths()
        {
            if (!site.StartsWith("http"))
                site = "http://" + site;
            if (Bot.CountMatches(site, "/", false) == 3 && site.EndsWith("/"))
                site = site.Substring(0, site.Length - 1);
            string filePathName = "Cache" + Path.DirectorySeparatorChar +
                HttpUtility.UrlEncode(site.Replace("://", ".").Replace("/", ".")) + ".dat";
            if (File.Exists(filePathName) == true)
            {
                string[] lines = File.ReadAllLines(filePathName, Encoding.UTF8);
                if (lines.GetUpperBound(0) >= 4)
                {
                    wikiPath = lines[0];
                    indexPath = lines[1];
                    xhtmlNSUri = lines[2];
                    language = lines[3];
                    langDirection = lines[4];
                    if (lines.GetUpperBound(0) >= 5)
                        site = lines[5];
                    return;
                }
            }
            Console.WriteLine(Bot.Msg("Logging in..."));
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(site);
            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webReq.UseDefaultCredentials = true;
            webReq.ContentType = Bot.webContentType;
            webReq.UserAgent = Bot.botVer;
            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {
                webReq.ProtocolVersion = HttpVersion.Version10;
                webReq.KeepAlive = false;
            }
            HttpWebResponse webResp = null;
            for (int errorCounter = 0; true; errorCounter++)
            {
                try
                {
                    webResp = (HttpWebResponse)webReq.GetResponse();
                    break;
                }
                catch (WebException e)
                {
                    string message = e.Message;
                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem
                        if (errorCounter > Bot.retryTimes)
                            throw;
                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));
                        Thread.Sleep(60000);
                    }
                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem
                        Bot.SwitchUnsafeHttpHeaderParsing(true);
                        GetPaths();
                        return;
                    }
                    else
                    {
                        Console.Error.WriteLine(Bot.Msg("Can't access the site.") + " " + message);
                        throw;
                    }
                }
            }
            site = webResp.ResponseUri.Scheme + "://" + webResp.ResponseUri.Authority;
            Regex wikiPathRE = new Regex("(?i)" + Regex.Escape(site) + "(/.+?/).+");
            Regex indexPathRE1 = new Regex("(?i)" + Regex.Escape(site) +
                "(/.+?/)index\\.php(\\?|/)");
            Regex indexPathRE2 = new Regex("(?i)href=\"(/[^\"\\s<>?]*?)index\\.php(\\?|/)");
            Regex indexPathRE3 = new Regex("(?i)wgScript=\"(/[^\"\\s<>?]*?)index\\.php");
            Regex xhtmlNSUriRE = new Regex("(?i)<html[^>]*( xmlns=\"(?'xmlns'[^\"]+)\")[^>]*>");
            Regex languageRE = new Regex("(?i)<html[^>]*( lang=\"(?'lang'[^\"]+)\")[^>]*>");
            Regex langDirectionRE = new Regex("(?i)<html[^>]*( dir=\"(?'dir'[^\"]+)\")[^>]*>");
            string mainPageUri = webResp.ResponseUri.ToString();
            if (mainPageUri.Contains("/index.php?"))
                indexPath = indexPathRE1.Match(mainPageUri).Groups[1].ToString();
            else
                wikiPath = wikiPathRE.Match(mainPageUri).Groups[1].ToString();
            if (string.IsNullOrEmpty(indexPath) && string.IsNullOrEmpty(wikiPath) &&
                mainPageUri[mainPageUri.Length - 1] != '/' &&
                Bot.CountMatches(mainPageUri, "/", false) == 3)
                wikiPath = "/";
            Stream respStream = webResp.GetResponseStream();
            if (webResp.ContentEncoding.ToLower().Contains("gzip"))
                respStream = new GZipStream(respStream, CompressionMode.Decompress);
            else if (webResp.ContentEncoding.ToLower().Contains("deflate"))
                respStream = new DeflateStream(respStream, CompressionMode.Decompress);
            StreamReader strmReader = new StreamReader(respStream, encoding);
            string src = strmReader.ReadToEnd();
            webResp.Close();
            if (!site.Contains("wikia.com"))
                indexPath = indexPathRE2.Match(src).Groups[1].ToString();
            else
                indexPath = indexPathRE3.Match(src).Groups[1].ToString();
            xhtmlNSUri = xhtmlNSUriRE.Match(src).Groups["xmlns"].ToString();
            if (string.IsNullOrEmpty(xhtmlNSUri))
                xhtmlNSUri = "http://www.w3.org/1999/xhtml";
            language = languageRE.Match(src).Groups["lang"].ToString();
            langDirection = langDirectionRE.Match(src).Groups["dir"].ToString();
            if (!Directory.Exists("Cache"))
                Directory.CreateDirectory("Cache");
            File.WriteAllText(filePathName, wikiPath + "\r\n" + indexPath + "\r\n" + xhtmlNSUri +
                "\r\n" + language + "\r\n" + langDirection + "\r\n" + site, Encoding.UTF8);
        }

        /// <summary>Gets a specified MediaWiki message.</summary>
        /// <param name="title">Title of the message.</param>
        /// <returns>Returns raw text of the message.
        /// If the message doesn't exist, exception is thrown.</returns>
        public string GetMediaWikiMessage(string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException("title");
            title = namespaces["8"].ToString() + ":" + Bot.Capitalize(RemoveNSPrefix(title, 8));
            if (messages == null)
                messages = new PageList(this);
            else if (messages.Contains(title))
                return messages[title].text;
            string src;
            try
            {
                src = GetPageHTM(site + indexPath + "index.php?title=" +
                    HttpUtility.UrlEncode(title.Replace(" ", "_")) +
                    "&action=raw&usemsgcache=1&dontcountme=s");
            }
            catch (WebException e)
            {
                if (e.Message.Contains(": (404) "))
                    throw new WikiBotException(
                        string.Format(Bot.Msg("MediaWiki message \"{0}\" was not found."), title));
                else
                    throw;
            }
            if (string.IsNullOrEmpty(src))
            {
                throw new WikiBotException(
                    string.Format(Bot.Msg("MediaWiki message \"{0}\" was not found."), title));
            }
            messages.Add(new Page(this, title));
            messages[messages.Count() - 1].text = src;
            return src;
        }

        /// <summary>Gets all modified MediaWiki messages (to be more precise, all messages that are
        /// contained in database), loads them into site.messages PageList (both titles and texts)
        /// and dumps them to XML file.</summary>
        /// <param name="forceLoad">If true, the messages are updated unconditionally. Otherwise
        /// the messages are updated only if they are outdated.</param>
        public void GetModifiedMediaWikiMessages(bool forceLoad)
        {
            if (messages == null)
                messages = new PageList(this);
            string filePathName = "Cache" + Path.DirectorySeparatorChar +
                HttpUtility.UrlEncode(site.Replace("://", ".")) + ".mw_db_msg.xml";
            if (forceLoad == false && File.Exists(filePathName) &&
                (DateTime.Now - File.GetLastWriteTime(filePathName)).Days <= 90)
            {
                messages.FillAndLoadFromXMLDump(filePathName);
                return;
            }
            Console.WriteLine(Bot.Msg("Updating MediaWiki messages dump. Please, wait..."));
            PageList pl = new PageList(this);
            bool prevBotQueryState = botQuery;
            botQuery = false;	// backward compatibility requirement
            pl.FillFromAllPages("!", 8, false, 100000);
            botQuery = prevBotQueryState;
            File.Delete(filePathName);
            pl.SaveXMLDumpToFile(filePathName);
            messages.FillAndLoadFromXMLDump(filePathName);
            Console.WriteLine(Bot.Msg("MediaWiki messages dump updated successfully."));
        }

        /// <summary>Gets all MediaWiki messages from "Special:Allmessages" page and loads them into
        /// site.messages PageList. The function is not backward compatible.</summary>
        public void GetMediaWikiMessages()
        {
            if (messages == null)
                messages = new PageList(this);
            Console.WriteLine(Bot.Msg("Updating MediaWiki messages dump. Please, wait..."));
            string res = site + indexPath + "index.php?title=Special:Allmessages";
            string src = "";
            Page p = null;
            Regex nextPortionRE = new Regex("offset=([^\"]+)\" title=\"[^\"]+\" rel=\"next\"");
            do
            {
                src = GetPageHTM(res + (src != ""
                    ? "&offset=" + HttpUtility.HtmlDecode(nextPortionRE.Match(src).Groups[1].Value)
                    : "&limit=5000"));
                using (XmlReader reader = GetXMLReader(src))
                {
                    reader.ReadToFollowing("tbody");
                    while (reader.Read())
                    {
                        if (reader.Name == "tr" && reader.NodeType == XmlNodeType.Element &&
                            reader["id"] != null)
                            p = new Page(this, namespaces["8"].ToString() + ":" +
                                Bot.Capitalize(reader["id"].Replace("msg_", "")));
                        else if (reader.Name == "td" &&
                            (reader["class"] == "am_default" || reader["class"] == "am_actual"))
                            p.text = reader.ReadString();
                        else if (reader.Name == "tr" && reader.NodeType == XmlNodeType.EndElement)
                            messages.Add(p);
                        else if (reader.Name == "tbody" &&
                            reader.NodeType == XmlNodeType.EndElement)
                            break;
                    }
                }
            } while (nextPortionRE.IsMatch(src));
            if (p != null)
                messages.Add(p);
            Console.WriteLine(Bot.Msg("MediaWiki messages dump updated successfully."));
        }

        /// <summary>Retrieves metadata and local namespace names from site.</summary>
        public void GetInfo()
        {
            try
            {
                langCulture = new CultureInfo(language, false);
            }
            catch (Exception)
            {
                langCulture = new CultureInfo("");
            }
            if (langCulture.Equals(CultureInfo.CurrentUICulture.Parent))
                regCulture = CultureInfo.CurrentUICulture;
            else
            {
                try
                {
                    regCulture = CultureInfo.CreateSpecificCulture(language);
                }
                catch (Exception)
                {
                    foreach (CultureInfo ci in
                        CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                    {
                        if (langCulture.Equals(ci.Parent))
                        {
                            regCulture = ci;
                            break;
                        }
                    }
                    if (regCulture == null)
                        regCulture = CultureInfo.InvariantCulture;
                }
            }

            string src = GetPageHTM(site + indexPath + "index.php?title=Special:Export/" +
                DateTime.Now.Ticks.ToString("x"));
            XmlTextReader reader = new XmlTextReader(new StringReader(src));
            reader.WhitespaceHandling = WhitespaceHandling.None;
            reader.ReadToFollowing("sitename");
            name = reader.ReadString();
            reader.ReadToFollowing("generator");
            generator = reader.ReadString();
            ver = new Version(Regex.Replace(generator, @"[^\d\.]", ""));
            float.TryParse(ver.ToString(), NumberStyles.AllowDecimalPoint,
                new CultureInfo("en-US"), out version);
            reader.ReadToFollowing("case");
            capitalization = reader.ReadString();
            namespaces.Clear();
            while (reader.ReadToFollowing("namespace"))
                namespaces.Add(reader.GetAttribute("key"),
                    HttpUtility.HtmlDecode(reader.ReadString()));
            reader.Close();
            namespaces.Remove("0");
            foreach (DictionaryEntry ns in namespaces)
            {
                if (!wikiNSpaces.ContainsKey(ns.Key) ||
                    ns.Key.ToString() == "4" || ns.Key.ToString() == "5")
                    wikiNSpaces[ns.Key] = ns.Value;
            }
            if (ver >= new Version(1, 14))
            {
                wikiNSpaces["6"] = "File";
                wikiNSpaces["7"] = "File talk";
            }
            wikiCategoryRE = new Regex(@"\[\[(?i)(((" + Regex.Escape(wikiNSpaces["14"].ToString()) +
                "|" + Regex.Escape(namespaces["14"].ToString()) + @"):(.+?))(\|(.+?))?)]]");
            wikiImageRE = new Regex(@"\[\[(?i)((File|Image" +
                "|" + Regex.Escape(namespaces["6"].ToString()) + @"):(.+?))(\|(.+?))*?]]");
            string namespacesStr = "";
            foreach (DictionaryEntry ns in namespaces)
                namespacesStr += Regex.Escape(ns.Value.ToString()) + "|";
            namespacesStr = namespacesStr.Replace("||", "|").Trim("|".ToCharArray());
            linkToPageRE3 = new Regex("<a href=\"[^\"]*?\" title=\"(" +
                Regex.Escape(namespaces["6"].ToString()) + ":[^\"]+?)\">");
            string redirectTag = "REDIRECT";
            switch (language)
            {		// Revised 2010-07-02 (MediaWiki 1.15.4)
                case "af": redirectTag += "|aanstuur"; break;
                case "ar": redirectTag += "|تحويل"; break;
                case "arz": redirectTag += "|تحويل|تحويل#"; break;
                case "be": redirectTag += "|перанакіраваньне"; break;
                case "be-x-old": redirectTag += "|перанакіраваньне"; break;
                case "bg": redirectTag += "|пренасочване|виж"; break;
                case "br": redirectTag += "|adkas"; break;
                case "bs": redirectTag += "|preusmjeri"; break;
                case "cs": redirectTag += "|přesměruj"; break;
                case "cu": redirectTag += "|прѣнаправлєниѥ"; break;
                case "cy": redirectTag += "|ail-cyfeirio|ailgyfeirio"; break;
                case "de": redirectTag += "|weiterleitung"; break;
                case "el": redirectTag += "|ανακατευθυνση"; break;
                case "eo": redirectTag += "|alidirektu"; break;
                case "es": redirectTag += "|redireccíon"; break;
                case "et": redirectTag += "|suuna"; break;
                case "eu": redirectTag += "|birzuzendu"; break;
                case "fa": redirectTag += "|تغییرمسیر"; break;
                case "fi": redirectTag += "|uudelleenohjaus|ohjaus"; break;
                case "fr": redirectTag += "|redirection"; break;
                case "ga": redirectTag += "|athsheoladh"; break;
                case "gl": redirectTag += "|redirección"; break;
                case "he": redirectTag += "|הפניה"; break;
                case "hr": redirectTag += "|preusmjeri"; break;
                case "hu": redirectTag += "|átirányítás"; break;
                case "hy": redirectTag += "|վերահղում"; break;
                case "id": redirectTag += "|alih"; break;
                case "is": redirectTag += "|tilvísun"; break;
                case "it": redirectTag += "|redirezione"; break;
                case "ja": redirectTag += "|転送|リダイレクト|転送|リダイレクト"; break;
                case "ka": redirectTag += "|გადამისამართება"; break;
                case "kk": redirectTag += "|ايداۋ|айдау|aýdaw"; break;
                case "km": redirectTag += "|បញ្ជូនបន្ត|ប្ដូរទីតាំងទៅ #ប្តូរទីតាំងទៅ"
                    + "|ប្ដូរទីតាំង|ប្តូរទីតាំង|ប្ដូរចំណងជើង"; break;
                case "ko": redirectTag += "|넘겨주기"; break;
                case "ksh": redirectTag += "|ömleidung"; break;
                case "lt": redirectTag += "|peradresavimas"; break;
                case "mk": redirectTag += "|пренасочување|види"; break;
                case "ml": redirectTag += "|аґ¤аґїаґ°аґїаґљаµЌаґљаµЃаґµаґїаґџаµЃаґ•" +
                    "|аґ¤аґїаґ°аґїаґљаµЌаґљаµЃаґµаґїаґџаґІаµЌвЂЌ"; break;
                case "mr": redirectTag += "|а¤ЄаҐЃа¤Ёа¤°аҐЌа¤Ёа¤їа¤°аҐЌа¤¦аҐ‡а¤¶а¤Ё"; break;
                case "mt": redirectTag += "|rindirizza"; break;
                case "mwl": redirectTag += "|ancaminar"; break;
                case "nds": redirectTag += "|wiederleiden"; break;
                case "nds-nl": redirectTag += "|deurverwiezing|doorverwijzing"; break;
                case "nl": redirectTag += "|doorverwijzing"; break;
                case "nn": redirectTag += "|omdiriger"; break;
                case "oc": redirectTag += "|redireccion"; break;
                case "pl": redirectTag += "|patrz|przekieruj|tam"; break;
                case "pt": redirectTag += "|redirecionamento"; break;
                case "ro": redirectTag += "|redirecteaza"; break;
                case "ru": redirectTag += "|перенаправление|перенапр"; break;
                case "sa": redirectTag += "|а¤ЄаҐЃа¤Ёа¤°аҐЌа¤Ёа¤їа¤¦аҐ‡а¤¶а¤Ё"; break;
                case "sd": redirectTag += "|چوريو"; break;
                case "si": redirectTag += "|а¶єа·…а·’а¶єа·ња¶ёа·”а·Ђ"; break;
                case "sk": redirectTag += "|presmeruj"; break;
                case "sl": redirectTag += "|preusmeritev"; break;
                case "sq": redirectTag += "|ridrejto"; break;
                case "sr": redirectTag += "|преусмери|preusmeri"; break;
                case "srn": redirectTag += "|doorverwijzing"; break;
                case "sv": redirectTag += "|omdirigering"; break;
                case "ta": redirectTag += "|а®µа®ґа®їа®®а®ѕа®±аЇЌа®±аЇЃ"; break;
                case "te": redirectTag += "|а°¦а°ѕа°°а°їа°®а°ѕа°°а±Ќа°Єа±Ѓ"; break;
                case "tr": redirectTag += "|yönlendİrme"; break;
                case "tt": redirectTag += "перенаправление|перенапр|yünältü"; break;
                case "uk": redirectTag += "|перенаправлення|перенаправление|перенапр"; break;
                case "vi": redirectTag += "|đổi|đổi"; break;
                case "vro": redirectTag += "|saadaq|suuna"; break;
                case "yi": redirectTag += "|ווייטערפירן|#הפניה"; break;
                default: redirectTag = "REDIRECT"; break;
            }
            redirectRE = new Regex(@"(?i)^#(?:" + redirectTag + @")\s*:?\s*\[\[(.+?)(\|.+)?]]",
                RegexOptions.Compiled);
            Console.WriteLine(Bot.Msg("Site: {0} ({1})"), name, generator);
            string botQueryUriStr = site + indexPath + "api.php?version";
            string respStr;
            try
            {
                respStr = GetPageHTM(botQueryUriStr);
                if (respStr.Contains("<title>MediaWiki API</title>"))
                {
                    botQuery = true;
                    Regex botQueryVersionsRE = new Regex(@"(?i)<b><i>\$" +
                        @"Id: (\S+) (\d+) (.+?) \$</i></b>");
                    foreach (Match m in botQueryVersionsRE.Matches(respStr))
                        botQueryVersions[m.Groups[1].ToString()] = m.Groups[2].ToString();
                    if (!botQueryVersions.ContainsKey("ApiMain.php") && ver > new Version(1, 17))
                    {
                        // if versioning system is broken
                        botQueryVersions["ApiQueryCategoryMembers.php"] = "104449";
                        botQueryVersions["ApiQueryRevisions.php"] = "104449";
                    }
                }
            }
            catch (WebException)
            {
                botQuery = false;
            }
            if ((botQuery == false || !botQueryVersions.ContainsKey("ApiQueryCategoryMembers.php"))
                && ver < new Version(1, 16))
            {
                botQueryUriStr = site + indexPath + "query.php";
                try
                {
                    respStr = GetPageHTM(botQueryUriStr);
                    if (respStr.Contains("<title>MediaWiki Query Interface</title>"))
                    {
                        botQuery = true;
                        botQueryVersions["query.php"] = "Unknown";
                    }
                }
                catch (WebException)
                {
                    return;
                }
            }
        }

        #endregion

        #region load

        /// <summary>Loads default English namespace names for site.</summary>
        public void LoadDefaults()
        {
            if (wikiNSpaces.Count != 0 && WMSites.Count != 0)
                return;

            string[] wikiNSNames = { "Media", "Special", "", "Talk", "User", "User talk", name,
				name + " talk", "Image", "Image talk", "MediaWiki", "MediaWiki talk", "Template",
				"Template talk", "Help", "Help talk", "Category", "Category talk" };
            for (int i = -2, j = 0; i < 16; i++, j++)
                wikiNSpaces.Add(i.ToString(), wikiNSNames[j]);
            wikiNSpaces.Remove("0");

            WMSites.Add("w", "wikipedia"); WMSites.Add("wikt", "wiktionary");
            WMSites.Add("b", "wikibooks"); WMSites.Add("n", "wikinews");
            WMSites.Add("q", "wikiquote"); WMSites.Add("s", "wikisource");
            foreach (DictionaryEntry s in WMSites)
                WMSitesStr += s.Key + "|" + s.Value + "|";

            // Revised 2010-07-02
            mediaWikiVars = new string[] { "currentmonth","currentmonthname","currentmonthnamegen",
				"currentmonthabbrev","currentday2","currentdayname","currentyear","currenttime",
				"currenthour","localmonth","localmonthname","localmonthnamegen","localmonthabbrev",
				"localday","localday2","localdayname","localyear","localtime","localhour",
				"numberofarticles","numberoffiles","sitename","server","servername","scriptpath",
				"pagename","pagenamee","fullpagename","fullpagenamee","namespace","namespacee",
				"currentweek","currentdow","localweek","localdow","revisionid","revisionday",
				"revisionday2","revisionmonth","revisionyear","revisiontimestamp","subpagename",
				"subpagenamee","talkspace","talkspacee","subjectspace","dirmark","directionmark",
				"subjectspacee","talkpagename","talkpagenamee","subjectpagename","subjectpagenamee",
				"numberofusers","rawsuffix","newsectionlink","numberofpages","currentversion",
				"basepagename","basepagenamee","urlencode","currenttimestamp","localtimestamp",
				"directionmark","language","contentlanguage","pagesinnamespace","numberofadmins",
				"currentday","numberofarticles:r","numberofpages:r","magicnumber",
				"numberoffiles:r", "numberofusers:r", "numberofadmins:r", "numberofactiveusers",
				"numberofactiveusers:r" };
            parserFunctions = new string[] { "ns:", "localurl:", "localurle:", "urlencode:",
				"anchorencode:", "fullurl:", "fullurle:",  "grammar:", "plural:", "lc:", "lcfirst:",
				"uc:", "ucfirst:", "formatnum:", "padleft:", "padright:", "#language:",
				"displaytitle:", "defaultsort:", "#if:", "#if:", "#switch:", "#ifexpr:",
				"numberingroup:", "pagesinns:", "pagesincat:", "pagesincategory:", "pagesize:",
				"gender:", "filepath:", "#special:", "#tag:" };
            templateModifiers = new string[] { ":", "int:", "msg:", "msgnw:", "raw:", "subst:" };
            // Revised 2010-07-02
            iwikiLinksOrderByLocalFW = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "as", "ast", "gn", "av", "ay", "az", "id", "ms",
				"bm", "bn", "zh-min-nan", "nan", "map-bms", "jv", "su", "ba", "be",
				"be-x-old", "bh", "bcl", "bi", "bar", "bo", "bs", "br", "bug", "bg",
				"bxr", "ca", "ceb", "cv", "cs", "ch", "cbk-zam", "ny", "sn", "tum",
				"cho", "co", "cy", "da", "dk", "pdc", "de", "dv", "nv", "dsb", "na",
				"dz", "mh", "et", "el", "eml", "en", "myv", "es", "eo", "ext", "eu",
				"ee", "fa", "hif", "fo", "fr", "fy", "ff", "fur", "ga", "gv", "sm",
				"gd", "gl", "gan", "ki", "glk", "gu", "got", "hak", "xal", "ko",
				"ha", "haw", "hy", "hi", "ho", "hsb", "hr", "io", "ig", "ilo",
				"bpy", "ia", "ie", "iu", "ik", "os", "xh", "zu", "is", "it", "he",
				"kl", "kn", "kr", "pam", "ka", "ks", "csb", "kk", "kw", "rw", "ky",
				"rn", "sw", "kv", "kg", "ht", "ku", "kj", "lad", "lbe", "lo", "la",
				"lv", "to", "lb", "lt", "lij", "li", "ln", "jbo", "lg", "lmo", "hu",
				"mk", "mg", "ml", "krc", "mt", "mi", "mr", "arz", "mzn", "cdo",
				"mwl", "mdf", "mo", "mn", "mus", "my", "nah", "fj", "nl", "nds-nl",
				"cr", "ne", "new", "ja", "nap", "ce", "pih", "no", "nb", "nn",
				"nrm", "nov", "ii", "oc", "mhr", "or", "om", "ng", "hz", "uz", "pa",
				"pi", "pag", "pnb", "pap", "ps", "km", "pcd", "pms", "nds", "pl",
				"pnt", "pt", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy", "rm",
				"qu", "ru", "sah", "se", "sa", "sg", "sc", "sco", "stq", "st", "tn",
				"sq", "scn", "si", "simple", "sd", "ss", "sk", "sl", "cu", "szl",
				"so", "ckb", "srn", "sr", "sh", "fi", "sv", "tl", "ta", "kab",
				"roa-tara", "tt", "te", "tet", "th", "vi", "ti", "tg", "tpi",
				"tokipona", "tp", "chr", "chy", "ve", "tr", "tk", "tw", "udm", "uk",
				"ur", "ug", "za", "vec", "vo", "fiu-vro", "wa", "zh-classical",
				"vls", "war", "wo", "wuu", "ts", "yi", "yo", "zh-yue", "diq", "zea",
				"bat-smg", "zh", "zh-tw", "zh-cn"
			};
            iwikiLinksOrderByLocal = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "as", "ast", "gn", "av", "ay", "az", "bm", "bn",
				"zh-min-nan", "nan", "map-bms", "ba", "be", "be-x-old", "bh", "bcl",
				"bi", "bar", "bo", "bs", "br", "bg", "bxr", "ca", "cv", "ceb", "cs",
				"ch", "cbk-zam", "ny", "sn", "tum", "cho", "co", "cy", "da", "dk",
				"pdc", "de", "dv", "nv", "dsb", "dz", "mh", "et", "el", "eml", "en",
				"myv", "es", "eo", "ext", "eu", "ee", "fa", "hif", "fo", "fr", "fy",
				"ff", "fur", "ga", "gv", "gd", "gl", "gan", "ki", "glk", "gu",
				"got", "hak", "xal", "ko", "ha", "haw", "hy", "hi", "ho", "hsb",
				"hr", "io", "ig", "ilo", "bpy", "id", "ia", "ie", "iu", "ik", "os",
				"xh", "zu", "is", "it", "he", "jv", "kl", "kn", "kr", "pam", "krc",
				"ka", "ks", "csb", "kk", "kw", "rw", "ky", "rn", "sw", "kv", "kg",
				"ht", "ku", "kj", "lad", "lbe", "lo", "la", "lv", "lb", "lt", "lij",
				"li", "ln", "jbo", "lg", "lmo", "hu", "mk", "mg", "ml", "mt", "mi",
				"mr", "arz", "mzn", "ms", "cdo", "mwl", "mdf", "mo", "mn", "mus",
				"my", "nah", "na", "fj", "nl", "nds-nl", "cr", "ne", "new", "ja",
				"nap", "ce", "pih", "no", "nb", "nn", "nrm", "nov", "ii", "oc",
				"mhr", "or", "om", "ng", "hz", "uz", "pa", "pi", "pag", "pnb",
				"pap", "ps", "km", "pcd", "pms", "tpi", "nds", "pl", "tokipona",
				"tp", "pnt", "pt", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy",
				"rm", "qu", "ru", "sah", "se", "sm", "sa", "sg", "sc", "sco", "stq",
				"st", "tn", "sq", "scn", "si", "simple", "sd", "ss", "sk", "cu",
				"sl", "szl", "so", "ckb", "srn", "sr", "sh", "su", "fi", "sv", "tl",
				"ta", "kab", "roa-tara", "tt", "te", "tet", "th", "ti", "tg", "to",
				"chr", "chy", "ve", "tr", "tk", "tw", "udm", "bug", "uk", "ur",
				"ug", "za", "vec", "vi", "vo", "fiu-vro", "wa", "zh-classical",
				"vls", "war", "wo", "wuu", "ts", "yi", "yo", "zh-yue", "diq", "zea",
				"bat-smg", "zh", "zh-tw", "zh-cn"
			};
            iwikiLinksOrderByLatinFW = new string[] {
				"ace", "af", "ak", "als", "am", "ang", "ab", "ar", "an", "arc",
				"roa-rup", "frp", "arz", "as", "ast", "gn", "av", "ay", "az", "id",
				"ms", "bg", "bm", "zh-min-nan", "nan", "map-bms", "jv", "su", "ba",
				"be", "be-x-old", "bh", "bcl", "bi", "bn", "bo", "bar", "bs", "bpy",
				"br", "bug", "bxr", "ca", "ceb", "ch", "cbk-zam", "sn", "tum", "ny",
				"cho", "chr", "co", "cy", "cv", "cs", "da", "dk", "pdc", "de", "nv",
				"dsb", "na", "dv", "dz", "mh", "et", "el", "eml", "en", "myv", "es",
				"eo", "ext", "eu", "ee", "fa", "hif", "fo", "fr", "fy", "ff", "fur",
				"ga", "gv", "sm", "gd", "gl", "gan", "ki", "glk", "got", "gu", "ha",
				"hak", "xal", "haw", "he", "hi", "ho", "hsb", "hr", "hy", "io",
				"ig", "ii", "ilo", "ia", "ie", "iu", "ik", "os", "xh", "zu", "is",
				"it", "ja", "ka", "kl", "kr", "pam", "krc", "csb", "kk", "kw", "rw",
				"ky", "rn", "sw", "km", "kn", "ko", "kv", "kg", "ht", "ks", "ku",
				"kj", "lad", "lbe", "la", "lv", "to", "lb", "lt", "lij", "li", "ln",
				"lo", "jbo", "lg", "lmo", "hu", "mk", "mg", "mt", "mi", "cdo",
				"mwl", "ml", "mdf", "mo", "mn", "mr", "mus", "my", "mzn", "nah",
				"fj", "ne", "nl", "nds-nl", "cr", "new", "nap", "ce", "pih", "no",
				"nb", "nn", "nrm", "nov", "oc", "mhr", "or", "om", "ng", "hz", "uz",
				"pa", "pag", "pap", "pi", "pcd", "pms", "nds", "pnb", "pl", "pt",
				"pnt", "ps", "aa", "kaa", "crh", "ty", "ksh", "ro", "rmy", "rm",
				"qu", "ru", "sa", "sah", "se", "sg", "sc", "sco", "sd", "stq", "st",
				"tn", "sq", "si", "scn", "simple", "ss", "sk", "sl", "cu", "szl",
				"so", "ckb", "srn", "sr", "sh", "fi", "sv", "ta", "tl", "kab",
				"roa-tara", "tt", "te", "tet", "th", "ti", "vi", "tg", "tokipona",
				"tp", "tpi", "chy", "ve", "tr", "tk", "tw", "udm", "uk", "ur", "ug",
				"za", "vec", "vo", "fiu-vro", "wa", "vls", "war", "wo", "wuu", "ts",
				"yi", "yo", "diq", "zea", "zh", "zh-tw", "zh-cn", "zh-classical",
				"zh-yue", "bat-smg"
			};
            botQueryLists.Add("allpages", "ap"); botQueryLists.Add("alllinks", "al");
            botQueryLists.Add("allusers", "au"); botQueryLists.Add("backlinks", "bl");
            botQueryLists.Add("categorymembers", "cm"); botQueryLists.Add("embeddedin", "ei");
            botQueryLists.Add("imageusage", "iu"); botQueryLists.Add("logevents", "le");
            botQueryLists.Add("recentchanges", "rc"); botQueryLists.Add("usercontribs", "uc");
            botQueryLists.Add("watchlist", "wl"); botQueryLists.Add("exturlusage", "eu");
            botQueryProps.Add("info", "in"); botQueryProps.Add("revisions", "rv");
            botQueryProps.Add("links", "pl"); botQueryProps.Add("langlinks", "ll");
            botQueryProps.Add("images", "im"); botQueryProps.Add("imageinfo", "ii");
            botQueryProps.Add("templates", "tl"); botQueryProps.Add("categories", "cl");
            botQueryProps.Add("extlinks", "el"); botQueryLists.Add("search", "sr");
        }

        #endregion

        #region login

        /// <summary>Logs in and retrieves cookies.</summary>
        public void LogIn()
        {
            string loginPageSrc = PostDataAndGetResultHTM(site + indexPath +
                "index.php?title=Special:Userlogin", "", true, true);
            string loginToken = "";
            int loginTokenPos = loginPageSrc.IndexOf(
                "<input type=\"hidden\" name=\"wpLoginToken\" value=\"");
            if (loginTokenPos != -1)
                loginToken = loginPageSrc.Substring(loginTokenPos + 48, 32);

            string postData = string.Format("wpName={0}&wpPassword={1}&wpDomain={2}" +
                "&wpLoginToken={3}&wpRemember=1&wpLoginattempt=Log+in",
                HttpUtility.UrlEncode(userName), HttpUtility.UrlEncode(userPass),
                HttpUtility.UrlEncode(userDomain), HttpUtility.UrlEncode(loginToken));
            string respStr = PostDataAndGetResultHTM(site + indexPath +
                "index.php?title=Special:Userlogin&action=submitlogin&type=login",
                postData, true, false);
            if (respStr.Contains("<div class=\"errorbox\">"))
                throw new WikiBotException(
                    "\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
            Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
        }

        /// <summary>Logs in via api.php and retrieves cookies.</summary>
        public void LogInViaApi()
        {
            string postData = string.Format("lgname={0}&lgpassword={1}&lgdomain={2}",
                HttpUtility.UrlEncode(userName), HttpUtility.UrlEncode(userPass),
                HttpUtility.UrlEncode(userDomain));
            string respStr = PostDataAndGetResultHTM(site + indexPath +
                "api.php?action=login&format=xml", postData, true, false);
            if (respStr.Contains("result=\"Success\""))
            {
                Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
                return;
            }

            int tokenPos = respStr.IndexOf("token=\"");
            if (tokenPos < 1)
                throw new WikiBotException(
                    "\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
            string loginToken = respStr.Substring(tokenPos + 7, 32);
            postData += "&lgtoken=" + HttpUtility.UrlEncode(loginToken);
            respStr = PostDataAndGetResultHTM(site + indexPath +
                "api.php?action=login&format=xml", postData, true, false);
            if (!respStr.Contains("result=\"Success\""))
                throw new WikiBotException(
                    "\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
            Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
        }

        /// <summary>Logs in SourceForge.net and retrieves cookies for work with
        /// SourceForge-hosted wikis. That's a special version of LogIn() function.</summary>
        public void LogInSourceForge()
        {
            string postData = string.Format("form_loginname={0}&form_pw={1}" +
                "&ssl_status=&form_rememberme=yes&login=Log+in",
                HttpUtility.UrlEncode(userName.ToLower()), HttpUtility.UrlEncode(userPass));
            string respStr = PostDataAndGetResultHTM("https://sourceforge.net/account/login.php",
                postData, true, false);
            if (respStr.Contains(" class=\"error\""))
                throw new WikiBotException(
                    "\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
            Console.WriteLine(Bot.Msg("Logged in SourceForge as {0}."), userName);
        }

        #endregion

        /// <summary>Gets the list of Wikimedia Foundation wiki sites and ISO 639-1
        /// language codes, used as prefixes.</summary>
        public void GetWikimediaWikisList()
        {
            Uri wikimediaMeta = new Uri("http://meta.wikimedia.org/wiki/Special:SiteMatrix");
            string respStr = Bot.GetWebResource(wikimediaMeta, "");
            Regex langCodeRE = new Regex("<a id=\"([^\"]+?)\"");
            Regex siteCodeRE = new Regex("<li><a href=\"[^\"]+?\">([^\\s]+?)<");
            MatchCollection langMatches = langCodeRE.Matches(respStr);
            MatchCollection siteMatches = siteCodeRE.Matches(respStr);
            foreach (Match m in langMatches)
                WMLangsStr += Regex.Escape(HttpUtility.HtmlDecode(m.Groups[1].ToString())) + "|";
            WMLangsStr = WMLangsStr.Remove(WMLangsStr.Length - 1);
            foreach (Match m in siteMatches)
                WMSitesStr += Regex.Escape(HttpUtility.HtmlDecode(m.Groups[1].ToString())) + "|";
            WMSitesStr += "m";
            Site.iwikiLinkRE = new Regex(@"(?i)\[\[((" + WMLangsStr + "):(.+?))]]\r?\n?");
            Site.iwikiDispLinkRE = new Regex(@"(?i)\[\[:((" + WMLangsStr + "):(.+?))]]");
            Site.sisterWikiLinkRE = new Regex(@"(?i)\[\[((" + WMSitesStr + "):(.+?))]]");
        }

        #region htm

        /// <summary>This internal function gets the hypertext markup (HTM) of wiki-page.</summary>
        /// <param name="pageURL">Absolute or relative URL of page to get.</param>
        /// <returns>Returns HTM source code.</returns>
        public string GetPageHTM(string pageURL)
        {
            return PostDataAndGetResultHTM(pageURL, "", false, true);
        }

        /// <summary>This internal function posts specified string to requested resource
        /// and gets the result hypertext markup (HTM).</summary>
        /// <param name="pageURL">Absolute or relative URL of page to get.</param>
        /// <param name="postData">String to post to site with web request.</param>
        /// <returns>Returns code of hypertext markup (HTM).</returns>
        public string PostDataAndGetResultHTM(string pageURL, string postData)
        {
            return PostDataAndGetResultHTM(pageURL, postData, false, true);
        }

        /// <summary>This internal function posts specified string to requested resource
        /// and gets the result hypertext markup (HTM).</summary>
        /// <param name="pageURL">Absolute or relative URL of page to get.</param>
        /// <param name="postData">String to post to site with web request.</param>
        /// <param name="getCookies">If set to true, gets cookies from web response and
        /// saves it in site.cookies container.</param>
        /// <param name="allowRedirect">Allow auto-redirection of web request by server.</param>
        /// <returns>Returns code of hypertext markup (HTM).</returns>
        public string PostDataAndGetResultHTM(string pageURL, string postData, bool getCookies,
            bool allowRedirect)
        {
            if (string.IsNullOrEmpty(pageURL))
                throw new WikiBotException(Bot.Msg("No URL specified."));
            // Bene: geändert
            if (!pageURL.StartsWith(site) && !site.Contains("sourceforge") && !pageURL.Contains("http"))
                pageURL = site + pageURL;
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(pageURL);
            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webReq.UseDefaultCredentials = true;
            webReq.ContentType = Bot.webContentType;
            webReq.UserAgent = Bot.botVer;
            webReq.AllowAutoRedirect = allowRedirect;
            if (cookies.Count == 0)
                webReq.CookieContainer = new CookieContainer();
            else
                webReq.CookieContainer = cookies;
            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {
                webReq.ProtocolVersion = HttpVersion.Version10;
                webReq.KeepAlive = false;
            }
            webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            if (!string.IsNullOrEmpty(postData))
            {
                if (Bot.isRunningOnMono)	// Mono bug 636219 evasion
                    webReq.AllowAutoRedirect = false;
                // https://bugzilla.novell.com/show_bug.cgi?id=636219
                webReq.Method = "POST";
                //webReq.Timeout = 180000;
                byte[] postBytes = Encoding.UTF8.GetBytes(postData);
                webReq.ContentLength = postBytes.Length;
                Stream reqStrm = webReq.GetRequestStream();
                reqStrm.Write(postBytes, 0, postBytes.Length);
                reqStrm.Close();
            }
            HttpWebResponse webResp = null;
            for (int errorCounter = 0; true; errorCounter++)
            {
                try
                {
                    
                    webResp = (HttpWebResponse)webReq.GetResponse();
                    break;
                }
                catch (WebException e)
                {
                    string message = e.Message;
                    if (webReq.AllowAutoRedirect == false &&
                        webResp.StatusCode == HttpStatusCode.Redirect)	// Mono bug 636219 evasion
                        return "";
                    if (Regex.IsMatch(message, ": \\(50[02349]\\) "))
                    {		// Remote problem
                        if (errorCounter > Bot.retryTimes)
                            throw;
                        Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));
                        Thread.Sleep(60000);
                    }
                    else if (message.Contains("Section=ResponseStatusLine"))
                    {	// Squid problem
                        Bot.SwitchUnsafeHttpHeaderParsing(true);
                        //Console.Write("|");
                        return PostDataAndGetResultHTM(pageURL, postData, getCookies,
                            allowRedirect);
                    }
                    else
                        throw;
                }
            }
            Stream respStream = webResp.GetResponseStream();
            if (webResp.ContentEncoding.ToLower().Contains("gzip"))
                respStream = new GZipStream(respStream, CompressionMode.Decompress);
            else if (webResp.ContentEncoding.ToLower().Contains("deflate"))
                respStream = new DeflateStream(respStream, CompressionMode.Decompress);
            if (getCookies == true)
            {
                Uri siteUri = new Uri(site);
                foreach (Cookie cookie in webResp.Cookies)
                {
                    if (cookie.Domain[0] == '.' &&
                        cookie.Domain.Substring(1) == siteUri.Host)
                        cookie.Domain = cookie.Domain.TrimStart(new char[] { '.' });
                    cookies.Add(cookie);
                }
            }
            StreamReader strmReader = new StreamReader(respStream, encoding);
            string respStr = strmReader.ReadToEnd();
            strmReader.Close();
            webResp.Close();
            return respStr;
        }

        #endregion

        /// <summary>This internal function deletes everything before startTag and everything after
        /// endTag. Optionally it can insert back the DOCTYPE definition and root element of
        /// XML/XHTML documents.</summary>
        /// <param name="text">Source text.</param>
        /// <param name="startTag">The beginning of returned content.</param>
        /// <param name="endTag">The end of returned content.</param>
        /// <param name="removeTags">If true, tags will also be removed.</param>
        /// <param name="leaveHead">If true, DOCTYPE definition and root element will be left
        /// intact.</param>
        /// <returns>Returns stripped content.</returns>
        public string StripContent(string text, string startTag, string endTag,
            bool removeTags, bool leaveHead)
        {
            if (string.IsNullOrEmpty(startTag))
                startTag = "<!-- bodytext -->";
            if (startTag == "<!-- bodytext -->" && ver < new Version(1, 16))
                startTag = "<!-- start content -->";

            if (startTag == "<!-- bodytext -->" && string.IsNullOrEmpty(endTag))
                endTag = "<!-- /bodytext -->";
            else if (startTag == "<!-- content -->" && string.IsNullOrEmpty(endTag))
                endTag = "<!-- /content -->";
            else if (startTag == "<!-- bodyContent -->" && string.IsNullOrEmpty(endTag))
                endTag = "<!-- /bodyContent -->";
            else if (startTag == "<!-- start content -->" && string.IsNullOrEmpty(endTag))
                endTag = "<!-- end content -->";

            if (text[0] != '<')
                text = text.Trim();

            string headText = "";
            string rootEnd = "";
            if (leaveHead == true)
            {
                int headEndPos = ((text.StartsWith("<!") || text.StartsWith("<?"))
                    && text.IndexOf('>') != -1) ? text.IndexOf('>') + 1 : 0;
                if (text.IndexOf('>', headEndPos) != -1)
                    headEndPos = text.IndexOf('>', headEndPos) + 1;
                headText = text.Substring(0, headEndPos);
                int rootEndPos = text.LastIndexOf("</");
                if (rootEndPos == -1)
                    headText = "";
                else
                    rootEnd = text.Substring(rootEndPos);
            }

            int startPos = text.IndexOf(startTag) + (removeTags == true ? startTag.Length : 0);
            int endPos = text.IndexOf(endTag) + (removeTags == false ? endTag.Length : 0);
            if (startPos == -1 || endPos == -1 || endPos < startPos)
                return headText + text + rootEnd;
            else
                return headText + text.Substring(startPos, endPos - startPos) + rootEnd;
        }

        /// <summary>This internal function constructs XPathDocument, makes XPath query and
        /// returns XPathNodeIterator for selected nodes.</summary>
        /// <param name="xmlSource">Source XML data.</param>
        /// <param name="xpathQuery">XPath query to select specific nodes in XML data.</param>
        /// <returns>XPathNodeIterator object.</returns>
        public XPathNodeIterator GetXMLIterator(string xmlSource, string xpathQuery)
        {
            XmlReader reader = GetXMLReader(xmlSource);
            XPathDocument doc = new XPathDocument(reader);
            XPathNavigator nav = doc.CreateNavigator();
            return nav.Select(xpathQuery, xmlNS);
        }

        /// <summary>This internal function constructs and returns XmlReader object.</summary>
        /// <param name="xmlSource">Source XML data.</param>
        /// <returns>XmlReader object.</returns>
        public XmlReader GetXMLReader(string xmlSource)
        {
            StringReader strReader = new StringReader(xmlSource);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlUrlResolverWithCache();
            settings.CheckCharacters = false;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.ProhibitDtd = false;
            return XmlReader.Create(strReader, settings);
        }

        #region NSprefix

        /// <summary>This internal function removes the namespace prefix from page title.</summary>
        /// <param name="pageTitle">Page title to remove prefix from.</param>
        /// <param name="nsIndex">Index of namespace to remove. If this parameter is 0,
        /// any found namespace prefix is removed.</param>
        /// <returns>Page title without prefix.</returns>
        public string RemoveNSPrefix(string pageTitle, int nsIndex)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (nsIndex != 0)
            {
                if (wikiNSpaces[nsIndex.ToString()] != null)
                    pageTitle = Regex.Replace(pageTitle, "(?i)^" +
                        Regex.Escape(wikiNSpaces[nsIndex.ToString()].ToString()) + ":", "");
                if (namespaces[nsIndex.ToString()] != null)
                    pageTitle = Regex.Replace(pageTitle, "(?i)^" +
                        Regex.Escape(namespaces[nsIndex.ToString()].ToString()) + ":", "");
                return pageTitle;
            }
            foreach (DictionaryEntry ns in wikiNSpaces)
            {
                if (ns.Value == null)
                    continue;
                pageTitle = Regex.Replace(pageTitle, "(?i)^" +
                    Regex.Escape(ns.Value.ToString()) + ":", "");
            }
            foreach (DictionaryEntry ns in namespaces)
            {
                if (ns.Value == null)
                    continue;
                pageTitle = Regex.Replace(pageTitle, "(?i)^" +
                    Regex.Escape(ns.Value.ToString()) + ":", "");
            }
            return pageTitle;
        }

        /// <summary>Function changes default English namespace prefixes to correct local prefixes
        /// (e.g. for German wiki-sites it changes "Category:..." to "Kategorie:...").</summary>
        /// <param name="pageTitle">Page title to correct prefix in.</param>
        /// <returns>Page title with corrected prefix.</returns>
        public string CorrectNSPrefix(string pageTitle)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            foreach (DictionaryEntry ns in wikiNSpaces)
            {
                if (ns.Value == null)
                    continue;
                if (Regex.IsMatch(pageTitle, "(?i)" + Regex.Escape(ns.Value.ToString()) + ":"))
                    pageTitle = namespaces[ns.Key] + pageTitle.Substring(pageTitle.IndexOf(":"));
            }
            return pageTitle;
        }

        #endregion

        #region template

        /// <summary>Parses the provided template body and returns the key/value pairs of it's
        /// parameters titles and values. Everything inside the double braces must be passed to
        /// this function, so first goes the template's title, then '|' character, and then go the
        /// parameters. Please, see the usage example.</summary>
        /// <param name="template">Complete template's body including it's title, but not
        /// including double braces.</param>
        /// <returns>Returns the Dictionary &lt;string, string&gt; object, where keys are parameters
        /// titles and values are parameters values. If parameter is untitled, it's number is
        /// returned as the (string) dictionary key. If parameter value is set several times in the
        /// template (normally that shouldn't occur), only the last value is returned. Template's
        /// title is not returned as a parameter.</returns>
        /// <example><code>
        /// Dictionary &lt;string, string&gt; parameters1 =
        /// 	site.ParseTemplate("TemplateTitle|param1=val1|param2=val2");
        /// string[] templates = page.GetTemplatesWithParams();
        /// Dictionary &lt;string, string&gt; parameters2 = site.ParseTemplate(templates[0]);
        /// parameters1["param2"] = "newValue";
        /// </code></example>
        public Dictionary<string, string> ParseTemplate(string template)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentNullException("template");
            if (template.StartsWith("{{"))
                template = template.Substring(2, template.Length - 4);

            int startPos, endPos, len = 0;
            string str = template;

            while ((startPos = str.LastIndexOf("{{")) != -1)
            {
                endPos = str.IndexOf("}}", startPos);
                len = (endPos != -1) ? endPos - startPos + 2 : 2;
                str = str.Remove(startPos, len);
                str = str.Insert(startPos, new String('_', len));
            }

            while ((startPos = str.LastIndexOf("[[")) != -1)
            {
                endPos = str.IndexOf("]]", startPos);
                len = (endPos != -1) ? endPos - startPos + 2 : 2;
                str = str.Remove(startPos, len);
                str = str.Insert(startPos, new String('_', len));
            }

            List<int> separators = Bot.GetMatchesPositions(str, "|", false);
            if (separators == null || separators.Count == 0)
                return new Dictionary<string, string>();
            List<string> parameters = new List<string>();
            endPos = template.Length;
            for (int i = separators.Count - 1; i >= 0; i--)
            {
                parameters.Add(template.Substring(separators[i] + 1, endPos - separators[i] - 1));
                endPos = separators[i];
            }
            parameters.Reverse();

            Dictionary<string, string> templateParams = new Dictionary<string, string>();
            for (int pos, i = 0; i < parameters.Count; i++)
            {
                pos = parameters[i].IndexOf('=');
                if (pos == -1)
                    templateParams[i.ToString()] = parameters[i].Trim();
                else
                    templateParams[parameters[i].Substring(0, pos).Trim()] =
                        parameters[i].Substring(pos + 1).Trim();
            }
            return templateParams;
        }

        /// <summary>Formats a template with the specified title and parameters. Default formatting
        /// options are used.</summary>
        /// <param name="templateTitle">Template's title.</param>
        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
        /// object, where keys are parameters titles and values are parameters values.</param>
        /// <returns>Returns the complete template in double braces.</returns>
        public string FormatTemplate(string templateTitle,
            Dictionary<string, string> templateParams)
        {
            return FormatTemplate(templateTitle, templateParams, false, false, 0);
        }

        /// <summary>Formats a template with the specified title and parameters. Formatting
        /// options are got from provided reference template. That function is usually used to
        /// format modified template as it was in it's initial state, though absolute format
        /// consistency can not be guaranteed.</summary>
        /// <param name="templateTitle">Template's title.</param>
        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
        /// object, where keys are parameters titles and values are parameters values.</param>
        /// <param name="referenceTemplate">Full template body to detect formatting options in.
        /// With or without double braces.</param>
        /// <returns>Returns the complete template in double braces.</returns>
        public string FormatTemplate(string templateTitle,
            Dictionary<string, string> templateParams, string referenceTemplate)
        {
            if (string.IsNullOrEmpty(referenceTemplate))
                throw new ArgumentNullException("referenceTemplate");

            bool inline = false;
            bool withoutSpaces = false;
            int padding = 0;

            if (!referenceTemplate.Contains("\n"))
                inline = true;
            if (!referenceTemplate.Contains(" ") && !referenceTemplate.Contains("\t"))
                withoutSpaces = true;
            if (withoutSpaces == false && referenceTemplate.Contains("  ="))
                padding = -1;

            return FormatTemplate(templateTitle, templateParams, inline, withoutSpaces, padding);
        }

        /// <summary>Formats a template with the specified title and parameters, allows extended
        /// format options to be specified.</summary>
        /// <param name="templateTitle">Template's title.</param>
        /// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
        /// object, where keys are parameters titles and values are parameters values.</param>
        /// <param name="inline">When set to true, template is formatted in one line, without any
        /// line breaks. Default value is false.</param>
        /// <param name="withoutSpaces">When set to true, template is formatted without spaces.
        /// Default value is false.</param>
        /// <param name="padding">When set to positive value, template parameters titles are padded
        /// on the right with specified number of spaces, so "=" characters could form a nice
        /// straight column. When set to -1, the number of spaces is calculated automatically.
        /// Default value is 0 (no padding). The padding will occur only when "inline" option
        /// is set to false and "withoutSpaces" option is also set to false.</param>
        /// <returns>Returns the complete template in double braces.</returns>
        public string FormatTemplate(string templateTitle,
            Dictionary<string, string> templateParams, bool inline, bool withoutSpaces, int padding)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            if (templateParams == null || templateParams.Count == 0)
                throw new ArgumentNullException("templateParams");

            if (inline != false || withoutSpaces != false)
                padding = 0;
            if (padding == -1)
                foreach (KeyValuePair<string, string> kvp in templateParams)
                    if (kvp.Key.Length > padding)
                        padding = kvp.Key.Length;

            int i = 1;
            string template = "{{" + templateTitle;
            foreach (KeyValuePair<string, string> kvp in templateParams)
            {
                template += "\n| ";
                if (padding <= 0)
                {
                    if (kvp.Key == i.ToString())
                        template += kvp.Value;
                    else
                        template += kvp.Key + " = " + kvp.Value;
                }
                else
                {
                    if (kvp.Key == i.ToString())
                        template += kvp.Value.PadRight(padding + 3);
                    else
                        template += kvp.Key.PadRight(padding) + " = " + kvp.Value;
                }
                i++;
            }
            template += "\n}}";

            if (inline == true)
                template = template.Replace("\n", " ");
            if (withoutSpaces == true)
                template = template.Replace(" ", "");
            return template;
        }

        #endregion

        /// <summary>Shows names and integer keys of local and default namespaces.</summary>
        public void ShowNamespaces()
        {
            foreach (DictionaryEntry ns in namespaces)
            {
                Console.WriteLine(ns.Key.ToString() + "\t" + ns.Value.ToString().PadRight(20) +
                    "\t" + wikiNSpaces[ns.Key.ToString()]);
            }
        }
    }
}
	