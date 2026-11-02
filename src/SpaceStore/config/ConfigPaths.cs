using System;
using System.Diagnostics;
using PeterHan.PLib.Core;
using UnityEngine;

namespace SpaceStore.Config {
  /// <summary>打开配置目录 / 在线编辑器。与游戏逻辑无关，单独一层方便替换。</summary>
  public static class ConfigPaths {
    public const string WebEditorUrl = "https://spacestore-config-editor.pages.dev/";

    public static void OpenFolder() {
      try {
        var dir = ConfigLoader.ConfigDir;
        switch (Application.platform) {
          case RuntimePlatform.WindowsPlayer:
          case RuntimePlatform.WindowsEditor:
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
            break;
          case RuntimePlatform.OSXPlayer:
          case RuntimePlatform.OSXEditor:
            Process.Start("open", $"\"{dir}\"");
            break;
          default:
            Process.Start("xdg-open", $"\"{dir}\"");
            break;
        }
      } catch (Exception e) {
        PUtil.LogExcWarn(e);
      }
    }

    public static void OpenWebEditor() {
      try {
        Process.Start(new ProcessStartInfo(WebEditorUrl) { UseShellExecute = true });
      } catch (Exception e) {
        PUtil.LogExcWarn(e);
      }
    }
  }
}
