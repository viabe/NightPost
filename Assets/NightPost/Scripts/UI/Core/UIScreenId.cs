namespace NightPost.UI
{
    /// <summary>
    /// 화면/팝업 식별자. PopupManager가 이 값으로 뷰를 열고 닫는다.
    /// technical_design_v1 §2-2 PostOffice Canvas 하위 팝업 목록과 1:1로 대응한다.
    /// </summary>
    public enum UIScreenId
    {
        None = 0,

        Home,           // 홈 오버레이(메인 우체국) — 상시 표시
        EnvelopePopup,  // 편지 겉면 (일반 편지, 개봉 버튼 없음)
        Assignment,     // 배달부·지역 배정 팝업
        MorningReport,  // 아침 보고 (오프라인 정산 결과)
        LetterRead,     // 편지 열람 본문 (할아버지/창고/답장/오늘의 편지)
        TodayLetter,    // 오늘의 편지 연출
        CourierHire,    // 배달부 고용
        Warehouse,      // 창고 (서사 편지)
        FirstReply,     // 첫 답장 (선택지)
        Inbox,          // 수신함 (답장 재열람)

        Confirm,        // 공통 확인/알림 다이얼로그
    }
}
