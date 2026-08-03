using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConcurrentDeliveryFlowTester : MonoBehaviour
{
    [Header("공통 테스트 데이터")]
    [SerializeField] private GameBootstrap gameBootstrap;

    [Header("서비스")]
    [SerializeField] private LetterService letterService;
    [SerializeField] private DeliveryService deliveryService;
    [SerializeField] private ReplyService replyService;
    [SerializeField] private ProgressionService progressionService;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private StaticDataCatalog staticDataCatalog;

    [Header("첫 번째 배달")]
    [SerializeField] private int firstLetterID = 1001;
    [SerializeField] private int firstCourierID = 2001;
    [SerializeField] private int firstRouteID = 3001;

    [Header("두 번째 배달")]
    [SerializeField] private int secondLetterID = 1002;
    [SerializeField] private int secondCourierID = 2002;
    [SerializeField] private int secondRouteID = 3001;

    [Header("시간 설정")]
    [SerializeField, Min(0.1f)]
    private float completionCheckInterval = 0.25f;

    [Header("진행도 해금 테스트")]
    [SerializeField] private int unlockTestCourierID;
    [SerializeField] private int unlockTestRouteID;

    [SerializeField, Min(1f)]
    private float timeoutSeconds = 30f;

    private Coroutine testCoroutine;
    private bool waitTimedOut;
    private bool isTestRunning;

    private int currencyChangedEventCount;
    private int letterReceivedEventCount;
    private int letterReadEventCount;
    private int letterStateChangedEventCount;
    private int waitingStateChangedEventCount;
    private int deliveringStateChangedEventCount;
    private int completedStateChangedEventCount;
    private int deliveryStartedEventCount;
    private int deliveryCompletedEventCount;
    private int deliveryResultCheckedEventCount;
    private int replyReceivedEventCount;
    private int replyReadEventCount;
    private int unreadReplyCountChangedEventCount;
    private int courierUnlockedEventCount;
    private int routeUnlockedEventCount;
    private int targetCourierUnlockedEventCount;
    private int targetRouteUnlockedEventCount;

    private int lastCurrency;
    private int lastUnreadReplyCount = -1;
    private int targetCourierUnlockedAtCompletedCount = -1;
    private int targetRouteUnlockedAtCompletedCount = -1;

    private readonly HashSet<int> receivedLetterEventIDs = new();
    private readonly HashSet<int> readLetterEventIDs = new();
    private readonly HashSet<int> startedDeliveryEventIDs = new();
    private readonly HashSet<int> completedDeliveryEventIDs = new();
    private readonly HashSet<int> checkedResultEventIDs = new();
    private readonly HashSet<int> receivedReplyEventIDs = new();
    private readonly HashSet<int> readReplyEventIDs = new();
    private readonly HashSet<int> unlockedCourierEventIDs = new();
    private readonly HashSet<int> unlockedRouteEventIDs = new();

    private void OnEnable()
    {
        GameEvents.CurrencyChanged += OnCurrencyChanged;

        GameEvents.LetterReceived += OnLetterReceived;
        GameEvents.LetterRead += OnLetterRead;
        GameEvents.LetterStateChanged += OnLetterStateChanged;

        GameEvents.DeliveryStarted += OnDeliveryStarted;
        GameEvents.DeliveryCompleted += OnDeliveryCompleted;
        GameEvents.DeliveryResultChecked += OnDeliveryResultChecked;

        GameEvents.ReplyReceived += OnReplyReceived;
        GameEvents.ReplyRead += OnReplyRead;
        GameEvents.UnreadReplyCountChanged += OnUnreadReplyCountChanged;

        GameEvents.CourierUnlocked += OnCourierUnlocked;
        GameEvents.RouteUnlocked += OnRouteUnlocked;
    }

    private void OnDisable()
    {
        GameEvents.CurrencyChanged -= OnCurrencyChanged;

        GameEvents.LetterReceived -= OnLetterReceived;
        GameEvents.LetterRead -= OnLetterRead;
        GameEvents.LetterStateChanged -= OnLetterStateChanged;

        GameEvents.DeliveryStarted -= OnDeliveryStarted;
        GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
        GameEvents.DeliveryResultChecked -= OnDeliveryResultChecked;

        GameEvents.ReplyReceived -= OnReplyReceived;
        GameEvents.ReplyRead -= OnReplyRead;
        GameEvents.UnreadReplyCountChanged -= OnUnreadReplyCountChanged;

        GameEvents.CourierUnlocked -= OnCourierUnlocked;
        GameEvents.RouteUnlocked -= OnRouteUnlocked;

        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
            testCoroutine = null;
        }

        isTestRunning = false;
    }

    [ContextMenu("Run Two Concurrent Delivery Test")]
    private void RunTwoConcurrentDeliveryTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 플레이 모드에서 실행해야 합니다.");
            return;
        }

        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
            testCoroutine = null;
        }

        isTestRunning = false;
        testCoroutine = StartCoroutine(RunTestWrapper());
    }

    private IEnumerator RunTestWrapper()
    {
        yield return TwoConcurrentDeliveryFlowCoroutine();

        isTestRunning = false;
        testCoroutine = null;
    }

    private IEnumerator TwoConcurrentDeliveryFlowCoroutine()
    {
        Debug.Log(
            "========== Two Concurrent Delivery Test 시작 ==========");

        if (!EnsureReady())
        {
            yield break;
        }

        PlayerSaveData saveData = gameBootstrap.RuntimeSaveData;

        if (!PrepareCleanTestState(saveData))
        {
            yield break;
        }

        if (!ValidateStaticData())
        {
            yield break;
        }

        ResetEventTracking();
        isTestRunning = true;

        int currencyBefore =
            saveData.Currency;

        int completedCountBefore =
            saveData.CompleteDeliveryCount;

        LogResult(
            "[진행도 1] 테스트 시작 시 배달부 잠금 상태",
            !playerDataManager.IsCourierOwned(unlockTestCourierID));

        LogResult(
            "[진행도 2] 테스트 시작 시 노선 잠금 상태",
            !playerDataManager.IsRouteUnlocked(unlockTestRouteID));

        // --------------------------------------------------
        // 1. 편지 두 개를 Waiting 상태까지 준비
        // --------------------------------------------------

        bool firstLetterPrepared =
            PrepareLetter(firstLetterID);

        LogResult(
            $"1. 첫 번째 편지 준비 LetterID={firstLetterID}",
            firstLetterPrepared);

        bool secondLetterPrepared =
            PrepareLetter(secondLetterID);

        LogResult(
            $"2. 두 번째 편지 준비 LetterID={secondLetterID}",
            secondLetterPrepared);

        if (!firstLetterPrepared || !secondLetterPrepared)
        {
            yield break;
        }

        LogResult(
            "3. 두 편지 모두 Waiting 상태",
            IsLetterState(
                firstLetterID,
                ELetterProgressState.Waiting) &&
            IsLetterState(
                secondLetterID,
                ELetterProgressState.Waiting));

        // --------------------------------------------------
        // 2. 첫 번째 배달 시작
        // --------------------------------------------------

        bool firstStartResult =
            deliveryService.StartDelivery(firstLetterID,firstCourierID,firstRouteID);

        LogResult(
            $"4. 첫 번째 배달 시작 " +
            $"Letter={firstLetterID}, Courier={firstCourierID}",
            firstStartResult);

        if (!firstStartResult)
        {
            yield break;
        }

        // --------------------------------------------------
        // 3. 같은 배달부로 두 번째 배달 시도
        // --------------------------------------------------

        bool sameCourierStartResult =
            deliveryService.StartDelivery(secondLetterID,
                firstCourierID,
                secondRouteID);

        LogResult(
            "5. 같은 배달부의 동시 배달 차단",
            !sameCourierStartResult);

        // --------------------------------------------------
        // 4. 다른 배달부로 두 번째 배달 시작
        // --------------------------------------------------

        bool secondStartResult =
            deliveryService.StartDelivery(
                secondLetterID,
                secondCourierID,
                secondRouteID);

        LogResult(
            $"6. 두 번째 배달 시작 " +
            $"Letter={secondLetterID}, Courier={secondCourierID}",
            secondStartResult);

        if (!secondStartResult)
        {
            yield break;
        }

        // --------------------------------------------------
        // 5. 두 배달이 동시에 진행 중인지 확인
        // --------------------------------------------------

        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        LogResult(
            $"7. ActiveDeliveryList에 2개 존재 " +
            $"Count={activeDeliveries?.Count ?? 0}",
            activeDeliveries != null &&
            activeDeliveries.Count == 2);

        LogResult(
            "8. 두 편지 모두 Delivering 상태",
            IsLetterState(
                firstLetterID,
                ELetterProgressState.Delivering) &&
            IsLetterState(
                secondLetterID,
                ELetterProgressState.Delivering));

        LogResult(
            "9. 두 배달부 모두 배달 중",
            playerDataManager.IsCourierDelivering(firstCourierID) &&
            playerDataManager.IsCourierDelivering(secondCourierID));

        ActiveDeliveryData firstActiveDelivery =
            FindActiveDelivery(firstLetterID);

        ActiveDeliveryData secondActiveDelivery =
            FindActiveDelivery(secondLetterID);

        LogResult(
            "10. 첫 번째 ActiveDeliveryData 생성",
            firstActiveDelivery != null);

        LogResult(
            "11. 두 번째 ActiveDeliveryData 생성",
            secondActiveDelivery != null);

        if (firstActiveDelivery == null ||
            secondActiveDelivery == null)
        {
            yield break;
        }

        Debug.Log(
            "[ConcurrentDeliveryTest] 동시 배달 상태\n" +
            $"First Letter: {firstLetterID}\n" +
            $"First CompleteAt: " +
            $"{firstActiveDelivery.CompleteAtUnixTime}\n" +
            $"Second Letter: {secondLetterID}\n" +
            $"Second CompleteAt: " +
            $"{secondActiveDelivery.CompleteAtUnixTime}\n" +
            $"ActiveDeliveryCount: " +
            $"{activeDeliveries.Count}");

        // --------------------------------------------------
        // 6. 완료 시각이 다르면 한 건만 먼저 완료되는지 확인
        // --------------------------------------------------

        long firstCompleteAt =
            firstActiveDelivery.CompleteAtUnixTime;

        long secondCompleteAt =
            secondActiveDelivery.CompleteAtUnixTime;

        long earliestCompleteAt =
            Math.Min(firstCompleteAt, secondCompleteAt);

        long latestCompleteAt =
            Math.Max(firstCompleteAt, secondCompleteAt);

        if (firstCompleteAt != secondCompleteAt)
        {
            yield return WaitUntilUnixTime(
                earliestCompleteAt,
                "첫 번째 완료 시각 대기");

            if (waitTimedOut)
            {
                yield break;
            }

            deliveryService.ProcessCompletedDeliveries();

            bool firstCompletedEarlier =
                firstCompleteAt < secondCompleteAt;

            int earlierLetterID =
                firstCompletedEarlier
                    ? firstLetterID
                    : secondLetterID;

            int laterLetterID =
                firstCompletedEarlier
                    ? secondLetterID
                    : firstLetterID;

            int earlierCourierID =
                firstCompletedEarlier
                    ? firstCourierID
                    : secondCourierID;

            int laterCourierID =
                firstCompletedEarlier
                    ? secondCourierID
                    : firstCourierID;

            LogResult(
                $"12. 먼저 끝난 편지만 Completed " +
                $"LetterID={earlierLetterID}",
                IsLetterState(
                    earlierLetterID,
                    ELetterProgressState.Completed));

            LogResult(
                $"13. 나중에 끝나는 편지는 Delivering 유지 " +
                $"LetterID={laterLetterID}",
                IsLetterState(
                    laterLetterID,
                    ELetterProgressState.Delivering));

            activeDeliveries =
                playerDataManager.GetActiveDeliveries();

            LogResult(
                $"14. 한 건 완료 후 ActiveDeliveryCount=1 " +
                $"Current={activeDeliveries?.Count ?? 0}",
                activeDeliveries != null &&
                activeDeliveries.Count == 1);

            LogResult(
                "15. 먼저 끝난 배달부만 사용 가능 상태",
                !playerDataManager.IsCourierDelivering(
                    earlierCourierID) &&
                playerDataManager.IsCourierDelivering(
                    laterCourierID));

            LogResult(
                "16. 먼저 끝난 편지 결과만 생성",
                playerDataManager.GetDeliveryResult(
                    earlierLetterID) != null &&
                playerDataManager.GetDeliveryResult(
                    laterLetterID) == null);
        }
        else
        {
            Debug.LogWarning(
                "[ConcurrentDeliveryTest] 두 배달의 완료 예정 시각이 같습니다.\n" +
                "동시 진행 여부는 확인되지만, 한 건만 먼저 완료되는 검사는 " +
                "생략됩니다.\n" +
                "서로 다른 Route 시간이나 Courier 속도를 사용하면 " +
                "순차 완료도 확인할 수 있습니다.");
        }

        // --------------------------------------------------
        // 7. 두 번째 배달까지 완료될 때까지 대기
        // --------------------------------------------------

        yield return WaitUntilUnixTime(
            latestCompleteAt,
            "전체 배달 완료 시각 대기");

        if (waitTimedOut)
        {
            yield break;
        }

        deliveryService.ProcessCompletedDeliveries();

        DeliveryResultData firstResult =
            playerDataManager.GetDeliveryResult(firstLetterID);

        DeliveryResultData secondResult =
            playerDataManager.GetDeliveryResult(secondLetterID);

        LogResult(
            "17. 두 편지 모두 Completed",
            IsLetterState(
                firstLetterID,
                ELetterProgressState.Completed) &&
            IsLetterState(
                secondLetterID,
                ELetterProgressState.Completed));

        activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        LogResult(
            $"18. 모든 배달이 ActiveDeliveryList에서 제거 " +
            $"Count={activeDeliveries?.Count ?? 0}",
            activeDeliveries != null &&
            activeDeliveries.Count == 0);

        LogResult(
            "19. 두 배달부 모두 사용 가능 상태",
            !playerDataManager.IsCourierDelivering(firstCourierID) &&
            !playerDataManager.IsCourierDelivering(secondCourierID));

        LogResult(
            "20. 첫 번째 DeliveryResultData 생성",
            firstResult != null);

        LogResult(
            "21. 두 번째 DeliveryResultData 생성",
            secondResult != null);

        LogResult(
            $"22. 완료 배달 수 2 증가 " +
            $"{completedCountBefore} → " +
            $"{saveData.CompleteDeliveryCount}",
            saveData.CompleteDeliveryCount ==
            completedCountBefore + 2);

        if (firstResult == null || secondResult == null)
        {
            yield break;
        }

        LogResult(
            "23. 두 결과가 아직 미확인 상태",
            !firstResult.IsChecked &&
            !secondResult.IsChecked);

        LogResult(
            "[진행도 3] 배달 2회 완료 후 배달부 해금",
            playerDataManager.IsCourierOwned(unlockTestCourierID));

        LogResult(
            "[진행도 4] 배달 2회 완료 후 노선 해금",
            playerDataManager.IsRouteUnlocked(unlockTestRouteID));

        LogResult(
            "[진행도 5] CourierUnlocked가 대상 배달부에 1회 발생",
            targetCourierUnlockedEventCount == 1 &&
            unlockedCourierEventIDs.Contains(unlockTestCourierID));

        LogResult(
            "[진행도 6] RouteUnlocked가 대상 노선에 1회 발생",
            targetRouteUnlockedEventCount == 1 &&
            unlockedRouteEventIDs.Contains(unlockTestRouteID));

        LogResult(
            "[진행도 7] 배달부가 완료 횟수 2에서 해금",
            targetCourierUnlockedAtCompletedCount == 2);

        LogResult(
            "[진행도 8] 노선이 완료 횟수 2에서 해금",
            targetRouteUnlockedAtCompletedCount == 2);

        int targetCourierEventCountBeforeReevaluate =
            targetCourierUnlockedEventCount;

        int targetRouteEventCountBeforeReevaluate =
            targetRouteUnlockedEventCount;

        progressionService.EvaluateProgressUnlocks();

        LogResult(
            "[진행도 9] 재평가 시 배달부 해금 이벤트 중복 없음",
            targetCourierUnlockedEventCount ==
            targetCourierEventCountBeforeReevaluate);

        LogResult(
            "[진행도 10] 재평가 시 노선 해금 이벤트 중복 없음",
            targetRouteUnlockedEventCount ==
            targetRouteEventCountBeforeReevaluate);

        // --------------------------------------------------
        // 8. 첫 번째 결과만 확인
        // --------------------------------------------------

        int firstReward =
            firstResult.RewardAmount;

        int secondReward =
            secondResult.RewardAmount;

        ReplyStaticData firstReply =
            staticDataCatalog.GetReplyByLetterID(firstLetterID);

        ReplyStaticData secondReply =
            staticDataCatalog.GetReplyByLetterID(secondLetterID);

        bool firstCheckResult =
            deliveryService.CheckDeliveryResult(firstLetterID);

        LogResult(
            "24. 첫 번째 배달 결과 확인",
            firstCheckResult);

        LogResult(
            "25. 첫 번째 결과만 Checked 상태",
            firstResult.IsChecked &&
            !secondResult.IsChecked);

        LogResult(
            $"26. 첫 번째 보상만 지급 " +
            $"{currencyBefore} → {saveData.Currency}",
            saveData.Currency ==
            currencyBefore + firstReward);

        LogResult(
            "27. 첫 번째 답장만 획득",
            firstReply != null &&
            secondReply != null &&
            playerDataManager.IsReplyReceived(firstReply.ReplyID) &&
            !playerDataManager.IsReplyReceived(secondReply.ReplyID));

        // --------------------------------------------------
        // 9. 두 번째 결과 확인
        // --------------------------------------------------

        bool secondCheckResult =
            deliveryService.CheckDeliveryResult(secondLetterID);

        LogResult(
            "28. 두 번째 배달 결과 확인",
            secondCheckResult);

        LogResult(
            "29. 두 결과 모두 Checked 상태",
            firstResult.IsChecked &&
            secondResult.IsChecked);

        LogResult(
            $"30. 두 보상이 각각 지급 " +
            $"Expected={currencyBefore + firstReward + secondReward}, " +
            $"Current={saveData.Currency}",
            saveData.Currency ==
            currencyBefore + firstReward + secondReward);

        LogResult(
            "31. 두 답장이 각각 획득됨",
            firstReply != null &&
            secondReply != null &&
            playerDataManager.IsReplyReceived(firstReply.ReplyID) &&
            playerDataManager.IsReplyReceived(secondReply.ReplyID));

        // --------------------------------------------------
        // 10. 중복 보상 차단
        // --------------------------------------------------

        int currencyBeforeDuplicateCheck = saveData.Currency;
        int currencyEventCountBeforeDuplicate =
            currencyChangedEventCount;
        int resultEventCountBeforeDuplicate =
            deliveryResultCheckedEventCount;
        int replyEventCountBeforeDuplicate =
            replyReceivedEventCount;

        bool duplicateFirstCheck =
            deliveryService.CheckDeliveryResult(firstLetterID);

        bool duplicateSecondCheck =
            deliveryService.CheckDeliveryResult(secondLetterID);

        LogResult(
            "32. 첫 번째 결과 중복 확인 차단",
            !duplicateFirstCheck);

        LogResult(
            "33. 두 번째 결과 중복 확인 차단",
            !duplicateSecondCheck);

        LogResult(
            "34. 중복 확인 시 재화 추가 지급 없음",
            saveData.Currency == currencyBeforeDuplicateCheck);

        LogResult(
            "35. 중복 확인 시 CurrencyChanged 추가 발생 없음",
            currencyChangedEventCount ==
            currencyEventCountBeforeDuplicate);

        LogResult(
            "36. 중복 확인 시 DeliveryResultChecked 추가 발생 없음",
            deliveryResultCheckedEventCount ==
            resultEventCountBeforeDuplicate);

        LogResult(
            "37. 중복 확인 시 ReplyReceived 추가 발생 없음",
            replyReceivedEventCount ==
            replyEventCountBeforeDuplicate);

        // --------------------------------------------------
        // 11. 편지 재열람 이벤트 중복 차단
        // --------------------------------------------------

        int letterReadCountBeforeReopen =
            letterReadEventCount;

        LetterStaticData reopenedLetter =
            letterService.OpenLetter(firstLetterID);

        LogResult(
            "38. 이미 읽은 편지도 다시 열기 가능",
            reopenedLetter != null);

        LogResult(
            "39. 편지 재열람 시 LetterRead 추가 발생 없음",
            letterReadEventCount ==
            letterReadCountBeforeReopen);

        // --------------------------------------------------
        // 12. 답장 열람 및 재열람
        // --------------------------------------------------

        if (firstReply == null || secondReply == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 답장 정적 데이터가 없습니다.");
            yield break;
        }

        ReplyStaticData openedFirstReply =
            replyService.OpenReply(firstReply.ReplyID);

        LogResult(
            "40. 첫 번째 답장 열람 성공",
            openedFirstReply != null);

        LogResult(
            "41. 첫 번째 답장 읽음 상태",
            playerDataManager.IsReplyRead(firstReply.ReplyID));

        LogResult(
            "42. 첫 번째 답장 열람 후 미열람 수 1",
            playerDataManager.GetUnreadReplyIDs().Count == 1 &&
            lastUnreadReplyCount == 1);

        int firstReplyReadCountBeforeReopen =
            replyReadEventCount;

        int firstUnreadEventCountBeforeReopen =
            unreadReplyCountChangedEventCount;

        ReplyStaticData reopenedFirstReply =
            replyService.OpenReply(firstReply.ReplyID);

        LogResult(
            "43. 첫 번째 답장 재열람 가능",
            reopenedFirstReply != null);

        LogResult(
            "44. 첫 번째 답장 재열람 이벤트 중복 없음",
            replyReadEventCount ==
            firstReplyReadCountBeforeReopen &&
            unreadReplyCountChangedEventCount ==
            firstUnreadEventCountBeforeReopen);

        ReplyStaticData openedSecondReply =
            replyService.OpenReply(secondReply.ReplyID);

        LogResult(
            "45. 두 번째 답장 열람 성공",
            openedSecondReply != null);

        LogResult(
            "46. 두 번째 답장 읽음 상태",
            playerDataManager.IsReplyRead(secondReply.ReplyID));

        LogResult(
            "47. 두 답장 열람 후 미열람 수 0",
            playerDataManager.GetUnreadReplyIDs().Count == 0 &&
            lastUnreadReplyCount == 0);

        int secondReplyReadCountBeforeReopen =
            replyReadEventCount;

        int secondUnreadEventCountBeforeReopen =
            unreadReplyCountChangedEventCount;

        ReplyStaticData reopenedSecondReply =
            replyService.OpenReply(secondReply.ReplyID);

        LogResult(
            "48. 두 번째 답장 재열람 가능",
            reopenedSecondReply != null);

        LogResult(
            "49. 두 번째 답장 재열람 이벤트 중복 없음",
            replyReadEventCount ==
            secondReplyReadCountBeforeReopen &&
            unreadReplyCountChangedEventCount ==
            secondUnreadEventCountBeforeReopen);

        // --------------------------------------------------
        // 13. 전체 이벤트 결과 검증
        // --------------------------------------------------

        LogResult(
            "50. LetterReceived 이벤트가 편지별 1회",
            letterReceivedEventCount == 2 &&
            receivedLetterEventIDs.Count == 2 &&
            receivedLetterEventIDs.Contains(firstLetterID) &&
            receivedLetterEventIDs.Contains(secondLetterID));

        LogResult(
            "51. LetterRead 이벤트가 편지별 1회",
            letterReadEventCount == 2 &&
            readLetterEventIDs.Count == 2 &&
            readLetterEventIDs.Contains(firstLetterID) &&
            readLetterEventIDs.Contains(secondLetterID));

        LogResult(
            "52. 편지 상태 변경 이벤트가 상태별 2회",
            letterStateChangedEventCount == 6 &&
            waitingStateChangedEventCount == 2 &&
            deliveringStateChangedEventCount == 2 &&
            completedStateChangedEventCount == 2);

        LogResult(
            "53. DeliveryStarted 이벤트가 배달별 1회",
            deliveryStartedEventCount == 2 &&
            startedDeliveryEventIDs.Count == 2 &&
            startedDeliveryEventIDs.Contains(firstLetterID) &&
            startedDeliveryEventIDs.Contains(secondLetterID));

        LogResult(
            "54. DeliveryCompleted 이벤트가 배달별 1회",
            deliveryCompletedEventCount == 2 &&
            completedDeliveryEventIDs.Count == 2 &&
            completedDeliveryEventIDs.Contains(firstLetterID) &&
            completedDeliveryEventIDs.Contains(secondLetterID));

        LogResult(
            "55. DeliveryResultChecked 이벤트가 결과별 1회",
            deliveryResultCheckedEventCount == 2 &&
            checkedResultEventIDs.Count == 2 &&
            checkedResultEventIDs.Contains(firstLetterID) &&
            checkedResultEventIDs.Contains(secondLetterID));

        LogResult(
            "56. CurrencyChanged 이벤트 2회 및 최종 재화 일치",
            currencyChangedEventCount == 2 &&
            lastCurrency == saveData.Currency);

        LogResult(
            "57. ReplyReceived 이벤트가 답장별 1회",
            replyReceivedEventCount == 2 &&
            receivedReplyEventIDs.Count == 2 &&
            receivedReplyEventIDs.Contains(firstReply.ReplyID) &&
            receivedReplyEventIDs.Contains(secondReply.ReplyID));

        LogResult(
            "58. ReplyRead 이벤트가 답장별 1회",
            replyReadEventCount == 2 &&
            readReplyEventIDs.Count == 2 &&
            readReplyEventIDs.Contains(firstReply.ReplyID) &&
            readReplyEventIDs.Contains(secondReply.ReplyID));

        LogResult(
            "59. UnreadReplyCountChanged 총 4회 및 최종 값 0",
            unreadReplyCountChangedEventCount == 4 &&
            lastUnreadReplyCount == 0);

        PrintFinalState(saveData, firstResult, secondResult);

        Debug.Log(
            "========== Two Concurrent Delivery Test 종료 ==========");
    }

    private bool PrepareCleanTestState(PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] RuntimeSaveData가 없습니다.");
            return false;
        }

        if (saveData.LetterProgressesList == null ||
            saveData.ActiveDeliveryList == null ||
            saveData.DeliveryResultsList == null ||
            saveData.OwnedCourierIDs == null ||
            saveData.UnlockedRouteIDs == null ||
            saveData.ReceivedReplyIDs == null ||
            saveData.ReadReplyIds == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] PlayerSaveData 목록 중 " +
                "초기화되지 않은 목록이 있습니다.");
            return false;
        }

        if (saveData.CompleteDeliveryCount != 0)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 진행도 해금 테스트는 " +
                "완료 배달 수가 0인 상태에서 시작해야 합니다.\n" +
                "플레이 모드를 다시 시작한 뒤 한 번만 실행하세요.");
            return false;
        }

        // 이번 테스트는 깨끗한 상태에서 시작
        saveData.LetterProgressesList.Clear();
        saveData.ActiveDeliveryList.Clear();
        saveData.DeliveryResultsList.Clear();
        saveData.ReceivedReplyIDs.Clear();
        saveData.ReadReplyIds.Clear();
        saveData.OwnedCourierIDs.Remove(unlockTestCourierID);
        saveData.UnlockedRouteIDs.Remove(unlockTestRouteID);

        AddUniqueID(
            saveData.OwnedCourierIDs,
            firstCourierID);

        AddUniqueID(
            saveData.OwnedCourierIDs,
            secondCourierID);

        AddUniqueID(
            saveData.UnlockedRouteIDs,
            firstRouteID);

        AddUniqueID(
            saveData.UnlockedRouteIDs,
            secondRouteID);

        Debug.Log(
            "[ConcurrentDeliveryTest] 테스트 데이터 준비 완료\n" +
            $"OwnedCouriers: " +
            $"{string.Join(", ", saveData.OwnedCourierIDs)}\n" +
            $"UnlockedRoutes: " +
            $"{string.Join(", ", saveData.UnlockedRouteIDs)}\n" +
            $"LetterProgressCount: " +
            $"{saveData.LetterProgressesList.Count}\n" +
            $"ActiveDeliveryCount: " +
            $"{saveData.ActiveDeliveryList.Count}\n" +
            $"Unlock Test Courier Owned: " +
            $"{playerDataManager.IsCourierOwned(unlockTestCourierID)}\n" +
            $"Unlock Test Route Unlocked: " +
            $"{playerDataManager.IsRouteUnlocked(unlockTestRouteID)}");

        return true;
    }

    private bool ValidateStaticData()
    {
        if (firstLetterID <= 0 ||
            secondLetterID <= 0 ||
            firstCourierID <= 0 ||
            secondCourierID <= 0 ||
            firstRouteID <= 0 ||
            secondRouteID <= 0 ||
            unlockTestCourierID <= 0 ||
            unlockTestRouteID <= 0)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 모든 테스트 ID는 1 이상이어야 합니다.");
            return false;
        }

        if (firstLetterID == secondLetterID)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 서로 다른 LetterID를 사용해야 합니다.");
            return false;
        }

        if (firstCourierID == secondCourierID)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 동시 배달 테스트에는 " +
                "서로 다른 CourierID가 필요합니다.");
            return false;
        }

        if (unlockTestCourierID == firstCourierID ||
            unlockTestCourierID == secondCourierID)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 해금 테스트 배달부는 " +
                "배달에 사용하는 배달부와 달라야 합니다.");
            return false;
        }

        if (unlockTestRouteID == firstRouteID ||
            unlockTestRouteID == secondRouteID)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 해금 테스트 노선은 " +
                "배달에 사용하는 노선과 달라야 합니다.");
            return false;
        }

        LetterStaticData firstLetter =
            staticDataCatalog.GetLetter(firstLetterID);

        LetterStaticData secondLetter =
            staticDataCatalog.GetLetter(secondLetterID);

        CourierStaticData firstCourier =
            staticDataCatalog.GetCourier(firstCourierID);

        CourierStaticData secondCourier =
            staticDataCatalog.GetCourier(secondCourierID);

        RouteStaticData firstRoute =
            staticDataCatalog.GetRoute(firstRouteID);

        RouteStaticData secondRoute =
            staticDataCatalog.GetRoute(secondRouteID);

        CourierStaticData unlockTestCourier =
            staticDataCatalog.GetCourier(unlockTestCourierID);

        RouteStaticData unlockTestRoute =
            staticDataCatalog.GetRoute(unlockTestRouteID);

        ReplyStaticData firstReply =
            staticDataCatalog.GetReplyByLetterID(firstLetterID);

        ReplyStaticData secondReply =
            staticDataCatalog.GetReplyByLetterID(secondLetterID);

        if (firstLetter == null ||
            secondLetter == null ||
            firstCourier == null ||
            secondCourier == null ||
            firstRoute == null ||
            secondRoute == null ||
            unlockTestCourier == null ||
            unlockTestRoute == null ||
            firstReply == null ||
            secondReply == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 필요한 정적 데이터 또는 " +
                "연결된 답장이 없습니다.");
            return false;
        }

        if (firstLetter.DestinationRegion !=
            firstRoute.RegionType)
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest] 첫 번째 편지와 노선 지역 불일치\n" +
                $"Letter Region: {firstLetter.DestinationRegion}\n" +
                $"Route Region: {firstRoute.RegionType}");
            return false;
        }

        if (secondLetter.DestinationRegion !=
            secondRoute.RegionType)
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest] 두 번째 편지와 노선 지역 불일치\n" +
                $"Letter Region: {secondLetter.DestinationRegion}\n" +
                $"Route Region: {secondRoute.RegionType}");
            return false;
        }

        if (unlockTestCourier.UnlockCondition == null ||
            unlockTestRoute.UnlockCondition == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 해금 테스트 대상의 " +
                "UnlockCondition이 없습니다.");
            return false;
        }

        if (unlockTestCourier.UnlockCondition.IsUnlockedByDefault ||
            unlockTestRoute.UnlockCondition.IsUnlockedByDefault)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 해금 테스트 대상은 " +
                "IsUnlockedByDefault가 false여야 합니다.");
            return false;
        }

        if (unlockTestCourier.UnlockCondition.RequiredCompletedDeliveryCount != 2 ||
            unlockTestRoute.UnlockCondition.RequiredCompletedDeliveryCount != 2)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 해금 테스트 대상의 " +
                "RequiredCompletedDeliveryCount는 2여야 합니다.");
            return false;
        }

        if (firstLetter.LetterReward <= 0 ||
            secondLetter.LetterReward <= 0)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 두 편지의 보상은 " +
                "1 이상이어야 합니다.");
            return false;
        }

        if (firstReply.ReplyID == secondReply.ReplyID)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 두 편지에 서로 다른 ReplyID가 " +
                "연결되어 있어야 합니다.");
            return false;
        }

        return true;
    }

    private bool PrepareLetter(int letterID)
    {
        bool receiveResult =
            letterService.ReceiveLetter(letterID);

        if (!receiveResult)
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest] 편지 수신 실패: {letterID}");
            return false;
        }

        LetterStaticData openedLetter =
            letterService.OpenLetter(letterID);

        if (openedLetter == null)
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest] 편지 열기 실패: {letterID}");
            return false;
        }

        bool sortingResult =
            letterService.CompleteSorting(letterID);

        if (!sortingResult)
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest] 편지 분류 실패: {letterID}");
            return false;
        }

        return IsLetterState(
            letterID,
            ELetterProgressState.Waiting);
    }

    private bool IsLetterState(
        int letterID,
        ELetterProgressState expectedState)
    {
        LetterProgressData progress =
            playerDataManager.GetLetterProgress(letterID);

        return progress != null &&
               progress.State == expectedState;
    }

    private ActiveDeliveryData FindActiveDelivery(int letterID)
    {
        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        if (activeDeliveries == null)
        {
            return null;
        }

        foreach (ActiveDeliveryData deliveryData in activeDeliveries)
        {
            if (deliveryData == null)
            {
                continue;
            }

            if (deliveryData.LetterID == letterID)
            {
                return deliveryData;
            }
        }

        return null;
    }

    private IEnumerator WaitUntilUnixTime(
        long targetUnixTime,
        string description)
    {
        waitTimedOut = false;
        float elapsedTime = 0f;

        Debug.Log(
            $"[ConcurrentDeliveryTest] {description}\n" +
            $"Target Unix Time: {targetUnixTime}");

        while (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
               < targetUnixTime)
        {
            if (elapsedTime >= timeoutSeconds)
            {
                Debug.LogError(
                    $"[ConcurrentDeliveryTest] {description} 시간 초과\n" +
                    $"테스트용 Route 시간이나 Courier 속도를 확인하세요.");

                waitTimedOut = true;
                yield break;
            }

            yield return new WaitForSecondsRealtime(
                completionCheckInterval);

            elapsedTime += completionCheckInterval;
        }
    }

    private void ResetEventTracking()
    {
        currencyChangedEventCount = 0;
        letterReceivedEventCount = 0;
        letterReadEventCount = 0;
        letterStateChangedEventCount = 0;
        waitingStateChangedEventCount = 0;
        deliveringStateChangedEventCount = 0;
        completedStateChangedEventCount = 0;
        deliveryStartedEventCount = 0;
        deliveryCompletedEventCount = 0;
        deliveryResultCheckedEventCount = 0;
        replyReceivedEventCount = 0;
        replyReadEventCount = 0;
        unreadReplyCountChangedEventCount = 0;
        courierUnlockedEventCount = 0;
        routeUnlockedEventCount = 0;
        targetCourierUnlockedEventCount = 0;
        targetRouteUnlockedEventCount = 0;

        lastCurrency = 0;
        lastUnreadReplyCount = -1;
        targetCourierUnlockedAtCompletedCount = -1;
        targetRouteUnlockedAtCompletedCount = -1;

        receivedLetterEventIDs.Clear();
        readLetterEventIDs.Clear();
        startedDeliveryEventIDs.Clear();
        completedDeliveryEventIDs.Clear();
        checkedResultEventIDs.Clear();
        receivedReplyEventIDs.Clear();
        readReplyEventIDs.Clear();
        unlockedCourierEventIDs.Clear();
        unlockedRouteEventIDs.Clear();
    }

    private void OnCurrencyChanged(int currentCurrency)
    {
        if (!isTestRunning) return;

        currencyChangedEventCount++;
        lastCurrency = currentCurrency;
    }

    private void OnLetterReceived(int letterID)
    {
        if (!isTestRunning) return;

        letterReceivedEventCount++;
        receivedLetterEventIDs.Add(letterID);
    }

    private void OnLetterRead(int letterID)
    {
        if (!isTestRunning) return;

        letterReadEventCount++;
        readLetterEventIDs.Add(letterID);
    }

    private void OnLetterStateChanged(
        int letterID,
        ELetterProgressState state)
    {
        if (!isTestRunning) return;

        letterStateChangedEventCount++;

        switch (state)
        {
            case ELetterProgressState.Waiting:
                waitingStateChangedEventCount++;
                break;

            case ELetterProgressState.Delivering:
                deliveringStateChangedEventCount++;
                break;

            case ELetterProgressState.Completed:
                completedStateChangedEventCount++;
                break;
        }
    }

    private void OnDeliveryStarted(
        int letterID,
        int courierID,
        int routeID)
    {
        if (!isTestRunning) return;

        deliveryStartedEventCount++;
        startedDeliveryEventIDs.Add(letterID);
    }

    private void OnDeliveryCompleted(int letterID)
    {
        if (!isTestRunning) return;

        deliveryCompletedEventCount++;
        completedDeliveryEventIDs.Add(letterID);
    }

    private void OnDeliveryResultChecked(int letterID)
    {
        if (!isTestRunning) return;

        deliveryResultCheckedEventCount++;
        checkedResultEventIDs.Add(letterID);
    }

    private void OnReplyReceived(int replyID)
    {
        if (!isTestRunning) return;

        replyReceivedEventCount++;
        receivedReplyEventIDs.Add(replyID);
    }

    private void OnReplyRead(int replyID)
    {
        if (!isTestRunning) return;

        replyReadEventCount++;
        readReplyEventIDs.Add(replyID);
    }

    private void OnUnreadReplyCountChanged(int unreadCount)
    {
        if (!isTestRunning) return;

        unreadReplyCountChangedEventCount++;
        lastUnreadReplyCount = unreadCount;
    }

    private void OnCourierUnlocked(int courierID)
    {
        if (!isTestRunning) return;

        courierUnlockedEventCount++;
        unlockedCourierEventIDs.Add(courierID);

        if (courierID == unlockTestCourierID)
        {
            targetCourierUnlockedEventCount++;
            targetCourierUnlockedAtCompletedCount =
                playerDataManager.GetCompletedDeliveryCount();
        }
    }

    private void OnRouteUnlocked(int routeID)
    {
        if (!isTestRunning) return;

        routeUnlockedEventCount++;
        unlockedRouteEventIDs.Add(routeID);

        if (routeID == unlockTestRouteID)
        {
            targetRouteUnlockedEventCount++;
            targetRouteUnlockedAtCompletedCount =
                playerDataManager.GetCompletedDeliveryCount();
        }
    }

    private void PrintFinalState(
        PlayerSaveData saveData,
        DeliveryResultData firstResult,
        DeliveryResultData secondResult)
    {
        LetterProgressData firstProgress =
            playerDataManager.GetLetterProgress(firstLetterID);

        LetterProgressData secondProgress =
            playerDataManager.GetLetterProgress(secondLetterID);

        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        Debug.Log(
            "[ConcurrentDeliveryTest] 최종 상태\n" +
            $"First Letter State: {firstProgress?.State}\n" +
            $"Second Letter State: {secondProgress?.State}\n" +
            $"First Courier Delivering: " +
            $"{playerDataManager.IsCourierDelivering(firstCourierID)}\n" +
            $"Second Courier Delivering: " +
            $"{playerDataManager.IsCourierDelivering(secondCourierID)}\n" +
            $"ActiveDeliveryCount: " +
            $"{activeDeliveries?.Count ?? 0}\n" +
            $"First Result Checked: {firstResult.IsChecked}\n" +
            $"Second Result Checked: {secondResult.IsChecked}\n" +
            $"First Reward: {firstResult.RewardAmount}\n" +
            $"Second Reward: {secondResult.RewardAmount}\n" +
            $"Currency: {saveData.Currency}\n" +
            $"CompletedDeliveryCount: " +
            $"{saveData.CompleteDeliveryCount}\n" +
            $"ReceivedReplyCount: " +
            $"{saveData.ReceivedReplyIDs.Count}\n" +
            $"ReadReplyCount: " +
            $"{saveData.ReadReplyIds.Count}\n" +
            $"UnreadReplyCount: " +
            $"{playerDataManager.GetUnreadReplyIDs().Count}\n\n" +
            "[ConcurrentDeliveryTest] 이벤트 최종 횟수\n" +
            $"CurrencyChanged: {currencyChangedEventCount}\n" +
            $"LetterReceived: {letterReceivedEventCount}\n" +
            $"LetterRead: {letterReadEventCount}\n" +
            $"LetterStateChanged: {letterStateChangedEventCount}\n" +
            $"DeliveryStarted: {deliveryStartedEventCount}\n" +
            $"DeliveryCompleted: {deliveryCompletedEventCount}\n" +
            $"DeliveryResultChecked: {deliveryResultCheckedEventCount}\n" +
            $"ReplyReceived: {replyReceivedEventCount}\n" +
            $"ReplyRead: {replyReadEventCount}\n" +
            $"UnreadReplyCountChanged: " +
            $"{unreadReplyCountChangedEventCount}\n" +
            $"CourierUnlocked: {courierUnlockedEventCount}\n" +
            $"RouteUnlocked: {routeUnlockedEventCount}\n" +
            $"Target Courier Unlocked: " +
            $"{playerDataManager.IsCourierOwned(unlockTestCourierID)}\n" +
            $"Target Route Unlocked: " +
            $"{playerDataManager.IsRouteUnlocked(unlockTestRouteID)}\n" +
            $"Target Courier Event Count: " +
            $"{targetCourierUnlockedEventCount}\n" +
            $"Target Route Event Count: " +
            $"{targetRouteUnlockedEventCount}\n" +
            $"Target Courier Unlocked At Count: " +
            $"{targetCourierUnlockedAtCompletedCount}\n" +
            $"Target Route Unlocked At Count: " +
            $"{targetRouteUnlockedAtCompletedCount}");
    }

    private void AddUniqueID(
        List<int> idList,
        int id)
    {
        if (!idList.Contains(id))
        {
            idList.Add(id);
        }
    }

    private void LogResult(
        string testName,
        bool success)
    {
        if (success)
        {
            Debug.Log(
                $"[ConcurrentDeliveryTest][PASS] {testName}");
        }
        else
        {
            Debug.LogError(
                $"[ConcurrentDeliveryTest][FAIL] {testName}");
        }
    }

    private bool EnsureReady()
    {
        if (gameBootstrap == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] GameBootstrap이 " +
                "연결되지 않았습니다.");
            return false;
        }

        if (gameBootstrap.RuntimeSaveData == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] RuntimeSaveData가 " +
                "초기화되지 않았습니다.");
            return false;
        }

        if (letterService == null ||
            deliveryService == null ||
            replyService == null ||
            progressionService == null ||
            playerDataManager == null ||
            staticDataCatalog == null)
        {
            Debug.LogError(
                "[ConcurrentDeliveryTest] 필요한 서비스가 " +
                "연결되지 않았습니다.");
            return false;
        }

        return true;
    }
}
