using System.Collections;
using UnityEngine;

internal sealed class 战斗镜头震动服务
{
    private Coroutine 当前震动协程;
    private Camera 当前相机;
    private Vector3 上次偏移;

    public void 播放暴击震动(MonoBehaviour 宿主, Camera 相机)
    {
        播放震动(宿主, 相机, 0.12f, 0.1f);
    }

    public void 停止(MonoBehaviour 宿主)
    {
        if (宿主 != null && 当前震动协程 != null)
        {
            宿主.StopCoroutine(当前震动协程);
        }

        恢复上次偏移();
        当前震动协程 = null;
        当前相机 = null;
    }

    private void 播放震动(MonoBehaviour 宿主, Camera 相机, float 持续时间, float 强度)
    {
        if (宿主 == null || 相机 == null || 持续时间 <= 0f || 强度 <= 0f)
        {
            return;
        }

        停止(宿主);
        当前相机 = 相机;
        当前震动协程 = 宿主.StartCoroutine(播放震动协程(相机, 持续时间, 强度));
    }

    private IEnumerator 播放震动协程(Camera 相机, float 持续时间, float 强度)
    {
        float elapsed = 0f;
        while (elapsed < 持续时间 && 相机 != null)
        {
            恢复上次偏移();

            float t = 1f - Mathf.Clamp01(elapsed / 持续时间);
            Vector2 random = Random.insideUnitCircle * 强度 * t;
            Transform cameraTransform = 相机.transform;
            上次偏移 = cameraTransform.right * random.x + cameraTransform.up * random.y;
            cameraTransform.position += 上次偏移;

            elapsed += Time.deltaTime;
            yield return null;
        }

        恢复上次偏移();
        当前震动协程 = null;
        当前相机 = null;
    }

    private void 恢复上次偏移()
    {
        if (当前相机 != null && 上次偏移 != Vector3.zero)
        {
            当前相机.transform.position -= 上次偏移;
        }

        上次偏移 = Vector3.zero;
    }
}
