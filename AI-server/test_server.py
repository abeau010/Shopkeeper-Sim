# Tests for the AI server - the LLM is faked so these run without Ollama
# Run with: pytest -v
import pytest
import requests
from fastapi.testclient import TestClient

import server

client = TestClient(server.app)
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


def test_characters_listed():
    ids = [c["id"] for c in client.get("/characters").json()]
    assert len(ids) >= 9 and "jeff" in ids


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
    assert r["say"] == "Fake reaction."
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
    r = offer(c, "Apple", 5).json()
    assert r["ai_online"] is False
    assert "$4" in r["say"]  # fallback counter line still states the counter price


def test_ban_and_remove():
    c = spawn("vinnie")
    r = client.post(f"/customers/{c['session_id']}/ban").json()
    assert r["status"] == "banned" and r["satisfaction"] == 0 and r["say"]
    client.delete(f"/customers/{c['session_id']}")
    assert client.get(f"/customers/{c['session_id']}").status_code == 404
