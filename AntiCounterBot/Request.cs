using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiCounterBot
{
    internal class Request
    {
        // search-item-by-hash-name
        public class Offer
        {
            public string market_hash_name { get; set; }
            public int price { get; set; }
            public long @class { get; set; }
            public long instance { get; set; }
            public int count { get; set; }
        }

        public class Offers
        {
            public bool success { get; set; }
            public string currency { get; set; }
            public List<Offer> data { get; set; }
        }
        // my-inventory
        public class Item
        {
            public string id { get; set; }
            public string classid { get; set; }
            public string instanceid { get; set; }
            public string market_hash_name { get; set; }
            public double market_price { get; set; }
            public bool tradable { get; set; }
        }

        public class Inventory
        {
            public bool success { get; set; }
            public List<Item> items { get; set; }
        }
        // add-to-sale
        public class AddToSale
        {
            public bool success { get; set; }
            public int item_id { get; set; }
            public string? error { get; set; }
        }
        // set-price
        public class SetPrice
        {
            public bool success { get; set; }
            public string? error { get; set; }
        }
    }
}
