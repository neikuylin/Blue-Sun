using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleLeftPanelBinder : MonoBehaviour
{
    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string LeftPanelPortraitPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u89d2\u8272\u80cc\u666f\u6846\u5de6/\u89d2\u8272\u80cc\u666f\u6846\u7acb\u7ed8";
    private const string LeftPanelPreviewPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u89d2\u8272\u80cc\u666f\u6846\u5de6/\u6444\u50cf\u5934\u6355\u6349";
    private const string LeftPanelSkillPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df";
    private const string PreviewImageName = "__ModelPreviewImage";
    private const int PreviewLayer = 31;
    private const int PreviewTextureSize = 1024;
    private const float PreviewOutlineWidth = 0.035f;
    private static readonly Color DisabledSkillColor = SkillUsabilityUtility.DisabledSkillColor;
    private static readonly Color EnabledSkillColor = SkillUsabilityUtility.EnabledSkillColor;

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
        public string skillId;
        public SkillHoverRelay hoverRelay;
    }

    private sealed class SkillHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleLeftPanelBinder owner;
        private int index;

        public void Configure(BattleLeftPanelBinder binder, int slotIndex)
        {
            owner = binder;
            index = slotIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandleSkillPointerEnter(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandleSkillPointerExit(index, eventData);
        }
    }

    private static BattleLeftPanelBinder instance;

    private readonly List<SkillSlotWidget> skillSlots = new List<SkillSlotWidget>();
    private BattleSkillDatabase skillDatabase;
    private BattleCharacterBindingDatabase characterBindingDatabase;
    private BattleSceneBindings battleBindings;
    private Image leftPanelPortraitImage;
    private RectTransform leftPanelPortraitAnchor;
    private RectTransform leftPanelPreviewAnchor;
    private RectTransform leftPanelSkillContainer;
    private string currentCharacterId = string.Empty;
    private int lastEquipmentSkillRevision = -1;
    private GameObject activePortraitPrefabInstance;
    private string activePortraitPrefabCharacterId = string.Empty;
    private RawImage previewImage;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private GameObject previewRuntimeRoot;
    private BattleUnit previewTargetUnit;
    private string previewCharacterId = string.Empty;
    private readonly Dictionary<Transform, int> previewOriginalLayers = new Dictionary<Transform, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(BattleLeftPanelBinder));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BattleLeftPanelBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        skillSlots.Clear();
        ClearPreviewTargetUnit();
    }

    private void Update()
    {
        if (!IsEquipmentPanelVisible())
        {
            currentCharacterId = string.Empty;
            ClearPreviewTargetUnit();
        }

        UpdatePreviewCameraFollow();

        string targetCharacterId = ResolveCharacterId();
        int equipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        if (string.Equals(currentCharacterId, targetCharacterId, StringComparison.Ordinal) &&
            lastEquipmentSkillRevision == equipmentSkillRevision)
        {
            return;
        }

        currentCharacterId = targetCharacterId;
        lastEquipmentSkillRevision = equipmentSkillRevision;
        RefreshLeftPanel();
    }

    private void UpdatePreviewCameraFollow()
    {
        if (previewTargetUnit == null || previewCamera == null || string.IsNullOrWhiteSpace(previewCharacterId))
        {
            return;
        }

        if (!IsEquipmentPanelVisible())
        {
            ClearPreviewTargetUnit();
            return;
        }

        if (!previewTargetUnit.IsAlive)
        {
            ClearPreviewTargetUnit();
            return;
        }

        StoreAndApplyPreviewLayer(previewTargetUnit.transform);
        previewTargetUnit.SetPreviewOutline(Color.white, PreviewOutlineWidth, true);
        PositionPreviewCamera(previewTargetUnit.gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        battleBindings = BattleSceneBindings.FindInActiveScene();
        leftPanelPortraitImage = ResolveLeftPanelPortrait();
        leftPanelPortraitAnchor = ResolveLeftPanelPortraitAnchor();
        leftPanelPreviewAnchor = ResolveLeftPanelPreviewAnchor();
        leftPanelSkillContainer = ResolveLeftPanelSkillContainer();
        CollectSkillSlots();
        currentCharacterId = ResolveCharacterId();
        lastEquipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        RefreshLeftPanel();
    }

    private void CollectSkillSlots()
    {
        skillSlots.Clear();
        RectTransform container = leftPanelSkillContainer != null ? leftPanelSkillContainer : ResolveLeftPanelSkillContainer();
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

            Image icon = EnsureOverlayIcon(child);
            if (icon == null)
            {
                continue;
            }

            skillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = icon
            });
        }
    }

    private void RefreshLeftPanel()
    {
        RefreshPortrait();
        RefreshModelPreview();
        RefreshSkillSlots();
    }

    private void RefreshPortrait()
    {
        if (leftPanelPortraitImage == null)
        {
            leftPanelPortraitImage = ResolveLeftPanelPortrait();
        }

        if (leftPanelPortraitAnchor == null)
        {
            leftPanelPortraitAnchor = ResolveLeftPanelPortraitAnchor();
        }

        if (leftPanelPortraitImage == null && leftPanelPortraitAnchor == null)
        {
            return;
        }

        if (characterBindingDatabase == null)
        {
            characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        }

        if (TryShowBackgroundPortraitPrefab(currentCharacterId))
        {
            if (leftPanelPortraitImage != null)
            {
                leftPanelPortraitImage.enabled = false;
                leftPanelPortraitImage.color = new Color(1f, 1f, 1f, 0f);
            }

            return;
        }

        DestroyActivePortraitPrefabInstance();

        if (leftPanelPortraitImage == null)
        {
            return;
        }

        leftPanelPortraitImage.sprite = null;
        leftPanelPortraitImage.enabled = false;
        leftPanelPortraitImage.preserveAspect = true;
        leftPanelPortraitImage.color = new Color(1f, 1f, 1f, 0f);
        leftPanelPortraitImage.gameObject.SetActive(true);
    }

    private void RefreshSkillSlots()
    {
        if (skillSlots.Count == 0)
        {
            return;
        }

        List<string> skillIds = CharacterSkillListUtility.BuildSkillIds(currentCharacterId);
        for (int i = 0; i < skillSlots.Count; i++)
        {
            string skillId = i < skillIds.Count ? skillIds[i] : string.Empty;
            Sprite icon = ResolveSkillIcon(skillId);
            SkillSlotWidget widget = skillSlots[i];
            widget.skillId = skillId;
            EnsureHoverRelay(widget, i);
            Image target = widget.skillIcon;
            if (target == null)
            {
                continue;
            }

            target.sprite = icon;
            target.enabled = icon != null;
            target.gameObject.SetActive(icon != null);
            target.color = SkillUsabilityUtility.IsSkillUsable(skillDatabase, currentCharacterId, skillId)
                ? EnabledSkillColor
                : DisabledSkillColor;
        }
    }

    private void HandleSkillPointerEnter(int index)
    {
        if (index < 0 || index >= skillSlots.Count)
        {
            return;
        }

        SkillSlotWidget widget = skillSlots[index];
        if (widget == null || widget.root == null || string.IsNullOrWhiteSpace(widget.skillId))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(widget.skillId) : null;
        if (entry == null || entry.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        float attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(currentCharacterId);
        float multiplier = Mathf.Max(0f, entry.damageMultiplier);
        SkillTooltipRuntime.Snapshot snapshot = new SkillTooltipRuntime.Snapshot
        {
            skillId = widget.skillId,
            displayName = widget.skillId,
            description = entry.description ?? string.Empty,
            ownerCharacterId = currentCharacterId ?? string.Empty,
            damage = Mathf.Max(0, Mathf.RoundToInt(attackPower * multiplier)),
            icon = entry.icon,
            isEmpty = false
        };

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            widget.root,
            0.5f,
            () => SkillTooltipRuntime.Show(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void HandleSkillPointerExit(int index, PointerEventData eventData)
    {
        SkillSlotWidget widget = index >= 0 && index < skillSlots.Count ? skillSlots[index] : null;
        if (widget == null || widget.root == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, widget.root, eventData);
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        return entry != null ? entry.icon : null;
    }

    private Sprite ResolveBackgroundPortraitSprite(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && characterBindingDatabase != null)
        {
            BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
            if (binding != null && binding.backgroundPortraitSprite != null)
            {
                return binding.backgroundPortraitSprite;
            }
        }

        return CharacterSelectionState.GetCapturedBackgroundPortraitSprite(characterId);
    }

    private bool TryShowBackgroundPortraitPrefab(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || characterBindingDatabase == null || leftPanelPortraitAnchor == null)
        {
            return false;
        }

        BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
        if (binding == null || binding.backgroundPortraitPrefab == null)
        {
            return false;
        }

        if (activePortraitPrefabInstance != null &&
            string.Equals(activePortraitPrefabCharacterId, characterId, StringComparison.Ordinal))
        {
            return true;
        }

        DestroyActivePortraitPrefabInstance();
        activePortraitPrefabInstance = Instantiate(binding.backgroundPortraitPrefab, leftPanelPortraitAnchor, false);
        activePortraitPrefabInstance.name = binding.backgroundPortraitPrefab.name;
        activePortraitPrefabInstance.SetActive(true);
        activePortraitPrefabCharacterId = characterId;
        return true;
    }

    private void DestroyActivePortraitPrefabInstance()
    {
        if (activePortraitPrefabInstance != null)
        {
            Destroy(activePortraitPrefabInstance);
            activePortraitPrefabInstance = null;
        }

        activePortraitPrefabCharacterId = string.Empty;
    }

    private void RefreshModelPreview()
    {
        if (leftPanelPreviewAnchor == null)
        {
            leftPanelPreviewAnchor = ResolveLeftPanelPreviewAnchor();
        }

        if (leftPanelPreviewAnchor == null)
        {
            ClearPreviewTargetUnit();
            return;
        }

        EnsurePreviewRuntime();

        bool shouldShow = !string.IsNullOrWhiteSpace(currentCharacterId);
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            ClearPreviewTargetUnit();
            return;
        }

        if (previewTargetUnit != null &&
            string.Equals(previewCharacterId, currentCharacterId, StringComparison.Ordinal))
        {
            PositionPreviewCamera(previewTargetUnit.gameObject);
            return;
        }

        BindPreviewTarget(currentCharacterId);
    }

    private void EnsurePreviewRuntime()
    {
        if (previewRuntimeRoot == null)
        {
            previewRuntimeRoot = new GameObject("BattleLeftPanelPreviewRuntime");
            previewRuntimeRoot.transform.SetParent(transform, false);
            previewRuntimeRoot.transform.position = new Vector3(10000f, 10000f, 10000f);
            previewRuntimeRoot.hideFlags = HideFlags.HideAndDontSave;
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject("BattleLeftPanelPreviewCamera");
            cameraObject.transform.SetParent(previewRuntimeRoot.transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.orthographic = true;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 50f;
            previewCamera.enabled = true;
            previewCamera.cullingMask = 1 << PreviewLayer;
        }

        EnsurePreviewTexture();
        EnsurePreviewImage();
    }

    private void EnsurePreviewTexture()
    {
        if (previewTexture == null)
        {
            previewTexture = new RenderTexture(PreviewTextureSize, PreviewTextureSize, 24, RenderTextureFormat.ARGB32)
            {
                name = "BattleLeftPanelPreviewTexture",
                antiAliasing = 2
            };
            previewTexture.Create();
        }

        if (previewCamera != null && previewCamera.targetTexture != previewTexture)
        {
            previewCamera.targetTexture = previewTexture;
        }
    }

    private void EnsurePreviewImage()
    {
        if (leftPanelPreviewAnchor == null)
        {
            return;
        }

        if (previewImage == null)
        {
            Transform existing = SceneHierarchyPathUtility.FindDirectChildByName(leftPanelPreviewAnchor, PreviewImageName);
            if (existing == null)
            {
                GameObject imageObject = new GameObject(PreviewImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                existing = imageObject.transform;
                existing.SetParent(leftPanelPreviewAnchor, false);
            }

            previewImage = existing.GetComponent<RawImage>();
        }

        if (previewImage == null)
        {
            return;
        }

        RectTransform rect = previewImage.rectTransform;
        rect.SetParent(leftPanelPreviewAnchor, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        previewImage.texture = previewTexture;
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;
    }

    private void BindPreviewTarget(string characterId)
    {
        ClearPreviewTargetUnit();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        BattleUnit targetUnit = FindBattleUnitByCharacterId(characterId);
        if (targetUnit == null)
        {
            return;
        }

        previewTargetUnit = targetUnit;
        previewCharacterId = characterId;
        ApplyPreviewState(previewTargetUnit);
        PositionPreviewCamera(previewTargetUnit.gameObject);
    }

    private void ApplyPreviewState(BattleUnit targetUnit)
    {
        if (targetUnit == null)
        {
            return;
        }

        targetUnit.SetPreviewOutline(Color.white, PreviewOutlineWidth, true);
        StoreAndApplyPreviewLayer(targetUnit.transform);
    }

    private void PositionPreviewCamera(GameObject previewObject)
    {
        if (previewObject == null || previewCamera == null)
        {
            return;
        }

        Bounds bounds;
        if (!TryGetPreviewBounds(previewObject, out bounds))
        {
            bounds = new Bounds(previewObject.transform.position, Vector3.one);
        }

        Vector3 center = bounds.center;
        float height = Mathf.Max(1f, bounds.size.y);
        float horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float radius = Mathf.Max(0.35f, bounds.extents.magnitude);
        float distance = Mathf.Max(3f, radius * 3.2f);
        Vector3 direction = new Vector3(1f, 1f, 1f).normalized;

        previewCamera.transform.position = center + direction * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(center - previewCamera.transform.position, Vector3.up);
        previewCamera.orthographicSize = Mathf.Max(height * 0.6f, horizontalExtent * 1.6f) * 1.1f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = Mathf.Max(25f, distance + height * 6f);
    }

    private static bool TryGetPreviewBounds(GameObject previewObject, out Bounds bounds)
    {
        Renderer[] renderers = previewObject != null ? previewObject.GetComponentsInChildren<Renderer>(true) : null;
        bool hasBounds = false;
        bounds = default;

        if (renderers == null)
        {
            return false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (renderer.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private BattleUnit FindBattleUnitByCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        BattleUnit[] units = FindObjectsOfType<BattleUnit>(true);
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            return unit;
        }

        return null;
    }

    private void ClearPreviewTargetUnit()
    {
        if (previewTargetUnit != null)
        {
            previewTargetUnit.ClearPreviewOutline();
            RestorePreviewLayers();
            previewTargetUnit = null;
        }

        previewCharacterId = string.Empty;
    }

    private void ReleasePreviewTexture()
    {
        if (previewTexture == null)
        {
            return;
        }

        if (previewCamera != null && previewCamera.targetTexture == previewTexture)
        {
            previewCamera.targetTexture = null;
        }

        previewTexture.Release();
        Destroy(previewTexture);
        previewTexture = null;
    }

    private void StoreAndApplyPreviewLayer(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (!previewOriginalLayers.ContainsKey(root))
        {
            previewOriginalLayers[root] = root.gameObject.layer;
        }

        root.gameObject.layer = PreviewLayer;
        for (int i = 0; i < root.childCount; i++)
        {
            StoreAndApplyPreviewLayer(root.GetChild(i));
        }
    }

    private void RestorePreviewLayers()
    {
        foreach (KeyValuePair<Transform, int> pair in previewOriginalLayers)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.layer = pair.Value;
            }
        }

        previewOriginalLayers.Clear();
    }

    private string ResolveCharacterId()
    {
        if (!IsEquipmentPanelVisible())
        {
            return string.Empty;
        }

        string characterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;

        BattleTurnSystem battleTurnSystem = FindObjectOfType<BattleTurnSystem>(true);
        if (battleTurnSystem != null)
        {
            return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            characterId = CharacterSelectionState.ActiveCharacterId;
        }
        return characterId;
    }

    private bool IsEquipmentPanelVisible()
    {
        RectTransform panel = battleBindings != null && battleBindings.equipmentContainer != null
            ? battleBindings.equipmentContainer
            : ResolveEquipmentPanelRoot();
        return panel != null && panel.gameObject.activeInHierarchy;
    }

    private RectTransform ResolveEquipmentPanelRoot()
    {
        Transform target = SceneHierarchyPathUtility.FindInActiveScene("Canvas/弹窗/左边栏位");
        return target as RectTransform;
    }

    private Image ResolveLeftPanelPortrait()
    {
        if (battleBindings != null && battleBindings.leftPanelPortraitImage != null)
        {
            return battleBindings.leftPanelPortraitImage;
        }

        Transform target = SceneHierarchyPathUtility.FindInActiveScene(LeftPanelPortraitPath);
        if (target == null)
        {
            return null;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            return image;
        }

        return target.GetComponentInChildren<Image>(true);
    }

    private RectTransform ResolveLeftPanelPortraitAnchor()
    {
        Transform target = SceneHierarchyPathUtility.FindInActiveScene(LeftPanelPortraitPath);
        if (target is RectTransform targetRect)
        {
            return targetRect;
        }

        if (leftPanelPortraitImage != null)
        {
            return leftPanelPortraitImage.rectTransform;
        }

        return null;
    }

    private RectTransform ResolveLeftPanelPreviewAnchor()
    {
        Transform target = SceneHierarchyPathUtility.FindInActiveScene(LeftPanelPreviewPath);
        return target as RectTransform;
    }

    private RectTransform ResolveLeftPanelSkillContainer()
    {
        if (battleBindings != null && battleBindings.leftPanelSkillSlotContainer != null)
        {
            return battleBindings.leftPanelSkillSlotContainer;
        }

        return SceneHierarchyPathUtility.FindInActiveScene(LeftPanelSkillPath) as RectTransform;
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        Transform existing = SceneHierarchyPathUtility.FindDirectChildByName(slotRoot, OverlayIconName);
        if (existing == null)
        {
            GameObject iconObject = new GameObject(OverlayIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = iconObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (rect == null || image == null)
        {
            return null;
        }

        if (existing.parent != slotRoot)
        {
            existing.SetParent(slotRoot, false);
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static void EnsureHoverRelay(SkillSlotWidget widget, int index)
    {
        if (widget == null || widget.root == null)
        {
            return;
        }

        if (widget.hoverRelay == null)
        {
            widget.hoverRelay = widget.root.GetComponent<SkillHoverRelay>();
            if (widget.hoverRelay == null)
            {
                widget.hoverRelay = widget.root.gameObject.AddComponent<SkillHoverRelay>();
            }
        }

        widget.hoverRelay.Configure(instance, index);
    }

    private void OnDestroy()
    {
        DestroyActivePortraitPrefabInstance();
        ClearPreviewTargetUnit();
        ReleasePreviewTexture();

        if (previewImage != null)
        {
            Destroy(previewImage.gameObject);
            previewImage = null;
        }

        if (previewRuntimeRoot != null)
        {
            Destroy(previewRuntimeRoot);
            previewRuntimeRoot = null;
        }
    }
}
