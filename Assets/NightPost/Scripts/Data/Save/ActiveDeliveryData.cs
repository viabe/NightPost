using UnityEngine;

public class ActiveDeliveryData : ScriptableObject
{
    // 배달 고유 ID
    [SerializeField]private int deliveryID = 0;
    // 배달 중인 편지 ID
    [SerializeField] private int letterID = 0;
    // 배정된 배달부 ID
    [SerializeField] private int courierID = 0;
    // 선택된 노선 ID
    [SerializeField] private int roteID = 0;
    // 배달 시작 시각
    [SerializeField] private long startedAtUnixTime = 0;
    // 배달 완료 예정 시각]
    [SerializeField] private long completeAtUnixTime = 0;

    public int DeliveryID => deliveryID;
    public int LetterID => letterID;
    public int CourierID => courierID;  
    public int RoteID => roteID;
    public long StartedAtUnixTime => startedAtUnixTime;
    public long CompleteAtUnixTime => completeAtUnixTime;

}

