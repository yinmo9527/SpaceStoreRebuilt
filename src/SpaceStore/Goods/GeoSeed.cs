using UnityEngine;

namespace SpaceStore.Goods {
  /// <summary>
  /// 地质种子实体。被激活时在自身位置生成指定类型的间歇泉，然后销毁自己。
  /// </summary>
  public sealed class GeoSeed : KMonoBehaviour {
    public void Activate(Tag geyserTag) {
      var prefab = Assets.GetPrefab(geyserTag);
      if (prefab == null) return;

      var spawned = GameUtil.KInstantiate(prefab, Grid.SceneLayer.Building);
      spawned.SetActive(false);
      var position = transform.position;
      position.z -= 0.15f;
      spawned.transform.SetPosition(position);
      spawned.SetActive(true);

      gameObject.DeleteObject();
    }
  }
}
