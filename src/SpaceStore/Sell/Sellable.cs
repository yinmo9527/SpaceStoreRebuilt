using System.Collections.Generic;
using SpaceStore.Economy;
using UnityEngine;

namespace SpaceStore.Sell {
  /// <summary>
  /// 可回收物的基类。挂在 prefab 上，物品被造出来时自动登记；
  /// 右键菜单里出现“卖 出”按钮，刷子工具也认它。
  /// </summary>
  public class Sellable : KMonoBehaviour {
    private static readonly List<Sellable> Registry = new List<Sellable>();

    private static readonly EventSystem.IntraObjectHandler<Sellable> OnRefreshUserMenuDelegate =
      new EventSystem.IntraObjectHandler<Sellable>((component, data) =>
        component.OnRefreshUserMenu(data));

    protected float cachedValue;

    public virtual bool CanSell => true;

    /// <summary>每次询问都重算：一堆矿石的质量会随搬运变化，缓存会导致报价过期。</summary>
    public float Value {
      get {
        CountValue();
        return cachedValue;
      }
    }

    public static IEnumerable<Sellable> AtCell(int cell) {
      for (var i = Registry.Count - 1; i >= 0; i--) {
        var sellable = Registry[i];
        if (sellable == null) {
          Registry.RemoveAt(i);
          continue;
        }
        if (sellable.gameObject == null || !sellable.isActiveAndEnabled) continue;
        if (Grid.PosToCell(sellable.transform.position) == cell) yield return sellable;
      }
    }

    protected override void OnSpawn() {
      base.OnSpawn();
      if (!Registry.Contains(this)) Registry.Add(this);
      Subscribe((int) GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
    }

    protected override void OnCleanUp() {
      Registry.Remove(this);
      base.OnCleanUp();
    }

    protected virtual void OnRefreshUserMenu(object data) {
      if (!CanSell) return;
      var value = Value;
      if (value <= 0f) return;

      Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
        "action_move_to_storage",
        MyString.UI.SELL.TITLE,
        Sell,
        tooltipText: MyString.UI.SELL.TOOL_TIP
          .Replace("{coin}", value.ToString("0.00"))
          .Replace("{item}", gameObject.GetProperName())));
    }

    public virtual void Sell() {
      var wallet = CoinWallet.Get();
      var value = Value;
      if (wallet == null || value <= 0f) return;

      wallet.Earn(value);
      if (PopFXManager.Instance != null) {
        PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Resource,
          $"{MyString.UI.COIN_NAME} {value:0.00}", transform, Vector3.zero);
      }
      gameObject.DeleteObject();
    }

    public virtual void CountValue() {
    }
  }
}
