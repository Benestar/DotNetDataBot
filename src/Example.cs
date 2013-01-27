// DotNetDataBot Framework 1.3 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

using DotNetDataBot;

namespace MyBot
{
    class Example
    {
        public static void Main(string[] args)
        {
            Site site = new Site("http://www.wikidata.org", "username", "#####"); // your user info

            // Edit an existing item
            Item item = new Item(site, "Q2");
            item.setSiteLink("en", "Earth");    // set sitelink for item Q2
            item.lang = "en";                   // set the language for working to en
            item.setLabel("Earth");             // set the label (same to item.setLabel("en", "Earth");)
            item.setDescription("planet");      // set the description
            System.Collections.Generic.List<string> aliases
                = new System.Collections.Generic.List<string>();
                                                // create list for aliases
            aliases.Add("Terra");
            aliases.Add("the Blue Planet");
            item.setAliases(aliases);           // set the aliases

            // create a new item
            Item newItem = new Item(site);
            newItem.createItem("en", "Wikidata", "Wikidata", "database for collecting interwikilinks");
        }
    }
}
