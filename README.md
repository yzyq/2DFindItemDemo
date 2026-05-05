# 2DDemo - 1屏找物 + 光圈照明 + Spine交互

这是一个面向移动端竖屏的 Unity 2D 找物 Demo。当前版本以场景中已摆放好的地图、UI、Spine、查找物为基础，代码负责流程、光圈显示、地图手感、找物收集、UI反馈、音效和重置。

## 运行方式

1. 使用 Unity 打开工程根目录。
2. 打开场景 `Assets/Scenes/SampleScene.unity`。
3. 确认场景中存在：
   - `GameEntry`
   - `MapRoot`
   - `Canvas/UILoginPanel`
   - `Canvas/UIMainPanel`
   - `MapRoot` 下的 `ColorMap_shangse` 和 `LineArt_xiangao`
   - `SpotlightMask` / `SpotlightVisual`
4. 直接点击 Play。

当前工程使用 Unity 新 Input System，代码侧已经使用 `UnityEngine.InputSystem`，不再读取旧版 `UnityEngine.Input`。

## 当前效果

- 初始页显示 Logo / Title / Eye 动画，点击开始进入游戏页。
- 游戏页默认开启光圈。
- 线稿图常显，彩色底图和可交互内容只在光圈内显示。
- 光圈进入 Spine 对象时播放 idle，离开光圈后停止动画，再次进入会重新播放 idle。
- 点击 Spine 对象可播放交互动画和对象自身音效，交互中重复点击无效。
- 点击本关目标物后：
  - 播放目标物自身 `interactClip`
  - 目标物略微放大并飞向找物栏对应 UI item
  - 对应 UI item 先移动到列表末尾并滚动到最右侧
  - item 显示 check 状态
  - 飞行结束后场景目标物隐藏
- 点击非本关目标物时不播放飞行动画，触发错误音效和屏幕短抖。
- 点击空白、光圈外目标、不可交互状态不会触发错误震动。
- 所有目标完成后显示完成弹窗，支持重新开始和返回初始页。
- 背景音乐在游戏过程中持续循环播放，返回初始页时停止。

## 目录结构

```text
Assets/ZYQ/Scripts
├── Frame
│   ├── App              # AppContext / AppFacade / IManager
│   ├── Event            # EventDispatcher
│   ├── Manager          # ManagerBase
│   ├── Pool             # 通用对象池
│   ├── Singleton        # Singleton 基类
│   └── UI               # UIPanel / UIPanelPool / UIStack
└── Game
    ├── Config           # GameDataConfig / HiddenObject SO 配置
    ├── Entry            # GameEntry 入口
    ├── Input            # MapCameraRig 地图镜头与手感
    ├── Item             # LevelItemData
    ├── Level            # LevelData / LevelDataSO
    ├── Manager          # DataManager / GameManager / UIManager 等
    ├── Map              # Map / SpotlightSpineView
    └── UI               # UILoginPanel / UIMainPanel / ScrollView
```

## 核心架构

工程当前是轻量 Manager + 场景对象驱动结构：

- `GameEntry`
  - 程序入口。
  - 构建 `AppContext`。
  - 注册 `DataManager`、`UIManager`、`GameManager`、`InputManager`、`MatchSystem`、`SceneLoader`。
  - 每帧调用 `context.TickAll(Time.deltaTime)` 和 `EventDispatcher.Tick()`。

- `AppContext`
  - Manager 注册、初始化、获取和释放。
  - 代替全局单例串联运行时系统。

- `DataManager`
  - 管理字体、通用配置、关卡配置、`LevelDataSO`。
  - 当前关卡数据通过 `CurrentLevelData` 暴露。
  - `LevelDataSO.background` / `foreground` 会注入到玩法配置中，用作彩色底图和线稿图。

- `GameManager`
  - 控制开始页和游戏页切换。
  - 进入游戏页时打开 `UIMainPanel`，并启动 `Map.SetRunning(true)`。
  - 返回初始页时隐藏游戏页，停止地图运行和 BGM。
  - 重置按钮调用 `Map.ResetGame()`。

- `UIManager`
  - 打开 UI panel。
  - 优先复用场景里已经摆放的 `UIPanel`，避免重复创建 `UIMainPanel`。
  - 仍支持通过 `GameDataConfig.uiPanels` 配置 prefab / Resources 路径。

- `Map`
  - 玩法核心。
  - 绑定场景中已摆放的地图、线稿、彩稿、光圈、Spine、目标物。
  - 处理触控/鼠标输入、地图拖拽、光圈拖拽、缩放、边界回弹。
  - 控制 SpriteMask 显示规则。
  - 控制目标点击、目标收集飞行、错误反馈、BGM 和 SFX。

- `MapCameraRig`
  - 封装相机移动、缩放、边界限制、回弹和光圈靠边跟随。

- `SpotlightSpineView`
  - 控制非找物 Spine 交互对象。
  - 序列化 idle / interact 动画名和交互音效。
  - 只在光圈进入时启用并播放，退出时清轨道并停止更新。

- `HiddenObjectTargetView`
  - 控制场景中的查找物。
  - 只允许光圈照到时交互。
  - 正确点击时播放对象自身 `interactClip`。
  - 收集完成后隐藏。

- `UIMainPanel`
  - 游戏页 UI 逻辑。
  - 绑定返回、重置、光圈、收起/展开、完成弹窗按钮。
  - 加载 `LevelData.Items` 到找物栏。
  - 找到目标时移动 item 到末尾、滚动到最右、刷新 check。
  - 序列化 UI 按钮点击音效。

## 数据与配置

### GameEntry

场景中的 `GameEntry` 序列化字段：

- `fontAsset`：全局字体，可给 UI 使用。
- `commonConfigAsset`：通用手感、音效、UI prefab 配置。
- `levelConfigAssets`：找物玩法目标配置。
- `levelDataSO`：关卡数据库，包含找物栏数据、目标 id、背景图、线稿图、BGM。
- `gameData`：兜底配置。
- `demoConfig`：无 SO 时的兜底玩法配置。

推荐使用方式：

1. 用 `LevelDataSO` 配找物栏 item、目标 itemId、底图、线稿和 BGM。
2. 在 `GameEntry` 上引用这些 SO。

### HiddenObjectCommonConfigAsset

创建菜单：

```text
Assets/Create/ZYQ/Hidden Object/Common Config
```

用途：

- 通用 UI prefab 列表。
- 字体。
- 光圈参数。
- 地图拖拽手感。
- 缩放参数。
- Spine 默认动画名。
- 默认音效和反馈参数。

### HiddenObjectLevelConfigAsset

创建菜单：

```text
Assets/Create/ZYQ/Hidden Object/Level Config
```

用途：

- 关卡 id 和展示名。
- 关卡 Logo、线稿、彩稿、云朵、找物栏资源。
- 目标物配置列表 `targets`。

`targets` 中的 `displayName` 用于和 `LevelData.Items.ItemName` 匹配，找到真实 `itemId`。因此场景目标配置名和 LevelData item 名需要保持一致。

### LevelDataSO

创建菜单：

```text
Assets/Create/ZYQ/Level Database
```

核心字段：

- `Levels`：关卡数据列表。
- `LevelData.background`：彩色底图，运行时绑定到 `ColorMap_shangse`。
- `LevelData.foreground`：线稿图，运行时绑定到 `LineArt_xiangao`。
- `LevelData.bgm`：当前关卡 BGM。
- `LevelData.items`：找物栏展示的所有 item。
- `LevelData.targetItemIds`：本关真正需要找的 itemId。

注意：

- `items` 可以包含非目标 item。
- `targetItemIds` 决定本关目标。
- 点击不在 `targetItemIds` 中的场景物时，只触发错误反馈，不飞行、不打勾。

## 场景绑定约定

当前 Demo 以场景预摆放为主，代码只做初始化和控制，不主动重新摆放核心 UI 和地图对象。

### MapRoot

`MapRoot` 挂载 `Map`，重要序列化引用：

- `colorMapRenderers`：彩色底图，一般是 `ColorMap_shangse`。
- `lineArtRenderers`：线稿图，一般是 `LineArt_xiangao`。
- `spotlightVisibleSpriteRenderers`：需要被光圈显示的 Sprite。
- `spotlightVisibleSkeletonRenderers`：需要被光圈显示的 Spine Renderer。
- `spotlightSpineViews`：Spine 交互对象。
- `targetViews`：查找物对象。
- `spotlightMask`：光圈 SpriteMask。
- `spotlightVisual`：光圈视觉表现。
- `sfxSource`：音效源，可不填，运行时会补。
- `bgmSource`：BGM 音源，可不填，运行时会补。

`Map` 运行时会自动补齐子级下的 `SkeletonRenderer` 和 `SpotlightSpineView`，降低漏拖引用导致的风险。

### 图层与遮罩

推荐排序：

- 彩色底图 `ColorMap_shangse`：Sorting Order 0。
- Spine：Sorting Order 2。
- 查找物：Sorting Order 4。
- 线稿图 `LineArt_xiangao`：盖在上方，并设置 `VisibleOutsideMask`。

运行时遮罩规则：

- 线稿：`SpriteMaskInteraction.VisibleOutsideMask`。
- 彩稿 / Spine / 查找物：`VisibleInsideMask`。

最终效果是：线稿覆盖在彩稿上，只有光圈内能看到彩色底图和内容。

### Spotlight Visual

`SpotlightVisual` 是光圈的可视化柔光对象，和 `SpotlightMask` 同步位置、缩放和开关。

- `SpotlightMask` 负责真正的 SpriteMask 裁切。
- `SpotlightVisual` 负责玩家看到的光圈边缘和亮度提示。

### UIMainPanel

`UIMainPanel` 已摆放在场景 Canvas 下，序列化字段主要包括：

- 返回 / 重置 / 光圈 / other / 收起按钮。
- 光圈开关图标：`spotlightOn`、`spotlightOff`。
- 找物栏根节点 `findViewRect`。
- 找物栏滚动组件 `levelItemsScrollView`。
- 边缘提示文本 `edgeHintText`。
- 完成弹窗 `completePopup`。
- 完成弹窗文本 `completeTipText`。
- 完成弹窗下一关按钮 `completeNextBtn`。
- 完成弹窗重新开始按钮 `completeRestartBtn`。
- UI 点击音效 `buttonClickClip`。

完成弹窗行为：

- 全部目标找到后显示。
- “重新开始”会重置当前关。
- “下一关”当前按作业要求返回初始页，等同返回按钮。

## 交互流程

### 进入游戏

```text
UILoginPanel.StartClicked
-> GameManager.ShowGamePageAsync
-> UIManager.Open<UIMainPanel>
-> Map.SetMainPanel
-> Map.SetRunning(true)
-> PlayLevelBgm
-> ResetGame
```

### 光圈照明

```text
Map.Tick
-> UpdateSpotlightRadius
-> HandleInput
-> MapCameraRig.Tick
-> UpdateTargetsLighting
```

每帧根据光圈位置计算：

- Spine 是否被照到。
- 查找物是否被照到。

Spine 只在状态切换时播放/停止，避免每帧清空轨道造成掉帧。

### 点击目标

```text
TryClickTarget
-> 命中 SpotlightSpineView：播放 Spine 交互
-> 命中 HiddenObjectTargetView：
   -> 判断是否属于当前 LevelData.TargetItemIds
   -> 不是目标：错误反馈
   -> 是目标：TryInteract + 播放 interactClip
   -> PrepareItemFound
   -> UI item 排序到末尾并滚动到最右
   -> 等待一帧 UI 布局稳定
   -> 飞向对应 UI item
   -> MarkCompleted
```

### 重置

```text
UIMainPanel.ResetClicked
-> GameManager.HandleResetClicked
-> Map.ResetGame
```

重置内容：

- 中断当前飞行动画。
- 地图和光圈回到初始位置。
- Spine 状态复位并停止。
- 所有目标物恢复。
- 找物栏 found 状态清空。
- 完成弹窗隐藏。

BGM 不会在重置时停止，会在游戏过程中持续播放。

## 调参点

### 光圈

- `spotlightRadiusScreenRatio`
  - 光圈半径相对于屏幕短边比例。
  - 默认 `0.25`。

- `spotlightLeakRatio`
  - 光圈靠边时允许漏出屏幕的比例。

- `spotlightEnabledByDefault`
  - 进入游戏后是否默认开启光圈。

### 地图拖拽与回弹

- `mapDragSpeed`
  - 地图拖拽速度。

- `spotlightDragSpeed`
  - 光圈拖拽速度。

- `edgeFollowSpeed`
  - 光圈靠近屏幕边缘时，地图跟随速度。

- `edgeFollowPadding`
  - 光圈触发边缘跟随的屏幕范围。

- `reboundTime`
  - 越界回弹时长。

- `reboundDamping`
  - 越界回弹阻尼。

### 缩放

- `initialViewHeightRatio`
  - 初始一屏显示高度占地图高度比例。

- `defaultZoom`
  - 默认缩放档位。

- `minZoom`
  - 最小拉远倍率，作业要求约 `0.7`。

- `maxZoom`
  - 最大推进倍率，作业要求约 `2`。

- `pinchZoomSpeed`
  - 双指缩放速度。

- `wheelZoomSpeed`
  - Editor 鼠标滚轮缩放速度。

### Spine

- `idleAnimationName`
  - 默认待机动画名。

- `interactAnimationName`
  - 默认交互动画名。

- `SpotlightSpineView.idleAnimationName`
  - 单个 Spine 对象自己的 idle 动画名，Inspector 中可下拉选择。

- `SpotlightSpineView.interactAnimationName`
  - 单个 Spine 对象自己的 interact 动画名。

- `SpotlightSpineView.interactClip`
  - 单个 Spine 对象点击音效。

### 查找物反馈

- `HiddenObjectTargetView.interactClip`
  - 正确点击该目标物时播放的音效。

- `defaultWrongClip`
  - 点击非本关目标物时播放的错误音效。

- `wrongShakeDistance`
  - 错误点击屏幕抖动强度。

- `wrongShakeDuration`
  - 错误点击屏幕抖动时长。

- `collectFlyDuration`
  - 目标物飞向找物栏时长。

- `collectPopScale`
  - 飞行开始时目标物放大比例。

### UI

- `UIMainPanel.buttonClickClip`
  - UI 按钮点击音效。

- `collapseDuration`
  - 找物栏收起/展开动画时长。

- `collapseDirectionY`
  - 找物栏收起方向。

- `collapseExtraOffset`
  - 收起额外偏移量。

- `edgeHintMessage`
  - 边缘提示文案。

- `completeTipMessage`
  - 完成弹窗提示文案。

## 第三方库

### UniTask

用途：

- UI 切页异步流程。
- 登录页动画流程。
- 找物收集飞行动画。
- Spine 交互动画等待。
- 重置时中断当前玩法异步任务。

原因：

- 替代 Coroutine，减少 `StartCoroutine/yield return` 分散在多个对象中的维护成本。
- 方便用 CancellationToken 处理重置、离开页面时的中断。

### LitMotion

用途：

- `UILoginPanel` Logo / Title / Eye 动画。
- `UIMainPanel` 找物栏收起/展开。
- 边缘提示淡入淡出。

原因：

- 写法轻量，适合 UI 动效。
- 和 UniTask 扩展结合后，动画等待和取消更清晰。

### Spine

用途：

- 地图中的 Spine 内容物。
- 光圈进入播放 idle。
- 点击播放 interact。
- 离开光圈停止动画。

原因：

- 目标作业要求验证 Spine 接入和动画状态控制。

## 当前已实现需求对照

- 初始页：已实现。
- 开始游戏按钮：已实现。
- 游戏页返回、重置、光圈开关：已实现。
- 找物栏滚动、收回/展开、完成打勾：已实现。
- 光圈照明：已实现，线稿在外，彩稿和内容在内。
- 地图拖拽：已实现。
- 光圈拖拽：已实现。
- 双指缩放 / Editor 滚轮缩放：已实现。
- 边缘限制和回弹：已实现。
- 边缘提示：已实现。
- 光圈靠边地图跟随：已实现。
- Spine idle / interact：已实现。
- 点击期间防重复：已实现。
- 正确目标飞到找物栏：已实现。
- UI item 移动到末尾并滚动到最右：已实现。
- 非目标点击错误反馈：已实现。
- 背景音乐循环：已实现。
- 震动反馈：正确目标调用 `Handheld.Vibrate()`。
- 重置：已实现。
- 完成弹窗：已实现。

## 已知注意事项

- `HiddenObjectTargetView.Config.displayName` 需要和 `LevelData.Items.ItemName` 对应，才能解析到正确 itemId。
- `LevelData.TargetItemIds` 才是本关真正目标，`Items` 可以包含干扰项。
- `buttonClickClip`、`interactClip`、BGM 未赋值时不会播放对应音效。
- 完成弹窗需要在 `UIMainPanel` Inspector 中拖入弹窗对象、文本和两个按钮。
- 如果新增 Spine 对象，建议挂 `SpotlightSpineView` 并设置动画名；也可以让 `Map` 自动补组件，但手动配置更稳。

