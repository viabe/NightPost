using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMovement playerMovement;

    // 캐릭터가 도착한 뒤 상호작용할 시설
    private InteractableStation pendingStation;

    private void OnEnable()
    {
        playerMovement.AutoMoveCompleted += OnAutoMoveCompleted;
        playerMovement.AutoMoveCanceled += CancelPendingInteraction;
    }
    private void OnDisable()
    {
        playerMovement.AutoMoveCompleted -= OnAutoMoveCompleted;
        playerMovement.AutoMoveCanceled -= CancelPendingInteraction;
    }
    public void RequestInteraction(InteractableStation station)
    {
        // 전달받은 상호작용 시설이 null이라면 요청을 처리하지 않고 종료
        if (station == null) return;

        // 현재 해당 시설과 상호작용할 수 없는 상태라면 이동 요청을 하지 않고 종료
        if (!station.CanInteract()) return;

        // 캐릭터가 도착한 뒤 실행할 시설을 pendingStation에 저장
        pendingStation = station;

        // 시설의 InteractionPoint X좌표를 가져와 PlayerController에 자동 이동을 요청
        playerController.RequestAutoMove(station.InteractionX);
    }
    private void CancelPendingInteraction()
    {
        pendingStation = null;
    }
    private void OnAutoMoveCompleted()
    {
        // 현재 도착 후 상호작용할 pendingStation이 없다면 종료
        if (pendingStation == null) return;
        InteractableStation station = pendingStation;
        pendingStation = null;

        // 이동하는 동안 시설 상태가 달라졌을 수 있으므로 현재도 상호작용 가능한지 다시 확인
        if (!station.CanInteract()) return;

        station.Interact();
    }
}
