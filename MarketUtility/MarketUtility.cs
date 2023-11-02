using Newtonsoft.Json;
using External;
using System.Net;
using ConsoleTables;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using Jint;
using Jint.CommonJS;

// Quick util for: 
//    - removing / setting price for items 
//    - checking lowest prices
//    - adding items for sale
//    - updating inventory

// TODO keys autobuy / auto orders

// Market ID is same for all actions with your offers
class MarketUtility
{
    static HttpClient client;
    static string token;

    static int check_key_price = 0;
    static string api;
    static string key;
    static async Task Main(string[] args)
    {
        //var engine = new Engine(options =>
        //{
        //    options.EnableModules(@"C:\Users\PC\Project\AntiCounterBot\MarketUtility\node_modules");
            
        //});
        //engine.AddModule("custom", @"
        //    require {toSKU} from 'C:\\Users\\PC\\Project\\AntiCounterBot\\MarketUtility\\node_modules\\tf2-item-format\\dist\\toSKU.js';
        //    import {parseSKU} from 'C:\\Users\\PC\\Project\\AntiCounterBot\\MarketUtility\\node_modules\\tf2-item-format\\dist\\parseSKU.js';
            
        //    const attributes = parseSKU('Taunt: The Fist Bump', true, true);
        //    const sku = toSKU(attributes);
        //    ");

        //var ns = engine.ImportModule("custom");
        //// Execute the JavaScript code
        //var result = ns.Get("sku").AsString();

        //Console.WriteLine(result);
        //Console.ReadLine();

        Console.Title = "MarketUtility";
        Utils.Config(typeof(MarketUtility));
        Console.WriteLine("-------------");
        Console.WriteLine("MarketUtility");
        Console.WriteLine("------------");
        HttpClient();
        Console.WriteLine("Enter command:");
        while (true)
        {
            Console.Write("> ");
            await Command(Console.ReadLine());
        }
    }
    static async Task Command(string input)
    {
        string[] s = input.Split(' ');
        string command = s[0];
        string[] args = s.Skip(1).ToArray();
        switch (command)
        {
            case "acb":
                ProcessStartInfo p_info = new ProcessStartInfo
                {
                    FileName = "AntiCounterBot.exe",
                    Arguments = string.Join(" ", args),
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                Process.Start(p_info);
                break;
            case "inventory":
                await Inventory(args);
                break;
            case "offers":
                await Offers();
                break;
            case "update":
                await Update();
                break;
            case "add":
                await Add(args);
                break;
            case "set":
                await Set(args);
                break;
            case "check":
                await CheckBatch(input.Replace("check ", "").Split(';').ToList(), true);
                break;
            case "reload":
                HttpClient();
                break;
            case "kill":
                foreach (var process in Process.GetProcessesByName(args[0]))
                    process.Kill();
                Console.WriteLine($"Killed {args[0]} >_<");
                break;
            case "help":
                Console.WriteLine("https://github.com/ExtevaXT/AntiCounterBot");
                break;
        }
    }
    static void HttpClient()
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(600);
        Console.WriteLine("Initialized HttpClient");
    }
    static async Task Update()
    {
        var json = await Request($"{api}/update-inventory/?key={key}");
        if (json != null)
        {
            bool success = JsonConvert.DeserializeObject<Request.UpdateInventory>(json).success;
            if (success) Console.WriteLine("Requested market inventory update");
            // It actually will update after 5s-10min
        }
    }
    static async Task Add(string[] args)
    {
        var json = await Request($"{api}/add-to-sale?key={key}&id={args[0]}&price={args[1]}&cur=RUB");
        if (json == null) return;
        var response = JObject.Parse(json);
        if (!(bool)response["success"])
        {
            Console.WriteLine(response["error"]);
            return;
        }
        Console.WriteLine(Utils.Log($"Added {args[0]} for {args[1]} -> {response["item_id"]}"));
    }
    static async Task Set(string[] args)
    {
        var json = await Request($"{api}/set-price?key={key}&item_id={args[0]}&price={args[1]}&cur=RUB");
        if (json == null) return;

        var response = JObject.Parse(json);
        if (!(bool)response["success"])
        {
            Console.WriteLine(response["error"]);
            return;
        }
        Console.WriteLine(Utils.Log($"Setted price {args[1]} for {args[0]}"));
    }
    static async Task<Dictionary<string, string>> CheckBatch(List<string> names, bool display = false)
    {
        string s = $"{api}/search-list-items-by-hash-name-all/?key={key}&extended=0";
        foreach (string name in names)
            s += $"&list_hash_name[]={name}";
        Dictionary<string, string> result = new Dictionary<string, string>();
        //&list_hash_name[]=[market_hash_name]
        var json = await Request(s);
        if (json != null)
        {
            var response = JsonConvert.DeserializeObject<Request.BatchOffers>(json);
            if (response.success)
            {
                var items = response.data;
                var table = new ConsoleTable("Market ID", "Name", "Class-Instance", "Price");
                foreach (KeyValuePair<string, List<Request.BatchItem>> offers in items)
                {
                    result.Add(offers.Key, offers.Value.First().price);
                    foreach (var item in offers.Value)
                    {
                        table.AddRow(item.id, offers.Key, $"{item.@class}-{item.instance}", item.price);
                    }
                }
                if (display)
                {
                    Console.WriteLine("Searched offers:");
                    table.Write();
                }
            }
            else
            {
                Console.WriteLine(response.error);
            }
        }
        return result;
    }
    static async Task Offers()
    {
        if (check_key_price == 1) Token();
        var json = await Request($"{api}/items/?key={key}");
        if (json != null)
        {
            var response = JsonConvert.DeserializeObject<Request.InventoryOffers>(json);
            if (response.success)
            {
                var items = response.items;
                List<string> names = items.Select(item => item.market_hash_name).ToList();
                Dictionary<string, string> prices = await CheckBatch(names);
                var table = new ConsoleTable("Market ID", "Name", "Class-Instance", "Price", "Lowest", "Keys");
                foreach (var item in items)
                {
                    prices.TryGetValue(item.market_hash_name, out string? lowest);
                    
                    string price = (item.price * 100).ToString() + " +";

                    if (lowest != null && double.Parse(lowest) / 100 < item.price)
                        price = (item.price * 100).ToString() + " -";

                    //var dict = await Price(item.market_hash_name);
                    double keys = 0;
                    //if (check_key_price == 1) keys = dict["item"]["sell"];
                    table.AddRow(item.item_id, item.market_hash_name, $"{item.classid}-{item.instanceid}", price, lowest, keys);
                }
                Console.WriteLine("Fetched your offers:");
                table.Write();
            }
        }
    }
    static async Task Inventory(string[] args)
    {
        string myInventory = $"{api}/my-inventory/?key={key}";
        var json = await Request(myInventory);
        if (json != null)
        {
            var response = JsonConvert.DeserializeObject<Request.Inventory>(json);
            if (response.success)
            {
                foreach (var item in response.items)
                {
                    if (args.Length == 0 || item.market_hash_name.ToLower().Contains(args[0].ToLower()))
                    {
                        Console.WriteLine($"{item.id}: {item.classid}-{item.instanceid} {item.market_hash_name}");
                    }
                }
            }
        }

    }

    static async Task<string?> Request(string api)
    {
        var content = await client.GetAsync(api);
        if (content.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine(content.StatusCode);
            return null;
        }
        var json = await content.Content.ReadAsStringAsync();
        return json;
    }
    static async Task Token()
    {
        string api = "https://api2.prices.tf/auth/access";
        StringContent jsonContent = new(
            JsonConvert.SerializeObject(new object()),
            Encoding.UTF8,
            "application/json");
        var content = await client.PostAsync(api, jsonContent);
        if (content.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine(content.StatusCode);
            return;
        }
        var json = await content.Content.ReadAsStringAsync();
        token = JObject.Parse(json)["accessToken"].ToString();
    }
    static async Task<Dictionary<string, Dictionary<string, double>>> Price(string name)
    {
        string sku = await GetSKU(name);
        // I am so fucking tired of existing
        // i literally spent 6 hours straight for fucking python interpreter in c# that can be done with fucking json []
        string api = "https://api2.prices.tf/prices/" + sku;
        client.DefaultRequestHeaders.Add("Authorization", token);
        var content = await client.GetAsync(api);
        if (content.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine(content.StatusCode);
            return null;
        }
        var json = await content.Content.ReadAsStringAsync();
        var response = JObject.Parse(json);
        // nice code
        var dictionary = new Dictionary<string, Dictionary<string, double>>
        {
            { "item", new Dictionary<string, double>
                {
                    { "buy", (int)response["buyKeys"] + (double)response["buyHalfScrap"] / 18 },
                    { "sell", (int)response["sellKeys"] + (double)response["sellHalfScrap"] / 18 }
                }
            },
            { "key", new Dictionary<string, double>
                {
                    { "buy", (double)response["buyKeyHalfScrap"] },
                    { "sell", (double)response["sellKeyHalfScrap"] }
                }
            }
        };
        return dictionary;
    }
    static async Task<string> GetSKU(string name)
    {
        return "";

    }
}//Taunt: The Fist Bump