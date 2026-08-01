using System.Collections.Generic;
using UnityEngine;

// 플레이어의 저장 데이터를 보관하고 각 시스템에 필요한 데이터 조회 및 변경 기능을 제공함
public class PlayerDataManager : MonoBehaviour
{
    // 현재 게임에서 사용하는 플레이어 저장 데이터임
    private PlayerSaveData currentData;

    /// <summary>
    /// PlayerDataManager에서 사용할 플레이어 저장 데이터를 등록함
    /// </summary>
    public void Initialize(PlayerSaveData saveData)
    {
        // 전달받은 저장 데이터가 없다면 오류를 출력하고 초기화를 중단함
        if (saveData == null)
        {
            Debug.LogError(
                "[PlayerDataManager] 초기화할 PlayerSaveData가 없습니다.");
            return;
        }

        // 전달받은 저장 데이터를 현재 플레이어 데이터로 저장함
        currentData = saveData;
    }

    /// <summary>
    /// 지정한 배달부를 플레이어가 보유하고 있는지 확인함
    /// </summary>
    public bool IsCourierOwned(int courierID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 보유 여부를 확인할 수 없으므로 false를 반환함
        if (currentData == null) return false;

        // 보유 배달부 ID 목록이 없다면 보유한 배달부가 없는 것으로 처리함
        if (currentData.OwnedCourierIDs == null) return false;

        // 보유 배달부 ID 목록에 지정한 배달부 ID가 포함되어 있는지 반환함

        return currentData.OwnedCourierIDs.Contains(courierID);
    }

    /// <summary>
    /// 지정한 노선이 현재 해금되어 있는지 확인함
    /// </summary>
    public bool IsRouteUnlocked(int routeID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 해금 여부를 확인할 수 없으므로 false를 반환함
        if (currentData == null) return false;

        // 해금된 노선 ID 목록이 없다면 이용 가능한 노선이 없는 것으로 처리함
        if (currentData.UnlockedRouteIDs == null) return false;

        // 해금된 노선 ID 목록에 지정한 노선 ID가 포함되어 있는지 반환함
        return currentData.UnlockedRouteIDs.Contains(routeID);
    }

    /// <summary>
    /// 지정한 배달부가 현재 배달을 진행 중인지 확인함
    /// </summary>
    public bool IsCourierDelivering(int courierID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 진행 중인 배달 정보를 확인할 수 없으므로 false를 반환함
        if (currentData == null) return false;

        // 현재 진행 중인 배달 목록이 없다면 배달 중인 배달부가 없는 것으로 처리함
        if (currentData.ActiveDeliveryList == null) return false;

        // 현재 진행 중인 전체 배달 데이터를 순회함
        foreach (ActiveDeliveryData delivery in currentData.ActiveDeliveryList)
        {
            // 유효하지 않은 배달 데이터는 건너뜀
            if (delivery == null) continue;
            // 배달 데이터의 배달부 ID가 지정한 ID와 같다면 배달 중인 것으로 처리함
            if (delivery.CourierID == courierID) return true;
        }

        // 지정한 배달부가 진행 중인 배달에 포함되지 않았다면 false를 반환함
        return false;
    }

    /// <summary>
    /// 신규 편지 진행 데이터를 플레이어 저장 데이터에 추가함
    /// </summary>
    public bool AddLetterProgress(LetterProgressData progressData)
    {
        // 저장 데이터 또는 전달받은 편지 진행 데이터가 없다면 추가하지 않음
        if (currentData == null || progressData == null) return false;
        // 편지 진행 데이터 목록이 없다면 추가하지 않음
        if (currentData.LetterProgressesList == null) return false;
        // 동일한 편지의 진행 데이터가 이미 존재한다면 중복 추가하지 않음
        if (GetLetterProgress(progressData.LetterID) != null) return false;

        // 편지 진행 데이터 목록에 신규 데이터를 추가함
        currentData.LetterProgressesList.Add(progressData);
        // 편지 진행 데이터가 정상적으로 추가되었음을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 편지의 진행 데이터를 반환함
    /// </summary>
    public LetterProgressData GetLetterProgress(int letterID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 편지 진행 데이터를 조회할 수 없으므로 null을 반환함
        if (currentData == null) return null;
        // 편지 진행 목록이 없다면 저장된 편지 진행 데이터가 없으므로 null을 반환함
        if (currentData.LetterProgressesList == null) return null;
        // 저장된 전체 편지 진행 데이터를 순회함
        foreach (LetterProgressData letter in currentData.LetterProgressesList)
        {
            // 유효하지 않은 편지 진행 데이터는 건너뜀
            if (letter == null) continue;

            // 편지 ID가 지정한 ID와 같다면 해당 진행 데이터를 반환함
            if (letter.LetterID == letterID) return letter;
        }

        // 지정한 편지의 진행 데이터가 없다면 null을 반환함
        return null;
    }

    /// <summary>
    /// 플레이어가 보유한 전체 편지 진행 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<LetterProgressData> GetLetterProgresses()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 빈 목록을 반환함
        if (currentData == null) return System.Array.Empty<LetterProgressData>();
        // 편지 진행 데이터 목록이 없다면 빈 목록을 반환함
        if (currentData.LetterProgressesList == null) return System.Array.Empty<LetterProgressData>();
        // 편지 진행 데이터 목록을 읽기 전용 형태로 반환함
        return currentData.LetterProgressesList;
    }

    /// <summary>
    /// 진행 중인 배달 데이터를 플레이어 저장 데이터에 추가함
    /// </summary>
    public bool AddActiveDelivery(ActiveDeliveryData deliveryData)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 배달 데이터를 추가하지 않음
        if (currentData == null) return false;

        // 전달받은 배달 데이터가 없다면 추가하지 않음
        if (deliveryData == null) return false;

        // 현재 진행 중인 배달 목록이 없다면 데이터를 추가하지 않음
        if (currentData.ActiveDeliveryList == null) return false;

        // 현재 진행 중인 배달 목록에 전달받은 배달 데이터를 추가함
        currentData.ActiveDeliveryList.Add(deliveryData);

        // 배달 데이터가 정상적으로 추가되었음을 반환함
        return true;
    }

    /// <summary>
    /// 현재 진행 중인 전체 배달 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<ActiveDeliveryData> GetActiveDeliveries()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 null을 반환함
        if (currentData == null) return null;

        // 현재 진행 중인 배달 목록을 읽기 전용 형태로 반환함
        return currentData.ActiveDeliveryList;
    }

    /// <summary>
    /// 완료된 배달 결과 데이터를 플레이어 저장 데이터에 추가함
    /// </summary>
    public bool AddDeliveryResult(DeliveryResultData resultData)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 결과 데이터를 추가하지 않음
        if (currentData == null) return false;

        // 전달받은 배달 결과 데이터가 없다면 추가하지 않음
        if (resultData == null) return false;

        // 배달 결과 목록이 없다면 데이터를 추가하지 않음
        if (currentData.DeliveryResultsList == null) return false;

        // 배달 결과 목록에 전달받은 결과 데이터를 추가함
        currentData.DeliveryResultsList.Add(resultData);

        // 배달 결과가 정상적으로 추가되었음을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 진행 중 배달 데이터를 목록에서 제거함
    /// </summary>
    public bool RemoveActiveDelivery(ActiveDeliveryData deliveryData)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 진행 중인 배달을 제거하지 않음
        if (currentData == null) return false;

        // 전달받은 배달 데이터가 없다면 제거하지 않음
        if (deliveryData == null) return false;

        // 진행 중인 배달 목록이 없다면 데이터를 제거하지 않음
        if (currentData.ActiveDeliveryList == null) return false;

        // 진행 중인 배달 목록에서 전달받은 배달 데이터를 제거하고 결과를 반환함

        return currentData.ActiveDeliveryList.Remove(deliveryData);
    }

    /// <summary>
    /// 지정한 편지에 해당하는 배달 결과 데이터를 반환함
    /// </summary>
    public DeliveryResultData GetDeliveryResult(int letterID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 배달 결과를 조회할 수 없으므로 null을 반환함
        if (currentData == null) return null;

        // 배달 결과 목록이 없다면 조회할 데이터가 없으므로 null을 반환함
        if (currentData.DeliveryResultsList == null) return null;

        // 저장된 전체 배달 결과 데이터를 순회함
        foreach (DeliveryResultData deliveryData in currentData.DeliveryResultsList)
        {
            // 유효하지 않은 배달 결과 데이터는 건너뜀
            if (deliveryData == null) continue;
            // 편지 ID가 지정한 ID와 같다면 해당 배달 결과를 반환함
            if (deliveryData.LetterID == letterID) return deliveryData;
        }
        // 지정한 편지의 배달 결과가 없다면 null을 반환함
        return null;
    }

    /// <summary>
    /// 플레이어의 보유 재화를 지정한 수량만큼 증가시킴
    /// </summary>
    public bool AddCurrency(int amount)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 재화를 변경하지 않음
        if (currentData == null) return false;
        // 증가시킬 수량이 0 이하라면 재화를 변경하지 않음
        if (amount < 0) return false;

        // PlayerSaveData에 재화 증가를 요청하고 실패했다면 false를 반환함
        if (!currentData.AddCurrency(amount)) return false;

        // 변경된 현재 재화량을 전달하는 이벤트를 발생시킴
        GameEvents.RaiseCurrencyChanged(currentData.Currency);

        // 재화 증가 처리가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 금액만큼 플레이어의 재화를 차감함
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        // 현재 저장 데이터 연결 여부 확인
        if (currentData == null) return false;
        // 차감 금액이 음수인지 확인
        if (amount < 0) return false;
        // PlayerSaveData의 SpendCurrency 호출
        // 재화 차감에 실패하면 false 반환
        if (!currentData.SpendCurrency(amount)) return false;

        // 차감 금액이 0보다 큰 경우 현재 재화 변경 이벤트 발생
        if (amount > 0)
        {
            GameEvents.RaiseCurrencyChanged(currentData.Currency);
        }
        // 재화 차감 성공 반환
        return true;
    }
    /// <summary>
    /// 플레이어의 완료 배달 횟수를 1만큼 증가시킴
    /// </summary>
    public bool IncreaseCompletedDeliveryCount()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 완료 횟수를 변경하지 않음
        if (currentData == null) return false;

        // PlayerSaveData에 완료 배달 횟수 증가를 요청함
        currentData.IncreaseCompletedDeliveryCount();

        // 완료 배달 횟수가 정상적으로 증가했음을 반환함
        return true;
    }

    /// <summary>
    /// 아직 확인하지 않은 배달 결과 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<DeliveryResultData> GetUncheckedDeliveryResults()
    {
        // 확인하지 않은 배달 결과를 저장할 목록을 생성함
        List<DeliveryResultData> deliveryResultDatas = new List<DeliveryResultData>();

        // PlayerDataManager가 아직 초기화되지 않았다면 빈 목록을 반환함
        if (currentData == null) return deliveryResultDatas;
        // 배달 결과 목록이 없다면 빈 목록을 반환함
        if (currentData.DeliveryResultsList == null) return deliveryResultDatas;

        // 저장된 전체 배달 결과 데이터를 순회함
        foreach (DeliveryResultData deliveryResultData in currentData.DeliveryResultsList)
        {
            // 유효하지 않은 배달 결과 데이터는 건너뜀
            if (deliveryResultData == null) continue;
            // 아직 확인하지 않은 결과만 반환 목록에 추가함
            if (!deliveryResultData.IsChecked)
            {
                deliveryResultDatas.Add(deliveryResultData);
            }
        }

        // 확인하지 않은 배달 결과만 담긴 목록을 반환함
        return deliveryResultDatas;
    }

    /// <summary>
    /// 지정한 답장을 플레이어의 수신 답장 목록에 추가함
    /// </summary>
    public bool AddReceivedReply(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 받은 답장을 추가하지 않음
        if (currentData == null) return false;

        // PlayerSaveData에 답장 수신 등록을 요청함
        bool isReceived = currentData.AddReceivedReply(replyID);
        // 답장 수신 등록에 실패했다면 false를 반환함
        if (!isReceived) return false;
        // 답장을 수신했음을 알리는 이벤트를 발생시킴
        GameEvents.RaiseReplyReceived(replyID);
        // 변경된 읽지 않은 답장 개수를 전달하는 이벤트를 발생시킴
        GameEvents.RaiseUnreadReplyCountChanged(GetUnreadReplyIDs().Count);
        // 답장 수신 처리가 완료되었음을 반환함
        return true;

    }

    /// <summary>
    /// 지정한 답장을 읽음 상태로 변경함
    /// </summary>
    public bool MarkReplyAsRead(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 답장 읽음 상태를 변경하지 않음
        if (currentData == null) return false;

        // PlayerSaveData에 답장 읽음 상태 변경을 요청함
        bool isReceived = currentData.MarkReplyAsRead(replyID);
        // 답장 읽음 상태 변경에 실패했다면 false를 반환함
        if (!isReceived) return false;
        // 답장을 읽었음을 알리는 이벤트를 발생시킴
        GameEvents.RaiseReplyRead(replyID);
        // 변경된 읽지 않은 답장 개수를 전달하는 이벤트를 발생시킴
        GameEvents.RaiseUnreadReplyCountChanged(GetUnreadReplyIDs().Count);
        // 답장 읽음 상태 변경이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 답장을 플레이어가 읽었는지 확인함
    /// </summary>
    public bool IsReplyRead(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 읽음 여부를 확인할 수 없으므로 false를 반환함
        if (currentData == null) return false;
        // 읽은 답장 ID 목록이 없다면 읽은 답장이 없는 것으로 처리함
        if (currentData.ReadReplyIds == null) return false;
        // 읽은 답장 ID 목록에 지정한 답장 ID가 포함되어 있는지 반환함
        return currentData.ReadReplyIds.Contains(replyID);
    }

    /// <summary>
    /// 지정한 답장을 플레이어가 수신했는지 확인함
    /// </summary>
    public bool IsReplyReceived(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 수신 여부를 확인할 수 없으므로 false를 반환함
        if (currentData == null) return false;
        // 받은 답장 ID 목록이 없다면 수신한 답장이 없는 것으로 처리함
        if (currentData.ReceivedReplyIDs == null) return false;
        // 받은 답장 ID 목록에 지정한 답장 ID가 포함되어 있는지 반환함
        return currentData.ReceivedReplyIDs.Contains(replyID);
    }

    /// <summary>
    /// 플레이어가 수신했지만 아직 읽지 않은 답장 ID 목록을 반환함
    /// </summary>
    public IReadOnlyList<int> GetUnreadReplyIDs()
    {
        // 읽지 않은 답장 ID를 저장할 목록을 생성함
        List<int> replyList = new List<int>();
        // PlayerDataManager가 아직 초기화되지 않았다면 빈 목록을 반환함
        if (currentData == null) return replyList;
        // 받은 답장 ID 목록이 없다면 빈 목록을 반환함
        if (currentData.ReceivedReplyIDs == null) return replyList;

        // 플레이어가 수신한 전체 답장 ID를 순회함
        foreach (int replyID in currentData.ReceivedReplyIDs)
        {
            // 아직 읽지 않은 답장 ID만 반환 목록에 추가함
            if (!IsReplyRead(replyID)) replyList.Add(replyID);
        }
        // 읽지 않은 답장 ID 목록을 반환함
        return replyList;
    }

    /// <summary>
    /// 플레이어가 현재 보유한 재화량을 반환함
    /// </summary>
    public int GetCurrency()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 0을 반환함
        if (currentData == null) return 0;

        // 현재 플레이어의 보유 재화량을 반환함
        return currentData.Currency;
    }

    /// <summary>
    /// 플레이어가 아직 읽지 않은 답장 개수를 반환함
    /// </summary>
    public int GetUnreadReplyCount()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 0을 반환함
        if (currentData == null) return 0;
        // 읽지 않은 답장 ID 목록의 개수를 반환함
        return GetUnreadReplyIDs().Count;
    }

    /// <summary>
    /// 플레이어의 완료 배달 횟수를 반환함
    /// </summary>
    public int GetCompletedDeliveryCount()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 0을 반환함
        if (currentData == null) return 0;

        // 현재까지 완료한 배달 횟수를 반환함
        return currentData.CompleteDeliveryCount;
    }

    /// <summary>
    /// 지정한 배달부를 플레이어의 보유 배달부 목록에 추가함
    /// </summary>
    public bool AddOwnedCourier(int courierID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 배달부를 추가하지 않음
        if (currentData == null) return false;
        // 유효하지 않은 배달부 ID라면 추가하지 않음
        if (courierID <= 0) return false;
        // 보유 배달부 ID 목록이 없다면 추가하지 않음
        if (currentData.OwnedCourierIDs == null) return false;
        // 이미 보유한 배달부라면 중복 추가하지 않음
        if (currentData.OwnedCourierIDs.Contains(courierID)) return false;

        // 보유 배달부 ID 목록에 지정한 배달부 ID를 추가함
        currentData.OwnedCourierIDs.Add(courierID);
        // 배달부가 해금되었음을 알리는 이벤트를 발생시킴
        GameEvents.RaiseCourierUnlocked(courierID);

        // 배달부 추가 처리가 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 노선을 플레이어의 해금 노선 목록에 추가함
    /// </summary>
    public bool AddUnlockedRoute(int routeID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 노선을 해금하지 않음
        if (currentData == null) return false;
        // 유효하지 않은 노선 ID라면 해금하지 않음
        if (routeID <= 0) return false;
        // 해금된 노선 ID 목록이 없다면 노선을 추가하지 않음
        if (currentData.UnlockedRouteIDs == null) return false;
        // 이미 해금된 노선이라면 중복 추가하지 않음
        if (currentData.UnlockedRouteIDs.Contains(routeID)) return false;
        // 해금된 노선 ID 목록에 지정한 노선 ID를 추가함
        currentData.UnlockedRouteIDs.Add(routeID);
        // 노선이 해금되었음을 알리는 이벤트를 발생시킴
        GameEvents.RaiseRouteUnlocked(routeID);
        // 노선 해금 처리가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 시설의 진행 데이터를 조회함
    /// </summary>
    public FacilityProgressData GetFacilityProgress(int facilityID)
    {
        // 현재 저장 데이터 연결 여부 확인
        if(currentData == null) return null;
        // facilityID 유효성 확인
        if(facilityID <= 0) return null;

        // 시설 진행 목록 null 여부 확인
        if(currentData.FacilityProgressesList == null) return null;
        // 시설 진행 목록 순회
        foreach(FacilityProgressData facilityProgress in currentData.FacilityProgressesList)
        {
            // null 데이터 건너뜀
            if (facilityProgress == null) continue;
            // FacilityID가 일치하면 해당 진행 데이터 반환
            if(facilityProgress.FacilityID == facilityID) return facilityProgress;
        }

        // 일치하는 시설 진행 데이터가 없으면 null 반환
        return null;
    }
    /// <summary>
    /// 지정한 시설의 진행 데이터를 새로 추가함
    /// </summary>
    public bool AddFacilityProgress(int facilityID)
    {
        // 현재 저장 데이터 연결 여부 확인
        if (currentData == null) return false;
        // facilityID 유효성 확인
        if (facilityID <= 0) return false;
        // 시설 진행 목록 null 여부 확인
        if (currentData.FacilityProgressesList == null) return false;
        // 동일한 시설 진행 데이터가 이미 존재하는지 확인
        if (GetFacilityProgress(facilityID) != null) return false;

        // 신규 FacilityProgressData 생성
        FacilityProgressData facilityProgressData = new FacilityProgressData(facilityID);

        // 시설 진행 목록에 추가
        currentData.FacilityProgressesList.Add(facilityProgressData);

        // 추가 성공 반환
        return true;
    }
    /// <summary>
    /// 지정한 시설의 레벨을 1 증가시킴
    /// </summary>
    public bool IncreaseFacilityLevel(int facilityID, int maxLevel)
    {
        // 현재 저장 데이터 연결 여부 확인
        if (currentData == null) return false;
        // facilityID 유효성 확인
        if (facilityID <= 0) return false;

        // 최대 레벨 유효성 확인
        if(maxLevel <= 0) return false;

        // 기존 시설 진행 데이터 조회
        FacilityProgressData facilityProgressData = GetFacilityProgress(facilityID);

        // 시설 진행 데이터가 없으면 새로 추가
        if(facilityProgressData == null)
        {
            // 추가에 실패하면 false 반환
            if(!AddFacilityProgress(facilityID)) return false;
        }

        // 새로 추가된 시설 진행 데이터 다시 조회
        facilityProgressData = GetFacilityProgress(facilityID);

        // 시설 진행 데이터가 없으면 false 반환
        if (facilityProgressData == null) return false;
        // FacilityProgressData의 IncreaseLevel 호출
        // 레벨 증가 결과 반환
        return facilityProgressData.IncreaseLevel(maxLevel);
    }
}
