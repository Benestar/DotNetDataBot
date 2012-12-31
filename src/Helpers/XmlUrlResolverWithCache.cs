// DotNetWikiBot Framework 2.101 - bot framework based on Microsoft .NET Framework 2.0 for wiki projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright (c) Iaroslav Vassiliev (2006-2012) codedriller@gmail.com

// DotNetDataBot Framework 1.1 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
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

namespace DotNetDataBot.Helpers
{

    /// <summary>Class defines custom XML URL resolver, that has a caching capability. See
    /// http://www.w3.org/blog/systeam/2008/02/08/w3c_s_excessive_dtd_traffic for details.</summary>
    //[PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    class XmlUrlResolverWithCache : XmlUrlResolver
    {
        /// <summary>List of cached files absolute URIs.</summary>
        static string[] cachedFilesURIs = {
			"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent"
			//http://www.mediawiki.org/xml/export-0.4/ http://www.mediawiki.org/xml/export-0.4.xsd
		};
        /// <summary>List of cached files names.</summary>
        static string[] cachedFiles = {
			"xhtml1-transitional.dtd",
			"xhtml-lat1.ent",
			"xhtml-symbol.ent",
			"xhtml-special.ent"
		};
        /// <summary>Local cache directory.</summary>
        static string cacheDir = "Cache" + Path.DirectorySeparatorChar;

        /// <summary>Overriding GetEntity() function to implement local cache.</summary>
        /// <param name="absoluteUri">Absolute URI of requested entity.</param>
        /// <param name="role">User's role for accessing specified URI.</param>
        /// <param name="ofObjectToReturn">Type of object to return.</param>
        /// <returns>Returns object or requested type.</returns>
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            for (int i = 0; i < XmlUrlResolverWithCache.cachedFilesURIs.Length; i++)
                if (absoluteUri.OriginalString == XmlUrlResolverWithCache.cachedFilesURIs[i])
                    return new FileStream(XmlUrlResolverWithCache.cacheDir +
                        XmlUrlResolverWithCache.cachedFiles[i],
                        FileMode.Open, FileAccess.Read, FileShare.Read);
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
    }
}