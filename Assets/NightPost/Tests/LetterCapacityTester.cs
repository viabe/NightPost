using UnityEngine;

// 편지 보유 한도와 수신 차단 흐름을 검증함
public class LetterCapacityTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameBootstrap gameBootstrap;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LetterService letterService;

    private int passCount;
    private int failCount;

    /// <summary>
    /// 현재 편지 보유 한도까지 편지를 수신하고 초과 수신이 차단되는지 검증함
    /// </summary>
    [ContextMenu("Run Letter Capacity Test")]
    private void RunLetterCapacityTest()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("========== Letter Capacity Test 시작 ==========");

        // 플레이 모드가 아니라면 런타임 테스트를 진행하지 않음
        if (!Application.isPlaying)
        {
            LogResult(
                "플레이 모드 확인",
                false,
                "플레이 모드에서 실행해야 함");

            PrintResult();
            return;
        }

        // 테스트에 필요한 참조와 초기화 상태를 확인함
        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        // 시설 효과가 적용된 최종 편지 보유 한도를 조회함
        int maxCapacity =
            letterService.GetMaxLetterCapacity();

        LogResult(
            "1. 최종 편지 보유 한도가 1 이상",
            maxCapacity >= 1,
            $"최종 한도: {maxCapacity}");

        if (maxCapacity < 1)
        {
            PrintResult();
            return;
        }

        // 테스트 시작 시점의 현재 보유 편지 수를 조회함
        int initialCount =
            letterService.GetCurrentLetterCount();

        LogResult(
            "2. 현재 보유 편지 수 조회",
            initialCount >= 0,
            $"현재 수: {initialCount}, 최종 한도: {maxCapacity}");

        // 현재 보유 수가 한도 이상이면 추가 수신 불가 상태만 검증함
        if (initialCount >= maxCapacity)
        {
            LogResult(
                "3. 한도 도달 상태에서 수신 불가",
                !letterService.CanReceiveLetter(),
                $"현재 수: {initialCount}, 최종 한도: {maxCapacity}");

            TestBlockedReceive(initialCount);
            PrintResult();
            return;
        }

        // 수신하지 않은 정적 편지를 사용해 현재 보유 수를 한도까지 채움
        int receivedCount =
            FillLettersToCapacity(maxCapacity);

        int currentCount =
            letterService.GetCurrentLetterCount();

        LogResult(
            "3. 편지를 최종 한도까지 수신",
            currentCount == maxCapacity,
            $"추가 수신: {receivedCount}, 현재 수: {currentCount}, 최종 한도: {maxCapacity}");

        if (currentCount != maxCapacity)
        {
            Debug.LogError(
                "[LetterCapacityTest] 한도까지 채울 수 있는 미수신 정적 편지가 부족할 수 있음.");

            PrintResult();
            return;
        }

        // 최종 한도에 도달하면 수신 가능 여부가 false인지 확인함
        LogResult(
            "4. 한도 도달 후 CanReceiveLetter 차단",
            !letterService.CanReceiveLetter(),
            $"현재 수: {currentCount}, 최종 한도: {maxCapacity}");

        // 한도에 도달한 상태에서 실제 ReceiveLetter 호출도 실패하는지 확인함
        TestBlockedReceive(currentCount);

        PrintResult();
    }

    /// <summary>
    /// 테스트에 필요한 참조와 GameBootstrap 초기화 상태를 확인함
    /// </summary>
    private bool ValidateReferences()
    {
        bool hasReferences =
            gameBootstrap != null &&
            staticDataCatalog != null &&
            playerDataManager != null &&
            letterService != null;

        LogResult(
            "필수 참조 연결",
            hasReferences,
            hasReferences
                ? "모든 참조가 연결됨"
                : "Inspector의 필수 참조를 확인해야 함");

        if (!hasReferences)
        {
            return false;
        }

        LogResult(
            "GameBootstrap 초기화 완료",
            gameBootstrap.IsInitialized,
            $"IsInitialized: {gameBootstrap.IsInitialized}");

        return gameBootstrap.IsInitialized;
    }

    /// <summary>
    /// 수신하지 않은 편지를 찾아 현재 편지 수가 최종 한도에 도달할 때까지 수신함
    /// </summary>
    private int FillLettersToCapacity(int maxCapacity)
    {
        int receivedCount = 0;
        var letters = staticDataCatalog.Letters();

        if (letters == null)
        {
            return receivedCount;
        }

        foreach (LetterStaticData letter in letters)
        {
            if (letter == null)
            {
                continue;
            }

            if (letterService.GetCurrentLetterCount() >= maxCapacity)
            {
                break;
            }

            if (letter.LetterID <= 0)
            {
                continue;
            }

            // 이미 수신한 편지는 중복 수신 대상에서 제외함
            if (playerDataManager.GetLetterProgress(letter.LetterID) != null)
            {
                continue;
            }

            bool receiveResult =
                letterService.ReceiveLetter(letter.LetterID);

            if (receiveResult)
            {
                receivedCount++;
            }
        }

        return receivedCount;
    }

    /// <summary>
    /// 한도에 도달한 상태에서 수신하지 않은 편지의 추가 수신이 차단되는지 검증함
    /// </summary>
    private void TestBlockedReceive(int countBefore)
    {
        int unreceivedLetterID =
            FindUnreceivedLetterID();

        LogResult(
            "5. 한도 초과 검증용 미수신 편지 조회",
            unreceivedLetterID > 0,
            unreceivedLetterID > 0
                ? $"LetterID: {unreceivedLetterID}"
                : "미수신 정적 편지가 없음");

        if (unreceivedLetterID <= 0)
        {
            return;
        }

        bool receiveResult =
            letterService.ReceiveLetter(unreceivedLetterID);

        LogResult(
            "6. 한도 초과 편지 수신 차단",
            !receiveResult,
            $"ReceiveLetter 결과: {receiveResult}");

        int countAfter =
            letterService.GetCurrentLetterCount();

        LogResult(
            "7. 차단 후 현재 편지 수 유지",
            countAfter == countBefore,
            $"수신 전: {countBefore}, 수신 후: {countAfter}");
    }

    /// <summary>
    /// 아직 진행 데이터가 생성되지 않은 편지 ID를 하나 반환함
    /// </summary>
    private int FindUnreceivedLetterID()
    {
        var letters = staticDataCatalog.Letters();

        if (letters == null)
        {
            return 0;
        }

        foreach (LetterStaticData letter in letters)
        {
            if (letter == null || letter.LetterID <= 0)
            {
                continue;
            }

            if (playerDataManager.GetLetterProgress(letter.LetterID) == null)
            {
                return letter.LetterID;
            }
        }

        return 0;
    }

    /// <summary>
    /// 개별 테스트 결과를 출력하고 성공·실패 횟수를 기록함
    /// </summary>
    private void LogResult(
        string testName,
        bool success,
        string detail)
    {
        if (success)
        {
            passCount++;

            Debug.Log(
                $"[LetterCapacityTest][PASS] {testName} | {detail}");
        }
        else
        {
            failCount++;

            Debug.LogError(
                $"[LetterCapacityTest][FAIL] {testName} | {detail}");
        }
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        Debug.Log(
            "========== Letter Capacity Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
