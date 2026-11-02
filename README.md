# Space Store 太空商店 · 重构版 v2.2

把《缺氧》创意工坊的 **Space Store 太空商店**（ID `3215097987`）和 **Space Store Expansion Pack**
（ID `3231852237`）合并成一个 mod，重写内部结构，并内置一份覆盖本体 + 全 DLC 内容、
共 **421 条** 商品的默认配置。

- 源码：`src/SpaceStore/`
- 默认配置：`src/SpaceStore/config/store_default.json`、`sell_default.json`
- 价目表（可读版）：`docs/items.csv`（421 行）
- 生成/校验脚本：`tools/`

## 特性

- **一个 mod 顶两个**：商店、回收、地质种子扩展包合并，不再需要两个 DLL 互相依赖。
- **开箱 421 件商品**：材料 195 / 食物 85 / 种子 47 / 生物 37 / 蛋 21 / 工业品 23 / 药品 9 / 香料 3 / 地质种子 1，
  覆盖本体与全部 DLC（铀系、钴系、钨铌、Frosty Planet、新行星包材料都在）。
- **买得顺手**：窗口占屏幕 1/4，中文名搜索、分类页签、分页，每件商品填数量后一键买；
  材料按「吨」、种子按「颗」、蛋按「枚」、生物按「只」，边输入边算总价。
- **卖得值钱**：回收价整体 ×10，卖 50 吨废料得 100 币；初始资金 10000。
- **不炸存档**：单一金币账本，旧版 `StoreData` 余额自动迁移；旧 `store_*.json` / `sell_*.json` 原样可用。
- **少踩坑**：只保留 4 个稳定的 Harmony 补丁，可回收组件改成 prefab 扫描注入，游戏更新不容易整个 mod 挂掉。

## 安装（给使用者）

**前置**：游戏本体；任意一个自带 `PLib.dll` 的 mod（PLib 是界面依赖，没有它本 mod 不会加载）。

### 方式 A：下载现成包（推荐，不用装开发工具）

1. 到 [Releases](../../releases) 下载 `SpaceStoreRebuilt-v2.2.0.zip`。
2. 解压，把里面的 `SpaceStoreRebuilt` 整个文件夹放到
   `文档\Klei\OxygenNotIncluded\mods\local\`。
3. 确认 PLib 处于启用状态。
4. 启动游戏 →「模组」→ 勾选 **Space Store 太空商店 重构版** → 重启游戏。

最终目录必须长这样，层级错了游戏读不到：

```
mods\local\SpaceStoreRebuilt\
├─ SpaceStoreRebuilt.dll
├─ mod.yaml
├─ mod_info.yaml
├─ translations\zh.po
└─ anim\assets\geo_seed\...
```

### 方式 B：从源码编译

见第 7 节。需要 .NET SDK 或 Visual Studio，编译后会自动铺到同一个目录。

### 更新 / 卸载

- 更新：关游戏，用新包覆盖同名文件夹；`mods\spacestore\` 里的个人商店配置不会被覆盖。
- 卸载：删掉 `mods\local\SpaceStoreRebuilt` 文件夹，或在模组菜单取消勾选。

### 装完没生效？按这个顺序查

1. 模组列表里**根本没有这一项**——多半是目录层级不对，常见错误是把 dll 直接丢进 `mods\local`。
2. **有这一项但进游戏没按钮**——PLib 没启用，这是最常见的原因。
3. 工坊的 `3215097987`、`3231852237` 是否还开着，它们会抢同一套工具栏和 `mods\spacestore` 配置目录。
4. 仍然不行，看 `文档\Klei\OxygenNotIncluded\Player.log`，搜 `Space Store`。

> 说明：已在 2026-11-01 用 **.NET SDK 8.0.404**（用户级安装，`%USERPROFILE%\.dotnet`）
> 对本机游戏程序集**实际编译通过**（`net48`，0 error）。产物已部署到
> `mods\local\SpaceStoreRebuilt\`。

---

## 1. 调研：原来这两个 mod 是什么

| | Space Store 太空商店 | Space Store Expansion Pack |
|---|---|---|
| 工坊 ID | 3215097987 | 3231852237 |
| staticID | `CalYu.SpaceStore` | `CalYu.StoreGoods` |
| 作者 | Chinchou（CalYu） | 同上体系 |
| 版本 / 体积 | v1.0.7 / 171.978 KB | 无版本号 |
| 兼容 | 本体 + Spaced Out! | 本体 |
| 依赖 | PLib（界面） | Space Store |
| 最后更新 | 2025-03-13 | 2024 年 |

**太空商店本体**
- 详情菜单给固体元素、种子、间歇泉加「卖出」按钮；
- 工具栏加「太空商店」按钮，弹窗里按网格铺商品，买完以**补给包**形式掉在打印舱位置；
- 商品与回收价都读 `mods/spacestore/` 下的 `store_*.json` / `sell_*.json`，没有文件则用内置默认表；
- 配置项只有条目宽度、列数、窗口高度。

**扩展包**
- 新增「地质种子」（Geo-Seed，600 币）：放到地上 → 详情面板选一种间歇泉 → 激活，原地生成该泉；
- 启动时往 `mods/spacestore/` 写一份 `store_*.json`，把种子塞进本体商店。

**原作者状态**：2025-06-21 在工坊回复「找不到之前处理数据的代码了，重写一遍很麻烦，
这个 mod 准备放弃迭代了，我要做一版新的」。代码仓库在 `yuanze31/ttdly-ONI-Mods`（SpaceStore / StoreGoods 两个目录）。

### 读源码读出来的问题

| # | 位置 | 问题 |
|---|---|---|
| 1 | `StoreData` + `CoinSaver` | 金额存两份（SaveGame 一份、打印舱一份），靠 `sync` 标志在 `OnSpawn` 互相覆盖，读档顺序一变就丢钱/翻倍 |
| 2 | `StoreList.GetLocalList` | 多个 `store_*.json` 直接追加，同一个 id 会刷出多条重复商品 |
| 3 | `StoreDialog` 刷新按钮 | 只 `marketItems.Clear()` 不重新 `Init()`，点「重载配置」后商店直接变空 |
| 4 | `Options` | `DialogHeight` 误用了 `ItemWidth` 的本地化 key；且 key `STRINGS.OPTIONS.*` 在 `MyString` 里不存在，选项页显示的是 key 原文 |
| 5 | `PraseJson` | `price` 用 `(int)` 强转、`PriceConvter.sellItems.Add` 重复 id 直接抛异常；异常被 `catch` 吞掉只留一行 warn |
| 6 | `StoreDialog.CreateItems` | 一次性构建全部卡片，商品上百时打开窗口明显卡顿 |
| 7 | `Patches` | 给 `EntityTemplates.CreateOreEntity` / `CreateAndRegisterSeedForPlant`（19 个参数）/ `GeyserGenericConfig.CreateGeyser` 打补丁逐个挂组件——这三个签名每次游戏大版本都可能变，一挂就整个 mod 起不来 |
| 8 | `StoreDialog` 构造 | 在 `ToolMenu.CreateBasicTools` 前缀里就 `new StoreDialog()`，此时 Assets 还没就绪 |
| 9 | `NeutroniumMover` | 卖间歇泉时顺手把下方中子物质转成沙子，等于变相送资源 |
| 10 | `BrushSellTool` | 每刷一格遍历全局按钮列表，物品多时是 N×M 次比较 |

---

## 2. 重构后的结构

```
src/SpaceStore/
├─ Mod.cs                    入口：PLib 初始化、选项、本地化、热键
├─ MyString.cs               全部文案（LocString）
├─ Options.cs                展示参数（宽度/列数/窗高/每页数）
├─ Patches.cs                Harmony 补丁，只留 4 个
├─ SellableInjector.cs       prefab 扫描注入，替代 3 个工厂方法补丁
├─ Config/
│  ├─ StoreItemDef.cs        商品 DTO（id / price / quantity）
│  ├─ ConfigLoader.cs        内置默认 + 用户覆盖、去重、错误汇总、真·重载
│  └─ ConfigPaths.cs         打开配置目录 / 在线编辑器
├─ Economy/
│  └─ CoinWallet.cs          唯一账本（SaveGame + ISaveLoadable），含旧存档迁移
├─ Store/
│  ├─ StoreEntry.cs          id → prefab/图标/名称/补给包，分类推断
│  ├─ Catalog.cs             目录缓存 + 解析失败清单
│  └─ TelepadLocator.cs      当前星球打印舱
├─ UI/
│  └─ StoreDialog.cs         分页 + 分类筛选的商店窗口
├─ Tools/
│  ├─ StoreTool.cs           商店工具（暂停 + 锁镜头）
│  └─ SellBrushTool.cs       刷子批量出售
├─ Sell/
│  ├─ Sellable.cs            可回收基类 + 右键菜单
│  ├─ ElementSellable.cs     按质量计价
│  └─ GeyserSellable.cs      间歇泉按口计价
├─ Goods/
│  ├─ GeoSeedConfig.cs       地质种子 prefab
│  ├─ GeoSeed.cs             激活 → 生成间歇泉
│  └─ GeoSeedSideScreen.cs   详情面板里选间歇泉（沿用扩展包做法）
├─ config/                   内置默认配置（编译进 DLL）
├─ images/                   金币与工具图标（脚生成）
├─ anim/                     地质种子动画（取自原扩展包）
└─ translations/zh.po        中文本地化
```

数据流：

```
store_*.json ─┐
内置默认表  ─┴→ ConfigLoader(覆盖+去重) → Catalog(解析 prefab) → StoreDialog(分页/筛选) → 购买 → CarePackageInfo.Deliver → 打印舱
sell_*.json  ─→ ConfigLoader → Sellable 计价 ← SellableInjector 注入的组件 ← 右键菜单 / 刷子
                                          ↘ CoinWallet（唯一真源，随存档）
```

---

## 3. 逐条改了什么

| 原问题 | 现在怎么做 |
|---|---|
| 金额两份 | 只留 `CoinWallet`（挂 SaveGame），UI 与所有交易读同一个 `Balance`；`TrySpend` 原子扣款 |
| 旧存档怎么办 | `CoinWallet.OnSpawn` 反射找 `SpaceStore.Store.StoreData/CoinSaver` 上的 `coin` 字段，抄过来，余额不归零 |
| 重复商品 | 以 id 为键合并，后加载的覆盖先加载的（内置 → 用户文件） |
| 重载无效 | `Reload()` 真正清空重读；UI 重载按钮 = Reload + 重建目录 + 重建窗口 |
| 解析失败静默 | 每条错误带「文件名#序号」，汇总进 `ConfigLoader.Errors`；目录层再汇总无法识别的 id 打到 `Player.log` |
| JSON 强转崩 | 全部走 `JObject.Value<T?>`，缺字段/类型错只记一条错误，不影响其它条目 |
| 选项 key 错 | 标题/说明直接写中英双语文案，不再依赖不存在的 LocString |
| 一次建几百张卡 | 分页（默认每页 40，可调），只构建当前页 |
| 三个脆弱补丁 | 删掉，改成 `SellableInjector`：存档对象生成时扫 `Assets.GetPrefabsWithComponent<PrimaryElement>()`，id 在回收价表里就挂组件；间歇泉同理扫 `Geyser` |
| 构造太早 | `Catalog` 延迟到第一次打开商店时解析 |
| 中子物质换沙 | 删掉 `NeutroniumMover`，卖泉就是卖泉 |
| 刷子 O(N) | 仍然是按格查，但组件在对象上，`Grid.PosToCell` 直接比对，且只处理 `CanSell` 的 |
| 分类 | 新增「全部/材料/食物/种子/蛋/其他」筛选（按 `ElementLoader` / `EdiblesManager` / id 后缀推断，不写进配置，旧配置照样能用） |

补丁只剩 4 个，且都是原 mod 已经在用、长期稳定的入口：
`PlayerController.OnPrefabInit`（挂工具）、`ToolMenu.CreateBasicTools`（工具栏）、
`SaveGame.OnPrefabInit`（钱包 + 注入）、`DetailsScreen.OnPrefabInit`（侧屏）。

---

## 4. 购买界面（v2.1）

**窗口**：默认占游戏窗口宽高的各 50%，面积正好是 1/4。想要别的比例，在模组选项里改
`窗口宽度占比 / Width Ratio`、`窗口高度占比 / Height Ratio` 即可；两个都设成 0 时回落到固定高度。

**搜索**：顶部输入框支持中文名和 id（不区分大小写），点「搜索」生效，「清除」复位。
名称取自游戏当前的本地化，所以中文环境下直接打「火成岩」「藻类」就能筛出来。
搜索词会和分类页签叠加，切换页签不会丢掉关键词。

**数量**：每张卡片自带输入框，边输入边在状态栏显示实时总价，改完直接点「购买」，不用反复点。购买后**窗口不会关**，
方便连续买多笔；底部状态栏会显示「已购买 火成岩 50 吨，花费 400 币」，余额实时刷新。

**单位**：不同物品的「1 个」含义不同，卡片上会标明单价单位，输入框旁边也有单位提示。

| 分类 | 单位 | 默认购买量 | 说明 |
|---|---|---|---|
| 材料（元素） | 吨 | **50 吨** | 按质量计价，50 吨 = 50000 kg |
| 食物 | 份 | 配置份数（默认 10） | 游戏内按食物单位发放 |
| 种子 | 颗 | 配置份数（默认 3） | |
| 蛋 | 枚 | 配置份数（默认 2） | |
| 生物 | 只 | 配置份数（默认 1） | 靠 `CreatureBrain` 识别 |
| 特殊消耗品 / 工业品 / 地质种子 | 个 | 配置份数 | 地质种子固定 1 枚 |

**计价方式**：`总价 = 配置单价 ÷ 配置份数 × 你输入的数量`，材料先把吨换算成 kg。
所以改价格只要改 `store_*.json`，界面上的单价会自动跟着变。
单次超过 20 吨的订单会拆成多个补给包投递，避免生成一个超大堆叠。

## 5. 配置

目录不变：`文档\Klei\OxygenNotIncluded\mods\spacestore\`

| 文件 | 作用 | 格式 |
|---|---|---|
| `store_*.json` | 商店商品 | `[{"id":"Algae","price":100,"quantity":5000}]` |
| `sell_*.json` | 回收单价（每单位质量/每件） | `[{"id":"Sand","price":0.0002}]` |

规则：
- 前缀必须是 `store_` / `sell_`，后缀 `.json`；
- **内置默认表先加载，用户文件覆盖同名 id**（不再追加，所以不会重复）；
- 同一个 id 在多个文件里出现时，按文件名排序，后一个赢；
- 改完在游戏里点「重载配置」，或重启游戏。

旧配置可以直接用：你现在 `mods/spacestore/` 里的 `store_常用.json`、`1store_1790658824155.json`、
`store_StoreGoods...json` 都会被读到并覆盖默认值。其中 `Store_GeoSeed` 那条现在已经是内置商品，
重复也没关系（同 id 合并）。

---

## 6. 默认配置：421 条，覆盖到哪

| 分类 | 条数 | 说明 |
|---|---|---|
| 材料 | 195 | 全部固体/液体/气体元素，含 DLC 的铀系（UraniumOre / EnrichedUranium / DepletedUranium / NuclearWaste / SolidNuclearWaste / Yellowcake / Radium）、钴系、钨/铌/富勒烯/异树脂、以及新 DLC 的 Amber / Cinnabar / Electrum / Galena / Iridium / Nickel / Zinc / Basalt / Coquina / Pearl / Peat / Shale / Siltstone / Sucrose / Tallow / Milk 等 |
| 食物 | 85 | 含 Frosty Planet 的 Tofu / SpicyTofu / Salsa / Quiche / ShellfishMeat / Pemmican / Smoked* 等 |
| 种子 | 47 | 含新植物 Carrot / DewDripper / Kelp / OxyCoral / PlanktonCoral / SeaTree 等 |
| 生物 | 37 | Hatch / Puft / Drecko / Pacu / Slickster / 辐射蜂 / 蟹类 / Seal / Bammoth / Flox / Stego / Raptor 等 |
| 蛋 | 21 | 按 `Assets` 里实际存在的 `*Egg` |
| 工业制品 | 23 | 纤维、外壳、气密垫、研究数据盘、电芯等 |
| 药品 | 9 | 各类药片 |
| 香料 | 3 | 机器/驾驶/保鲜香料 |
| 自建商品 | 1 | `Store_GeoSeed` 地质种子（600 币） |

**价格模型**（`tools/gen_config.py` 里可改）：

- 材料按「单位回收价 × 整包质量 × 溢价」定价，分 5 档：
  `T0 废料 5000kg / 40 币`、`T1 常见 3000kg`、`T2 加工 1500kg`、`T3 稀有 500kg`、`T4 尖端 100kg / 1000 币`；
- 食物 10 份、种子 3 个、蛋 2 个、生物 1 只、药品 3 片；
- **回收价整体 ×10**（`SELL_MULTIPLIER = 10.0`）：沙岩从 0.0002/吨涨到 0.002/吨，
  卖 50 吨废料从 10 币变成 100 币；
- 回收价再高也不能高过「买入单价的一半」（`SELL_CAP_RATIO = 0.5`）。材料溢价动辄几十倍，
  吃满 10 倍没问题；食物 / 种子 / 蛋 / 生物 / 药品这几类原本只有 3 倍溢价，直接 ×10 会变成
  「买了再卖净赚」，所以被压到 1.5 倍左右。想让它们也吃满 10 倍，把 `SELL_CAP_RATIO` 调大并
  同时抬高这些商品的买入价即可；
- 初始资金 **10000 币**（模组选项 `初始太空币 / Starting Coins` 可改），新档发放一次，
  已有余额的存档不会重置。

重新生成：

```bash
python tools/gen_config.py      # 读 items_db.json → 写 src/SpaceStore/config/*.json
python tools/validate_config.py # 校验字段/重复/ID/套利
```

`items_db.json` 是从**本机游戏安装目录**反推出来的：
`strings/strings_template.pot` 里的 `STRINGS.ELEMENTS.*` / `STRINGS.ITEMS.*` / `STRINGS.CREATURES.SPECIES.SEEDS.*`
等条目，和 `Assembly-CSharp.dll` 里的字符串字面量做大小写还原匹配，所以 id 是真实存在的 prefab id，
不是猜的。`tools/_probe.py` 保留了这个反推过程（路径按本机写死，游戏更新或换机器时改路径重跑即可）。

---

## 7. 构建与安装

前置：.NET SDK（本机已装在 `%USERPROFILE%\.dotnet`，8.0.404，用户级、无需管理员）；
游戏本体；**PLib**（本机随工坊 `2781902332` 一起提供，必须有一个带 PLib 的 mod 处于启用状态）。

**构建命令**：

```bash
cd src/SpaceStore
dotnet build SpaceStore.csproj -c Release
```

若 `dotnet` 不在 `PATH` 里（用户级安装常见），直接写全路径：
`"%USERPROFILE%\.dotnet\dotnet.exe" build SpaceStore.csproj -c Release`。

目标框架是 **net48**（游戏程序集本身就是 4.8，用 4.7.2 会因引用不兼容而全线报 `CS0246`）。
换机器时用属性覆盖路径：

```bash
dotnet build SpaceStore.csproj -c Release \
  -p:ONIGameDir="C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded" \
  -p:PLibDll="...\PLib.dll" \
  -p:DeployDir="...\mods\local\SpaceStoreRebuilt"
```

编译后自动按游戏要求的层级复制到 `文档\Klei\OxygenNotIncluded\mods\local\SpaceStoreRebuilt\`：

```
SpaceStoreRebuilt.dll
mod.yaml  mod_info.yaml
translations\zh.po
anim\assets\geo_seed\...
```

（`Copy` 任务不能压平目录，否则 PLib 找不到 `translations\*.po`、游戏加载不到 kanim。）
最后在游戏「模组」菜单里勾选 **Space Store 太空商店 重构版**，重启。

**务必先关掉工坊的 `3215097987` 和 `3231852237`**，否则两个 mod 会抢同一套工具栏按钮和同一个
`spacestore` 配置目录。

---

## 8. 已知风险 / 待验证

1. **编译已通过，但未进游戏跑过**：所有游戏/PLib API 都经过编译器校验（`net48` + 本机
   `Assembly-CSharp.dll`、`PLib 2.2.22`），剩下的风险是运行期行为（界面排版、侧屏注册）。
   第一次进游戏若有异常，`Player.log` 里搜 `Space Store`。
2. **地质种子侧屏**沿用了扩展包的做法：复制 `ArtableSelectionSideScreen` 再用 `PDetours` 取私有控件
   （`applyButton` / `buttonContainer` 等）。游戏若改动这个界面，侧屏会失效——但不会影响商店与回收。
3. **物品覆盖**：材料/食物/种子来自本地化表，命中率高；生物、工业制品、药品是按名称匹配 + 人工补的，
   个别 id 可能对不上。对不上不会崩，会在 `Player.log` 里打印
   `Space Store: 以下条目无法解析 -> id (prefab 不存在)`，照着删掉或改名即可。
4. **动画资源**：`anim/assets/geo_seed/*` 取自本机已订阅的原扩展包（同一作者资源，自用没问题）；
   若要公开发布，请自行替换成自制贴图。
5. 卖间歇泉不再清理下方中子物质，被中子物质包住的泉卖完仍会留个洞。
6. **PLib 版本差异**：界面用到的是按你本机装的 **2.2.22** 核对过的 API
   （`PTextField` 在 2.2.22 里没有 `FieldType`，所以数量校验放在代码里做；`PDialog.Size` 这个版本有）。
   若日后把 PLib 升到 4.x，这两处是最先要复查的地方，其余属性名两版一致。
7. **搜索不是实时过滤**：输入后要点「搜索」（或切一次页签）才生效。这样做的原因是实时过滤需要每敲一个字
   就重建整页卡片，输入框会失焦、根本没法连续打字。

---

## 9. 数据来源

- Steam 创意工坊页面 `3215097987` / `3231852237`（功能、价格、版本、作者回复）
- 源码参考：`github.com/yuanze31/ttdly-ONI-Mods` 的 `SpaceStore/`、`StoreGoods/`
- 物品 id 与分类：本机 `OxygenNotIncluded_Data/StreamingAssets/strings/strings_template.pot`
  \+ `Managed/Assembly-CSharp.dll` 元数据
- 在线配置编辑器 `spacestore-config-editor.pages.dev`（配置格式 `[{id,price,quantity}]` 即来源于此）

---

## 10. 授权与转载

- `src/` 下的重构代码由本项目作者编写，写作过程中参考了原 mod 的公开源码
  （`github.com/yuanze31/ttdly-ONI-Mods` 的 `SpaceStore`、`StoreGoods` 目录）。
  代码部分欢迎参考和二次开发，保留本仓库链接即可。
- `anim/assets/geo_seed/*`（地质种子的贴图与 kanim 动画）来自原
  **Space Store Expansion Pack**，版权归原作者 **Chinchou（CalYu）** 所有，
  本仓库仅作个人整合使用。
- 本仓库不包含《缺氧》游戏本体的任何资源，也不含 `PLib.dll`（请自行订阅 PLib）。
- 若要转载或并入整合包，**素材部分请先联系原作者取得授权**。
