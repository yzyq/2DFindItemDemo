#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZYQ.Demo;

public class LevelEditorWindow : EditorWindow
{
    private const string DatabasePath = "Assets/GameData/LevelDataSO.asset";

    private const float LevelListWidth = 320f;

    // 右侧关卡详情整体固定宽度
    private const float DetailRootWidth = 850f;

    // 右侧关卡详情内部实际可用宽度，用于避免横向滚动条
    private const float DetailInnerWidth = 820f;

    // 关卡详情内部左下物品列表宽度
    private const float ItemListWidth = 200f;

    // 物品详情 + 指定目标区域宽度
    private const float ItemDetailWidth = 580f;

    private const float BottomAreaHeight = 460f;

    private LevelDataSO database;
    private SerializedObject serializedDatabase;
    private SerializedProperty levelsProperty;

    private int selectedLevelIndex = -1;
    private int selectedItemIndex = -1;

    private Vector2 levelListScroll;
    private Vector2 detailScroll;
    private Vector2 itemListScroll;
    private Vector2 targetListScroll;

    private string searchKeyword = string.Empty;

    [MenuItem("Tools/ZYQ/Level Editor")]
    public static void Open()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>();
        window.titleContent = new GUIContent("关卡编辑器");
        window.minSize = new Vector2(1220, 680);
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateDatabase();
    }

    private void OnGUI()
    {
        DrawTopToolbar();

        if (database == null || serializedDatabase == null || levelsProperty == null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("LevelDatabase 加载失败。", MessageType.Error);

            if (GUILayout.Button("重新加载 / 创建数据库", GUILayout.Height(32)))
            {
                LoadOrCreateDatabase();
            }

            return;
        }

        serializedDatabase.Update();

        GUILayout.BeginHorizontal();

        DrawLeftLevelList();

        GUILayout.Space(6);

        DrawLevelDetailRoot();

        GUILayout.EndHorizontal();

        serializedDatabase.ApplyModifiedProperties();
    }

    private void DrawTopToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("加载/创建数据库", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            LoadOrCreateDatabase();
        }

        if (GUILayout.Button("定位SO", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            PingDatabase();
        }

        if (GUILayout.Button("刷新ID/排序", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            NormalizeLevelIdsAndOrders();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Label("搜索", GUILayout.Width(35));

        searchKeyword = GUILayout.TextField(
            searchKeyword,
            EditorStyles.toolbarSearchField,
            GUILayout.Width(220));

        GUILayout.EndHorizontal();
    }

    private void DrawLeftLevelList()
    {
        GUILayout.BeginVertical(
            "box",
            GUILayout.Width(LevelListWidth),
            GUILayout.ExpandHeight(true));

        GUILayout.BeginHorizontal();

        GUILayout.Label("关卡列表", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ 添加", GUILayout.Width(70), GUILayout.Height(26)))
        {
            AddLevel();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        levelListScroll = GUILayout.BeginScrollView(
            levelListScroll,
            GUILayout.ExpandHeight(true));

        for (int i = 0; i < levelsProperty.arraySize; i++)
        {
            SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(i);

            int levelId = levelProperty.FindPropertyRelative("levelId").intValue;
            int sortOrder = levelProperty.FindPropertyRelative("sortOrder").intValue;
            string levelName = levelProperty.FindPropertyRelative("levelName").stringValue;

            if (!IsMatchSearch(levelId, levelName))
            {
                continue;
            }

            DrawLevelListItem(i, levelId, sortOrder, levelName);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4);

        GUILayout.Label($"关卡数量：{levelsProperty.arraySize}", EditorStyles.miniLabel);

        GUILayout.EndVertical();
    }

    private void DrawLevelListItem(int index, int levelId, int sortOrder, string levelName)
    {
        bool selected = selectedLevelIndex == index;

        string displayName = string.IsNullOrEmpty(levelName) ? "未命名关卡" : levelName;
        string label = $"[{levelId}] {displayName}";

        GUILayout.BeginHorizontal();

        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = selected ? FontStyle.Bold : FontStyle.Normal
        };

        Color oldColor = GUI.backgroundColor;

        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
        }

        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(28)))
        {
            selectedLevelIndex = index;
            selectedItemIndex = -1;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = oldColor;

        if (GUILayout.Button("↑", GUILayout.Width(26), GUILayout.Height(28)))
        {
            MoveLevel(index, index - 1);
        }

        if (GUILayout.Button("↓", GUILayout.Width(26), GUILayout.Height(28)))
        {
            MoveLevel(index, index + 1);
        }

        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);

        if (GUILayout.Button("-", GUILayout.Width(34), GUILayout.Height(18)))
        {
            RemoveLevelAt(index);
        }

        GUI.backgroundColor = oldColor;

        GUILayout.EndHorizontal();
    }

    private void DrawLevelDetailRoot()
    {
        GUILayout.BeginVertical(
            "box",
            GUILayout.Width(DetailRootWidth),
            GUILayout.ExpandHeight(true));

        GUILayout.Label("关卡详情", EditorStyles.boldLabel);

        if (selectedLevelIndex < 0 || selectedLevelIndex >= levelsProperty.arraySize)
        {
            EditorGUILayout.HelpBox("请选择左侧关卡。", MessageType.Info);
            GUILayout.EndVertical();
            return;
        }

        SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(selectedLevelIndex);
        SerializedProperty itemsProperty = levelProperty.FindPropertyRelative("items");
        SerializedProperty targetItemIdsProperty = levelProperty.FindPropertyRelative("targetItemIds");

        // 关键：关闭横向滚动条，只允许纵向滚动
        detailScroll = GUILayout.BeginScrollView(
            detailScroll,
            false,
            true,
            GUILayout.Width(DetailInnerWidth),
            GUILayout.ExpandHeight(true));

        DrawLevelBaseInfo(levelProperty);

        GUILayout.Space(8);

        DrawLevelDetailBottom(itemsProperty, targetItemIdsProperty);

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
    }

    private void DrawLevelBaseInfo(SerializedProperty levelProperty)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(DetailInnerWidth - 20f));

        GUILayout.Label("关卡基础信息", EditorStyles.boldLabel);

        SerializedProperty levelIdProperty = levelProperty.FindPropertyRelative("levelId");
        SerializedProperty sortOrderProperty = levelProperty.FindPropertyRelative("sortOrder");
        SerializedProperty levelNameProperty = levelProperty.FindPropertyRelative("levelName");
        SerializedProperty levelIconProperty = levelProperty.FindPropertyRelative("levelIcon");

        SerializedProperty levelbackgroundProperty = levelProperty.FindPropertyRelative("background");
        SerializedProperty levelforegroundProperty = levelProperty.FindPropertyRelative("foreground");
        SerializedProperty levelbgmProperty = levelProperty.FindPropertyRelative("bgm");
        SerializedProperty descriptionProperty = levelProperty.FindPropertyRelative("description");

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(levelIdProperty, new GUIContent("关卡ID"));
        EditorGUILayout.PropertyField(sortOrderProperty, new GUIContent("排序"));
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(levelNameProperty, new GUIContent("关卡名称"));
        DrawIconFieldWithPreview("关卡 Icon", levelIconProperty);
        DrawIconFieldWithPreview("关卡背景", levelbackgroundProperty);
        DrawIconFieldWithPreview("关卡前景", levelforegroundProperty);
        DrawIconFieldWithPreview("关卡背景音乐", levelbgmProperty);



        GUILayout.Label("关卡描述");
        descriptionProperty.stringValue = EditorGUILayout.TextArea(
            descriptionProperty.stringValue,
            GUILayout.MinHeight(50));

        if (EditorGUI.EndChangeCheck())
        {
            serializedDatabase.ApplyModifiedProperties();
            SaveDatabase();
        }

        GUILayout.EndVertical();
    }

    private void DrawLevelDetailBottom(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        GUILayout.BeginHorizontal(GUILayout.Width(DetailInnerWidth - 20f));

        DrawLevelItemListInDetail(itemsProperty, targetItemIdsProperty);

        GUILayout.Space(8);

        DrawLevelTargetAndItemDetail(itemsProperty, targetItemIdsProperty);

        GUILayout.EndHorizontal();
    }

    private void DrawLevelItemListInDetail(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        GUILayout.BeginVertical(
            "box",
            GUILayout.Width(ItemListWidth),
            GUILayout.Height(BottomAreaHeight));

        GUILayout.BeginHorizontal();

        GUILayout.Label("物品列表", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+", GUILayout.Width(28), GUILayout.Height(24)))
        {
            AddItem(itemsProperty);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        itemListScroll = GUILayout.BeginScrollView(
            itemListScroll,
            GUILayout.Height(BottomAreaHeight - 60f));

        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);

            int itemId = itemProperty.FindPropertyRelative("itemId").intValue;
            string itemName = itemProperty.FindPropertyRelative("itemName").stringValue;

            DrawItemListElement(itemsProperty, targetItemIdsProperty, i, itemId, itemName);
        }

        GUILayout.EndScrollView();

        GUILayout.Label($"数量：{itemsProperty.arraySize}", EditorStyles.miniLabel);

        GUILayout.EndVertical();
    }

    private void DrawItemListElement(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty,
        int index,
        int itemId,
        string itemName)
    {
        bool selected = selectedItemIndex == index;

        string displayName = string.IsNullOrEmpty(itemName) ? "未命名" : itemName;
        string targetMark = IsItemUsedAsTarget(targetItemIdsProperty, itemId) ? "★ " : string.Empty;
        string label = $"{targetMark}[{itemId}] {displayName}";

        GUILayout.BeginHorizontal();

        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = selected ? FontStyle.Bold : FontStyle.Normal
        };

        Color oldColor = GUI.backgroundColor;

        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
        }

        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(26)))
        {
            selectedItemIndex = index;
            GUI.FocusControl(null);
        }

        GUI.backgroundColor = oldColor;
        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);

        if (GUILayout.Button("×", GUILayout.Width(26), GUILayout.Height(26)))
        {
            RemoveItemAt(itemsProperty, targetItemIdsProperty, index);
        }

        GUI.backgroundColor = oldColor;

        GUILayout.EndHorizontal();
    }

    private void DrawLevelTargetAndItemDetail(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        GUILayout.BeginVertical(
            "box",
            GUILayout.Width(ItemDetailWidth),
            GUILayout.Height(BottomAreaHeight));

        DrawSelectedItemDetail(itemsProperty, targetItemIdsProperty);

        GUILayout.Space(8);

        DrawTargetItemList(itemsProperty, targetItemIdsProperty);

        GUILayout.EndVertical();
    }

    private void DrawSelectedItemDetail(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(ItemDetailWidth - 25f));

        GUILayout.Label("物品详情", EditorStyles.boldLabel);

        if (selectedItemIndex < 0 || selectedItemIndex >= itemsProperty.arraySize)
        {
            EditorGUILayout.HelpBox("请选择左侧物品列表中的一个物品。", MessageType.Info);
            GUILayout.EndVertical();
            return;
        }

        SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(selectedItemIndex);

        SerializedProperty itemIdProperty = itemProperty.FindPropertyRelative("itemId");
        SerializedProperty itemNameProperty = itemProperty.FindPropertyRelative("itemName");
        SerializedProperty uiIconProperty = itemProperty.FindPropertyRelative("uiIcon");
        SerializedProperty sceneIconProperty = itemProperty.FindPropertyRelative("sceneIcon");
        SerializedProperty prefabProperty = itemProperty.FindPropertyRelative("prefab");
        SerializedProperty descriptionProperty = itemProperty.FindPropertyRelative("description");

        GUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(itemIdProperty, new GUIContent("物品ID"));
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        bool isTarget = IsItemUsedAsTarget(targetItemIdsProperty, itemIdProperty.intValue);

        if (isTarget)
        {
            GUILayout.Label("已加入需要查找列表", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("移出目标", GUILayout.Width(80), GUILayout.Height(22)))
            {
                RemoveTargetReferences(targetItemIdsProperty, itemIdProperty.intValue);
                serializedDatabase.ApplyModifiedProperties();
                SaveDatabase();
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            if (GUILayout.Button("设为需要查找", GUILayout.Width(120), GUILayout.Height(22)))
            {
                AddTargetItemById(targetItemIdsProperty, itemIdProperty.intValue);
            }
        }

        GUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(itemNameProperty, new GUIContent("物品名称"));

        DrawIconFieldWithPreview("UI Icon", uiIconProperty);
        DrawIconFieldWithPreview("场景 Icon", sceneIconProperty);

        EditorGUILayout.PropertyField(prefabProperty, new GUIContent("Prefab"));

        GUILayout.Label("描述");
        descriptionProperty.stringValue = EditorGUILayout.TextArea(
            descriptionProperty.stringValue,
            GUILayout.MinHeight(60));

        if (EditorGUI.EndChangeCheck())
        {
            serializedDatabase.ApplyModifiedProperties();
            SaveDatabase();
        }

        GUILayout.EndVertical();
    }

    private void DrawIconFieldWithPreview(string label, SerializedProperty iconProperty)
    {
        GUILayout.BeginHorizontal();

        DrawSpritePreview(label, iconProperty.objectReferenceValue as Sprite);

        GUILayout.BeginVertical();

        EditorGUILayout.PropertyField(iconProperty, new GUIContent(label));

        Sprite sprite = iconProperty.objectReferenceValue as Sprite;

        if (sprite != null)
        {
            GUILayout.Label(sprite.name, EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("未设置", EditorStyles.miniLabel);
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(4);
    }

    private void DrawTargetItemList(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(ItemDetailWidth - 25f));

        GUILayout.BeginHorizontal();

        GUILayout.Label("指定需要找到的物品", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ 添加目标", GUILayout.Width(90), GUILayout.Height(26)))
        {
            AddTargetItem(itemsProperty, targetItemIdsProperty);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        targetListScroll = GUILayout.BeginScrollView(targetListScroll, GUILayout.Height(155));

        if (itemsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前关卡没有物品，请先添加关卡物品。", MessageType.Info);
        }
        else if (targetItemIdsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前关卡还没有需要查找的物品。", MessageType.Info);
        }

        for (int i = 0; i < targetItemIdsProperty.arraySize; i++)
        {
            DrawTargetItem(itemsProperty, targetItemIdsProperty, i);
        }

        GUILayout.EndScrollView();

        GUILayout.Label($"目标数量：{targetItemIdsProperty.arraySize}", EditorStyles.miniLabel);

        GUILayout.EndVertical();
    }

    private void DrawTargetItem(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty,
        int index)
    {
        SerializedProperty targetIdProperty = targetItemIdsProperty.GetArrayElementAtIndex(index);

        BuildItemPopupOptions(itemsProperty, out string[] optionLabels, out int[] optionIds);

        GUILayout.BeginVertical("box");

        if (optionIds.Length == 0)
        {
            EditorGUILayout.HelpBox("无可选物品。", MessageType.Warning);
            GUILayout.EndVertical();
            return;
        }

        int currentId = targetIdProperty.intValue;
        int currentPopupIndex = GetPopupIndex(optionIds, currentId);

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginHorizontal();

        GUILayout.Label("选择物品", GUILayout.Width(60));

        int newPopupIndex = EditorGUILayout.Popup(
            currentPopupIndex,
            optionLabels,
            GUILayout.Width(320));

        GUILayout.FlexibleSpace();

        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);

        if (GUILayout.Button("-", GUILayout.Width(40), GUILayout.Height(18)))
        {
            RemoveTargetAt(targetItemIdsProperty, index);
        }

        GUI.backgroundColor = oldColor;

        if (EditorGUI.EndChangeCheck())
        {
            int selectedItemId = optionIds[newPopupIndex];

            if (IsItemUsedAsTargetExceptSelf(targetItemIdsProperty, selectedItemId, index))
            {
                EditorUtility.DisplayDialog("重复目标", "该物品已经在需要查找列表中。", "确定");
            }
            else
            {
                targetIdProperty.intValue = selectedItemId;
                serializedDatabase.ApplyModifiedProperties();
                SaveDatabase();
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawTargetPreview(SerializedProperty itemsProperty, int itemId)
    {
        SerializedProperty itemProperty = FindItemPropertyById(itemsProperty, itemId);

        if (itemProperty == null)
        {
            EditorGUILayout.HelpBox($"目标物品ID {itemId} 不存在于当前关卡物品列表中。", MessageType.Warning);
            return;
        }

        string itemName = itemProperty.FindPropertyRelative("itemName").stringValue;
        Sprite uiIcon = itemProperty.FindPropertyRelative("uiIcon").objectReferenceValue as Sprite;
        Sprite sceneIcon = itemProperty.FindPropertyRelative("sceneIcon").objectReferenceValue as Sprite;

        GUILayout.Label($"名称：{(string.IsNullOrEmpty(itemName) ? "未命名物品" : itemName)}");

        GUILayout.BeginHorizontal();

        DrawSpritePreview("UI Icon", uiIcon);
        DrawSpritePreview("Scene Icon", sceneIcon);

        GUILayout.EndHorizontal();
    }

    private void DrawSpritePreview(string title, Sprite sprite)
    {
        GUILayout.BeginVertical(GUILayout.Width(76));

        GUILayout.Label(title, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(70));

        Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

        if (sprite != null && sprite.texture != null)
        {
            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
            GUI.Label(rect, "None", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.EndVertical();
    }

    private void AddLevel()
    {
        serializedDatabase.Update();

        int index = levelsProperty.arraySize;

        levelsProperty.InsertArrayElementAtIndex(index);

        SerializedProperty newLevel = levelsProperty.GetArrayElementAtIndex(index);

        newLevel.FindPropertyRelative("levelId").intValue = index + 1;
        newLevel.FindPropertyRelative("sortOrder").intValue = index;
        newLevel.FindPropertyRelative("levelName").stringValue = $"关卡 {index + 1}";
        newLevel.FindPropertyRelative("levelIcon").objectReferenceValue = null;
        newLevel.FindPropertyRelative("description").stringValue = string.Empty;
        newLevel.FindPropertyRelative("items").ClearArray();
        newLevel.FindPropertyRelative("targetItemIds").ClearArray();

        selectedLevelIndex = index;
        selectedItemIndex = -1;

        serializedDatabase.ApplyModifiedProperties();

        NormalizeLevelIdsAndOrders();
        SaveDatabase();
    }

    private void RemoveLevelAt(int index)
    {
        if (index < 0 || index >= levelsProperty.arraySize) return;

        SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(index);

        int levelId = levelProperty.FindPropertyRelative("levelId").intValue;
        string levelName = levelProperty.FindPropertyRelative("levelName").stringValue;

        bool confirm = EditorUtility.DisplayDialog(
            "删除关卡",
            $"确定删除关卡 [{levelId}] {levelName} 吗？",
            "删除",
            "取消");

        if (!confirm) return;

        serializedDatabase.Update();

        levelsProperty.DeleteArrayElementAtIndex(index);

        if (selectedLevelIndex == index)
        {
            selectedLevelIndex = -1;
            selectedItemIndex = -1;
        }
        else if (selectedLevelIndex > index)
        {
            selectedLevelIndex--;
        }

        serializedDatabase.ApplyModifiedProperties();

        NormalizeLevelIdsAndOrders();
        SaveDatabase();

        GUIUtility.ExitGUI();
    }

    private void MoveLevel(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= levelsProperty.arraySize) return;
        if (toIndex < 0 || toIndex >= levelsProperty.arraySize) return;
        if (fromIndex == toIndex) return;

        serializedDatabase.Update();

        levelsProperty.MoveArrayElement(fromIndex, toIndex);

        selectedLevelIndex = toIndex;
        selectedItemIndex = -1;

        serializedDatabase.ApplyModifiedProperties();

        NormalizeLevelIdsAndOrders();
        SaveDatabase();

        GUIUtility.ExitGUI();
    }

    private void NormalizeLevelIdsAndOrders()
    {
        if (serializedDatabase == null || levelsProperty == null) return;

        serializedDatabase.Update();

        for (int i = 0; i < levelsProperty.arraySize; i++)
        {
            SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(i);

            levelProperty.FindPropertyRelative("levelId").intValue = i + 1;
            levelProperty.FindPropertyRelative("sortOrder").intValue = i;
        }

        serializedDatabase.ApplyModifiedProperties();
        SaveDatabase();
    }

    private void AddItem(SerializedProperty itemsProperty)
    {
        serializedDatabase.Update();

        int index = itemsProperty.arraySize;

        itemsProperty.InsertArrayElementAtIndex(index);

        SerializedProperty newItem = itemsProperty.GetArrayElementAtIndex(index);

        newItem.FindPropertyRelative("itemId").intValue = GenerateNextItemId(itemsProperty);
        newItem.FindPropertyRelative("itemName").stringValue = $"物品 {index + 1}";
        newItem.FindPropertyRelative("uiIcon").objectReferenceValue = null;
        newItem.FindPropertyRelative("sceneIcon").objectReferenceValue = null;
        newItem.FindPropertyRelative("prefab").objectReferenceValue = null;
        newItem.FindPropertyRelative("description").stringValue = string.Empty;

        selectedItemIndex = index;

        serializedDatabase.ApplyModifiedProperties();
        SaveDatabase();
    }

    private void RemoveItemAt(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty,
        int index)
    {
        if (index < 0 || index >= itemsProperty.arraySize) return;

        SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(index);

        int itemId = itemProperty.FindPropertyRelative("itemId").intValue;
        string itemName = itemProperty.FindPropertyRelative("itemName").stringValue;

        bool confirm = EditorUtility.DisplayDialog(
            "删除物品",
            $"确定删除物品 [{itemId}] {itemName} 吗？\n\n如果它在“指定需要找到的物品”中，也会同步移除。",
            "删除",
            "取消");

        if (!confirm) return;

        serializedDatabase.Update();

        RemoveTargetReferences(targetItemIdsProperty, itemId);

        itemsProperty.DeleteArrayElementAtIndex(index);

        if (selectedItemIndex == index)
        {
            selectedItemIndex = -1;
        }
        else if (selectedItemIndex > index)
        {
            selectedItemIndex--;
        }

        serializedDatabase.ApplyModifiedProperties();
        SaveDatabase();

        GUIUtility.ExitGUI();
    }

    private int GenerateNextItemId(SerializedProperty itemsProperty)
    {
        int maxId = 0;

        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);
            int id = itemProperty.FindPropertyRelative("itemId").intValue;

            if (id > maxId)
            {
                maxId = id;
            }
        }

        return maxId + 1;
    }

    private void AddTargetItem(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        if (itemsProperty.arraySize == 0)
        {
            EditorUtility.DisplayDialog("无法添加", "当前关卡没有物品，请先添加关卡物品。", "确定");
            return;
        }

        int availableItemId = FindFirstAvailableTargetItemId(itemsProperty, targetItemIdsProperty);

        if (availableItemId <= 0)
        {
            EditorUtility.DisplayDialog("无法添加", "所有物品都已经加入需要查找列表。", "确定");
            return;
        }

        AddTargetItemById(targetItemIdsProperty, availableItemId);
    }

    private void AddTargetItemById(SerializedProperty targetItemIdsProperty, int itemId)
    {
        if (IsItemUsedAsTarget(targetItemIdsProperty, itemId))
        {
            EditorUtility.DisplayDialog("重复目标", "该物品已经在需要查找列表中。", "确定");
            return;
        }

        serializedDatabase.Update();

        int index = targetItemIdsProperty.arraySize;

        targetItemIdsProperty.InsertArrayElementAtIndex(index);
        targetItemIdsProperty.GetArrayElementAtIndex(index).intValue = itemId;

        serializedDatabase.ApplyModifiedProperties();
        SaveDatabase();
    }

    private void RemoveTargetAt(SerializedProperty targetItemIdsProperty, int index)
    {
        if (index < 0 || index >= targetItemIdsProperty.arraySize) return;

        serializedDatabase.Update();

        targetItemIdsProperty.DeleteArrayElementAtIndex(index);

        serializedDatabase.ApplyModifiedProperties();
        SaveDatabase();

        GUIUtility.ExitGUI();
    }

    private bool IsItemUsedAsTarget(SerializedProperty targetItemIdsProperty, int itemId)
    {
        for (int i = 0; i < targetItemIdsProperty.arraySize; i++)
        {
            if (targetItemIdsProperty.GetArrayElementAtIndex(i).intValue == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsItemUsedAsTargetExceptSelf(
        SerializedProperty targetItemIdsProperty,
        int itemId,
        int selfIndex)
    {
        for (int i = 0; i < targetItemIdsProperty.arraySize; i++)
        {
            if (i == selfIndex) continue;

            if (targetItemIdsProperty.GetArrayElementAtIndex(i).intValue == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveTargetReferences(SerializedProperty targetItemIdsProperty, int itemId)
    {
        for (int i = targetItemIdsProperty.arraySize - 1; i >= 0; i--)
        {
            if (targetItemIdsProperty.GetArrayElementAtIndex(i).intValue == itemId)
            {
                targetItemIdsProperty.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private int FindFirstAvailableTargetItemId(
        SerializedProperty itemsProperty,
        SerializedProperty targetItemIdsProperty)
    {
        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);
            int itemId = itemProperty.FindPropertyRelative("itemId").intValue;

            if (!IsItemUsedAsTarget(targetItemIdsProperty, itemId))
            {
                return itemId;
            }
        }

        return -1;
    }

    private void BuildItemPopupOptions(
        SerializedProperty itemsProperty,
        out string[] labels,
        out int[] ids)
    {
        List<string> labelList = new List<string>();
        List<int> idList = new List<int>();

        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);

            int itemId = itemProperty.FindPropertyRelative("itemId").intValue;
            string itemName = itemProperty.FindPropertyRelative("itemName").stringValue;

            if (string.IsNullOrEmpty(itemName))
            {
                itemName = "未命名物品";
            }

            labelList.Add($"[{itemId}] {itemName}");
            idList.Add(itemId);
        }

        labels = labelList.ToArray();
        ids = idList.ToArray();
    }

    private int GetPopupIndex(int[] ids, int currentId)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == currentId)
            {
                return i;
            }
        }

        return 0;
    }

    private SerializedProperty FindItemPropertyById(SerializedProperty itemsProperty, int itemId)
    {
        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);

            if (itemProperty.FindPropertyRelative("itemId").intValue == itemId)
            {
                return itemProperty;
            }
        }

        return null;
    }

    private bool IsMatchSearch(int levelId, string levelName)
    {
        if (string.IsNullOrWhiteSpace(searchKeyword)) return true;

        string keyword = searchKeyword.ToLower();

        bool idMatch = levelId.ToString().Contains(keyword);

        bool nameMatch = !string.IsNullOrEmpty(levelName) &&
                         levelName.ToLower().Contains(keyword);

        return idMatch || nameMatch;
    }

    private void LoadOrCreateDatabase()
    {
        database = AssetDatabase.LoadAssetAtPath<LevelDataSO>(DatabasePath);

        if (database == null)
        {
            EnsureFolder("Assets/GameData");

            database = CreateInstance<LevelDataSO>();

            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"已创建 LevelDatabase: {DatabasePath}");
        }

        serializedDatabase = new SerializedObject(database);
        levelsProperty = serializedDatabase.FindProperty("levels");

        if (selectedLevelIndex >= levelsProperty.arraySize)
        {
            selectedLevelIndex = levelsProperty.arraySize - 1;
            selectedItemIndex = -1;
        }

        NormalizeLevelIdsAndOrders();
    }

    private void PingDatabase()
    {
        if (database == null)
        {
            LoadOrCreateDatabase();
        }

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }

    private void SaveDatabase()
    {
        if (database == null) return;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif