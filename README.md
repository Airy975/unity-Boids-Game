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