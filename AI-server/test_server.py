# Tests for the AI server - the LLM is faked so these run without Ollama
# Run with: pytest -v
import re
import time
from pathlib import Path

import pytest
import requests
from fastapi.testclient import TestClient

import server

client = TestClient(server.app)
UNITY_ASSETS = Path(__file__).resolve().parent.parent / "Assets"
UNITY_ITEMS = UNITY_ASSETS / "Objects"
SHOP = [{"name": "Apple", "price": 4, "stock": 5}, {"name": "Box", "price": 15, "stock": 2}]


@pytest.fixture(autouse=True)
def setup(monkeypatch):
    server.sessions.clear()
    monkeypatch.setattr(server, "ask_llm", lambda prompt: "Fake reaction.")


def spawn(character_id="jeff", budget=None):
    r = client.post("/customers/spawn", json={"character_id": character_id, "shop_items": SHOP})
    assert r.status_code == 200
    data = r.json()
    if budget is not None:
        server.sessions[data["session_id"]]["budget"] = budget
    return data


def offer(customer, item, price):
    return client.post(f"/customers/{customer['session_id']}/offer",
                       json={"item": item, "price": price, "shop_items": SHOP})


def reaction(customer, offer_id):
    return client.get(f"/customers/{customer['session_id']}/reaction/{offer_id}")


def slow_llm(seconds):
    def answer(prompt):
        time.sleep(seconds)
        return "Too slow."
    return answer


def test_characters_listed():
    ids = [c["id"] for c in client.get("/characters").json()]
    assert len(ids) >= 15 and "jeff" in ids


def test_characters_are_complete():
    # Item names from the Unity item assets - a character's favourites must match ItemSO.itemName exactly
    unity_items = {m.group(1).strip() for path in UNITY_ITEMS.glob("*.asset")
                   if (m := re.search(r"itemName: (.+)", path.read_text()))}
    assert len(unity_items) >= 15
    wanted = set()
    for cid, c in server.CHARACTERS.items():
        assert "{item}" in c["greeting"], cid
        assert set(c["wants"]) <= unity_items, f"{cid} wants items the shop doesn't sell"
        low, high = c["budget_range"]
        assert 0 < low <= high, cid
        wanted |= set(c["wants"])
    assert wanted == unity_items  # every item is someone's favourite


def test_spawn_requests_item_the_shop_sells():
    data = spawn("jeff")
    assert data["wants"] == "Apple"
    assert "Apple" in data["say"]
    assert data["status"] == "shopping"
    assert data["offers_left"] == server.CHARACTERS["jeff"]["max_offers"]


def test_spawn_random_character():
    assert spawn("")["character_id"] in server.CHARACTERS


def test_spawn_asks_for_any_item_on_the_shelves():
    shop = [{"name": "Water Bottle", "price": 2, "stock": 3}, {"name": "Candy Bar", "price": 2, "stock": 3}]
    r = client.post("/customers/spawn", json={"character_id": "vinnie", "shop_items": shop}).json()
    assert r["wants"] in ("Water Bottle", "Candy Bar")
    assert r["wants"] in r["say"]


def test_spawn_unknown_character_404():
    assert client.post("/customers/spawn", json={"character_id": "nobody"}).status_code == 404


def test_offer_within_budget_sells():
    c = spawn("jeff", budget=5)
    r = offer(c, "Apple", 4).json()
    assert r["outcome"] == "accept" and r["sale"] is True
    assert r["sale_item"] == "Apple" and r["sale_price"] == 4
    assert r["say"] == server.FALLBACK_LINES["accept"]  # backup line until the reaction is fetched
    assert r["leaving"] is True and r["status"] == "bought"


def test_cheap_offer_makes_customer_happier():
    c = spawn("jeff", budget=5)
    assert offer(c, "Apple", 2).json()["satisfaction"] == c["satisfaction"] + 10


def test_slightly_over_budget_gets_counter_offer():
    c = spawn("rose", budget=4)
    r = offer(c, "Apple", 5).json()
    assert r["outcome"] == "counter" and r["counter_offer"] == 4
    assert r["sale"] is False and r["leaving"] is False
    assert offer(c, "Apple", 4).json()["sale"] is True  # accepting the counter works


def test_way_over_budget_rejected():
    c = spawn("rose", budget=4)
    r = offer(c, "Apple", 50).json()
    assert r["outcome"] == "reject" and r["sale"] is False
    assert r["satisfaction"] == c["satisfaction"] - 8


def test_wrong_item_refused():
    c = spawn("jeff", budget=5)
    r = offer(c, "Box", 1).json()
    assert r["outcome"] == "wrong_item" and r["sale"] is False


def test_customer_leaves_after_max_offers():
    c = spawn("jeff", budget=4)
    for _ in range(server.CHARACTERS["jeff"]["max_offers"]):
        r = offer(c, "Apple", 100).json()
    assert r["leaving"] is True and r["status"] == "left"
    assert offer(c, "Apple", 1).status_code == 409


def test_ollama_down_uses_fallback_line(monkeypatch):
    def boom(prompt):
        raise requests.ConnectionError()
    monkeypatch.setattr(server, "ask_llm", boom)
    c = spawn("rose", budget=4)
    r = reaction(c, offer(c, "Apple", 5).json()["offer_id"]).json()
    assert r["ai_online"] is False
    assert "$4" in r["say"]  # fallback counter line still states the counter price


def test_reaction_returns_llm_line():
    c = spawn("jeff", budget=5)
    r = offer(c, "Apple", 4).json()
    assert reaction(c, r["offer_id"]).json() == {"offer_id": 1, "say": "Fake reaction.", "ai_online": True}


def test_sale_does_not_wait_for_slow_llm(monkeypatch):
    monkeypatch.setattr(server, "ask_llm", slow_llm(5))
    c = spawn("jeff", budget=5)
    start = time.monotonic()
    r = offer(c, "Apple", 4).json()
    assert time.monotonic() - start < 1
    assert r["sale"] is True and r["status"] == "bought"


def test_slow_llm_gives_up_and_uses_fallback(monkeypatch):
    monkeypatch.setattr(server, "LLM_TIMEOUT", 0.2)
    monkeypatch.setattr(server, "ask_llm", slow_llm(1))
    c = spawn("rose", budget=4)
    offer_id = offer(c, "Apple", 5).json()["offer_id"]
    start = time.monotonic()
    r = reaction(c, offer_id).json()
    assert time.monotonic() - start < 0.8
    assert r["ai_online"] is False and "$4" in r["say"]


def test_old_reaction_replaced_by_newer_offer():
    c = spawn("rose", budget=4)
    first = offer(c, "Apple", 100).json()
    second = offer(c, "Apple", 100).json()
    assert reaction(c, first["offer_id"]).status_code == 410
    assert reaction(c, second["offer_id"]).status_code == 200


def test_ban_and_remove():
    c = spawn("vinnie")
    r = client.post(f"/customers/{c['session_id']}/ban").json()
    assert r["status"] == "banned" and r["satisfaction"] == 0 and r["say"]
    client.delete(f"/customers/{c['session_id']}")
    assert client.get(f"/customers/{c['session_id']}").status_code == 404


def test_same_character_cant_be_in_shop_twice():
    spawn("jeff")
    r = client.post("/customers/spawn", json={"character_id": "jeff", "shop_items": SHOP})
    assert r.status_code == 409


def test_random_spawn_skips_characters_in_shop():
    for cid in server.CHARACTERS:
        if cid != "rose":
            spawn(cid)
    assert spawn("")["character_id"] == "rose"
    assert client.post("/customers/spawn", json={"shop_items": SHOP}).status_code == 409  # everyone is in


def test_character_can_return_after_leaving():
    c = spawn("jeff")
    client.delete(f"/customers/{c['session_id']}")
    spawn("jeff")


def test_reset_clears_the_shop():
    spawn("jeff")
    client.post("/reset")
    spawn("jeff")


# ---------- Unity <-> server contract ----------
# Unity's JsonUtility and the server's pydantic models both silently ignore fields whose names don't match,
# so a typo on either side would just leave a value empty. These compare the JSON with the C# classes Unity uses.

UNITY_CLIENT = UNITY_ASSETS / "Scripts" / "AI" / "AIServerClient.cs"


# Public fields of a C# class in AIServerClient.cs, including ones inherited from a base class there
def cs_fields(class_name):
    source = UNITY_CLIENT.read_text()
    match = re.search(rf"class {class_name}\b(?:\s*:\s*(\w+))?\s*\{{(.*?)\n\}}", source, re.S)
    assert match, f"class {class_name} not found in {UNITY_CLIENT.name}"
    fields = set(re.findall(r"public [\w\[\]]+ (\w+);", match.group(2)))
    base = match.group(1)
    return fields | cs_fields(base) if base else fields


def test_contract_spawn_response():
    assert set(spawn("jeff")) == cs_fields("CustomerData")


def test_contract_offer_response():
    c = spawn("jeff", budget=5)
    assert set(offer(c, "Apple", 4).json()) == cs_fields("OfferResponseData")


def test_contract_reaction_response():
    c = spawn("jeff", budget=5)
    assert set(reaction(c, offer(c, "Apple", 4).json()["offer_id"]).json()) == cs_fields("ReactionData")


def test_contract_ban_and_state_responses():
    c = spawn("jeff")
    assert set(client.get(f"/customers/{c['session_id']}").json()) == cs_fields("CustomerData") - {"say"}
    assert set(client.post(f"/customers/{c['session_id']}/ban").json()) == cs_fields("CustomerData")


def test_contract_health_response(monkeypatch):
    def ollama_down(*args, **kwargs):
        raise requests.ConnectionError()
    monkeypatch.setattr(server.requests, "get", ollama_down)
    assert set(client.get("/health").json()) == cs_fields("HealthData")


def test_contract_request_bodies():
    assert cs_fields("SpawnRequestData") == set(server.SpawnRequest.model_fields)
    assert cs_fields("OfferRequestData") == set(server.OfferRequest.model_fields)
    assert cs_fields("ShopItemData") == set(server.ShopItem.model_fields)
