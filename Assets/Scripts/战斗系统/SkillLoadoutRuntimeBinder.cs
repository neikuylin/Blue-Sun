using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillLoadoutRuntimeBinder : MonoBehaviour
{
    private static readonly string[] JourneySkillContainerChain =
    {
        "\u89d2\u8272\u9875\u9762",
        "\u6280\u80fd\u680f\u4f4d",
        "\u6280\u80fd\u683c\u5b50\u533a\u57df"
    };

    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string DefaultCharacterId = "\u73a9\u5bb6";

    private sealed class SkillSlotWidget
    {
        public Image skillIcon;
    }

    private static SkillLoadoutRuntimeBinder instance;

    private readonly List<SkillSlotWidget> journeySkillSlots = new List<SkillSlotWidget>();
    private BattleSkillDatabase skillDatabase;
    private CharacterSkillLoadoutDatabase loadoutDatabase;
    private string currentCharacterId = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(SkillLoadoutRuntimeBinder));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillLoadoutRuntimeBinder>();
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
        loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CollectJourneySkillSlots();
        currentCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        RefreshJourneySkillSlots();
    }

    private void CollectJourneySkillSlots()
    {
        journeySkillSlots.Clear();

        RectTransform container = FindJourneySkillContainer();
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

            Image overlay = EnsureOverlayIcon(child);
            if (overlay == null)
            {
                continue;
            }

            journeySkillSlots.Add(new SkillSlotWidget
            {
                skillIcon = overlay
            });
        }
    }

    private static RectTransform FindJourneySkillContainer()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            RectTransform found = FindContainerRecursive(roots[i] != null ? roots[i].transform : null, 0);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RectTransform FindContainerRecursive(Transform current, int matchedDepth)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, JourneySkillContainerChain[matchedDepth], StringComparison.Ordinal))
        {
            if (matchedDepth == JourneySkillContainerChain.Length - 1)
            {
                return current as RectTransform;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth + 1);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            int childCount = Mathf.Max(1, container.childCount);
            int columnCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
            grid.constraintCount = columnCount;
        }
    }

    private void RefreshJourneySkillSlots()
    {
        if (journeySkillSlots.Count == 0)
        {
            return;
        }

        List<string> skillIds = BuildJourneySkillList(currentCharacterId);
        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            string skillId = i < skillIds.Count ? skillIds[i] : string.Empty;
            Sprite icon = ResolveSkillIcon(skillId);
            Image target = journeySkillSlots[i].skillIcon;
            if (target == null)
            {
                continue;
            }

            target.sprite = icon;
            target.enabled = icon != null;
            target.gameObject.SetActive(icon != null);
        }
    }

    private List<string> BuildJourneySkillList(string characterId)
    {
        List<string> result = new List<string>();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(characterId);
        if (entry == null || entry.skillIds == null)
        {
            return result;
        }

        for (int i = 0; i < entry.skillIds.Count; i++)
        {
            result.Add(entry.skillIds[i]);
        }

        return result;
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

    private CharacterSkillLoadoutDatabase.CharacterSkillEntry ResolveLoadoutEntry(string characterId)
    {
        if (loadoutDatabase == null)
        {
            loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        }

        if (loadoutDatabase == null)
        {
            return null;
        }

        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = loadoutDatabase.FindEntry(ResolveCharacterId(characterId));
        if (entry != null)
        {
            return entry;
        }

        entry = loadoutDatabase.FindEntry(DefaultCharacterId);
        if (entry != null)
        {
            return entry;
        }

        List<CharacterSkillLoadoutDatabase.CharacterSkillEntry> entries = loadoutDatabase.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSkillLoadoutDatabase.CharacterSkillEntry candidate = entries[i];
            if (candidate != null && candidate.skillIds != null && candidate.skillIds.Count > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        Transform existing = FindChildByName(slotRoot, OverlayIconName);
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

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        existing.SetAsLastSibling();

        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = Color.white;
        return image;
    }

    private string ResolveCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
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
