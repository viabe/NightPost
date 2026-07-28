using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMoveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float moveDirection;
    public void OnPointerDown(PointerEventData eventData)
    {
        // 이동 버튼이 눌리면
        // 이 버튼에 설정된 이동 방향을 PlayerController에 전달한다.
        playerController.RequestManualMove(moveDirection);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 이동 버튼에서 손을 떼면
        // PlayerController에 수동 이동 정지를 요청한다.
        playerController.StopManualMove();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        playerController.StopManualMove();
    }
}
