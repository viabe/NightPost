using UnityEngine;

// 플레이어의 이동 담당
// 1. 좌우 수동 이동
// 2. 시설까지의 자동 이동
// 3. 이동 범위 제한
// 4. 목표 지점 도착 판정
// 5. 이동 방향과 이동 상태 제공
// 6. 자동 이동 완료 이벤트 발생

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // 플레이어의 초당 이동 속도임
    [SerializeField] private float moveSpeed = 5;
    // 자동 이동 완료로 판정할 목표 지점과의 허용 거리임
    [SerializeField] private float stopDistance = 0.05f;
    // 플레이어가 이동할 수 있는 최소 X좌표임
    [SerializeField] private float minMoveX = -9;
    // 플레이어가 이동할 수 있는 최대 X좌표임
    [SerializeField] private float maxMoveX = 9;

    // 플레이어의 물리 이동을 처리하는 Rigidbody2D임
    private Rigidbody2D rigidbody;

    // 현재 수동 이동 입력값임
    private float manualMoveInput;
    // 자동 이동할 목표 X좌표임
    private float autoMoveTargetX;

    // 현재 수동 이동 중인지 나타내는 값임
    private bool isManualMoving;
    // 현재 자동 이동 중인지 나타내는 값임
    private bool isAutoMoving;
    // 플레이어가 마지막으로 이동한 방향임
    private float moveDirection = 1f;

    // 자동 이동이 정상적으로 완료되었을 때 발생함
    public event System.Action AutoMoveCompleted;
    // 진행 중인 자동 이동이 취소되었을 때 발생함
    public event System.Action AutoMoveCanceled;

    /// <summary>
    /// 플레이어의 물리 이동에 사용할 Rigidbody2D를 가져옴
    /// </summary>
    private void Awake()
    {
        // 같은 GameObject에 붙어 있는 Rigidbody2D를 가져옴
        // 실제 위치 이동은 Transform이 아니라 Rigidbody2D를 통해 처리함
        rigidbody = GetComponent<Rigidbody2D>();

    }
    /// <summary>
    /// 현재 이동 상태에 따라 자동 이동 또는 수동 이동을 처리함
    /// </summary>
    private void FixedUpdate()
    {
        // 자동 이동 중이라면 목표 위치를 향한 이동을 처리함
        if (isAutoMoving)
        {
            MoveAuto();
            return;
        }
        // 자동 이동 중이 아니라면 현재 수동 입력에 따른 이동을 처리함
        MoveManual();
    }
    /// <summary>
    /// 전달받은 수동 이동 입력을 저장하고 이동 방향을 갱신함
    /// </summary>
    public void SetManualMoveInput(float input)
    {
        // 전달받은 입력값을 -1부터 1까지의 범위로 제한함
        manualMoveInput = Mathf.Clamp(input, -1f, 1f);
        // 입력값이 0이라면 마지막 이동 방향을 유지하고 종료함
        if (Mathf.Approximately(manualMoveInput, 0f))
        {
            isManualMoving = false;
            return;
        }

        // 수동 이동 입력이 발생하면 진행 중인 자동 이동을 취소함
        CancelAutoMove();

        // 현재 입력 방향을 플레이어의 마지막 이동 방향으로 저장함
        moveDirection = manualMoveInput;

    }
    /// <summary>
    /// 현재 수동 입력에 따라 플레이어를 좌우로 이동시킴
    /// </summary>
    private void MoveManual()
    {
        // 현재 좌우 입력값이 0이면 이동할 필요가 없으므로 종료함
        if (Mathf.Approximately(manualMoveInput, 0f))
        {
            isManualMoving = false;
            return;
        }

        // Rigidbody2D에 저장된 현재 위치를 가져옴
        Vector2 currentPosition = rigidbody.position;

        // 이번 물리 프레임에 이동할 X축 거리를 계산함
        // 이동량은 입력 방향, 이동 속도, 물리 프레임 간격을 곱해 계산함
        float diff = manualMoveInput * moveSpeed * Time.fixedDeltaTime;

        // 현재 X좌표에 이동량을 더해 다음 X좌표를 계산함
        float calculatedX = currentPosition.x + diff;
        // 다음 X좌표를 플레이어의 이동 가능 범위 안으로 제한함
        float nextX = Mathf.Clamp(calculatedX, minMoveX, maxMoveX);

        // 수동 이동 중에는 현재 Y좌표를 유지함
        float nextY = currentPosition.y;

        // 실제로 위치가 변경되는지 확인함
        isManualMoving = !Mathf.Approximately(currentPosition.x, nextX);

        // 이동 범위 끝이라 실제로 움직이지 않는다면 종료함
        if (!isManualMoving) return;

        // 계산한 X좌표와 기존 Y좌표로 다음 위치를 생성함
        Vector2 nextPosition = new Vector2(nextX, nextY);
        // Rigidbody2D를 통해 계산된 다음 위치로 이동함
        rigidbody.MovePosition(nextPosition);
    }
    /// <summary>
    /// 전달받은 목표 X좌표를 향한 자동 이동을 시작함
    /// </summary>
    public void MoveTo(float targetX)
    {
        // 목표 X좌표를 이동 가능 범위 안으로 보정해 저장함
        autoMoveTargetX = Mathf.Clamp(targetX, minMoveX, maxMoveX);
        // 기존 수동 입력을 제거해 수동 이동을 중단함
        CancelManualMove();
        // 자동 이동 상태로 전환함
        isAutoMoving = true;
    }
    /// <summary>
    /// 저장된 목표 X좌표를 향해 플레이어를 자동으로 이동시킴
    /// </summary>
    private void MoveAuto()
    {
        // Rigidbody2D에 저장된 현재 위치를 가져옴
        Vector2 currentPosition = rigidbody.position;
        // 현재 위치에서 목표 X좌표까지 남은 거리를 계산함
        float distance = autoMoveTargetX - currentPosition.x;
        // 목표 지점과의 거리가 허용 거리 이내라면 자동 이동을 완료함
        if (Mathf.Abs(distance) <= stopDistance)
        {
            CompleteAutoMove();
            return;
        }

        // 목표 지점이 있는 방향으로 플레이어의 이동 방향을 갱신함
        moveDirection = Mathf.Sign(distance);
        // 목표 X좌표를 지나치지 않도록 다음 X좌표를 계산함
        float nextX = Mathf.MoveTowards(currentPosition.x, autoMoveTargetX, moveSpeed * Time.fixedDeltaTime);
        // 계산한 X좌표와 기존 Y좌표로 다음 위치를 생성함
        Vector2 nextPosition = new Vector2(nextX, currentPosition.y);
        // Y좌표를 유지하면서 Rigidbody2D를 통해 다음 위치로 이동함
        rigidbody.MovePosition(nextPosition);

    }
    /// <summary>
    /// 진행 중인 자동 이동을 취소하고 취소 이벤트를 발생시킴
    /// </summary>
    public void CancelAutoMove()
    {
        // 현재 자동 이동 중이 아니라면 취소할 필요가 없으므로 종료함
        if (!isAutoMoving) return;
        // 자동 이동 상태를 해제함
        isAutoMoving = false;
        // 자동 이동 취소 이벤트를 발생시킴
        AutoMoveCanceled?.Invoke();
    }
    /// <summary>
    /// 현재 남아 있는 수동 이동 입력을 제거함
    /// </summary>
    private void CancelManualMove()
    {
        // 수동 이동 입력을 0으로 변경해 수동 이동을 중단함
        manualMoveInput = 0;
        // 수동 이동 상태를 해제함
        isManualMoving = false;
    }
    /// <summary>
    /// 현재 진행 중인 수동 이동과 자동 이동을 모두 중단함
    /// </summary>
    public void StopAllMovement()
    {
        // 수동 이동 입력을 제거함
        manualMoveInput = 0f;
        // 자동 이동 상태를 해제함
        CancelAutoMove();
        // 수동 이동 상태를 해제함
        isManualMoving = false;
    }
    /// <summary>
    /// 플레이어를 목표 위치에 배치하고 자동 이동 완료 이벤트를 발생시킴
    /// </summary>
    private void CompleteAutoMove()
    {
        // Rigidbody2D에 저장된 현재 위치를 가져옴
        Vector2 currentPosition = rigidbody.position;
        // 목표 X좌표와 현재 Y좌표를 사용해 최종 위치를 생성함
        Vector2 targetPosition = new Vector2(autoMoveTargetX, currentPosition.y);

        // 플레이어를 목표 X좌표에 정확히 배치함
        rigidbody.MovePosition(targetPosition);
        // 자동 이동 상태를 해제함
        isAutoMoving = false;

        // 자동 이동 완료 이벤트를 발생시킴
        AutoMoveCompleted?.Invoke();
    }
    //==================================================================
    // getter
    //==================================================================
    // 수동 이동 입력이 있거나 자동 이동 중인지 반환함
    public bool IsMoving => isAutoMoving || isManualMoving;
    // 현재 자동 이동 중인지 반환함
    public bool IsAutoMoving => isAutoMoving;
    // 플레이어의 마지막 이동 방향을 반환함
    public float MoveDirection => moveDirection;

}
