// DotNetWikiBot Framework 2.101 - bot framework based on Microsoft .NET Framework 2.0 for wiki projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright (c) Iaroslav Vassiliev (2006-2012) codedriller@gmail.com

// DotNetDataBot Framework 1.3 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

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

namespace DotNetDataBot
{
    /// <summary>Class defines a set of wiki pages (constructed inside as List object).</summary>
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [Serializable]
    public class PageList
    {
        #region variables

        /// <summary>Internal generic List, that contains collection of pages.</summary>
        public List<Page> pages = new List<Page>();
        /// <summary>Site, on which the pages are located.</summary>
        public Site site;

        #endregion

        #region construction

        /// <summary>This constructor creates PageList object with specified Site object and fills
        /// it with Page objects with specified titles. When constructed, new Page objects
        /// in PageList don't contain text. Use Load() method to get text from live wiki,
        /// or use LoadEx() to get both text and metadata via XML export interface.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <param name="pageNames">Page titles as array of strings.</param>
        /// <returns>Returns the PageList object.</returns>
        public PageList(Site site, string[] pageNames)
        {
            this.site = site;
            foreach (string pageName in pageNames)
                pages.Add(new Page(site, pageName));
            CorrectNSPrefixes();
        }

        /// <summary>This constructor creates PageList object with specified Site object and fills
        /// it with Page objects with specified titles. When constructed, new Page objects
        /// in PageList don't contain text. Use Load() method to get text from live wiki,
        /// or use LoadEx() to get both text and metadata via XML export interface.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <param name="pageNames">Page titles as StringCollection object.</param>
        /// <returns>Returns the PageList object.</returns>
        public PageList(Site site, StringCollection pageNames)
        {
            this.site = site;
            foreach (string pageName in pageNames)
                pages.Add(new Page(site, pageName));
            CorrectNSPrefixes();
        }

        /// <summary>This constructor creates empty PageList object with specified
        /// Site object.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <returns>Returns the PageList object.</returns>
        public PageList(Site site)
        {
            this.site = site;
        }

        /// <summary>This constructor creates empty PageList object, Site object with default
        /// properties is created internally and logged in. Constructing new Site object
        /// is too slow, don't use this constructor needlessly.</summary>
        /// <returns>Returns the PageList object.</returns>
        public PageList()
        {
            site = new Site();
        }

        #endregion

        #region pages

        /// <summary>This index allows to call pageList[i] instead of pageList.pages[i].</summary>
        /// <param name="index">Zero-based index.</param>
        /// <returns>Returns the Page object.</returns>
        public Page this[int index]
        {
            get { return pages[index]; }
            set { pages[index] = value; }
        }

        /// <summary>This function allows to access individual pages in this PageList.
        /// But it's better to use simple pageList[i] index, when it is possible.</summary>
        /// <param name="index">Zero-based index.</param>
        /// <returns>Returns the Page object.</returns>
        public Page GetPageAtIndex(int index)
        {
            return pages[index];
        }

        /// <summary>This function allows to set individual pages in this PageList.
        /// But it's better to use simple pageList[i] index, when it is possible.</summary>
        /// <param name="page">Page object to set in this PageList.</param>
        /// <param name="index">Zero-based index.</param>
        /// <returns>Returns the Page object.</returns>
        public void SetPageAtIndex(Page page, int index)
        {
            pages[index] = page;
        }

        /// <summary>This index allows to call pageList["title"]. Don't forget to use correct
        /// local namespace prefixes. Call CorrectNSPrefixes function to correct namespace
        /// prefixes in a whole PageList at once.</summary>
        /// <param name="index">Title of page to get.</param>
        /// <returns>Returns the Page object, or null if there is no page with the specified
        /// title in this PageList.</returns>
        public Page this[string index]
        {
            get
            {
                foreach (Page p in pages)
                    if (p.title == index)
                        return p;
                return null;
            }
            set
            {
                for (int i = 0; i < pages.Count; i++)
                    if (pages[i].title == index)
                        pages[i] = value;
            }
        }

        #endregion

        /// <summary>This standard internal function allows to directly use PageList objects
        /// in "foreach" statements.</summary>
        /// <returns>Returns IEnumerator object.</returns>
        public IEnumerator GetEnumerator()
        {
            return pages.GetEnumerator();
        }

        /// <summary>This function adds specified page to the end of this PageList.</summary>
        /// <param name="page">Page object to add.</param>
        public void Add(Page page)
        {
            pages.Add(page);
        }

        /// <summary>Inserts an element into this PageList at the specified index.</summary>
        /// <param name="page">Page object to insert.</param>
        /// <param name="index">Zero-based index.</param>
        public void Insert(Page page, int index)
        {
            pages.Insert(index, page);
        }

        /// <summary>This function returns true, if in this PageList there exists a page with
        /// the same title, as a page specified as a parameter.</summary>
        /// <param name="page">.</param>
        /// <returns>Returns bool value.</returns>
        public bool Contains(Page page)
        {
            page.CorrectNSPrefix();
            CorrectNSPrefixes();
            foreach (Page p in pages)
                if (p.title == page.title)
                    return true;
            return false;
        }

        /// <summary>This function returns true, if a page with specified title exists
        /// in this PageList.</summary>
        /// <param name="title">Title of page to check.</param>
        /// <returns>Returns bool value.</returns>
        public bool Contains(string title)
        {
            Page page = new Page(site, title);
            page.CorrectNSPrefix();
            CorrectNSPrefixes();
            foreach (Page p in pages)
                if (p.title == page.title)
                    return true;
            return false;
        }

        /// <summary>This function returns the number of pages in PageList.</summary>
        /// <returns>Number of pages as positive integer value.</returns>
        public int Count()
        {
            return pages.Count;
        }

        /// <summary>Removes page at specified index from PageList.</summary>
        /// <param name="index">Zero-based index.</param>
        public void RemoveAt(int index)
        {
            pages.RemoveAt(index);
        }

        /// <summary>Removes a page with specified title from this PageList.</summary>
        /// <param name="title">Title of page to remove.</param>
        public void Remove(string title)
        {
            for (int i = 0; i < Count(); i++)
                if (pages[i].title == title)
                    pages.RemoveAt(i);
        }

        #region fill

        /// <summary>Gets page titles for this PageList from "Special:Allpages" MediaWiki page.
        /// That means a list of pages in alphabetical order.</summary>
        /// <param name="firstPageTitle">Title of page to start enumerating from. The title
        /// must have no namespace prefix (like "Talk:"), just the page title itself. Or you can
        /// specify just a letter or two instead of full real title. Pass the empty string or null
        /// to start from the very beginning.</param>
        /// <param name="neededNSpace">Integer, presenting the key of namespace to get pages
        /// from. Only one key of one namespace can be specified (zero for default).</param>
        /// <param name="acceptRedirects">Set this to "false" to exclude redirects.</param>
        /// <param name="quantity">Maximum allowed quantity of pages in this PageList.</param>
        public void FillFromAllPages(string firstPageTitle, int neededNSpace, bool acceptRedirects,
            int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            if (Bot.useBotQuery == true && site.botQuery == true)
            {
                FillFromCustomBotQueryList("allpages", "apnamespace=" + neededNSpace +
                (acceptRedirects ? "" : "&apfilterredir=nonredirects") +
                (string.IsNullOrEmpty(firstPageTitle) ? "" : "&apfrom=" +
                HttpUtility.UrlEncode(firstPageTitle)), quantity);
                return;
            }
            Console.WriteLine(
                Bot.Msg("Getting {0} page titles from \"Special:Allpages\" MediaWiki page..."),
                quantity);
            int count = pages.Count;
            quantity += pages.Count;
            Regex linkToPageRE;
            if (acceptRedirects)
                linkToPageRE = new Regex("<td[^>]*>(?:<div class=\"allpagesredirect\">)?" +
                    "<a href=\"[^\"]*?\" title=\"([^\"]*?)\">");
            else
                linkToPageRE = new Regex("<td[^>]*><a href=\"[^\"]*?\" title=\"([^\"]*?)\">");
            MatchCollection matches;
            do
            {
                string res = site.site + site.indexPath +
                    "index.php?title=Special:Allpages&from=" +
                    HttpUtility.UrlEncode(
                        string.IsNullOrEmpty(firstPageTitle) ? "!" : firstPageTitle) +
                    "&namespace=" + neededNSpace.ToString();
                matches = linkToPageRE.Matches(site.GetPageHTM(res));
                if (matches.Count < 2)
                    break;
                for (int i = 1; i < matches.Count; i++)
                    pages.Add(new Page(site, HttpUtility.HtmlDecode(matches[i].Groups[1].Value)));
                firstPageTitle = site.RemoveNSPrefix(pages[pages.Count - 1].title, neededNSpace) +
                    "!";
            }
            while (pages.Count < quantity);
            if (pages.Count > quantity)
                pages.RemoveRange(quantity, pages.Count - quantity);
            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +
                "\"Special:Allpages\" MediaWiki page."), (pages.Count - count).ToString());
        }

        /// <summary>Gets page titles for this PageList from specified special page,
        /// e.g. "Deadendpages". The function does not filter namespaces. And the function
        /// does not clear the existing PageList, so new titles will be added.</summary>
        /// <param name="pageTitle">Title of special page, e.g. "Deadendpages".</param>
        /// <param name="quantity">Maximum number of page titles to get. Usually
        /// MediaWiki provides not more than 1000 titles.</param>
        public void FillFromCustomSpecialPage(string pageTitle, int quantity)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            Console.WriteLine(Bot.Msg("Getting {0} page titles from \"Special:{1}\" page..."),
                quantity, pageTitle);
            string res = site.site + site.indexPath + "index.php?title=Special:" +
                HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();
            string src = site.GetPageHTM(res);
            MatchCollection matches;
            if (pageTitle == "Unusedimages" || pageTitle == "Uncategorizedimages" ||
                pageTitle == "UnusedFiles" || pageTitle == "UncategorizedFiles")
                matches = site.linkToPageRE3.Matches(src);
            else
                matches = Site.linkToPageRE2.Matches(src);
            if (matches.Count == 0)
                throw new WikiBotException(string.Format(
                    Bot.Msg("Page \"Special:{0}\" does not contain page titles."), pageTitle));
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +
                "\"Special:{1}\" page."), matches.Count, pageTitle);
        }

        /// <summary>Gets page titles for this PageList from specified special page,
        /// e.g. "Deadendpages". The function does not filter namespaces. And the function
        /// does not clear the existing PageList, so new titles will be added.
        /// The function uses XML (XHTML) parsing instead of regular expressions matching.
        /// This function is slower, than FillFromCustomSpecialPage.</summary>
        /// <param name="pageTitle">Title of special page, e.g. "Deadendpages".</param>
        /// <param name="quantity">Maximum number of page titles to get. Usually
        /// MediaWiki provides not more than 1000 titles.</param>
        public void FillFromCustomSpecialPageEx(string pageTitle, int quantity)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            Console.WriteLine(Bot.Msg("Getting {0} page titles from \"Special:{1}\" page..."),
                quantity, pageTitle);
            string res = site.site + site.indexPath + "index.php?title=Special:" +
                HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();
            string src = site.StripContent(site.GetPageHTM(res), null, null, true, true);
            XPathNodeIterator ni = site.GetXMLIterator(src, "//ns:ol/ns:li/ns:a[@title != '']");
            if (ni.Count == 0)
                throw new WikiBotException(string.Format(
                    Bot.Msg("Nothing was found on \"Special:{0}\" page."), pageTitle));
            while (ni.MoveNext())
                pages.Add(new Page(site,
                    HttpUtility.HtmlDecode(ni.Current.GetAttribute("title", ""))));
            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +
                "\"Special:{1}\" page."), ni.Count, pageTitle);
        }

        /// <summary>Gets page titles for this PageList from specified MediaWiki events log.
        /// The function does not filter namespaces. And the function does not clear the
        /// existing PageList, so new titles will be added.</summary>
        /// <param name="logType">Type of log, it could be: "block" for blocked users log;
        /// "protect" for protected pages log; "rights" for users rights log; "delete" for
        /// deleted pages log; "upload" for uploaded files log; "move" for renamed pages log;
        /// "import" for transwiki import log; "renameuser" for renamed accounts log;
        /// "newusers" for new users log; "makebot" for bot status assignment log.</param>
        /// <param name="userName">Select log entries only for specified account. Pass empty
        /// string, if that restriction is not needed.</param>
        /// <param name="pageTitle">Select log entries only for specified page. Pass empty
        /// string, if that restriction is not needed.</param>
        /// <param name="quantity">Maximum number of page titles to get.</param>
        public void FillFromCustomLog(string logType, string userName, string pageTitle,
            int quantity)
        {
            if (string.IsNullOrEmpty(logType))
                throw new ArgumentNullException("logType");
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            Console.WriteLine(Bot.Msg("Getting {0} page titles from \"{1}\" log..."),
                quantity.ToString(), logType);
            string res = site.site + site.indexPath + "index.php?title=Special:Log&type=" +
                 logType + "&user=" + HttpUtility.UrlEncode(userName) + "&page=" +
                 HttpUtility.UrlEncode(pageTitle) + "&limit=" + quantity.ToString();
            string src = site.GetPageHTM(res);
            MatchCollection matches = Site.linkToPageRE2.Matches(src);
            if (matches.Count == 0)
                throw new WikiBotException(
                    string.Format(Bot.Msg("Log \"{0}\" does not contain page titles."), logType));
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from \"{1}\" log."),
                matches.Count, logType);
        }

        /// <summary>Gets page titles for this PageList from specified list, produced by
        /// bot query interface ("api.php" MediaWiki extension). The function
        /// does not clear the existing PageList, so new titles will be added.</summary>
        /// <param name="listType">Title of list, the following values are supported: 
        /// "allpages", "alllinks", "allusers", "backlinks", "categorymembers",
        /// "embeddedin", "imageusage", "logevents", "recentchanges", 
        /// "usercontribs", "watchlist", "exturlusage". Detailed documentation
        /// can be found at "http://en.wikipedia.org/w/api.php".</param>
        /// <param name="queryParams">Additional query parameters, specific to the
        /// required list, e.g. "cmtitle=Category:Physical%20sciences&amp;cmnamespace=0|2".
        /// Parameter values must be URL-encoded with HttpUtility.UrlEncode function
        /// before calling this function.</param>
        /// <param name="quantity">Maximum number of page titles to get.</param>
        /// <example><code>
        /// pageList.FillFromCustomBotQueryList("categorymembers",
        /// 	"cmcategory=Physical%20sciences&amp;cmnamespace=0|14",
        /// 	int.MaxValue);
        /// </code></example>
        public void FillFromCustomBotQueryList(string listType, string queryParams, int quantity)
        {
            if (!site.botQuery)
                throw new WikiBotException(
                    Bot.Msg("The \"api.php\" MediaWiki extension is not available."));
            if (string.IsNullOrEmpty(listType))
                throw new ArgumentNullException("listType");
            if (!Site.botQueryLists.Contains(listType))
                throw new WikiBotException(
                    string.Format(Bot.Msg("The list \"{0}\" is not supported."), listType));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            string prefix = Site.botQueryLists[listType].ToString();
            string continueAttrTag1 = prefix + "from";
            string continueAttrTag2 = prefix + "continue";
            string attrTag = (listType != "allusers") ? "title" : "name";
            string queryUri = site.indexPath + "api.php?action=query&list=" + listType +
                "&format=xml&" + prefix + "limit=" +
                ((quantity > 500) ? "500" : quantity.ToString());
            string src = "", next = "", queryFullUri = "";
            int count = pages.Count;
            if (quantity != int.MaxValue)
                quantity += pages.Count;
            do
            {
                queryFullUri = queryUri;
                if (next != "")
                    queryFullUri += "&" + prefix + "continue=" + HttpUtility.UrlEncode(next);
                src = site.PostDataAndGetResultHTM(queryFullUri, queryParams);
                using (XmlTextReader reader = new XmlTextReader(new StringReader(src)))
                {
                    next = "";
                    while (reader.Read())
                    {
                        if (reader.IsEmptyElement && reader[attrTag] != null)
                            pages.Add(new Page(site, HttpUtility.HtmlDecode(reader[attrTag])));
                        if (reader.IsEmptyElement && reader[continueAttrTag1] != null)
                            next = reader[continueAttrTag1];
                        if (reader.IsEmptyElement && reader[continueAttrTag2] != null)
                            next = reader[continueAttrTag2];
                    }
                }
            }
            while (next != "" && pages.Count < quantity);
            if (pages.Count > quantity)
                pages.RemoveRange(quantity, pages.Count - quantity);
            if (!string.IsNullOrEmpty(Environment.StackTrace) &&
                !Environment.StackTrace.Contains("FillAllFromCategoryEx"))
                Console.WriteLine(Bot.Msg("PageList filled with {0} page titles " +
                    "from \"{1}\" bot interface list."),
                    (pages.Count - count).ToString(), listType);
        }

        /// <summary>Gets page titles for this PageList from recent changes page,
        /// "Special:Recentchanges". File uploads, page deletions and page renamings are
        /// not included, use FillFromCustomLog function instead to fill from respective logs.
        /// The function does not clear the existing PageList, so new titles will be added.
        /// Use FilterNamespaces() or RemoveNamespaces() functions to remove
        /// pages from unwanted namespaces.</summary>
        /// <param name="hideMinor">Ignore minor edits.</param>
        /// <param name="hideBots">Ignore bot edits.</param>
        /// <param name="hideAnons">Ignore anonymous users edits.</param>
        /// <param name="hideLogged">Ignore logged-in users edits.</param>
        /// <param name="hideSelf">Ignore edits of this bot account.</param>
        /// <param name="limit">Maximum number of changes to get.</param>
        /// <param name="days">Get changes for this number of recent days.</param>
        public void FillFromRecentChanges(bool hideMinor, bool hideBots, bool hideAnons,
            bool hideLogged, bool hideSelf, int limit, int days)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
            if (days <= 0)
                throw new ArgumentOutOfRangeException("days",
                    Bot.Msg("Number of days must be positive."));
            Console.WriteLine(Bot.Msg("Getting {0} page titles from " +
                "\"Special:Recentchanges\" page..."), limit);
            string uri = string.Format("{0}{1}index.php?title=Special:Recentchanges&" +
                "hideminor={2}&hideBots={3}&hideAnons={4}&hideliu={5}&hidemyself={6}&" +
                "limit={7}&days={8}", site.site, site.indexPath,
                hideMinor ? "1" : "0", hideBots ? "1" : "0", hideAnons ? "1" : "0",
                hideLogged ? "1" : "0", hideSelf ? "1" : "0",
                limit.ToString(), days.ToString());
            string respStr = site.GetPageHTM(uri);
            MatchCollection matches = Site.linkToPageRE2.Matches(respStr);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(Bot.Msg("PageList filled with {0} page titles from " +
                "\"Special:Recentchanges\" page."), matches.Count);
        }

        /// <summary>Gets page titles for this PageList from specified wiki category page, excluding
        /// subcategories. Use FillSubsFromCategory function to get subcategories.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillFromCategory(string categoryName)
        {
            int count = pages.Count;
            PageList pl = new PageList(site);
            pl.FillAllFromCategory(categoryName);
            pl.RemoveNamespaces(new int[] { 14 });
            pages.AddRange(pl.pages);
            if (pages.Count != count)
                Console.WriteLine(
                    Bot.Msg("PageList filled with {0} page titles, found in \"{1}\" category."),
                    (pages.Count - count).ToString(), categoryName);
            else
                Console.Error.WriteLine(
                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
        }

        /// <summary>Gets subcategories titles for this PageList from specified wiki category page,
        /// excluding other pages. Use FillFromCategory function to get other pages.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillSubsFromCategory(string categoryName)
        {
            int count = pages.Count;
            PageList pl = new PageList(site);
            pl.FillAllFromCategory(categoryName);
            pl.FilterNamespaces(new int[] { 14 });
            pages.AddRange(pl.pages);
            if (pages.Count != count)
                Console.WriteLine(Bot.Msg("PageList filled with {0} subcategory page titles, " +
                    "found in \"{1}\" category."), (pages.Count - count).ToString(), categoryName);
            else
                Console.Error.WriteLine(
                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
        }

        /// <summary>This internal function gets all page titles for this PageList from specified
        /// category page, including subcategories.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillAllFromCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");
            categoryName = categoryName.Trim("[]\f\n\r\t\v ".ToCharArray());
            categoryName = site.RemoveNSPrefix(categoryName, 14);
            categoryName = site.namespaces["14"] + ":" + categoryName;
            Console.WriteLine(Bot.Msg("Getting category \"{0}\" contents..."), categoryName);
            //RemoveAll();
            if (Bot.useBotQuery == true && site.botQuery == true)
            {
                FillAllFromCategoryEx(categoryName);
                return;
            }
            string src = "";
            MatchCollection matches;
            Regex nextPortionRE = new Regex("&(?:amp;)?from=([^\"=]+)\" title=\"");
            do
            {
                string res = site.site + site.indexPath + "index.php?title=" +
                    HttpUtility.UrlEncode(categoryName) +
                    "&from=" + nextPortionRE.Match(src).Groups[1].Value;
                src = site.GetPageHTM(res);
                src = HttpUtility.HtmlDecode(src);
                matches = Site.linkToPageRE1.Matches(src);
                foreach (Match match in matches)
                    pages.Add(new Page(site, match.Groups[1].Value));
                if (src.Contains("<div class=\"gallerytext\">\n"))
                {
                    matches = Site.linkToImageRE.Matches(src);
                    foreach (Match match in matches)
                        pages.Add(new Page(site, match.Groups[1].Value));
                }
                if (src.Contains("<div class=\"CategoryTreeChildren\""))
                {
                    matches = Site.linkToSubCategoryRE.Matches(src);
                    foreach (Match match in matches)
                        pages.Add(new Page(site, site.namespaces["14"] + ":" +
                            match.Groups[1].Value));
                }
            }
            while (nextPortionRE.IsMatch(src));
        }

        /// <summary>This internal function gets all page titles for this PageList from specified
        /// category using "api.php" MediaWiki extension (bot interface), if it is available.
        /// It gets subcategories too.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillAllFromCategoryEx(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");
            categoryName = categoryName.Trim("[]\f\n\r\t\v ".ToCharArray());
            categoryName = site.RemoveNSPrefix(categoryName, 14);
            if (site.botQueryVersions.ContainsKey("ApiQueryCategoryMembers.php"))
            {
                if (int.Parse(
                    site.botQueryVersions["ApiQueryCategoryMembers.php"].ToString()) >= 30533)
                    FillFromCustomBotQueryList("categorymembers", "cmtitle=" +
                        HttpUtility.UrlEncode(site.namespaces["14"].ToString() + ":" +
                        categoryName), int.MaxValue);
                else
                    FillFromCustomBotQueryList("categorymembers", "cmcategory=" +
                        HttpUtility.UrlEncode(categoryName), int.MaxValue);
            }
            else if (site.botQueryVersions.ContainsKey("query.php"))
                FillAllFromCategoryExOld(categoryName);
            else
            {
                Console.WriteLine(Bot.Msg("Can't get category members using bot interface.\n" +
                    "Switching to common user interface (\"site.botQuery\" is set to \"false\")."));
                site.botQuery = false;
                FillAllFromCategory(categoryName);
            }
        }

        /// <summary>This internal function is kept for backwards compatibility only.
        /// It gets all pages and subcategories in specified category using old obsolete 
        /// "query.php" bot interface and adds all found pages and subcategories to PageList object.
        /// It gets titles portion by portion. The "query.php" interface was superseded by
        /// "api.php" in MediaWiki 1.8.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillAllFromCategoryExOld(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");
            string src = "";
            MatchCollection matches;
            Regex nextPortionRE = new Regex("<category next=\"(.+?)\" />");
            do
            {
                string res = site.site + site.indexPath + "query.php?what=category&cptitle=" +
                    HttpUtility.UrlEncode(categoryName) + "&cpfrom=" +
                    nextPortionRE.Match(src).Groups[1].Value + "&cplimit=500&format=xml";
                src = site.GetPageHTM(res);
                matches = Site.pageTitleTagRE.Matches(src);
                foreach (Match match in matches)
                    pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            }
            while (nextPortionRE.IsMatch(src));
        }

        /// <summary>Gets all levels of subcategories of some wiki category (that means
        /// subcategories, sub-subcategories, and so on) and fills this PageList with titles
        /// of all pages, found in all levels of subcategories. The multiplicates of recurring pages
        /// are removed. Use FillSubsFromCategoryTree function instead to get titles
        /// of subcategories. This operation may be very time-consuming and traffic-consuming.
        /// The function clears the PageList before filling.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillFromCategoryTree(string categoryName)
        {
            FillAllFromCategoryTree(categoryName);
            RemoveNamespaces(new int[] { 14 });
            if (pages.Count != 0)
                Console.WriteLine(
                    Bot.Msg("PageList filled with {0} page titles, found in \"{1}\" category."),
                    Count().ToString(), categoryName);
            else
                Console.Error.WriteLine(
                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
        }

        /// <summary>Gets all levels of subcategories of some wiki category (that means
        /// subcategories, sub-subcategories, and so on) and fills this PageList with found
        /// subcategory titles. Use FillFromCategoryTree function instead to get pages of other
        /// namespaces. The multiplicates of recurring categories are removed. The operation may
        /// be very time-consuming and traffic-consuming. The function clears the PageList
        /// before filling.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillSubsFromCategoryTree(string categoryName)
        {
            FillAllFromCategoryTree(categoryName);
            FilterNamespaces(new int[] { 14 });
            if (pages.Count != 0)
                Console.WriteLine(Bot.Msg("PageList filled with {0} subcategory page titles, " +
                    "found in \"{1}\" category."), Count().ToString(), categoryName);
            else
                Console.Error.WriteLine(
                    Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
        }

        /// <summary>Gets all levels of subcategories of some wiki category (that means
        /// subcategories, sub-subcategories, and so on) and fills this PageList with titles
        /// of all pages, found in all levels of subcategories, including the titles of
        /// subcategories. The multiplicates of recurring pages and subcategories are removed.
        /// The operation may be very time-consuming and traffic-consuming. The function clears
        /// the PageList before filling.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void FillAllFromCategoryTree(string categoryName)
        {
            Clear();
            categoryName = site.CorrectNSPrefix(categoryName);
            StringCollection doneCats = new StringCollection();
            FillAllFromCategory(categoryName);
            doneCats.Add(categoryName);
            for (int i = 0; i < Count(); i++)
                if (pages[i].GetNamespace() == 14 && !doneCats.Contains(pages[i].title))
                {
                    FillAllFromCategory(pages[i].title);
                    doneCats.Add(pages[i].title);
                }
            RemoveRecurring();
        }

        /// <summary>Gets page history and fills this PageList with specified number of recent page
        /// revisions. Only revision identifiers, user names, timestamps and comments are
        /// loaded, not the texts. Call Load() (but not LoadEx) to load the texts of page revisions.
        /// The function combines XML (XHTML) parsing and regular expressions matching.</summary>
        /// <param name="pageTitle">Page to get history of.</param>
        /// <param name="lastRevisions">Number of last page revisions to get.</param>
        public void FillFromPageHistory(string pageTitle, int lastRevisions)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (lastRevisions <= 0)
                throw new ArgumentOutOfRangeException("quantity",
                    Bot.Msg("Quantity must be positive."));
            Console.WriteLine(
                Bot.Msg("Getting {0} last revisons of \"{1}\" page..."), lastRevisions, pageTitle);
            string res = site.site + site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(pageTitle) + "&limit=" + lastRevisions.ToString() +
                    "&action=history";
            string src = site.GetPageHTM(res);
            src = src.Substring(src.IndexOf("<ul id=\"pagehistory\">"));
            src = src.Substring(0, src.IndexOf("</ul>") + 5);
            Page p = null;
            using (XmlReader reader = site.GetXMLReader(src))
            {
                while (reader.Read())
                {
                    if (reader.Name == "li" && reader.NodeType == XmlNodeType.Element)
                    {
                        p = new Page(site, pageTitle);
                        p.lastMinorEdit = false;
                        p.comment = "";
                    }
                    else if (reader.Name == "span" && reader["class"] == "mw-history-histlinks")
                    {
                        reader.ReadToFollowing("a");
                        p.lastRevisionID = reader["href"].Substring(
                            reader["href"].IndexOf("oldid=") + 6);
                        DateTime.TryParse(reader.ReadString(),
                            site.regCulture, DateTimeStyles.AssumeLocal, out p.timestamp);
                    }
                    else if (reader.Name == "span" && reader["class"] == "history-user")
                    {
                        reader.ReadToFollowing("a");
                        p.lastUser = reader.ReadString();
                    }
                    else if (reader.Name == "abbr")
                        p.lastMinorEdit = true;
                    else if (reader.Name == "span" && reader["class"] == "history-size")
                        int.TryParse(Regex.Replace(reader.ReadString(), @"[^-+\d]", ""),
                            out p.lastBytesModified);
                    else if (reader.Name == "span" && reader["class"] == "comment")
                    {
                        p.comment = Regex.Replace(reader.ReadInnerXml().Trim(), "<.+?>", "");
                        p.comment = p.comment.Substring(1, p.comment.Length - 2);	// brackets
                    }
                    if (reader.Name == "li" && reader.NodeType == XmlNodeType.EndElement)
                        pages.Add(p);
                }
            }
            Console.WriteLine(Bot.Msg("PageList filled with {0} last revisons of \"{1}\" page..."),
                pages.Count, pageTitle);
        }

        /// <summary>Gets page history using  bot query interface ("api.php" MediaWiki extension)
        /// and fills this PageList with specified number of last page revisions, optionally loading
        /// revision texts as well. On most sites not more than 50 last revisions can be obtained.
        /// Thanks to Jutiphan Mongkolsuthree for idea and outline of this function.</summary>
        /// <param name="pageTitle">Page to get history of.</param>
        /// <param name="lastRevisions">Number of last page revisions to obtain.</param>
        /// <param name="loadTexts">Load revision texts right away.</param>
        public void FillFromPageHistoryEx(string pageTitle, int lastRevisions, bool loadTexts)
        {
            if (!site.botQuery)
                throw new WikiBotException(
                    Bot.Msg("The \"api.php\" MediaWiki extension is not available."));
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (lastRevisions <= 0)
                throw new ArgumentOutOfRangeException("lastRevisions",
                    Bot.Msg("Quantity must be positive."));
            Console.WriteLine(
                Bot.Msg("Getting {0} last revisons of \"{1}\" page..."), lastRevisions, pageTitle);
            string queryUri = site.site + site.indexPath +
                "api.php?action=query&prop=revisions&titles=" +
                HttpUtility.UrlEncode(pageTitle) + "&rvprop=ids|user|comment|timestamp" +
                (loadTexts ? "|content" : "") + "&format=xml&rvlimit=" + lastRevisions.ToString();
            string src = site.GetPageHTM(queryUri);
            Page p;
            using (XmlReader reader = XmlReader.Create(new StringReader(src)))
            {
                reader.ReadToFollowing("api");
                reader.Read();
                if (reader.Name == "error")
                    Console.Error.WriteLine(Bot.Msg("Error: {0}"), reader.GetAttribute("info"));
                while (reader.ReadToFollowing("rev"))
                {
                    p = new Page(site, pageTitle);
                    p.lastRevisionID = reader.GetAttribute("revid");
                    p.lastUser = reader.GetAttribute("user");
                    p.comment = reader.GetAttribute("comment");
                    p.timestamp =
                        DateTime.Parse(reader.GetAttribute("timestamp")).ToUniversalTime();
                    if (loadTexts)
                        p.text = reader.ReadString();
                    pages.Add(p);
                }
            }
            Console.WriteLine(Bot.Msg("PageList filled with {0} last revisons of \"{1}\" page."),
                pages.Count, pageTitle);
        }

        /// <summary>Gets page titles for this PageList from links in some wiki page. But only
        /// links to articles and pages from Project, Template and Help namespaces will be
        /// retrieved. And no interwiki links. Use FillFromAllPageLinks function instead
        /// to filter namespaces manually.</summary>
        /// <param name="pageTitle">Page name to get links from.</param>
        public void FillFromPageLinks(string pageTitle)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            FillFromAllPageLinks(pageTitle);
            FilterNamespaces(new int[] { 0, 4, 10, 12 });
        }

        /// <summary>Gets page titles for this PageList from all links in some wiki page. All links
        /// will be retrieved, from all standard namespaces, except interwiki links to other
        /// sites. Use FillFromPageLinks function instead to filter namespaces
        /// automatically.</summary>
        /// <param name="pageTitle">Page title as string.</param>
        /// <example><code>pageList.FillFromAllPageLinks("Art");</code></example>
        public void FillFromAllPageLinks(string pageTitle)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            if (string.IsNullOrEmpty(Site.WMLangsStr))
                site.GetWikimediaWikisList();
            Regex wikiLinkRE = new Regex(@"\[\[:*(.+?)(]]|\|)");
            Page page = new Page(site, pageTitle);
            page.Load();
            MatchCollection matches = wikiLinkRE.Matches(page.text);
            Regex outWikiLink = new Regex("^(" + Site.WMLangsStr +
                /*"|" + Site.WMSitesStr + */ "):");
            foreach (Match match in matches)
                if (!outWikiLink.IsMatch(match.Groups[1].Value))
                    pages.Add(new Page(site, match.Groups[1].Value));
            Console.WriteLine(
                Bot.Msg("PageList filled with links, found on \"{0}\" page."), pageTitle);
        }

        /// <summary>Gets page titles for this PageList from "Special:Whatlinkshere" Mediawiki page
        /// of specified page. That means the titles of pages, referring to the specified page.
        /// But not more than 5000 titles. The function does not internally remove redirecting
        /// pages from the results. Call RemoveRedirects() manually, if you need it. And the
        /// function does not clear the existing PageList, so new titles will be added.</summary>
        /// <param name="pageTitle">Page title as string.</param>
        public void FillFromLinksToPage(string pageTitle)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            //RemoveAll();
            string res = site.site + site.indexPath +
                "index.php?title=Special:Whatlinkshere/" +
                HttpUtility.UrlEncode(pageTitle) + "&limit=5000";
            string src = site.GetPageHTM(res);
            MatchCollection matches = Site.linkToPageRE1.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            //RemoveRedirects();
            Console.WriteLine(
                Bot.Msg("PageList filled with titles of pages, referring to \"{0}\" page."),
                pageTitle);
        }

        /// <summary>Gets titles of pages which transclude the specified page. No more than
        /// 5000 titles are listed. The function does not internally remove redirecting
        /// pages from results. Call RemoveRedirects() manually, if you need it. And the
        /// function does not clear the existing PageList, so new titles will be added.</summary>
        /// <param name="pageTitle">Page title as string.</param>
        public void FillFromTransclusionsOfPage(string pageTitle)
        {
            if (string.IsNullOrEmpty(pageTitle))
                throw new ArgumentNullException("pageTitle");
            string res = site.site + site.indexPath +
                "index.php?title=Special:Whatlinkshere/" +
                HttpUtility.UrlEncode(pageTitle) + "&limit=5000&hidelinks=1";
            string src = site.GetPageHTM(res);
            MatchCollection matches = Site.linkToPageRE1.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(
                Bot.Msg("PageList filled with titles of pages, which transclude \"{0}\" page."),
                pageTitle);
        }

        /// <summary>Gets titles of pages, in which the specified image file is included.
        /// Function also works with non-image files.</summary>
        /// <param name="imageFileTitle">File title. With or without "Image:" or
        /// "File:" prefix.</param>
        public void FillFromPagesUsingImage(string imageFileTitle)
        {
            if (string.IsNullOrEmpty(imageFileTitle))
                throw new ArgumentNullException("imageFileTitle");
            int pagesCount = Count();
            imageFileTitle = site.RemoveNSPrefix(imageFileTitle, 6);
            string res = site.site + site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(site.namespaces["6"].ToString()) + ":" +
                HttpUtility.UrlEncode(imageFileTitle);
            string src = site.GetPageHTM(res);
            int startPos = src.IndexOf("<h2 id=\"filelinks\">");
            int endPos = src.IndexOf("<div class=\"printfooter\">");
            if (startPos == -1 || endPos == -1)
            {
                Console.Error.WriteLine(Bot.Msg("No page contains \"{0}\" image."), imageFileTitle);
                return;
            }
            src = src.Substring(startPos, endPos - startPos);
            MatchCollection matches = Site.linkToPageRE1.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            if (pagesCount == Count())
                Console.Error.WriteLine(Bot.Msg("No page contains \"{0}\" image."), imageFileTitle);
            else
                Console.WriteLine(
                    Bot.Msg("PageList filled with titles of pages, that contain \"{0}\" image."),
                    imageFileTitle);
        }

        /// <summary>Gets page titles for this PageList from user contributions
        /// of specified user. The function does not internally remove redirecting
        /// pages from the results. Call RemoveRedirects() manually, if you need it. And the
        /// function does not clears the existing PageList, so new titles will be added.</summary>
        /// <param name="userName">User's name.</param>
        /// <param name="limit">Maximum number of page titles to get.</param>
        public void FillFromUserContributions(string userName, int limit)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException("userName");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
            string res = site.site + site.indexPath +
                "index.php?title=Special:Contributions&target=" + HttpUtility.UrlEncode(userName) +
                "&limit=" + limit.ToString();
            string src = site.GetPageHTM(res);
            MatchCollection matches = Site.linkToPageRE2.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(
                Bot.Msg("PageList filled with user's \"{0}\" contributions."), userName);
        }

        /// <summary>Gets page titles for this PageList from watchlist
        /// of bot account. The function does not internally remove redirecting
        /// pages from the results. Call RemoveRedirects() manually, if you need that. And the
        /// function neither filters namespaces, nor clears the existing PageList,
        /// so new titles will be added to the existing in PageList.</summary>
        public void FillFromWatchList()
        {
            string src = site.GetPageHTM(site.indexPath + "index.php?title=Special:Watchlist/edit");
            MatchCollection matches = Site.linkToPageRE2.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(Bot.Msg("PageList filled with bot account's watchlist."));
        }

        /// <summary>Gets page titles for this PageList from list of recently changed
        /// watched articles (watched by bot account). The function does not internally
        /// remove redirecting pages from the results. Call RemoveRedirects() manually,
        /// if you need it. And the function neither filters namespaces, nor clears
        /// the existing PageList, so new titles will be added to the existing
        /// in PageList.</summary>
        public void FillFromChangedWatchedPages()
        {
            string src = site.GetPageHTM(site.indexPath + "index.php?title=Special:Watchlist/edit");
            MatchCollection matches = Site.linkToPageRE2.Matches(src);
            Console.WriteLine(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(
                Bot.Msg("PageList filled with changed pages from bot account's watchlist."));
        }

        /// <summary>Gets page titles for this PageList from wiki site internal search results.
        /// The function does not filter namespaces. And the function does not clear
        /// the existing PageList, so new titles will be added.</summary>
        /// <param name="searchStr">String to search.</param>
        /// <param name="limit">Maximum number of page titles to get.</param>
        public void FillFromSearchResults(string searchStr, int limit)
        {
            if (string.IsNullOrEmpty(searchStr))
                throw new ArgumentNullException("searchStr");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
            string res = site.site + site.indexPath +
                "index.php?title=Special:Search&fulltext=Search&search=" +
                HttpUtility.UrlEncode(searchStr) + "&limit=" + limit.ToString();
            string src = site.GetPageHTM(res);
            src = Bot.GetStringPortion(src, "<ul class='mw-search-results'>", "</ul>");
            Regex linkRE = new Regex("<a href=\".+?\" title=\"(.+?)\">");
            MatchCollection matches = linkRE.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
            Console.WriteLine(Bot.Msg("PageList filled with search results."));
        }

        /// <summary>Gets page titles for this PageList from www.google.com search results.
        /// The function does not filter namespaces. And the function does not clear
        /// the existing PageList, so new titles will be added.</summary>
        /// <param name="searchStr">Words to search for. Use quotes to find exact phrases.</param>
        /// <param name="limit">Maximum number of page titles to get.</param>
        public void FillFromGoogleSearchResults(string searchStr, int limit)
        {
            if (string.IsNullOrEmpty(searchStr))
                throw new ArgumentNullException("searchStr");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
            Uri res = new Uri("http://www.google.com/search?q=" + HttpUtility.UrlEncode(searchStr) +
                "+site:" + site.site.Substring(site.site.IndexOf("://") + 3) +
                "&num=" + limit.ToString());
            string src = Bot.GetWebResource(res, "");
            Regex GoogleLinkToPageRE = new Regex(
                "<h3[^>]*><a href=\"" + Regex.Escape(site.site) +
                "(" + (string.IsNullOrEmpty(site.wikiPath) == false ?
                    Regex.Escape(site.wikiPath) + "|" : "") +
                    Regex.Escape(site.indexPath) + @"index\.php\?title=)" +
                "([^\"]+?)\"");		// ..." class=\"?l\"?
            MatchCollection matches = GoogleLinkToPageRE.Matches(src);
            foreach (Match match in matches)
                pages.Add(new Page(site,
                    HttpUtility.UrlDecode(match.Groups[2].Value).Replace("_", " ")));
            Console.WriteLine(Bot.Msg("PageList filled with www.google.com search results."));
        }

        /// <summary>Gets page titles from UTF8-encoded file. Each title must be on new line.
        /// The function does not clear the existing PageList, so new pages will be added.</summary>
        public void FillFromFile(string filePathName)
        {
            //RemoveAll();
            StreamReader strmReader = new StreamReader(filePathName);
            string input;
            while ((input = strmReader.ReadLine()) != null)
            {
                input = input.Trim(" []".ToCharArray());
                if (string.IsNullOrEmpty(input) != true)
                    pages.Add(new Page(site, input));
            }
            strmReader.Close();
            Console.WriteLine(
                Bot.Msg("PageList filled with titles, found in \"{0}\" file."), filePathName);
        }

        #endregion

        /// <summary>Protects or unprotects all pages in this PageList, so only chosen category
        /// of users can edit or rename it. Changing page protection modes requires administrator
        /// (sysop) rights on target wiki.</summary>
        /// <param name="editMode">Protection mode for editing this page (0 = everyone allowed
        /// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed 
        /// to edit).</param>
        /// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to
        /// rename, 1 = only registered users are allowed, 2 = only administrators
        /// are allowed).</param>
        /// <param name="cascadeMode">In cascading mode, all the pages, included into this page
        /// (e.g., templates or images) are also fully automatically protected.</param>
        /// <param name="expiryDate">Date ant time, expressed in UTC, when the protection expires
        /// and page becomes fully unprotected. Use DateTime.ToUniversalTime() method to convert
        /// local time to UTC, if necessary. Pass DateTime.MinValue to make protection
        /// indefinite.</param>
        /// <param name="reason">Reason for protecting this page.</param>
        public void Protect(int editMode, int renameMode, bool cascadeMode,
            DateTime expiryDate, string reason)
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to protect."));
            foreach (Page p in pages)
                p.Protect(editMode, renameMode, cascadeMode, expiryDate, reason);
        }

        /// <summary>Adds all pages in this PageList to bot account's watchlist.</summary>
        public void Watch()
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to watch."));
            foreach (Page p in pages)
                p.Watch();
        }

        /// <summary>Removes all pages in this PageList from bot account's watchlist.</summary>
        public void Unwatch()
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to unwatch."));
            foreach (Page p in pages)
                p.Unwatch();
        }

        /// <summary>Removes the pages, that are not in given namespaces.</summary>
        /// <param name="neededNSs">Array of integers, presenting keys of namespaces
        /// to retain.</param>
        /// <example><code>pageList.FilterNamespaces(new int[] {0,3});</code></example>
        public void FilterNamespaces(int[] neededNSs)
        {
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                if (Array.IndexOf(neededNSs, pages[i].GetNamespace()) == -1)
                    pages.RemoveAt(i);
            }
        }

        /// <summary>Removes the pages, that are in given namespaces.</summary>
        /// <param name="needlessNSs">Array of integers, presenting keys of namespaces
        /// to remove.</param>
        /// <example><code>pageList.RemoveNamespaces(new int[] {2,4});</code></example>
        public void RemoveNamespaces(int[] needlessNSs)
        {
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                if (Array.IndexOf(needlessNSs, pages[i].GetNamespace()) != -1)
                    pages.RemoveAt(i);
            }
        }

        /// <summary>This function sorts all pages in PageList by titles.</summary>
        public void Sort()
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to sort."));
            pages.Sort(ComparePagesByTitles);
        }

        /// <summary>This internal function compares pages by titles (alphabetically).</summary>
        /// <returns>Returns 1 if x is greater, -1 if y is greater, 0 if equal.</returns>
        public int ComparePagesByTitles(Page x, Page y)
        {
            int r = string.Compare(x.title, y.title, false, site.langCulture);
            return (r != 0) ? r / Math.Abs(r) : 0;
        }

        /// <summary>Removes all pages in PageList from specified category by deleting
        /// links to that category in pages texts.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void RemoveFromCategory(string categoryName)
        {
            foreach (Page p in pages)
                p.RemoveFromCategory(categoryName);
        }

        /// <summary>Adds all pages in PageList to the specified category by adding
        /// links to that category in pages texts.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void AddToCategory(string categoryName)
        {
            foreach (Page p in pages)
                p.AddToCategory(categoryName);
        }

        /// <summary>Adds a specified template to the end of all pages in PageList.</summary>
        /// <param name="templateText">Template text, like "{{template_name|...|...}}".</param>
        public void AddTemplate(string templateText)
        {
            foreach (Page p in pages)
                p.AddTemplate(templateText);
        }

        /// <summary>Removes a specified template from all pages in PageList.</summary>
        /// <param name="templateTitle">Title of template  to remove.</param>
        public void RemoveTemplate(string templateTitle)
        {
            foreach (Page p in pages)
                p.RemoveTemplate(templateTitle);
        }

        #region load

        /// <summary>Loads text for pages in PageList from site via common web interface.
        /// Please, don't use this function when going to edit big amounts of pages on
        /// popular public wikis, as it compromises edit conflict detection. In that case,
        /// each page's text should be loaded individually right before its processing
        /// and saving.</summary>
        public void Load()
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));
            foreach (Page page in pages)
                page.Load();
        }

        /// <summary>Loads texts and metadata for pages in PageList via XML export interface.
        /// Non-existent pages will be automatically removed from the PageList.
        /// Please, don't use this function when going to edit big amounts of pages on
        /// popular public wikis, as it compromises edit conflict detection. In that case,
        /// each page's text should be loaded individually right before its processing
        /// and saving.</summary>
        public void LoadEx()
        {
            if (IsEmpty())
                throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));
            Console.WriteLine(Bot.Msg("Loading {0} pages..."), pages.Count);
            string res = site.site + site.indexPath +
                "index.php?title=Special:Export&action=submit";
            string postData = "curonly=True&pages=";
            foreach (Page page in pages)
                postData += HttpUtility.UrlEncode(page.title) + "\r\n";
            XmlReader reader = XmlReader.Create(
                new StringReader(site.PostDataAndGetResultHTM(res, postData)));
            Clear();
            while (reader.ReadToFollowing("page"))
            {
                Page p = new Page(site, "");
                p.ParsePageXML(reader.ReadOuterXml());
                pages.Add(p);
            }
            reader.Close();
        }

        /// <summary>Loads text and metadata for pages in PageList via XML export interface.
        /// The function uses XPathNavigator and is less efficient than LoadEx().</summary>
        public void LoadEx2()
        {
            if (IsEmpty())
                throw new WikiBotException("The PageList is empty. Nothing to load.");
            Console.WriteLine(Bot.Msg("Loading {0} pages..."), pages.Count);
            string res = site.site + site.indexPath +
                "index.php?title=Special:Export&action=submit";
            string postData = "curonly=True&pages=";
            foreach (Page page in pages)
                postData += HttpUtility.UrlEncode(page.title + "\r\n");
            string src = site.PostDataAndGetResultHTM(res, postData);
            src = Bot.RemoveXMLRootAttributes(src);
            StringReader strReader = new StringReader(src);
            XPathDocument doc = new XPathDocument(strReader);
            strReader.Close();
            XPathNavigator nav = doc.CreateNavigator();
            foreach (Page page in pages)
            {
                if (page.title.Contains("'"))
                {
                    page.LoadEx();
                    continue;
                }
                string query = "//page[title='" + page.title + "']/";
                try
                {
                    page.text =
                        nav.SelectSingleNode(query + "revision/text").InnerXml;
                }
                catch (System.NullReferenceException)
                {
                    continue;
                }
                page.text = HttpUtility.HtmlDecode(page.text);
                page.pageID = nav.SelectSingleNode(query + "id").InnerXml;
                try
                {
                    page.lastUser = nav.SelectSingleNode(query +
                        "revision/contributor/username").InnerXml;
                    page.lastUserID = nav.SelectSingleNode(query +
                        "revision/contributor/id").InnerXml;
                }
                catch (System.NullReferenceException)
                {
                    page.lastUser = nav.SelectSingleNode(query +
                        "revision/contributor/ip").InnerXml;
                }
                page.lastUser = HttpUtility.HtmlDecode(page.lastUser);
                page.lastRevisionID = nav.SelectSingleNode(query + "revision/id").InnerXml;
                page.lastMinorEdit = (nav.SelectSingleNode(query +
                    "revision/minor") == null) ? false : true;
                try
                {
                    page.comment = nav.SelectSingleNode(query + "revision/comment").InnerXml;
                    page.comment = HttpUtility.HtmlDecode(page.comment);
                }
                catch (System.NullReferenceException) { ;}
                page.timestamp = nav.SelectSingleNode(query + "revision/timestamp").ValueAsDateTime;
            }
            Console.WriteLine(Bot.Msg("Pages download completed."));
        }

        /// <summary>Loads text and metadata for pages in PageList via XML export interface.
        /// The function loads pages one by one, it is slightly less efficient
        /// than LoadEx().</summary>
        public void LoadEx3()
        {
            if (IsEmpty())
                throw new WikiBotException("The PageList is empty. Nothing to load.");
            foreach (Page p in pages)
                p.LoadEx();
        }

        /// <summary>Gets page titles and page text from local XML dump.
        /// This function consumes much resources.</summary>
        /// <param name="filePathName">The path to and name of the XML dump file as string.</param>
        public void FillAndLoadFromXMLDump(string filePathName)
        {
            Console.WriteLine(Bot.Msg("Loading pages from XML dump..."));
            XmlReader reader = XmlReader.Create(filePathName);
            while (reader.ReadToFollowing("page"))
            {
                Page p = new Page(site, "");
                p.ParsePageXML(reader.ReadOuterXml());
                pages.Add(p);
            }
            reader.Close();
            Console.WriteLine(Bot.Msg("XML dump loaded successfully."));
        }

        /// <summary>Gets page titles and page texts from all ".txt" files in the specified
        /// directory (folder). Each file becomes a page. Page titles are constructed from
        /// file names. Page text is read from file contents. If any Unicode numeric codes
        /// (also known as numeric character references or NCRs) of the forbidden characters
        /// (forbidden in filenames) are recognized in filenames, those codes are converted
        /// to characters (e.g. "&#x7c;" is converted to "|").</summary>
        /// <param name="dirPath">The path and name of a directory (folder)
        /// to load files from.</param>
        public void FillAndLoadFromFiles(string dirPath)
        {
            foreach (string fileName in Directory.GetFiles(dirPath, "*.txt"))
            {
                Page p = new Page(site, Path.GetFileNameWithoutExtension(fileName));
                p.title = p.title.Replace("&#x22;", "\"");
                p.title = p.title.Replace("&#x3c;", "<");
                p.title = p.title.Replace("&#x3e;", ">");
                p.title = p.title.Replace("&#x3f;", "?");
                p.title = p.title.Replace("&#x3a;", ":");
                p.title = p.title.Replace("&#x5c;", "\\");
                p.title = p.title.Replace("&#x2f;", "/");
                p.title = p.title.Replace("&#x2a;", "*");
                p.title = p.title.Replace("&#x7c;", "|");
                p.LoadFromFile(fileName);
                pages.Add(p);
            }
        }

        #endregion

        #region save

        /// <summary>Saves all pages in PageList to live wiki site. Uses default bot
        /// edit comment and default minor edit mark setting ("true" by default). This function
        /// doesn't limit the saving speed, so in case of working on public wiki, it's better
        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or
        /// framework errors may arise in case of overloading).</summary>
        public void Save()
        {
            Save(Bot.editComment, Bot.isMinorEdit);
        }

        /// <summary>Saves all pages in PageList to live wiki site. This function
        /// doesn't limit the saving speed, so in case of working on public wiki, it's better
        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or
        /// framework errors may arise in case of overloading).</summary>
        /// <param name="comment">Your edit comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
        public void Save(string comment, bool isMinorEdit)
        {
            foreach (Page page in pages)
                page.Save(page.text, comment, isMinorEdit);
        }

        /// <summary>Saves all pages in PageList to live wiki site. The function waits for 5 seconds
        /// between each page save operation in order not to overload server. Uses default bot
        /// edit comment and default minor edit mark setting ("true" by default). This function
        /// doesn't limit the saving speed, so in case of working on public wiki, it's better
        /// to use SaveSmoothly function in order not to overload public server (HTTP errors or
        /// framework errors may arise in case of overloading).</summary>
        public void SaveSmoothly()
        {
            SaveSmoothly(5, Bot.editComment, Bot.isMinorEdit);
        }

        /// <summary>Saves all pages in PageList to live wiki site. The function waits for specified
        /// number of seconds between each page save operation in order not to overload server.
        /// Uses default bot edit comment and default minor edit mark setting
        /// ("true" by default).</summary>
        /// <param name="intervalSeconds">Number of seconds to wait between each
        /// save operation.</param>
        public void SaveSmoothly(int intervalSeconds)
        {
            SaveSmoothly(intervalSeconds, Bot.editComment, Bot.isMinorEdit);
        }

        /// <summary>Saves all pages in PageList to live wiki site. The function waits for specified
        /// number of seconds between each page save operation in order not to overload
        /// server.</summary>
        /// <param name="intervalSeconds">Number of seconds to wait between each
        /// save operation.</param>
        /// <param name="comment">Your edit comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
        public void SaveSmoothly(int intervalSeconds, string comment, bool isMinorEdit)
        {
            if (intervalSeconds == 0)
                intervalSeconds = 1;
            foreach (Page page in pages)
            {
                Thread.Sleep(intervalSeconds * 1000);
                page.Save(page.text, comment, isMinorEdit);
            }
        }

        /// <summary>Undoes the last edit of every page in this PageList, so every page text reverts
        /// to previous contents. The function doesn't affect other operations
        /// like renaming.</summary>
        /// <param name="comment">Your edit comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
        public void Revert(string comment, bool isMinorEdit)
        {
            foreach (Page page in pages)
                page.Revert(comment, isMinorEdit);
        }

        /// <summary>Saves titles of all pages in PageList to the specified file. Each title
        /// on a new line. If the target file already exists, it is overwritten.</summary>
        /// <param name="filePathName">The path to and name of the target file as string.</param>
        public void SaveTitlesToFile(string filePathName)
        {
            SaveTitlesToFile(filePathName, false);
        }

        /// <summary>Saves titles of all pages in PageList to the specified file. Each title
        /// on a separate line. If the target file already exists, it is overwritten.</summary>
        /// <param name="filePathName">The path to and name of the target file as string.</param>
        /// <param name="useSquareBrackets">If true, each page title is enclosed
        /// in square brackets.</param>
        public void SaveTitlesToFile(string filePathName, bool useSquareBrackets)
        {
            StringBuilder titles = new StringBuilder();
            foreach (Page page in pages)
                titles.Append(useSquareBrackets ?
                    "[[" + page.title + "]]\r\n" : page.title + "\r\n");
            File.WriteAllText(filePathName, titles.ToString().Trim(), Encoding.UTF8);
            Console.WriteLine(Bot.Msg("Titles in PageList saved to \"{0}\" file."), filePathName);
        }

        /// <summary>Saves the contents of all pages in pageList to ".txt" files in specified
        /// directory. Each page is saved to separate file, the name of that file is constructed
        /// from page title. Forbidden characters in filenames are replaced with their
        /// Unicode numeric codes (also known as numeric character references or NCRs).
        /// If the target file already exists, it is overwritten.</summary>
        /// <param name="dirPath">The path and name of a directory (folder)
        /// to save files to.</param>
        public void SaveToFiles(string dirPath)
        {
            string curDirPath = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(dirPath);
            foreach (Page page in pages)
                page.SaveToFile();
            Directory.SetCurrentDirectory(curDirPath);
        }

        /// <summary>Loads the contents of all pages in pageList from live site via XML export
        /// and saves the retrieved XML content to the specified file. The functions just dumps
        /// data, it does not load pages in PageList itself, call LoadEx() or
        /// FillAndLoadFromXMLDump() to do that. Note, that on some sites, MediaWiki messages
        /// from standard namespace 8 are not available for export.</summary>
        /// <param name="filePathName">The path to and name of the target file as string.</param>
        public void SaveXMLDumpToFile(string filePathName)
        {
            Console.WriteLine(Bot.Msg("Loading {0} pages for XML dump..."), this.pages.Count);
            string res = site.site + site.indexPath +
                "index.php?title=Special:Export&action=submit";
            string postData = "catname=&curonly=true&action=submit&pages=";
            foreach (Page page in pages)
                postData += HttpUtility.UrlEncode(page.title + "\r\n");
            string rawXML = site.PostDataAndGetResultHTM(res, postData);
            rawXML = Bot.RemoveXMLRootAttributes(rawXML).Replace("\n", "\r\n");
            if (File.Exists(filePathName))
                File.Delete(filePathName);
            FileStream fs = File.Create(filePathName);
            byte[] XMLBytes = new System.Text.UTF8Encoding(true).GetBytes(rawXML);
            fs.Write(XMLBytes, 0, XMLBytes.Length);
            fs.Close();
            Console.WriteLine(
                Bot.Msg("XML dump successfully saved in \"{0}\" file."), filePathName);
        }

        #endregion

        #region remove

        /// <summary>Removes all empty pages from PageList. But firstly don't forget to load
        /// the pages from site using pageList.LoadEx().</summary>
        public void RemoveEmpty()
        {
            for (int i = pages.Count - 1; i >= 0; i--)
                if (pages[i].IsEmpty())
                    pages.RemoveAt(i);
        }

        /// <summary>Removes all recurring pages from PageList. Only one page with some title will
        /// remain in PageList. This makes all page elements in PageList unique.</summary>
        public void RemoveRecurring()
        {
            for (int i = pages.Count - 1; i >= 0; i--)
                for (int j = i - 1; j >= 0; j--)
                    if (pages[i].title == pages[j].title)
                    {
                        pages.RemoveAt(i);
                        break;
                    }
        }

        /// <summary>Removes all redirecting pages from PageList. But firstly don't forget to load
        /// the pages from site using pageList.LoadEx().</summary>
        public void RemoveRedirects()
        {
            for (int i = pages.Count - 1; i >= 0; i--)
                if (pages[i].IsRedirect())
                    pages.RemoveAt(i);
        }

        /// <summary>For all redirecting pages in this PageList, this function loads the titles and
        /// texts of redirected-to pages.</summary>
        public void ResolveRedirects()
        {
            foreach (Page page in pages)
            {
                if (page.IsRedirect() == false)
                    continue;
                page.title = page.RedirectsTo();
                page.Load();
            }
        }

        /// <summary>Removes all disambiguation pages from PageList. But firstly don't
        /// forget to load the pages from site using pageList.LoadEx().</summary>
        public void RemoveDisambigs()
        {
            for (int i = pages.Count - 1; i >= 0; i--)
                if (pages[i].IsDisambig())
                    pages.RemoveAt(i);
        }


        /// <summary>Removes all pages from PageList.</summary>
        public void RemoveAll()
        {
            pages.Clear();
        }

        /// <summary>Removes all pages from PageList.</summary>
        public void Clear()
        {
            pages.Clear();
        }

        #endregion

        /// <summary>Function changes default English namespace prefixes to correct local prefixes
        /// (e.g. for German wiki-sites it changes "Category:..." to "Kategorie:...").</summary>
        public void CorrectNSPrefixes()
        {
            foreach (Page p in pages)
                p.CorrectNSPrefix();
        }

        /// <summary>Shows if there are any Page objects in this PageList.</summary>
        /// <returns>Returns bool value.</returns>
        public bool IsEmpty()
        {
            return (pages.Count == 0) ? true : false;
        }

        /// <summary>Sends titles of all contained pages to console.</summary>
        public void ShowTitles()
        {
            Console.WriteLine("\n" + Bot.Msg("Pages in this PageList:"));
            foreach (Page p in pages)
                Console.WriteLine(p.title);
            Console.WriteLine("\n");
        }

        /// <summary>Sends texts of all contained pages to console.</summary>
        public void ShowTexts()
        {
            Console.WriteLine("\n" + Bot.Msg("Texts of all pages in this PageList:"));
            Console.WriteLine("--------------------------------------------------");
            foreach (Page p in pages)
            {
                p.ShowText();
                Console.WriteLine("--------------------------------------------------");
            }
            Console.WriteLine("\n");
        }
    }
}
