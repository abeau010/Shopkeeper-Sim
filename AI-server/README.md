# Shopkeeper Sim – AI Server

Python server that runs the AI customers. Unity talks to it over HTTP on `http://127.0.0.1:8000`.
The LLM runs locally through [Ollama](https://ollama.com), so no API key or internet connection is needed.

## How a sale works

1. A customer walks in and asks for an item (for example "I need an Apple").
2. The player grabs that item from a shelf, walks up to the customer, and presses **E**.
3. A price picker opens. The player picks a price and offers it.
4. The **server decides** the outcome from the customer's secret budget:
   - **accept**: the price is within budget. The item sells and the customer leaves.
   - **counter**: the price is a bit over budget. The customer names the price they'd pay.
   - **reject**: the price is way over budget.
   - **wrong_item**: the player brought the wrong item.
5. The **LLM writes the customer's reaction** in character, based on that outcome.

Each customer only hears a limited number of offers (`max_offers` in `characters.py`) before leaving.

```
Unity (C#)                               Python (this folder)                    Ollama
CustomerSpawner ── POST /customers/spawn ──▶ picks character, item, budget
PriceSelectUI ──── POST /customers/{id}/offer ─▶ judge_offer() decides outcome
                                              reaction_prompt() ──────────────▶ llama3.2:3b
               ◀── say / outcome / sale ──────────────────────────── reaction ◀┘
```

## Setup (one time)

```bash
brew install ollama
ollama pull llama3.2:3b

cd AI-server
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Run

```bash
ollama serve            # skip this if the Ollama app is already running
cd AI-server && source .venv/bin/activate
python server.py        # or: uvicorn server:app --port 8000 --reload
```

Open http://127.0.0.1:8000/docs to try every endpoint in the browser.

Environment overrides: `OLLAMA_MODEL` (for example `qwen2.5:7b-instruct`), `OLLAMA_URL`, `PORT`.

## Test

```bash
pytest -v                    # unit tests, LLM is faked (Ollama not needed)
python sell_cli.py vinnie    # sell to a real customer in the terminal (server must be running)
```

In `sell_cli.py`, type offers as `Apple 5`. Other commands: `/ban` and `/quit`.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/health` | Server status, and whether Ollama and the model are available |
| GET | `/characters` | List of character templates |
| POST | `/customers/spawn` | `{character_id?, shop_items?}` → new customer with their request (`say`), wanted item, colour |
| POST | `/customers/{id}/offer` | `{item, price, shop_items?}` → reaction, outcome, counter-offer, sale result |
| POST | `/customers/{id}/ban` | Kick the customer out |
| GET | `/customers/{id}` | Current state (debugging) |
| DELETE | `/customers/{id}` | Forget a customer (Unity calls this when one despawns) |
| POST | `/reset` | Clear all customers |

## Adding a character

Add an entry to `characters.py`. Budgets are multipliers of the wanted item's price, so characters keep working when shop prices change. `wants` lists preferred item names, which must match `ItemSO.itemName` in Unity.
