using UnityEngine;

public class SortingStation : InteractableStation
{
    [SerializeField] private bool isInteractable = true;
    public override bool CanInteract()
    {
        return isInteractable;
    }
    public override void Interact()
    {
        // 플레이어가 분류대 위치에 정상적으로 도착한 뒤
        // 상호작용이 실행됐는지 확인할 수 있도록
        // 콘솔에 분류대 상호작용 완료 로그를 출력한다.
        Debug.Log("interact");
    }
}
