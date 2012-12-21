// DotNetDataBot Framework 1.0 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetDataBot.Exceptions
{
    /// <summary>
    /// Class for Exceptions caused by the api
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// The error code of the api request
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ApiException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code">The error code of the api request</param>
        public ApiException(string code)
        {
            this.code = code;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code">The error code of the api request</param>
        /// <param name="message">The message</param>
        public ApiException(string code, string message)
            : base(message)
        {
            this.code = code;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code">The error code of the api request</param>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        public ApiException(string code, string message, Exception innerException)
            : base(message, innerException)
        {
            this.code = code;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">The info</param>
        /// <param name="context">The context</param>
        public ApiException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    
    }
}
