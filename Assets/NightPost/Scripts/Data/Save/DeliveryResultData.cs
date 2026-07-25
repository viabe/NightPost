using UnityEngine;

public class DeliveryResultData : ScriptableObject
{
    // 원본 배달 ID
    [SerializeField] private int deliveryID = 0;
    // 완료된 편지 ID
    [SerializeField] private int letterID = 0;
    // 실제 지급할 보상
    [SerializeField] private int rewardAmount = 0;
    // 배달 완료 시각
    [SerializeField] private long completedAtUnixTime = 0;
    // 플레이어가 결과를 확인했는지
    [SerializeField] private bool isChecked = false;

    public int DeliveryID => deliveryID;
    public int LetterID => letterID;
    public int RewardAmount => rewardAmount;
    public long CompletedAtUnixTime => completedAtUnixTime;
    public bool IsChecked => isChecked;
}

