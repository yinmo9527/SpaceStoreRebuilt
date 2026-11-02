using System.Collections.Generic;
using HarmonyLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using SpaceStore.Economy;
using SpaceStore.Goods;
using SpaceStore.Tools;
using UnityEngine;

namespace SpaceStore {
  /// <summary>
  /// 全部 Harmony 补丁集中在这里，每个都只做一件事。
  /// 旧的 ElementSellable / GeyserSellable 生成期补丁已经删掉，改由 SellableInjector 统一处理。
  /// </summary>
  public static class Patches {
    /// <summary>把两个工具挂到玩家控制器上。</summary>
    [HarmonyPatch(typeof(PlayerController), "OnPrefabInit")]
    public static class PlayerController_OnPrefabInit_Patch {
      public static void Postfix(PlayerController __instance) {
        var tools = new List<InterfaceTool>(__instance.tools);
        tools.Add(CreateTool<StoreTool>(StaticVars.ToolId, __instance));
        tools.Add(CreateTool<SellBrushTool>(StaticVars.SellToolId, __instance));
        __instance.tools = tools.ToArray();
      }

      private static T CreateTool<T>(string name, PlayerController parent) where T : InterfaceTool {
        var go = new GameObject(name);
        var tool = go.AddComponent<T>();
        go.transform.SetParent(parent.gameObject.transform);
        go.SetActive(true);
        go.SetActive(false);
        return tool;
      }
    }

    /// <summary>在右下角工具栏加入“太空商店”和“出售物品”。</summary>
    [HarmonyPatch(typeof(ToolMenu), "CreateBasicTools")]
    public static class ToolMenu_CreateBasicTools_Patch {
      private static bool iconRegistered;

      public static void Prefix(ToolMenu __instance) {
        if (!iconRegistered) {
          Assets.Sprites[new HashedString(StaticVars.ToolIconName)] = StaticVars.ToolIcon;
          iconRegistered = true;
        }

        __instance.basicTools.Add(ToolMenu.CreateToolCollection(
          MyString.UI.TOOL.TITLE, StaticVars.ToolIconName, StaticVars.OpenStoreAction.GetKAction(),
          StaticVars.ToolId, MyString.UI.TOOL.TOOL_TIP, false));

        __instance.basicTools.Add(ToolMenu.CreateToolCollection(
          MyString.UI.TOOL_SELL.TITLE, "icon_action_sweep", StaticVars.OpenStoreAction.GetKAction(),
          StaticVars.SellToolId, MyString.UI.TOOL_SELL.TOOL_TIP, false));
      }
    }

    /// <summary>存档对象上挂钱包，并在 prefab 就绪后注入回收组件。</summary>
    [HarmonyPatch(typeof(SaveGame), "OnPrefabInit")]
    public static class SaveGame_OnPrefabInit_Patch {
      public static void Postfix(SaveGame __instance) {
        __instance.gameObject.AddOrGet<CoinWallet>();
        SellableInjector.Install();
      }
    }

    /// <summary>
    /// 读档前再补一次注入。SaveGame.OnPrefabInit 有时早于 Assets 就绪，
    /// 这一步保证地图上的物品都能带出售组件。
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Load))]
    public static class Game_Load_Patch {
      public static void Prefix() {
        if (SellableInjector.Installed == 0) SellableInjector.Install();
      }
    }

    /// <summary>详情面板加入地质种子的间歇泉选择侧屏。</summary>
    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class DetailsScreen_OnPrefabInit_Patch {
      public static void Postfix(List<DetailsScreen.SideScreenRef> ___sideScreens,
        DetailsScreen.SidescreenTab[] ___sidescreenTabs) {
        try {
          GeoSeedSideScreen.AddSideScreen(___sideScreens, ___sidescreenTabs);
        } catch (System.Exception e) {
          PUtil.LogExcWarn(e);
        }
      }
    }
  }
}
