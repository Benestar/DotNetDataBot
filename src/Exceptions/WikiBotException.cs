
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

namespace DotNetDataBot.Exceptions
{
    /// <summary>Class establishes custom application exceptions.</summary>
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [Serializable]
    public class WikiBotException : System.Exception
    {
        /// <summary>Just overriding default constructor.</summary>
        /// <returns>Returns Exception object.</returns>
        public WikiBotException() { }
        /// <summary>Just overriding constructor.</summary>
        /// <returns>Returns Exception object.</returns>
        public WikiBotException(string message)
            : base(message) { Console.Beep(); /*Console.ForegroundColor = ConsoleColor.Red;*/ }
        /// <summary>Just overriding constructor.</summary>
        /// <returns>Returns Exception object.</returns>
        public WikiBotException(string message, System.Exception inner)
            : base(message, inner) { }
        /// <summary>Just overriding constructor.</summary>
        /// <returns>Returns Exception object.</returns>
        protected WikiBotException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
        /// <summary>Destructor is invoked automatically when exception object becomes
        /// inaccessible.</summary>
        ~WikiBotException() { }
    }
}