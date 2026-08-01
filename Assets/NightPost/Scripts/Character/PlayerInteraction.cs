using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // 플레이어의 수동 이동과 자동 이동 요청을 전달하는 컨트롤러임
    [SerializeField] private PlayerController playerController;
    // 자동 이동 완료 및 취소 상태를 제공하는 이동 컴포넌트임
    [SerializeField] private PlayerMovement playerMovement;

    // 캐릭터가 도착한 뒤 상호작용할 시설임
    private InteractableStation pendingStation;

    /// <summary>
    /// 자동 이동 완료 및 취소 이벤트를 등록함
    /// </summary>
    private void OnEnable()
    {
        // 자동 이동 완료 시 대기 중인 시설과의 상호작용을 처리하도록 등록함
        playerMovement.AutoMoveCompleted += OnAutoMoveCompleted;
        // 자동 이동 취소 시 대기 중인 상호작용을 제거하도록 등록함
        playerMovement.AutoMoveCanceled += CancelPendingInteraction;
    }
    /// <summary>
    /// 자동 이동 완료 및 취소 이벤트 등록을 해제함
    /// </summary>
    private void OnDisable()
    {
        // 자동 이동 완료 이벤트 등록을 해제함
        playerMovement.AutoMoveCompleted -= OnAutoMoveCompleted;
        // 자동 이동 취소 이벤트 등록을 해제함
        playerMovement.AutoMoveCanceled -= CancelPendingInteraction;
    }
    /// <summary>
    /// 전달받은 시설로 자동 이동한 뒤 상호작용하도록 요청함
    /// </summary>
    public void RequestInteraction(InteractableStation station)
    {
        // 전달받은 상호작용 시설이 null이라면 요청을 처리하지 않고 종료함
        if (station == null) return;

        // 현재 해당 시설과 상호작용할 수 없는 상태라면 이동 요청을 처리하지 않고 종료함
        if (!station.CanInteract()) return;

        // 캐릭터가 도착한 뒤 상호작용할 시설을 저장함
        pendingStation = station;

        // 시설의 상호작용 지점 X좌표로 자동 이동을 요청함
        playerController.RequestAutoMove(station.InteractionX);
    }
    /// <summary>
    /// 자동 이동 취소 시 대기 중인 시설 상호작용을 제거함
    /// </summary>
    private void CancelPendingInteraction()
    {
        // 저장된 상호작용 대기 시설을 제거함
        pendingStation = null;
    }
    /// <summary>
    /// 자동 이동 완료 후 대기 중인 시설과 상호작용함
    /// </summary>
    private void OnAutoMoveCompleted()
    {
        // 도착 후 상호작용할 시설이 없다면 종료함
        if (pendingStation == null) return;
        // 대기 중인 시설을 지역 변수에 저장함
        InteractableStation station = pendingStation;
        // 중복 상호작용을 방지하기 위해 대기 중인 시설을 제거함
        pendingStation = null;

        // 이동 중 시설 상태가 변경되었을 수 있으므로 상호작용 가능 여부를 다시 확인함
        if (!station.CanInteract()) return;

        // 시설의 상호작용 기능을 실행함
        station.Interact();
    }
}
