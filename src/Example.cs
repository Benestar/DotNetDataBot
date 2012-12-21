// DotNetDataBot Framework 1.0 - bot framework based on Microsoft .NET Framework 2.0 for wikibase projects
// Distributed under the terms of the MIT (X11) license: http://www.opensource.org/licenses/mit-license.php
// Copyright © Bene* at http://www.wikidata.org (2012)

using DotNetDataBot;

namespace MyBot
{
    class Example
    {
        public static void Main(string[] args)
        {
            Site site = new Site("http://www.wikidata.org", "#####", "#####");
            Item item = new Item(site, "Q1000");
            item.setSiteLink("en", "Earth");

            Item newItem = new Item(site);
            newItem.createItem("en", "Wikidata", "database for collecting interwikilinks");
        }
    }
}
