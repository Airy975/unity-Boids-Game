using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;

    [Header("敌人检测设置")]
    public float damageRange = 2f;        // 敌人靠近多少距离会造成伤害
    public float damageInterval = 1f;     // 每次伤害间隔（秒）
    public int damageAmount = 10;         // 每次伤害量
    private float lastDamageTime;

    [Header("玩家生命值")]
    public int maxHealth = 100;
    public Slider slider;
    public Image fill;
    public Color FullHealthColor = Color.green;
    public Color ZeroHealthColor = Color.red;
    private int currentHealth;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        slider.value = maxHealth;

    }

    void Update()
    {
        HandleMovement();
        CheckEnemyDistance();
        slider.transform.LookAt(Camera.main.transform);
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                navMeshAgent.SetDestination(hit.point);
            }
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
        Debug.Log($"主角受到 {amount} 点伤害！当前血量：{currentHealth}");

        slider.value = currentHealth;
        fill.color = Color.Lerp(ZeroHealthColor, FullHealthColor, slider.normalizedValue);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("主角死亡！");
        // 可在此添加死亡动画或重置游戏逻辑
    }
}