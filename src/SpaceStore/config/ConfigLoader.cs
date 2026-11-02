using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KMod;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core;

namespace SpaceStore.Config {
  /// <summary>
  /// 配置读取层。相比旧版的三处改进：
  ///  1) 内置默认配置 + 用户配置做“覆盖”而不是“追加”，同名 id 不会再刷出两条；
  ///  2) 解析失败会记进 Errors 并汇总上报，不再静默 catch；
  ///  3) Reload() 真正重读文件，UI 的“重载配置”按钮终于有意义。
  /// </summary>
  public static class ConfigLoader {
    public const string StorePrefix = "store_";
    public const string SellPrefix = "sell_";

    public static readonly List<StoreItemDef> StoreItems = new List<StoreItemDef>();
    public static readonly Dictionary<Tag, float> SellPrices = new Dictionary<Tag, float>();
    public static readonly List<string> Errors = new List<string>();

    public static int StoreFileCount { get; private set; }
    public static int SellFileCount { get; private set; }

    /// <summary>mods/spacestore，与旧版共用同一个目录，切过来不用搬配置。</summary>
    public static string ConfigDir =>
      Path.GetFullPath(Path.Combine(Manager.GetDirectory(), "spacestore"));

    public static void Reload() {
      StoreItems.Clear();
      SellPrices.Clear();
      Errors.Clear();
      StoreFileCount = 0;
      SellFileCount = 0;

      var storeMap = new Dictionary<string, StoreItemDef>(StringComparer.OrdinalIgnoreCase);
      var sellMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

      LoadStoreJson(ReadEmbedded("SpaceStore.config.store_default.json"), storeMap, "builtin");
      LoadSellJson(ReadEmbedded("SpaceStore.config.sell_default.json"), sellMap, "builtin");

      try {
        if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
        foreach (var path in Directory.GetFiles(ConfigDir, "*.json").OrderBy(p => p)) {
          var name = Path.GetFileName(path);
          if (name.StartsWith(StorePrefix, StringComparison.OrdinalIgnoreCase)) {
            LoadStoreJson(SafeRead(path), storeMap, name);
            StoreFileCount++;
          } else if (name.StartsWith(SellPrefix, StringComparison.OrdinalIgnoreCase)) {
            LoadSellJson(SafeRead(path), sellMap, name);
            SellFileCount++;
          }
        }
      } catch (Exception e) {
        Errors.Add($"读取配置目录失败: {e.Message}");
        PUtil.LogExcWarn(e);
      }

      StoreItems.AddRange(storeMap.Values);
      foreach (var kv in sellMap) SellPrices[new Tag(kv.Key)] = kv.Value;

      PUtil.LogDebug($"Space Store: {StoreItems.Count} 件商品 / {SellPrices.Count} 条回收价 " +
                     $"(商店文件 {StoreFileCount} 个, 回收文件 {SellFileCount} 个, 错误 {Errors.Count} 条)");
    }

    public static float SellPriceOf(Tag tag) {
      return SellPrices.TryGetValue(tag, out var p) ? p : 0f;
    }

    /// <summary>把一条卖出记录写回 sell_ 文件，供“导出当前回收价”之类的工具使用。</summary>
    public static string DescribeSources() {
      return $"{StoreFileCount} store / {SellFileCount} sell";
    }

    private static string SafeRead(string path) {
      try {
        return File.ReadAllText(path);
      } catch (Exception e) {
        Errors.Add($"{Path.GetFileName(path)}: {e.Message}");
        PUtil.LogExcWarn(e);
        return null;
      }
    }

    private static string ReadEmbedded(string resource) {
      try {
        var asm = Assembly.GetExecutingAssembly();
        using (var stream = asm.GetManifestResourceStream(resource)) {
          if (stream == null) {
            PUtil.LogError($"内置资源缺失: {resource}");
            return null;
          }
          using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }
      } catch (Exception e) {
        PUtil.LogExcWarn(e);
        return null;
      }
    }

    private static void LoadStoreJson(string json, IDictionary<string, StoreItemDef> sink, string source) {
      if (string.IsNullOrWhiteSpace(json)) return;
      JArray array;
      try {
        array = JArray.Parse(json);
      } catch (Exception e) {
        Errors.Add($"{source}: JSON 格式错误 ({e.Message})");
        return;
      }

      var index = 0;
      foreach (var token in array) {
        index++;
        try {
          if (!(token is JObject obj)) continue;
          var id = obj.Value<string>("id");
          var price = obj.Value<int?>("price") ?? 0;
          var quantity = obj.Value<int?>("quantity") ?? 0;
          if (string.IsNullOrWhiteSpace(id)) {
            Errors.Add($"{source}#{index}: 缺少 id");
            continue;
          }
          if (price <= 0 || quantity <= 0) {
            Errors.Add($"{source}#{index} ({id}): price/quantity 必须大于 0");
            continue;
          }
          sink[id] = new StoreItemDef(id, price, quantity);
        } catch (Exception e) {
          Errors.Add($"{source}#{index}: {e.Message}");
        }
      }
    }

    private static void LoadSellJson(string json, IDictionary<string, float> sink, string source) {
      if (string.IsNullOrWhiteSpace(json)) return;
      JArray array;
      try {
        array = JArray.Parse(json);
      } catch (Exception e) {
        Errors.Add($"{source}: JSON 格式错误 ({e.Message})");
        return;
      }

      var index = 0;
      foreach (var token in array) {
        index++;
        try {
          if (!(token is JObject obj)) continue;
          var id = obj.Value<string>("id");
          var price = obj.Value<float?>("price") ?? 0f;
          if (string.IsNullOrWhiteSpace(id) || price <= 0f) {
            Errors.Add($"{source}#{index}: id 为空或单价不大于 0");
            continue;
          }
          sink[id] = price;
        } catch (Exception e) {
          Errors.Add($"{source}#{index}: {e.Message}");
        }
      }
    }
  }
}
