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
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float stopDistance = 0.05f;
    [SerializeField] private float minMoveX = -9;
    [SerializeField] private float maxMoveX = 9;

    private Rigidbody2D rigidbody;

    private float manualMoveInput;
    private float autoMoveTargetX;

    private bool isManualMoving;
    private bool isAutoMoving;
    private float moveDirection = 1f;

    public event System.Action AutoMoveCompleted;

    private void Awake()
    {
        // 같은 GameObject에 붙어 있는 Rigidbody2D를 가져옴
        // 실제 위치 이동은 Transform이 아니라 Rigidbody2D를 통해 처리
        rigidbody = GetComponent<Rigidbody2D>();

    }
    private void FixedUpdate()
    {
        // 자동이동 중인경우 목표 위치로 이동
        if(isAutoMoving)
        {
            MoveAuto();
            return;
        }
        // 자동이동이 아니라면 버튼을 통해 이동
        MoveManual();
    }
    public void SetManualMoveInput(float input)
    {
        // 들어온 값을 -1~1 범위로 제한한다.
        manualMoveInput = Mathf.Clamp(input, -1f, 1f);
        // 방향은 유지하되 손을 땐 상태
        if (Mathf.Approximately(manualMoveInput, 0f)) return;

        CancelAutoMove();

        // 마지막 방향
        moveDirection = manualMoveInput;

    }
    private void MoveManual()
    {
        // 현재 좌우 입력값이 0이면 이동할 필요가 없으므로 종료한다.
        if (Mathf.Approximately(manualMoveInput, 0f)) return;

        // 현재 위치
        Vector2 currentPosition = rigidbody.position;

        // 이번 물리 프레임에 이동할 X축 거리를 계산한다.
        // 이동량 = 입력 방향 × 이동 속도 × 물리 프레임 간격
        float diff = manualMoveInput * moveSpeed * Time.fixedDeltaTime;

        // 다음 좌표
        float calculatedX = currentPosition.x + diff;
        float nextX = Mathf.Clamp(calculatedX, minMoveX, maxMoveX);

        float nextY = currentPosition.y;

        Vector2 nextPosition = new Vector2(nextX, nextY);
        rigidbody.MovePosition(nextPosition);
    }
    public void MoveTo(float targetX)
    {
        // 목표 X좌표를 이동 가능 범위 안으로 보정해 저장한다.
        autoMoveTargetX = Mathf.Clamp(targetX, minMoveX, maxMoveX);
        // 기존 수동 입력을 제거해 수동 이동을 중단한다.
        CancelManualMove();
        // 자동 이동 상태로 전환한다.
        isAutoMoving = true;
    }
    private void MoveAuto()
    {
        // 현재 위치
        Vector2 currentPosition = rigidbody.position;
        // 현재 위치에서 목표까지 남은 거리를 계산
        float distance = autoMoveTargetX - currentPosition.x;
        // 목표 지점에 충분히 가까우면 자동 이동을 완료
        if(Mathf.Abs(distance) <= stopDistance)
        {
            CompleteAutoMove();
            return;
        }

        // 목표가 있는 방향으로 바라보는 방향을 갱신
        moveDirection = Mathf.Sign(distance);
        // 목표 X좌표를 지나치지 않도록 다음 X좌표를 계산
        float nextX = Mathf.MoveTowards(currentPosition.x, autoMoveTargetX, moveSpeed * Time.fixedDeltaTime);
        Vector2 nextPosition = new Vector2(nextX, currentPosition.y);
        // Y좌표는 그대로 유지하면서 이동
        rigidbody.MovePosition(nextPosition);

    }
    public void CancelAutoMove()
    {
        isAutoMoving = false;
    }
    private void CancelManualMove()
    {
        manualMoveInput = 0;
    }
    public void StopAllMovement()
    {
        manualMoveInput = 0f;
        isAutoMoving = false;
    }
    private void CompleteAutoMove()
    {
        // 목표 X좌표에 정확히 위치시킨다.
        Vector2 currentPosition = rigidbody.position;
        Vector2 targetPosition = new Vector2(autoMoveTargetX, currentPosition.y);

        rigidbody.MovePosition(targetPosition);
        // 자동 이동을 종료한다.
        isAutoMoving = false;

        // 이후 자동 이동 완료 이벤트를 발생시킨다.
        AutoMoveCompleted?.Invoke();
    }
    //==================================================================
    // getter
    //==================================================================
    public bool IsMoving => isAutoMoving || manualMoveInput != 0f;
    public bool IsAutoMoving => isAutoMoving;
    public float MoveDirection => moveDirection;
   
}
