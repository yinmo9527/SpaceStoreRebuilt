using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace SpaceStore {
  /// <summary>
  /// 模组选项。只放纯展示参数，不参与经济计算——价格永远来自配置文件。
  /// </summary>
  [ConfigFile("SpaceStoreRebuilt.json", true, true)]
  [RestartRequired]
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Options {
    [Option("初始太空币 / Starting Coins", "新开存档时发放的启动资金，只对没领过的存档生效")]
    [Limit(0, 1000000)]
    [JsonProperty]
    public int StartingCoins { get; set; } = 10000;

    [Option("条目宽度 / Item Width", "单个商品卡片的宽度（像素）")]
    [Limit(110, 300)]
    [JsonProperty]
    public int ItemWidth { get; set; } = 168;

    [Option("列数 / Columns", "每行显示几个商品")]
    [Limit(1, 12)]
    [JsonProperty]
    public int Columns { get; set; } = 5;

    [Option("窗口宽度占比 / Width Ratio",
      "占游戏窗口宽度的比例。0.5 表示宽高各取一半，面积正好是 1/4")]
    [Limit(0.2, 1.0)]
    [JsonProperty]
    public float WindowWidthRatio { get; set; } = 0.5f;

    [Option("窗口高度占比 / Height Ratio", "占游戏窗口高度的比例，默认同为 0.5")]
    [Limit(0.2, 1.0)]
    [JsonProperty]
    public float WindowHeightRatio { get; set; } = 0.5f;

    [Option("窗口高度兜底 / Fallback Height",
      "当上面两个比例设为 0 时，改用这个固定高度（像素）")]
    [Limit(240, 1200)]
    [JsonProperty]
    public int DialogHeight { get; set; } = 460;

    [Option("每页数量 / Page Size", "每页显示的商品数，觉得卡就调小")]
    [Limit(6, 200)]
    [JsonProperty]
    public int PageSize { get; set; } = 30;
  }
}
