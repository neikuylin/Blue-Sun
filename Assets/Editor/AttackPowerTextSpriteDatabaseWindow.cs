using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class AttackPowerTextSpriteDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/AttackPowerTextSpriteDatabase.asset";

    private AttackPowerTextSpriteDatabase database;

    [MenuItem("Tools/文本/TMP绑定编辑器")]
    private static void Open()
    {
        AttackPowerTextSpriteDatabaseWindow window = GetWindow<AttackPowerTextSpriteDatabaseWindow>();
        window.titleContent = new GUIContent("TMP绑定编辑器");
        window.minSize = new Vector2(420f, 140f);
        window.Show();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("TMP 绑定数据加载失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("攻击力文本 TMP 绑定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("把包含 物理伤害 / 火焰伤害 / 腐败伤害 / 寒冷伤害 的 TMP Sprite Asset 直接拖进来。", MessageType.Info);
        EditorGUILayout.Space(6f);
        database.spriteAsset = (TMP_SpriteAsset)EditorGUILayout.ObjectField("属性伤害", database.spriteAsset, typeof(TMP_SpriteAsset), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
    }

    private static AttackPowerTextSpriteDatabase LoadOrCreateDatabase()
    {
        AttackPowerTextSpriteDatabase asset = AssetDatabase.LoadAssetAtPath<AttackPowerTextSpriteDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<AttackPowerTextSpriteDatabase>();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private void SaveDatabase()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
