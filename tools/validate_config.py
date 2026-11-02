#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""校验生成的配置：字段完整性、重复 id、ID 是否在物品库里、是否存在买卖套利。"""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
CFG = os.path.abspath(os.path.join(HERE, "..", "src", "SpaceStore", "config"))


def load(name):
    with open(os.path.join(CFG, name), encoding="utf-8") as f:
        return json.load(f)


def main():
    store, sell = load("store_default.json"), load("sell_default.json")
    db = {x["id"] for x in json.load(open(os.path.join(HERE, "items_db.json"), encoding="utf-8"))}
    db.add("Store_GeoSeed")

    problems = []
    seen = set()
    for i, it in enumerate(store):
        if not isinstance(it.get("id"), str) or not it["id"]:
            problems.append(f"store[{i}] id 非法")
        if it.get("price", 0) <= 0 or it.get("quantity", 0) <= 0:
            problems.append(f"store[{i}] {it.get('id')} price/quantity 非正")
        if it["id"] in seen:
            problems.append(f"store 重复 id: {it['id']}")
        seen.add(it["id"])
        if it["id"] not in db:
            problems.append(f"store 未知 id: {it['id']}")

    sell_ids = set()
    for i, it in enumerate(sell):
        if it.get("price", 0) <= 0:
            problems.append(f"sell[{i}] {it.get('id')} 单价非正")
        if it["id"] not in db:
            problems.append(f"sell 未知 id: {it['id']}")
        sell_ids.add(it["id"])

    unit = {x["id"]: x["price"] for x in sell}
    arbitrage = []
    for it in store:
        resale = it["quantity"] * unit.get(it["id"], 0)
        if resale >= it["price"]:
            arbitrage.append((it["id"], it["price"], round(resale, 2)))

    print(f"商店 {len(store)} 条，回收 {len(sell)} 条，物品库 {len(db)} 条")
    print(f"商店里没有回收价的商品：{len([x for x in store if x['id'] not in sell_ids])} 条")
    if problems:
        print("发现问题：")
        for p in problems[:30]:
            print("  -", p)
        if len(problems) > 30:
            print(f"  ... 另有 {len(problems) - 30} 条")
    else:
        print("字段/重复/ID 校验全部通过")
    if arbitrage:
        print("可套利条目：", arbitrage[:10])
    else:
        print("套利校验通过")
    return 1 if problems or arbitrage else 0


if __name__ == "__main__":
    sys.exit(main())
