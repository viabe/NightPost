using System.Collections.Generic;
using UnityEngine;

// 편지·답장 도감 목록과 각 항목의 표시 상태가 현재 플레이어 데이터와 일치하는지 검증함
public class CollectionServiceTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;

    private int passCount;
    private int failCount;

    /// <summary>
    /// 편지·답장 도감의 항목 수와 각 상태값을 검증함
    /// </summary>
    [ContextMenu("Run Collection Service Test")]
    private void RunCollectionServiceTest()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("========== Collection Service Test 시작 ==========");

        // 플레이 모드가 아니라면 런타임 도감 테스트를 진행하지 않음
        if (!Application.isPlaying)
        {
            LogResult("플레이 모드 확인", false, "플레이 모드에서 실행해야 함");
            PrintResult();
            return;
        }

        // 테스트에 필요한 참조를 확인함
        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        // GameFlowController를 통해 전체 편지 도감 목록을 조회함
        IReadOnlyList<LetterCollectionEntryData> letterEntries =
            gameFlowController.GetLetterCollectionEntries();

        // GameFlowController를 통해 전체 답장 도감 목록을 조회함
        IReadOnlyList<ReplyCollectionEntryData> replyEntries =
            gameFlowController.GetReplyCollectionEntries();

        // 편지 도감 목록이 null이 아닌지 확인함
        LogResult(
            "1. 편지 도감 목록 생성",
            letterEntries != null,
            letterEntries != null
                ? $"항목 수: {letterEntries.Count}"
                : "편지 도감 목록이 null임");

        // 답장 도감 목록이 null이 아닌지 확인함
        LogResult(
            "2. 답장 도감 목록 생성",
            replyEntries != null,
            replyEntries != null
                ? $"항목 수: {replyEntries.Count}"
                : "답장 도감 목록이 null임");

        if (letterEntries == null || replyEntries == null)
        {
            PrintResult();
            return;
        }

        // 카탈로그에 등록된 유효 편지 수를 계산함
        int expectedLetterCount = CountValidLetters();

        // 편지 도감 항목 수가 유효한 전체 편지 수와 같은지 확인함
        LogResult(
            "3. 편지 도감 항목 수 일치",
            letterEntries.Count == expectedLetterCount,
            $"기대: {expectedLetterCount}, 실제: {letterEntries.Count}");

        // 카탈로그에 등록된 유효 답장 수를 계산함
        int expectedReplyCount = CountValidReplies();

        // 답장 도감 항목 수가 유효한 전체 답장 수와 같은지 확인함
        LogResult(
            "4. 답장 도감 항목 수 일치",
            replyEntries.Count == expectedReplyCount,
            $"기대: {expectedReplyCount}, 실제: {replyEntries.Count}");

        // 전체 편지 도감 항목의 상태를 검증함
        ValidateLetterEntries(letterEntries);

        // 전체 답장 도감 항목의 상태를 검증함
        ValidateReplyEntries(replyEntries);

        PrintResult();
    }

    /// <summary>
    /// 테스트에 필요한 참조가 모두 연결되어 있는지 확인함
    /// </summary>
    private bool ValidateReferences()
    {
        // 필수 참조 연결 여부를 확인함
        bool hasReferences =
            gameFlowController != null &&
            staticDataCatalog != null &&
            playerDataManager != null;

        // 참조 검사 결과를 출력함
        LogResult(
            "필수 참조 연결",
            hasReferences,
            hasReferences
                ? "모든 참조가 연결됨"
                : "Inspector의 필수 참조를 확인해야 함");

        // 참조 연결 결과를 반환함
        return hasReferences;
    }

    /// <summary>
    /// 카탈로그에 등록된 유효한 편지 정적 데이터 수를 반환함
    /// </summary>
    private int CountValidLetters()
    {
        // 전체 편지 정적 데이터를 조회함
        IReadOnlyList<LetterStaticData> letters =
            staticDataCatalog.Letters();

        // 편지 목록이 없다면 0을 반환함
        if (letters == null) return 0;

        // 유효한 편지 수를 저장함
        int validCount = 0;

        // 전체 편지 정적 데이터를 순회함
        foreach (LetterStaticData letterStaticData in letters)
        {
            // null이거나 ID가 유효하지 않은 편지는 제외함
            if (letterStaticData == null ||
                letterStaticData.LetterID <= 0)
            {
                continue;
            }

            // 유효한 편지 수를 증가시킴
            validCount++;
        }

        // 계산한 유효 편지 수를 반환함
        return validCount;
    }

    /// <summary>
    /// 카탈로그에 등록된 유효한 답장 정적 데이터 수를 반환함
    /// </summary>
    private int CountValidReplies()
    {
        // 전체 답장 정적 데이터를 조회함
        IReadOnlyList<ReplyStaticData> replies =
            staticDataCatalog.Replies();

        // 답장 목록이 없다면 0을 반환함
        if (replies == null) return 0;

        // 유효한 답장 수를 저장함
        int validCount = 0;

        // 전체 답장 정적 데이터를 순회함
        foreach (ReplyStaticData replyStaticData in replies)
        {
            // null이거나 ID가 유효하지 않은 답장은 제외함
            if (replyStaticData == null ||
                replyStaticData.ReplyID <= 0)
            {
                continue;
            }

            // 유효한 답장 수를 증가시킴
            validCount++;
        }

        // 계산한 유효 답장 수를 반환함
        return validCount;
    }

    /// <summary>
    /// 전체 편지 도감 항목의 ID와 수신·읽음·완료·콘텐츠 표시 상태를 검증함
    /// </summary>
    private void ValidateLetterEntries(
        IReadOnlyList<LetterCollectionEntryData> letterEntries)
    {
        // 개별 항목 검증 실패 여부를 저장함
        bool allEntriesValid = true;

        // 전체 편지 도감 항목을 순회함
        foreach (LetterCollectionEntryData entryData in letterEntries)
        {
            // null 항목은 잘못된 도감 데이터로 처리함
            if (entryData == null)
            {
                allEntriesValid = false;
                Debug.LogError(
                    "[CollectionServiceTest] null 편지 도감 항목이 존재함");
                continue;
            }

            // 항목 ID에 해당하는 편지 정적 데이터를 조회함
            LetterStaticData letterStaticData =
                staticDataCatalog.GetLetter(entryData.LetterID);

            // 항목 ID에 해당하는 편지 진행 데이터를 조회함
            LetterProgressData letterProgressData =
                playerDataManager.GetLetterProgress(entryData.LetterID);

            // 현재 데이터 기준의 예상 수신 여부를 계산함
            bool expectedIsReceived =
                letterProgressData != null;

            // 현재 데이터 기준의 예상 읽음 여부를 계산함
            bool expectedIsRead =
                expectedIsReceived &&
                letterProgressData.IsRead;

            // 현재 데이터 기준의 예상 배달 완료 여부를 계산함
            bool expectedIsCompleted =
                expectedIsReceived &&
                letterProgressData.State ==
                ELetterProgressState.Completed;

            // 수신한 편지만 정적 데이터가 노출되어야 함
            LetterStaticData expectedVisibleData =
                expectedIsReceived
                    ? letterStaticData
                    : null;

            // 현재 항목이 모든 예상 상태와 일치하는지 확인함
            bool isEntryValid =
                letterStaticData != null &&
                entryData.IsReceived == expectedIsReceived &&
                entryData.IsRead == expectedIsRead &&
                entryData.IsCompleted == expectedIsCompleted &&
                entryData.LetterData == expectedVisibleData;

            if (!isEntryValid)
            {
                allEntriesValid = false;

                Debug.LogError(
                    "[CollectionServiceTest] 편지 도감 상태 불일치 | " +
                    $"LetterID: {entryData.LetterID}, " +
                    $"IsReceived 기대/실제: {expectedIsReceived}/{entryData.IsReceived}, " +
                    $"IsRead 기대/실제: {expectedIsRead}/{entryData.IsRead}, " +
                    $"IsCompleted 기대/실제: {expectedIsCompleted}/{entryData.IsCompleted}, " +
                    $"LetterData 공개 기대/실제: " +
                    $"{expectedVisibleData != null}/{entryData.LetterData != null}");
            }
        }

        // 전체 편지 항목 검증 결과를 출력함
        LogResult(
            "5. 편지 도감 상태 및 콘텐츠 노출 규칙",
            allEntriesValid,
            allEntriesValid
                ? "모든 편지 항목이 현재 진행 상태와 일치함"
                : "일치하지 않는 편지 항목이 존재함");
    }

    /// <summary>
    /// 전체 답장 도감 항목의 ID와 수신·읽음·콘텐츠 표시 상태를 검증함
    /// </summary>
    private void ValidateReplyEntries(
        IReadOnlyList<ReplyCollectionEntryData> replyEntries)
    {
        // 개별 항목 검증 실패 여부를 저장함
        bool allEntriesValid = true;

        // 전체 답장 도감 항목을 순회함
        foreach (ReplyCollectionEntryData entryData in replyEntries)
        {
            // null 항목은 잘못된 도감 데이터로 처리함
            if (entryData == null)
            {
                allEntriesValid = false;
                Debug.LogError(
                    "[CollectionServiceTest] null 답장 도감 항목이 존재함");
                continue;
            }

            // 항목 ID에 해당하는 답장 정적 데이터를 조회함
            ReplyStaticData replyStaticData =
                staticDataCatalog.GetReply(entryData.ReplyID);

            // 현재 데이터 기준의 예상 수신 여부를 계산함
            bool expectedIsReceived =
                playerDataManager.IsReplyReceived(entryData.ReplyID);

            // 수신한 답장에 대해서만 예상 읽음 여부를 계산함
            bool expectedIsRead =
                expectedIsReceived &&
                playerDataManager.IsReplyRead(entryData.ReplyID);

            // 수신한 답장만 정적 데이터가 노출되어야 함
            ReplyStaticData expectedVisibleData =
                expectedIsReceived
                    ? replyStaticData
                    : null;

            // 현재 항목이 모든 예상 상태와 일치하는지 확인함
            bool isEntryValid =
                replyStaticData != null &&
                entryData.IsReceived == expectedIsReceived &&
                entryData.IsRead == expectedIsRead &&
                entryData.ReplyData == expectedVisibleData;

            if (!isEntryValid)
            {
                allEntriesValid = false;

                Debug.LogError(
                    "[CollectionServiceTest] 답장 도감 상태 불일치 | " +
                    $"ReplyID: {entryData.ReplyID}, " +
                    $"IsReceived 기대/실제: {expectedIsReceived}/{entryData.IsReceived}, " +
                    $"IsRead 기대/실제: {expectedIsRead}/{entryData.IsRead}, " +
                    $"ReplyData 공개 기대/실제: " +
                    $"{expectedVisibleData != null}/{entryData.ReplyData != null}");
            }
        }

        // 전체 답장 항목 검증 결과를 출력함
        LogResult(
            "6. 답장 도감 상태 및 콘텐츠 노출 규칙",
            allEntriesValid,
            allEntriesValid
                ? "모든 답장 항목이 현재 수신 상태와 일치함"
                : "일치하지 않는 답장 항목이 존재함");
    }

    /// <summary>
    /// 개별 테스트 결과를 출력하고 성공·실패 횟수를 기록함
    /// </summary>
    private void LogResult(
        string testName,
        bool success,
        string detail)
    {
        // 테스트 성공 시 성공 횟수를 증가시킴
        if (success)
        {
            passCount++;
            Debug.Log(
                $"[CollectionServiceTest][PASS] {testName} | {detail}");
            return;
        }

        // 테스트 실패 시 실패 횟수를 증가시킴
        failCount++;
        Debug.LogError(
            $"[CollectionServiceTest][FAIL] {testName} | {detail}");
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        // 전체 성공 및 실패 횟수를 출력함
        Debug.Log(
            "========== Collection Service Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
