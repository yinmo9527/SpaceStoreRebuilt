namespace SpaceStore {
  /// <summary>
  /// 所有可见文案。字段名即本地化键的末段（SpaceStore.MyString.*），
  /// translations/zh.po 与之一一对应，缺翻译时回落到这里的英文原文。
  /// </summary>
  public static class MyString {
    public static class OPTIONS {
      public static LocString ITEM_WIDTH = "Item Width";
      public static LocString ITEM_WIDTH_DESC = "Width of a single shop entry.";
      public static LocString COLUMNS = "Columns";
      public static LocString COLUMNS_DESC = "How many entries per row.";
      public static LocString DIALOG_HEIGHT = "Window Height";
      public static LocString DIALOG_HEIGHT_DESC = "Height of the Space Store window.";
      public static LocString PAGE_SIZE = "Page Size";
      public static LocString PAGE_SIZE_DESC = "Entries shown per page. Lower it if the shop feels laggy.";
    }

    public static class UI {
      public static LocString COIN_NAME = "Space Coin";

      public static class TOOL {
        public static LocString TITLE = "Space Store";
        public static LocString TOOL_TIP = "Open the Space Store.";
      }

      public static class TOOL_SELL {
        public static LocString TITLE = "Sell Objects";
        public static LocString TOOL_TIP = "Drag to sell anything on the ground.";
      }

      public static class SELL {
        public static LocString TITLE = "SELL";
        public static LocString TOOL_TIP = "Sell these {item} for {coin} space coins.";
      }

      public static class STORE {
        public static LocString BALANCE = "{0} space coins";
        public static LocString NO_TELEPAD = "No Printing Pod found - trading unavailable";
        public static LocString NOT_ENOUGH = "Not enough space coins";
        public static LocString PURCHASED = "Purchased {item}";
        public static LocString RELOAD = "Reload Config";
        public static LocString OPEN_FOLDER = "Open Config Folder";
        public static LocString OPEN_WEB = "Make a Config File";
        public static LocString PREV = "< Prev";
        public static LocString NEXT = "Next >";
        public static LocString PAGE = "Page {0} / {1}";
        public static LocString CONFIG_WARN = "{0} entries could not be resolved, see Player.log";
        public static LocString EMPTY = "Nothing for sale in this category";

        public static LocString SEARCH_HINT = "Search by name or id";
        public static LocString SEARCH = "Search";
        public static LocString CLEAR = "Clear";
        public static LocString BUY = "BUY";
        public static LocString UNIT_PRICE = "{price} coins / {unit}";
        public static LocString INVALID_QTY = "Enter a quantity greater than 0";
        public static LocString BOUGHT = "Bought {item} x{amount}{unit} for {cost} coins";
        public static LocString PREVIEW = "{item} x{amount} {unit} = {cost} coins";

        public static LocString UNIT_TONNE = "t";
        public static LocString UNIT_PORTION = "portion";
        public static LocString UNIT_SEED = "seed";
        public static LocString UNIT_EGG = "egg";
        public static LocString UNIT_CRITTER = "critter";
        public static LocString UNIT_PIECE = "pc";

        public static class FILTER {
          public static LocString ALL = "All";
          public static LocString MATERIAL = "Materials";
          public static LocString FOOD = "Food";
          public static LocString SEED = "Seeds";
          public static LocString EGG = "Eggs";
          public static LocString CRITTER = "Critters";
          public static LocString OTHER = "Other";
        }
      }

      public static class GOODS {
        public static class GEOSEED {
          public static LocString NAME = "Geo-Seed";
          public static LocString DESC =
            "\"Explosions bring the Geo-Seed, and meteors make them germinate.\"\n" +
            "Place it, pick a geyser type in the details panel, then activate it.";
          public static LocString TITLE = "Choose a Geyser";
          public static LocString ACTIVATE = "ACTIVATE";
          public static LocString TOOLTIP =
            "A geyser of the selected type will be created at this seed's position.";
        }
      }
    }
  }
}
