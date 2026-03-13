using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillLoadoutRuntimeBinder : MonoBehaviour
{
    [Serializable]
    public struct SkillSlotData
    {
        public string skillId;

        public bool IsEmpty => string.IsNullOrWhiteSpace(skillId);
    }

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
    }

    private const string JourneySkillContainerPath = "Canvas/UI控制器/目录/角色页面/技能栏位/技能格子区域";
    private const string SlotNameKeyword = "格子";
    private const string OverlayIconName = "技能图案";
    private const string DefaultCharacterId = "玩家";

    private static SkillLoadoutRuntimeBinder instance;

    private readonly Dictionary<string, List<SkillSlotData>> loadoutByCharacter = new Dictionary<string, List<SkillSlotData>>(StringComparer.Ordinal);
    private readonly List<SkillSlotWidget> journeySkillSlots = new List<SkillSlotWidget>();

    private BattleSkillDatabase skillDatabase;
    private string currentCharacterId = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("SkillLoadoutRuntimeBinder");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillLoadoutRuntimeBinder>();
    }

    public static bool TryGetSkillSlotData(string characterId, int index, out SkillSlotData data)
    {
        data = default;
        if (instance == null)
        {
            return false;
        }

        List<SkillSlotData> loadout = instance.GetLoadout(characterId, createIfMissing: false);
        if (loadout == null || index < 0 || index >= loadout.Count)
        {
            return false;
        }

        data = loadout[index];
        return true;
    }

    public static bool TrySetSkillSlotData(string characterId, int index, SkillSlotData data)
    {
        if (instance == null || index < 0)
        {
            return false;
        }

        List<SkillSlotData> loadout = instance.GetLoadout(characterId, createIfMissing: true);
        EnsureSize(loadout, Mathf.Max(index + 1, instance.journeySkillSlots.Count));
        if (index >= loadout.Count)
        {
            return false;
        }

        loadout[index] = data;
        if (string.Equals(instance.currentCharacterId, instance.ResolveCharacterId(characterId), StringComparison.Ordinal))
        {
            instance.RefreshJourneySkillSlots();
        }

        return true;
    }

    public static bool TrySetSkillSlotId(string characterId, int index, string skillId)
    {
        return TrySetSkillSlotData(characterId, index, new SkillSlotData
        {
            skillId = skillId
        });
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        journeySkillSlots.Clear();
    }

    private void Update()
    {
        string targetCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        if (string.Equals(currentCharacterId, targetCharacterId, StringComparison.Ordinal))
        {
            return;
        }

        currentCharacterId = targetCharacterId;
        RefreshJourneySkillSlots();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        CollectJourneySkillSlots();
        currentCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        EnsureSize(GetCurrentLoadout(createIfMissing: true), journeySkillSlots.Count);
        RefreshJourneySkillSlots();
    }

    private void CollectJourneySkillSlots()
    {
        journeySkillSlots.Clear();

        RectTransform container = FindTransformByPath(JourneySkillContainerPath) as RectTransform;
        if (container == null)
        {
            return;
        }

        EnsureJourneyGridLayout(container);

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            if (!child.name.Contains(SlotNameKeyword, StringComparison.Ordinal))
            {
                continue;
            }

            Image overlay = EnsureOverlayIcon(child);
            if (overlay == null)
            {
                continue;
            }

            journeySkillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = overlay
            });
        }
    }

    private static void EnsureJourneyGridLayout(RectTransform container)
    {
        if (container == null)
        {
            return;
        }

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        int childCount = Mathf.Max(1, container.childCount);
        int columnCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
        grid.constraintCount = columnCount;

        Vector2 spacing = grid.spacing;
        RectOffset padding = grid.padding ?? new RectOffset();
        float availableWidth = Mathf.Max(1f, container.rect.width - padding.left - padding.right - spacing.x * Mathf.Max(0, columnCount - 1));
        float cellWidth = availableWidth / columnCount;
        float cellHeight = container.childCount > 0 ? Mathf.Max(1f, cellWidth) : grid.cellSize.y;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    private void RefreshJourneySkillSlots()
    {
        if (journeySkillSlots.Count == 0)
        {
            return;
        }

        List<SkillSlotData> loadout = GetCurrentLoadout(createIfMissing: true);
        EnsureSize(loadout, journeySkillSlots.Count);
        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            SkillSlotData slotData = i < loadout.Count ? loadout[i] : default;
            RefreshJourneySkillSlot(i, slotData);
        }
    }

    private void RefreshJourneySkillSlot(int index, SkillSlotData data)
    {
        if (index < 0 || index >= journeySkillSlots.Count)
        {
            return;
        }

        SkillSlotWidget widget = journeySkillSlots[index];
        if (widget == null || widget.skillIcon == null)
        {
            return;
        }

        Sprite icon = ResolveSkillIcon(data.skillId);
        widget.skillIcon.sprite = icon;
        widget.skillIcon.enabled = icon != null;
        widget.skillIcon.gameObject.SetActive(icon != null);
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

    private List<SkillSlotData> GetCurrentLoadout(bool createIfMissing)
    {
        return GetLoadout(currentCharacterId, createIfMissing);
    }

    private List<SkillSlotData> GetLoadout(string characterId, bool createIfMissing)
    {
        string resolvedCharacterId = ResolveCharacterId(characterId);
        List<SkillSlotData> loadout;
        if (loadoutByCharacter.TryGetValue(resolvedCharacterId, out loadout))
        {
            return loadout;
        }

        if (!createIfMissing)
        {
            return null;
        }

        loadout = new List<SkillSlotData>();
        EnsureSize(loadout, journeySkillSlots.Count);
        loadoutByCharacter[resolvedCharacterId] = loadout;
        return loadout;
    }

    private string ResolveCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
    }

    private static void EnsureSize(List<SkillSlotData> data, int size)
    {
        while (data.Count < size)
        {
            data.Add(default);
        }

        while (data.Count > size)
        {
            data.RemoveAt(data.Count - 1);
        }
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        Transform existing = FindChildByName(slotRoot, OverlayIconName);
        if (existing == null)
        {
            GameObject iconObject = new GameObject(OverlayIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = iconObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing.GetComponent<Image>();
        if (rect == null || image == null)
        {
            return null;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        existing.SetAsLastSibling();

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
