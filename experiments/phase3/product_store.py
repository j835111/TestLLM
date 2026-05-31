#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent
DATA_FILE = ROOT / "data" / "products.json"


@dataclass(frozen=True)
class ProductInput:
    name: str
    description: str | None
    price: Decimal
    stock: int


def iso_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def ensure_store() -> None:
    if DATA_FILE.exists():
        return

    DATA_FILE.parent.mkdir(parents=True, exist_ok=True)
    DATA_FILE.write_text(
        json.dumps({"nextId": 1, "products": []}, indent=2) + "\n",
        encoding="utf-8",
    )


def load_store() -> dict[str, Any]:
    ensure_store()
    return json.loads(DATA_FILE.read_text(encoding="utf-8"))


def save_store(store: dict[str, Any]) -> None:
    DATA_FILE.parent.mkdir(parents=True, exist_ok=True)
    DATA_FILE.write_text(json.dumps(store, indent=2) + "\n", encoding="utf-8")


def normalize_input(name: str, description: str | None, price_text: str, stock: int) -> ProductInput:
    clean_name = name.strip()
    clean_description = description.strip() if description is not None else None
    clean_description = clean_description or None

    errors: list[str] = []

    if not clean_name or len(clean_name) > 120:
        errors.append("name must be non-empty and at most 120 characters")

    if clean_description is not None and len(clean_description) > 1000:
        errors.append("description must be at most 1000 characters")

    try:
        price = Decimal(price_text)
    except InvalidOperation as exc:
        raise SystemExit("price must be a valid decimal number") from exc

    if price < 0 or price > Decimal("999999999"):
        errors.append("price must be between 0 and 999999999")

    if stock < 0:
        errors.append("stock must be 0 or greater")

    if errors:
        raise SystemExit("; ".join(errors))

    return ProductInput(
        name=clean_name,
        description=clean_description,
        price=price,
        stock=stock,
    )


def decimal_to_json(value: Decimal) -> int | float:
    integral = value.to_integral_value()
    return int(value) if value == integral else float(value)


def command_health(_: argparse.Namespace) -> None:
    store = load_store()
    print(
        json.dumps(
            {
                "ok": True,
                "dataFile": str(DATA_FILE),
                "productCount": len(store["products"]),
            },
            indent=2,
        )
    )


def command_list(_: argparse.Namespace) -> None:
    store = load_store()
    print(json.dumps(store["products"], indent=2))


def command_get(args: argparse.Namespace) -> None:
    store = load_store()
    product = next((item for item in store["products"] if item["id"] == args.id), None)
    if product is None:
        raise SystemExit(f"product {args.id} was not found")

    print(json.dumps(product, indent=2))


def command_create(args: argparse.Namespace) -> None:
    payload = normalize_input(args.name, args.description, args.price, args.stock)
    store = load_store()
    timestamp = iso_now()
    product = {
        "id": store["nextId"],
        "name": payload.name,
        "description": payload.description,
        "price": decimal_to_json(payload.price),
        "stock": payload.stock,
        "createdAt": timestamp,
        "updatedAt": timestamp,
    }
    store["products"].append(product)
    store["nextId"] += 1
    save_store(store)
    print(json.dumps(product, indent=2))


def command_update(args: argparse.Namespace) -> None:
    payload = normalize_input(args.name, args.description, args.price, args.stock)
    store = load_store()

    for product in store["products"]:
        if product["id"] != args.id:
            continue

        product["name"] = payload.name
        product["description"] = payload.description
        product["price"] = decimal_to_json(payload.price)
        product["stock"] = payload.stock
        product["updatedAt"] = iso_now()
        save_store(store)
        print(json.dumps(product, indent=2))
        return

    raise SystemExit(f"product {args.id} was not found")


def command_delete(args: argparse.Namespace) -> None:
    store = load_store()
    kept_products = [item for item in store["products"] if item["id"] != args.id]

    if len(kept_products) == len(store["products"]):
        raise SystemExit(f"product {args.id} was not found")

    store["products"] = kept_products
    save_store(store)
    print(json.dumps({"deleted": True, "id": args.id}, indent=2))


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="File-backed product store for Phase3.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    health_parser = subparsers.add_parser("health", help="Check that the product store is available.")
    health_parser.set_defaults(handler=command_health)

    list_parser = subparsers.add_parser("list", help="List products.")
    list_parser.set_defaults(handler=command_list)

    get_parser = subparsers.add_parser("get", help="Get one product by id.")
    get_parser.add_argument("id", type=int)
    get_parser.set_defaults(handler=command_get)

    create_parser = subparsers.add_parser("create", help="Create a product.")
    create_parser.add_argument("--name", required=True)
    create_parser.add_argument("--description")
    create_parser.add_argument("--price", required=True)
    create_parser.add_argument("--stock", required=True, type=int)
    create_parser.set_defaults(handler=command_create)

    update_parser = subparsers.add_parser("update", help="Update a product.")
    update_parser.add_argument("id", type=int)
    update_parser.add_argument("--name", required=True)
    update_parser.add_argument("--description")
    update_parser.add_argument("--price", required=True)
    update_parser.add_argument("--stock", required=True, type=int)
    update_parser.set_defaults(handler=command_update)

    delete_parser = subparsers.add_parser("delete", help="Delete a product.")
    delete_parser.add_argument("id", type=int)
    delete_parser.set_defaults(handler=command_delete)

    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    args.handler(args)


if __name__ == "__main__":
    main()
