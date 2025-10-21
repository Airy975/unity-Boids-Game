using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainLogic : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject survivorPrefab;       // 幸存者预制体
    public GameObject EnemyPrefab;       // 敌人预制体
    public int minCount = 3;                // 最少生成数量
    public int maxCount = 8;                // 最多生成数量
    public Vector3 areaSize = new Vector3(10f, 0f, 10f);  // 生成区域大小
    public Transform ground;                // 地面中心点（可选）

    [Header("生成参数")]
    public float yOffset = 1.02f;            // 高度偏移（避免穿地）

    void Start()
    {
        if (survivorPrefab == null)
        {
            Debug.LogError("未指定幸存者预制体！");
            return;
        }
        if (EnemyPrefab == null)
        {
            Debug.LogError("未指定敌人预制体！");
            return;
        }

        // 随机生成幸存者数量
        int spawnCount = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPos = GetRandomPosition();
            Instantiate(survivorPrefab, randomPos, Quaternion.identity);
        }

        Debug.Log($"成功生成 {spawnCount} 个幸存者。");

        // 随机生成敌人数量
        int spawnEnemyCount = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < spawnEnemyCount; i++)
        {
            Vector3 randomPos = GetRandomPosition();
            Instantiate(EnemyPrefab, randomPos, Quaternion.identity);
        }

        Debug.Log($"成功生成 {spawnCount} 个敌人。");
    }

    // 生成区域内随机位置
    Vector3 GetRandomPosition()
    {
        Vector3 center = ground != null ? ground.position : transform.position;
        float randomX = Random.Range(-areaSize.x / 2, areaSize.x / 2);
        float randomZ = Random.Range(-areaSize.z / 2, areaSize.z / 2);
        return new Vector3(center.x + randomX, center.y + yOffset, center.z + randomZ);
    }

    // 在Scene视图中绘制生成区域可视化框
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Vector3 center = ground != null ? ground.position : transform.position;
        Gizmos.DrawCube(center + Vector3.up * yOffset, new Vector3(areaSize.x, 0.1f, areaSize.z));
    }
}
