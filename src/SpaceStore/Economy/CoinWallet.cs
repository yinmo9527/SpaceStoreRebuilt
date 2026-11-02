using System;
using System.Reflection;
using System.Runtime.Serialization;
using KSerialization;
using PeterHan.PLib.Core;

namespace SpaceStore.Economy {
  /// <summary>
  /// 唯一的金币账本。挂在 SaveGame 上，随存档序列化。
  ///
  /// 旧版把金额同时存在 StoreData(SaveGame) 和 CoinSaver(打印舱) 两处，靠一个
  /// sync 标志在 OnSpawn 时互相覆盖——读档顺序一变就会丢钱或翻倍。
  /// 这里只保留一份，UI 和所有交易都读同一个 Balance。
  /// </summary>
  [SerializationConfig(MemberSerialization.OptIn)]
  public sealed class CoinWallet : KMonoBehaviour, ISaveLoadable {
    /// <summary>没有配置项时的兜底初始资金。</summary>
    public const float DefaultStartingCoins = 10000f;

    [Serialize] public float coins;

    /// <summary>是否已经发过初始资金 / 已经正常读档过，避免每次读档重复发放。</summary>
    [Serialize] public bool initialized;

    public static CoinWallet Instance { get; private set; }

    /// <summary>余额变化时触发，参数为新余额。</summary>
    public static event Action<float> Changed;

    public float Balance => coins;

    public static CoinWallet Get() {
      if (Instance != null) return Instance;
      var saveGame = SaveGame.Instance;
      if (saveGame == null) return null;
      Instance = saveGame.gameObject.GetComponent<CoinWallet>() ??
                 saveGame.gameObject.AddOrGet<CoinWallet>();
      return Instance;
    }

    protected override void OnSpawn() {
      base.OnSpawn();
      Instance = this;
      MigrateLegacyWallet();

      // 新档发一笔启动资金；老档（余额已有钱）不动，只把标记补上
      if (!initialized) {
        initialized = true;
        if (coins <= 0f) coins = StartingCoins;
      }

      Changed?.Invoke(coins);
    }

    /// <summary>初始资金来自模组选项，取不到时用兜底值。</summary>
    private static float StartingCoins {
      get {
        try {
          var options = PeterHan.PLib.Options.SingletonOptions<Options>.Instance;
          if (options != null && options.StartingCoins >= 0) return options.StartingCoins;
        } catch {
          // 选项还没初始化就当没配
        }
        return DefaultStartingCoins;
      }
    }

    protected override void OnCleanUp() {
      base.OnCleanUp();
      if (Instance == this) Instance = null;
    }

    public bool CanAfford(float amount) {
      return coins >= amount;
    }

    public bool TrySpend(float amount) {
      if (amount < 0f || coins < amount) return false;
      coins -= amount;
      Changed?.Invoke(coins);
      return true;
    }

    public void Earn(float amount) {
      if (amount <= 0f) return;
      coins += amount;
      Changed?.Invoke(coins);
    }

    /// <summary>
    /// 兼容旧存档：旧版把余额写在 SpaceStore.Store.StoreData / CoinSaver 上。
    /// 老存档里如果还带着这些组件，把数值抄过来，避免升级后余额归零。
    /// </summary>
    private void MigrateLegacyWallet() {
      if (coins > 0f) return;
      var total = 0f;
      foreach (var behaviour in GetComponents<KMonoBehaviour>()) {
        var type = behaviour.GetType();
        if (type.FullName != "SpaceStore.Store.StoreData" &&
            type.FullName != "SpaceStore.Store.CoinSaver") continue;
        var field = type.GetField("coin",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(float)) continue;
        total = Math.Max(total, (float) field.GetValue(behaviour));
      }

      if (total > 0f) {
        coins = total;
        PUtil.LogDebug($"Space Store: 已从旧存档迁移 {total:0.##} 太空币");
      }
    }
  }
}
