using System.Collections.Generic;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using SpaceStore.Config;
using UnityEngine;

namespace SpaceStore {
  /// <summary>
  /// 模组入口。只做装配：初始化 PLib、注册选项与本地化、注册工具热键、加载配置。
  /// 具体逻辑分散在 Config / Economy / Catalog / UI / Tools / Sell / Goods 各层，
  /// 互不直接依赖，方便单独替换。
  /// </summary>
  public sealed class Mod : UserMod2 {
    public override void OnLoad(Harmony harmony) {
      base.OnLoad(harmony);

      PUtil.InitLibrary();
      new POptions().RegisterOptions(this, typeof(Options));
      LocString.CreateLocStringKeys(typeof(MyString));
      new PLocalization().Register();

      StaticVars.CoinIcon = PUIUtils.LoadSprite("SpaceStore.images.coin.png");
      StaticVars.ToolIcon = PUIUtils.LoadSprite("SpaceStore.images.icon_space_store.png");

      StaticVars.OpenStoreAction = new PActionManager().CreateAction(
        "OpenSpaceStore", MyString.UI.TOOL.TITLE, new PKeyBinding());
    }

    public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods) {
      base.OnAllModsLoaded(harmony, mods);
      // 只读配置；prefab 还没就绪，目录留到第一次开商店时再解析
      ConfigLoader.Reload();
    }
  }

  /// <summary>跨层共享的只读状态，不放业务逻辑。</summary>
  public static class StaticVars {
    public const string ToolId = "SpaceStoreTool";
    public const string SellToolId = "SpaceStoreSellTool";
    public const string ToolIconName = "icon_space_store";

    public static Sprite CoinIcon;
    public static Sprite ToolIcon;
    public static PAction OpenStoreAction;
  }
}
