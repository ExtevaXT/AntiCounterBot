using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

using External;
using Newtonsoft.Json.Linq;
using System;
// TODO
// 2. Batch offers
// 3. MMF feature

// bug if he removed offer we do not sticking to next lowest
public class AntiCounterBot
{
    // Crutch for one time adding only TODO implement into loop
    static long? marketItem;
    static string itemName;
    static int lowest_price;
    static int price;
    static int delay = 15000;
    static int outranned = 0;
    static Request.Inventory? inventory = null;
    static Request.Item? item = null;
    static string steamItemId;

    // Runtime Initializing
    static int min_price;
    static int default_price;

    // Config Initializing
    static int step = 1;
    static int threshold = 4;
    static int keep_price = 1; // not working
    static int step_incrementing = 1;

    static int single_target = 0;
    static int cooldown_delay = 60000;
    static int default_delay = 15000;
    static int short_delay = 5000;

    static string key;
    static string api;
    public static async Task Main(string[] args)
    {
        try
        {
            if(args.Length > 0) marketItem = long.Parse(args[0]);
            if (args.Length > 1) int.TryParse(args[1], out default_price);
            if (args.Length > 2) int.TryParse(args[2], out min_price);
            if (args.Length > 3) int.TryParse(args[3], out step);
            if (args.Length > 4) int.TryParse(args[4], out single_target);

            Console.Title = "AntiCounterBot";
            Utils.Config(typeof(AntiCounterBot));
            
            string path = Path.Combine(Environment.CurrentDirectory, "inventory.txt");
            Console.WriteLine("------------------");
            Console.WriteLine("AntiCounterBot V2");
            Console.WriteLine("-------------------");
            Console.WriteLine("Fetching market inventory...");

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(600);
            // Manual initialize
            if(marketItem == null)
            {
                while (true)
                {
                    string myInventory = $"{api}/my-inventory/?key={key}";
                    var json = await Request(client, myInventory);
                    if (json != null)
                    {
                        inventory = JsonConvert.DeserializeObject<Request.Inventory>(json);
                        break;
                    }
                    await Task.Delay(short_delay);
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
                Console.WriteLine("Enter steam item id:");
                while (itemName == null)
                {
                    steamItemId = Console.ReadLine();
                    item = inventory.items.Find(item => item.id == steamItemId);
                    if (item == null)
                    {
                        Console.WriteLine("Invalid id, try again");
                        continue;
                    }
                    itemName = item.market_hash_name;
                }
            }
            // Through args
            else
            {
                while (true)
                {
                    var json = await Request(client, $"{api}/items/?key={key}");
                    if (json != null)
                    {
                        var response = JsonConvert.DeserializeObject<Request.InventoryOffers>(json);
                        if (response.success)
                        {
                            var items = response.items;
                            var item = items.Find(x => x.item_id == marketItem.ToString());
                            if(item != null)
                            {
                                itemName = item.market_hash_name;
                                // This retards forgot to add steam asset id in existing offers
                                json = await Request(client, $"{api}/get-my-steam-id/?key={key}");
                                if (json != null)
                                {
                                    var steamid = JObject.Parse(json)["steamid64"];
                                    var appid = api.Contains("tf2") ? 440 : api.Contains("cs") ? 710 : api.Contains("dota") ? 570 : 0;
                                    json = await Request(client, $"https://steamcommunity.com/inventory/{steamid}/{appid}/2?l=english&count=5000");
                                    if (json != null)
                                    {
                                        var inventory = JsonConvert.DeserializeObject<Request.SteamInventory>(json);
                                        var steamItem = inventory.assets.First(asset => asset.classid == item.classid && asset.instanceid == item.instanceid);
                                        // Pray for finding right one
                                        steamItemId = steamItem.assetid;
                                        AntiCounterBot.item = new Request.Item() { classid = item.classid, instanceid = item.instanceid };
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    await Task.Delay(short_delay);
                }
            }
            
            Console.WriteLine(Utils.Log("Selected: " + itemName));
            Console.Title += $" | {itemName}";
            if(default_price == default(int))
            {
                Console.WriteLine("Default price, 0 to lowest:");
                default_price = Convert.ToInt32(Console.ReadLine());
            }
            if (min_price == default(int))
            {
                Console.WriteLine("Min price to harass:");
                min_price = Convert.ToInt32(Console.ReadLine());
            }
            if(default_price < 1000 && min_price < 1000)
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
            Utils.FlashWindow(Process.GetCurrentProcess().MainWindowHandle);
            Console.WriteLine(Utils.Log(ex.StackTrace));
            Console.WriteLine(Utils.Log(ex.Message));
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
            if(marketItem == null && default_price != 0)
            {
                price = default_price;
            }
            else
            {
                Console.WriteLine(Utils.Log($"{d} Reached min price, lowest: {lowest_price}"));
                delay = cooldown_delay;
                return;
            }
        }
        // If we are first skip
        if(price == lowest_price && marketItem != null)
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
            Console.WriteLine(Utils.Log($"{d} Item was outranned, removed"));
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
            var sale = JsonConvert.DeserializeObject<Request.AddToSale>(json);
            if (!sale.success)
            {
                Console.WriteLine(sale.error);
                delay = short_delay;
                return;
            }
            marketItem = sale.item_id;
            Console.WriteLine(Utils.Log($"{d} Added item for {price}"));
            // Here comes counterbot
            // Let it cook
            await Task.Delay(default_delay);
            await CheckPrices(client);
            outranned = 0;
        }

        string setPrice = $"{api}/set-price?key={key}&item_id={marketItem}&price={price}&cur=RUB";
        json = await Request(client, setPrice);
        if (json == null) return;

        var response = JsonConvert.DeserializeObject<Request.SetPrice>(json);
        if (!response.success)
        {
            if(response.error == "")
            {
                Console.WriteLine($"Finally {itemName} has been bought for {price}");
                Utils.FlashWindow(Process.GetCurrentProcess().MainWindowHandle);
                Console.Read();
            }
            Console.WriteLine(response.error);
            // It spams 'too often' if bitch starting to finding gap
            // It is limited to market ~60s cooldown
            if (response.error == "too_often")
                outranned++;
            delay = short_delay;
            return;
        }
        if (price == default_price)
        {
            Console.WriteLine($"Item has default {price}");
            delay = default_delay * 2;
        }
        else
        {
            if (price != lowest_price)
                Console.WriteLine(Utils.Log($"{d} Anticountered {lowest_price} with {price}"));
            else 
                Console.WriteLine("Item has lowest price");
            delay = default_delay;
        }
        outranned = 0;
    }
    static async Task CheckPrices(HttpClient client)
    {
        string searchItemByName = $"{api}/search-item-by-hash-name/?key={key}&hash_name={itemName}";
        var json = await Request(client, searchItemByName);
        if (json == null) return;

        var offers = JsonConvert.DeserializeObject<Request.Offers>(json);
        if(offers.data.Count == 0)
        {
            lowest_price = default_price;
            return;
        }
        lowest_price = offers.data.First().price;
        if (single_target == 1)
        {
            lowest_price = offers.data.First(offer => offer.instance.ToString() == item.instanceid && offer.@class.ToString() == item.classid).price;
        }

        if (lowest_price < min_price)
        {
            price = default_price;
        }
        else if (lowest_price != price)
        {
            price = lowest_price - step;
        }
        // If items are not stacked
        if (lowest_price == price && offers.data.Count >= 2 && offers.data.First().count == 1)
        {
            if (default_price == 0)
            {
                price = offers.data[1].price - step;
            }
        }
    }
    static async Task<string?> Request(HttpClient client, string api)
    {
        try
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
        catch {  return null; }
    }
}