using UnityEngine;

// 해금 조건 
[System.Serializable]
public class UnlockConditionData
{
    // 게임 시작 시 기본 해금 여부
    [SerializeField] private bool unlockedByDefault = false;
    // 필요한 누적 배달 완료 수
    [SerializeField, Min(0)] private int requiredCompletedDeliveryCount = 0;

    public bool IsUnlockedByDefault => unlockedByDefault;
    public int RequiredCompletedDeliveryCount => requiredCompletedDeliveryCount;
}
