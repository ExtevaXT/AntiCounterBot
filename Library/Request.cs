namespace External
{
    public class Request
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
            public long item_id { get; set; }
            public string? error { get; set; }
        }
        // set-price
        public class SetPrice
        {
            public bool success { get; set; }
            public string? error { get; set; }
        }
        // update-inventory
        public class UpdateInventory
        {
            public bool success { get; set; }
        }
        // items
        public class InventoryOffer
        {
            public string item_id { get; set; }
            public string assetid { get; set; }
            public string classid { get; set; }
            public string instanceid { get; set; }
            public string real_instance { get; set; }
            public string market_hash_name { get; set; }
            public int position { get; set; }
            public double price { get; set; }
            public string currency { get; set; }
            public string status { get; set; }
            public int live_time { get; set; }
            public object left { get; set; }
            public string botid { get; set; }
        }

        public class InventoryOffers
        {
            public bool success { get; set; }
            public List<InventoryOffer> items { get; set; }
        }
        // search-list-items-by-hash-name-all
        public class Extra
        {
            public object seller_steam_level { get; set; }
            public string asset { get; set; }
            public string percent_success { get; set; }
            public string average_time { get; set; }
            public int volume { get; set; }
        }

        public class BatchItem
        {
            public int id { get; set; }
            public string price { get; set; }
            public long @class { get; set; }
            public long instance { get; set; }
            public Extra extra { get; set; }
        }

        public class BatchOffers
        {
            public bool success { get; set; }
            public string error { get; set; }
            public string currency { get; set; }
            public Dictionary<string, List<BatchItem>> data { get; set; }
        }
        // STEAM
        public class SteamInventory
        {
            public List<Asset> assets { get; set; }
            public bool success { get; set; }
        }
        public class Asset
        {
            public int appid { get; set; }
            public string contextid { get; set; }
            public string assetid { get; set; }
            public string classid { get; set; }
            public string instanceid { get; set; }
            public string amount { get; set; }
        }
    }
}
