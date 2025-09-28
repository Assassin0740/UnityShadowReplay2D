using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour
{
    private List<Player.PlayerAction> recordedInputs; // 存储玩家输入序列
    private float actionDuration; // 记录的动作总时长
    private float playbackTime = 0; // 动作回放的已流逝时间
    private Rigidbody2D rb; // 影子的刚体组件
    private bool hasFinishedAction = false; // 标记动作是否已全部回放完成
    private bool hasLanded = false; // 标记影子是否已落地（用于触发消失）
    private bool isJumping = false; // 标记影子是否在跳跃中（防止连跳）

    // 移动与地面检测参数（与玩家保持一致）
    private float moveSpeed; // 水平移动速度
    private float jumpForce; // 跳跃力
    private float groundCheckDistance; // 地面检测射线长度
    private float diagonalCheckAngle; // 斜向检测角度
    private bool enableDualDiagonalCheck; // 是否启用双向斜向检测
    private LayerMask groundLayer; // 地面图层
    private LayerMask shadowLayer; // 影子图层（用于站在其他影子上）
    private LayerMask playerLayer; // 玩家图层（新增：用于检测是否站在玩家身上）

    // 初始化影子（接收玩家的记录数据和参数）
    public void Initialize(
        List<Player.PlayerAction> inputs,
        float duration,
        float speed,
        float jump,
        float groundCheckLen,
        float diagAngle,
        bool enableDualDiag,
        LayerMask ground,
        LayerMask shadow,
        LayerMask player) // 新增：接收玩家图层参数
    {
        recordedInputs = inputs;
        actionDuration = duration;
        moveSpeed = speed;
        jumpForce = jump;
        groundCheckDistance = groundCheckLen;
        diagonalCheckAngle = diagAngle;
        enableDualDiagonalCheck = enableDualDiag;
        groundLayer = ground;
        shadowLayer = shadow;
        playerLayer = player; // 赋值玩家图层

        // 确保影子有刚体组件（没有则自动添加）
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // 配置影子物理参数（重力为玩家的2倍，下落更快）
        rb.gravityScale = 2;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 重置所有状态标记
        hasFinishedAction = false;
        hasLanded = false;
        isJumping = false;
    }

    void Update()
    {
        // 若没有输入记录，或影子已落地消失，则提前退出
        if (recordedInputs == null || recordedInputs.Count == 0 || hasLanded)
            return;

        // 更新落地状态和跳跃锁
        UpdateGroundedState();

        // 动作回放完成后，若已落地（地面或玩家身上），则销毁影子
        if (hasFinishedAction && IsGrounded())
        {
            hasLanded = true;
            FinishPlayback();
            return;
        }

        // 更新回放时间（跟踪当前回放进度）
        playbackTime += Time.deltaTime;

        // 当回放时间超过动作总时长，标记动作已完成
        if (playbackTime >= actionDuration && !hasFinishedAction)
        {
            hasFinishedAction = true;
            return;
        }

        // 若动作未完成，处理当前时间点的输入
        if (!hasFinishedAction)
        {
            Player.PlayerAction currentInput = GetCurrentInput();
            // 只有在落地状态（可跳跃）且检测到跳跃输入时，才执行跳跃
            if (currentInput.isJumpPressed && !isJumping)
            {
                Jump();
            }

            // 应用水平移动
            Move(currentInput.horizontalInput);
        }
    }

    // 更新影子的落地状态，并管理跳跃锁
    private void UpdateGroundedState()
    {
        bool isCurrentlyGrounded = IsGrounded(); // 检测是否站在地面/影子/玩家身上

        // 落地时解锁跳跃（允许再次跳跃）
        if (isCurrentlyGrounded && isJumping)
        {
            isJumping = false;
        }
        // 离开地面时锁定跳跃（防止空中连跳）
        else if (!isCurrentlyGrounded && !isJumping)
        {
            isJumping = true;
        }
    }

    // 获取当前回放时间点对应的玩家输入
    private Player.PlayerAction GetCurrentInput()
    {
        for (int i = 0; i < recordedInputs.Count; i++)
        {
            if (recordedInputs[i].time >= playbackTime)
            {
                return recordedInputs[i];
            }
        }
        // 若超出记录范围，返回最后一个输入
        return recordedInputs[recordedInputs.Count - 1];
    }

    // 应用水平移动
    private void Move(float input)
    {
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // 根据移动方向翻转精灵
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // 执行跳跃（仅在落地状态下有效）
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true; // 锁定跳跃状态，直到落地
    }

    // 检测影子是否站在有效表面（地面/影子/玩家）
    private bool IsGrounded()
    {
        // 合并检测图层：地面 + 影子 + 玩家（新增玩家图层）
        LayerMask combinedDetectLayer = groundLayer | playerLayer;

        // 计算斜向射线方向（角度转弧度）
        float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
        Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 verticalDownDir = Vector2.down;

        // 发射射线检测碰撞
        RaycastHit2D verticalHit = Physics2D.Raycast(transform.position, verticalDownDir, groundCheckDistance, combinedDetectLayer);
        RaycastHit2D leftDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, leftDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;
        RaycastHit2D rightDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, rightDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;

        // 绘制调试射线
        DrawDebugRays(verticalDownDir, leftDiagonalDir, rightDiagonalDir);

        // 只要有一条射线命中有效表面（地面/影子/玩家），就判定为落地
        return verticalHit.collider != null || leftDiagHit.collider != null || rightDiagHit.collider != null;
    }

    // 绘制调试射线（场景视图可见）
    private void DrawDebugRays(Vector2 verticalDir, Vector2 leftDiagDir, Vector2 rightDiagDir)
    {
        // 垂直射线（青色）
        Debug.DrawRay(transform.position, verticalDir * groundCheckDistance, Color.cyan);

        // 斜向射线（品红=左，绿色=右）
        if (enableDualDiagonalCheck)
        {
            Debug.DrawRay(transform.position, leftDiagDir * groundCheckDistance, Color.magenta);
            Debug.DrawRay(transform.position, rightDiagDir * groundCheckDistance, Color.green);
        }
    }

    // 动作完成且落地后，销毁影子
    void FinishPlayback()
    {
        Destroy(gameObject);
    }

    // 绘制编辑模式下的持久化 gizmo（便于调整参数）
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
            Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 verticalDownDir = Vector2.down;

            // 垂直射线（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)verticalDownDir * groundCheckDistance);

            // 斜向射线（品红=左，绿色=右）
            if (enableDualDiagonalCheck)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftDiagonalDir * groundCheckDistance);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightDiagonalDir * groundCheckDistance);
            }
        }
    }
}
