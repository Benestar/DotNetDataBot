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
