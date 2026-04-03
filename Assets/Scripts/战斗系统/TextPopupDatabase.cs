using UnityEngine;

[CreateAssetMenu(fileName = "TextPopupDatabase", menuName = "战斗/文本弹窗库")]
public sealed class TextPopupDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "TextPopupDatabase";

    [SerializeField] private GameObject damagePopupTextObject;
    [SerializeField] private GameObject missPopupTextObject;
    [SerializeField] private GameObject effectPopupTextObject;

    public GameObject DamagePopupTextObject
    {
        get { return damagePopupTextObject; }
        set { damagePopupTextObject = value; }
    }

    public GameObject MissPopupTextObject
    {
        get { return missPopupTextObject; }
        set { missPopupTextObject = value; }
    }

    public GameObject EffectPopupTextObject
    {
        get { return effectPopupTextObject; }
        set { effectPopupTextObject = value; }
    }

    public static TextPopupDatabase LoadDefault()
    {
        return Resources.Load<TextPopupDatabase>(DefaultResourcePath);
    }
}
