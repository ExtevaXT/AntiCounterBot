# AntiCounterBot

Offender of official CounterBot (bots that interrupt offers for a 0.01) ~~and market~~

It is same but free and should go ahead of it on page

For auto trades check out https://github.com/soyware/OpenMarketClient

<hr>

1. Counterbot have {price}
2. Add item to market for {price-step}
3. Counterbot instantly sets {price-step-1} and it have ~60s cooldown
4. Set item price as {price-step-step} and we are on page
5. Repeat after our cooldown

<hr>

- If failed run again after short delay to not lose advantage
- We should be always ahead of him
- If we lost advantage re-add item
- If we are first in offers stick to second {price-1} to anticounter it again
- Logging in `log.txt`
<hr>

![screenshot](screenshot.png)

## Important
1. If counterbot one user placed several bots they may prevent your advantage. It will spam re-adding
2. Multiprocess is working
3. Price API format: 100.00 is 10000

##  Guide:
1. Use `config.txt` to specify:
```csharp
    int step = 1;
    int threshold = 4; // {short_delay} times before re-adding
    int keep_price = 1; // not working
    int step_incrementing = 1; // not working

    int cooldown_delay = 60000;
    int default_delay = 15000;
    int short_delay = 5000;

    string key;
    string api; // Tested only tf2.tm
```
2. Follow instructions in console
3. Each `too_often` error - {short_delay} gap between lowest and yours
4. `BadGateway` or `InternalServerError` errors when market denies request or lagging

