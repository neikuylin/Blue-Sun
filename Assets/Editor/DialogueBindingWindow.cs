using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DialogueBindingWindow : EditorWindow
{
    private const string RootObjectName = "DialogueSceneBindings";

    [MenuItem("Tools/事件/对话绑定")]
    private static void Open()
    {
        DialogueBindingWindow window = GetWindow<DialogueBindingWindow>("对话绑定");
        window.minSize = new Vector2(620f, 520f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorGUILayout.HelpBox("当前没有可编辑的已加载场景。", MessageType.Warning);
            return;
        }

        DialogueSceneBindings bindings = GetOrCreateBindings(scene);
        if (bindings == null)
        {
            EditorGUILayout.HelpBox("对话绑定组件创建失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("对话绑定", EditorStyles.boldLabel);
        EditorGUILayout.Space(6f);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
        bindings.mainViewPrefab = (GameObject)EditorGUILayout.ObjectField("主视角 Prefab", bindings.mainViewPrefab, typeof(GameObject), false);
        bindings.secondaryViewPrefab = (GameObject)EditorGUILayout.ObjectField("副视角 Prefab", bindings.secondaryViewPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("GameObject", EditorStyles.boldLabel);
        bindings.mainViewPortraitContainer = (GameObject)EditorGUILayout.ObjectField("主视角立绘容器", bindings.mainViewPortraitContainer, typeof(GameObject), true);
        bindings.secondaryViewPortraitContainer = (GameObject)EditorGUILayout.ObjectField("副视角立绘容器", bindings.secondaryViewPortraitContainer, typeof(GameObject), true);
        bindings.mainViewRoleName = (GameObject)EditorGUILayout.ObjectField("主视角角色名字", bindings.mainViewRoleName, typeof(GameObject), true);
        bindings.secondaryViewRoleName = (GameObject)EditorGUILayout.ObjectField("副视角角色名字", bindings.secondaryViewRoleName, typeof(GameObject), true);
        bindings.mainViewContent = (GameObject)EditorGUILayout.ObjectField("主视角对话内容", bindings.mainViewContent, typeof(GameObject), true);
        bindings.secondaryViewContent = (GameObject)EditorGUILayout.ObjectField("副视角对话内容", bindings.secondaryViewContent, typeof(GameObject), true);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(bindings, "修改对话绑定");
            EditorUtility.SetDirty(bindings);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    private static DialogueSceneBindings GetOrCreateBindings(Scene scene)
    {
        DialogueSceneBindings existing = DialogueSceneBindings.FindInActiveScene();
        if (existing != null && existing.gameObject.scene == scene)
        {
            return existing;
        }

        GameObject root = FindRoot(scene, RootObjectName);
        if (root == null)
        {
            root = new GameObject(RootObjectName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create DialogueSceneBindings Root");
        }

        DialogueSceneBindings bindings = root.GetComponent<DialogueSceneBindings>();
        if (bindings == null)
        {
            bindings = Undo.AddComponent<DialogueSceneBindings>(root);
        }

        return bindings;
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
}
