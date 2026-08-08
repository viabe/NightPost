namespace NightPost.UI
{
    /// <summary>
    /// 한 번에 하나만 열려야 하는 전체 화면 UI.
    ///
    /// 분류·배달·시설 목록·아침 보고처럼 서로 대체 관계인 화면이 여기 해당한다.
    /// 이런 화면은 겹쳐 봐야 의미가 없고, 뒤에 남아 있으면 닫기 버튼이 헷갈린다.
    ///
    /// 반대로 팝업(시설 강화 상세·편지 열람·확인 창)은 이 인터페이스를 쓰지 않는다.
    /// 팝업은 아래 화면을 유지한 채 그 위에 겹쳐야 하기 때문이다.
    /// </summary>
    public interface IUIScreen
    {
        /// <summary>현재 열려 있는지.</summary>
        bool IsScreenOpen { get; }

        /// <summary>다른 화면이 열릴 때 호출된다.</summary>
        void Close();
    }
}
