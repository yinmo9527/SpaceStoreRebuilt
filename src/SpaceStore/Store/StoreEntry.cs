using System;
using UnityEngine;
using PeterHan.PLib.Core;

namespace SpaceStore.Store {
  /// <summary>商品分类，用于筛选页签。</summary>
  public enum StoreCategory {
    Material,
    Food,
    Seed,
    Egg,
    Critter,
    Other
  }

  /// <summary>
  /// 计价单位。不同东西的“数量”含义完全不同：
  /// 材料按质量买（吨），种子按颗，蛋按枚，小动物按只，食物按份。
  /// 所有换算都收在这一层，UI 只负责显示和收集数字。
  /// </summary>
  public enum StoreUnit {
    Tonne,
    Portion,
    Seed,
    Egg,
    Critter,
    Piece
  }

  /// <summary>
  /// 一条可交易商品：把配置里的 id 解析成 prefab、图标、显示名与补给包。
  /// 解析失败的商品不会进入列表，而是记进 Catalog.Unresolved 统一上报。
  /// </summary>
  public sealed class StoreEntry {
    /// <summary>材料默认一次买 50 吨。</summary>
    public const float DefaultTonnes = 50f;

    private const float KgPerTonne = 1000f;

    public Tag Tag;
    public string Id;
    public string Name;
    public int Price;
    public int Quantity;
    public float SellPrice;
    public StoreCategory Category;
    public StoreUnit Unit;
    public Tuple<Sprite, Color> Sprite;
    public CarePackageInfo Package;

    /// <summary>配置里“一份”对应的游戏内数量：材料是 kg，其它是件数。</summary>
    public float PackAmount => Mathf.Max(1f, Quantity);

    /// <summary>每 1 个游戏单位的价格（材料 = 每 kg）。</summary>
    public float PricePerGameUnit => Price / PackAmount;

    /// <summary>输入框默认值：材料 50（吨），其它按配置份数。</summary>
    public string DefaultAmountText => Unit == StoreUnit.Tonne
      ? DefaultTonnes.ToString("0.##")
      : Mathf.Max(1, Quantity).ToString();

    public string UnitLabel {
      get {
        switch (Unit) {
          case StoreUnit.Tonne: return MyString.UI.STORE.UNIT_TONNE.ToString();
          case StoreUnit.Portion: return MyString.UI.STORE.UNIT_PORTION.ToString();
          case StoreUnit.Seed: return MyString.UI.STORE.UNIT_SEED.ToString();
          case StoreUnit.Egg: return MyString.UI.STORE.UNIT_EGG.ToString();
          case StoreUnit.Critter: return MyString.UI.STORE.UNIT_CRITTER.ToString();
          default: return MyString.UI.STORE.UNIT_PIECE.ToString();
        }
      }
    }

    /// <summary>界面上展示的单价：材料按吨，其它按件。</summary>
    public float DisplayUnitPrice => Unit == StoreUnit.Tonne
      ? PricePerGameUnit * KgPerTonne
      : PricePerGameUnit;

    /// <summary>把输入框里的数字换算成游戏内的数量（材料 吨 → kg）。</summary>
    public float ToGameAmount(float input) {
      return Unit == StoreUnit.Tonne ? input * KgPerTonne : input;
    }

    /// <summary>按输入数量算总价，向上取整，最低 1 币。</summary>
    public int CostFor(float input) {
      if (input <= 0f) return 0;
      return Math.Max(1, (int) Math.Ceiling(PricePerGameUnit * ToGameAmount(input)));
    }

    public string Describe() {
      var priceText = MyString.UI.STORE.UNIT_PRICE
        .Replace("{price}", DisplayUnitPrice.ToString("0.##"))
        .Replace("{unit}", UnitLabel);
      return Name + "\n" + priceText;
    }

    public static StoreUnit UnitFor(StoreCategory category) {
      switch (category) {
        case StoreCategory.Material: return StoreUnit.Tonne;
        case StoreCategory.Food: return StoreUnit.Portion;
        case StoreCategory.Seed: return StoreUnit.Seed;
        case StoreCategory.Egg: return StoreUnit.Egg;
        case StoreCategory.Critter: return StoreUnit.Critter;
        default: return StoreUnit.Piece;
      }
    }

    public static StoreCategory Classify(string id, Tag tag, GameObject prefab) {
      // 地质种子是本 mod 自建商品，名字也以 Seed 结尾，必须先排除
      if (id.StartsWith("Store_", StringComparison.Ordinal)) return StoreCategory.Other;
      if (ElementLoader.GetElement(tag) != null) return StoreCategory.Material;
      if (EdiblesManager.GetFoodInfo(id) != null) return StoreCategory.Food;
      if (id.EndsWith("Seed", StringComparison.OrdinalIgnoreCase)) return StoreCategory.Seed;
      if (id.EndsWith("Egg", StringComparison.OrdinalIgnoreCase)) return StoreCategory.Egg;
      if (HasCreatureBrain(prefab)) return StoreCategory.Critter;
      return StoreCategory.Other;
    }

    /// <summary>
    /// 用类型名判断是不是小动物，而不是直接引用 CreatureBrain——
    /// 这样即使以后类被挪了命名空间，也不会编译不过。
    /// </summary>
    private static bool HasCreatureBrain(GameObject prefab) {
      if (prefab == null) return false;
      foreach (var component in prefab.GetComponents<Component>())
        if (component != null && component.GetType().Name == "CreatureBrain") return true;
      return false;
    }

    public static StoreEntry TryCreate(string id, int price, int quantity, out string error) {
      error = null;
      try {
        var tag = new Tag(id);
        var prefab = Assets.GetPrefab(tag);
        if (prefab == null) {
          error = "prefab 不存在";
          return null;
        }

        var category = Classify(id, tag, prefab);
        var entry = new StoreEntry {
          Tag = tag,
          Id = id,
          Price = price,
          Quantity = quantity,
          Name = prefab.GetProperName(),
          Sprite = Def.GetUISprite(tag),
          Category = category,
          Unit = UnitFor(category),
          SellPrice = Config.ConfigLoader.SellPriceOf(tag),
          Package = new CarePackageInfo(tag.ToString(), quantity, null)
        };
        return entry;
      } catch (Exception e) {
        error = e.Message;
        PUtil.LogExcWarn(e);
        return null;
      }
    }
  }
}
