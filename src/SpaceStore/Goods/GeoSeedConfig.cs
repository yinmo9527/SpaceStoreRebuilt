using TUNING;
using UnityEngine;

namespace SpaceStore.Goods {
  /// <summary>
  /// 地质种子。原“太空商店扩展包”的核心商品，现在作为内置商品合并进本体：
  /// 从商店买来，放到地上，在详情面板里选一种间歇泉，点激活即原地生成。
  /// </summary>
  public sealed class GeoSeedConfig : IEntityConfig {
    public const string ID = "Store_GeoSeed";

    public static readonly Tag TAG = TagManager.Create(ID);

    public string[] GetDlcIds() {
      return DlcManager.AVAILABLE_ALL_VERSIONS;
    }

    public GameObject CreatePrefab() {
      var prefab = EntityTemplates.CreatePlacedEntity(
        ID,
        MyString.UI.GOODS.GEOSEED.NAME,
        MyString.UI.GOODS.GEOSEED.DESC,
        1f,
        decor: DECOR.BONUS.TIER1,
        anim: Assets.GetAnim("geo_seed_kanim"),
        initialAnim: "object",
        sceneLayer: Grid.SceneLayer.BuildingFront,
        width: 1,
        height: 1,
        element: SimHashes.Unobtanium,
        additionalTags: null,
        defaultTemperature: 255f);

      prefab.AddOrGet<GeoSeed>();
      prefab.AddOrGet<Pickupable>();
      prefab.AddOrGet<Movable>();
      return prefab;
    }

    public void OnPrefabInit(GameObject inst) {
    }

    public void OnSpawn(GameObject inst) {
    }
  }
}
