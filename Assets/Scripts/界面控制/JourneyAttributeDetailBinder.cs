using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneyAttributeDetailBinder : MonoBehaviour
{
    private const string CharacterNamePath = "\u6587\u672c\u533a\u57df/\u89d2\u8272ID";
    private const string StrengthPath = "\u6587\u672c\u533a\u57df/\u529b\u91cf";
    private const string AgilityPath = "\u6587\u672c\u533a\u57df/\u654f\u6377";
    private const string IntelligencePath = "\u6587\u672c\u533a\u57df/\u667a\u529b";

    [SerializeField] private Component characterNameText;
    [SerializeField] private Component strengthText;
    [SerializeField] private Component agilityText;
    [SerializeField] private Component intelligenceText;

    [SerializeField] private CharacterStatDatabase statDatabase;
    [SerializeField] private BattleCharacterBindingDatabase characterBindingDatabase;

    private string lastCharacterId = string.Empty;

    private void Reset()
    {
        AutoBind();
    }

    private void Awake()
    {
        EnsureDatabases();
    }

    private void OnEnable()
    {
        AutoBindMissingReferences();
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    [ContextMenu("\u81ea\u52a8\u7ed1\u5b9a")]
    private void AutoBind()
    {
        characterNameText = FindTextByPath(CharacterNamePath);
        strengthText = FindTextByPath(StrengthPath);
        agilityText = FindTextByPath(AgilityPath);
        intelligenceText = FindTextByPath(IntelligencePath);
        EnsureDatabases();
    }

    private void AutoBindMissingReferences()
    {
        if (characterNameText == null)
        {
            characterNameText = FindTextByPath(CharacterNamePath);
        }

        if (strengthText == null)
        {
            strengthText = FindTextByPath(StrengthPath);
        }

        if (agilityText == null)
        {
            agilityText = FindTextByPath(AgilityPath);
        }

        if (intelligenceText == null)
        {
            intelligenceText = FindTextByPath(IntelligencePath);
        }

        EnsureDatabases();
    }

    private void EnsureDatabases()
    {
        if (statDatabase == null)
        {
            statDatabase = CharacterStatDatabase.LoadDefault();
        }

        if (characterBindingDatabase == null)
        {
            characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        }
    }

    private void Refresh(bool force)
    {
        string characterId = ResolveCurrentCharacterId();
        if (!force && string.Equals(lastCharacterId, characterId, System.StringComparison.Ordinal))
        {
            return;
        }

        lastCharacterId = characterId ?? string.Empty;
        ApplyCharacter(lastCharacterId);
    }

    private static string ResolveCurrentCharacterId()
    {
        CharacterSlotView[] slots = FindObjectsOfType<CharacterSlotView>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            for (int j = 0; j < slot.selectToggles.Count; j++)
            {
                Toggle toggle = slot.selectToggles[j];
                if (toggle != null && toggle.isOn)
                {
                    string resolvedId = CharacterSelectionState.ResolveCharacterId(slot);
                    if (!string.IsNullOrWhiteSpace(resolvedId))
                    {
                        return resolvedId;
                    }
                }
            }
        }

        return string.Empty;
    }

    private void ApplyCharacter(string characterId)
    {
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;

        SetText(characterNameText, ResolveDisplayName(characterId));
        SetText(strengthText, statEntry != null ? "\u529b\u91cf:" + statEntry.strength : "\u529b\u91cf:");
        SetText(agilityText, statEntry != null ? "\u654f\u6377:" + statEntry.agility : "\u654f\u6377:");
        SetText(intelligenceText, statEntry != null ? "\u667a\u529b:" + statEntry.intelligence : "\u667a\u529b:");
    }

    private string ResolveDisplayName(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && characterBindingDatabase != null)
        {
            BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
            if (binding != null && !string.IsNullOrWhiteSpace(binding.displayName))
            {
                return binding.displayName;
            }
        }

        return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId;
    }

    private Component FindTextByPath(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            return null;
        }

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            return tmp;
        }

        Text legacyText = target.GetComponent<Text>();
        if (legacyText != null)
        {
            return legacyText;
        }

        return null;
    }

    private static void SetText(Component target, string value)
    {
        if (target is TMP_Text tmp)
        {
            tmp.text = value ?? string.Empty;
            return;
        }

        if (target is Text legacyText)
        {
            legacyText.text = value ?? string.Empty;
        }
    }
}
