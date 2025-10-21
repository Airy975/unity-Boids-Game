using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                      // 主角 Transform
    public Vector3 offset = new Vector3(0, 5, -7); // 摄像机相对主角的偏移（世界坐标）
    public float smoothSpeed = 5f;                // 摄像机平滑移动速度

    void LateUpdate()
    {
        if (target == null) return;

        // 不考虑角色旋转，只在世界坐标中偏移
        Vector3 desiredPosition = target.position + offset;

        // 平滑移动摄像机到目标位置
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 摄像机始终看向角色（可根据需要调整）
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
