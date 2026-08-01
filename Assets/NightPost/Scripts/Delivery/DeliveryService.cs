using System.Collections.Generic;
using System;
using UnityEngine;

public class DeliveryService : MonoBehaviour
{
    // 플레이어 런타임 데이터 관리
    private PlayerDataManager playerDataManager;

    // 배달부, 편지, 노선 정적 데이터 조회
    private StaticDataCatalog staticDataCatalog;

    // 누적 배달 완료 수 기반 해금 처리
    private ProgressionService progressionService;

    private FacilityService facilityService;

    /// <summary>
    /// 배달 서비스에 필요한 의존성 연결함
    /// </summary>
    public bool Initialize(PlayerDataManager dataManager,StaticDataCatalog catalog, ProgressionService progression, FacilityService facility)
    {
        // 필수 의존성 확인
        if (dataManager == null) return false;
        if (catalog == null) return false;
        if (progression == null) return false;
        if (facility == null) return false;

        // 전달받은 의존성 저장
        playerDataManager = dataManager;
        staticDataCatalog = catalog;
        progressionService = progression;
        facilityService = facility;

        // 초기화 성공
        return true;
    }

    /// <summary>
    /// 선택한 편지, 배달부, 노선으로 새 배달 시작함
    /// </summary>
    public bool StartDelivery(int courierID, int letterID, int routeID)
    {
        // 배달 시작 가능 여부 확인
        if (!CanStartDelivery(courierID, letterID, routeID)) return false;

        // 배달부 정적 데이터 조회
        CourierStaticData courier = staticDataCatalog.GetCourier(courierID);
        // 노선 정적 데이터 조회
        RouteStaticData route = staticDataCatalog.GetRoute(routeID);
        // 필수 정적 데이터 확인
        if (courier == null || route == null) return false;
        // 실제 배달 소요 시간 계산
        float realDeliveryTime = CalculateBaseDeliveryDuration(courier, route);
        // 잘못된 배달 시간 차단
        if (realDeliveryTime <= 0) return false;

        // 현재 시각을 배달 시작 시각으로 사용
        long startedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // 배달 완료 예정 시각 계산
        long completedAtUnixTime = CalculateCompleteAtUnixTime(startedAtUnixTime, realDeliveryTime);
        // 잘못된 완료 시각 차단
        if (completedAtUnixTime <= 0) return false;

        // 진행 중 배달 데이터 생성
        ActiveDeliveryData activeDeliveryData = new ActiveDeliveryData(letterID, courierID, routeID, startedAtUnixTime, completedAtUnixTime);
        // 진행 중 배달 목록에 추가
        if (!playerDataManager.AddActiveDelivery(activeDeliveryData)) return false;

        // 편지 진행 데이터 조회
        LetterProgressData letterProgress = playerDataManager.GetLetterProgress(letterID);
        // 편지 진행 데이터 확인
        if (letterProgress == null) return false;
        // 편지 상태를 Delivering으로 변경
        if (!letterProgress.StartDelivery()) return false;

        // 편지 상태 변경 알림
        GameEvents.RaiseLetterStateChanged(letterID, ELetterProgressState.Delivering);
        // 배달 시작 알림
        GameEvents.RaiseDeliveryStarted(letterID, courierID, routeID);
        // 배달 시작 성공
        return true;
    }

    /// <summary>
    /// 배달부, 편지, 노선이 배달 시작 조건을 만족하는지 확인함
    /// </summary>
    private bool CanStartDelivery(int courierID, int letterID, int routeID)
    {
        // 플레이어 데이터 연결 여부 확인
        if (playerDataManager == null) return false;
        // 플레이어의 배달부 보유 여부 확인
        if (!playerDataManager.IsCourierOwned(courierID)) return false;
        // 배달부의 다른 배달 진행 여부 확인
        if (playerDataManager.IsCourierDelivering(courierID)) return false;

        // 노선 해금 여부 확인
        if (!playerDataManager.IsRouteUnlocked(routeID)) return false;

        // 편지 목적지와 노선 지역 호환 여부 확인
        if (!IsRouteCompatible(letterID, routeID)) return false;

        // 편지 진행 데이터를 조회
        LetterProgressData letter = playerDataManager.GetLetterProgress(letterID);
        // 편지 진행 데이터 확인
        if (letter == null) return false;
        // Waiting 상태 편지만 배달 가능
        if (letter.State != ELetterProgressState.Waiting) return false;

        // 모든 배달 시작 조건 충족
        return true;
    }
    /// <summary>
    /// 노선 기본 시간과 배달부 속도로 배달 시간 계산함
    /// </summary>
    private float
        CalculateBaseDeliveryDuration( CourierStaticData courierData,RouteStaticData routeData)
    {
        // 배달부 데이터 확인
        if (courierData == null) return 0f;

        // 노선 데이터 확인
        if (routeData == null) return 0f;

        // 배달부 속도 확인
        if (courierData.Speed <= 0f) return 0f;

        // 노선 기본 배달 시간 확인
        if (routeData.BaseDeliveryTimeSeconds <= 0f) return 0f;

        // 노선 기본 시간을 배달부 속도로 보정
        float time = routeData.BaseDeliveryTimeSeconds / courierData.Speed;

        // 시설의 배달 시간 감소 효과를 적용한 최종 시간 반환
        return ApplyFacilityDeliveryTimeReduction(time);
    }
    /// <summary>
    /// 시작 시각과 배달 시간으로 완료 예정 Unix 시각 계산함
    /// </summary>
    private long CalculateCompleteAtUnixTime(long startedAtUnixTime, float deliveryDurationSeconds)
    {
        // 배달 시작 시각 확인
        if (startedAtUnixTime <= 0) return 0;

        // 배달 소요 시간 확인
        if (deliveryDurationSeconds <= 0) return 0;

        // 소수점 배달 시간을 초 단위로 올림
        long durationSeconds = (long)Math.Ceiling(deliveryDurationSeconds);

        // 완료 예정 Unix 시각 반환
        return startedAtUnixTime + durationSeconds;
    }
    /// <summary>
    /// 편지 목적지와 노선 담당 지역의 일치 여부 확인함
    /// </summary>
    private bool IsRouteCompatible(int letterID, int routeID)
    {
        // 정적 데이터 카탈로그 연결 여부 확인
        if (staticDataCatalog == null) return false;

        // 편지 정적 데이터 조회
        LetterStaticData letter = staticDataCatalog.GetLetter(letterID);

        // 노선 정적 데이터 조회
        RouteStaticData route = staticDataCatalog.GetRoute(routeID);

        // 편지 또는 노선 데이터 확인
        if (letter == null || route == null) return false;

        // 편지 목적지와 노선 지역 비교
        return letter.DestinationRegion == route.RegionType;
    }

    /// <summary>
    /// 완료 시각에 도달한 배달을 완료 상태로 처리함
    /// </summary>
    private bool CompleteDelivery(ActiveDeliveryData deliveryData,long currentUnixTime)
    {
        // 필수 의존성 확인
        if (playerDataManager == null || staticDataCatalog == null) return false;

        // 진행 중 배달 데이터 확인
        if (deliveryData == null) return false;

        // 배달 완료 시각 도달 여부 확인
        if (!IsDeliveryCompleted(deliveryData, currentUnixTime)) return false;

        // 편지 정적 데이터 조회
        LetterStaticData letter = staticDataCatalog.GetLetter(deliveryData.LetterID);

        // 편지 진행 데이터 조회
        LetterProgressData letterProgressData = playerDataManager.GetLetterProgress(deliveryData.LetterID);

        // 편지 관련 데이터 확인
        if (letter == null || letterProgressData == null) return false;

        // 배달 결과 데이터 생성
        if (letterProgressData.State != ELetterProgressState.Delivering) return false;

        // 배달 결과 목록에 추가
        DeliveryResultData deliveryResult = new DeliveryResultData(deliveryData.LetterID, letter.LetterReward, deliveryData.CompleteAtUnixTime);
        // 배달 결과 목록에 추가
        if (!playerDataManager.AddDeliveryResult(deliveryResult))return false;

        // 편지 상태를 Completed로 변경
        if (!letterProgressData.CompleteDelivery()) return false;

        // 진행 중 배달 목록에서 제거
        if (!playerDataManager.RemoveActiveDelivery(deliveryData)) return false;
        // 누적 완료 배달 수 증가
        if (!playerDataManager.IncreaseCompletedDeliveryCount()) return false;
        // 편지 상태 변경 알림
        GameEvents.RaiseLetterStateChanged(deliveryData.LetterID, ELetterProgressState.Completed);
        // 배달 완료 알림
        GameEvents.RaiseDeliveryCompleted(deliveryData.LetterID);
        progressionService.EvaluateProgressUnlocks();

        // 배달 완료 처리 성공
        return true; 
    }

    /// <summary>
    /// 현재 시각이 배달 완료 예정 시각에 도달했는지 확인함
    /// </summary>
    private bool IsDeliveryCompleted(ActiveDeliveryData deliveryData, long currentUnixTime)
    {
        // 진행 중 배달 데이터 확인
        if (deliveryData == null) return false;

        // 현재 Unix 시각 확인
        if (currentUnixTime <= 0) return false;

        // 배달 완료 예정 시각 확인
        if (deliveryData.CompleteAtUnixTime <= 0) return false;

        // 현재 시각과 완료 예정 시각 비교
        return currentUnixTime >= deliveryData.CompleteAtUnixTime;
    }

    /// <summary>
    /// 진행 중 배달 목록에서 완료된 배달을 찾아 처리함
    /// </summary>
    public void ProcessCompletedDeliveries()
    {
        // 플레이어 데이터 연결 여부 확인
        if (playerDataManager == null) return;
        // 현재 진행 중 배달 목록 조회
        IReadOnlyList<ActiveDeliveryData> activeDeliveryDatas = playerDataManager.GetActiveDeliveries();

        // 완료 검사 대상 존재 여부 확인
        if (activeDeliveryDatas == null || activeDeliveryDatas.Count == 0) return;

        // 모든 배달에 사용할 현재 시각 조회
        long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 목록 제거에 대비한 역순 순회
        for (int i = activeDeliveryDatas.Count - 1; i >= 0; i--)
        {
            // 현재 검사할 배달 데이터 조회
            ActiveDeliveryData deliveryData = activeDeliveryDatas[i];
            // null 데이터 건너뜀
            if (deliveryData == null) continue;
            // 완료 시각에 도달한 배달 처리
            if (currentUnixTime >= deliveryData.CompleteAtUnixTime)
            {
                CompleteDelivery(deliveryData, currentUnixTime);
            }
        }
    }
    /// <summary>
    /// 배달 결과를 확인하고 보상과 답장을 지급함
    /// </summary>
    public bool CheckDeliveryResult(int letterID)
    {
        // 플레이어 데이터 연결 여부 확인
        if (playerDataManager == null) return false;

        // 편지 ID에 해당하는 배달 결과 조회
        DeliveryResultData deliveryResult = playerDataManager.GetDeliveryResult(letterID);

        // 배달 결과 존재 여부 확인
        if (deliveryResult == null) return false;

        // 이미 확인한 결과의 중복 처리 차단
        if (deliveryResult.IsChecked) return false;

        // 연결된 답장 등록
        if (!RegisterReceivedReply(letterID)) return false;
        // 배달 보상 재화 지급
        if (!playerDataManager.AddCurrency(deliveryResult.RewardAmount)) return false;

        // 배달 결과를 확인 상태로 변경
        if (!deliveryResult.MarkAsChecked()) return false;

        // 배달 결과 확인 알림
        GameEvents.RaiseDeliveryResultChecked(letterID);
        // 결과 확인과 보상 지급 성공
        return true;
    }
    /// <summary>
    /// 완료한 편지에 연결된 답장을 수신 목록에 등록함
    /// </summary>
    private bool RegisterReceivedReply(int letterID)
    {
        // 필수 의존성 확인
        if (playerDataManager == null || staticDataCatalog == null) return false;
        // 편지 ID에 연결된 답장 조회
        ReplyStaticData reply = staticDataCatalog.GetReplyByLetterID(letterID);
        // 연결된 답장 존재 여부 확인
        if (reply == null) return false;
        // 답장 수신 목록에 등록
        return playerDataManager.AddReceivedReply(reply.ReplyID);

    }
    /// <summary>
    /// 시설의 배달 시간 감소 효과를 기본 배달 시간에 적용함
    /// </summary>
    private float ApplyFacilityDeliveryTimeReduction(float baseDeliveryDuration)
    {
        // 기본 배달 시간이 0 이하이면 0 반환
        if(baseDeliveryDuration <= 0) return 0;
        // FacilityService 연결 여부 확인
        if (facilityService == null) return baseDeliveryDuration;

        // 모든 시설의 배달 시간 감소 효과값 조회
        float facilityTimeReduction = facilityService.GetTotalFacilityEffectValue(EFacilityEffectType.DeliveryTimeReduction);
        // 감소 효과가 0 이하이면 기본 배달 시간 반환
        if (facilityTimeReduction <= 0) return baseDeliveryDuration;

        // 감소율을 0부터 1 사이로 제한
        facilityTimeReduction = Mathf.Clamp01(facilityTimeReduction);

        // 기본 배달 시간에 감소율 적용
        float finalDeliveryDuration = baseDeliveryDuration * (1f - facilityTimeReduction);

        // 최종 배달 시간이 1초 미만이면 1초로 보정
        if (finalDeliveryDuration <= 1) return 1;

        // 최종 배달 시간 반환
        return finalDeliveryDuration;
    }

}
