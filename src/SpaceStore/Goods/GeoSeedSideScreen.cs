using System.Collections.Generic;
using System.Linq;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using STRINGS;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceStore.Goods {
  /// <summary>
  /// 地质种子的详情侧屏：列出所有可生成的间歇泉，选一个再点激活。
  ///
  /// 实现方式沿用扩展包：复制游戏自带的 ArtableSelectionSideScreen 作为外壳，
  /// 再用 PLib 的 Detour 拿到它的私有控件。这样不用自己拼 UI 预制体。
  /// </summary>
  public sealed class GeoSeedSideScreen : SideScreenContent {
    private static readonly IDetouredField<ArtableSelectionSideScreen, KButton> APPLY_BUTTON =
      PDetours.DetourField<ArtableSelectionSideScreen, KButton>(nameof(applyButton));

    private static readonly IDetouredField<ArtableSelectionSideScreen, KButton> CLEAR_BUTTON =
      PDetours.DetourField<ArtableSelectionSideScreen, KButton>(nameof(clearButton));

    private static readonly IDetouredField<ArtableSelectionSideScreen, GameObject> STATE_BUTTON_PREFAB =
      PDetours.DetourField<ArtableSelectionSideScreen, GameObject>(nameof(stateButtonPrefab));

    private static readonly IDetouredField<ArtableSelectionSideScreen, RectTransform> BUTTON_CONTAINER =
      PDetours.DetourField<ArtableSelectionSideScreen, RectTransform>(nameof(buttonContainer));

    public GameObject stateButtonPrefab;

    [SerializeField] private RectTransform buttonContainer;

    private readonly Dictionary<int, MultiToggle> buttons = new Dictionary<int, MultiToggle>();

    private KButton applyButton;
    private KButton clearButton;
    private Tag selectedGeyser;
    private GeoSeed target;

    protected override void OnSpawn() {
      base.OnSpawn();
      if (applyButton != null) {
        applyButton.onClick += () => {
          if (selectedGeyser != default && target != null) target.Activate(selectedGeyser);
          else PlaySound(GlobalAssets.GetSound("Negative"));
        };
      }
    }

    public override void SetTarget(GameObject targetObject) {
      target = targetObject.GetComponent<GeoSeed>();
      GenerateStateButtons();
    }

    public override string GetTitle() {
      return MyString.UI.GOODS.GEOSEED.TITLE;
    }

    public override bool IsValidForTarget(GameObject targetObject) {
      return targetObject.GetComponent<GeoSeed>() != null;
    }

    private void GenerateStateButtons() {
      if (buttons.Count > 0 || buttonContainer == null) return;

      var index = 0;
      var geyserPrefabs = Assets.GetPrefabsWithComponent<Geyser>();
      if (geyserPrefabs != null) {
        foreach (var go in geyserPrefabs) {
          var prefabId = go.GetComponent<KPrefabID>();
          if (prefabId != null && prefabId.HasTag(GameTags.DeprecatedContent)) continue;
          AddOneButton(go, index++);
        }
      }

      var oilWell = Assets.GetPrefab(OilWellConfig.ID);
      if (oilWell != null) AddOneButton(oilWell, index);
    }

    private void AddOneButton(GameObject go, int index) {
      var sprite = Def.GetUISprite(go).first;
      var instance = Util.KInstantiateUI(stateButtonPrefab, buttonContainer.gameObject, true);
      var toggle = instance.GetComponent<MultiToggle>();

      var tooltip = instance.GetComponent<ToolTip>();
      if (tooltip != null) tooltip.SetSimpleTooltip(STRINGS.UI.StripLinkFormatting(go.GetProperName()));

      var references = instance.GetComponent<HierarchyReferences>();
      if (references != null) references.GetReference<Image>("Icon").sprite = sprite;

      toggle.onClick = () => {
        if (selectedGeyser != go.PrefabID()) {
          selectedGeyser = go.PrefabID();
          RefreshButtons();
          toggle.ChangeState(1);
        }
      };
      buttons.Add(index, toggle);
    }

    private void RefreshButtons() {
      foreach (var button in buttons.Values) {
        button.gameObject.SetActive(true);
        button.ChangeState(0);
      }
    }

    private static GeoSeedSideScreen CreateScreen(ArtableSelectionSideScreen template) {
      var clone = Instantiate(template.gameObject);
      clone.name = nameof(GeoSeedSideScreen);
      var active = clone.activeSelf;
      clone.SetActive(false);

      var oldScreen = clone.GetComponent<ArtableSelectionSideScreen>();
      var screen = clone.AddComponent<GeoSeedSideScreen>();
      screen.stateButtonPrefab = STATE_BUTTON_PREFAB.Get(oldScreen);
      screen.buttonContainer = BUTTON_CONTAINER.Get(oldScreen);

      screen.applyButton = APPLY_BUTTON.Get(oldScreen);
      if (screen.applyButton != null) {
        var tooltip = screen.applyButton.GetComponent<ToolTip>();
        if (tooltip != null) tooltip.SetSimpleTooltip(MyString.UI.GOODS.GEOSEED.TOOLTIP);
        TryChangeText(screen.applyButton.transform, "Label", MyString.UI.GOODS.GEOSEED.ACTIVATE);
      }

      screen.clearButton = CLEAR_BUTTON.Get(oldScreen);
      if (screen.clearButton != null) screen.clearButton.gameObject.SetActive(false);

      DestroyImmediate(oldScreen);
      clone.SetActive(active);
      return screen;
    }

    private static void TryChangeText(Transform root, string childName, string text) {
      var target = string.IsNullOrEmpty(childName) ? root : root.Find(childName);
      if (target == null) return;
      var label = target.gameObject.GetComponent<LocText>();
      if (label == null) return;
      label.key = string.Empty;
      label.text = text;
    }

    public static void AddSideScreen(IList<DetailsScreen.SideScreenRef> existing,
      DetailsScreen.SidescreenTab[] tabs) {
      GameObject parent = null;
      foreach (var tab in tabs) {
        if (tab.type == DetailsScreen.SidescreenTabTypes.Config) parent = tab.bodyInstance;
      }
      if (parent == null) {
        PUtil.LogError("Space Store: 找不到详情面板的 Config 页签，地质种子侧屏注册失败");
        return;
      }

      foreach (var reference in existing) {
        if (!(reference.screenPrefab is ArtableSelectionSideScreen template)) continue;
        var screen = CreateScreen(template);
        var newRef = new DetailsScreen.SideScreenRef {
          name = nameof(GeoSeedSideScreen),
          screenPrefab = screen,
          screenInstance = screen
        };
        var screenTransform = screen.gameObject.transform;
        screenTransform.SetParent(parent.transform);
        screenTransform.localScale = Vector3.one;
        existing.Insert(0, newRef);
        return;
      }

      PUtil.LogWarning("Space Store: 找不到可复用的侧屏模板（ArtableSelectionSideScreen）");
    }
  }
}
