using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using SpaceStore.Config;
using SpaceStore.Economy;
using SpaceStore.Store;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceStore.UI {
  /// <summary>
  /// 商店窗口。
  ///
  /// 相对旧版的三点变化：
  ///  1) 窗口尺寸按屏幕比例（默认宽高各 50%，面积正好是游戏窗口的 1/4）；
  ///  2) 顶部支持按名称 / id 搜索，中文名直接匹配；
  ///  3) 每张卡片自带数量输入框，按各自的单位（吨 / 颗 / 枚 / 只 / 个）换算总价，
  ///     材料默认 50 吨，其它按配置份数。
  /// </summary>
  public sealed class StoreDialog {
    public const int SPACE = 6;
    public const float ICON_SIZE = 42f;
    public const float COIN_ICON_SIZE = 22f;
    public const float MAX_AMOUNT = 100000f;

    /// <summary>单个补给包的最大量（材料按 kg 算），超出就拆包。</summary>
    private const float MaxPackageAmount = 20000f;

    public static GameObject DialogObj;

    private static StoreCategory? filter;
    private static int page;
    private static string query = string.Empty;
    private static string pendingQuery = string.Empty;

    /// <summary>每件商品当前输入的数量（字符串原样保存，切页/搜索后不丢）。</summary>
    private static readonly Dictionary<string, string> Amounts = new Dictionary<string, string>();

    private PLabel coinLabel;
    private PLabel statusLabel;
    private LocText coinText;
    private LocText statusText;

    private static Options Settings => SingletonOptions<Options>.Instance ?? new Options();

    public static void Show() {
      Close();
      if (Catalog.All.Count == 0 || Catalog.Dirty) Catalog.Rebuild();
      new StoreDialog();
      if (DialogObj != null) DialogObj.SetActive(true);
    }

    public static void Close() {
      if (DialogObj != null) {
        DialogObj.SetActive(false);
        UnityEngine.Object.Destroy(DialogObj);
      }
      DialogObj = null;
    }

    private StoreDialog() {
      var options = Settings;
      var size = DialogSize(options);

      coinLabel = new PLabel("coin") {
        Text = BalanceText(),
        TextStyle = PUITuning.Fonts.TextDarkStyle,
        Sprite = StaticVars.CoinIcon,
        SpriteSize = new Vector2(COIN_ICON_SIZE, COIN_ICON_SIZE),
        SpritePosition = TextAnchor.MiddleLeft,
        FlexSize = Vector2.right,
        TextAlignment = TextAnchor.MiddleLeft
      };

      statusLabel = new PLabel("status") {
        Text = string.Empty,
        TextStyle = PUITuning.Fonts.TextDarkStyle,
        TextAlignment = TextAnchor.MiddleLeft,
        FlexSize = Vector2.right
      };

      var body = new PPanel("Body") {
        Direction = PanelDirection.Vertical,
        Spacing = SPACE,
        FlexSize = Vector2.right
      };
      body.AddChild(BuildHeader());
      body.AddChild(statusLabel);
      body.AddChild(BuildFilterRow());
      body.AddChild(BuildGrid(options));
      body.AddChild(BuildFooter());

      var scroll = new PScrollPane("Scroll") {
        Child = body,
        FlexSize = Vector2.right,
        ScrollVertical = true,
        AlwaysShowVertical = true,
        TrackSize = 8
      };

      var dialog = new PDialog("SpaceStoreDialog") {
        Title = MyString.UI.TOOL.TITLE,
        DialogClosed = _ => {
          if (ToolMenu.Instance != null) ToolMenu.Instance.ClearSelection();
        },
        Size = size,
        MaxSize = size,
        DialogBackColor = Color.white
      };
      dialog.Body.AddChild(scroll);

      DialogObj = dialog.Build();
      coinText = FindLabelText(DialogObj, "coin");
      statusText = FindLabelText(DialogObj, "status");
    }

    private static Vector2 DialogSize(Options options) {
      var width = options.WindowWidthRatio > 0f
        ? Screen.width * options.WindowWidthRatio
        : 900f;
      var height = options.WindowHeightRatio > 0f
        ? Screen.height * options.WindowHeightRatio
        : options.DialogHeight;
      return new Vector2(Mathf.Max(640f, width), Mathf.Max(360f, height));
    }

    private PPanel BuildHeader() {
      var header = new PPanel("Header") {
        Direction = PanelDirection.Horizontal,
        Spacing = SPACE,
        FlexSize = Vector2.right
      };

      header.AddChild(coinLabel);

      header.AddChild(new PTextField("search") {
        Text = query,
        PlaceholderText = MyString.UI.STORE.SEARCH_HINT.ToString(),
        MinWidth = 220,
        MaxLength = 40,
        TextStyle = PUITuning.Fonts.TextDarkStyle,
        OnTextChanged = (source, text) => pendingQuery = text
      });

      header.AddChild(new PButton("doSearch") {
        Text = MyString.UI.STORE.SEARCH,
        Color = PUITuning.Colors.ButtonBlueStyle,
        OnClick = _ => {
          query = pendingQuery ?? string.Empty;
          page = 0;
          Rebuild();
        }
      });

      header.AddChild(new PButton("clearSearch") {
        Text = MyString.UI.STORE.CLEAR,
        Color = PUITuning.Colors.ButtonBlueStyle,
        OnClick = _ => {
          query = string.Empty;
          pendingQuery = string.Empty;
          page = 0;
          Rebuild();
        }
      });

      return header;
    }

    private PPanel BuildFilterRow() {
      var row = new PPanel("Filters") {
        Direction = PanelDirection.Horizontal,
        Spacing = SPACE,
        FlexSize = Vector2.right
      };

      row.AddChild(FilterButton(null, MyString.UI.STORE.FILTER.ALL));
      row.AddChild(FilterButton(StoreCategory.Material, MyString.UI.STORE.FILTER.MATERIAL));
      row.AddChild(FilterButton(StoreCategory.Food, MyString.UI.STORE.FILTER.FOOD));
      row.AddChild(FilterButton(StoreCategory.Seed, MyString.UI.STORE.FILTER.SEED));
      row.AddChild(FilterButton(StoreCategory.Egg, MyString.UI.STORE.FILTER.EGG));
      row.AddChild(FilterButton(StoreCategory.Critter, MyString.UI.STORE.FILTER.CRITTER));
      row.AddChild(FilterButton(StoreCategory.Other, MyString.UI.STORE.FILTER.OTHER));
      return row;
    }

    private PButton FilterButton(StoreCategory? category, LocString label) {
      var active = Nullable.Equals(filter, category);
      return new PButton($"filter_{category?.ToString() ?? "all"}") {
        Text = active ? "▸ " + label.ToString() : label.ToString(),
        Color = PUITuning.Colors.ButtonBlueStyle,
        OnClick = _ => {
          ApplyPending();
          filter = category;
          page = 0;
          Rebuild();
        }
      };
    }

    private PPanel BuildGrid(Options options) {
      var grid = new PPanel("Grid") {
        Direction = PanelDirection.Vertical,
        Spacing = SPACE,
        FlexSize = Vector2.right
      };

      var items = Matches().ToList();
      if (items.Count == 0) {
        grid.AddChild(new PLabel("empty") {
          Text = MyString.UI.STORE.EMPTY,
          TextStyle = PUITuning.Fonts.TextDarkStyle,
          TextAlignment = TextAnchor.MiddleCenter,
          FlexSize = Vector2.right
        });
        return grid;
      }

      var pageSize = Math.Max(1, options.PageSize);
      var pageCount = (items.Count + pageSize - 1) / pageSize;
      page = Math.Max(0, Math.Min(page, pageCount - 1));

      var columns = Math.Max(1, options.Columns);
      // 窗口变窄时按实际宽度收列，避免卡片被挤出可视区
      var maxByWidth = Mathf.FloorToInt((DialogSize(options).x - 80f) / (options.ItemWidth + SPACE));
      columns = Mathf.Clamp(Mathf.Min(columns, Math.Max(1, maxByWidth)), 1, 12);

      var slice = items.Skip(page * pageSize).Take(pageSize).ToList();

      PPanel row = null;
      for (var i = 0; i < slice.Count; i++) {
        if (i % columns == 0) {
          row = new PPanel($"row_{i}") {
            Direction = PanelDirection.Horizontal,
            Spacing = SPACE,
            FlexSize = Vector2.right
          };
          grid.AddChild(row);
        }
        row?.AddChild(BuildCard(slice[i], options.ItemWidth));
      }

      return grid;
    }

    private PPanel BuildCard(StoreEntry entry, int width) {
      var id = entry.Id;

      // 用纵向排列而不是相对布局：名称换行时高度会自己撑开，
      // 购买按钮不会被钉在底部压到名字上。
      var card = new PPanel($"card_{id}") {
        Direction = PanelDirection.Vertical,
        Spacing = 2,
        BackImage = PUITuning.Images.ButtonBorder,
        ImageMode = Image.Type.Sliced,
        BackColor = PUITuning.Colors.ButtonBlueStyle.inactiveColor
      };

      card.AddChild(new PLabel($"label_{id}") {
        Sprite = entry.Sprite?.first,
        SpriteTint = entry.Sprite?.second ?? Color.white,
        SpriteSize = new Vector2(ICON_SIZE, ICON_SIZE),
        SpritePosition = TextAnchor.UpperCenter,
        Text = entry.Describe(),
        IconSpacing = SPACE,
        FlexSize = Vector2.right,
        TextAlignment = TextAnchor.LowerCenter,
        Margin = new RectOffset(4, 4, 4, 0)
      });

      var row = new PPanel($"qty_{id}") {
        Direction = PanelDirection.Horizontal,
        Spacing = 4,
        FlexSize = Vector2.right,
        Margin = new RectOffset(4, 4, 2, 4)
      };

      row.AddChild(new PTextField($"qtyField_{id}") {
        Text = Amounts.TryGetValue(id, out var saved) ? saved : entry.DefaultAmountText,
        MinWidth = 48,
        MaxLength = 6,
        TextStyle = PUITuning.Fonts.TextDarkStyle,
        OnTextChanged = (source, text) => {
          Amounts[id] = text;
          Preview(entry, text);
        }
      });

      row.AddChild(new PLabel($"unit_{id}") {
        Text = entry.UnitLabel,
        TextStyle = PUITuning.Fonts.TextDarkStyle,
        TextAlignment = TextAnchor.MiddleLeft
      });

      row.AddChild(new PButton($"buy_{id}") {
        Text = MyString.UI.STORE.BUY,
        Color = PUITuning.Colors.ButtonBlueStyle,
        OnClick = _ => Buy(entry)
      });

      card.AddChild(row);

      // 零高度的占位，只为把卡片宽度固定成统一值
      card.AddChild(new PSpacer {
        PreferredSize = new Vector2(width, 0f)
      });

      return card;
    }

    private PPanel BuildFooter() {
      var footer = new PPanel("Footer") {
        Direction = PanelDirection.Horizontal,
        Spacing = SPACE,
        FlexSize = Vector2.right
      };

      footer
        .AddChild(new PButton("prev") {
          Text = MyString.UI.STORE.PREV,
          OnClick = _ => {
            ApplyPending();
            page = Math.Max(0, page - 1);
            Rebuild();
          }
        })
        .AddChild(new PLabel("page") {
          Text = PageText(),
          TextStyle = PUITuning.Fonts.TextDarkStyle,
          TextAlignment = TextAnchor.MiddleLeft,
          FlexSize = Vector2.right
        })
        .AddChild(new PButton("next") {
          Text = MyString.UI.STORE.NEXT,
          OnClick = _ => {
            ApplyPending();
            page++;
            Rebuild();
          }
        })
        .AddChild(new PButton("reload") {
          Text = MyString.UI.STORE.RELOAD,
          OnClick = _ => {
            ConfigLoader.Reload();
            Catalog.Rebuild();
            page = 0;
            Rebuild();
          }
        })
        .AddChild(new PButton("folder") {
          Text = MyString.UI.STORE.OPEN_FOLDER,
          OnClick = _ => ConfigPaths.OpenFolder()
        })
        .AddChild(new PButton("web") {
          Text = MyString.UI.STORE.OPEN_WEB,
          OnClick = _ => ConfigPaths.OpenWebEditor()
        });

      return footer;
    }

    /// <summary>把输入框里的关键字落到查询条件上，翻页/切页签时也不会丢。</summary>
    private static void ApplyPending() {
      if (!string.IsNullOrEmpty(pendingQuery)) query = pendingQuery;
    }

    private static IEnumerable<StoreEntry> Matches() {
      var keyword = query?.Trim();
      return Catalog.All.Where(entry => {
        if (filter != null && entry.Category != filter.Value) return false;
        if (string.IsNullOrEmpty(keyword)) return true;
        return entry.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
               entry.Id.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
      });
    }

    private static string PageText() {
      var count = Matches().Count();
      var pageSize = Math.Max(1, Settings.PageSize);
      var pageCount = Math.Max(1, (count + pageSize - 1) / pageSize);
      return string.Format(MyString.UI.STORE.PAGE.ToString(), page + 1, pageCount);
    }

    private void Rebuild() {
      Show();
    }

    private static string BalanceText() {
      var wallet = CoinWallet.Get();
      return wallet == null
        ? MyString.UI.STORE.NO_TELEPAD.ToString()
        : string.Format(MyString.UI.STORE.BALANCE.ToString(), wallet.Balance.ToString("0.00"));
    }

    private void Buy(StoreEntry entry) {
      var wallet = CoinWallet.Get();
      var telepad = TelepadLocator.Active();
      if (wallet == null || telepad == null) {
        SetStatus(MyString.UI.STORE.NO_TELEPAD.ToString());
        return;
      }

      var raw = Amounts.TryGetValue(entry.Id, out var saved) ? saved : null;
      if (string.IsNullOrWhiteSpace(raw)) raw = entry.DefaultAmountText;
      if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) ||
          amount <= 0f) {
        SetStatus(MyString.UI.STORE.INVALID_QTY.ToString());
        return;
      }
      amount = Mathf.Min(amount, MAX_AMOUNT);

      var cost = entry.CostFor(amount);
      if (!wallet.TrySpend(cost)) {
        SetStatus(MyString.UI.STORE.NOT_ENOUGH.ToString());
        return;
      }

      // 数量是动态的，不能复用目录里按配置份数建好的 CarePackageInfo。
      // 单包超过 20 吨时拆成多包，避免一次生成过大的堆叠。
      var remaining = entry.ToGameAmount(amount);
      var teleportTo = telepad.transform.position;
      var guard = 0;
      while (remaining > 0f && guard++ < 64) {
        var chunk = Mathf.Min(remaining, MaxPackageAmount);
        new CarePackageInfo(entry.Tag.ToString(), chunk, null).Deliver(teleportTo);
        remaining -= chunk;
      }

      if (CameraController.Instance != null)
        CameraController.Instance.CameraGoTo(teleportTo);

      SetStatus(MyString.UI.STORE.BOUGHT
        .Replace("{item}", entry.Name)
        .Replace("{amount}", amount.ToString("0.##"))
        .Replace("{unit}", entry.UnitLabel)
        .Replace("{cost}", cost.ToString()));

      if (coinText != null) coinText.text = BalanceText();
    }

    private void SetStatus(string text) {
      if (statusText != null) statusText.text = text;
      else if (statusLabel != null) statusLabel.Text = text;
    }

    /// <summary>输入数量时实时给出总价，省得点下去才知道多少钱。</summary>
    private void Preview(StoreEntry entry, string text) {
      if (string.IsNullOrWhiteSpace(text)) return;
      if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) ||
          amount <= 0f) return;
      amount = Mathf.Min(amount, MAX_AMOUNT);
      SetStatus(MyString.UI.STORE.PREVIEW
        .Replace("{item}", entry.Name)
        .Replace("{amount}", amount.ToString("0.##"))
        .Replace("{unit}", entry.UnitLabel)
        .Replace("{cost}", entry.CostFor(amount).ToString()));
    }

    /// <summary>PLib 的 Label 在 Build 之后才能拿到真正的 LocText，用它做局部刷新。</summary>
    private static LocText FindLabelText(GameObject root, string name) {
      if (root == null) return null;
      foreach (var transform in root.GetComponentsInChildren<Transform>(true)) {
        if (transform.name != name) continue;
        var text = transform.GetComponentInChildren<LocText>(true);
        if (text != null) return text;
      }
      return null;
    }
  }
}
