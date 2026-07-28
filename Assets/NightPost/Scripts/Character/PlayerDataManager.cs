using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    private PlayerSaveData currentData;

    public void Initialize(PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError(
                "[PlayerDataManager] 초기화할 PlayerSaveData가 없습니다.");
            return;
        }

        currentData = saveData;
    }
    public bool IsCourierOwned(int courierID)
    {
        // PlayerDataManager가 아직 초기화되지 않아
        // currentData가 없다면 false를 반환
        if (currentData == null) return false;

        // 보유 배달부 ID 목록이 null이라면
        // 보유한 배달부가 없는 것으로 처리
        if(currentData.OwnedCourierIDs == null) return false;

        // 보유 배달부 ID 목록에 전달받은 courierID가
        // 포함되어 있는지 확인해 결과를 반환
        
        return currentData.OwnedCourierIDs.Contains(courierID);
    }
    public bool IsRouteUnlocked(int routeID)
    {
        // PlayerDataManager가 아직 초기화되지 않아
        // currentData가 없다면 false를 반환
        if(currentData == null) return false;

        // 해금된 노선 ID 목록이 null이라면
        // 이용 가능한 노선이 없는 것으로 처리
        if(currentData.UnlockedRouteIDs == null) return false;

        // 해금된 노선 목록에 전달받은 routeID가
        // 포함되어 있는지 확인해 결과를 반환
        return currentData.UnlockedRouteIDs.Contains(routeID);
    }

    public bool IsCourierDelivering(int courierID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 진행 중인 배달 정보를 확인할 수 없으므로 false를 반환
        if (currentData == null) return false;

        // 현재 진행 중인 배달 목록이 null이라면
        // 배달 중인 배달부가 없는 것으로 처리
        if(currentData.ActiveDeliveryList == null) return false;

        // 현재 진행 중인 배달 목록을 순회
        foreach(ActiveDeliveryData delivery in currentData.ActiveDeliveryList)
        {
            // 배달 데이터가 null이라면 해당 항목을 건너뜀
            if(delivery == null) continue;
            // 배달 데이터에 저장된 배달부 ID가
            // 전달받은 courierID와 같다면 true를 반환
            if(delivery.CourierID == courierID) return true;
        }

        return false;
    }
    public bool AddLetterProgress(LetterProgressData progressData)
    {
        if (currentData == null || progressData == null) return false;
        if(currentData.LetterProgressesList == null) return false;
        if(GetLetterProgress(progressData.LetterID) != null) return false;

        currentData.LetterProgressesList.Add(progressData);
        return true;
    }
    public LetterProgressData GetLetterProgress(int letterID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 편지 진행 데이터를 조회할 수 없으므로 null을 반환
        if (currentData == null) return null;
        // 편지 진행 목록이 null이라면
        // 저장된 편지 진행 데이터가 없으므로 null을 반환
        if(currentData.LetterProgressesList == null) return null;
        // 편지 진행 목록을 순회
        foreach(LetterProgressData letter in  currentData.LetterProgressesList)
        {
            // 목록의 편지 데이터가 null이라면
            // 해당 항목은 건너뜀
            if (letter == null) continue;

            // 편지 데이터의 LetterID가 전달받은 letterID와 같다면
            // 해당 LetterProgressData를 반환
            if(letter.LetterID == letterID) return letter;
        }

        return null;
    }
    public IReadOnlyList<LetterProgressData> GetLetterProgresses()
    {
        if(currentData == null) return System.Array.Empty<LetterProgressData>();
        if (currentData.LetterProgressesList == null) return System.Array.Empty<LetterProgressData>();
        return currentData.LetterProgressesList;
    }
    public bool AddActiveDelivery(ActiveDeliveryData deliveryData)
    {
        if (currentData == null) return false;

        if(deliveryData == null) return false;

        // 현재 진행 중인 배달 목록이 null이라면
        // 데이터를 추가할 수 없으므로 false
        if(currentData.ActiveDeliveryList == null) return false;

        // 현재 진행 중인 배달 목록에
        // 전달받은 배달 데이터를 추가
        currentData.ActiveDeliveryList.Add(deliveryData);

        // 정상적으로 추가했음을 알리기 위해 true를 반환한다.
        return true;
    }
    public IReadOnlyList<ActiveDeliveryData> GetActiveDeliveries()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        if(currentData == null) return null;

        // 현재 진행 중인 배달 목록을
        // 외부에서 직접 교체할 수 없는 읽기 전용 목록 형태로 반환
        return currentData.ActiveDeliveryList;
    }
    public bool AddDeliveryResult(DeliveryResultData resultData)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 결과 데이터를 추가할 수 없으므로 false를 반환
        if(currentData == null) return false;

        // 전달받은 배달 결과가 null이라면
        // 유효하지 않은 데이터이므로 false를 반환
        if(resultData == null) return false;

        // 배달 결과 목록이 null이라면
        // 데이터를 추가할 수 없으므로 false를 반환
        if(currentData.DeliveryResultsList == null) return false;

        // 배달 결과 목록에 전달받은 결과 데이터를 추가한다.
        currentData.DeliveryResultsList.Add(resultData);

        // 정상적으로 추가했음을 알리기 위해 true를 반환한다.
        return true;
    }
    public bool RemoveActiveDelivery(ActiveDeliveryData deliveryData)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 진행 중인 배달을 제거할 수 없으므로 false를 반환
        if (currentData == null) return false;

        // 전달받은 배달 데이터가 null이라면
        // 제거할 대상이 없으므로 false를 반환
        if(deliveryData == null) return false;

        // 진행 중인 배달 목록이 null이라면
        // 제거할 수 없으므로 false를 반환
        if (currentData.ActiveDeliveryList == null) return false;

        // 진행 중인 배달 목록에서
        // 전달받은 배달 데이터를 제거
       
        return currentData.ActiveDeliveryList.Remove(deliveryData);
    }
    public DeliveryResultData GetDeliveryResult(int letterID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면 null을 반환
        if(currentData == null) return null;

        // 배달 결과 목록이 null이라면
        // 조회할 데이터가 없으므로 null을 반환
        if(currentData.DeliveryResultsList == null) return null;

        // 배달 결과 목록을 처음부터 순회
        foreach(DeliveryResultData deliveryData in currentData.DeliveryResultsList)
        {
            if(deliveryData == null) continue;
            if (deliveryData.LetterID == letterID) return deliveryData;
        }    
        return null;
    }

    public bool AddCurrency(int amount)
    {
        if (currentData == null) return false;
        if (amount <= 0) return false;

        if (!currentData.AddCurrency(amount)) return false;

        GameEvents.RaiseCurrencyChanged(currentData.Currency);

        return true;
    }
    public bool IncreaseCompletedDeliveryCount()
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 완료 횟수를 변경할 수 없으므로 false를 반환
        if(currentData == null) return false;

        // PlayerSaveData의 완료 횟수 증가 함수를 호출
        currentData.IncreaseCompletedDeliveryCount();

        // 정상적으로 증가했으므로 true를 반환
        return true;
    }

    public IReadOnlyList<DeliveryResultData> GetUncheckedDeliveryResults()
    {
        // 반환할 배달 결과 목록을 새로 생성
        List<DeliveryResultData> deliveryResultDatas = new List<DeliveryResultData>();

        if (currentData == null) return deliveryResultDatas;
        if (currentData.DeliveryResultsList == null) return deliveryResultDatas;

        // 저장된 배달 결과 목록을 순회
        foreach (DeliveryResultData deliveryResultData in currentData.DeliveryResultsList)
        {
            if (deliveryResultData == null) continue;
            if (!deliveryResultData.IsChecked)
            {
                deliveryResultDatas.Add(deliveryResultData);
            }
        }

        // 확인하지 않은 결과들만 담긴 목록을 반환
        return deliveryResultDatas;
    }

    public bool AddReceivedReply(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 받은 답장 정보를 추가할 수 없으므로 false를 반환
        if(currentData == null) return false;

        bool isReceived = currentData.AddReceivedReply(replyID);
        if(!isReceived) return false;
        GameEvents.RaiseReplyReceived(replyID);
        GameEvents.RaiseUnreadReplyCountChanged(GetUnreadReplyIDs().Count);
        return true;

    }
    public bool MarkReplyAsRead(int replyID)
    {
        // PlayerDataManager가 아직 초기화되지 않았다면
        // 답장 읽음 상태를 변경할 수 없으므로 false를 반환
        if(currentData == null) return false;

        bool isReceived = currentData.MarkReplyAsRead(replyID);
        if (!isReceived) return false;
        GameEvents.RaiseReplyRead(replyID);
        GameEvents.RaiseUnreadReplyCountChanged(GetUnreadReplyIDs().Count);
        return true;
    }

    public bool IsReplyRead(int replyID)
    {
        // PlayerDataManager가 초기화되지 않았다면 false를 반환
        if(currentData == null) return false;
        // 읽은 답장 ID 목록이 null이라면 false를 반환
        if(currentData.ReadReplyIds == null) return false;
        // 읽은 답장 ID 목록에 replyID가 포함되어 있는지 반환
        return currentData.ReadReplyIds.Contains(replyID);
    }
    public bool IsReplyReceived(int replyID)
    {
        if (currentData == null) return false;
        if(currentData.ReceivedReplyIDs == null) return false;
        return currentData.ReceivedReplyIDs.Contains(replyID);
    }

    public IReadOnlyList<int> GetUnreadReplyIDs()
    {
        List<int> replyList = new List<int>();
        if (currentData == null) return replyList;
        if (currentData.ReceivedReplyIDs == null) return replyList;

        foreach(int replyID in currentData.ReceivedReplyIDs)
        {
            if(!IsReplyRead(replyID)) replyList.Add(replyID);
        }
        return replyList;
    }

    public int GetCurrency()
    {
        if (currentData == null) return 0;

        return currentData.Currency;
    }
    public int GetUnreadReplyCount()
    {
        if(currentData == null) return 0;
        return GetUnreadReplyIDs().Count; 
    }
    public int GetCompletedDeliveryCount()
    {
        if (currentData == null) return 0;

        return currentData.CompleteDeliveryCount;
    }
}
