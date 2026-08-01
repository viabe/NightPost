using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(
    fileName = "PlayerSaveData",
    menuName = "NightPost/Save/Player Save Data")]
public class PlayerSaveData : ScriptableObject
{
    // 플레이어가 보유한 재화
    [SerializeField] private int currency = 0;
    // 누적 배달 완료 수
    [SerializeField] private int completedDeliveryCount = 0;
    // 실제 고용한 배달부 ID
    [SerializeField] private List<int> ownedCourierIDs = new();
    // 현재 이용 가능한 노선 ID
    [SerializeField] private List<int> unlockedRouteIDs = new();
    // 편지별 진행 상태
    [SerializeField] private List<LetterProgressData> letterProgressList = new();
    // 현재 진행 중인 배달
    [SerializeField] private List<ActiveDeliveryData> activeDeliveryList = new();
    // 완료됐지만 결과 확인이 필요한 배달
    [SerializeField] private List<DeliveryResultData> deliveryResultsList = new();
    // 설비별 현재 레벨
    [SerializeField] private List<FacilityProgressData> facilityProgressesList = new();
    // 획득한 답장 ID
    [SerializeField] private List<int> receivedReplyIDs = new();
    // 읽은 답장 ID
    [SerializeField] private List<int> readReplyIDs = new();
    // 마지막 저장 시각
    [SerializeField] private long lastSaveUnixTime;


    public int Currency => currency;
    public int CompleteDeliveryCount => completedDeliveryCount;
    public List<int> OwnedCourierIDs => ownedCourierIDs;
    public List<int> UnlockedRouteIDs => unlockedRouteIDs;
    public List<LetterProgressData> LetterProgressesList => letterProgressList;
    public List<ActiveDeliveryData> ActiveDeliveryList => activeDeliveryList;
    public List<DeliveryResultData> DeliveryResultsList => deliveryResultsList;
    public List<FacilityProgressData> FacilityProgressesList => facilityProgressesList;
    public List<int> ReceivedReplyIDs => receivedReplyIDs;
    public List<int> ReadReplyIds => readReplyIDs;
    public long LastSaveUnixTime => lastSaveUnixTime;
    public bool AddCurrency(int amount)
    {
        if (amount <= 0) return false;
        currency += amount;
        return true;
    }
    /// <summary>
    /// 지정한 금액만큼 플레이어의 재화를 차감함
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        // 차감 금액이 음수이면 실패 처리
        if (amount < 0) return false;
        // 현재 재화가 차감 금액보다 적으면 실패 처리
        if (currency < amount) return false;
        // 현재 재화에서 금액 차감
        currency -= amount;
        // 차감 성공 반환
        return true;
    }
    public void IncreaseCompletedDeliveryCount()
    {
        completedDeliveryCount++;
    }
    public bool AddReceivedReply(int replyID)
    {
        if (replyID <= 0) return false;
        if (receivedReplyIDs == null) return false;
        if (receivedReplyIDs.Contains(replyID)) return false;

        receivedReplyIDs.Add(replyID);
        return true;
    }
    public bool MarkReplyAsRead(int replyID)
    {
        // replyID가 유효하지 않다면 false를 반환
        if(replyID <= 0) return false;

        // 받은 답장 목록 또는 읽은 답장 목록이 null이라면
        // 상태를 변경할 수 없으므로 false를 반환
        if(receivedReplyIDs == null || readReplyIDs == null) return false;

        // 아직 받지 않은 답장이라면
        // 읽음 상태로 변경할 수 없으므로 false를 반환
        if (!receivedReplyIDs.Contains(replyID)) return false;

        // 이미 읽은 답장이라면
        // 중복 등록을 막기 위해 false를 반환
        if (readReplyIDs.Contains(replyID)) return false;

        // 읽은 답장 ID 목록에 replyID를 추가
        readReplyIDs.Add(replyID);
        return true;
    }
}
