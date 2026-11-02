namespace SpaceStore.Sell {
  /// <summary>
  /// 固体/种子等按质量计价的物品。单价来自 sell_*.json 的 id → 每单位价格表，
  /// 表里没有的 id 不出售按钮（CanSell=false），避免“点了没反应”。
  /// </summary>
  public sealed class ElementSellable : Sellable {
    [MyCmpGet] private readonly PrimaryElement primaryElement;

    public override bool CanSell => primaryElement != null && Config.ConfigLoader.SellPriceOf(PrefabID()) > 0f;

    public override void CountValue() {
      if (primaryElement == null) {
        cachedValue = 0f;
        return;
      }
      cachedValue = primaryElement.Units * Config.ConfigLoader.SellPriceOf(PrefabID());
    }

    private Tag PrefabID() {
      return gameObject.PrefabID();
    }
  }
}
