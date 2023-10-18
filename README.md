# AntiCounterBot

It is same as counterbot but free and should go ahead of it on page (At least will harass it)

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

## IMPORTANT
1. If counterbot user placed 2 bots in different time they may prevent your advantage but you still will harass them
2. Multiprocess maybe will work
3. Price API format: 100.00 is 10000

##  Guide:
1. Use `config.txt` to specify market api key. Also you can specify threshold to re-add item, step of lowering price, and API url (I tested it only with tf2.tm)
2. Follow instructions in console
3. Restart if error before main loop
4. If error after main loop you can ignore it

