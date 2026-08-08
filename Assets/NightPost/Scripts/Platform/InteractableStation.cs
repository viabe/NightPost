using UnityEngine;
using UnityEngine.EventSystems;

public abstract class InteractableStation : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float interactionOffsetX = 0.5f;
    [SerializeField] private PlayerInteraction playerInteraction;

    // 플레이어가 시설과 상호작용할 때 서 있어야 하는 위치를 외부에 제공
    public float InteractionX => transform.position.x + interactionOffsetX;

    // 현재 이 시설을 사용할 수 있는지 자식 클래스에서 판단
    public abstract bool CanInteract();

    // 시설마다 다른 실제 상호작용을 자식 클래스에서 구현
    public abstract void Interact();

    public void OnPointerClick(PointerEventData eventData)
    {
        // 화면을 눌러 이동하는 중에 손가락이 시설 위를 지나가면 의도치 않게 열린다.
        // 끌었던 입력은 이동으로만 보고 상호작용으로 치지 않는다.
        if (eventData != null && eventData.dragging) return;

        if (playerInteraction == null)
        {
            Debug.LogWarning("PlayerInteraction이 연결되지 않음");
            return;
        }

        // 플레이어에게 현재 클릭된 구조물인 자기 자신을 상호작용 대상으로 전달
        playerInteraction.RequestInteraction(this);
    }
}
