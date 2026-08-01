using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    private bool isControlEnabled = true;

    /// <summary>
    /// 전달받은 방향으로 플레이어의 수동 이동을 요청함
    /// </summary>
    public void RequestManualMove(float direction)
    {
        // 현재 플레이어 조작이 비활성화된 상태라면 수동 이동 요청을 처리하지 않고 종료함
        if (!isControlEnabled) return;
        // 전달받은 이동 방향을 PlayerMovement에 전달함
        playerMovement.SetManualMoveInput(direction);
    }

    /// <summary>
    /// 현재 남아 있는 플레이어의 수동 이동 입력을 제거함
    /// </summary>
    public void StopManualMove()
    {
        // 조작 가능 여부와 관계없이 수동 이동 입력을 0으로 변경함
        playerMovement.SetManualMoveInput(0);
    }

    /// <summary>
    /// 플레이어의 조작 가능 상태를 변경함
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        // 전달받은 값을 현재 플레이어 조작 가능 상태로 저장함
        isControlEnabled = enabled;

        // 조작을 활성화한 경우 이후 이동 요청을 허용하고 종료함
        if (isControlEnabled) return;

        // 조작을 비활성화한 경우 현재 진행 중인 수동 이동과 자동 이동을 모두 중단함
        playerMovement.StopAllMovement();

    }

    /// <summary>
    /// 전달받은 목표 X좌표로 플레이어의 자동 이동을 요청함
    /// </summary>
    public void RequestAutoMove(float targetX)
    {
        // 현재 플레이어 조작이 비활성화된 상태라면 자동 이동 요청을 처리하지 않고 종료함
        if (!isControlEnabled) return;
        // 전달받은 목표 X좌표를 PlayerMovement에 전달하여 자동 이동을 시작함
        playerMovement.MoveTo(targetX);
    }
}
