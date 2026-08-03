using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryServiceTester : MonoBehaviour
{
    [Header("테스트 대상")]
    [SerializeField] private DeliveryService deliveryService;
    [SerializeField] private ReplyService replyService;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private NightPostTestBootstrap testBootstrap;

    [Header("테스트 ID")]
    [SerializeField] private int courierID = 2001;
    [SerializeField] private int letterID = 1001;
    [SerializeField] private int routeID = 3001;
    [Header("답장 테스트")]
    [SerializeField] private int unreceivedReplyID;

    [Header("전체 흐름 테스트")]
    [SerializeField, Min(0.1f)]
    private float completionCheckInterval = 0.25f;

    [SerializeField, Min(1f)]
    private float fullFlowTimeoutSeconds = 20f;

    private Coroutine fullFlowCoroutine;

    // --------------------------------------------------
    // 02. 배달 시작 조건 준비
    // --------------------------------------------------

    [ContextMenu("02. Prepare Delivery Prerequisites")]
    private void PrepareDeliveryPrerequisites()
    {
        bool result = PrepareDeliveryPrerequisitesInternal();

        LogResult(
            "배달 시작 조건 준비",
            result);
    }

    private bool PrepareDeliveryPrerequisitesInternal()
    {
        if (!EnsureReady()) return false;

        if (courierID <= 0 || letterID <= 0 || routeID <= 0)
        {
            Debug.LogError(
                "[DeliveryTest] Courier, Letter, Route ID는 1 이상이어야 합니다.");
            return false;
        }

        CourierStaticData courierData =
            staticDataCatalog.GetCourier(courierID);

        LetterStaticData letterData =
            staticDataCatalog.GetLetter(letterID);

        RouteStaticData routeData =
            staticDataCatalog.GetRoute(routeID);

        if (courierData == null)
        {
            Debug.LogError(
                $"[DeliveryTest] 배달부 정적 데이터 없음: {courierID}");
            return false;
        }

        if (letterData == null)
        {
            Debug.LogError(
                $"[DeliveryTest] 편지 정적 데이터 없음: {letterID}");
            return false;
        }

        if (routeData == null)
        {
            Debug.LogError(
                $"[DeliveryTest] 노선 정적 데이터 없음: {routeID}");
            return false;
        }

        if (letterData.DestinationRegion != routeData.RegionType)
        {
            Debug.LogError(
                $"[DeliveryTest] 편지와 노선 지역이 일치하지 않습니다. " +
                $"Letter={letterData.DestinationRegion}, " +
                $"Route={routeData.RegionType}");
            return false;
        }
        PlayerSaveData runtimeSaveData = testBootstrap.RuntimeSaveData;
        // 테스트용으로 배달부와 노선을 자동 등록한다.
        if (!runtimeSaveData.OwnedCourierIDs.Contains(courierID))
        {
            runtimeSaveData.OwnedCourierIDs.Add(courierID);
        }

        if (!runtimeSaveData.UnlockedRouteIDs.Contains(routeID))
        {
            runtimeSaveData.UnlockedRouteIDs.Add(routeID);
        }

        LetterProgressData letterProgress =
            playerDataManager.GetLetterProgress(letterID);

        if (letterProgress == null)
        {
            Debug.LogError(
                $"[DeliveryTest] LetterProgressData가 없습니다. " +
                $"테스트용 PlayerSaveData의 LetterProgressList에 " +
                $"LetterID {letterID}를 추가해야 합니다.");
            return false;
        }

        // New 상태라면 테스트를 위해 읽기 및 분류 완료 처리한다.
        if (letterProgress.State == ELetterProgressState.New)
        {
            if (!letterProgress.IsRead)
            {
                letterProgress.MarkAsRead();
            }

            if (!letterProgress.CompleteSorting())
            {
                Debug.LogError(
                    "[DeliveryTest] 편지를 Waiting 상태로 변경하지 못했습니다.");
                return false;
            }
        }

        if (letterProgress.State != ELetterProgressState.Waiting)
        {
            Debug.LogError(
                $"[DeliveryTest] 편지 상태가 Waiting이 아닙니다. " +
                $"현재 상태: {letterProgress.State}\n" +
                $"01번 초기화를 다시 실행하거나 테스트 저장 데이터를 확인하세요.");
            return false;
        }

        return true;
    }

    // --------------------------------------------------
    // 03. 배달 시작 테스트
    // --------------------------------------------------

    [ContextMenu("03. Test Start Delivery")]
    private void TestStartDelivery()
    {
        if (!EnsureReady()) return;

        bool result = deliveryService.StartDelivery(letterID, courierID, routeID);

        LogResult(
            $"배달 시작 Courier={courierID}, " +
            $"Letter={letterID}, Route={routeID}",
            result);

        PrintCurrentDeliveryState();
    }

    // --------------------------------------------------
    // 04. 같은 배달부 중복 시작 차단 테스트
    // --------------------------------------------------

    [ContextMenu("04. Test Duplicate Start Rejected")]
    private void TestDuplicateStartRejected()
    {
        if (!EnsureReady()) return;

        bool startResult = deliveryService.StartDelivery(letterID, courierID, routeID);

        // 이미 배달 중이므로 false가 나와야 테스트 성공
        bool testPassed = !startResult;

        LogResult(
            "배달 중인 배달부의 중복 배달 시작 차단",
            testPassed);
    }

    // --------------------------------------------------
    // 05. 완료된 배달 처리 테스트
    // --------------------------------------------------

    [ContextMenu("05. Process Completed Deliveries")]
    private void TestProcessCompletedDeliveries()
    {
        if (!EnsureReady()) return;

        deliveryService.ProcessCompletedDeliveries();

        LetterProgressData letterProgress =
            playerDataManager.GetLetterProgress(letterID);

        DeliveryResultData deliveryResult =
            playerDataManager.GetDeliveryResult(letterID);

        bool isCompleted =
            letterProgress != null &&
            letterProgress.State == ELetterProgressState.Completed;

        bool resultCreated =
            deliveryResult != null;

        bool activeDeliveryRemoved =
            !playerDataManager.IsCourierDelivering(courierID);

        LogResult(
            "편지 상태 Delivering → Completed",
            isCompleted);

        LogResult(
            "DeliveryResultData 생성",
            resultCreated);

        LogResult(
            "ActiveDeliveryList에서 완료 배달 제거",
            activeDeliveryRemoved);

        PrintCurrentDeliveryState();
    }

    // --------------------------------------------------
    // 06. 결과 확인 및 보상 지급 테스트
    // --------------------------------------------------

    [ContextMenu("06. Check Delivery Result")]
    private void TestCheckDeliveryResult()
    {
        if (!EnsureReady()) return;

        DeliveryResultData deliveryResult =
            playerDataManager.GetDeliveryResult(letterID);

        if (deliveryResult == null)
        {
            Debug.LogError(
                "[DeliveryTest] 확인할 DeliveryResultData가 없습니다.");
            return;
        }
        PlayerSaveData runtimeSaveData = testBootstrap.RuntimeSaveData;
        int currencyBefore = runtimeSaveData.Currency;
        int expectedReward = deliveryResult.RewardAmount;

        bool checkResult =
            deliveryService.CheckDeliveryResult(letterID);

        int currencyAfter = runtimeSaveData.Currency;

        bool rewardAdded =
            currencyAfter == currencyBefore + expectedReward;

        bool markedAsChecked =
            deliveryResult.IsChecked;

        ReplyStaticData replyData =
            staticDataCatalog.GetReplyByLetterID(letterID);

        bool replyReceived =
            replyData != null &&
            playerDataManager.IsReplyReceived(replyData.ReplyID);

        LogResult(
            "배달 결과 확인",
            checkResult);

        LogResult(
            $"보상 지급 {currencyBefore} → {currencyAfter}",
            rewardAdded);

        LogResult(
            "DeliveryResultData IsChecked 변경",
            markedAsChecked);

        LogResult(
            "연결된 답장 획득",
            replyReceived);

        PrintCurrentDeliveryState();
    }

    // --------------------------------------------------
    // 07. 현재 상태 출력
    // --------------------------------------------------

    [ContextMenu("07. Print Current Delivery State")]
    private void PrintCurrentDeliveryState()
    {
        if (!EnsureReady()) return;

        LetterProgressData letterProgress =
            playerDataManager.GetLetterProgress(letterID);

        DeliveryResultData deliveryResult =
            playerDataManager.GetDeliveryResult(letterID);

        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        IReadOnlyList<DeliveryResultData> uncheckedResults =
            playerDataManager.GetUncheckedDeliveryResults();

        IReadOnlyList<int> unreadReplyIDs =
            playerDataManager.GetUnreadReplyIDs();

        string letterState =
            letterProgress == null
                ? "데이터 없음"
                : letterProgress.State.ToString();

        string deliveryResultState =
            deliveryResult == null
                ? "없음"
                : $"있음 / Checked={deliveryResult.IsChecked} / " +
                  $"Reward={deliveryResult.RewardAmount}";
        PlayerSaveData runtimeSaveData = testBootstrap.RuntimeSaveData;
        Debug.Log(
            "[DeliveryTest] 현재 상태\n" +
            $"LetterID: {letterID}\n" +
            $"LetterState: {letterState}\n" +
            $"CourierDelivering: " +
            $"{playerDataManager.IsCourierDelivering(courierID)}\n" +
            $"ActiveDeliveryCount: " +
            $"{activeDeliveries?.Count ?? 0}\n" +
            $"DeliveryResult: {deliveryResultState}\n" +
            $"UncheckedResultCount: " +
            $"{uncheckedResults?.Count ?? 0}\n" +
            $"Currency: {runtimeSaveData.Currency}\n" +
            $"CompletedDeliveryCount: " +
            $"{runtimeSaveData.CompleteDeliveryCount}\n" +
            $"UnreadReplyCount: " +
            $"{unreadReplyIDs?.Count ?? 0}");
    }

    // --------------------------------------------------
    // 08. 전체 배달 흐름 자동 테스트
    // --------------------------------------------------

    [ContextMenu("08. Run Full Delivery Flow Test")]
    private void RunFullDeliveryFlowTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[DeliveryTest] 플레이 모드에서 실행해야 합니다.");
            return;
        }

        if (fullFlowCoroutine != null)
        {
            StopCoroutine(fullFlowCoroutine);
        }

        fullFlowCoroutine =
            StartCoroutine(FullDeliveryFlowCoroutine());
    }

    [ContextMenu("09. Test Open Reply")]
    private void TestOpenReply()
    {
        // ReplyService가 연결되지 않았다면 종료한다.
        if (replyService == null || playerDataManager == null || staticDataCatalog)
        {
            Debug.LogError(
                "[ReplyTest] 필요한 서비스가 연결되지 않았습니다.");
            return;
        }

        // letterID에 연결된 ReplyStaticData를 조회한다.
        ReplyStaticData replyStaticData = staticDataCatalog.GetReplyByLetterID(letterID);

        // 연결된 답장이 없다면 테스트를 종료한다.
        if (replyStaticData == null)
        {
            Debug.LogError(
                $"[ReplyTest] Letter {letterID}에 연결된 답장이 없습니다.");
            return;
        }

        // 답장을 열기 전 읽지 않은 답장 수를 저장한다.
        int replyID = replyStaticData.ReplyID;
        int unreadCountBefore = playerDataManager.GetUnreadReplyIDs().Count;

        // ReplyService.OpenReply(replyID)를 호출한다.
        ReplyStaticData reply = replyService.OpenReply(replyID);
        LogResult("답장 열기 성공",reply != null);
        // 반환된 ReplyStaticData가 null이 아닌지 확인한다.
        if (reply == null) return;

        // 해당 답장이 읽음 상태로 변경됐는지 확인한다.
        bool isRead = playerDataManager.IsReplyRead(replyID);
        LogResult("답장 읽음 상태 변경",isRead);
        // 읽지 않은 답장 수가 1 감소했는지 확인한다.
        int unreadCountAfter = playerDataManager.GetUnreadReplyIDs().Count;
        // 같은 답장을 다시 열어도 정상적으로 반환되는지 확인한다.
        LogResult(
        $"읽지 않은 답장 수 감소 " +
        $"{unreadCountBefore} → {unreadCountAfter}",
        unreadCountAfter == unreadCountBefore - 1);

        ReplyStaticData reopenedReply = replyService.OpenReply(replyID);

        LogResult(
       "읽은 답장 다시 열기",
       reopenedReply != null);
    }
    [ContextMenu("10. Test Unreceived Reply Rejected")]
    private void TestUnreceivedReplyRejected()
    {
        if (replyService == null || playerDataManager == null)
        {
            Debug.LogError(
                "[ReplyTest] ReplyService 또는 PlayerDataManager가 연결되지 않았습니다.");
            return;
        }

        if (unreceivedReplyID <= 0)
        {
            Debug.LogError(
                "[ReplyTest] 테스트할 unreceivedReplyID를 설정해야 합니다.");
            return;
        }

        // 이미 획득한 답장이면 '받지 않은 답장' 테스트로 사용할 수 없다.
        if (playerDataManager.IsReplyReceived(unreceivedReplyID))
        {
            Debug.LogError(
                $"[ReplyTest] ReplyID {unreceivedReplyID}는 이미 받은 답장입니다.");
            return;
        }

        ReplyStaticData result =
            replyService.OpenReply(unreceivedReplyID);

        // 받지 않은 답장이므로 null이 반환돼야 테스트 성공
        LogResult(
            $"받지 않은 답장 열기 차단 ReplyID={unreceivedReplyID}",
            result == null);
    }
    private IEnumerator FullDeliveryFlowCoroutine()
    {
        if (replyService == null)
        {
            Debug.LogError(
                "[DeliveryTest] ReplyService가 연결되지 않았습니다.");
            yield break;
        }
        Debug.Log(
            "========== Delivery Full Flow Test 시작 ==========");

        yield return null;

        if (!PrepareDeliveryPrerequisitesInternal())
        {
            Debug.LogError(
                "[DeliveryTest] 시작 조건 준비 실패");
            yield break;
        }
        PlayerSaveData runtimeSaveData = testBootstrap.RuntimeSaveData;
        int currencyBefore = runtimeSaveData.Currency;

        int completedCountBefore = runtimeSaveData.CompleteDeliveryCount;

        // 1. 배달 시작
        bool startResult =
            deliveryService.StartDelivery(letterID,courierID,routeID);

        LogResult(
            "1. 배달 시작",
            startResult);

        if (!startResult)
        {
            yield break;
        }

        // 2. 동일 배달부 중복 시작 차단
        bool duplicateStartResult =
            deliveryService.StartDelivery(letterID, courierID, routeID);

        LogResult(
            "2. 중복 배달 시작 차단",
            !duplicateStartResult);

        ActiveDeliveryData activeDelivery =
            FindActiveDelivery();

        if (activeDelivery == null)
        {
            Debug.LogError(
                "[DeliveryTest] ActiveDeliveryData가 생성되지 않았습니다.");
            yield break;
        }

        // 3. 완료 시각까지 대기
        float elapsedTime = 0f;

        while (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
               < activeDelivery.CompleteAtUnixTime)
        {
            if (elapsedTime >= fullFlowTimeoutSeconds)
            {
                Debug.LogError(
                    "[DeliveryTest] 배달 완료 대기 시간이 초과되었습니다.\n" +
                    "테스트용 RouteStaticData의 " +
                    "BaseDeliveryTimeSeconds를 3~5초 정도로 설정하세요.");

                yield break;
            }

            yield return new WaitForSecondsRealtime(
                completionCheckInterval);

            elapsedTime += completionCheckInterval;
        }

        // 4. 완료 처리
        deliveryService.ProcessCompletedDeliveries();

        LetterProgressData letterProgress =
            playerDataManager.GetLetterProgress(letterID);

        DeliveryResultData deliveryResult =
            playerDataManager.GetDeliveryResult(letterID);

        LogResult(
            "3. 편지 상태 Completed",
            letterProgress != null &&
            letterProgress.State == ELetterProgressState.Completed);

        LogResult(
            "4. 진행 중 배달 제거",
            !playerDataManager.IsCourierDelivering(courierID));

        LogResult(
            "5. 배달 결과 생성",
            deliveryResult != null);

        LogResult(
            "6. 완료 배달 수 증가",
            runtimeSaveData.CompleteDeliveryCount ==
            completedCountBefore + 1);

        if (deliveryResult == null)
        {
            yield break;
        }

        int expectedReward =
            deliveryResult.RewardAmount;

        ReplyStaticData replyData =
            staticDataCatalog.GetReplyByLetterID(letterID);

        if (replyData == null)
        {
            Debug.LogError(
                $"[DeliveryTest] 편지 {letterID}에 연결된 답장이 없습니다.");
            yield break;
        }

        // 5. 배달 결과 확인
        bool checkResult =
            deliveryService.CheckDeliveryResult(letterID);

        LogResult(
            "7. 배달 결과 확인",
            checkResult);

        LogResult(
            "8. 보상 지급",
            runtimeSaveData.Currency ==
            currencyBefore + expectedReward);

        LogResult(
            "9. 결과 IsChecked 변경",
            deliveryResult.IsChecked);

        LogResult(
            "10. 답장 획득",
            playerDataManager.IsReplyReceived(replyData.ReplyID));

        // 이미 확인한 결과를 다시 확인하면 false가 나와야 함
        bool duplicateCheckResult =
            deliveryService.CheckDeliveryResult(letterID);

        LogResult(
            "11. 중복 보상 지급 차단",
            !duplicateCheckResult);

        PrintCurrentDeliveryState();
        // 6. 답장 열기 및 읽음 처리 테스트
        int replyID = replyData.ReplyID;

        int unreadCountBefore =
            playerDataManager.GetUnreadReplyIDs().Count;

        LogResult(
            "12. 획득한 답장이 미읽음 상태",
            playerDataManager.IsReplyReceived(replyID) &&
            !playerDataManager.IsReplyRead(replyID) &&
            unreadCountBefore > 0);

        // 처음 답장 열기
        ReplyStaticData openedReply =
            replyService.OpenReply(replyID);

        LogResult(
            "13. 답장 열기",
            openedReply != null);

        // 답장을 열면 읽음 상태로 변경돼야 함
        LogResult(
            "14. 답장 읽음 상태 변경",
            playerDataManager.IsReplyRead(replyID));

        // 미읽음 답장 수가 하나 감소해야 함
        int unreadCountAfter =
            playerDataManager.GetUnreadReplyIDs().Count;

        LogResult(
            $"15. 미읽음 답장 수 감소 " +
            $"{unreadCountBefore} → {unreadCountAfter}",
            unreadCountAfter == unreadCountBefore - 1);

        // 이미 읽은 답장도 다시 열 수 있어야 함
        ReplyStaticData reopenedReply =
            replyService.OpenReply(replyID);

        LogResult(
            "16. 읽은 답장 다시 열기",
            reopenedReply != null);
        Debug.Log(
            "========== Delivery Full Flow Test 종료 ==========");

        fullFlowCoroutine = null;
    }

    // --------------------------------------------------
    // 내부 공통 함수
    // --------------------------------------------------

    private bool EnsureReady()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[DeliveryTest] 플레이 모드에서 실행해야 합니다.");
            return false;
        }

        if (deliveryService == null)
        {
            Debug.LogError(
                "[DeliveryTest] DeliveryService가 연결되지 않았습니다.");
            return false;
        }

        if (playerDataManager == null)
        {
            Debug.LogError(
                "[DeliveryTest] PlayerDataManager가 연결되지 않았습니다.");
            return false;
        }

        if (staticDataCatalog == null)
        {
            Debug.LogError(
                "[DeliveryTest] StaticDataCatalog가 연결되지 않았습니다.");
            return false;
        }
        PlayerSaveData runtimeSaveData = testBootstrap.RuntimeSaveData;

        return runtimeSaveData != null;
    }

    private ActiveDeliveryData FindActiveDelivery()
    {
        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        if (activeDeliveries == null)
        {
            return null;
        }

        foreach (ActiveDeliveryData deliveryData in activeDeliveries)
        {
            if (deliveryData == null) continue;

            if (deliveryData.LetterID == letterID)
            {
                return deliveryData;
            }
        }

        return null;
    }

    private void LogResult(string testName, bool success)
    {
        if (success)
        {
            Debug.Log(
                $"[DeliveryTest][PASS] {testName}");
        }
        else
        {
            Debug.LogError(
                $"[DeliveryTest][FAIL] {testName}");
        }
    }
}
