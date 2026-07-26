using System.Collections.Generic;
using UnityEngine;

public class LetterServiceTest : MonoBehaviour
{
    [Header("Test Target")]
    [SerializeField] private LetterService letterService;

    [Header("StaticDataCatalog에 실제로 존재하는 편지 ID")]
    [SerializeField, Min(1)] private int normalTestLetterID = 1;
    [SerializeField, Min(1)] private int unreadTestLetterID = 2;

    [Header("StaticDataCatalog에 존재하지 않는 테스트 ID")]
    [SerializeField] private int invalidLetterID = -999;

    [ContextMenu("Run LetterService Test")]
    private void RunLetterServiceTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[LetterServiceTest] Play Mode에서 테스트를 실행해 주세요.");
            return;
        }

        if (!ValidateTestSettings())
        {
            return;
        }

        Debug.Log(
            "========== LetterService 테스트 시작 ==========");

        ResetProgressData();

        int passedTestCount = 0;
        const int totalTestCount = 6;

        if (!TestReceiveLetter()) return;
        passedTestCount++;

        if (!TestMarkAsRead()) return;
        passedTestCount++;

        if (!TestCompleteSorting()) return;
        passedTestCount++;

        if (!TestUnreadSorting()) return;
        passedTestCount++;

        if (!TestAvailableLetters()) return;
        passedTestCount++;

        if (!TestInvalidLetterID()) return;
        passedTestCount++;

        Debug.Log(
            $"========== LetterService 전체 테스트 통과 " +
            $"({passedTestCount}/{totalTestCount}) ==========");
    }

    private bool ValidateTestSettings()
    {
        if (letterService == null)
        {
            Debug.LogError(
                "[LetterServiceTest] LetterService가 설정되지 않았습니다.");
            return false;
        }

        if (normalTestLetterID == unreadTestLetterID)
        {
            Debug.LogError(
                "[LetterServiceTest] 두 테스트 편지 ID는 서로 달라야 합니다.");
            return false;
        }

        if (invalidLetterID == normalTestLetterID ||
            invalidLetterID == unreadTestLetterID)
        {
            Debug.LogError(
                "[LetterServiceTest] invalidLetterID는 정상 테스트 ID와 달라야 합니다.");
            return false;
        }

        return true;
    }

    private void ResetProgressData()
    {
        letterService.InitializeProgressData(
            new List<LetterProgressData>());

        Debug.Log("[PASS] 진행 데이터 초기화");
    }

    private bool TestReceiveLetter()
    {
        bool receiveResult =
            letterService.ReceiveLetter(normalTestLetterID);

        if (!Check(
                receiveResult,
                "정상 편지 수신에 실패했습니다."))
        {
            return false;
        }

        LetterProgressData progress =
            letterService.GetLetterProgress(normalTestLetterID);

        if (!Check(
                progress != null,
                "수신한 편지의 진행 데이터가 없습니다."))
        {
            return false;
        }

        if (!Check(
                progress.State == ELetterProgressState.New,
                "신규 편지의 초기 상태가 New가 아닙니다."))
        {
            return false;
        }

        if (!Check(
                !progress.IsRead,
                "신규 편지의 IsRead가 false가 아닙니다."))
        {
            return false;
        }

        bool duplicateResult =
            letterService.ReceiveLetter(normalTestLetterID);

        if (!Check(
                !duplicateResult,
                "같은 편지가 중복 수신되었습니다."))
        {
            return false;
        }

        Debug.Log("[PASS] 편지 수신 및 중복 검사");
        return true;
    }

    private bool TestMarkAsRead()
    {
        bool readResult =
            letterService.MarkAsRead(normalTestLetterID);

        if (!Check(
                readResult,
                "편지 읽음 처리에 실패했습니다."))
        {
            return false;
        }

        LetterProgressData progress =
            letterService.GetLetterProgress(normalTestLetterID);

        if (!Check(
                progress != null,
                "읽음 처리 후 진행 데이터를 찾을 수 없습니다."))
        {
            return false;
        }

        if (!Check(
                progress.IsRead,
                "읽음 처리 후 IsRead가 true가 아닙니다."))
        {
            return false;
        }

        if (!Check(
                progress.State == ELetterProgressState.New,
                "읽음 처리만 했는데 편지 상태가 New에서 변경되었습니다."))
        {
            return false;
        }

        bool duplicateReadResult =
            letterService.MarkAsRead(normalTestLetterID);

        if (!Check(
                !duplicateReadResult,
                "이미 읽은 편지가 다시 읽음 처리되었습니다."))
        {
            return false;
        }

        Debug.Log("[PASS] 편지 읽음 처리");
        return true;
    }

    private bool TestCompleteSorting()
    {
        bool sortingResult =
            letterService.CompleteSorting(normalTestLetterID);

        if (!Check(
                sortingResult,
                "읽은 편지의 분류 완료에 실패했습니다."))
        {
            return false;
        }

        LetterProgressData progress =
            letterService.GetLetterProgress(normalTestLetterID);

        if (!Check(
                progress != null,
                "분류 완료 후 진행 데이터를 찾을 수 없습니다."))
        {
            return false;
        }

        if (!Check(
                progress.State == ELetterProgressState.Waiting,
                "분류 완료 후 상태가 Waiting이 아닙니다."))
        {
            return false;
        }

        bool duplicateSortingResult =
            letterService.CompleteSorting(normalTestLetterID);

        if (!Check(
                !duplicateSortingResult,
                "Waiting 상태의 편지가 다시 분류되었습니다."))
        {
            return false;
        }

        Debug.Log("[PASS] 편지 분류 완료");
        return true;
    }

    private bool TestUnreadSorting()
    {
        bool receiveResult =
            letterService.ReceiveLetter(unreadTestLetterID);

        if (!Check(
                receiveResult,
                "읽지 않은 편지 테스트용 수신에 실패했습니다."))
        {
            return false;
        }

        bool sortingResult =
            letterService.CompleteSorting(unreadTestLetterID);

        if (!Check(
                !sortingResult,
                "읽지 않은 편지가 분류 완료되었습니다."))
        {
            return false;
        }

        LetterProgressData progress =
            letterService.GetLetterProgress(unreadTestLetterID);

        if (!Check(
                progress != null,
                "읽지 않은 편지의 진행 데이터를 찾을 수 없습니다."))
        {
            return false;
        }

        if (!Check(
                progress.State == ELetterProgressState.New,
                "분류 실패 후 상태가 New로 유지되지 않았습니다."))
        {
            return false;
        }

        if (!Check(
                !progress.IsRead,
                "분류 실패 후 읽지 않은 편지의 IsRead가 변경되었습니다."))
        {
            return false;
        }

        Debug.Log("[PASS] 읽지 않은 편지 분류 거절");
        return true;
    }

    private bool TestAvailableLetters()
    {
        IReadOnlyList<LetterStaticData> availableLetters =
            letterService.GetAvailableLetters();

        if (!Check(
                availableLetters != null,
                "Available 편지 목록이 null입니다."))
        {
            return false;
        }

        bool containsWaitingLetter =
            ContainsLetter(availableLetters, normalTestLetterID);

        bool containsNewLetter =
            ContainsLetter(availableLetters, unreadTestLetterID);

        if (!Check(
                containsWaitingLetter,
                "Waiting 편지가 Available 목록에 없습니다."))
        {
            return false;
        }

        if (!Check(
                containsNewLetter,
                "New 편지가 Available 목록에 없습니다."))
        {
            return false;
        }

        Debug.Log(
            $"[PASS] Available 편지 조회: {availableLetters.Count}개");

        return true;
    }

    private bool TestInvalidLetterID()
    {
        bool receiveResult =
            letterService.ReceiveLetter(invalidLetterID);

        bool readResult =
            letterService.MarkAsRead(invalidLetterID);

        bool sortingResult =
            letterService.CompleteSorting(invalidLetterID);

        LetterProgressData progress =
            letterService.GetLetterProgress(invalidLetterID);

        if (!Check(
                !receiveResult,
                "존재하지 않는 편지가 수신되었습니다."))
        {
            return false;
        }

        if (!Check(
                !readResult,
                "존재하지 않는 편지가 읽음 처리되었습니다."))
        {
            return false;
        }

        if (!Check(
                !sortingResult,
                "존재하지 않는 편지가 분류되었습니다."))
        {
            return false;
        }

        if (!Check(
                progress == null,
                "존재하지 않는 ID의 진행 데이터가 반환되었습니다."))
        {
            return false;
        }

        Debug.Log("[PASS] 존재하지 않는 ID 처리");
        return true;
    }

    private static bool ContainsLetter(
        IReadOnlyList<LetterStaticData> letters,
        int letterID)
    {
        if (letters == null)
        {
            return false;
        }

        foreach (LetterStaticData letter in letters)
        {
            if (letter != null &&
                letter.LetterID == letterID)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Check(
        bool condition,
        string failureMessage)
    {
        if (condition)
        {
            return true;
        }

        Debug.LogError($"[FAIL] {failureMessage}");
        return false;
    }
}
