namespace SpaceStore.Sell {
  /// <summary>
  /// 间歇泉按“口”卖，固定 100 币（旧版行为）。卖掉会直接移除该间歇泉。
  /// 旧版还会顺手把泉下方的中子物质转换成沙子，那等于变相送资源，这里不再这么做。
  /// </summary>
  public sealed class GeyserSellable : Sellable {
    public const float GeyserPrice = 100f;

    public override void CountValue() {
      cachedValue = GeyserPrice;
    }
  }
}
