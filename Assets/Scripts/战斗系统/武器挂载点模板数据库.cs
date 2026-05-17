using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "武器挂载点模板数据库", menuName = "战斗/武器挂载点模板数据库")]
public sealed class 武器挂载点模板数据库 : ScriptableObject
{
    [SerializeField]
    private List<武器挂载点模板> 模板列表 = new List<武器挂载点模板>
    {
        new 武器挂载点模板
        {
            模型名称 = "索拉娜",
            左手本地位置 = new Vector3(0.05f, 0f, 0f),
            左手本地欧拉角 = new Vector3(90f, 0f, 0f),
            右手本地位置 = new Vector3(-0.06873833f, -0.005495171f, 0.0047287312f),
            右手本地欧拉角 = new Vector3(-78.97751f, 37.49056f, -32.32306f),
        }
    };

    public List<武器挂载点模板> 模板 => 模板列表;

    public int 查找模板索引(string modelName)
    {
        for (int i = 0; i < 模板列表.Count; i++)
        {
            武器挂载点模板 template = 模板列表[i];
            if (template != null && template.模型名称 == modelName)
            {
                return i;
            }
        }

        return -1;
    }

    public 武器挂载点模板 取得或新增模板(string modelName, out int index)
    {
        index = 查找模板索引(modelName);
        if (index >= 0)
        {
            return 模板列表[index];
        }

        武器挂载点模板 template = new 武器挂载点模板
        {
            模型名称 = modelName,
        };
        模板列表.Add(template);
        index = 模板列表.Count - 1;
        return template;
    }
}
