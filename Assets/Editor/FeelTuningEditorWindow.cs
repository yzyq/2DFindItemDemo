#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZYQ.Demo;

public class FeelTuningEditorWindow : EditorWindow
{
    private const string DefaultAssetPath = "Assets/GameData/HiddenObjectCommonConfig.asset";

    private HiddenObjectCommonConfigAsset configAsset;
    private SerializedObject serializedConfig;
    private Vector2 scroll;

    [MenuItem("Tools/ZYQ/Feel Tuning Editor")]
    public static void Open()
    {
        var window = GetWindow<FeelTuningEditorWindow>();
        window.titleContent = new GUIContent("手感调参");
        window.minSize = new Vector2(720f, 760f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDefaultAsset();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (configAsset == null)
        {
            EditorGUILayout.HelpBox("请选择或创建 HiddenObjectCommonConfigAsset。该窗口只编辑通用手感 SO，不修改业务逻辑。", MessageType.Info);
            return;
        }

        serializedConfig ??= new SerializedObject(configAsset);
        serializedConfig.Update();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawIntro();
        DrawSpotlightSection();
        DrawMapFeelSection();
        DrawZoomSection();
        DrawSpineSection();
        DrawFeedbackSection();

        EditorGUILayout.Space(12f);
        DrawPresetButtons();

        EditorGUILayout.EndScrollView();

        if (serializedConfig.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(configAsset);
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("加载默认SO", EditorStyles.toolbarButton, GUILayout.Width(90f)))
        {
            LoadDefaultAsset();
        }

        if (GUILayout.Button("创建默认SO", EditorStyles.toolbarButton, GUILayout.Width(95f)))
        {
            CreateDefaultAsset();
        }

        if (GUILayout.Button("定位SO", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            if (configAsset != null)
                EditorGUIUtility.PingObject(configAsset);
        }

        GUILayout.FlexibleSpace();

        var selected = (HiddenObjectCommonConfigAsset)EditorGUILayout.ObjectField(
            configAsset,
            typeof(HiddenObjectCommonConfigAsset),
            false,
            GUILayout.Width(260f));

        if (selected != configAsset)
        {
            configAsset = selected;
            serializedConfig = configAsset != null ? new SerializedObject(configAsset) : null;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawIntro()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "本窗口集中编辑 HiddenObjectCommonConfigAsset 中影响手感的参数。" +
            "修改后会写入 SO，运行时由 GameEntry -> DataManager -> Map / MapCameraRig 使用。",
            MessageType.None);
    }

    private void DrawSpotlightSection()
    {
        DrawSectionTitle("光圈手感");
        DrawProperty("spotlightRadiusScreenRatio", "光圈半径 / 屏幕短边比例", "控制光圈大小。推荐 0.24-0.28，移动端较容易观察目标。");
        DrawProperty("spotlightLeakRatio", "光圈靠边漏出比例", "控制光圈靠屏幕边缘时允许露出屏幕外的比例。推荐 0.28-0.38。");
        DrawProperty("spotlightEnabledByDefault", "进入游戏默认开启光圈", "开启后进入游戏即显示光圈，符合当前玩法预期。");
    }

    private void DrawMapFeelSection()
    {
        DrawSectionTitle("地图 / 光圈拖拽");
        DrawProperty("mapDragSpeed", "地图拖拽速度", "单指拖动地图时的位移倍率。推荐 0.9-1.15，过大会飘。");
        DrawProperty("spotlightDragSpeed", "光圈拖拽速度", "拖动光圈时的位移倍率。推荐 0.95-1.1，保持跟手。");
        DrawProperty("edgeFollowSpeed", "光圈靠边时地图跟随速度", "光圈接近屏幕边缘后地图跟随移动速度。推荐 4.5-6。");
        DrawProperty("edgeFollowPadding", "边缘跟随触发范围", "距离屏幕边缘多少像素开始推动地图。推荐 72-96。");
        DrawProperty("reboundTime", "边缘回弹时间", "地图越界后回到合法范围的时间。推荐 0.18-0.24。");
        DrawProperty("reboundDamping", "边缘回弹阻尼", "回弹阻尼强度。推荐 10-14，越大越干脆。");
    }

    private void DrawZoomSection()
    {
        DrawSectionTitle("缩放手感");
        DrawProperty("initialViewHeightRatio", "初始一屏显示比例", "初始相机视野占地图高度比例。推荐 0.62-0.68。");
        DrawProperty("defaultZoom", "默认缩放档位", "进入游戏时的基准缩放。通常保持 1。");
        DrawProperty("minZoom", "最小缩放 / 拉远", "作业要求相对初始最小拉远到 0.7。");
        DrawProperty("maxZoom", "最大缩放 / 推近", "作业要求相对初始最大推进到 2。");
        DrawProperty("pinchZoomSpeed", "双指缩放速度", "移动端双指缩放灵敏度。推荐 0.0045-0.006。");
        DrawProperty("wheelZoomSpeed", "Editor 滚轮缩放速度", "仅用于编辑器/鼠标测试。推荐 0.06-0.1。");
    }

    private void DrawSpineSection()
    {
        DrawSectionTitle("Spine 触发");
        DrawProperty("idleReplayInterval", "待机动画重播间隔", "光圈移走再照到时允许重播 idle 的最小间隔。推荐 0.15-0.25。");
    }

    private void DrawFeedbackSection()
    {
        DrawSectionTitle("反馈动画");
        DrawProperty("wrongShakeDistance", "错误点击屏幕抖动强度", "点击非本关目标物时的屏幕抖动像素强度。推荐 8-12。");
        DrawProperty("wrongShakeDuration", "错误点击屏幕抖动时长", "错误反馈持续时间。推荐 0.14-0.2。");
        DrawProperty("collectFlyDuration", "目标物飞行时长", "正确点击后飞到找物栏的时间。推荐 0.5-0.65。");
        DrawProperty("collectPopScale", "飞行起始放大比例", "飞行开始时目标物略微放大。推荐 1.15-1.25。");
    }

    private void DrawPresetButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("应用推荐默认手感", GUILayout.Height(34f)))
        {
            ApplyRecommendedPreset();
        }

        if (GUILayout.Button("保存资源", GUILayout.Height(34f), GUILayout.Width(120f)))
        {
            SaveAsset();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSectionTitle(string title)
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2f);
    }

    private void DrawProperty(string propertyName, string displayName, string usage)
    {
        SerializedProperty property = serializedConfig.FindProperty(propertyName);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"未找到参数：{propertyName}", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(property, new GUIContent(displayName));
        EditorGUILayout.LabelField("用途", usage, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void LoadDefaultAsset()
    {
        configAsset = AssetDatabase.LoadAssetAtPath<HiddenObjectCommonConfigAsset>(DefaultAssetPath);
        serializedConfig = configAsset != null ? new SerializedObject(configAsset) : null;
    }

    private void CreateDefaultAsset()
    {
        configAsset = AssetDatabase.LoadAssetAtPath<HiddenObjectCommonConfigAsset>(DefaultAssetPath);
        if (configAsset == null)
        {
            configAsset = CreateInstance<HiddenObjectCommonConfigAsset>();
            ApplyRecommendedValues(configAsset);
            AssetDatabase.CreateAsset(configAsset, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        serializedConfig = new SerializedObject(configAsset);
        EditorGUIUtility.PingObject(configAsset);
    }

    private void ApplyRecommendedPreset()
    {
        if (configAsset == null)
            return;

        Undo.RecordObject(configAsset, "Apply Recommended Feel Tuning");
        ApplyRecommendedValues(configAsset);
        EditorUtility.SetDirty(configAsset);
        serializedConfig = new SerializedObject(configAsset);
        SaveAsset();
    }

    private static void ApplyRecommendedValues(HiddenObjectCommonConfigAsset config)
    {
        config.spotlightRadiusScreenRatio = 0.25f;
        config.spotlightLeakRatio = 0.35f;
        config.spotlightEnabledByDefault = true;

        config.mapDragSpeed = 1f;
        config.spotlightDragSpeed = 1f;
        config.edgeFollowSpeed = 5.2f;
        config.edgeFollowPadding = 84f;
        config.reboundTime = 0.2f;
        config.reboundDamping = 12f;

        config.initialViewHeightRatio = 0.65f;
        config.defaultZoom = 1f;
        config.minZoom = 0.7f;
        config.maxZoom = 2f;
        config.wheelZoomSpeed = 0.08f;
        config.pinchZoomSpeed = 0.005f;

        config.idleReplayInterval = 0.2f;

        config.wrongShakeDistance = 10f;
        config.wrongShakeDuration = 0.16f;
        config.collectFlyDuration = 0.58f;
        config.collectPopScale = 1.18f;
    }

    private void SaveAsset()
    {
        if (configAsset == null)
            return;

        EditorUtility.SetDirty(configAsset);
        AssetDatabase.SaveAssets();
    }
}
#endif
