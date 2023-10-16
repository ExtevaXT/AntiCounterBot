# AntiCounterBot

It is same as counterbot but free and should go ahead of it on page (At least will harass it)

<hr>

1. Counterbot have {price} -> cache it
2. Add item to market for {price-1}
3. Counterbot instantly sets {price-2} and it have ~60s cooldown
4. Set item price as {price-3} and we are on page
5. Repeat after our cooldown

<hr>

- If failed run again after short delay to not lose advantage
- We should be always ahead of him
- If we lost advantage re-add item
- If we are first in offers stick to second {price-1} to anticounter it again
- Logging in log.txt
<hr>

## IMPORTANT
1. If this bitch have > 1 bots they will gangbang you
2. This is partially fixed by automatically re-adding item
3. If counterbot user placed 2 bots in different time they will prevent your advantage but you still will harass them
4. Multiprocess maybe will work

##  Guide:
1. Use `config.txt` to specify market api key. Also you can specify threshold to re-add item (4 should be enough), step of lowering price, keep price after re-adding item to harass them more (0 enable, -1 disable), and API url (I tested it only with tf2.tm)
2. Follow instructions in console
3. Price API format: 100.00 is 10000
4. Restart if error is after specifying item (Market is very unstable because counterbots flood it)
5. If error after main loop you can ignore it

