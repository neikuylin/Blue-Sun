using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneReferenceAutoBinder
{
    private const string JourneyBindingsObjectName = "JourneySceneBindings";
    private const string BattleBindingsObjectName = "BattleSceneBindings";

    private const string AutoBindMenu = "Tools/UI References/Auto Bind Active Scene";
    private const string AutoBindJourneyMenu = "Tools/UI References/Auto Bind Journey Scene";
    private const string AutoBindBattleSceneMenu = "Tools/UI References/Auto Bind 战斗副本 Scene";
    private const string AutoBindBattleMenu = "Tools/UI References/Auto Bind 战斗副本 Scene";

    private const string DialogTitle = "\u81ea\u52a8\u7ed1\u5b9a";
    private const string DialogNoScene = "\u5f53\u524d\u6ca1\u6709\u53ef\u7ed1\u5b9a\u7684\u5df2\u52a0\u8f7d\u573a\u666f\u3002";
    private const string DialogDone = "\u5df2\u5b8c\u6210\uff0c\u66f4\u65b0\u4e86 {0} \u4e2a\u573a\u666f\u5f15\u7528\u7ec4\u4ef6\u3002";
    private const string DialogConfirm = "\u786e\u5b9a";

    private const string JourneyWarehousePath = "Canvas/UI\u63a7\u5236\u5668/\u76ee\u5f55/\u4ed3\u5e93\u9875\u9762/\u4ed3\u5e93\u9762\u677f/\u683c\u5b50\u533a\u57df/\u683c\u5b50\u5bb9\u5668";
    private const string JourneyBackpackPath = "Canvas/UI\u63a7\u5236\u5668/\u76ee\u5f55/\u4ed3\u5e93\u9875\u9762/\u80cc\u5305\u9762\u677f/\u683c\u5b50\u533a\u57df/\u683c\u5b50\u5bb9\u5668";
    private const string JourneyEquipmentPath = "Canvas/UI\u63a7\u5236\u5668/\u76ee\u5f55/\u89d2\u8272\u9875\u9762/\u88c5\u5907\u680f\u4f4d";
    private const string JourneyQuickPath = "Canvas/UI\u63a7\u5236\u5668/\u76ee\u5f55/\u89d2\u8272\u9875\u9762/\u53f3\u8fb9\u680f\u4f4d/\u683c\u5b50\u533a\u57df";
    private const string JourneySkillPath = "Canvas/UI\u63a7\u5236\u5668/\u76ee\u5f55/\u89d2\u8272\u9875\u9762/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df";

    private const string BattleTimelinePath = "Canvas/\u4e0a\u65b9\u680f\u4f4d/\u56de\u5408\u65f6\u95f4\u8f74";
    private const string BattleEndTurnButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u7ed3\u675f\u56de\u5408\u6309\u94ae";
    private const string BattleMoveButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u79fb\u52a8\u6309\u94ae";
    private const string PreviousPageButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u7ffb\u9875\u7cfb\u7edf/\u5f80\u524d\u7ffb\u9875";
    private const string NextPageButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u7ffb\u9875\u7cfb\u7edf/\u5f80\u540e\u7ffb\u9875";
    private const string SpellCurrentPagePath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u6570\u5b57\u663e\u793a/\u6cd5\u672f\u5f53\u524d\u9875";
    private const string SpellTotalPagePath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u6570\u5b57\u663e\u793a/\u603b\u9875";
    private const string BattleCurrentPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u5f53\u524d\u89d2\u8272/\u5f53\u524d\u89d2\u8272\u56fe";
    private const string BattleSecondPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e8c\u89d2\u8272/\u7b2c\u4e8c\u89d2\u8272\u56fe";
    private const string BattleThirdPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e09\u89d2\u8272/\u7b2c\u4e09\u89d2\u8272\u56fe";
    private const string BattleFourthPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u56db\u89d2\u8272/\u7b2c\u56db\u89d2\u8272\u56fe";
    private const string BattleActionPointPanelPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u884c\u52a8\u529b\u9762\u677f";
    private const string BattleHealthSlotPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u751f\u547d\u503c\u9762\u677f/\u751f\u547d\u69fd\u4f4d";
    private const string BattleHealthFillPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u751f\u547d\u503c\u9762\u677f/\u5f53\u524d\u751f\u547d\u503c";
    private const string BattleHealthTextPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u751f\u547d\u503c\u9762\u677f/\u751f\u547d\u503c\u6570\u5b57";
    private const string BattleManaSlotPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u9b54\u6cd5\u503c\u9762\u677f/\u9b54\u6cd5\u69fd\u4f4d";
    private const string BattleManaFillPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u9b54\u6cd5\u503c\u9762\u677f/\u5f53\u524d\u9b54\u6cd5\u503c";
    private const string BattleManaTextPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u9b54\u6cd5\u503c\u9762\u677f/\u9b54\u6cd5\u503c\u6570\u5b57";
    private const string BattleBackpackContainerPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u80cc\u5305/\u80cc\u5305\u5185\u5bb9/\u683c\u5b50\u533a\u57df";
    private const string BattleBackpackContentPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u80cc\u5305/\u80cc\u5305\u5185\u5bb9";
    private const string BattleBackpackDragHandlePath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u80cc\u5305/\u80cc\u5305\u5185\u5bb9/\u80cc\u5305\u80cc\u666f\u677f";
    private const string BattleEquipmentPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d";
    private const string BattleLeftPanelSkillPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df";
    private const string CanvasPath = "Canvas";

    [MenuItem(AutoBindMenu)]
    private static void AutoBindActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(DialogTitle, DialogNoScene, DialogConfirm);
            return;
        }

        int boundCount = 0;
        boundCount += AutoBindJourneyScene(scene);
        boundCount += AutoBindBattleScene(scene);

        if (boundCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorUtility.DisplayDialog(DialogTitle, string.Format(DialogDone, boundCount), DialogConfirm);
    }

    [MenuItem(AutoBindJourneyMenu)]
    private static void AutoBindJourneyOnly()
    {
        Scene scene = SceneManager.GetActiveScene();
        int count = AutoBindJourneyScene(scene);
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    [MenuItem(AutoBindBattleSceneMenu)]
    private static void AutoBindBattleOnly()
    {
        Scene scene = SceneManager.GetActiveScene();
        int count = AutoBindBattleScene(scene);
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static int AutoBindJourneyScene(Scene scene)
    {
        RectTransform warehouse = FindRectTransform(scene, JourneyWarehousePath);
        RectTransform backpack = FindRectTransform(scene, JourneyBackpackPath);
        RectTransform equipment = FindRectTransform(scene, JourneyEquipmentPath);
        RectTransform quick = FindRectTransform(scene, JourneyQuickPath);
        RectTransform skill = FindRectTransform(scene, JourneySkillPath);

        if (warehouse == null && backpack == null && equipment == null && quick == null && skill == null)
        {
            return 0;
        }

        JourneySceneBindings bindings = GetOrCreateRootComponent<JourneySceneBindings>(scene, JourneyBindingsObjectName);
        Undo.RecordObject(bindings, "Auto Bind Journey Scene");
        bindings.warehouseContainer = warehouse;
        bindings.backpackContainer = backpack;
        bindings.equipmentContainer = equipment;
        bindings.quickSlotAnchor = quick;
        EditorUtility.SetDirty(bindings);

        if (skill != null)
        {
            JourneySkillGridBinding skillBinding = GetOrCreateRootComponent<JourneySkillGridBinding>(scene, nameof(JourneySkillGridBinding));
            Undo.RecordObject(skillBinding, "Auto Bind Journey Skill Grid");
            skillBinding.skillSlotContainer = skill;
            EditorUtility.SetDirty(skillBinding);
        }

        return 1;
    }

    private static int AutoBindBattleScene(Scene scene)
    {
        Transform timeline = FindTransform(scene, BattleTimelinePath);
        Button endTurnButton = FindButton(scene, BattleEndTurnButtonPath);
        Button moveSkillButton = FindButton(scene, BattleMoveButtonPath);
        Button previousSkillPageButton = FindButton(scene, PreviousPageButtonPath);
        Button nextSkillPageButton = FindButton(scene, NextPageButtonPath);
        Image currentPortrait = FindImage(scene, BattleCurrentPortraitPath);
        Image secondPortrait = FindImage(scene, BattleSecondPortraitPath);
        Image thirdPortrait = FindImage(scene, BattleThirdPortraitPath);
        Image fourthPortrait = FindImage(scene, BattleFourthPortraitPath);
        Transform actionPointPanel = FindTransform(scene, BattleActionPointPanelPath);
        Image healthSlot = FindImage(scene, BattleHealthSlotPath);
        Image healthFill = FindImage(scene, BattleHealthFillPath);
        TMP_Text healthText = FindText(scene, BattleHealthTextPath);
        Image manaSlot = FindImage(scene, BattleManaSlotPath);
        Image manaFill = FindImage(scene, BattleManaFillPath);
        TMP_Text manaText = FindText(scene, BattleManaTextPath);
        TMP_Text spellCurrentPageText = FindText(scene, SpellCurrentPagePath);
        TMP_Text spellTotalPageText = FindText(scene, SpellTotalPagePath);
        RectTransform skillContainer = FindRectTransform(scene, "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df");
        RectTransform battleBackpackContainer = FindRectTransform(scene, BattleBackpackContainerPath);
        RectTransform battleBackpackContent = FindRectTransform(scene, BattleBackpackContentPath);
        RectTransform battleBackpackDragHandle = FindRectTransform(scene, BattleBackpackDragHandlePath);
        RectTransform equipmentContainer = FindRectTransform(scene, BattleEquipmentPath);
        RectTransform leftPanelSkillSlotContainer = FindRectTransform(scene, BattleLeftPanelSkillPath);
        RectTransform overlayCanvas = FindRectTransform(scene, CanvasPath);

        if (timeline == null &&
            endTurnButton == null &&
            moveSkillButton == null &&
            currentPortrait == null &&
            actionPointPanel == null &&
            healthSlot == null &&
            manaSlot == null &&
            equipmentContainer == null &&
            battleBackpackContainer == null &&
            overlayCanvas == null)
        {
            return 0;
        }

        BattleSceneBindings bindings = GetOrCreateRootComponent<BattleSceneBindings>(scene, BattleBindingsObjectName);
        Undo.RecordObject(bindings, "Auto Bind Battle Scene");
        bindings.timelineAnchor = timeline;
        bindings.endTurnButton = endTurnButton;
        bindings.moveSkillButton = moveSkillButton;
        bindings.previousSkillPageButton = previousSkillPageButton;
        bindings.nextSkillPageButton = nextSkillPageButton;
        bindings.currentPortrait = currentPortrait;
        bindings.secondPortrait = secondPortrait;
        bindings.thirdPortrait = thirdPortrait;
        bindings.fourthPortrait = fourthPortrait;
        bindings.actionPointPanel = actionPointPanel;
        bindings.healthSlotImage = healthSlot;
        bindings.healthFillImage = healthFill;
        bindings.healthText = healthText;
        bindings.manaSlotImage = manaSlot;
        bindings.manaFillImage = manaFill;
        bindings.manaText = manaText;
        bindings.battleBackpackContainer = battleBackpackContainer;
        bindings.battleBackpackContent = battleBackpackContent;
        bindings.battleBackpackDragHandle = battleBackpackDragHandle;
        bindings.equipmentContainer = equipmentContainer;
        bindings.leftPanelSkillSlotContainer = leftPanelSkillSlotContainer;
        bindings.overlayCanvas = overlayCanvas;
        bindings.spellCurrentPageText = spellCurrentPageText;
        bindings.spellTotalPageText = spellTotalPageText;
        CollectBattleSkillPageWidgets(skillContainer, bindings.skillPageButtons, bindings.skillPageIcons);
        EditorUtility.SetDirty(bindings);
        return 1;
    }

    private static void CollectBattleSkillPageWidgets(RectTransform container, List<Button> buttons, List<Image> icons)
    {
        if (buttons == null || icons == null)
        {
            return;
        }

        buttons.Clear();
        icons.Clear();
        if (container == null)
        {
            return;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Button button = child.GetComponent<Button>();
            Image icon = FindBestSkillIcon(child);
            if (button != null)
            {
                buttons.Add(button);
                icons.Add(icon);
            }
        }
    }

    private static T GetOrCreateRootComponent<T>(Scene scene, string rootName) where T : Component
    {
        T existing = Object.FindObjectOfType<T>(true);
        if (existing != null && existing.gameObject.scene == scene)
        {
            return existing;
        }

        GameObject root = FindRoot(scene, rootName);
        if (root == null)
        {
            root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Scene Bindings Root");
        }

        T component = root.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(root);
        }

        return component;
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == rootName)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static RectTransform FindRectTransform(Scene scene, string path)
    {
        return FindTransform(scene, path) as RectTransform;
    }

    private static Image FindImage(Scene scene, string path)
    {
        Transform target = FindTransform(scene, path);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Button FindButton(Scene scene, string path)
    {
        Transform target = FindTransform(scene, path);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static TMP_Text FindText(Scene scene, string path)
    {
        Transform target = FindTransform(scene, path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindTransform(Scene scene, string path)
    {
        return SceneHierarchyPathUtility.Find(scene, path);
    }

    private static Image FindBestSkillIcon(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform explicitPattern = SceneHierarchyPathUtility.FindDirectChildByName(root, "\u6280\u80fd\u56fe\u6848");
        if (explicitPattern != null)
        {
            Image explicitImage = explicitPattern.GetComponent<Image>();
            if (explicitImage != null)
            {
                return explicitImage;
            }
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        Image rootImage = root.GetComponent<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image != rootImage)
            {
                return image;
            }
        }

        return rootImage;
    }
}
