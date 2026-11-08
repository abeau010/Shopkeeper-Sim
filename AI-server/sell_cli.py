# Sell to customers from the terminal without opening Unity (server must be running)
# Usage: python sell_cli.py [character_id]     e.g. python sell_cli.py vinnie
# Type an offer as "<item> <price>" e.g. "Apple 5". Other commands: /ban   /quit
import sys

import requests

SERVER = "http://127.0.0.1:8000"


def main():
    character_id = sys.argv[1] if len(sys.argv) > 1 else ""
    customer = requests.post(f"{SERVER}/customers/spawn", json={"character_id": character_id}).json()
    if "session_id" not in customer:
        print(customer)
        return
    sid, name = customer["session_id"], customer["name"]
    print(f"[{name} walks in - wants {customer['wants']} - mood {customer['satisfaction']} - {customer['offers_left']} offers]")
    print(f"{name}: {customer['say']}")

    while True:
        cmd = input("Offer (item price): ").strip()
        if cmd == "/quit":
            break
        if cmd == "/ban":
            print(f"{name}: {requests.post(f'{SERVER}/customers/{sid}/ban').json()['say']}")
            break
        try:
            item, price = cmd.rsplit(" ", 1)
            price = int(price)
        except ValueError:
            print("   Type an item and price, e.g. Apple 5")
            continue

        r = requests.post(f"{SERVER}/customers/{sid}/offer", json={"item": item, "price": price}).json()
        print(f"{name}: {r['say']}")
        print(f"   [{r['outcome']} | mood {r['satisfaction']} | offers left {r['offers_left']}"
              f"{' | counter $' + str(r['counter_offer']) if r['counter_offer'] else ''}]")
        if r["sale"]:
            print(f"   [SOLD {r['sale_item']} for ${r['sale_price']}]")
        if r["leaving"]:
            print(f"   [{name} leaves the shop]")
            break

    requests.delete(f"{SERVER}/customers/{sid}")


if __name__ == "__main__":
    main()
