# 2D Hidden Object Spotlight Demo

## 运行方式

打开 `Assets/Scenes/SampleScene.unity`，确保场景中有挂载 `GameEntry` 的对象，然后直接 Play。

当前 Demo 会在 Unity Editor 下自动加载 `Assets/ZYQ` 里的现有资源：

- 初始页 Logo：`Assets/ZYQ/Sprites/公司logo/logo2.png`
- 地图线稿：`Assets/ZYQ/Sprites/大图/xiangao.png`
- 地图彩稿：`Assets/ZYQ/Sprites/大图/shangse.png`
- 云彩填充：`Assets/ZYQ/Sprites/大图/yun.png`
- 找物栏资源：`Assets/ZYQ/Sprites/找物栏背景`
- 目标物：`Assets/ZYQ/Sprites/目标物`
- Spine：`Assets/ZYQ/Spine`

## 模块结构

- `GameEntry`：程序入口，注册并初始化所有 manager。
- `AppContext`：manager 服务定位与统一生命周期。
- `GameManager`：创建并驱动单屏找物 Demo。
- `HiddenObjectDemoController`：流程页、地图、光圈、目标点击、飞行动画、重置。
- `UIMainPanel`：场景中已摆放的游戏页 UI，负责按钮事件、找物栏列表、收起/展开。
- `MapCameraRig`：地图拖拽、缩放、边缘限制、回弹、光圈靠边跟随。
- `HiddenObjectTargetView`：目标物点击、光圈照亮状态、Spine 待机/交互动画。
- `LevelItemsScrollView`：找物栏单项虚拟滚动、完成态刷新。

## 调参点

推荐使用 SO 管配置：

- `GameEntry.commonConfigAsset`：拖入通用配置 SO。
- `GameEntry.levelConfigAssets`：拖入一个或多个关卡配置 SO。
- `GameEntry.levelDataSO`：拖入找物栏列表数据 SO，`LevelData.Items` 用于底部找物栏，`background/foreground` 用于动态图层加载。

旧的 `GameEntry.demoConfig` 仍保留为兜底配置。通用 SO 可调：

- `spotlightRadiusScreenRatio`：光圈半径，默认 `0.25`，表示屏幕短边的 25%。
- `spotlightLeakRatio`：光圈靠边时允许漏出的比例。
- `mapDragSpeed`：地图拖动速度。
- `spotlightDragSpeed`：光圈拖动速度。
- `edgeFollowSpeed`：光圈靠近屏幕边缘时地图跟随速度。
- `edgeFollowPadding`：触发地图跟随的屏幕边缘范围。
- `reboundTime` / `reboundDamping`：地图越界回弹时间与阻尼。
- `minZoom` / `maxZoom` / `defaultZoom`：双指或滚轮缩放范围。
- `pinchZoomSpeed` / `wheelZoomSpeed`：触控与鼠标滚轮缩放手感。
- `idleReplayInterval`：光圈重新照到目标后，待机动画允许重播的最小间隔。
- `collectFlyDuration` / `collectPopScale`：找到目标后的飞行动画时长和起始放大比例。
- `backgroundClip` / `defaultCorrectClip` / `defaultWrongClip`：背景循环音与默认反馈音效；不填时会生成简单提示音。

关卡 SO 可调：

- `companyLogo` / `lineArtMap` / `colorMap` / `cloudFill`：关卡美术资源。
- `findBarBackground` / `findItemFrame` / `checkMark`：找物栏 UI 资源。
- `targets`：目标物列表、图标、地图位置、Spine 数据和动画名。

`LevelDataSO` 可调：

- `background`：彩色底图，运行时注入到 `HiddenObjectDemoConfig.colorMap`。
- `foreground`：线稿前景图，运行时注入到 `HiddenObjectDemoConfig.lineArtMap`。
- `items`：找物栏展示用数据，`UIMainPanel` 直接读取 `LevelData.Items`。
- `targetItemIds`：目标顺序映射；玩法目标下标会映射到对应 `itemId` 并打勾。

## 动态 UI 与关卡资源

`GameEntry` 现在支持两种配置方式：

- 推荐：`commonConfigAsset + levelConfigAssets`
- 兼容：直接填 `gameData`

`Common Config SO` 里有：

- `uiPanels`：可配置 UI prefab。当前游戏页 `UIMainPanel` 已摆在场景 Canvas 下，`UIManager` 会优先复用场景面板，不会重复创建。

`Level Config SO` 里有：

- `levelId`：关卡 id，例如 `level_001`
- `displayName`：展示名称
- 该关卡的目标列表、Spine、音效；地图大图优先来自 `LevelDataSO.background/foreground`

运行时入口：

- `DataManager.LoadLevel("level_002")`：切换当前关卡数据。
- `DataManager.DemoConfig`：获取当前关卡的找物配置。
- `UIManager.Open<UILoadingPanel>(UIPanelType.Loading)`：动态打开 UI prefab。
- `UIManager.Open<UIMainPanel>(UIPanelType.Game)`：打开场景中已摆放的游戏页 UI。

UI 约定：场景中已摆放的 UI 根节点挂对应脚本并继承 `UIPanel`，`Type` 设置为对应面板类型。`UIManager` 初始化时会注册场景面板，打开时优先复用。

## 已实现玩法

- 初始页与游戏页切换。
- 返回、重置、光圈开关。
- 底部找物栏读取 `LevelDataSO.Items`，支持横向滑动、完成态打勾、收回/展开。
- 地图拖拽、光圈拖拽、双指/滚轮缩放、边缘回弹。
- 线稿常显，彩稿只在光圈内显示。
- 光圈照到目标时显示目标并播放待机动画。
- 点击目标时播放交互逻辑、震动、飞到找物栏对应项、打勾。
- 点击空白或未照亮目标时播放错误音效。
- 重置后地图、光圈、目标、找物栏全部复位。

## 第三方库使用

- UniTask：用于流程切页、点击交互、防止动画期间重复触发、收集飞行动画异步流程和重置中断。
- LitMotion：用于 UI 面板动画，例如登录页入场、眼睛眨动、找物栏收起/展开。

## 后续接入建议

- 为每个目标补全真实 Spine 动画名和音效。
- 如果需要打包运行，把当前 Editor 自动加载资源改成 `Resources`、Addressables 或 prefab 引用。
