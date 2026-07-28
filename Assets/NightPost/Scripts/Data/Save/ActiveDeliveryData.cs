using UnityEngine;

[System.Serializable]
public class ActiveDeliveryData 
{
    // 배달 중인 편지 ID
    [SerializeField] private int letterID = 0;
    // 배정된 배달부 ID
    [SerializeField] private int courierID = 0;
    // 선택된 노선 ID
    [SerializeField] private int routeID = 0;
    // 배달 시작 시각
    [SerializeField] private long startedAtUnixTime = 0;
    // 배달 완료 예정 시각]
    [SerializeField] private long completeAtUnixTime = 0;

    public ActiveDeliveryData(int letterID, int courierID, int routeID, long startedAtUnixTime, long completeAtUnixTime)
    {
        this.letterID = letterID;
        this.courierID = courierID;
        this.routeID = routeID;
        this.startedAtUnixTime = startedAtUnixTime;
        this.completeAtUnixTime = completeAtUnixTime;
    }

    public int LetterID => letterID;
    public int CourierID => courierID;  
    public int RouteID => routeID;
    public long StartedAtUnixTime => startedAtUnixTime;
    public long CompleteAtUnixTime => completeAtUnixTime;

}

