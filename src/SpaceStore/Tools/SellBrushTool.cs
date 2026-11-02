using System.Collections.Generic;
using SpaceStore.Sell;
using UnityEngine;

namespace SpaceStore.Tools {
  /// <summary>
  /// 刷子出售工具。按住左键拖过地面即可批量卖掉可拾取物。
  ///
  /// 与旧版的差别：不再维护一份全局按钮列表逐个比对 cell，
  /// 直接用 Grid.Objects 取该格上的 pickupable，省掉每格 N 次遍历。
  /// </summary>
  public sealed class SellBrushTool : BrushTool {
    public static SellBrushTool Instance;

    private readonly string soundPath = GlobalAssets.GetSound("SandboxTool_ClearFloor");

    public static void DestroyInstance() {
      Instance = null;
    }

    protected override void OnPrefabInit() {
      base.OnPrefabInit();
      Instance = this;
    }

    protected override string GetDragSound() {
      return string.Empty;
    }

    public void Activate() {
      PlayerController.Instance.ActivateTool(this);
    }

    protected override void OnActivateTool() {
      base.OnActivateTool();
      SandboxToolParameterMenu.instance.gameObject.SetActive(true);
      SandboxToolParameterMenu.instance.DisableParameters();
      SandboxToolParameterMenu.instance.brushRadiusSlider.row.SetActive(true);
      SandboxToolParameterMenu.instance.brushRadiusSlider.SetValue(1);
    }

    protected override void OnDeactivateTool(InterfaceTool new_tool) {
      base.OnDeactivateTool(new_tool);
      SandboxToolParameterMenu.instance.gameObject.SetActive(false);
    }

    public override void GetOverlayColorData(out HashSet<ToolMenu.CellColorData> colors) {
      colors = new HashSet<ToolMenu.CellColorData>();
      foreach (var cell in cellsInRadius)
        colors.Add(new ToolMenu.CellColorData(cell, radiusIndicatorColor));
    }

    public override void OnLeftClickDown(Vector3 cursorPos) {
      base.OnLeftClickDown(cursorPos);
      KFMOD.PlayUISound(GlobalAssets.GetSound("SandboxTool_Click"));
    }

    protected override void OnPaintCell(int cell, int distFromOrigin) {
      base.OnPaintCell(cell, distFromOrigin);

      var sold = false;
      foreach (var sellable in Sellable.AtCell(cell)) {
        if (sellable == null || !sellable.CanSell) continue;
        if (!sold) {
          KFMOD.PlayOneShot(soundPath, sellable.transform.GetPosition());
          sold = true;
        }
        sellable.Sell();
      }
    }
  }
}
