using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueSceneBindings : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject mainViewPrefab;
    public GameObject secondaryViewPrefab;

    [Header("Portrait Containers")]
    public GameObject mainViewPortraitContainer;
    public GameObject secondaryViewPortraitContainer;

    [Header("Role Names")]
    public GameObject mainViewRoleName;
    public GameObject secondaryViewRoleName;

    [Header("Contents")]
    public GameObject mainViewContent;
    public GameObject secondaryViewContent;

    public static DialogueSceneBindings FindInActiveScene()
    {
        return FindObjectOfType<DialogueSceneBindings>(true);
    }
}
