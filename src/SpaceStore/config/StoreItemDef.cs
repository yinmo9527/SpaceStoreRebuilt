using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core;

namespace SpaceStore.Config {
  /// <summary>一条商店商品。字段名与旧版 JSON 完全一致，旧配置可直接沿用。</summary>
  public sealed class StoreItemDef {
    public string id;
    public int price;
    public int quantity;

    public StoreItemDef(string id, int price, int quantity) {
      this.id = id;
      this.price = price;
      this.quantity = quantity;
    }
  }
}
