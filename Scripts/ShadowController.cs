using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour
{
    private List<Player.PlayerAction> recordedInputs; // �洢�����������
    private float actionDuration; // ��¼�Ķ�����ʱ��
    private float playbackTime = 0; // �����طŵ�������ʱ��
    private Rigidbody2D rb; // Ӱ�ӵĸ������
    private bool hasFinishedAction = false; // ��Ƕ����Ƿ���ȫ���ط����
    private bool hasLanded = false; // ���Ӱ���Ƿ�����أ����ڴ�����ʧ��
    private bool isJumping = false; // ���Ӱ���Ƿ�����Ծ�У���ֹ������

    // �ƶ�����������������ұ���һ�£�
    private float moveSpeed; // ˮƽ�ƶ��ٶ�
    private float jumpForce; // ��Ծ��
    private float groundCheckDistance; // ���������߳���
    private float diagonalCheckAngle; // б����Ƕ�
    private bool enableDualDiagonalCheck; // �Ƿ�����˫��б����
    private LayerMask groundLayer; // ����ͼ��
    private LayerMask shadowLayer; // Ӱ��ͼ�㣨����վ������Ӱ���ϣ�
    private LayerMask playerLayer; // ���ͼ�㣨���������ڼ���Ƿ�վ��������ϣ�

    // ��ʼ��Ӱ�ӣ�������ҵļ�¼���ݺͲ�����
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
        LayerMask player) // �������������ͼ�����
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
        playerLayer = player; // ��ֵ���ͼ��

        // ȷ��Ӱ���и��������û�����Զ���ӣ�
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // ����Ӱ���������������Ϊ��ҵ�2����������죩
        rb.gravityScale = 2;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ��������״̬���
        hasFinishedAction = false;
        hasLanded = false;
        isJumping = false;
    }

    void Update()
    {
        // ��û�������¼����Ӱ���������ʧ������ǰ�˳�
        if (recordedInputs == null || recordedInputs.Count == 0 || hasLanded)
            return;

        // �������״̬����Ծ��
        UpdateGroundedState();

        // �����ط���ɺ�������أ������������ϣ���������Ӱ��
        if (hasFinishedAction && IsGrounded())
        {
            hasLanded = true;
            FinishPlayback();
            return;
        }

        // ���»ط�ʱ�䣨���ٵ�ǰ�طŽ��ȣ�
        playbackTime += Time.deltaTime;

        // ���ط�ʱ�䳬��������ʱ������Ƕ��������
        if (playbackTime >= actionDuration && !hasFinishedAction)
        {
            hasFinishedAction = true;
            return;
        }

        // ������δ��ɣ�����ǰʱ��������
        if (!hasFinishedAction)
        {
            Player.PlayerAction currentInput = GetCurrentInput();
            // ֻ�������״̬������Ծ���Ҽ�⵽��Ծ����ʱ����ִ����Ծ
            if (currentInput.isJumpPressed && !isJumping)
            {
                Jump();
            }

            // Ӧ��ˮƽ�ƶ�
            Move(currentInput.horizontalInput);
        }
    }

    // ����Ӱ�ӵ����״̬����������Ծ��
    private void UpdateGroundedState()
    {
        bool isCurrentlyGrounded = IsGrounded(); // ����Ƿ�վ�ڵ���/Ӱ��/�������

        // ���ʱ������Ծ�������ٴ���Ծ��
        if (isCurrentlyGrounded && isJumping)
        {
            isJumping = false;
        }
        // �뿪����ʱ������Ծ����ֹ����������
        else if (!isCurrentlyGrounded && !isJumping)
        {
            isJumping = true;
        }
    }

    // ��ȡ��ǰ�ط�ʱ����Ӧ���������
    private Player.PlayerAction GetCurrentInput()
    {
        for (int i = 0; i < recordedInputs.Count; i++)
        {
            if (recordedInputs[i].time >= playbackTime)
            {
                return recordedInputs[i];
            }
        }
        // ��������¼��Χ���������һ������
        return recordedInputs[recordedInputs.Count - 1];
    }

    // Ӧ��ˮƽ�ƶ�
    private void Move(float input)
    {
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // �����ƶ�����ת����
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // ִ����Ծ���������״̬����Ч��
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true; // ������Ծ״̬��ֱ�����
    }

    // ���Ӱ���Ƿ�վ����Ч���棨����/Ӱ��/��ң�
    private bool IsGrounded()
    {
        // �ϲ����ͼ�㣺���� + Ӱ�� + ��ң��������ͼ�㣩
        LayerMask combinedDetectLayer = groundLayer | playerLayer;

        // ����б�����߷��򣨽Ƕ�ת���ȣ�
        float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
        Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 verticalDownDir = Vector2.down;

        // �������߼����ײ
        RaycastHit2D verticalHit = Physics2D.Raycast(transform.position, verticalDownDir, groundCheckDistance, combinedDetectLayer);
        RaycastHit2D leftDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, leftDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;
        RaycastHit2D rightDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, rightDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;

        // ���Ƶ�������
        DrawDebugRays(verticalDownDir, leftDiagonalDir, rightDiagonalDir);

        // ֻҪ��һ������������Ч���棨����/Ӱ��/��ң������ж�Ϊ���
        return verticalHit.collider != null || leftDiagHit.collider != null || rightDiagHit.collider != null;
    }

    // ���Ƶ������ߣ�������ͼ�ɼ���
    private void DrawDebugRays(Vector2 verticalDir, Vector2 leftDiagDir, Vector2 rightDiagDir)
    {
        // ��ֱ���ߣ���ɫ��
        Debug.DrawRay(transform.position, verticalDir * groundCheckDistance, Color.cyan);

        // б�����ߣ�Ʒ��=����ɫ=�ң�
        if (enableDualDiagonalCheck)
        {
            Debug.DrawRay(transform.position, leftDiagDir * groundCheckDistance, Color.magenta);
            Debug.DrawRay(transform.position, rightDiagDir * groundCheckDistance, Color.green);
        }
    }

    // �����������غ�����Ӱ��
    void FinishPlayback()
    {
        Destroy(gameObject);
    }

    // ���Ʊ༭ģʽ�µĳ־û� gizmo�����ڵ���������
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
            Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 verticalDownDir = Vector2.down;

            // ��ֱ���ߣ���ɫ��
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)verticalDownDir * groundCheckDistance);

            // б�����ߣ�Ʒ��=����ɫ=�ң�
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
