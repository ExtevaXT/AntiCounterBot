using Newtonsoft.Json;
using System.Net;
using System.Reflection;
using static AntiCounterBot.Request;
// TODO
// 1. Finishing after buying
// 2. Batch offers
// 3. MMF feature
// 4. Quick util for: 
//    - removing / setting price for items 
//    - checking lowest prices
//    - adding items for sale
//    - updating inventory
// On start check inventory and check prices for offers on sale
// Display offers and their prices, if it is not lowest display lowest price
// commands:
//  update. Update steam inventory
//  add {steam_id} {price}. Add item for sale. Cache and store market id
//  set {market_id} {price]. Set price for item offer
//  check {steam_id}. Display lowest prices for item
//  check all. Display lowest for your offers
class Program
{
    // Crutch for one time adding only TODO implement into loop
    static int? marketItem;
    static string itemName;
    static int lowest_price;
    static int price;
    static int delay = 15000;
    static int outranned = 0;

    // Runtime Initializing
    static int min_price;
    static int default_price;

    // Config Initializing
    static int step = 1;
    static int threshold = 4;
    static int keep_price = 1; // not working
    static int step_incrementing = 1;

    static int cooldown_delay = 60000;
    static int default_delay = 15000;
    static int short_delay = 5000;

    static string key;
    static string api;
    static async Task Main(string[] args)
    {
        try
        {
            Config();
            string path = Path.Combine(Environment.CurrentDirectory, "inventory.txt");
            Console.WriteLine("AntiCounterBot V2");
            Console.WriteLine("Fetching market inventory...");

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(600);
            Inventory? inventory = null;
            while (inventory == null)
            {
                string myInventory = $"{api}/my-inventory/?key={key}";
                var json = await Request(client, myInventory);
                if (json != null)
                    inventory = JsonConvert.DeserializeObject<Inventory>(json);
            }
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
            Console.WriteLine("Enter steam item id:");
            while(itemName == null)
            {
                steamItemId = Console.ReadLine();
                var item = inventory.items.Find(item => item.id == steamItemId);
                if (item == null)
                {
                    Console.WriteLine("Invalid id, try again");
                    continue;
                }
                itemName = item.market_hash_name;
            }
            Console.WriteLine(Log("Selected: " + itemName));
            Console.WriteLine("Default price, 0 to lowest:");
            default_price = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Min price to harass:");
            min_price = Convert.ToInt32(Console.ReadLine());
            if(default_price < 1000 || min_price < 1000)
            {
                Console.WriteLine("Are you sure?");
                Console.ReadLine();
            }
            // Main loop
            while (true)
            {
                await ProcessItem(api, key, client, steamItemId, itemName);
                await Task.Delay(delay); // Wait for 60 seconds before the next iteration
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(Log(ex.StackTrace));
            Console.WriteLine(Log(ex.Message));
            Console.ReadLine();
        }
        
    }
    static async Task ProcessItem(string api, string key, HttpClient client, string steamItemId, string itemName)
    {
        string d = $"[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}]";
        string? json = null;

        await CheckPrices(client);

        if (price <= min_price)
        {
            Console.WriteLine(Log($"{d} Reached min price, lowest: {lowest_price}"));
            delay = cooldown_delay;
            return;
        }
        // If we are first skip
        if(price == lowest_price)
        {
            delay = short_delay;
            return;
        }
        // Restart if they are banging us
        // If gap more than {threshold} iterations
        if (outranned >= threshold && marketItem != null)
        {
            if(keep_price != -1) keep_price = price;
            string removeItem = $"{api}/set-price?key={key}&item_id={marketItem}&price=0&cur=RUB";
            json = await Request(client, removeItem);
            if (json == null) return;
            Console.WriteLine(Log($"{d} Item was outranned, removed"));
            marketItem = null;
            // Let it cook
            delay = cooldown_delay + short_delay;
            return;
        }
        // First iteration
        if (marketItem == null)
        {
            if (keep_price < 0) price = keep_price;
            if(default_price != 0) price = default_price;
            //if (outranned > threshold && step_incrementing > 0) step += step_incrementing;
            string addToSale = $"{api}/add-to-sale?key={key}&id={steamItemId}&price={price}&cur=RUB";
            json = await Request(client, addToSale);
            if (json == null) return;
            var sale = JsonConvert.DeserializeObject<AddToSale>(json);
            if (!sale.success)
            {
                Console.WriteLine(sale.error);
                delay = short_delay;
                return;
            }
            marketItem = sale.item_id;
            Console.WriteLine(Log($"{d} Added item for {price}"));
            // Here comes counterbot
            // Let it cook
            await Task.Delay(default_delay);
            await CheckPrices(client);
            outranned = 0;
        }

        string setPrice = $"{api}/set-price?key={key}&item_id={marketItem}&price={price}&cur=RUB";
        json = await Request(client, setPrice);
        if (json == null) return;

        var response = JsonConvert.DeserializeObject<SetPrice>(json);
        if (!response.success)
        {
            Console.WriteLine(response.error);
            // It spams 'too often' if bitch starting to finding gap
            // It is limited to market ~60s cooldown
            if (response.error == "too_often")
                outranned++;
            delay = short_delay;
            return;
        }
        if (price != lowest_price)
            Console.WriteLine(Log($"{d} Anticountered {lowest_price} with {price}"));
        else Console.WriteLine("Item has lowest price");
        delay = default_delay;
        outranned = 0;
    }
    static async Task CheckPrices(HttpClient client)
    {
        string searchItemByName = $"{api}/search-item-by-hash-name/?key={key}&hash_name={itemName}";
        var json = await Request(client, searchItemByName);
        if (json == null) return;

        var offers = JsonConvert.DeserializeObject<Offers>(json);
        lowest_price = offers.data.First().price;
        if (lowest_price != price) 
            price = lowest_price - step;
        if (lowest_price == price && offers.data.Count>=2 && offers.data.First().count == 1)
            if(default_price == 0)
                price = offers.data[1].price - step;
            //else price = default_price;
        // default_price NOT WORKING, IT PLACES FIRST TIME ONLY / THEN GOES RE-ADD LOOP
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
        // If market is lagging we might spam it
        if (json == null) delay = short_delay;
        return json;
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