using UnityEngine;
using System.Collections.Generic;

// 편지 확인, 분류, 배달, 결과 확인, 답장 열람으로 이어지는 게임 진행 흐름을 관리함
public class GameFlowController : MonoBehaviour
{
    // 편지 조회 및 상태 변경을 담당하는 서비스임
    private LetterService letterService;
    // 배달 시작 및 결과 확인을 담당하는 서비스임
    private DeliveryService deliveryService;
    // 답장 조회 및 읽음 처리를 담당하는 서비스임
    private ReplyService replyService;
    // 플레이어의 진행 데이터를 조회하는 매니저임
    private PlayerDataManager playerDataManager;
    // 시설 조회 및 업그레이드를 담당하는 서비스임
    private FacilityService facilityService;
    private SortingService sortingService;
    private ProgressionService progressionService;
    // 현재 플레이어 진행 상태를 아침 보고서 형태로 집계하는 서비스임
    private MorningReportService morningReportService;
    // 편지와 답장의 수집 상태를 도감 항목으로 구성하는 서비스임
    private CollectionService collectionService;

    // 현재 선택한 편지 ID임
    private int selectedLetterID;
    // 현재 선택한 배달 결과의 편지 ID임
    private int selectedResultLetterID;
    // 현재 선택한 답장 ID임
    private int selectedReplyID;
    // 현재 선택한 시설 ID임
    private int selectedFacilityID;

    /// <summary>
    /// 게임 진행에 필요한 서비스와 데이터 매니저를 등록함
    /// </summary>
    public bool Initialize(LetterService letter, SortingService sorting, DeliveryService delivery, ReplyService reply, FacilityService facility, ProgressionService progression, MorningReportService morningReport, CollectionService collection, PlayerDataManager dataManager)
    {
        // 필요한 참조 중 하나라도 없다면 초기화하지 않음
        if (letter == null || delivery == null || reply == null || facility == null || dataManager == null
            || sorting == null || progression == null) return false;
        // 아침 배달 보고서 서비스가 없다면 초기화하지 않음
        if (morningReport == null) return false;
        if (collection == null) return false;

        // 편지 서비스를 저장함
        letterService = letter;
        // 전달받은 분류 서비스를 내부 필드에 저장함
        sortingService = sorting;
        // 배달 서비스를 저장함
        deliveryService = delivery;
        // 답장 서비스를 저장함
        replyService = reply;
        // 시설 서비스를 저장함
        facilityService = facility;
        // 전달받은 진행도 서비스를 내부 필드에 저장함
        progressionService = progression;
        // 전달받은 아침 배달 보고서 서비스를 저장함
        morningReportService = morningReport;
        // 플레이어 데이터 매니저를 저장함
        playerDataManager = dataManager;
        // 전달받은 편지·답장 도감 서비스를 저장함
        collectionService = collection;

        // 필요한 참조 등록이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 편지를 열고 현재 선택한 편지로 저장함
    /// </summary>
    public bool SelectLetter(int letterID)
    {
        // 편지 서비스가 등록되지 않았다면 선택하지 않음
        if (letterService == null) return false;
        // 유효하지 않은 편지 ID라면 선택하지 않음
        if (letterID <= 0) return false;

        // 지정한 편지를 열고 표시할 정적 데이터를 조회함
        LetterStaticData letterStaticData = letterService.OpenLetter(letterID);
        // 편지를 열 수 없다면 선택 처리하지 않음
        if (letterStaticData == null) return false;

        // 열린 편지의 ID를 현재 선택한 편지 ID로 저장함
        selectedLetterID = letterStaticData.LetterID;

        // 편지 선택이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 현재 선택한 편지의 배달을 지정한 배달부와 노선으로 시작함
    /// </summary>
    public bool StartSelectedLetterDelivery(int courierID, int routeID)
    {
        // 배달 서비스가 등록되지 않았다면 배달을 시작하지 않음
        if (deliveryService == null) return false;
        // 현재 선택한 편지가 없다면 배달을 시작하지 않음
        if (selectedLetterID <= 0) return false;
        // 유효하지 않은 배달부 또는 노선 ID라면 배달을 시작하지 않음
        if (courierID <= 0 || routeID <= 0) return false;
        // 현재 선택한 편지의 배달 시작을 요청함
        bool startResult = deliveryService.StartDelivery(courierID, selectedLetterID, routeID);
        // 배달 시작에 실패했다면 선택 상태를 유지하고 종료함
        if (!startResult) return false;

        // 배달을 시작한 편지의 선택 상태를 초기화함
        selectedLetterID = 0;

        // 배달 시작 처리가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 현재 선택한 편지와 지정한 배달부·노선의 배달 예상 정보를 반환함
    /// </summary>
    public DeliveryPreviewData GetSelectedLetterDeliveryPreview(int courierID,int routeID)
    {
        // 배달 서비스가 등록되지 않았다면 예상 정보를 조회하지 않음
        if(deliveryService == null) return null;

        // 현재 선택한 편지가 없다면 예상 정보를 조회하지 않음
        if(selectedLetterID <= 0) return null;

        // 유효하지 않은 배달부 또는 노선 ID라면 예상 정보를 조회하지 않음
        if (courierID <= 0 || routeID <= 0) return null;

        // 현재 선택한 편지와 배달부·노선 정보를 배달 서비스에 전달함
        // 계산된 배달 예상 정보를 반환함
        return deliveryService.GetDeliveryPreview(selectedLetterID, courierID, routeID);
    }
    /// <summary>
    /// 지정한 편지의 미확인 배달 결과를 현재 선택 결과로 저장함
    /// </summary>
    public bool SelectDeliveryResult(int letterID)
    {
        // 플레이어 데이터 매니저가 등록되지 않았다면 결과를 선택하지 않음
        if (playerDataManager == null) return false;
        // 유효하지 않은 편지 ID라면 결과를 선택하지 않음
        if (letterID <= 0) return false;
        // 지정한 편지의 배달 결과 데이터를 조회함
        DeliveryResultData deliveryResultData = playerDataManager.GetDeliveryResult(letterID);
        // 배달 결과 데이터가 없다면 선택하지 않음
        if (deliveryResultData == null) return false;
        // 이미 확인한 배달 결과라면 다시 선택하지 않음
        if (deliveryResultData.IsChecked) return false;

        // 배달 결과의 편지 ID를 현재 선택한 결과 ID로 저장함
        selectedResultLetterID = deliveryResultData.LetterID;
        // 배달 결과 선택이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 현재 선택한 배달 결과를 확인 완료 상태로 변경함
    /// </summary>
    public bool CheckSelectedDeliveryResult()
    {
        // 배달 서비스가 등록되지 않았다면 결과 확인을 처리하지 않음
        if (deliveryService == null) return false;
        // 현재 선택한 배달 결과가 없다면 확인을 처리하지 않음
        if (selectedResultLetterID <= 0) return false;
        // 현재 선택한 편지의 배달 결과 확인을 요청함
        bool checkResult = deliveryService.CheckDeliveryResult(selectedResultLetterID);
        // 배달 결과 확인에 실패했다면 선택 상태를 유지하고 종료함
        if (!checkResult) return false;
        // 확인을 완료한 배달 결과의 선택 상태를 초기화함
        selectedResultLetterID = 0;
        // 배달 결과 확인 처리가 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 수신한 답장을 현재 선택한 답장으로 저장함
    /// </summary>
    public bool SelectReply(int replyID)
    {
        // 플레이어 데이터 매니저가 등록되지 않았다면 답장을 선택하지 않음
        if (playerDataManager == null) return false;
        // 유효하지 않은 답장 ID라면 선택하지 않음
        if (replyID <= 0) return false;

        // 지정한 답장을 플레이어가 수신했는지 확인함
        bool isReceived = playerDataManager.IsReplyReceived(replyID);
        // 수신하지 않은 답장이라면 선택하지 않음
        if (!isReceived) return false;
        // 지정한 답장 ID를 현재 선택한 답장으로 저장함
        selectedReplyID = replyID;
        // 답장 선택이 완료되었음을 반환함
        return true;
    }

    /// <summary>
    /// 현재 선택한 답장을 열고 답장 정적 데이터를 반환함
    /// </summary>
    public ReplyStaticData OpenSelectedReply()
    {
        // 답장 서비스가 등록되지 않았다면 답장을 열지 않음
        if (replyService == null) return null;
        // 현재 선택한 답장을 열고 표시할 정적 데이터를 조회함
        ReplyStaticData reply = replyService.OpenReply(selectedReplyID);
        // 답장을 열 수 없다면 선택 상태를 유지하고 null을 반환함
        if (reply == null) return null;
        // 열람을 완료한 답장의 선택 상태를 초기화함
        selectedReplyID = 0;

        // 화면에 표시할 답장 정적 데이터를 반환함
        return reply;
    }

    /// <summary>
    /// 지정한 시설을 현재 선택한 시설로 저장함
    /// </summary>
    public bool SelectFacility(int facilityID)
    {
        // 시설 서비스가 등록되지 않았다면 선택하지 않음
        if(facilityService == null) return false;

        // 유효하지 않은 시설 ID라면 선택하지 않음
        if(facilityID <= 0) return false;

        // 지정한 시설의 현재 레벨 데이터 조회
        FacilityLevelData currentLevelData = facilityService.GetCurrentLevelData(facilityID);

        // 지정한 시설의 다음 레벨 데이터 조회
        FacilityLevelData nextLevelData = facilityService.GetNextLevelData(facilityID);

        // 현재 레벨 데이터와 다음 레벨 데이터가 모두 없다면
        // 존재하지 않거나 사용할 수 없는 시설이므로 선택하지 않음
        if(currentLevelData == null && nextLevelData == null) return false;

        // 지정한 시설 ID를 현재 선택한 시설로 저장함
        selectedFacilityID = facilityID;

        // 시설 선택이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 현재 선택한 시설을 다음 레벨로 업그레이드함
    /// </summary>
    public bool UpgradeSelectedFacility()
    {
        // 시설 서비스가 등록되지 않았다면 업그레이드하지 않음
        if (facilityService == null) return false;
        // 현재 선택한 시설이 없다면 업그레이드하지 않음
        if (selectedFacilityID <= 0) return false;

        // 현재 선택한 시설의 업그레이드를 요청함
        bool isUpgrade = facilityService.UpgradeFacility(selectedFacilityID);

        // 시설 업그레이드에 실패하면 false 반환
        return isUpgrade;
    }

    /// <summary>
    /// 현재 선택한 편지에 플레이어가 선택한 분류값을 제출함
    /// </summary>
    public SortingResultData SubmitSelectedLetterSorting(ERegionType selectedRegion, ELetterUrgency selectedUrgency, ELetterWeight selectedWeight)
    {
        // 분류 서비스가 등록되지 않았다면 제출하지 않음
        if(sortingService == null) return null;

        // 현재 선택한 편지가 없다면 제출하지 않음
        if(selectedLetterID <= 0) return null;

        // 선택한 편지 ID와 지역·긴급도·무게 선택값을 분류 서비스에 전달함
        // 분류 판정 및 상태 변경 결과를 반환함
        return sortingService.SubmitSorting(selectedLetterID, selectedRegion, selectedUrgency, selectedWeight);
    }

    /// <summary>
    /// 진행 조건을 충족한 지정 노선의 수동 해금을 요청함
    /// </summary>
    public bool UnlockRoute(int routeID)
    {
        // 진행도 서비스가 등록되지 않았다면 해금하지 않음
        if(progressionService == null) return false;
        // 유효하지 않은 노선 ID라면 해금하지 않음
        if(routeID <= 0) return false;
        // 진행도 서비스에 지정한 노선의 수동 해금을 요청함
        // 노선 해금 처리 결과를 반환함
        return progressionService.UnlockRoute(routeID);
    }

    /// <summary>
    /// 현재 플레이어 진행 상태를 집계한 아침 배달 보고서를 반환함
    /// </summary>
    public MorningReportData GetMorningReport()
    {
        // 아침 배달 보고서 서비스가 등록되지 않았다면 보고서를 반환하지 않음
        if(morningReportService == null) return null;
        // 아침 배달 보고서 서비스에 현재 진행 상태 집계를 요청함
        // 생성된 아침 배달 보고서 데이터를 반환함
        return morningReportService.CreateMorningReport();
    }

    /// <summary>
    /// 전체 편지의 수신·읽음·배달 완료 상태를 도감 항목 목록으로 반환함
    /// </summary>
    public IReadOnlyList<LetterCollectionEntryData> GetLetterCollectionEntries()
    {
        // 편지·답장 도감 서비스가 등록되지 않았다면 빈 목록을 반환함
        if(collectionService == null) return System.Array.Empty<LetterCollectionEntryData>();
        // 도감 서비스에 전체 편지 도감 항목 생성을 요청함
        // 생성된 편지 도감 항목 목록을 반환함
        return collectionService.GetLetterCollectionEntries();
    }

    /// <summary>
    /// 전체 답장의 수신·읽음 상태를 도감 항목 목록으로 반환함
    /// </summary>
    public IReadOnlyList<ReplyCollectionEntryData> GetReplyCollectionEntries()
    {
        // 편지·답장 도감 서비스가 등록되지 않았다면 빈 목록을 반환함
        if (collectionService == null) return System.Array.Empty<ReplyCollectionEntryData>();
        // 도감 서비스에 전체 답장 도감 항목 생성을 요청함
        // 생성된 답장 도감 항목 목록을 반환함
        return collectionService.GetReplyCollectionEntries();
    }
}
