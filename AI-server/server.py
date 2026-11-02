# AI server that the Unity game talks to over HTTP (see Assets/Scripts/AI/AIServerClient.cs)
# Python handles the AI side; Unity spawns customers and sends price offers here
# The server decides whether an offer is accepted (so sales are reliable), and the LLM writes the customer's reaction
# Uses a local Ollama model - requires `brew install ollama` and `ollama pull llama3.2:3b`
# Run with: python server.py   (or: uvicorn server:app --port 8000 --reload)
import json
import os
import random
import threading
import uuid

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from characters import CHARACTERS

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://localhost:11434")  # local ollama install
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama3.2:3b")  # model being used currently - subject to change
OLLAMA_TIMEOUT = 60
MAX_SESSIONS = 50  # oldest customers are dropped past this so memory doesn't grow forever
COUNTER_RANGE = 1.5  # offers up to budget * this get a counter-offer, anything higher is rejected outright

# Used when Unity doesn't send its own inventory (e.g. testing with curl)
DEFAULT_SHOP_ITEMS = [
    {"name": "Apple", "price": 4, "stock": 5},
    {"name": "Orange", "price": 3, "stock": 5},
    {"name": "Box", "price": 15, "stock": 5},
]

# What the model must reply with - Ollama enforces this schema via the "format" field
REPLY_SCHEMA = {"type": "object", "properties": {"say": {"type": "string"}}, "required": ["say"]}

# Used when Ollama isn't reachable so the game still works
FALLBACK_LINES = {
    "accept": "Deal! Pleasure doing business.",
    "counter": "Too much. I'll give you ${counter}.",
    "reject": "That price is ridiculous!",
    "wrong_item": "That's not what I asked for. I want a {wants}.",
    "leave": "Forget it, I'm out of here.",
}

app = FastAPI(title="Shopkeeper Sim AI Server")

# Active customers in the shop, keyed by session id
sessions = {}
sessions_lock = threading.Lock()


class ShopItem(BaseModel):
    name: str
    price: int
    stock: int = 0


class SpawnRequest(BaseModel):
    character_id: str = ""  # empty = random character
    shop_items: list[ShopItem] = []


class OfferRequest(BaseModel):
    item: str  # item the shopkeeper is handing over
    price: int  # price the shopkeeper picked
    shop_items: list[ShopItem] = []


def shop_items_or_default(items):
    return [i.model_dump() for i in items] if items else DEFAULT_SHOP_ITEMS


def find_item(items, name):
    return next((i for i in items if i["name"].lower() == name.lower()), None)


# Picks what the customer wants from what the shop actually sells, preferring the character's favourites
def pick_wanted_item(character, items):
    favourites = [i for i in items if i["name"] in character["wants"]]
    in_stock = [i for i in (favourites or items) if i["stock"] > 0]
    return random.choice(in_stock or favourites or items)


# Game logic for an offer - returns (outcome, counter_offer, satisfaction_change)
def judge_offer(customer, item, price, listed_price):
    if item.lower() != customer["wants"].lower():
        return "wrong_item", 0, -5
    budget = customer["budget"]
    if price <= budget:
        reference = listed_price or budget
        change = 10 if price <= reference * 0.8 else 5 if price <= reference else 1
        return "accept", 0, change
    if price <= budget * COUNTER_RANGE:
        return "counter", budget, -3
    return "reject", 0, -8


def reaction_prompt(customer, item, price, listed_price, outcome, counter, leaving):
    c = customer["character"]
    situation = {
        "accept": f"You happily ACCEPT and buy it for ${price}.",
        "counter": f"It's more than you want to pay. You REFUSE ${price} and counter-offer ${counter}.",
        "reject": f"${price} is outrageous. You flatly REFUSE. Do NOT suggest any other price.",
        "wrong_item": f"That is the WRONG item - you asked for a {customer['wants']}, not a {item}. You refuse it.",
    }[outcome]
    if leaving:
        situation += " You've run out of patience and are LEAVING the shop now."
    previous = "; ".join(customer["offers"][-3:]) or "none"
    return f"""You are {c['name']}, a customer in a small 8-bit style shop. Stay in character.
Personality: {c['personality']}
Speech style: {c['speech_style']}
You came in to buy a {customer['wants']}. The shelf price is ${listed_price}. Your mood is {customer['satisfaction']}/100.
Earlier offers from the shopkeeper: {previous}.
The shopkeeper just offered you a {item} for ${price}.
{situation}
Say your reaction out loud in 1-2 short, expressive sentences. Only mention dollar amounts given above. Never reveal your budget.
Reply ONLY with JSON: {{"say": "..."}}"""


# Sends the prompt to Ollama and returns the customer's line (raises on network/parse failure)
def ask_llm(prompt):
    r = requests.post(
        f"{OLLAMA_URL}/api/chat",
        json={
            "model": OLLAMA_MODEL,
            "messages": [{"role": "user", "content": prompt}],
            "format": REPLY_SCHEMA,
            "stream": False,
            "options": {"temperature": 0.9, "num_predict": 80},
        },
        timeout=OLLAMA_TIMEOUT,
    )
    r.raise_for_status()
    return str(json.loads(r.json()["message"]["content"])["say"]).strip()


def get_customer(session_id):
    customer = sessions.get(session_id)
    if customer is None:
        raise HTTPException(status_code=404, detail=f"No customer with session id '{session_id}'")
    return customer


def public_state(customer):
    c = customer["character"]
    return {
        "session_id": customer["session_id"],
        "character_id": customer["character_id"],
        "name": c["name"],
        "color": c["color"],
        "wants": customer["wants"],
        "satisfaction": customer["satisfaction"],
        "status": customer["status"],
        "offers_left": c["max_offers"] - len(customer["offers"]),
    }


@app.get("/health")
def health():
    try:
        tags = requests.get(f"{OLLAMA_URL}/api/tags", timeout=3).json()
        models = [m["name"] for m in tags.get("models", [])]
        ollama_ok, model_ok = True, OLLAMA_MODEL in models
    except requests.RequestException:
        ollama_ok, model_ok = False, False
    return {"status": "ok", "model": OLLAMA_MODEL, "ollama_reachable": ollama_ok, "model_installed": model_ok}


@app.get("/characters")
def list_characters():
    return [{"id": cid, "name": c["name"], "personality": c["personality"], "color": c["color"]}
            for cid, c in CHARACTERS.items()]


# A new customer walks into the shop and says what they want
@app.post("/customers/spawn")
def spawn_customer(req: SpawnRequest):
    character_id = req.character_id.lower() or random.choice(list(CHARACTERS))
    if character_id not in CHARACTERS:
        raise HTTPException(status_code=404, detail=f"Unknown character '{req.character_id}'")
    character = CHARACTERS[character_id]
    items = shop_items_or_default(req.shop_items)
    item = pick_wanted_item(character, items)
    low, high = character["budget_range"]

    customer = {
        "session_id": uuid.uuid4().hex[:8],
        "character_id": character_id,
        "character": character,
        "wants": item["name"],
        "budget": max(1, round(item["price"] * random.uniform(low, high))),
        "satisfaction": character["satisfaction"],
        "status": "shopping",  # shopping -> bought | left | banned
        "offers": [],  # short summaries of previous offers, fed back into the prompt
    }
    with sessions_lock:
        if len(sessions) >= MAX_SESSIONS:
            sessions.pop(next(iter(sessions)))
        sessions[customer["session_id"]] = customer

    return {**public_state(customer), "say": character["greeting"].format(item=item["name"])}


# Shopkeeper hands over an item at a chosen price - returns the customer's reaction and whether it sold
@app.post("/customers/{session_id}/offer")
def make_offer(session_id: str, req: OfferRequest):
    customer = get_customer(session_id)
    if customer["status"] != "shopping":
        raise HTTPException(status_code=409, detail=f"{customer['character']['name']} has already {customer['status']}")

    items = shop_items_or_default(req.shop_items)
    listed = find_item(items, customer["wants"])
    listed_price = listed["price"] if listed else 0
    price = max(0, req.price)

    outcome, counter, change = judge_offer(customer, req.item, price, listed_price)
    customer["satisfaction"] = max(0, min(100, customer["satisfaction"] + change))
    customer["offers"].append(f"{req.item} for ${price} ({outcome})")
    sale = outcome == "accept"
    if sale:
        customer["status"] = "bought"
    elif len(customer["offers"]) >= customer["character"]["max_offers"] or customer["satisfaction"] <= 0:
        customer["status"] = "left"
    leaving = customer["status"] != "shopping"

    ai_online = True
    try:
        say = ask_llm(reaction_prompt(customer, req.item, price, listed_price, outcome, counter, leaving and not sale))
    except requests.RequestException:
        ai_online, say = False, ""
    except (json.JSONDecodeError, ValueError, TypeError, KeyError):
        say = ""
    if not say:
        key = "leave" if leaving and not sale else outcome
        say = FALLBACK_LINES[key].format(counter=counter, wants=customer["wants"])

    return {
        **public_state(customer),
        "say": say,
        "outcome": outcome,  # accept | counter | reject | wrong_item
        "counter_offer": counter,
        "sale": sale,
        "sale_item": customer["wants"] if sale else "",
        "sale_price": price if sale else 0,
        "leaving": leaving,
        "ai_online": ai_online,
    }


# Shopkeeper kicks the customer out
@app.post("/customers/{session_id}/ban")
def ban_customer(session_id: str):
    customer = get_customer(session_id)
    customer["status"] = "banned"
    customer["satisfaction"] = 0
    return {**public_state(customer), "say": customer["character"]["ban_line"].format(item=customer["wants"])}


@app.get("/customers/{session_id}")
def get_customer_state(session_id: str):
    return public_state(get_customer(session_id))


# Unity calls this when the customer object is destroyed
@app.delete("/customers/{session_id}")
def remove_customer(session_id: str):
    with sessions_lock:
        sessions.pop(session_id, None)
    return {"removed": session_id}


# Clears every customer (e.g. when a new in-game day starts)
@app.post("/reset")
def reset():
    with sessions_lock:
        sessions.clear()
    return {"status": "reset"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=int(os.getenv("PORT", "8000")))
