using UnityEngine;
using UnityEngine.Events;

// 플레이어가 시설에 도착했을 때 인스펙터에 연결된 UI 기능을 실행하는 공통 상호작용 시설임
public class UIInteractionStation : InteractableStation
{
    // 현재 시설의 상호작용 가능 여부임
    [SerializeField] private bool isInteractable = true;

    // 상호작용 완료 시 인스펙터에서 연결한 UI 열기 기능을 실행함
    [SerializeField] private UnityEvent onInteractionRequested;

    /// <summary>
    /// 현재 UI 상호작용 시설을 이용할 수 있는지 반환함
    /// </summary>
    public override bool CanInteract()
    {
        // 현재 설정된 상호작용 가능 상태를 반환함
        return isInteractable;
    }

    /// <summary>
    /// 플레이어가 시설에 도착하면 인스펙터에 연결된 UI 기능을 실행함
    /// </summary>
    public override void Interact()
    {
        // 현재 시설이 상호작용 불가능한 상태라면 요청을 실행하지 않음
        if (!CanInteract()) return;
        // 인스펙터에 연결된 UI 열기 기능을 호출함
        onInteractionRequested?.Invoke();
    }
}
