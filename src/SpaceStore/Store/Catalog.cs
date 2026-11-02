using System.Collections.Generic;
using System.Linq;
using PeterHan.PLib.Core;

namespace SpaceStore.Store {
  /// <summary>
  /// 商品目录。配置 → 实体只解析一次；UI 分页/筛选都在这份缓存上做。
  /// </summary>
  public static class Catalog {
    public static readonly List<StoreEntry> All = new List<StoreEntry>();
    public static readonly List<string> Unresolved = new List<string>();

    public static bool Dirty { get; private set; }

    public static void Rebuild() {
      All.Clear();
      Unresolved.Clear();

      foreach (var def in Config.ConfigLoader.StoreItems) {
        var entry = StoreEntry.TryCreate(def.id, def.price, def.quantity, out var error);
        if (entry == null) {
          Unresolved.Add($"{def.id} ({error})");
          continue;
        }
        All.Add(entry);
      }

      // 便宜的在前面，同一个价位按名字排，方便找
      All.Sort((a, b) => {
        var byPrice = a.Price.CompareTo(b.Price);
        return byPrice != 0 ? byPrice : string.CompareOrdinal(a.Name, b.Name);
      });

      Dirty = false;
      PUtil.LogDebug($"Space Store: 目录解析完成 {All.Count} 件，失败 {Unresolved.Count} 件");
      if (Unresolved.Count > 0)
        PUtil.LogWarning("Space Store: 以下条目无法解析 -> " + string.Join(", ", Unresolved));
    }

    public static IEnumerable<StoreEntry> Filter(StoreCategory? category) {
      return category == null ? All : All.Where(e => e.Category == category.Value);
    }

    public static void MarkDirty() {
      Dirty = true;
    }
  }
}
