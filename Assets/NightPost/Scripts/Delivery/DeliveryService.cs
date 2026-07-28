using System.Collections.Generic;
using System;
using UnityEngine;

public class DeliveryService : MonoBehaviour
{
    private PlayerDataManager playerDataManager;
    private StaticDataCatalog staticDataCatalog;
    public bool Initialize(PlayerDataManager dataManager,StaticDataCatalog catalog)
    {
        if (dataManager == null) return false;
        if(catalog == null) return false;

        playerDataManager = dataManager;
        staticDataCatalog = catalog;
        return true;
    }

    public bool StartDelivery(int courierID, int letterID, int routeID)
    {
        if (!CanStartDelivery(courierID, letterID, routeID)) return false;

        CourierStaticData courier = staticDataCatalog.GetCourier(courierID);
        RouteStaticData route = staticDataCatalog.GetRoute(routeID);

        if(courier == null || route == null) return false;

        float realDeliveryTime = CalculateBaseDeliveryDuration(courier, route);
        if(realDeliveryTime <= 0) return false;

        long startedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long completedAtUnixTime = CalculateCompleteAtUnixTime(startedAtUnixTime, realDeliveryTime);
        if (completedAtUnixTime <= 0) return false;

        ActiveDeliveryData activeDeliveryData = new ActiveDeliveryData(letterID, courierID, routeID, startedAtUnixTime, completedAtUnixTime);
        
        if(!playerDataManager.AddActiveDelivery(activeDeliveryData)) return false;

        LetterProgressData letterProgress = playerDataManager.GetLetterProgress(letterID);
        if (letterProgress == null) return false;
        if (!letterProgress.StartDelivery()) return false;
        GameEvents.RaiseLetterStateChanged(letterID, ELetterProgressState.Delivering);
        GameEvents.RaiseDeliveryStarted(letterID, courierID, routeID);
        return true;
    }    
    private bool CanStartDelivery(int courierID, int letterID, int routeID)
    {
        // PlayerDataManager가 연결되지 않았다면
        // 배달 조건을 확인할 수 없으므로 false를 반환
        if (playerDataManager == null) return false;
        // 선택한 배달부를 플레이어가 보유하고 있지 않다면
        // 배달을 시작할 수 없으므로 false를 반환
        if(!playerDataManager.IsCourierOwned(courierID)) return false;
        // 선택한 배달부가 이미 다른 배달을 진행 중이라면
        // 중복 배정을 막기 위해 false를 반환
        if (playerDataManager.IsCourierDelivering(courierID)) return false;

        // 선택한 노선이 아직 해금되지 않았다면
        // 이용할 수 없으므로 false를 반환
        if(!playerDataManager.IsRouteUnlocked(routeID)) return false;

        // 편지 목적지와 선택한 노선의 지역이 맞지 않다면 false 반환
        if (!IsRouteCompatible(letterID, routeID)) return false;

        // 편지 진행 데이터를 조회
        LetterProgressData letter = playerDataManager.GetLetterProgress(letterID);
        if (letter == null) return false;
        // 편지 데이터가 없거나 현재 상태가 Waiting이 아니라면
        // 배달 가능한 편지가 아니므로 false를 반환
        if (letter.State != ELetterProgressState.Waiting) return false;

        // 모든 조건을 통과했다면 true를 반환한다.
        return true;
    }

    private float CalculateBaseDeliveryDuration( CourierStaticData courierData,RouteStaticData routeData)
    {
        // 전달받은 배달부 데이터가 null이라면
        // 배달 시간을 계산할 수 없으므로 0을 반환
        if(courierData == null) return 0f;

        // 전달받은 노선 데이터가 null이라면
        // 배달 시간을 계산할 수 없으므로 0을 반환
        if(routeData == null) return 0f;

        // 배달부 속도가 0 이하라면 0을 반환
        if (courierData.Speed <= 0f) return 0f;

        // 노선의 기본 소요 시간을 배달부의 기본 속도로 나누어
        // 실제 기본 배달 시간을 계산
        if (routeData.BaseDeliveryTimeSeconds <= 0f) return 0f;
        float time = routeData.BaseDeliveryTimeSeconds / courierData.Speed;

        // 계산된 기본 배달 시간을 반환한다.
        return time;
    }

    private long CalculateCompleteAtUnixTime(long startedAtUnixTime, float deliveryDurationSeconds)
    {
        // 배달 시작 시각이 0 이하라면 0을 반환
        if (startedAtUnixTime <= 0) return 0;

        // 계산된 배달 시간이 0 이하라면
        if (deliveryDurationSeconds <= 0) return 0;

        // 실수로 계산된 배달 시간을 초 단위 정수로 올림 처리
        // 소수 시간이 버려져 배달이 일찍 완료되는 것을 방지
        long durationSeconds = (long)Math.Ceiling(deliveryDurationSeconds);

        // 계산된 완료 예정 시각을 반환한다.
        return startedAtUnixTime + durationSeconds;
    }

    private bool IsRouteCompatible(int letterID, int routeID)
    {
        // StaticDataCatalog가 연결되지 않았다면
        // 정적 데이터를 조회할 수 없으므로 false를 반환
        if(staticDataCatalog == null) return false;

        // 전달받은 letterID로 편지 정적 데이터를 조회한다.
        LetterStaticData letter = staticDataCatalog.GetLetter(letterID);

        // 전달받은 routeID로 노선 정적 데이터를 조회한다.
        RouteStaticData route = staticDataCatalog.GetRoute(routeID);

        // 편지 또는 노선 데이터가 존재하지 않는다면
        // 호환 여부를 판단할 수 없으므로 false를 반환
        if (letter == null || route == null) return false;

        // 편지의 목적 지역과 노선의 담당 지역이 같은지 비교해
        // 그 결과를 반환
        return letter.DestinationRegion == route.RegionType;
    }

    private bool CompleteDelivery(ActiveDeliveryData deliveryData,long currentUnixTime)
    {
        // PlayerDataManager 또는 StaticDataCatalog가 연결되지 않았다면
        // 완료 처리를 진행할 수 없으므로 false를 반환
        if(playerDataManager == null || staticDataCatalog == null) return false;

        // 전달받은 진행 중 배달 데이터가 없다면
        // 처리할 배달이 없으므로 false를 반환
        if(deliveryData == null) return false;

        // 현재 시각이 아직 배달 완료 예정 시각에 도달하지 않았다면
        // 완료 처리하지 않고 false를 반환
        if (!IsDeliveryCompleted(deliveryData, currentUnixTime)) return false;

        // 배달 데이터의 LetterID를 이용해
        // 편지 정적 데이터를 조회
        LetterStaticData letter = staticDataCatalog.GetLetter(deliveryData.LetterID);

        // 같은 LetterID를 이용해
        // 편지 진행 데이터를 조회
        LetterProgressData letterProgressData = playerDataManager.GetLetterProgress(deliveryData.LetterID);

        // 편지 정적 데이터 또는 진행 데이터를 찾지 못했다면
        // 정상적인 완료 처리를 할 수 없으므로 false를 반환
        if (letter == null || letterProgressData == null) return false;

        // 편지 진행 상태가 Delivering이 아니라면
        // 현재 완료할 수 있는 편지가 아니므로 false를 반환
        if(letterProgressData.State != ELetterProgressState.Delivering) return false;

        // 편지의 기본 보상과 현재 완료 시각을 이용해
        // DeliveryResultData를 생성
        DeliveryResultData deliveryResult = new DeliveryResultData(deliveryData.LetterID, letter.LetterReward, deliveryData.CompleteAtUnixTime);
        // 생성한 배달 결과를 PlayerDataManager에 추가한다.
        // 추가에 실패하면 false를 반환
        if(!playerDataManager.AddDeliveryResult(deliveryResult))return false;

        // 편지 진행 상태를 Delivering에서 Completed로 변경한다.
        // 상태 변경에 실패하면 false를 반환
        if(!letterProgressData.CompleteDelivery()) return false;

        // 완료된 배달을 진행 중 배달 목록에서 제거한다.
        // 제거에 실패하면 false를 반환
        if(!playerDataManager.RemoveActiveDelivery(deliveryData)) return false;
        if (!playerDataManager.IncreaseCompletedDeliveryCount()) return false;
        GameEvents.RaiseLetterStateChanged(deliveryData.LetterID, ELetterProgressState.Completed);
        GameEvents.RaiseDeliveryCompleted(deliveryData.LetterID);
        // 배달 완료 처리가 모두 성공했으므로 true를 반환
        return true; 
    }
    private bool IsDeliveryCompleted(ActiveDeliveryData deliveryData, long currentUnixTime)
    {
        // 전달받은 진행 중 배달 데이터가 null이라면
        // 완료 여부를 판단할 수 없으므로 false를 반환
        if (deliveryData == null) return false;

        // 현재 Unix 시각이 0 이하라면
        // 유효하지 않은 시간이므로 false를 반환
        if (currentUnixTime <= 0) return false;

        // 배달 완료 예정 시각이 0 이하라면
        // 정상적인 배달 데이터가 아니므로 false를 반환
        if (deliveryData.CompleteAtUnixTime <= 0) return false;

        // 현재 시각이 배달 완료 예정 시각과 같거나 지났는지
        // 비교한 결과를 반환
        return currentUnixTime >= deliveryData.CompleteAtUnixTime;
    }
    public void ProcessCompletedDeliveries()
    {
        // PlayerDataManager가 연결되지 않았다면
        // 진행 중인 배달을 조회할 수 없으므로 종료
        if (playerDataManager == null) return;
        // PlayerDataManager에서 현재 진행 중인 배달 목록을 가져옴
        IReadOnlyList<ActiveDeliveryData> activeDeliveryDatas = playerDataManager.GetActiveDeliveries();

        // 진행 중인 배달 목록이 null이거나 비어 있다면
        // 완료 처리할 배달이 없으므로 종료.
        if (activeDeliveryDatas == null || activeDeliveryDatas.Count == 0) return;

        // 여러 배달을 같은 기준 시각으로 검사할 수 있도록
        // 현재 UTC Unix 시각을 한 번만 가져옴
        long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 완료된 배달을 처리하면서 목록에서 제거하므로
        // 목록의 마지막 항목부터 첫 번째 항목까지 역순으로 순회
        for(int i = activeDeliveryDatas.Count - 1; i >= 0; i--)
        {
            ActiveDeliveryData deliveryData = activeDeliveryDatas[i];

            if (deliveryData == null) continue;
            if (currentUnixTime >= deliveryData.CompleteAtUnixTime)
            {
                CompleteDelivery(deliveryData, currentUnixTime);
            }
        }
    }

    public bool CheckDeliveryResult(int letterID)
    {
        // PlayerDataManager가 연결되지 않았다면
        // 결과 확인과 보상 지급을 할 수 없으므로 false를 반환
        if (playerDataManager == null) return false;

        // letterID에 해당하는 배달 결과를 조회
        DeliveryResultData deliveryResult = playerDataManager.GetDeliveryResult(letterID);

        // 배달 결과가 없다면 false를 반환한다.
        if (deliveryResult == null) return false;

        // 이미 확인한 결과라면
        // 보상 중복 지급을 막기 위해 false를 반환
        if (deliveryResult.IsChecked) return false;

        // 결과에 저장된 RewardAmount를 플레이어 재화에 추가
        // 재화 추가에 실패하면 false를 반환
        if (!RegisterReceivedReply(letterID)) return false;
        if (!playerDataManager.AddCurrency(deliveryResult.RewardAmount)) return false;

        // 배달 결과를 확인한 상태로 변경
        // 상태 변경에 실패하면 false를 반환
        if(!deliveryResult.MarkAsChecked()) return false;

        GameEvents.RaiseDeliveryResultChecked(letterID);
        // 보상 지급과 확인 처리가 완료됐으므로 true를 반환
        return true;
    }

    private bool RegisterReceivedReply(int letterID)
    {
        // PlayerDataManager 또는 StaticDataCatalog가 연결되지 않았다면
        // 답장을 등록할 수 없으므로 false를 반환
        if(playerDataManager == null || staticDataCatalog == null) return false;
        // letterID에 연결된 ReplyStaticData를 조회
        ReplyStaticData reply = staticDataCatalog.GetReplyByLetterID(letterID);
        // 연결된 답장이 없다면 등록할 수 없으므로 false를 반환
        if (reply == null) return false;
        // 조회한 답장의 ReplyID를
        // PlayerDataManager.AddReceivedReply()에 전달
        return playerDataManager.AddReceivedReply(reply.ReplyID);

    }
}
