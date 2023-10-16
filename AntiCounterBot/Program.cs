using Newtonsoft.Json;
using System.Net;
using System.Reflection;

//Counterbitch have { price } -> cache it
// Add item to market for {price-1}
// Loop
// Counterbitch instantly sets {price-2} and it have ~60s cooldown
// Set item price as {price-3} and we are on page
// Repeat after our ~60s cooldown

// - If failed run again after 5s to not lose advantage
// - We should be always ahead of him

// After ~720 iterations counterbitch mammoth must notice
// TODO If he re-added item to make advantage repeat from start
// If we are first in offers stick to second {price-1} to anticounter it again


// IMPORTANT
// Huge vulnerability: if this bitch have > 1 bots, they will gangbang you.
// They do not overbid theirselves so they autofind gap between cooldown.
class Program
{
    // Crutch for one time adding only TODO implement into loop
    static int? marketItem;
    static string itemName;
    static int lowest_price;
    static int price;
    static int delay = 15000;
    static int outranned = 0;

    static int step = 1;
    static int threshold = 4;
    static int keepPrice = 1;

    static string key;
    static string api;
    static async Task Main(string[] args)
    {
        Config();
        string path = Path.Combine(Environment.CurrentDirectory, "inventory.txt");
        Console.WriteLine("AntiCounterBot V2");
        Console.WriteLine("Fetching market inventory...");
        string myInventory = $"{api}/my-inventory/?key={key}";

        HttpClient client = new HttpClient();
        var json = await Request(client, myInventory);
        if(json == null)
        {
            Console.WriteLine("Market is dead, restart");
            return;
        }
        var inventory = JsonConvert.DeserializeObject<Inventory>(json);
        if (inventory != null)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (var item in inventory.items)
                {
                    string itemText = $"{item.id}: {item.market_hash_name}";
                    writer.WriteLine(itemText);
                }
            }
            Console.WriteLine($"Inventory written to {path}");
            string steamItemId = "";
            // TODO remove validId shit from gpt
            bool validId = false;
            Console.WriteLine("Enter steam item id:");
            do
            {
                steamItemId = Console.ReadLine();
                var item = inventory.items.Find(item => item.id == steamItemId);
                if (item != null)
                {
                    validId = true;
                    itemName = item.market_hash_name;
                }  
                else Console.WriteLine("Invalid id, try again");
            } 
            while (!validId);
            Console.WriteLine(Log("Selected: " + itemName));
            Console.WriteLine("Min price to harass:");
            int min_price = Convert.ToInt32(Console.ReadLine());

            while (true) // Run indefinitely
            {
                await ProcessItem(api, key, client, steamItemId, itemName, min_price);
                await Task.Delay(delay); // Wait for 60 seconds before the next iteration
            }
        }
    }
    static async Task ProcessItem(string api, string key, HttpClient client, string steamItemId, string itemName, int min_price)
    {
        string d = $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}]";
        string? json = null;

        await CheckPrices(client);

        if (price <= min_price)
        {
            Console.WriteLine(Log($"{d} Reached min price, lowest: {lowest_price}"));
            delay = 60000;
            return;
        }
        // If we are first skip
        if(price == lowest_price)
        {
            delay = 15000;
            return;
        }
        // Restart if they are banging us
        // If gap more than {threshold} iterations
        if (outranned >= threshold && marketItem != null)
        {
            if(keepPrice != -1) keepPrice = price;
            string removeItem = $"{api}/set-price?key={key}&item_id={marketItem}&price=0&cur=RUB";
            json = await Request(client, removeItem);
            if (json == null)
            {
                Console.WriteLine("Request denied");
                delay = 5000;
                return;
            }
            Console.WriteLine(Log($"{d} Item was outranned, removed"));
            marketItem = null;
            delay = 65000;
            return;
        }
        // First iteration
        if (marketItem == null)
        {
            if (keepPrice < 0) price = keepPrice;
            string addToSale = $"{api}/add-to-sale?key={key}&id={steamItemId}&price={price}&cur=RUB";
            json = await Request(client, addToSale);
            if (json == null)
            {
                Console.WriteLine("Request denied");
                delay = 5000;
                return;
            }
            var sale = JsonConvert.DeserializeObject<AddToSale>(json);
            if (!sale.success)
            {
                Console.WriteLine(Log($"{d} {sale.error}"));
                Console.ReadLine();
                return;
            }
            marketItem = sale.item_id;
            Console.WriteLine(Log($"{d} Added item for {price}"));
            // Here comes counterbitch
            // Let it cook
            await Task.Delay(10000);
            await CheckPrices(client);
            outranned = 0;
        }

        string setPrice = $"{api}/set-price?key={key}&item_id={marketItem}&price={price}&cur=RUB";
        json = await Request(client, setPrice);
        if (json == null)
        {
            Console.WriteLine("Request denied");
            delay = 5000;
            return;
        }
            
        var response = JsonConvert.DeserializeObject<SetPrice>(json);
        if (!response.success)
        {
            Console.WriteLine(response.error);
            // It spams 'too often' if bitch starting to finding gap
            // It is limited to market ~60s cooldown
            if (response.error == "too_often")
                outranned++;
            delay = 5000;
            return;
        }
        if (price != lowest_price)
            Console.WriteLine(Log($"{d} Anticountered {lowest_price} with {price}"));
        else Console.WriteLine("Item has lowest price");
        delay = 15000;
        outranned = 0;
    }
    static async Task CheckPrices(HttpClient client)
    {
        string searchItemByName = $"{api}/search-item-by-hash-name/?key={key}&hash_name={itemName}";
        var json = await Request(client, searchItemByName);
        if (json == null)
            return;

        var offers = JsonConvert.DeserializeObject<Offers>(json);
        lowest_price = offers.data.First().price;
        if (lowest_price != price) 
            price = lowest_price - step;
        if (lowest_price == price)
            price = offers.data[1].price - step;
    }
    static async Task<string?> Request(HttpClient client, string api)
    {
        var content = await client.GetAsync(api);
        if (content.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine(content.StatusCode);
            return null;
        }
        var json = await content.Content.ReadAsStringAsync();
        return await content.Content.ReadAsStringAsync();
    }
    static void Config()
    {
        string path = Path.Combine(Environment.CurrentDirectory, "config.txt");
        using (StreamReader reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] var = line.Split('=');
                Type type = typeof(Program);
                FieldInfo field = type.GetField(var[0], BindingFlags.Static | BindingFlags.NonPublic);
                if (int.TryParse(var[1], out int value))
                    field.SetValue(null, value);
                else field.SetValue(null, var[1]);
            }
        }
        Console.WriteLine("Loaded config with key: " + key);
    }
    static string Log(string message)
    {
        string path = Path.Combine(Environment.CurrentDirectory, "log.txt");
        using (StreamWriter writer = new StreamWriter(path, true))
            writer.WriteLine(message);
        return message;
    }
}
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