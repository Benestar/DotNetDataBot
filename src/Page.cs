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

namespace DotNetDataBot
{
    /// <summary>Class defines wiki page object.</summary>
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [Serializable]
    public class Page
    {
        #region variables

        /// <summary>Page title.</summary>
        public string title;
        /// <summary>Page text.</summary>
        public string text;
        /// <summary>Page ID in internal MediaWiki database.</summary>
        public string pageID;
        /// <summary>Username or IP-address of last page contributor.</summary>
        public string lastUser;
        /// <summary>Last contributor ID in internal MediaWiki database.</summary>
        public string lastUserID;
        /// <summary>Page revision ID in the internal MediaWiki database.</summary>
        public string lastRevisionID;
        /// <summary>True, if last edit was minor edit.</summary>
        public bool lastMinorEdit;
        /// <summary>Amount of bytes, modified during last edit.</summary>
        public int lastBytesModified;
        /// <summary>Last edit comment.</summary>
        public string comment;
        /// <summary>Date and time of last edit expressed in UTC (Coordinated Universal Time).
        /// Call "timestamp.ToLocalTime()" to convert to local time if it is necessary.</summary>
        public DateTime timestamp;
        /// <summary>True, if this page is in bot account's watchlist. Call GetEditSessionData
        /// function to get the actual state of this property.</summary>
        public bool watched;
        /// <summary>This edit session time attribute is used to edit pages.</summary>
        public string editSessionTime;
        /// <summary>This edit session token attribute is used to edit pages.</summary>
        public string editSessionToken;
        /// <summary>Site, on which the page is.</summary>
        public Site site;

        #endregion

        #region construction

        /// <summary>This constructor creates Page object with specified title and specified
        /// Site object. This is preferable constructor. When constructed, new Page object doesn't
        /// contain text. Use Load() method to get text from live wiki. Or use LoadEx() to get
        /// both text and metadata via XML export interface.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <param name="title">Page title as string.</param>
        /// <returns>Returns Page object.</returns>
        public Page(Site site, string title)
        {
            this.title = title;
            this.site = site;
        }

        /// <summary>This constructor creates empty Page object with specified Site object,
        /// but without title. Avoid using this constructor needlessly.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <returns>Returns Page object.</returns>
        public Page(Site site)
        {
            this.site = site;
        }

        /// <summary>This constructor creates Page object with specified title. Site object
        /// with default properties is created internally and logged in. Constructing
        /// new Site object is too slow, don't use this constructor needlessly.</summary>
        /// <param name="title">Page title as string.</param>
        /// <returns>Returns Page object.</returns>
        public Page(string title)
        {
            this.site = new Site();
            this.title = title;
        }

        /// <summary>This constructor creates Page object with specified page's numeric revision ID
        /// (also called "oldid"). Page title is retrieved automatically
        /// in this constructor.</summary>
        /// <param name="site">Site object, it must be constructed beforehand.</param>
        /// <param name="revisionID">Page's numeric revision ID (also called "oldid").</param>
        /// <returns>Returns Page object.</returns>
        public Page(Site site, Int64 revisionID)
        {
            if (revisionID <= 0)
                throw new ArgumentOutOfRangeException("revisionID",
                    Bot.Msg("Revision ID must be positive."));
            this.site = site;
            lastRevisionID = revisionID.ToString();
            GetTitle();
        }

        /// <summary>This constructor creates Page object with specified page's numeric revision ID
        /// (also called "oldid"). Page title is retrieved automatically in this constructor.
        /// Site object with default properties is created internally and logged in. Constructing
        /// new Site object is too slow, don't use this constructor needlessly.</summary>
        /// <param name="revisionID">Page's numeric revision ID (also called "oldid").</param>
        /// <returns>Returns Page object.</returns>
        public Page(Int64 revisionID)
        {
            if (revisionID <= 0)
                throw new ArgumentOutOfRangeException("revisionID",
                    Bot.Msg("Revision ID must be positive."));
            this.site = new Site();
            lastRevisionID = revisionID.ToString();
            GetTitle();
        }

        /// <summary>This constructor creates empty Page object without title. Site object with
        /// default properties is created internally and logged in. Constructing new Site object
        /// is too slow, avoid using this constructor needlessly.</summary>
        /// <returns>Returns Page object.</returns>
        public Page()
        {
            this.site = new Site();
        }

        #endregion

        #region Init

        /// <summary>Loads actual page text for live wiki site via raw web interface.
        /// If Page.lastRevisionID is specified, the function gets that specified
        /// revision.</summary>
        public void Load()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to load."));
            string res = site.site + site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title) +
                (string.IsNullOrEmpty(lastRevisionID) ? "" : "&oldid=" + lastRevisionID) +
                "&redirect=no&action=raw&ctype=text/plain&dontcountme=s";
            try
            {
                text = site.GetPageHTM(res);
            }
            catch (WebException e)
            {
                string message = e.Message;
                if (message.Contains(": (404) "))
                {
                    // Not Found
                    Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
                    text = "";
                    return;
                }
                else
                    throw;
            }
            Console.WriteLine(Bot.Msg("Page \"{0}\" loaded successfully."), title);
        }

        /// <summary>Loads page text and metadata via XML export interface. It is slower,
        /// than Load(), don't use it if you don't need page metadata (page id, timestamp,
        /// comment, last contributor, minor edit mark).</summary>
        public void LoadEx()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to load."));
            string res = site.site + site.indexPath + "index.php?title=Special:Export/" +
                HttpUtility.UrlEncode(title) + "&action=submit";
            string src = site.GetPageHTM(res);
            ParsePageXML(src);
        }

        /// <summary>This internal function parses MediaWiki XML export data using XmlDocument
        /// to get page text and metadata.</summary>
        /// <param name="xmlSrc">XML export source code.</param>
        public void ParsePageXML(string xmlSrc)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlSrc);
            if (doc.GetElementsByTagName("page").Count == 0)
            {
                Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
                return;
            }
            text = doc.GetElementsByTagName("text")[0].InnerText;
            pageID = doc.GetElementsByTagName("id")[0].InnerText;
            if (doc.GetElementsByTagName("username").Count != 0)
            {
                lastUser = doc.GetElementsByTagName("username")[0].InnerText;
                lastUserID = doc.GetElementsByTagName("id")[2].InnerText;
            }
            else if (doc.GetElementsByTagName("ip").Count != 0)
                lastUser = doc.GetElementsByTagName("ip")[0].InnerText;
            else
                lastUser = "(n/a)";
            lastRevisionID = doc.GetElementsByTagName("id")[1].InnerText;
            if (doc.GetElementsByTagName("comment").Count != 0)
                comment = doc.GetElementsByTagName("comment")[0].InnerText;
            timestamp = DateTime.Parse(doc.GetElementsByTagName("timestamp")[0].InnerText);
            timestamp = timestamp.ToUniversalTime();
            lastMinorEdit = (doc.GetElementsByTagName("minor").Count != 0) ? true : false;
            if (string.IsNullOrEmpty(title))
                title = doc.GetElementsByTagName("title")[0].InnerText;
            else
                Console.WriteLine(Bot.Msg("Page \"{0}\" loaded successfully."), title);
        }

        /// <summary>Loads page text from the specified UTF8-encoded file.</summary>
        /// <param name="filePathName">Path and name of the file.</param>
        public void LoadFromFile(string filePathName)
        {
            StreamReader strmReader = new StreamReader(filePathName);
            text = strmReader.ReadToEnd();
            strmReader.Close();
            Console.WriteLine(
                Bot.Msg("Text for page \"{0}\" successfully loaded from \"{1}\" file."),
                title, filePathName);
        }

        #endregion

        #region session

        /// <summary>This function is used internally to gain rights to edit page
        /// on a live wiki site.</summary>
        public void GetEditSessionData()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(
                    Bot.Msg("No title specified for page to get edit session data."));
            string src = site.GetPageHTM(site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title) + "&action=edit");
            editSessionTime = Site.editSessionTimeRE1.Match(src).Groups[1].ToString();
            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();
            watched = Regex.IsMatch(src, "<a href=\"[^\"]+&(amp;)?action=unwatch\"");
        }

        /// <summary>This function is used internally to gain rights to edit page on a live wiki
        /// site. The function queries rights, using bot interface, thus saving traffic.</summary>
        public void GetEditSessionDataEx()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(
                    Bot.Msg("No title specified for page to get edit session data."));
            string src = site.GetPageHTM(site.indexPath + "api.php?action=query&prop=info" +
                "&format=xml&intoken=edit&titles=" + HttpUtility.UrlEncode(title));
            editSessionToken = Site.editSessionTokenRE3.Match(src).Groups[1].ToString();
            if (editSessionToken == "+\\")
                editSessionToken = "";
            editSessionTime = Site.editSessionTimeRE3.Match(src).Groups[1].ToString();
            if (!string.IsNullOrEmpty(editSessionTime))
                editSessionTime = Regex.Replace(editSessionTime, "\\D", "");
            if (string.IsNullOrEmpty(editSessionTime) && !string.IsNullOrEmpty(editSessionToken))
                editSessionTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
            if (site.watchList == null)
            {
                site.watchList = new PageList(site);
                site.watchList.FillFromWatchList();
            }
            watched = site.watchList.Contains(title);
        }

        #endregion

        #region save

        /// <summary>Saves current contents of page.text on live wiki site. Uses default bot
        /// edit comment and default minor edit mark setting ("true" in most cases)/</summary>
        public void Save()
        {
            Save(text, Bot.editComment, Bot.isMinorEdit);
        }

        /// <summary>Saves specified text in page on live wiki. Uses default bot
        /// edit comment and default minor edit mark setting ("true" in most cases).</summary>
        /// <param name="newText">New text for this page.</param>
        public void Save(string newText)
        {
            Save(newText, Bot.editComment, Bot.isMinorEdit);
        }

        /// <summary>Saves current page.text contents on live wiki site.</summary>
        /// <param name="comment">Your edit comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
        public void Save(string comment, bool isMinorEdit)
        {
            Save(text, comment, isMinorEdit);
        }

        /// <summary>Saves specified text in page on live wiki.</summary>
        /// <param name="newText">New text for this page.</param>
        /// <param name="comment">Your edit comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
        public void Save(string newText, string comment, bool isMinorEdit)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to save text to."));
            if (string.IsNullOrEmpty(newText) && string.IsNullOrEmpty(text))
                throw new WikiBotException(Bot.Msg("No text specified for page to save."));
            /*/ Edited by Bene ** Um Bot-Sperren zu umgehen! **
            if (text != null && Regex.IsMatch(text, @"(?is)\{\{(nobots|bots\|(allow=none|" +
                @"deny=(?!none)[^\}]*(" + site.userName + @"|all)|optout=all))\}\}"))
                throw new WikiBotException(string.Format(Bot.Msg(
                    "Bot action on \"{0}\" page is prohibited " +
                    "by \"nobots\" or \"bots|allow=none\" template."), title));
            //*/

            if (Bot.useBotQuery == true && site.botQuery == true &&
                (site.ver.Major > 1 || (site.ver.Major == 1 && site.ver.Minor >= 15)))
                GetEditSessionDataEx();
            else
                GetEditSessionData();
            if (string.IsNullOrEmpty(editSessionTime) || string.IsNullOrEmpty(editSessionToken))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Insufficient rights to edit page \"{0}\"."), title));
            string postData = string.Format("wpSection=&wpStarttime={0}&wpEdittime={1}" +
                "&wpScrolltop=&wpTextbox1={2}&wpSummary={3}&wpSave=Save%20Page" +
                "&wpEditToken={4}{5}{6}",
                // &wpAutoSummary=00000000000000000000000000000000&wpIgnoreBlankSummary=1
                DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss"),
                HttpUtility.UrlEncode(editSessionTime),
                HttpUtility.UrlEncode(newText),
                HttpUtility.UrlEncode(comment),
                HttpUtility.UrlEncode(editSessionToken),
                watched ? "&wpWatchthis=1" : "",
                isMinorEdit ? "&wpMinoredit=1" : "");
            if (Bot.askConfirm)
            {
                Console.Write("\n\n" +
                    Bot.Msg("The following text is going to be saved on page \"{0}\":"), title);
                Console.Write("\n\n" + text + "\n\n");
                if (!Bot.UserConfirms())
                    return;
            }
            string respStr = site.PostDataAndGetResultHTM(site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title) + "&action=submit", postData);
            if (respStr.Contains(" name=\"wpTextbox2\""))
                throw new WikiBotException(string.Format(
                    Bot.Msg("Edit conflict occurred while trying to savе page \"{0}\"."), title));
            if (respStr.Contains("<div class=\"permissions-errors\">"))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Insufficient rights to edit page \"{0}\"."), title));
            if (respStr.Contains("input name=\"wpCaptchaWord\" id=\"wpCaptchaWord\""))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Error occurred when saving page \"{0}\": " +
                    "Bot operation is not allowed for this account at \"{1}\" site."),
                    title, site.site));
            Console.WriteLine(Bot.Msg("Page \"{0}\" saved successfully."), title);
            text = newText;
        }

        #endregion

        #region undo

        /// <summary>Undoes the last edit, so page text reverts to previous contents.
        /// The function doesn't affect other actions like renaming.</summary>
        /// <param name="comment">Revert comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>
        public void Revert(string comment, bool isMinorEdit)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to revert."));
            PageList pl = new PageList(site);
            if (Bot.useBotQuery == true && site.botQuery == true &&
                site.botQueryVersions.ContainsKey("ApiQueryRevisions.php"))
                pl.FillFromPageHistoryEx(title, 2, false);
            else
                pl.FillFromPageHistory(title, 2);
            if (pl.Count() != 2)
            {
                Console.Error.WriteLine(Bot.Msg("Can't revert page \"{0}\"."), title);
                return;
            }
            pl[1].Load();
            Save(pl[1].text, comment, isMinorEdit);
            Console.WriteLine(Bot.Msg("Page \"{0}\" was reverted."), title);
        }

        /// <summary>Undoes all last edits of last page contributor, so page text reverts to
        /// previous contents. The function doesn't affect other operations
        /// like renaming or protecting.</summary>
        /// <param name="comment">Revert comment.</param>
        /// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>
        /// <returns>Returns true if last edits were undone.</returns>
        public bool UndoLastEdits(string comment, bool isMinorEdit)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to revert."));
            PageList pl = new PageList(site);
            string lastEditor = "";
            for (int i = 50; i <= 5000; i *= 10)
            {
                if (Bot.useBotQuery == true && site.botQuery == true &&
                    site.botQueryVersions.ContainsKey("ApiQueryRevisions.php"))
                    pl.FillFromPageHistoryEx(title, i, false);
                else
                    pl.FillFromPageHistory(title, i);
                lastEditor = pl[0].lastUser;
                foreach (Page p in pl)
                    if (p.lastUser != lastEditor)
                    {
                        p.Load();
                        Save(p.text, comment, isMinorEdit);
                        Console.WriteLine(
                            Bot.Msg("Last edits of page \"{0}\" by user {1} were undone."),
                            title, lastEditor);
                        return true;
                    }
                if (pl.pages.Count < i)
                    break;
                pl.Clear();
            }
            Console.Error.WriteLine(Bot.Msg("Can't undo last edits of page \"{0}\" by user {1}."),
                title, lastEditor);
            return false;
        }

        #endregion

        #region protect

        /// <summary>Protects or unprotects the page, so only authorized group of users can edit or
        /// rename it. Changing page protection mode requires administrator (sysop)
        /// rights.</summary>
        /// <param name="editMode">Protection mode for editing this page (0 = everyone allowed
        /// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed
        /// to edit).</param>
        /// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to
        /// rename, 1 = only registered users are allowed, 2 = only administrators
        /// are allowed).</param>
        /// <param name="cascadeMode">In cascading mode, all the pages, included into this page
        /// (e.g., templates or images) are also automatically protected.</param>
        /// <param name="expiryDate">Date and time, expressed in UTC, when protection expires
        /// and page becomes unprotected. Use DateTime.ToUniversalTime() method to convert local
        /// time to UTC, if necessary. Pass DateTime.MinValue to make protection indefinite.</param>
        /// <param name="reason">Reason for protecting this page.</param>
        public void Protect(int editMode, int renameMode, bool cascadeMode,
            DateTime expiryDate, string reason)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to protect."));
            string errorMsg =
                Bot.Msg("Only values 0, 1 and 2 are accepted. Please, consult documentation.");
            if (editMode > 2 || editMode < 0)
                throw new ArgumentOutOfRangeException("editMode", errorMsg);
            if (renameMode > 2 || renameMode < 0)
                throw new ArgumentOutOfRangeException("renameMode", errorMsg);
            if (expiryDate != DateTime.MinValue && expiryDate < DateTime.Now)
                throw new ArgumentOutOfRangeException("expiryDate",
                    Bot.Msg("Protection expiry date must be hereafter."));
            string res = site.site + site.indexPath +
                "index.php?title=" + HttpUtility.UrlEncode(title) + "&action=protect";
            string src = site.GetPageHTM(res);
            editSessionTime = Site.editSessionTimeRE1.Match(src).Groups[1].ToString();
            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
            {
                Console.Error.WriteLine(
                    Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);
                return;
            }
            string postData = string.Format("mwProtect-level-edit={0}&mwProtect-level-move={1}" +
                "&mwProtect-reason={2}&wpEditToken={3}&mwProtect-expiry={4}{5}",
                HttpUtility.UrlEncode(
                    editMode == 2 ? "sysop" : editMode == 1 ? "autoconfirmed" : ""),
                HttpUtility.UrlEncode(
                    renameMode == 2 ? "sysop" : renameMode == 1 ? "autoconfirmed" : ""),
                HttpUtility.UrlEncode(reason),
                HttpUtility.UrlEncode(editSessionToken),
                expiryDate == DateTime.MinValue ? "" : expiryDate.ToString("u"),
                cascadeMode == true ? "&mwProtect-cascade=1" : "");
            string respStr = site.PostDataAndGetResultHTM(site.indexPath +
                "index.php?title=" + HttpUtility.UrlEncode(title) + "&action=protect", postData);
            if (string.IsNullOrEmpty(respStr))
            {
                Console.Error.WriteLine(
                    Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);
                return;
            }
            Console.WriteLine(
                Bot.Msg("Protection mode for page \"{0}\" changed successfully."), title);
        }

        #endregion

        #region watch

        /// <summary>Adds page to bot account's watchlist.</summary>
        public void Watch()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to watch."));
            string res = site.site + site.indexPath +
                "index.php?title=" + HttpUtility.UrlEncode(title);
            string respStr = site.GetPageHTM(res);
            string watchToken = "";
            Regex watchTokenRE = new Regex("&amp;action=watch&amp;token=([^\"]+?)\"");
            if (watchTokenRE.IsMatch(respStr))
                watchToken = watchTokenRE.Match(respStr).Groups[1].ToString();
            respStr = site.GetPageHTM(res + "&action=watch&token=" + watchToken);
            watched = true;
            Console.WriteLine(Bot.Msg("Page \"{0}\" added to watchlist."), title);
        }

        /// <summary>Removes page from bot account's watchlist.</summary>
        public void Unwatch()
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to unwatch."));
            string res = site.site + site.indexPath +
                "index.php?title=" + HttpUtility.UrlEncode(title);
            string respStr = site.GetPageHTM(res);
            string unwatchToken = "";
            Regex unwatchTokenRE = new Regex("&amp;action=unwatch&amp;token=([^\"]+?)\"");
            if (unwatchTokenRE.IsMatch(respStr))
                unwatchToken = unwatchTokenRE.Match(respStr).Groups[1].ToString();
            respStr = site.GetPageHTM(res + "&action=unwatch&token=" + unwatchToken);
            watched = false;
            Console.WriteLine(Bot.Msg("Page \"{0}\" was removed from watchlist."), title);
        }

        #endregion

        #region word

        /// <summary>This function opens page text in Microsoft Word for editing.
        /// Just close Word after editing, and the revised text will appear in
        /// Page.text variable.</summary>
        /// <remarks>Appropriate PIAs (Primary Interop Assemblies) for available MS Office
        /// version must be installed and referenced in order to use this function. Follow
        /// instructions in "Compile and Run.bat" file to reference PIAs properly in compilation
        /// command, and then recompile the framework. Redistributable PIAs can be downloaded from
        /// http://www.microsoft.com/downloads/results.aspx?freetext=Office%20PIA</remarks>
        public void ReviseInMSWord()
        {
#if MS_WORD_INTEROP
			if (string.IsNullOrEmpty(text))
				throw new WikiBotException(Bot.Msg("No text on page to revise in Microsoft Word."));
			Microsoft.Office.Interop.Word.Application app =
				new Microsoft.Office.Interop.Word.Application();
			app.Visible = true;
			object mv = System.Reflection.Missing.Value;
			object template = mv;
			object newTemplate = mv;
			object documentType = Microsoft.Office.Interop.Word.WdDocumentType.wdTypeDocument;
			object visible = true;
			Microsoft.Office.Interop.Word.Document doc =
				app.Documents.Add(ref template, ref newTemplate, ref documentType, ref visible);
			doc.Words.First.InsertBefore(text);
			text = null;
			Microsoft.Office.Interop.Word.DocumentEvents_Event docEvents =
				(Microsoft.Office.Interop.Word.DocumentEvents_Event) doc;
			docEvents.Close +=
				new Microsoft.Office.Interop.Word.DocumentEvents_CloseEventHandler(
					delegate { text = doc.Range(ref mv, ref mv).Text; doc.Saved = true; } );
			app.Activate();
			while (text == null);
			text = Regex.Replace(text, "\r(?!\n)", "\r\n");
			app = null;
			doc = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Console.WriteLine(
				Bot.Msg("Text of \"{0}\" page was revised in Microsoft Word."), title);
#else
            throw new WikiBotException(Bot.Msg("Page.ReviseInMSWord() function requires MS " +
                "Office PIAs to be installed and referenced. Please, see remarks in function's " +
                "documentation in \"Documentation.chm\" file for additional instructions.\n"));
#endif
        }

        #endregion

        #region image

        /// <summary>Uploads local image to wiki site. Function also works with non-image files.
        /// Note: uploaded image title (wiki page title) will be the same as title of this Page
        /// object, not the title of source file.</summary>
        /// <param name="filePathName">Path and name of local file.</param>
        /// <param name="description">File (image) description.</param>
        /// <param name="license">File license type (may be template title). Used only on
        /// some wiki sites. Pass empty string, if the wiki site doesn't require it.</param>
        /// <param name="copyStatus">File (image) copy status. Used only on some wiki sites. Pass
        /// empty string, if the wiki site doesn't require it.</param>
        /// <param name="source">File (image) source. Used only on some wiki sites. Pass
        /// empty string, if the wiki site doesn't require it.</param>
        public void UploadImage(string filePathName, string description,
            string license, string copyStatus, string source)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for image to upload."));
            if (!File.Exists(filePathName))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Image file \"{0}\" doesn't exist."), filePathName));
            if (Path.GetFileNameWithoutExtension(filePathName).Length < 3)
                throw new WikiBotException(string.Format(Bot.Msg("Name of file \"{0}\" must " +
                    "contain at least 3 characters (excluding extension) for successful upload."),
                    filePathName));
            Console.WriteLine(Bot.Msg("Uploading image \"{0}\"..."), title);
            string targetName = site.RemoveNSPrefix(title, 6);
            targetName = Bot.Capitalize(targetName);
            string res = site.site + site.indexPath + "index.php?title=" +
                site.namespaces["-1"].ToString() + ":Upload";
            string src = site.GetPageHTM(res);
            editSessionToken = Site.editSessionTokenRE1.Match(src).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                editSessionToken = Site.editSessionTokenRE2.Match(src).Groups[1].ToString();
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(res);
            webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webReq.UseDefaultCredentials = true;
            webReq.Method = "POST";
            string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            webReq.ContentType = "multipart/form-data; boundary=" + boundary;
            webReq.UserAgent = Bot.botVer;
            webReq.CookieContainer = site.cookies;
            if (Bot.unsafeHttpHeaderParsingUsed == 0)
            {
                webReq.ProtocolVersion = HttpVersion.Version10;
                webReq.KeepAlive = false;
            }
            webReq.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(
                System.Net.Cache.HttpRequestCacheLevel.Refresh);
            StringBuilder sb = new StringBuilder();
            string ph = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"";
            sb.Append(ph + "wpIgnoreWarning\"\r\n\r\n1\r\n");
            sb.Append(ph + "wpDestFile\"\r\n\r\n" + targetName + "\r\n");
            sb.Append(ph + "wpUploadAffirm\"\r\n\r\n1\r\n");
            sb.Append(ph + "wpWatchthis\"\r\n\r\n0\r\n");
            sb.Append(ph + "wpEditToken\"\r\n\r\n" + editSessionToken + "\r\n");
            sb.Append(ph + "wpUploadCopyStatus\"\r\n\r\n" + copyStatus + "\r\n");
            sb.Append(ph + "wpUploadSource\"\r\n\r\n" + source + "\r\n");
            sb.Append(ph + "wpUpload\"\r\n\r\n" + "upload bestand" + "\r\n");
            sb.Append(ph + "wpLicense\"\r\n\r\n" + license + "\r\n");
            sb.Append(ph + "wpUploadDescription\"\r\n\r\n" + description + "\r\n");
            sb.Append(ph + "wpUploadFile\"; filename=\"" +
                HttpUtility.UrlEncode(Path.GetFileName(filePathName)) + "\"\r\n" +
                "Content-Type: application/octet-stream\r\n\r\n");
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] fileBytes = File.ReadAllBytes(filePathName);
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            webReq.ContentLength = postHeaderBytes.Length + fileBytes.Length + boundaryBytes.Length;
            Stream reqStream = webReq.GetRequestStream();
            reqStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
            reqStream.Write(fileBytes, 0, fileBytes.Length);
            reqStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            WebResponse webResp = null;
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
                        UploadImage(filePathName, description, license, copyStatus, source);
                        return;
                    }
                    else
                        throw;
                }
            }
            StreamReader strmReader = new StreamReader(webResp.GetResponseStream());
            string respStr = strmReader.ReadToEnd();
            strmReader.Close();
            webResp.Close();
            if (!respStr.Contains(HttpUtility.HtmlEncode(targetName)))
                throw new WikiBotException(string.Format(
                    Bot.Msg("Error occurred when uploading image \"{0}\"."), title));
            try
            {
                string errorMessage = site.GetMediaWikiMessage("MediaWiki:Uploadcorrupt");
                if (respStr.Contains(errorMessage))
                    throw new WikiBotException(string.Format(
                        Bot.Msg("Error occurred when uploading image \"{0}\"."), title));
            }
            catch (WikiBotException e)
            {
                if (!e.Message.Contains("Uploadcorrupt"))	// skip, if MediaWiki message not found
                    throw;
            }
            title = site.namespaces["6"] + ":" + targetName;
            text = description;
            Console.WriteLine(Bot.Msg("Image \"{0}\" uploaded successfully."), title);
        }

        /// <summary>Uploads web image to wiki site.</summary>
        /// <param name="imageFileUrl">Full URL of image file on the web.</param>
        /// <param name="description">Image description.</param>
        /// <param name="license">Image license type. Used only in some wiki sites. Pass
        /// empty string, if the wiki site doesn't require it.</param>
        /// <param name="copyStatus">Image copy status. Used only in some wiki sites. Pass
        /// empty string, if the wiki site doesn't require it.</param>
        public void UploadImageFromWeb(string imageFileUrl, string description,
            string license, string copyStatus)
        {
            if (string.IsNullOrEmpty(imageFileUrl))
                throw new WikiBotException(Bot.Msg("No URL specified of image to upload."));
            Uri res = new Uri(imageFileUrl);
            Bot.InitWebClient();
            string imageFileName = imageFileUrl.Substring(imageFileUrl.LastIndexOf("/") + 1);
            try
            {
                Bot.wc.DownloadFile(res, "Cache" + Path.DirectorySeparatorChar + imageFileName);
            }
            catch (System.Net.WebException)
            {
                throw new WikiBotException(string.Format(
                    Bot.Msg("Can't access image \"{0}\"."), imageFileUrl));
            }
            if (!File.Exists("Cache" + Path.DirectorySeparatorChar + imageFileName))
                throw new WikiBotException(string.Format(
                    Bot.Msg("Error occurred when downloading image \"{0}\"."), imageFileUrl));
            UploadImage("Cache" + Path.DirectorySeparatorChar + imageFileName,
                description, license, copyStatus, imageFileUrl);
            File.Delete("Cache" + Path.DirectorySeparatorChar + imageFileName);
        }

        /// <summary>Downloads image, audio or video file, pointed by this page title,
        /// from the wiki site. Redirection is resolved automatically.</summary>
        /// <param name="filePathName">Path and name of local file to save image to.</param>
        public void DownloadImage(string filePathName)
        {
            string res = site.site + site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title);
            string src = "";
            try
            {
                src = site.GetPageHTM(res);
            }
            catch (WebException e)
            {
                string message = e.Message;
                if (message.Contains(": (404) "))
                {		// Not Found
                    Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
                    text = "";
                    return;
                }
                else
                    throw;
            }
            Regex fileLinkRE1 = new Regex("<a href=\"([^\"]+?)\" class=\"internal\"");
            Regex fileLinkRE2 =
                new Regex("<div class=\"fullImageLink\" id=\"file\"><a href=\"([^\"]+?)\"");
            string fileLink = "";
            if (fileLinkRE1.IsMatch(src))
                fileLink = fileLinkRE1.Match(src).Groups[1].ToString();
            else if (fileLinkRE2.IsMatch(src))
                fileLink = fileLinkRE2.Match(src).Groups[1].ToString();
            else
                throw new WikiBotException(string.Format(
                    Bot.Msg("Image \"{0}\" doesn't exist."), title));
            if (!fileLink.StartsWith("http"))
                fileLink = site.site + fileLink;
            Bot.InitWebClient();
            Console.WriteLine(Bot.Msg("Downloading image \"{0}\"..."), title);
            Bot.wc.DownloadFile(fileLink, filePathName);
            Console.WriteLine(Bot.Msg("Image \"{0}\" downloaded successfully."), title);
        }

        #endregion

        #region save

        /// <summary>Saves page text to the specified file. If the target file already exists,
        /// it is overwritten.</summary>
        /// <param name="filePathName">Path and name of the file.</param>
        public void SaveToFile(string filePathName)
        {
            if (IsEmpty())
            {
                Console.Error.WriteLine(Bot.Msg("Page \"{0}\" contains no text to save."), title);
                return;
            }
            File.WriteAllText(filePathName, text, Encoding.UTF8);
            Console.WriteLine(Bot.Msg("Text of \"{0}\" page successfully saved in \"{1}\" file."),
                title, filePathName);
        }

        /// <summary>Saves page text to the ".txt" file in current directory.
        /// Use Directory.SetCurrentDirectory function to change the current directory (but don't
        /// forget to change it back after saving file). The name of the file is constructed
        /// from the title of the article. Forbidden characters in filenames are replaced
        /// with their Unicode numeric codes (also known as numeric character references
        /// or NCRs).</summary>
        public void SaveToFile()
        {
            string fileTitle = title;
            //Path.GetInvalidFileNameChars();
            fileTitle = fileTitle.Replace("\"", "&#x22;");
            fileTitle = fileTitle.Replace("<", "&#x3c;");
            fileTitle = fileTitle.Replace(">", "&#x3e;");
            fileTitle = fileTitle.Replace("?", "&#x3f;");
            fileTitle = fileTitle.Replace(":", "&#x3a;");
            fileTitle = fileTitle.Replace("\\", "&#x5c;");
            fileTitle = fileTitle.Replace("/", "&#x2f;");
            fileTitle = fileTitle.Replace("*", "&#x2a;");
            fileTitle = fileTitle.Replace("|", "&#x7c;");
            SaveToFile(fileTitle + ".txt");
        }

        #endregion

        #region pages

        /// <summary>Returns true, if page.text field is empty. Don't forget to call
        /// page.Load() before using this function.</summary>
        /// <returns>Returns bool value.</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(text);
        }

        /// <summary>Returns true, if page.text field is not empty. Don't forget to call
        /// Load() or LoadEx() before using this function.</summary>
        /// <returns>Returns bool value.</returns>
        public bool Exists()
        {
            return (string.IsNullOrEmpty(text) == true) ? false : true;
        }

        /// <summary>Returns true, if page redirects to another page. Don't forget to load
        /// actual page contents from live wiki "Page.Load()" before using this function.</summary>
        /// <returns>Returns bool value.</returns>
        public bool IsRedirect()
        {
            if (!Exists())
                return false;
            return site.redirectRE.IsMatch(text);
        }

        /// <summary>Returns redirection target. Don't forget to load
        /// actual page contents from live wiki "Page.Load()" before using this function.</summary>
        /// <returns>Returns redirection target page title as string. Or empty string, if this
        /// Page object does not redirect anywhere.</returns>
        public string RedirectsTo()
        {
            if (IsRedirect())
                return site.redirectRE.Match(text).Groups[1].ToString().Trim();
            else
                return string.Empty;
        }

        /// <summary>If this page is a redirection, this function loads the title and text
        /// of redirected-to page into this Page object.</summary>
        public void ResolveRedirect()
        {
            if (IsRedirect())
            {
                lastRevisionID = null;
                title = RedirectsTo();
                Load();
            }
        }

        /// <summary>Returns true, if this page is a disambiguation page. Don't forget to load
        /// actual page contents from live wiki  before using this function. Local redirect
        /// templates of Wikimedia sites are also recognized, but if this extended functionality
        /// is undesirable, then just set appropriate disambiguation template's title in
        /// "disambigStr" variable of Site object. Use "|" as a delimiter when enumerating
        /// several templates in "disambigStr" variable.</summary>
        /// <returns>Returns bool value.</returns>
        public bool IsDisambig()
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (!string.IsNullOrEmpty(site.disambigStr))
                return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambigStr + ")}}");
            Console.WriteLine(Bot.Msg("Initializing disambiguation template tags..."));
            site.disambigStr = "disambiguation|disambig|dab";
            Uri res = new Uri("http://en.wikipedia.org/w/index.php?title=Template:Disambig/doc" +
                "&action=raw&ctype=text/plain&dontcountme=s");
            string buffer = text;
            text = Bot.GetWebResource(res, "");
            string[] iw = GetInterWikiLinks();
            foreach (string s in iw)
                if (s.StartsWith(site.language + ":"))
                {
                    site.disambigStr += "|" + s.Substring(s.LastIndexOf(":") + 1,
                        s.Length - s.LastIndexOf(":") - 1);
                    break;
                }
            text = buffer;
            return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambigStr + ")}}");
        }

        /// <summary>This internal function removes the namespace prefix from page title.</summary>
        public void RemoveNSPrefix()
        {
            title = site.RemoveNSPrefix(title, 0);
        }

        /// <summary>Function changes default English namespace prefixes to correct local prefixes
        /// (e.g. for German wiki sites it changes "Category:..." to "Kategorie:...").</summary>
        public void CorrectNSPrefix()
        {
            title = site.CorrectNSPrefix(title);
        }

        #endregion

        #region links

        /// <summary>Returns the array of strings, containing all wikilinks ([[...]])
        /// found in page text, excluding links in image descriptions, but including
        /// interwiki links, links to sister projects, categories, images, etc.</summary>
        /// <returns>Returns raw links in strings array.</returns>
        public string[] GetAllLinks()
        {
            MatchCollection matches = Site.wikiLinkRE.Matches(text);
            string[] matchStrings = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
                matchStrings[i] = matches[i].Groups[1].Value;
            return matchStrings;
        }

        /// <summary>Finds all internal wikilinks in page text, excluding interwiki
        /// links, links to sister projects, categories, embedded images and links in
        /// image descriptions.</summary>
        /// <returns>Returns the PageList object, in which page titles are the wikilinks,
        /// found in text.</returns>
        public PageList GetLinks()
        {
            MatchCollection matches = Site.wikiLinkRE.Matches(text);
            StringCollection exclLinks = new StringCollection();
            exclLinks.AddRange(GetInterWikiLinks());
            exclLinks.AddRange(GetSisterWikiLinks(true));
            string str;
            int fragmentPosition;
            PageList pl = new PageList(site);
            for (int i = 0; i < matches.Count; i++)
            {
                str = matches[i].Groups[1].Value;
                if (str.StartsWith(site.namespaces["6"] + ":", true, site.langCulture) ||
                    str.StartsWith(Site.wikiNSpaces["6"] + ":", true, site.langCulture) ||
                    str.StartsWith(site.namespaces["14"] + ":", true, site.langCulture) ||
                    str.StartsWith(Site.wikiNSpaces["14"] + ":", true, site.langCulture))
                    continue;
                str = str.TrimStart(':');
                if (exclLinks.Contains(str))
                    continue;
                fragmentPosition = str.IndexOf("#");
                if (fragmentPosition != -1)
                    str = str.Substring(0, fragmentPosition);
                pl.Add(new Page(site, str));
            }
            return pl;
        }

        /// <summary>Returns the array of strings, containing external links,
        /// found in page text.</summary>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetExternalLinks()
        {
            MatchCollection matches = Site.webLinkRE.Matches(text);
            string[] matchStrings = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
                matchStrings[i] = matches[i].Value;
            return matchStrings;
        }

        /// <summary>Returns the array of strings, containing interwiki links,
        /// found in page text. But no displayed links are returned,
        /// like [[:de:Stern]] - these are returned by GetSisterWikiLinks(true)
        /// function. Interwiki links are returned without square brackets.</summary>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetInterWikiLinks()
        {
            return GetInterWikiLinks(false);
        }

        /// <summary>Returns the array of strings, containing interwiki links,
        /// found in page text. Displayed links like [[:de:Stern]] are not returned,
        /// these are returned by GetSisterWikiLinks(true) function.</summary>
        /// <param name="inSquareBrackets">Pass "true" to get interwiki links
        ///in square brackets, for example "[[de:Stern]]", otherwise the result
        /// will be like "de:Stern", without brackets.</param>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetInterWikiLinks(bool inSquareBrackets)
        {
            if (string.IsNullOrEmpty(Site.WMLangsStr))
                site.GetWikimediaWikisList();
            MatchCollection matches = Site.iwikiLinkRE.Matches(text);
            string[] matchStrings = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                matchStrings[i] = matches[i].Groups[1].Value;
                if (inSquareBrackets)
                    matchStrings[i] = "[[" + matchStrings[i] + "]]";
            }
            return matchStrings;
        }

        /// <summary>Adds interwiki links to the page. It doesn't remove or replace old
        /// interwiki links, this can be done by calling RemoveInterWikiLinks function
        /// or manually, if necessary.</summary>
        /// <param name="iwikiLinks">Interwiki links as an array of strings, with or
        /// without square brackets, for example: "de:Stern" or "[[de:Stern]]".</param>
        public void AddInterWikiLinks(string[] iwikiLinks)
        {
            if (iwikiLinks.Length == 0)
                throw new ArgumentNullException("iwikiLinks");
            List<string> iwList = new List<string>(iwikiLinks);
            AddInterWikiLinks(iwList);
        }

        /// <summary>Adds interwiki links to the page. It doesn't remove or replace old
        /// interwiki links, this can be done by calling RemoveInterWikiLinks function
        /// or manually, if necessary.</summary>
        /// <param name="iwikiLinks">Interwiki links as List of strings, with or
        /// without square brackets, for example: "de:Stern" or "[[de:Stern]]".</param>
        public void AddInterWikiLinks(List<string> iwikiLinks)
        {
            if (iwikiLinks.Count == 0)
                throw new ArgumentNullException("iwikiLinks");
            if (iwikiLinks.Count == 1 && iwikiLinks[0] == null)
                iwikiLinks.Clear();
            for (int i = 0; i < iwikiLinks.Count; i++)
                iwikiLinks[i] = iwikiLinks[i].Trim("[]\f\n\r\t\v ".ToCharArray());
            iwikiLinks.AddRange(GetInterWikiLinks());
            SortInterWikiLinks(ref iwikiLinks);
            RemoveInterWikiLinks();
            text += "\r\n";
            foreach (string str in iwikiLinks)
                text += "\r\n[[" + str + "]]";
        }

        /// <summary>Sorts interwiki links in page text according to site rules.
        /// Only rules for some Wikipedia projects are implemented so far.
        /// In other cases links are ordered alphabetically.</summary>
        public void SortInterWikiLinks()
        {
            AddInterWikiLinks(new string[] { null });
        }

        /// <summary>This internal function sorts interwiki links in page text according 
        /// to site rules. Only rules for some Wikipedia projects are implemented
        /// so far. In other cases links are ordered alphabetically.</summary>
        /// <param name="iwList">Interwiki links without square brackets in
        /// List object, either ordered or unordered.</param>
        public void SortInterWikiLinks(ref List<string> iwList)
        {
            string[] iwikiLinksOrder = null;
            if (iwList.Count < 2)
                return;
            switch (site.site)
            {		// special sort orders
                case "http://en.wikipedia.org":
                case "http://simple.wikipedia.org":
                case "http://be-x-old.wikipedia.org":
                case "http://lb.wikipedia.org":
                case "http://mk.wikipedia.org":
                case "http://no.wikipedia.org":
                case "http://pl.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLocal; break;
                case "http://ms.wikipedia.org":
                case "http://et.wikipedia.org":
                case "http://vi.wikipedia.org":
                case "http://fi.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLocalFW; break;
                case "http://sr.wikipedia.org": iwikiLinksOrder = Site.iwikiLinksOrderByLatinFW; break;
                default: iwList.Sort(); break;
            }
            if (iwikiLinksOrder == null)
                return;
            List<string> sortedIwikiList = new List<string>();
            string prefix;
            foreach (string iwikiLang in iwikiLinksOrder)
            {
                prefix = iwikiLang + ":";
                foreach (string iwikiLink in iwList)
                    if (iwikiLink.StartsWith(prefix))
                        sortedIwikiList.Add(iwikiLink);
            }
            foreach (string iwikiLink in iwList)
                if (!sortedIwikiList.Contains(iwikiLink))
                    sortedIwikiList.Add(iwikiLink);
            iwList = sortedIwikiList;
            switch (site.site)
            {		// special sort orders, based on default iwList.Sort();
                case "http://hu.wikipedia.org":
                case "http://he.wikipedia.org": iwList.Remove("en"); iwList.Insert(0, "en"); break;
                case "http://nn.wikipedia.org":
                    iwList.Remove("no"); iwList.Remove("sv"); iwList.Remove("da");
                    iwList.InsertRange(0, new string[] { "no", "sv", "da" }); break;
                case "http://te.wikipedia.org":
                    iwList.Remove("en"); iwList.Remove("hi"); iwList.Remove("kn");
                    iwList.Remove("ta"); iwList.Remove("ml");
                    iwList.InsertRange(0, new string[] { "en", "hi", "kn", "ta", "ml" }); break;
                case "http://yi.wikipedia.org":
                    iwList.Remove("en"); iwList.Remove("he"); iwList.Remove("de");
                    iwList.InsertRange(0, new string[] { "en", "he", "de" }); break;
                case "http://ur.wikipedia.org":
                    iwList.Remove("ar"); iwList.Remove("fa"); iwList.Remove("en");
                    iwList.InsertRange(0, new string[] { "ar", "fa", "en" }); break;
            }
        }

        /// <summary>Removes all interwiki links from text of page.</summary>
        public void RemoveInterWikiLinks()
        {
            if (string.IsNullOrEmpty(Site.WMLangsStr))
                site.GetWikimediaWikisList();
            text = Site.iwikiLinkRE.Replace(text, "");
            text = text.TrimEnd("\r\n".ToCharArray());
        }

        /// <summary>Returns the array of strings, containing links to sister Wikimedia
        /// Foundation Projects, found in page text.</summary>
        /// <param name="includeDisplayedInterWikiLinks">Include displayed interwiki
        /// links like "[[:de:Stern]]".</param>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetSisterWikiLinks(bool includeDisplayedInterWikiLinks)
        {
            if (string.IsNullOrEmpty(Site.WMLangsStr))
                site.GetWikimediaWikisList();
            MatchCollection sisterMatches = Site.sisterWikiLinkRE.Matches(text);
            MatchCollection iwikiMatches = Site.iwikiDispLinkRE.Matches(text);
            int size = (includeDisplayedInterWikiLinks == true) ?
                sisterMatches.Count + iwikiMatches.Count : sisterMatches.Count;
            string[] matchStrings = new string[size];
            int i = 0;
            for (; i < sisterMatches.Count; i++)
                matchStrings[i] = sisterMatches[i].Groups[1].Value;
            if (includeDisplayedInterWikiLinks == true)
                for (int j = 0; j < iwikiMatches.Count; i++, j++)
                    matchStrings[i] = iwikiMatches[j].Groups[1].Value;
            return matchStrings;
        }

        #endregion

        #region html<>wiki

        /// <summary>Function converts basic HTML markup in page text to wiki
        /// markup, except for tables markup, that is left unchanged. Use
        /// ConvertHtmlTablesToWikiTables function to convert HTML
        /// tables markup to wiki format.</summary>
        public void ConvertHtmlMarkupToWikiMarkup()
        {
            text = Regex.Replace(text, "(?is)n?<(h1)( [^/>]+?)?>(.+?)</\\1>n?", "\n= $3 =\n");
            text = Regex.Replace(text, "(?is)n?<(h2)( [^/>]+?)?>(.+?)</\\1>n?", "\n== $3 ==\n");
            text = Regex.Replace(text, "(?is)n?<(h3)( [^/>]+?)?>(.+?)</\\1>n?", "\n=== $3 ===\n");
            text = Regex.Replace(text, "(?is)n?<(h4)( [^/>]+?)?>(.+?)</\\1>n?", "\n==== $3 ====\n");
            text = Regex.Replace(text, "(?is)n?<(h5)( [^/>]+?)?>(.+?)</\\1>n?",
                "\n===== $3 =====\n");
            text = Regex.Replace(text, "(?is)n?<(h6)( [^/>]+?)?>(.+?)</\\1>n?",
                "\n====== $3 ======\n");
            text = Regex.Replace(text, "(?is)\n?\n?<p( [^/>]+?)?>(.+?)</p>", "\n\n$2");
            text = Regex.Replace(text, "(?is)<a href ?= ?[\"'](http:[^\"']+)[\"']>(.+?)</a>",
                "[$1 $2]");
            text = Regex.Replace(text, "(?i)</?(b|strong)>", "'''");
            text = Regex.Replace(text, "(?i)</?(i|em)>", "''");
            text = Regex.Replace(text, "(?i)\n?<hr ?/?>\n?", "\n----\n");
            text = Regex.Replace(text, "(?i)<(hr|br)( [^/>]+?)? ?/?>", "<$1$2 />");
        }

        /// <summary>Function converts HTML table markup in page text to wiki
        /// table markup.</summary>
        public void ConvertHtmlTablesToWikiTables()
        {
            if (!text.Contains("</table>"))
                return;
            text = Regex.Replace(text, ">\\s+<", "><");
            text = Regex.Replace(text, "<table( ?[^>]*)>", "\n{|$1\n");
            text = Regex.Replace(text, "</table>", "|}\n");
            text = Regex.Replace(text, "<caption( ?[^>]*)>", "|+$1 | ");
            text = Regex.Replace(text, "</caption>", "\n");
            text = Regex.Replace(text, "<tr( ?[^>]*)>", "|-$1\n");
            text = Regex.Replace(text, "</tr>", "\n");
            text = Regex.Replace(text, "<th([^>]*)>", "!$1 | ");
            text = Regex.Replace(text, "</th>", "\n");
            text = Regex.Replace(text, "<td([^>]*)>", "|$1 | ");
            text = Regex.Replace(text, "</td>", "\n");
            text = Regex.Replace(text, "\n(\\||\\|\\+|!) \\| ", "\n$1 ");
            text = text.Replace("\n\n|", "\n|");
        }

        #endregion

        #region categories

        /// <summary>Returns the array of strings, containing category names found in
        /// page text with namespace prefix, but without sorting keys. Use the result
        /// strings to call FillFromCategory(string) or FillFromCategoryTree(string)
        /// function. Categories, added by templates, are not returned. Use GetAllCategories
        /// function to get such categories too.</summary>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetCategories()
        {
            return GetCategories(true, false);
        }

        /// <summary>Returns the array of strings, containing category names found in
        /// page text. Categories, added by templates, are not returned. Use GetAllCategories
        /// function to get categories added by templates too.</summary>
        /// <param name="withNameSpacePrefix">If true, function returns strings with
        /// namespace prefix like "Category:Stars", not just "Stars".</param>
        /// <param name="withSortKey">If true, function returns strings with sort keys,
        /// if found. Like "Stars|D3" (in [[Category:Stars|D3]]), not just "Stars".</param>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetCategories(bool withNameSpacePrefix, bool withSortKey)
        {
            MatchCollection matches = site.wikiCategoryRE.Matches(
                Regex.Replace(text, "(?is)<nowiki>.+?</nowiki>", ""));
            string[] matchStrings = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                matchStrings[i] = matches[i].Groups[4].Value.Trim();
                if (withSortKey == true)
                    matchStrings[i] += matches[i].Groups[5].Value.Trim();
                if (withNameSpacePrefix == true)
                    matchStrings[i] = site.namespaces["14"] + ":" + matchStrings[i];
            }
            return matchStrings;
        }

        /// <summary>Returns the array of strings, containing category names found in
        /// page text and added by page's templates. Categories are returned  with
        /// namespace prefix, but without sorting keys. Use the result strings
        /// to call FillFromCategory(string) or FillFromCategoryTree(string).</summary>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetAllCategories()
        {
            return GetAllCategories(true);
        }

        /// <summary>Returns the array of strings, containing category names found in
        /// page text and added by page's templates.</summary>
        /// <param name="withNameSpacePrefix">If true, function returns strings with
        /// namespace prefix like "Category:Stars", not just "Stars".</param>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetAllCategories(bool withNameSpacePrefix)
        {
            string uri;
            if (Bot.useBotQuery == true && site.botQuery == true && site.ver >= new Version(1, 15))
                uri = site.site + site.indexPath +
                    "api.php?action=query&prop=categories" +
                    "&clprop=sortkey|hidden&cllimit=5000&format=xml&titles=" +
                    HttpUtility.UrlEncode(title);
            else
                uri = site.site + site.indexPath + "index.php?title=" +
                    HttpUtility.UrlEncode(title) + "&redirect=no";

            string xpathQuery;
            if (Bot.useBotQuery == true && site.botQuery == true && site.ver >= new Version(1, 15))
                xpathQuery = "//categories/cl/@title";
            else if (site.ver >= new Version(1, 13))
                xpathQuery = "//ns:div[ @id='mw-normal-catlinks' or @id='mw-hidden-catlinks' ]" +
                    "/ns:span/ns:a";
            else
                xpathQuery = "//ns:div[ @id='catlinks' ]/ns:p/ns:span/ns:a";

            string src = site.GetPageHTM(uri);
            if (Bot.useBotQuery != true || site.botQuery != true || site.ver < new Version(1, 15))
            {
                int startPos = src.IndexOf("<!-- start content -->");
                int endPos = src.IndexOf("<!-- end content -->");
                if (startPos != -1 && endPos != -1 && startPos < endPos)
                    src = src.Remove(startPos, endPos - startPos);
                else
                {
                    startPos = src.IndexOf("<!-- bodytext -->");
                    endPos = src.IndexOf("<!-- /bodytext -->");
                    if (startPos != -1 && endPos != -1 && startPos < endPos)
                        src = src.Remove(startPos, endPos - startPos);
                }
            }

            XPathNodeIterator iterator = site.GetXMLIterator(src, xpathQuery);
            string[] matchStrings = new string[iterator.Count];
            iterator.MoveNext();
            for (int i = 0; i < iterator.Count; i++)
            {
                matchStrings[i] = (withNameSpacePrefix ? site.namespaces["14"] + ":" : "") +
                    site.RemoveNSPrefix(HttpUtility.HtmlDecode(iterator.Current.Value), 14);
                iterator.MoveNext();
            }

            return matchStrings;
        }

        /// <summary>Adds the page to the specified category by adding
        /// link to that category in page text. If the link to the specified category
        /// already exists, the function does nothing.</summary>
        /// <param name="categoryName">Category name, with or without prefix.
        /// Sort key can also be included after "|", like "Category:Stars|D3".</param>
        public void AddToCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");
            categoryName = site.RemoveNSPrefix(categoryName, 14);
            string cleanCategoryName = !categoryName.Contains("|") ? categoryName.Trim()
                : categoryName.Substring(0, categoryName.IndexOf('|')).Trim();
            string[] categories = GetCategories(false, false);
            foreach (string category in categories)
                if (category == Bot.Capitalize(cleanCategoryName) ||
                    category == Bot.Uncapitalize(cleanCategoryName))
                    return;
            string[] iw = GetInterWikiLinks();
            RemoveInterWikiLinks();
            text += (categories.Length == 0 ? "\r\n" : "") +
                "\r\n[[" + site.namespaces["14"] + ":" + categoryName + "]]\r\n";
            if (iw.Length != 0)
                AddInterWikiLinks(iw);
            text = text.TrimEnd("\r\n".ToCharArray());
        }

        /// <summary>Removes the page from category by deleting link to that category in
        /// page text.</summary>
        /// <param name="categoryName">Category name, with or without prefix.</param>
        public void RemoveFromCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                throw new ArgumentNullException("categoryName");
            categoryName = site.RemoveNSPrefix(categoryName, 14).Trim();
            categoryName = !categoryName.Contains("|") ? categoryName
                : categoryName.Substring(0, categoryName.IndexOf('|'));
            string[] categories = GetCategories(false, false);
            if (Array.IndexOf(categories, Bot.Capitalize(categoryName)) == -1 &&
                Array.IndexOf(categories, Bot.Uncapitalize(categoryName)) == -1)
                return;
            string regexCategoryName = Regex.Escape(categoryName);
            regexCategoryName = regexCategoryName.Replace("_", "\\ ").Replace("\\ ", "[_\\ ]");
            int firstCharIndex = (regexCategoryName[0] == '\\') ? 1 : 0;
            regexCategoryName = "[" + char.ToLower(regexCategoryName[firstCharIndex]) +
                char.ToUpper(regexCategoryName[firstCharIndex]) + "]" +
                regexCategoryName.Substring(firstCharIndex + 1);
            text = Regex.Replace(text, @"\[\[((?i)" + site.namespaces["14"] + "|" +
                Site.wikiNSpaces["14"] + "): ?" + regexCategoryName + @"(\|.*?)?]]\r?\n?", "");
            text = text.TrimEnd("\r\n".ToCharArray());
        }

        #endregion

        #region templates

        /// <summary>Returns the array of strings, containing titles of templates, found on page.
        /// The "msgnw:" template modifier is not returned.
        /// Links to templates (like [[:Template:...]]) are not returned. Templates,
        /// mentioned inside &lt;nowiki&gt;&lt;/nowiki&gt; tags are also not returned. The
        /// "magic words" (see http://meta.wikimedia.org/wiki/Help:Magic_words) are recognized and
        /// not returned by this function as templates. When using this function on text of the
        /// template, parameters names and numbers (like {{{link}}} and {{{1}}}) are not returned
        /// by this function as templates too.</summary>
        /// <param name="withNameSpacePrefix">If true, function returns strings with
        /// namespace prefix like "Template:SomeTemplate", not just "SomeTemplate".</param>
        /// <returns>Returns the string[] array. Duplicates are possible.</returns>
        public string[] GetTemplates(bool withNameSpacePrefix)
        {
            string str = Site.noWikiMarkupRE.Replace(text, "");
            if (GetNamespace() == 10)
                str = Regex.Replace(str, @"\{\{\{.*?}}}", "");
            MatchCollection matches = Regex.Matches(str, @"(?s)\{\{(.+?)(}}|\|)");
            string[] matchStrings = new string[matches.Count];
            string match = "", matchLowerCase = "";
            int j = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                match = matches[i].Groups[1].Value;
                matchLowerCase = match.ToLower();
                foreach (string mediaWikiVar in Site.mediaWikiVars)
                    if (matchLowerCase == mediaWikiVar)
                    {
                        match = "";
                        break;
                    }
                if (string.IsNullOrEmpty(match))
                    continue;
                foreach (string parserFunction in Site.parserFunctions)
                    if (matchLowerCase.StartsWith(parserFunction))
                    {
                        match = "";
                        break;
                    }
                if (string.IsNullOrEmpty(match))
                    continue;
                if (match.StartsWith("msgnw:") && match.Length > 6)
                    match = match.Substring(6);
                match = site.RemoveNSPrefix(match, 10).Trim();
                if (withNameSpacePrefix)
                    matchStrings[j++] = site.namespaces["10"] + ":" + match;
                else
                    matchStrings[j++] = match;
            }
            Array.Resize(ref matchStrings, j);
            return matchStrings;
        }

        /// <summary>Returns the array of strings, containing templates, found on page
        /// Everything inside braces is returned with all parameters
        /// untouched. Links to templates (like [[:Template:...]]) are not returned. Templates,
        /// mentioned inside &lt;nowiki&gt;&lt;/nowiki&gt; tags are also not returned. The
        /// "magic words" (see http://meta.wikimedia.org/wiki/Help:Magic_words) are recognized and
        /// not returned by this function as templates. When using this function on text of the
        /// template (on [[Template:NNN]] page), parameters names and numbers (like {{{link}}} 
        /// and {{{1}}}) are not returned by this function as templates too.</summary>
        /// <returns>Returns the string[] array.</returns>
        public string[] GetTemplatesWithParams()
        {
            Dictionary<int, int> templPos = new Dictionary<int, int>();
            StringCollection templates = new StringCollection();
            int startPos, endPos, len = 0;
            string str = text;
            while ((startPos = str.LastIndexOf("{{")) != -1)
            {
                endPos = str.IndexOf("}}", startPos);
                len = (endPos != -1) ? endPos - startPos + 2 : 2;
                if (len != 2)
                    templPos.Add(startPos, len);
                str = str.Remove(startPos, len);
                str = str.Insert(startPos, new String('_', len));
            }
            string[] templTitles = GetTemplates(false);
            Array.Reverse(templTitles);
            foreach (KeyValuePair<int, int> pos in templPos)
                templates.Add(text.Substring(pos.Key + 2, pos.Value - 4));
            for (int i = 0; i < templTitles.Length; i++)
                while (i < templates.Count &&
                    !templates[i].StartsWith(templTitles[i]) &&
                    !templates[i].StartsWith(site.namespaces["10"].ToString() + ":" +
                        templTitles[i], true, site.langCulture) &&
                    !templates[i].StartsWith(Site.wikiNSpaces["10"].ToString() + ":" +
                        templTitles[i], true, site.langCulture) &&
                    !templates[i].StartsWith("msgnw:" + templTitles[i]))
                    templates.RemoveAt(i);
            string[] arr = new string[templates.Count];
            templates.CopyTo(arr, 0);
            Array.Reverse(arr);
            return arr;
        }

        /// <summary>Adds a specified template to the end of the page text
        /// (right before categories).</summary>
        /// <param name="templateText">Complete template in double brackets,
        /// e.g. "{{TemplateTitle|param1=val1|param2=val2}}".</param>
        public void AddTemplate(string templateText)
        {
            if (string.IsNullOrEmpty(templateText))
                throw new ArgumentNullException("templateText");
            Regex templateInsertion = new Regex("([^}]\n|}})\n*\\[\\[((?i)" +
                Regex.Escape(site.namespaces["14"].ToString()) + "|" +
                Regex.Escape(Site.wikiNSpaces["14"].ToString()) + "):");
            if (templateInsertion.IsMatch(text))
                text = templateInsertion.Replace(text, "$1\n" + templateText + "\n\n[[" +
                    site.namespaces["14"] + ":", 1);
            else
            {
                string[] iw = GetInterWikiLinks();
                RemoveInterWikiLinks();
                text += "\n\n" + templateText;
                if (iw.Length != 0)
                    AddInterWikiLinks(iw);
                text = text.TrimEnd("\r\n".ToCharArray());
            }
        }

        /// <summary>Removes all instances of a specified template from page text.</summary>
        /// <param name="templateTitle">Title of template to remove.</param>
        public void RemoveTemplate(string templateTitle)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            templateTitle = Regex.Escape(templateTitle);
            templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +
                Char.ToLower(templateTitle[0]) + ")" +
                (templateTitle.Length > 1 ? templateTitle.Substring(1) : "");
            text = Regex.Replace(text, @"(?s)\{\{\s*" + templateTitle +
                @"(.*?)}}\r?\n?", "");
        }

        /// <summary>Returns specified parameter of a specified template. If several instances
        /// of specified template are found in text of this page, all parameter values
        /// are returned.</summary>
        /// <param name="templateTitle">Title of template to get parameter of.</param>
        /// <param name="templateParameter">Title of template's parameter. If parameter is
        /// untitled, specify it's number as string. If parameter is titled, but it's number is
        /// specified, the function will return empty List &lt;string&gt; object.</param>
        /// <returns>Returns the List &lt;string&gt; object with strings, containing values of
        /// specified parameters in all found template instances. Returns empty List &lt;string&gt;
        /// object if no specified template parameters were found.</returns>
        public List<string> GetTemplateParameter(string templateTitle, string templateParameter)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            if (string.IsNullOrEmpty(templateParameter))
                throw new ArgumentNullException("templateParameter");
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            List<string> parameterValues = new List<string>();
            Dictionary<string, string> parameters;
            templateTitle = templateTitle.Trim();
            templateParameter = templateParameter.Trim();
            Regex templateTitleRegex = new Regex("^\\s*(" +
                Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +
                Bot.Uncapitalize(Regex.Escape(templateTitle)) +
                ")\\s*\\|");
            foreach (string template in GetTemplatesWithParams())
            {
                if (templateTitleRegex.IsMatch(template))
                {
                    parameters = site.ParseTemplate(template);
                    if (parameters.ContainsKey(templateParameter))
                        parameterValues.Add(parameters[templateParameter]);
                }
            }
            return parameterValues;
        }

        /// <summary>This helper method returns specified parameter of a first found instance of
        /// specified template. If no such template or no such parameter was found,
        /// empty string "" is returned.</summary>
        /// <param name="templateTitle">Title of template to get parameter of.</param>
        /// <param name="templateParameter">Title of template's parameter. If parameter is
        /// untitled, specify it's number as string. If parameter is titled, but it's number is
        /// specified, the function will return empty List &lt;string&gt; object.</param>
        /// <returns>Returns parameter as string or empty string "".</returns>
        /// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this
        /// function.</remarks>
        public string GetFirstTemplateParameter(string templateTitle, string templateParameter)
        {
            List<string> paramsList = GetTemplateParameter(templateTitle, templateParameter);
            if (paramsList.Count == 0)
                return "";
            else return paramsList[0];
        }

        /// <summary>Sets the specified parameter of the specified template to new value.
        /// If several instances of specified template are found in text of this page, either
        /// first value can be set, or all values in all instances.</summary>
        /// <param name="templateTitle">Title of template.</param>
        /// <param name="templateParameter">Title of template's parameter.</param>
        /// <param name="newParameterValue">New value to set the parameter to.</param>
        /// <param name="firstTemplateOnly">When set to true, only first found template instance
        /// is modified. When set to false, all found template instances are modified.</param>
        /// <returns>Returns the number of modified values.</returns>
        /// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this
        /// function.</remarks>
        public int SetTemplateParameter(string templateTitle, string templateParameter,
            string newParameterValue, bool firstTemplateOnly)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            if (string.IsNullOrEmpty(templateParameter))
                throw new ArgumentNullException("templateParameter");
            if (string.IsNullOrEmpty(templateParameter))
                throw new ArgumentNullException("newParameterValue");
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            int i = 0;
            Dictionary<string, string> parameters;
            templateTitle = templateTitle.Trim();
            templateParameter = templateParameter.Trim();
            Regex templateTitleRegex = new Regex("^\\s*(" +
                Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +
                Bot.Uncapitalize(Regex.Escape(templateTitle)) +
                ")\\s*\\|");
            foreach (string template in GetTemplatesWithParams())
            {
                if (templateTitleRegex.IsMatch(template))
                {
                    parameters = site.ParseTemplate(template);
                    parameters[templateParameter] = newParameterValue;
                    Regex oldTemplate = new Regex(Regex.Escape(template));
                    string newTemplate = site.FormatTemplate(templateTitle, parameters, template);
                    newTemplate = newTemplate.Substring(2, newTemplate.Length - 4);
                    text = oldTemplate.Replace(text, newTemplate, 1);
                    i++;
                    if (firstTemplateOnly == true)
                        break;
                }
            }
            return i;
        }

        #endregion

        #region other

        /// <summary>Returns the array of strings, containing names of files,
        /// embedded in page, including images in galleries (inside "gallery" tag).
        /// But no links to images and files, like [[:Image:...]] or [[:File:...]] or
        /// [[Media:...]].</summary>
        /// <param name="withNameSpacePrefix">If true, function returns strings with
        /// namespace prefix like "Image:Example.jpg" or "File:Example.jpg",
        /// not just "Example.jpg".</param>
        /// <returns>Returns the string[] array. The array can be empty (of size 0). Strings in
        /// array may recur, indicating that file was mentioned several times on the page.</returns>
        public string[] GetImages(bool withNameSpacePrefix)
        {
            return GetImagesEx(withNameSpacePrefix, false);
        }

        /// <summary>Returns the array of strings, containing names of files,
        /// mentioned on a page.</summary>
        /// <param name="withNameSpacePrefix">If true, function returns strings with
        /// namespace prefix like "Image:Example.jpg" or "File:Example.jpg",
        /// not just "Example.jpg".</param>
        /// <param name="includeFileLinks">If true, function also returns links to images,
        /// like [[:Image:...]] or [[:File:...]] or [[Media:...]]</param>
        /// <returns>Returns the string[] array. The array can be empty (of size 0).Strings in
        /// array may recur, indicating that file was mentioned several times on the page.</returns>
        public string[] GetImagesEx(bool withNameSpacePrefix, bool includeFileLinks)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");
            string nsPrefixes = "File|Image|" + Regex.Escape(site.namespaces["6"].ToString());
            if (includeFileLinks)
            {
                nsPrefixes += "|" + Regex.Escape(site.namespaces["-2"].ToString()) + "|" +
                    Regex.Escape(Site.wikiNSpaces["-2"].ToString());
            }
            MatchCollection matches;
            if (Regex.IsMatch(text, "(?is)<gallery>.*</gallery>"))
                matches = Regex.Matches(text, "(?i)" + (includeFileLinks ? "" : "(?<!:)") +
                    "(" + nsPrefixes + ")(:)(.*?)(\\||\r|\n|]])");		// FIXME: inexact matches
            else
                matches = Regex.Matches(text, @"\[\[" + (includeFileLinks ? ":?" : "") +
                    "(?i)((" + nsPrefixes + @"):(.+?))(\|(.+?))*?]]");
            string[] matchStrings = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                if (withNameSpacePrefix == true)
                    matchStrings[i] = site.namespaces["6"] + ":" + matches[i].Groups[3].Value;
                else
                    matchStrings[i] = matches[i].Groups[3].Value;
            }
            return matchStrings;
        }

        /// <summary>Identifies the namespace of the page.</summary>
        /// <returns>Returns the integer key of the namespace.</returns>
        public int GetNamespace()
        {
            title = title.TrimStart(new char[] { ':' });
            foreach (DictionaryEntry ns in site.namespaces)
            {
                if (title.StartsWith(ns.Value + ":", true, site.langCulture))
                    return int.Parse(ns.Key.ToString());
            }
            foreach (DictionaryEntry ns in Site.wikiNSpaces)
            {
                if (title.StartsWith(ns.Value + ":", true, site.langCulture))
                    return int.Parse(ns.Key.ToString());
            }
            return 0;
        }

        /// <summary>Sends page title to console.</summary>
        public void ShowTitle()
        {
            Console.Write("\n" + Bot.Msg("The title of this page is \"{0}\".") + "\n", title);
        }

        /// <summary>Sends page text to console.</summary>
        public void ShowText()
        {
            Console.Write("\n" + Bot.Msg("The text of \"{0}\" page:"), title);
            Console.Write("\n\n" + text + "\n\n");
        }

        /// <summary>Renames the page.</summary>
        /// <param name="newTitle">New title of that page.</param>
        /// <param name="reason">Reason for renaming.</param>
        public void RenameTo(string newTitle, string reason)
        {
            if (string.IsNullOrEmpty(newTitle))
                throw new ArgumentNullException("newTitle");
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to rename."));
            //Page mp = new Page(site, "Special:Movepage/" + HttpUtility.UrlEncode(title));
            Page mp = new Page(site, "Special:Movepage/" + title);
            mp.GetEditSessionData();
            if (string.IsNullOrEmpty(mp.editSessionToken))
                throw new WikiBotException(string.Format(
                    Bot.Msg("Unable to rename page \"{0}\" to \"{1}\"."), title, newTitle));
            if (Bot.askConfirm)
            {
                Console.Write("\n\n" +
                    Bot.Msg("The page \"{0}\" is going to be renamed to \"{1}\".\n"),
                    title, newTitle);
                if (!Bot.UserConfirms())
                    return;
            }
            string postData = string.Format("wpNewTitle={0}&wpOldTitle={1}&wpEditToken={2}" +
                "&wpReason={3}", HttpUtility.UrlEncode(newTitle), HttpUtility.UrlEncode(title),
                HttpUtility.UrlEncode(mp.editSessionToken), HttpUtility.UrlEncode(reason));
            string respStr = site.PostDataAndGetResultHTM(site.indexPath +
                "index.php?title=Special:Movepage&action=submit", postData);
            if (Site.editSessionTokenRE2.IsMatch(respStr))
                throw new WikiBotException(string.Format(
                    Bot.Msg("Failed to rename page \"{0}\" to \"{1}\"."), title, newTitle));
            Console.WriteLine(
                Bot.Msg("Page \"{0}\" was successfully renamed to \"{1}\"."), title, newTitle);
            title = newTitle;
        }

        /// <summary>Deletes the page. Sysop rights are needed to delete page.</summary>
        /// <param name="reason">Reason for deleting.</param>
        public void Delete(string reason)
        {
            if (string.IsNullOrEmpty(title))
                throw new WikiBotException(Bot.Msg("No title specified for page to delete."));
            string respStr1 = site.GetPageHTM(site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title) + "&action=delete");
            editSessionToken = Site.editSessionTokenRE1.Match(respStr1).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                editSessionToken = Site.editSessionTokenRE2.Match(respStr1).Groups[1].ToString();
            if (string.IsNullOrEmpty(editSessionToken))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Unable to delete page \"{0}\"."), title));
            if (Bot.askConfirm)
            {
                Console.Write("\n\n" + Bot.Msg("The page \"{0}\" is going to be deleted.\n"), title);
                if (!Bot.UserConfirms())
                    return;
            }
            string postData = string.Format("wpReason={0}&wpEditToken={1}",
                HttpUtility.UrlEncode(reason), HttpUtility.UrlEncode(editSessionToken));
            string respStr2 = site.PostDataAndGetResultHTM(site.indexPath + "index.php?title=" +
                HttpUtility.UrlEncode(title) + "&action=delete", postData);
            if (Site.editSessionTokenRE2.IsMatch(respStr2))
                throw new WikiBotException(
                    string.Format(Bot.Msg("Failed to delete page \"{0}\"."), title));
            Console.WriteLine(Bot.Msg("Page \"{0}\" was successfully deleted."), title);
            title = "";
        }

        /// <summary>Retrieves the title for this Page object using page's numeric revision ID
        /// (also called "oldid"), stored in "lastRevisionID" object's property. Make sure that
        /// "lastRevisionID" property is set before calling this function. Use this function
        /// when working with old revisions to detect if the page was renamed at some
        /// moment.</summary>
        public void GetTitle()
        {
            if (string.IsNullOrEmpty(lastRevisionID))
                throw new WikiBotException(
                    Bot.Msg("No revision ID specified for page to get title for."));
            string src = site.GetPageHTM(site.site + site.indexPath +
                "index.php?oldid=" + lastRevisionID);
            title = Regex.Match(src, "<h1 (?:id=\"firstHeading\" )?class=\"firstHeading\">" +
                "(.+?)</h1>").Groups[1].ToString();
        }

        #endregion
    }
}
	