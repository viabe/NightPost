using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    private bool isControlEnabled = true;

    public void RequestManualMove(float direction)
    {
        // 현재 플레이어 조작이 비활성화된 상태라면 수동 이동 요청을 전달하지 않고 함수를 종료
        if (!isControlEnabled) return;
        // 조작 가능한 상태라면 전달받은 이동 방향을 PlayerMovement의 수동 이동 입력 함수에 전달
        playerMovement.SetManualMoveInput(direction);
    }

    public void StopManualMove()
    {
        // 플레이어가 현재 조작 불가능한 상태인지와 관계없이남아 있는 수동 이동 입력을 확실하게 제거
        playerMovement.SetManualMoveInput(0);
    }

    public void SetControlEnabled(bool enabled)
    {
        // 전달받은 값을 현재 플레이어 조작 가능 상태로 저장
        isControlEnabled = enabled;

        // 조작을 활성화하는 경우에는
        // 이후 들어오는 이동 요청을 허용하기만 하고 함수를 종료
        if (isControlEnabled) return;

        // 조작을 비활성화하는 경우에는
        // 현재 남아 있는 수동 이동과 자동 이동을 모두 중단
        playerMovement.StopAllMovement();

    }

    public void RequestAutoMove(float targetX)
    {
        // 현재 플레이어 조작이 비활성화되어 있다면
        // 자동 이동 요청을 처리하지 않고 종료
        if (!isControlEnabled) return;
        // 조작 가능한 상태라면 전달받은 목표 X좌표를
        // PlayerMovement의 자동 이동 시작 함수에 전달
        playerMovement.MoveTo(targetX);
    }
}
