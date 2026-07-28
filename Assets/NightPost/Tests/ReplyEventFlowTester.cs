using System.Collections.Generic;
using UnityEngine;

public class ReplyEventFlowTester : MonoBehaviour
{
    [Header("초기화")]
    [SerializeField] private GameBootstrap gameBootstrap;

    [Header("서비스")]
    [SerializeField] private ReplyService replyService;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private StaticDataCatalog staticDataCatalog;

    [Header("테스트 데이터")]
    [SerializeField] private int replyID = 5001;

    private bool isTestRunning;

    private int replyReceivedEventCount;
    private int replyReadEventCount;
    private int unreadReplyCountChangedEventCount;

    private int lastReceivedReplyID;
    private int lastReadReplyID;
    private int lastUnreadReplyCount = -1;

    private int passCount;
    private int failCount;

    private readonly List<string> eventOrder = new();


    private void OnEnable()
    {
        GameEvents.ReplyReceived += OnReplyReceived;
        GameEvents.ReplyRead += OnReplyRead;
        GameEvents.UnreadReplyCountChanged +=
            OnUnreadReplyCountChanged;
    }

    private void OnDisable()
    {
        GameEvents.ReplyReceived -= OnReplyReceived;
        GameEvents.ReplyRead -= OnReplyRead;
        GameEvents.UnreadReplyCountChanged -=
            OnUnreadReplyCountChanged;
    }


    [ContextMenu("Run Reply Event Test")]
    private void RunReplyEventTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[ReplyEventTest] 플레이 모드에서 실행해야 합니다.");
            return;
        }

        if (isTestRunning)
        {
            Debug.LogWarning(
                "[ReplyEventTest] 이미 테스트가 진행 중입니다.");
            return;
        }

        ResetTestResult();

        if (!EnsureReady())
        {
            return;
        }

        PlayerSaveData saveData =
            gameBootstrap.RuntimeSaveData;

        if (!PrepareCleanTestState(saveData))
        {
            return;
        }

        isTestRunning = true;

        Debug.Log(
            "========== Reply Event Test 시작 ==========");

        // --------------------------------------------------
        // 1. 초기 상태 확인
        // --------------------------------------------------

        LogResult(
            "1. 테스트 시작 시 답장 미수신 상태",
            !playerDataManager.IsReplyReceived(replyID));

        LogResult(
            "2. 테스트 시작 시 답장 미열람 상태",
            !playerDataManager.IsReplyRead(replyID));

        LogResult(
            "3. 테스트 시작 시 미열람 답장 수 0",
            playerDataManager.GetUnreadReplyIDs().Count == 0);


        // --------------------------------------------------
        // 2. 답장 수신
        // --------------------------------------------------

        bool receiveResult =
            playerDataManager.AddReceivedReply(replyID);

        LogResult(
            "4. 답장 수신 성공",
            receiveResult);

        LogResult(
            "5. 답장 수신 상태 저장",
            playerDataManager.IsReplyReceived(replyID));

        LogResult(
            "6. 수신 직후 아직 미열람 상태",
            !playerDataManager.IsReplyRead(replyID));

        LogResult(
            "7. 수신 후 미열람 답장 수 1",
            playerDataManager.GetUnreadReplyIDs().Count == 1);

        LogResult(
            "8. ReplyReceived 이벤트 1회",
            replyReceivedEventCount == 1);

        LogResult(
            "9. ReplyReceived 이벤트 ID 일치",
            lastReceivedReplyID == replyID);

        LogResult(
            "10. 미열람 개수 이벤트 1회",
            unreadReplyCountChangedEventCount == 1);

        LogResult(
            "11. 미열람 개수 이벤트 값 1",
            lastUnreadReplyCount == 1);


        // --------------------------------------------------
        // 3. 같은 답장 중복 수신 차단
        // --------------------------------------------------

        bool duplicateReceiveResult =
            playerDataManager.AddReceivedReply(replyID);

        LogResult(
            "12. 같은 답장 중복 수신 차단",
            !duplicateReceiveResult);

        LogResult(
            "13. 중복 수신 시 ReplyReceived 추가 발생 없음",
            replyReceivedEventCount == 1);

        LogResult(
            "14. 중복 수신 시 미열람 이벤트 추가 발생 없음",
            unreadReplyCountChangedEventCount == 1);


        // --------------------------------------------------
        // 4. 답장 최초 열람
        // --------------------------------------------------

        ReplyStaticData firstOpenedReply =
            replyService.OpenReply(replyID);

        LogResult(
            "15. 답장 최초 열람 성공",
            firstOpenedReply != null);

        LogResult(
            "16. 반환된 답장 ID 일치",
            firstOpenedReply != null &&
            firstOpenedReply.ReplyID == replyID);

        LogResult(
            "17. 답장 읽음 상태 저장",
            playerDataManager.IsReplyRead(replyID));

        LogResult(
            "18. 열람 후 미열람 답장 수 0",
            playerDataManager.GetUnreadReplyIDs().Count == 0);

        LogResult(
            "19. ReplyRead 이벤트 1회",
            replyReadEventCount == 1);

        LogResult(
            "20. ReplyRead 이벤트 ID 일치",
            lastReadReplyID == replyID);

        LogResult(
            "21. 미열람 개수 이벤트 총 2회",
            unreadReplyCountChangedEventCount == 2);

        LogResult(
            "22. 열람 후 미열람 개수 이벤트 값 0",
            lastUnreadReplyCount == 0);


        // --------------------------------------------------
        // 5. 같은 답장 다시 열기
        // --------------------------------------------------

        ReplyStaticData secondOpenedReply =
            replyService.OpenReply(replyID);

        LogResult(
            "23. 이미 읽은 답장도 다시 열기 가능",
            secondOpenedReply != null);

        LogResult(
            "24. 재열람한 답장 ID 일치",
            secondOpenedReply != null &&
            secondOpenedReply.ReplyID == replyID);

        LogResult(
            "25. 재열람 시 ReplyRead 추가 발생 없음",
            replyReadEventCount == 1);

        LogResult(
            "26. 재열람 시 미열람 이벤트 추가 발생 없음",
            unreadReplyCountChangedEventCount == 2);


        // --------------------------------------------------
        // 6. 이벤트 순서 확인
        // --------------------------------------------------

        bool correctEventOrder =
            eventOrder.Count == 4 &&
            eventOrder[0] ==
            $"ReplyReceived:{replyID}" &&
            eventOrder[1] ==
            "UnreadReplyCountChanged:1" &&
            eventOrder[2] ==
            $"ReplyRead:{replyID}" &&
            eventOrder[3] ==
            "UnreadReplyCountChanged:0";

        LogResult(
            "27. 답장 이벤트 발생 순서 정상",
            correctEventOrder);


        isTestRunning = false;

        PrintFinalResult();

        Debug.Log(
            "========== Reply Event Test 종료 ==========");
    }


    private bool PrepareCleanTestState(
        PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError(
                "[ReplyEventTest] RuntimeSaveData가 없습니다.");
            return false;
        }

        if (saveData.ReceivedReplyIDs == null ||
            saveData.ReadReplyIds == null)
        {
            Debug.LogError(
                "[ReplyEventTest] 답장 관련 저장 목록이 " +
                "초기화되지 않았습니다.");
            return false;
        }

        saveData.ReceivedReplyIDs.Clear();
        saveData.ReadReplyIds.Clear();

        return true;
    }


    private bool EnsureReady()
    {
        if (gameBootstrap == null)
        {
            Debug.LogError(
                "[ReplyEventTest] GameBootstrap이 연결되지 않았습니다.");
            return false;
        }

        if (gameBootstrap.RuntimeSaveData == null)
        {
            Debug.LogError(
                "[ReplyEventTest] GameBootstrap의 RuntimeSaveData가 " +
                "초기화되지 않았습니다.");
            return false;
        }

        if (replyService == null ||
            playerDataManager == null ||
            staticDataCatalog == null)
        {
            Debug.LogError(
                "[ReplyEventTest] 필요한 서비스가 연결되지 않았습니다.");
            return false;
        }

        if (replyID <= 0)
        {
            Debug.LogError(
                "[ReplyEventTest] ReplyID는 1 이상이어야 합니다.");
            return false;
        }

        ReplyStaticData replyData =
            staticDataCatalog.GetReply(replyID);

        if (replyData == null)
        {
            Debug.LogError(
                $"[ReplyEventTest] ReplyID={replyID}에 해당하는 " +
                "정적 답장 데이터가 없습니다.");
            return false;
        }

        return true;
    }


    private void ResetTestResult()
    {
        isTestRunning = false;

        replyReceivedEventCount = 0;
        replyReadEventCount = 0;
        unreadReplyCountChangedEventCount = 0;

        lastReceivedReplyID = 0;
        lastReadReplyID = 0;
        lastUnreadReplyCount = -1;

        passCount = 0;
        failCount = 0;

        eventOrder.Clear();
    }


    private void OnReplyReceived(int receivedReplyID)
    {
        if (!isTestRunning) return;

        replyReceivedEventCount++;
        lastReceivedReplyID = receivedReplyID;

        eventOrder.Add(
            $"ReplyReceived:{receivedReplyID}");

        Debug.Log(
            $"[ReplyEventTest][EVENT] ReplyReceived | " +
            $"ReplyID={receivedReplyID} | " +
            $"Count={replyReceivedEventCount}");
    }

    private void OnReplyRead(int readReplyID)
    {
        if (!isTestRunning) return;

        replyReadEventCount++;
        lastReadReplyID = readReplyID;

        eventOrder.Add(
            $"ReplyRead:{readReplyID}");

        Debug.Log(
            $"[ReplyEventTest][EVENT] ReplyRead | " +
            $"ReplyID={readReplyID} | " +
            $"Count={replyReadEventCount}");
    }

    private void OnUnreadReplyCountChanged(int unreadCount)
    {
        if (!isTestRunning) return;

        unreadReplyCountChangedEventCount++;
        lastUnreadReplyCount = unreadCount;

        eventOrder.Add(
            $"UnreadReplyCountChanged:{unreadCount}");

        Debug.Log(
            $"[ReplyEventTest][EVENT] " +
            $"UnreadReplyCountChanged | " +
            $"UnreadCount={unreadCount} | " +
            $"Count={unreadReplyCountChangedEventCount}");
    }


    private void LogResult(
        string testName,
        bool success)
    {
        if (success)
        {
            passCount++;

            Debug.Log(
                $"[ReplyEventTest][PASS] {testName}");
        }
        else
        {
            failCount++;

            Debug.LogError(
                $"[ReplyEventTest][FAIL] {testName}");
        }
    }


    private void PrintFinalResult()
    {
        Debug.Log(
            "[ReplyEventTest] 최종 결과\n" +
            $"PASS: {passCount}\n" +
            $"FAIL: {failCount}\n" +
            $"ReplyReceived Events: " +
            $"{replyReceivedEventCount}\n" +
            $"ReplyRead Events: " +
            $"{replyReadEventCount}\n" +
            $"UnreadReplyCountChanged Events: " +
            $"{unreadReplyCountChangedEventCount}\n" +
            $"Event Order: " +
            $"{string.Join(" → ", eventOrder)}");
    }
}
