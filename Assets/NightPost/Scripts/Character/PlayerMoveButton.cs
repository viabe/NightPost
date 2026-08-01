using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMoveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // 플레이어의 이동 요청을 전달할 컨트롤러임
    [SerializeField] private PlayerController playerController;
    // 버튼에 설정된 이동 방향 값임
    [SerializeField] private float moveDirection;
    /// <summary>
    /// 포인터가 버튼을 누르면 설정된 방향으로 수동 이동을 요청함
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 이 버튼에 설정된 이동 방향을 PlayerController에 전달함
        playerController.RequestManualMove(moveDirection);
    }

    /// <summary>
    /// 포인터가 버튼에서 떨어지면 수동 이동을 중단함
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        // PlayerController에 수동 이동 정지를 요청함
        playerController.StopManualMove();
    }

    /// <summary>
    /// 포인터가 버튼 영역을 벗어나면 수동 이동을 중단함
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 버튼을 누른 상태로 영역을 벗어나는 경우 남아 있는 이동 입력을 제거함
        playerController.StopManualMove();
    }
}
