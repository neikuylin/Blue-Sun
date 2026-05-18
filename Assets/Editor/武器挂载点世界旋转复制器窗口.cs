using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class 武器挂载点世界旋转复制器窗口 : EditorWindow
{
    private const string 左手挂载点名称 = "武器挂载点（左）";
    private const string 右手挂载点名称 = "武器挂载点（右）";

    private Transform 读取根物体;
    private Transform 粘贴根物体;
    private Quaternion 已记录左手世界旋转 = Quaternion.identity;
    private Quaternion 已记录右手世界旋转 = Quaternion.identity;
    private bool 已记录;

    [MenuItem("工具/战斗/武器挂载点世界旋转复制器")]
    private static void 打开窗口()
    {
        GetWindow<武器挂载点世界旋转复制器窗口>("挂载点旋转复制");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("武器挂载点世界旋转复制", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"读取根物体下的“{左手挂载点名称}”和“{右手挂载点名称}”世界旋转，再粘贴到另一个根物体下同名挂载点。只改左右挂载点旋转，不改根物体、位置和缩放。", MessageType.Info);

        读取根物体 = (Transform)EditorGUILayout.ObjectField("读取根物体", 读取根物体, typeof(Transform), true);
        粘贴根物体 = (Transform)EditorGUILayout.ObjectField("粘贴根物体", 粘贴根物体, typeof(Transform), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(读取根物体 == null))
            {
                if (GUILayout.Button("读取左右挂载点世界旋转"))
                {
                    读取左右挂载点世界旋转();
                }
            }

            using (new EditorGUI.DisabledScope(!已记录 || 粘贴根物体 == null))
            {
                if (GUILayout.Button("粘贴左右挂载点世界旋转"))
                {
                    粘贴左右挂载点世界旋转();
                }
            }
        }

        EditorGUILayout.Space(6f);
        if (!已记录)
        {
            EditorGUILayout.HelpBox("还没有读取左右挂载点世界旋转。", MessageType.Warning);
            return;
        }

        EditorGUILayout.Vector3Field("左手已记录世界欧拉角", 已记录左手世界旋转.eulerAngles);
        EditorGUILayout.LabelField("左手已记录世界四元数", 格式化四元数(已记录左手世界旋转));
        EditorGUILayout.Vector3Field("右手已记录世界欧拉角", 已记录右手世界旋转.eulerAngles);
        EditorGUILayout.LabelField("右手已记录世界四元数", 格式化四元数(已记录右手世界旋转));
    }

    private void 读取左右挂载点世界旋转()
    {
        if (!查找左右挂载点(读取根物体, out Transform leftMount, out Transform rightMount))
        {
            已记录 = false;
            Debug.LogError($"读取失败：在“{读取根物体.name}”下没有同时找到“{左手挂载点名称}”和“{右手挂载点名称}”。", 读取根物体);
            return;
        }

        已记录左手世界旋转 = leftMount.rotation;
        已记录右手世界旋转 = rightMount.rotation;
        已记录 = true;

        Debug.Log($"已读取“{读取根物体.name}”下左右武器挂载点的世界旋转。左手：{格式化四元数(已记录左手世界旋转)}，右手：{格式化四元数(已记录右手世界旋转)}。", 读取根物体);
    }

    private void 粘贴左右挂载点世界旋转()
    {
        if (!查找左右挂载点(粘贴根物体, out Transform leftMount, out Transform rightMount))
        {
            Debug.LogError($"粘贴失败：在“{粘贴根物体.name}”下没有同时找到“{左手挂载点名称}”和“{右手挂载点名称}”。", 粘贴根物体);
            return;
        }

        Undo.RecordObjects(new Object[] { leftMount, rightMount }, "粘贴左右武器挂载点世界旋转");

        leftMount.rotation = 已记录左手世界旋转;
        rightMount.rotation = 已记录右手世界旋转;

        标记已修改(leftMount);
        标记已修改(rightMount);

        Debug.Log($"已把记录的左右武器挂载点世界旋转粘贴到“{粘贴根物体.name}”下的同名挂载点。", 粘贴根物体);
    }

    private static bool 查找左右挂载点(Transform root, out Transform leftMount, out Transform rightMount)
    {
        leftMount = 查找子物体(root, 左手挂载点名称);
        rightMount = 查找子物体(root, 右手挂载点名称);
        return leftMount != null && rightMount != null;
    }

    private static Transform 查找子物体(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = 查找子物体(root.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void 标记已修改(Transform target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        if (target.gameObject != null)
        {
            EditorUtility.SetDirty(target.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(target.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
            }
        }

        if (target.gameObject != null && target.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
    }

    private static string 格式化四元数(Quaternion value)
    {
        return $"x:{value.x:F9} y:{value.y:F9} z:{value.z:F9} w:{value.w:F9}";
    }
}
