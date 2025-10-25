# 基于鸟群算法的跟随系统实现
本项目为一款受鸟群算法启发的Unity游戏原型。玩家可通过鼠标点击在随机生成的障碍物地图中操控主角移动，系统基于Unity内置的NavMesh寻路实现自动路径规划。当主角接近其他幸存者时，幸存者会自动加入队伍并进行跟随，形成动态的群体移动行为；当队伍靠近敌人时，主角与跟随者将自动进行攻击与受伤判定，模拟出简易的群体协作与战斗机制。

## 游戏展示
![](https://github.com/Airy975/unity-Boids-Game/blob/main/image/1.png)
![](https://github.com/Airy975/unity-Boids-Game/blob/main/image/2.png)
![](https://github.com/Airy975/unity-Boids-Game/blob/main/image/3.png)

## 鸟群跟随与群体协同逻辑
在整个群体的控制系统中，Survivor脚本扮演了核心角色。它通过跟随主角、保持间距、避障与群体分离等逻辑，实现了自然流畅的群体移动效果。

### 开始跟随
群体成员会在检测到主角进入自身的触发范围后，自动进入“跟随模式”。在Update()方法中，系统会不断计算成员与主角的距离
```csharp
float distanceToPlayer = Vector3.Distance(transform.position, player.position);

if (!isFollowing && distanceToPlayer <= detectionRange)
{
    isFollowing = true;

    if (OutlineLogic != null)
        OutlineLogic.enabled = false;
}
```
当主角进入范围内，当前个体将启动跟随逻辑，并关闭高亮效果。随后，它会自动朝向主角后方的目标点移动，以保持距离。
```csharp
Vector3 targetPos = player.position - player.forward * followDistance;
Vector3 moveDir = (targetPos - transform.position).normalized;
```

### 群体分离与协同移动
为了防止群体成员之间发生重叠，脚本拥有分离力机制，通过检测周围同伴的距离来计算偏移方向
```csharp
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
            float strength = Mathf.Clamp01(1f - distance / separationDistance);
            separationDir += diff.normalized * strength;
            neighborCount++;
        }
    }

    if (neighborCount > 0)
        separationDir /= neighborCount;

    return separationDir.normalized;
}
```
当成员之间距离过近时，会施加反向推力，使它们彼此分离。推力与距离成反比，越靠近的个体分离力越强。最终移动方向由两部分合成：
最终方向 = 跟随主角的方向 + 分离力修正方向（权重较小）
```csharp
Vector3 finalDir = (moveDir + separationSmooth * 0.5f).normalized;
```
这种“微分离 + 群体协调”的设计，使整个鸟群在跟随时既保持整体队形，又避免个体重叠。

### 避障系统
当成员前方出现障碍物时，会自动检测并调整方向以避开障碍
```csharp
void ApplyObstacleAvoidance(ref Vector3 moveDir)
{
    RaycastHit hit;
    Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

    if (Physics.Raycast(rayOrigin, transform.forward, out hit, obstacleAvoidDistance, obstacleLayer))
    {
        Vector3 avoidDir = Vector3.Reflect(transform.forward, hit.normal);
        avoidDir.y = 0;
        moveDir = Vector3.Lerp(moveDir, avoidDir.normalized, Time.deltaTime * avoidForce);
    }
}
```

## 主角与敌人的导航寻路
在该系统中，主角与敌人均通过NavMeshAgent实现智能寻路与动态交互。

### 主角移动
主角的移动逻辑由NavMeshAgent控制，玩家通过鼠标点击场景来实现导航寻路。
```csharp
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
```
玩家在场景中点击任意可行走区域，系统会使用Physics.Raycast()从摄像机发射射线检测地面碰撞点，并将该点设置为NavMeshAgent的目标位置。这样主角就会自动计算最优路径并平滑地移动到目标点。

### 敌人自动寻路
敌人的智能行为由EnemyLogic脚本控制，包含检测 → 追击 → 攻击 → 停止四个阶段。
敌人会实时检测主角距离，并根据范围自动切换寻路状态。
```csharp
void HandleMovement()
{
    float distanceToPlayer = Vector3.Distance(transform.position, player.position);

    if (distanceToPlayer <= detectionRange)
        agent.SetDestination(player.position);
    else if (distanceToPlayer > missingRange)
        agent.ResetPath();
}
```
当主角进入 detectionRange 范围内时，敌人开始追踪玩家。

当主角超出 missingRange，敌人会停止移动，保持原地状态。

## 幸存者、敌人与障碍物的自动生成系统
在整个场景的初始化流程中，MainLogic 脚本负责在游戏开始时随机生成幸存者、敌人以及障碍物。通过可配置的生成范围与数量参数，系统能够在每次运行时产生动态变化、随机分布的战斗环境。

### 自动生成的核心逻辑
系统的入口位于Start() 方法中，当游戏运行时，会自动执行以下步骤
```csharp
void Start()
{
    // 检查是否设置预制体
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

    // 随机生成敌人数量
    int spawnEnemyCount = Random.Range(minCount, maxCount + 1);
    for (int i = 0; i < spawnEnemyCount; i++)
    {
        Vector3 randomPos = GetRandomPosition();
        Instantiate(EnemyPrefab, randomPos, Quaternion.identity);
    }
}
```
系统首先判断是否指定了对应的预制体（防止空引用）。

使用 Random.Range() 在 minCount 与 maxCount 之间生成一个随机数量。

每个对象在指定的areaSize内随机生成。

使用 Instantiate() 实例化预制体，位置随机，朝向默认。

这样，每次游戏启动都会在地面上随机生成数量不等的幸存者与敌人，从而实现“动态开局”的效果。

### 随机生成位置计算
随机位置的计算由GetRandomPosition()方法完成，确保生成点在设定区域内且避免穿地。
```csharp
Vector3 GetRandomPosition()
{
    Vector3 center = ground != null ? ground.position : transform.position;
    float randomX = Random.Range(-areaSize.x / 2, areaSize.x / 2);
    float randomZ = Random.Range(-areaSize.z / 2, areaSize.z / 2);
    return new Vector3(center.x + randomX, center.y + yOffset, center.z + randomZ);
}
```
这段代码以地面对象或脚本挂载点为中心，计算出生成区域的边界范围。之后通过Random.Range()生成随机的 X 与 Z 坐标，使对象分布在一个矩形区域中。最后使用yOffset参数略微抬高生成位置，避免模型与地面穿插。

例如，当areaSize = (10, 0, 10) 时，系统会在以中心点为原点的 10x10 区域内均匀分布对象。
