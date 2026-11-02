using System.Linq;

namespace SpaceStore.Store {
  /// <summary>
  /// 定位“当前星球的打印舱”。补给包只能掉在打印舱位置，所以购买前必须先找到它。
  /// 原版把这段逻辑塞在 UI 里，这里抽出来，界面和逻辑都不再各自实现一遍。
  /// </summary>
  public static class TelepadLocator {
    public static Telepad Active() {
      if (Components.Telepads == null || Components.Telepads.Count == 0) return null;

      // 本体只有一个打印舱；火箭/多星球场景下优先取当前激活星球上的那个
      if (DlcManager.GetActiveDLCIds().Contains("EXPANSION1_ID")) {
        var local = Components.Telepads.GetWorldItems(ClusterManager.Instance.activeWorldId);
        if (local.Count > 0) return local[0];
      }

      return Components.Telepads[0];
    }
  }
}
