#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
根据 items_db.json 生成太空商店的默认配置：
  - store_default.json  商店售卖清单 {id, price, quantity}
  - sell_default.json   回收单价表   {id, price}

价格模型（可按需调整下面的常量）：
  材料按“单位质量单价 × 整包质量 × 溢价倍数”定价；
  溢价倍数保证买入价远高于卖出回收价，杜绝“买了再卖”套利。
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))

# 回收价倍率：卖东西的单价整体放大。10 = 全部回收价 ×10
SELL_MULTIPLIER = 10.0
# 回收价最高只能到“买入单价”的这个比例，防止买了再卖套利（0.5 = 一半）
SELL_CAP_RATIO = 0.5
OUT = os.path.abspath(os.path.join(HERE, "..", "src", "SpaceStore", "config"))

# 等级 -> (单位回收价/千克, 整包质量kg, 买入溢价, 商店显示名)
TIERS = {
    "T0": (0.00020, 5000, 40, "废料"),
    "T1": (0.00080, 3000, 50, "常见"),
    "T2": (0.00300, 1500, 80, "加工"),
    "T3": (0.01200, 500, 120, "稀有"),
    "T4": (0.05000, 100, 200, "尖端"),
}

# 各非材料类别的 (售卖数量, 回收单价, 溢价)
CATEGORY_RULES = {
    "food":       (10, 2.0, 1),
    "seed":       (3, 25.0, 1),
    "egg":        (2, 40.0, 1),
    "critter":    (1, 120.0, 1),
    "industrial": (20, 1.5, 1),
    "medicine":   (3, 15.0, 1),
    "spice":      (5, 10.0, 1),
}

# ---- 材料分级：显式覆盖优先，其余按规则推断 ----
T4 = {
    "Fullerene", "Isoresin", "Niobium", "Tungsten", "TungstenDiselenide", "SuperCoolant",
    "ViscoGel", "TempConductorSolid", "SuperInsulator", "EnrichedUranium", "DepletedUranium",
    "NuclearWaste", "SolidNuclearWaste", "Radium", "Yellowcake", "Aerogel", "CarbonFibre",
    "Iridium", "Graphite", "Diamond", "NiobiumGas", "Snow",  # Snow 在下方会被降级覆盖
    "TungstenGas", "MoltenTungsten", "MoltenNiobium", "SolidViscoGel", "SolidSuperCoolant",
}
T3 = {
    "Gold", "GoldAmalgam", "Lead", "Cobalt", "Cobaltite", "CobaltGas", "Nickel", "NickelOre",
    "NickelGas", "Zinc", "ZincOre", "ZincGas", "Electrum", "Galena", "Cinnabar", "Pearl",
    "Amber", "Fossil", "Lime", "Ceramic", "Glass", "Steel", "Polypropylene", "HardPolypropylene",
    "RefinedCarbon", "Salt", "BleachStone", "OxyRock", "Rust", "Resin", "NaturalResin",
    "NaturalSolidResin", "SolidResin", "Latex", "Ink", "FrozenInk", "Tallow", "PhytoOil",
    "FrozenPhytoOil", "Sucrose", "FishMilk", "FishMilkIce", "Milk", "MilkFat", "MilkIce",
    "WoodLog", "PalmWood", "FabricatedWood", "Mercury", "SolidMercury", "MercuryGas",
    "Corallium", "Pearl", "Katairite", "Obsidian", "Granite", "Marble", "MoltenSteel",
    "MoltenGlass", "MoltenGold", "MoltenCopper", "MoltenIron", "MoltenAluminum", "MoltenLead",
    "MoltenCobalt", "MoltenNickel", "MoltenZinc", "MoltenIridium", "MoltenSalt", "MoltenCarbon",
    "MoltenSucrose", "MoltenUranium", "Magma",
}
T0 = {
    "Sand", "Sandstone", "Dirt", "Clay", "Regolith", "CrushedRock", "CrushedIce", "Ice",
    "DirtyIce", "BrineIce", "Snow", "StableSnow", "Mud", "ToxicMud", "Peat", "SlimeMold",
    "ToxicSand", "SandCement", "CementMix", "Siltstone", "Shale", "Coquina", "Basalt",
    "MaficRock", "SedimentaryRock", "IgneousRock", "Brick", "Cement", "Composition", "Slabs",
    "Gunk", "Mucus", "SolidMucus", "Pondscum", "Brine", "MurkyBrine", "MurkyBrineIce",
    "DirtyWater", "SaltWater", "Water", "Carbon", "CarbonDioxide", "Oxygen", "ContaminatedOxygen",
    "Hydrogen", "Chlorine", "ChlorineGas", "Steam", "SourGas", "RockGas", "Sulfur",
    "SulfurGas", "Phosphorus", "PhosphorusGas", "Phosphorite", "PhosphateNodules", "Fertilizer",
    "Algae", "Fallout", "Corium", "SolidCarbonDioxide", "SolidChlorine", "SolidCrudeOil",
    "SolidEthanol", "SolidHydrogen", "SolidMethane", "SolidNaphtha", "SolidOxygen",
    "SolidPetroleum", "SolidPropane", "LiquidCarbonDioxide", "LiquidGunk", "LiquidHelium",
    "LiquidHydrogen", "LiquidMethane", "LiquidOxygen", "LiquidPhosphorus", "LiquidPropane",
    "CrudeOil", "Petroleum", "Naphtha", "Ethanol", "EthanolGas", "Bitumen", "Water",
    "CarbonGas", "Hydrogen", "Methane", "Propane", "Helium", "Oxygen", "Vacuum", "Void",
}
SKIP = {"Creature", "Vacuum", "Void", "Composition", "Slabs", "Unobtanium", "TestElement"}

# 合并扩展包后新增的内置商品（不在原版物品表里，由本 mod 自己注册 prefab）
MANUAL_STORE = [
    {"id": "Store_GeoSeed", "price": 600, "quantity": 1},
]
MANUAL_SELL = [
    {"id": "Store_GeoSeed", "price": 120.0},
]


def material_tier(mid):
    if mid in T4:
        return "T4"
    if mid in T3:
        return "T3"
    if mid in T0:
        return "T0"
    if mid.endswith("Gas") or mid in ("Hydrogen", "Oxygen", "Steam", "Methane", "Propane",
                                      "Helium", "Chlorine", "CarbonDioxide"):
        return "T0"
    if mid.startswith(("Liquid", "Molten", "Solid")):
        return "T2"
    return "T1"


def main():
    db = json.load(open(os.path.join(HERE, "items_db.json"), encoding="utf-8"))
    store, sell, report = [], [], {"by_cat": {}, "arbitrage": [], "skipped": []}
    for it in db:
        mid, cat = it["id"], it["cat"]
        if mid in SKIP:
            report["skipped"].append(mid)
            continue
        if cat == "material":
            tier = material_tier(mid)
            unit, qty, spread, _ = TIERS[tier]
            price = max(20, int(round(qty * unit * spread)))
            sell_price = min(round(unit * SELL_MULTIPLIER, 6), round(price / qty * SELL_CAP_RATIO, 6))
        else:
            if cat not in CATEGORY_RULES:
                continue
            qty, unit, _ = CATEGORY_RULES[cat]
            price = max(15, int(round(qty * unit * 3)))
            sell_price = min(round(unit * SELL_MULTIPLIER, 4), round(price / qty * SELL_CAP_RATIO, 4))
        raw_sell = (unit * SELL_MULTIPLIER) if cat == "material" else (unit * SELL_MULTIPLIER)
        if sell_price < round(raw_sell, 6):
            report.setdefault("clamped", []).append((mid, round(raw_sell, 4), sell_price))
        store.append({"id": mid, "price": price, "quantity": qty})
        sell.append({"id": mid, "price": sell_price})
        report["by_cat"].setdefault(cat, 0)
        report["by_cat"][cat] += 1
        # 套利检查：买一包再全卖，必须亏损
        resale = qty * sell_price
        if resale >= price:
            report["arbitrage"].append((mid, price, round(resale, 2)))

    store.sort(key=lambda x: x["id"])
    sell.sort(key=lambda x: x["id"])

    # 本 mod 自建的商品（地质种子等）追加在最后
    for extra in MANUAL_STORE:
        if not any(x["id"] == extra["id"] for x in store):
            store.append(extra)
    for extra in MANUAL_SELL:
        if not any(x["id"] == extra["id"] for x in sell):
            sell.append(extra)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "store_default.json"), "w", encoding="utf-8") as f:
        json.dump(store, f, ensure_ascii=False, indent=2)
    with open(os.path.join(OUT, "sell_default.json"), "w", encoding="utf-8") as f:
        json.dump(sell, f, ensure_ascii=False, indent=2)

    print("商店条目:", len(store), "回收条目:", len(sell))
    print("分类:", json.dumps(report["by_cat"], ensure_ascii=False))
    print("跳过:", ", ".join(report["skipped"]))
    clamped = report.pop("clamped", [])
    if clamped:
        print(f"回收价被压到不套利上限的条目: {len(clamped)} 个（材料之外的基本都在这里）")
        print("   例:", ", ".join(f"{i}({a}->{b})" for i, a, b in clamped[:6]))
    if report["arbitrage"]:
        print("!! 可套利条目:", report["arbitrage"])
    else:
        print("套利检查通过：所有商品买入价均高于整包回收价")


if __name__ == "__main__":
    main()
