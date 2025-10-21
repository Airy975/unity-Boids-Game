using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyLogic : MonoBehaviour
{
    [Header("寻路相关参数")]
    public float detectionRange = 5f;
    public float missingRange = 15f;
    private NavMeshAgent agent;
    private Transform player;

    [Header("敌人生命设置")]
    public int maxHealth = 100;
    public Slider slider;
    public Image fill;
    public Color FullHealthColor = Color.green;
    public Color ZeroHealthColor = Color.red;
    private int currentHealth;

    [Header("受伤设置")]
    public float damageRange = 2f;
    public float damageInterval = 1f;
    public int damageAmount = 10;

    [Header("目标检测")]
    public string survivorTag = "Survivor";

    // 每个攻击者的冷却时间记录
    private Dictionary<Transform, float> lastDamageTimeDict = new Dictionary<Transform, float>();

    void Start()
    {
        // 获取主角
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("未找到主角，请确保主角 Tag 为 Player");

        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        slider.value = maxHealth;
    }

    void Update()
    {
        HandleMovement();
        CheckDamageFromNearbyAllies();
        slider.transform.LookAt(Camera.main.transform);
    }

    // 自动寻路逻辑
    void HandleMovement()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
            agent.SetDestination(player.position);
        else if (distanceToPlayer > missingRange)
            agent.ResetPath();
    }

    // 检查主角/幸存者是否靠近并造成伤害
    void CheckDamageFromNearbyAllies()
    {
        List<Transform> attackers = new List<Transform>();

        // 主角
        if (player != null)
            attackers.Add(player);

        // 幸存者
        GameObject[] survivors = GameObject.FindGameObjectsWithTag(survivorTag);
        foreach (GameObject s in survivors)
            attackers.Add(s.transform);

        foreach (Transform attacker in attackers)
        {
            float distance = Vector3.Distance(transform.position, attacker.position);

            if (distance <= damageRange)
            {
                // 获取该攻击者的上次伤害时间
                float lastTime = 0f;
                lastDamageTimeDict.TryGetValue(attacker, out lastTime);

                if (Time.time - lastTime >= damageInterval)
                {
                    TakeDamage(damageAmount);
                    lastDamageTimeDict[attacker] = Time.time; // 单独更新该攻击者的冷却时间
                }
            }
        }
    }

    // 扣血逻辑
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} 受到 {amount} 点伤害，当前血量：{currentHealth}");
        slider.value = currentHealth;
        fill.color = Color.Lerp(ZeroHealthColor, FullHealthColor, slider.normalizedValue);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} 已死亡！");
        Destroy(gameObject);
    }
}
