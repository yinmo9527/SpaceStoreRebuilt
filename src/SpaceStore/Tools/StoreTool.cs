using SpaceStore.UI;
using UnityEngine;

namespace SpaceStore.Tools {
  /// <summary>商店工具：激活时暂停游戏、锁镜头，并弹出商店窗口。</summary>
  public sealed class StoreTool : InterfaceTool {
    public static StoreTool Instance;

    protected override void OnPrefabInit() {
      base.OnPrefabInit();
      Instance = this;
    }

    protected override void OnCleanUp() {
      base.OnCleanUp();
      Instance = null;
    }

    protected override void OnActivateTool() {
      base.OnActivateTool();
      SetPaused(true);
      StoreDialog.Show();
    }

    protected override void OnDeactivateTool(InterfaceTool new_tool) {
      base.OnDeactivateTool(new_tool);
      SetPaused(false);
      StoreDialog.Close();
    }

    private static void SetPaused(bool paused) {
      if (SpeedControlScreen.Instance != null) {
        if (paused) SpeedControlScreen.Instance.Pause(false);
        else SpeedControlScreen.Instance.Unpause(false);
      }
      if (CameraController.Instance != null)
        CameraController.Instance.DisableUserCameraControl = paused;
    }
  }
}
