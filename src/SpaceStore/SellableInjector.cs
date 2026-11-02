using PeterHan.PLib.Core;
using SpaceStore.Config;
using SpaceStore.Sell;
using UnityEngine;

namespace SpaceStore {
  /// <summary>
  /// 给已有 prefab 批量挂上“可回收”组件。
  ///
  /// 旧版靠给 CreateOreEntity / CreateAndRegisterSeedForPlant 打补丁逐个挂，
  /// 那两个方法签名一改（游戏每次更新都可能动）整个 mod 就跟着挂。
  /// 这里改成在存档对象生成时扫一遍 prefab 表：谁的 id 在回收价表里就给谁挂上，
  /// 不依赖任何具体工厂方法的签名。
  /// </summary>
  public static class SellableInjector {
    public static int Installed { get; private set; }
    public static int Geysers { get; private set; }

    public static void Install() {
      Installed = 0;
      Geysers = 0;

      try {
        var prefabs = Assets.GetPrefabsWithComponent<PrimaryElement>();
        if (prefabs != null) {
          foreach (var prefab in prefabs) {
            if (prefab == null) continue;
            var tag = prefab.PrefabID();
            if (ConfigLoader.SellPriceOf(tag) <= 0f) continue;
            if (prefab.GetComponent<ElementSellable>() != null) continue;
            prefab.AddOrGet<ElementSellable>();
            Installed++;
          }
        }
      } catch (System.Exception e) {
        PUtil.LogExcWarn(e);
      }

      try {
        var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
        if (geyserPrefabs != null) {
          foreach (var prefab in geyserPrefabs) {
            if (prefab == null || prefab.GetComponent<GeyserSellable>() != null) continue;
            prefab.AddOrGet<GeyserSellable>();
            Geysers++;
          }
        }
      } catch (System.Exception e) {
        PUtil.LogExcWarn(e);
      }

      PUtil.LogDebug($"Space Store: 已注入 {Installed} 个可回收 prefab、{Geysers} 个间歇泉");
    }
  }
}
