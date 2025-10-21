using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Survivor : MonoBehaviour
{
    [Header("基本参数")]
    public float followDistance = 3f;         // 幸存者与主角保持的距离
    public float separationDistance = 1.5f;   // 最小间距
    public float moveSpeed = 3f;              // 移动速度
    public float rotationSpeed = 5f;          // 转向速度
    public float detectionRange = 5f;         // 触发范围

    [Header("高亮相关")]
    public MonoBehaviour OutlineLogic;        // 高亮脚本
    private Transform player;                 // 主角

    [Header("玩家生命值")]
    public float damageRange = 2f;        // 敌人靠近多少距离会造成伤害
    public float damageInterval = 1f;     // 每次伤害间隔（秒）
    public int damageAmount = 10;         // 每次伤害量
    private float lastDamageTime;
    public int maxHealth = 100;
    public Slider slider;
    public Image fill;
    public Color FullHealthColor = Color.green;
    public Color ZeroHealthColor = Color.red;
    private int currentHealth;

    [Header("障碍躲避参数")]
    public float obstacleAvoidDistance = 2f;   // 射线检测距离
    public float avoidForce = 5f;              // 躲避力度
    public LayerMask obstacleLayer;            // 障碍物层（在Inspector中设置）

    private static List<Survivor> survivors = new List<Survivor>();
    private bool isFollowing = false;

    // 平滑用变量
    private Vector3 separationSmooth = Vector3.zero;

    void Start()
    {
        survivors.Add(this);

        // 自动寻找Player对象
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;

            IgnorePlayerCollision(playerObj);
        }
        else
        {
            Debug.LogWarning($"{name} 未找到Tag为 'Player' 的对象，请确保主角已正确设置Tag！");
        }

        // 初始状态下高亮开启
        if (OutlineLogic != null)
            OutlineLogic.enabled = true;

        // 初始化生命值
        currentHealth = maxHealth;
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
        fill.color = FullHealthColor;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 当Player靠近时开始跟随
        if (!isFollowing && distanceToPlayer <= detectionRange)
        {
            isFollowing = true;

            if (OutlineLogic != null)
                OutlineLogic.enabled = false;
        }

        // 未开始跟随
        if (!isFollowing)
        {
            if (OutlineLogic != null && !OutlineLogic.enabled)
                OutlineLogic.enabled = true;

            // 静止状态下也进行轻微分离，避免重叠
            ApplyIdleSeparation();
            return;
        }

        // 目标点在玩家身后一定距离
        Vector3 targetPos = player.position - player.forward * followDistance;
        Vector3 moveDir = (targetPos - transform.position);
        moveDir.y = 0;
        moveDir.Normalize();

        // 分离方向
        Vector3 separationDir = GetSeparationDir();

        // 平滑分离力，避免抖动
        separationSmooth = Vector3.Lerp(separationSmooth, separationDir, Time.deltaTime * 5f);

        // 避障修正
        ApplyObstacleAvoidance(ref moveDir);

        // 最终合成方向（分离力占较小权重）
        Vector3 finalDir = (moveDir + separationSmooth * 0.5f).normalized;

        // 平滑朝向
        Vector3 smoothedDir = Vector3.Lerp(transform.forward, finalDir, Time.deltaTime * rotationSpeed);
        smoothedDir.y = 0;

        // 旋转与移动
        if (smoothedDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(smoothedDir);
            transform.position += smoothedDir * moveSpeed * Time.deltaTime;
        }

        CheckEnemyDistance();
        slider.transform.LookAt(Camera.main.transform);
    }

    // ✅ 忽略与主角的碰撞
    void IgnorePlayerCollision(GameObject playerObj)
    {
        Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
        Collider[] myColliders = GetComponentsInChildren<Collider>();

        foreach (var pCol in playerColliders)
        {
            foreach (var sCol in myColliders)
            {
                Physics.IgnoreCollision(pCol, sCol, true);
            }
        }
    }

    // 获取分离方向（群体移动核心）
    Vector3 GetSeparationDir()
    {
        Vector3 separationDir = Vector3.zero;
        int neighborCount = 0;

        foreach (var s in survivors)
        {
            if (s == this) continue;
            Vector3 diff = transform.position - s.transform.position;
            float distance = diff.magnitude;

            if (distance < separationDistance && distance > 0.01f)
            {
                // 距离越近推力越强，但限制最大值
                float strength = Mathf.Clamp01(1f - distance / separationDistance);
                separationDir += diff.normalized * strength;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
            separationDir /= neighborCount;

        return separationDir.normalized;
    }

    // 未跟随时，轻微位置修正避免重叠
    void ApplyIdleSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        foreach (var s in survivors)
        {
            if (s == this) continue;
            Vector3 diff = transform.position - s.transform.position;
            float distance = diff.magnitude;

            if (distance < separationDistance && distance > 0.01f)
            {
                separationForce += diff.normalized * (1f - distance / separationDistance);
            }
        }

        if (separationForce.sqrMagnitude > 0.001f)
        {
            transform.position += separationForce.normalized * moveSpeed * 0.5f * Time.deltaTime;
        }
    }

    // 避障逻辑
    void ApplyObstacleAvoidance(ref Vector3 moveDir)
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, obstacleAvoidDistance, obstacleLayer))
        {
            // 计算反射方向（避让）
            Vector3 avoidDir = Vector3.Reflect(transform.forward, hit.normal);
            avoidDir.y = 0;

            moveDir = Vector3.Lerp(moveDir, avoidDir.normalized, Time.deltaTime * avoidForce);
        }
    }

    void CheckEnemyDistance()
    {
        // 查找场景中所有带有 "Enemy" 标签的对象
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= damageRange)
            {
                // 每 damageInterval 秒才会造成一次伤害
                if (Time.time - lastDamageTime >= damageInterval)
                {
                    TakeDamage(damageAmount);
                    lastDamageTime = Time.time;
                }
            }
        }
    }

    void TakeDamage(int amount)
    {
        currentHealth -= amount;

        slider.value = currentHealth;
        fill.color = Color.Lerp(ZeroHealthColor, FullHealthColor, slider.normalizedValue);

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
            survivors.Remove(this);
        }
    }
}
