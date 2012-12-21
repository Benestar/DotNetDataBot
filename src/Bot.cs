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

namespace DotNetDataBot
{

	/// <summary>Class defines a Bot instance, some configuration settings
	/// and some auxiliary functions.</summary>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class Bot
    {
        #region variables

        /// <summary>Title and description of web agent.</summary>
		public static readonly string botVer = "DotNetWikiBot";
		/// <summary>Version of DotNetWikiBot Framework.</summary>
		public static readonly Version version = new Version("2.101");
		/// <summary>Desired bot's messages language (ISO 639-1 language code).
		/// If not set explicitly, the language will be detected automatically.</summary>
		/// <example><code>Bot.botMessagesLang = "fr";</code></example>
		public static string botMessagesLang = null;
		/// <summary>Default edit comment. You can set it to what you like.</summary>
		/// <example><code>Bot.editComment = "My default edit comment";</code></example>
		public static string editComment = "Automatic page editing";
		/// <summary>If set to true, all the bot's edits are marked as minor by default.</summary>
		public static bool isMinorEdit = true;
		/// <summary>If true, the bot uses "MediaWiki API" extension
		/// (special MediaWiki bot interface, "api.php"), if it is available.
		/// If false, the bot uses common user interface. True by default.
		/// Set it to false manually, if some problem with bot interface arises on site.</summary>
		/// <example><code>Bot.useBotQuery = false;</code></example>
		public static bool useBotQuery = true;
		/// <summary>Number of times to retry bot action in case of temporary connection failure or
		/// some other common net problems.</summary>
		public static int retryTimes = 3;
		/// <summary>If true, the bot asks user to confirm next Save, RenameTo or Delete operation.
		/// False by default. Set it to true manually, when necessary.</summary>
		/// <example><code>Bot.askConfirm = true;</code></example>
		public static bool askConfirm = false;
		/// <summary>If true, bot only reports errors and warnings. Call EnableSilenceMode
		/// function to enable that mode, don't change this variable's value manually.</summary>
		public static bool silenceMode = false;
		/// <summary>If set to some file name (e.g. "DotNetWikiBot_Report.txt"), the bot
		/// writes all output to that file instead of a console. If no path was specified,
		/// the bot creates that file in it's current directory. File is encoded in UTF-8.
		/// Call EnableLogging function to enable log writing, don't change this variable's
		/// value manually.</summary>
		public static string logFile = null;

		/// <summary>Array, containing localized DotNetWikiBot interface messages.</summary>
		public static SortedDictionary<string, string> messages =
			new SortedDictionary<string, string>();
		/// <summary>Internal web client, that is used to access sites.</summary>
		public static WebClient wc = new WebClient();
		/// <summary>Content type for HTTP header of web client.</summary>
		public static readonly string webContentType = "application/x-www-form-urlencoded";
		/// <summary>If true, assembly is running on Mono framework. If false,
		/// it is running on original Microsoft .NET Framework. This variable is set
		/// automatically, just get it's value, don't change it.</summary>
		public static readonly bool isRunningOnMono = (Type.GetType("Mono.Runtime") != null);
		/// <summary>Initial state of HttpWebRequestElement.UseUnsafeHeaderParsing boolean
		/// configuration setting. 0 means true, 1 means false, 2 means unchanged.</summary>
		public static int unsafeHttpHeaderParsingUsed = 2;

        #endregion

        /// <summary>This constructor is used to generate Bot object.</summary>
		/// <returns>Returns Bot object.</returns>
		static Bot()
		{
			if (botMessagesLang == null)
				botMessagesLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
			if (botMessagesLang != "en")
				if (!LoadLocalizedMessages(botMessagesLang))
					botMessagesLang = "en";
			botVer += "/" + version + " (" + Environment.OSVersion.VersionString + "; " +
				".NET CLR " + Environment.Version.ToString() + ")";
			ServicePointManager.Expect100Continue = false;
		}

		/// <summary>The destructor is used to uninitialize Bot objects.</summary>
		~Bot()
		{
			//if (unsafeHttpHeaderParsingUsed != 2)
				//SwitchUnsafeHttpHeaderParsing(unsafeHttpHeaderParsingUsed == 1 ? true : false);
		}

		/// <summary>Call this function to make bot write all output to the specified file
		/// instead of a console. If only error logging is desirable, first call this
		/// function, and after that call EnableSilenceMode function.</summary>
		/// <param name="logFileName">Path and name of a file to write output to.
		/// If no path was specified, the bot creates that file in it's current directory.
		/// File is encoded in UTF-8.</param>
		public static void EnableLogging(string logFileName)
		{
			logFile = logFileName;
			StreamWriter log = File.AppendText(logFile);
			log.AutoFlush = true;
			Console.SetError(log);
			if (!silenceMode)
				Console.SetOut(log);
		}

		/// <summary>Call this function to make bot report only errors and warnings,
		/// no other messages will be displayed or logged.</summary>
		public static void EnableSilenceMode()
		{
			silenceMode = true;
			Console.SetOut(new StringWriter());
		}

		/// <summary>Call this function to disable silent mode previously enabled by
		/// EnableSilenceMode() function.</summary>
		public static void DisableSilenceMode()
		{
			silenceMode = false;
			StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
			standardOutput.AutoFlush = true;
			Console.SetOut(standardOutput);
		}

		/// <summary>Function loads localized bot interface messages from 
		/// "DotNetWikiBot.i18n.xml" file. Function is called in Bot class constructor, 
		/// but can also be called manually to change interface language at runtime.</summary>
		/// <param name="language">Desired language's ISO 639-1 code.</param>
		/// <returns>Returns false, if messages for specified language were not found.
		/// Returns true on success.</returns>
		public static bool LoadLocalizedMessages(string language)
		{
			if (!File.Exists("DotNetWikiBot.i18n.xml")) {
				Console.Error.WriteLine("Localization file \"DotNetWikiBot.i18n.xml\" is missing.");
				return false;
			}
			using (XmlReader reader = XmlReader.Create("DotNetWikiBot.i18n.xml")) {
				if (!reader.ReadToFollowing(language)) {
					Console.Error.WriteLine("\nLocalized messages not found for language \"{0}\"." +
						"\nYou can help DotNetWikiBot project by translating the messages in\n" +
						"\"DotNetWikiBot.i18n.xml\" file and sending it to developers for " +
						"distribution.\n", language);
					return false;
				}
				if (!reader.ReadToDescendant("msg"))
					return false;
				else {
					if (messages.Count > 0)
						messages.Clear();
					messages[reader["id"]] = reader.ReadString();
				}
				while (reader.ReadToNextSibling("msg"))
					messages[reader["id"]] = reader.ReadString();
			}
			return true;
		}

		/// <summary>The function gets localized (translated) form of the specified bot
		/// interface message.</summary>
		/// <param name="message">Message itself, placeholders for substituted parameters are
		/// denoted in curly brackets like {0}, {1}, {2} and so on.</param>
		/// <returns>Returns localized form of the specified bot interface message,
		/// or English form if localized form was not found.</returns>
		public static string Msg(string message)
		{
			if (botMessagesLang == "en")
				return message;
			try {
				return messages[message];
			}
			catch (KeyNotFoundException) {
				return message;
			}
		}

		/// <summary>This function asks user to confirm next action. The message
		/// "Would you like to proceed (y/n/a)? " is displayed and user response is
		/// evaluated. Make sure to set "askConfirm" variable to "true" before
		/// calling this function.</summary>
		/// <returns>Returns true, if user has confirmed the action.</returns>
		/// <example><code>
		/// if (Bot.askConfirm) {
		///     Console.Write("Some action on live wiki is going to occur.\n\n");
		///     if(!Bot.UserConfirms())
		///         return;
		/// }
		/// </code></example>
		public static bool UserConfirms()
		{
			if (!askConfirm)
				return true;
			ConsoleKeyInfo k;
			Console.Write(Bot.Msg("Would you like to proceed (y/n/a)?") + " ");
			k = Console.ReadKey();
			Console.Write("\n");
			if (k.KeyChar == 'y')
				return true;
			else if (k.KeyChar == 'a') {
				askConfirm = false;
				return true;
			}
			else
				return false;
		}

		/// <summary>This auxiliary function counts the occurrences of specified string
		/// in specified text. This count is often needed, but strangely there is no
		/// such function in .NET Framework's String class.</summary>
		/// <param name="text">String to look in.</param>
        /// <param name="str">String to look for.</param>
		/// <param name="ignoreCase">Pass "true" if you need case-insensitive search.
		/// But remember that case-sensitive search is faster.</param>
		/// <returns>Returns the number of found occurrences.</returns>
		/// <example><code>int m = CountMatches("Bot Bot bot", "Bot", false); // =2</code></example>
		public static int CountMatches(string text, string str, bool ignoreCase)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");
			if (string.IsNullOrEmpty(str))
				throw new ArgumentNullException("result");
			int matches = 0;
			int position = 0;
			StringComparison rule = ignoreCase
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			while ((position = text.IndexOf(str, position, rule)) != -1) {
				matches++;
				position++;
			}
			return matches;
		}

		/// <summary>This auxiliary function returns the zero-based indexes of all occurrences
		/// of specified string in specified text.</summary>
		/// <param name="text">String to look in.</param>
        /// <param name="str">String to look for.</param>
		/// <param name="ignoreCase">Pass "true" if you need case-insensitive search.
		/// But remember that case-sensitive search is faster.</param>
		/// <returns>Returns the List of positions (zero-based integer indexes) of all found
		/// instances, or empty List if nothing was found.</returns>
		public static List<int> GetMatchesPositions(string text, string str, bool ignoreCase)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");
			if (string.IsNullOrEmpty(str))
				throw new ArgumentNullException("result");
			List<int> positions = new List<int>();
			StringComparison rule = ignoreCase
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			int position = 0;
			while ((position = text.IndexOf(str, position, rule)) != -1) {
				positions.Add(position);
				position++;
			}
			return positions;
		}

		/// <summary>This auxiliary function returns portion of the string which begins
		/// with some specified substring and ends with some specified substring.</summary>
		/// <param name="src">Source string.</param>
		/// <param name="startMark">Substring that the resultant string portion
		/// must begin with. Can be null.</param>
		/// <param name="endMark">Substring that the resultant string portion
		/// must end with. Can be null.</param>
		/// <returns>Final portion of the source string.</returns>
		public static string GetStringPortion(string src, string startMark, string endMark)
		{
			return GetStringPortionEx(src, startMark, endMark, false, false, true);
		}

		/// <summary>This auxiliary function returns portion of the string which begins
		/// with some specified substring and ends with some specified substring.</summary>
		/// <param name="src">Source string.</param>
		/// <param name="startMark">Substring that the resultant string portion
		/// must begin with. Can be null.</param>
		/// <param name="endMark">Substring that the resultant string portion
		/// must end with. Can be null.</param>
		/// <param name="removeStartMark">Don't include startMark in returned substring.
		/// Default is false.</param>
		/// <param name="removeEndMark">Don't include endMark in returned substring.
		/// Default is false.</param>
		/// <param name="raiseExceptionOnError">Raise ArgumentOutOfRangeException if specified
		/// startMark or endMark was not found. Default is true.</param>
		/// <returns>Final portion of the source string.</returns>
		public static string GetStringPortionEx(string src, string startMark, string endMark,
			bool removeStartMark, bool removeEndMark, bool raiseExceptionOnError)
		{
			if (string.IsNullOrEmpty(src))
				throw new ArgumentNullException("src");
			int startPos = 0;
			int endPos = src.Length;

			if (!string.IsNullOrEmpty(startMark)) {
				startPos = src.IndexOf(startMark);
				if (startPos == -1) {
					if (raiseExceptionOnError == true)
						throw new ArgumentOutOfRangeException("startPos");
					else
						startPos = 0;
				}
				else if (removeStartMark)
					startPos += startMark.Length;
			}

			if (!string.IsNullOrEmpty(endMark)) {
				endPos = src.IndexOf(endMark, startPos);
				if (endPos == -1) {
					if (raiseExceptionOnError == true)
						throw new ArgumentOutOfRangeException("endPos");
					else
						endPos = src.Length;
				}
				else if (!removeEndMark)
					endPos += endMark.Length;
			}

			return src.Substring(startPos, endPos - startPos);
		}

		/// <summary>This auxiliary function makes the first letter in specified string upper-case.
		/// This is often needed, but strangely there is no such function in .NET Framework's
		/// String class.</summary>
        /// <param name="str">String to capitalize.</param>
		/// <returns>Returns capitalized string.</returns>
		public static string Capitalize(string str)
		{
			return char.ToUpper(str[0]) + str.Substring(1);
		}

		/// <summary>This auxiliary function makes the first letter in specified string lower-case.
		/// This is often needed, but strangely there is no such function in .NET Framework's
		/// String class.</summary>
        /// <param name="str">String to uncapitalize.</param>
		/// <returns>Returns uncapitalized string.</returns>
		public static string Uncapitalize(string str)
		{
			return char.ToLower(str[0]) + str.Substring(1);
		}

		/// <summary>Suspends execution for specified number of seconds.</summary>
		/// <param name="seconds">Number of seconds to wait.</param>
		public static void Wait(int seconds)
		{
			Thread.Sleep(seconds * 1000);
		}

		/// <summary>This internal function switches unsafe HTTP headers parsing on or off.
		/// This is needed to ignore unimportant HTTP protocol violations,
		/// committed by misconfigured web servers.</summary>
		public static void SwitchUnsafeHttpHeaderParsing(bool enabled)
		{
			System.Configuration.Configuration config =
				System.Configuration.ConfigurationManager.OpenExeConfiguration(
					System.Configuration.ConfigurationUserLevel.None);
			System.Net.Configuration.SettingsSection section =
				(System.Net.Configuration.SettingsSection)config.GetSection("system.net/settings");
			if (unsafeHttpHeaderParsingUsed == 2)
				unsafeHttpHeaderParsingUsed = section.HttpWebRequest.UseUnsafeHeaderParsing ? 1 : 0;
			section.HttpWebRequest.UseUnsafeHeaderParsing = enabled;
			config.Save();
			System.Configuration.ConfigurationManager.RefreshSection("system.net/settings");
		}

		/// <summary>This internal function clears the CanonicalizeAsFilePath attribute in
		/// .NET UriParser to fix a major .NET bug when System.Uri incorrectly strips trailing 
		/// dots in URIs. The bug was discussed in details at:
		/// https://connect.microsoft.com/VisualStudio/feedback/details/386695/system-uri-in
		/// </summary>
		public static void DisableCanonicalizingUriAsFilePath()
		{
			MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			FieldInfo flagsField = typeof(UriParser).GetField("m_Flags",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (getSyntax != null && flagsField != null)
			{
				foreach (string scheme in new string[] { "http", "https" })
				{
					UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
					if (parser != null)
					{
						int flagsValue = (int)flagsField.GetValue(parser);
						// Clear the CanonicalizeAsFilePath attribute
						if ((flagsValue & 0x1000000) != 0)
							flagsField.SetValue(parser, flagsValue & ~0x1000000);
					}
				}
			}
		}

		/// <summary>This internal function removes all attributes from root XML/XHTML element
		/// (XML namespace declarations, schema links, etc.) for easy processing.</summary>
		/// <returns>Returns document without unnecessary declarations.</returns>
		public static string RemoveXMLRootAttributes(string xmlSource)
		{
			int startPos = ((xmlSource.StartsWith("<!") || xmlSource.StartsWith("<?"))
				&& xmlSource.IndexOf('>') != -1) ? xmlSource.IndexOf('>') + 1 : 0;
			int firstSpacePos = xmlSource.IndexOf(' ', startPos);
			int firstCloseTagPos = xmlSource.IndexOf('>', startPos);
			if (firstSpacePos != -1 && firstCloseTagPos != -1 && firstSpacePos < firstCloseTagPos)
				return xmlSource.Remove(firstSpacePos, firstCloseTagPos - firstSpacePos);
			return xmlSource;
		}

		/// <summary>This internal function initializes web client to get resources
		/// from web.</summary>
		public static void InitWebClient()
		{
			if (!Bot.isRunningOnMono)
				wc.UseDefaultCredentials = true;
			wc.Encoding = Encoding.UTF8;
			wc.Headers.Add("Content-Type", webContentType);
			wc.Headers.Add("User-agent", botVer);
		}

		/// <summary>This internal wrapper function gets web resource in a fault-tolerant manner.
		/// It should be used only in simple cases, because it sends no cookies, it doesn't support
		/// traffic compression and lacks other special features.</summary>
		/// <param name="address">Web resource address.</param>
		/// <param name="postData">Data to post with web request, can be "" or null.</param>
		/// <returns>Returns web resource as text.</returns>
		public static string GetWebResource(Uri address, string postData)
		{
			string webResourceText = null;
			for (int errorCounter = 0; true; errorCounter++) {
				try {
					Bot.InitWebClient();
					if (string.IsNullOrEmpty(postData))
						webResourceText = Bot.wc.DownloadString(address);
					else
						webResourceText = Bot.wc.UploadString(address, postData);
					break;
				}
				catch (WebException e) {
					if (errorCounter > retryTimes)
						throw;
					string message = e.Message;
					if (Regex.IsMatch(message, ": \\(50[02349]\\) ")) {		// Remote problem
						Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));
						Thread.Sleep(60000);
					}
					else if (message.Contains("Section=ResponseStatusLine")) {	// Squid problem
						SwitchUnsafeHttpHeaderParsing(true);
						Console.Error.WriteLine(message + " " + Bot.Msg("Retrying in 60 seconds."));
						Thread.Sleep(60000);
					}
					else
						throw;
				}
			}
			return webResourceText;
		}
	}
}