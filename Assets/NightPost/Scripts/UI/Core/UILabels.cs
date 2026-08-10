namespace NightPost.UI
{
    /// <summary>
    /// enum → 화면 표시 문자열 변환 모음.
    /// 여러 화면이 같은 값을 다르게 표기하지 않도록 여기 한 곳에서만 정의한다.
    /// </summary>
    public static class UILabels
    {
        /// <summary>목적 지역 표시명.</summary>
        public static string Region(ERegionType region)
        {
            switch (region)
            {
                case ERegionType.Town: return "마을";
                case ERegionType.Mountain: return "산간";
                case ERegionType.Outskirts: return "외곽";
                default: return "-";
            }
        }

        /// <summary>긴급도 표시명. 일반은 빈 문자열(굳이 표시하지 않는다).</summary>
        public static string Urgency(ELetterUrgency urgency)
            => urgency == ELetterUrgency.Urgent ? "급함" : string.Empty;

        /// <summary>무게 표시명.</summary>
        public static string Weight(ELetterWeight weight)
        {
            switch (weight)
            {
                case ELetterWeight.Light: return "가벼움";
                case ELetterWeight.Heavy: return "무거움";
                default: return "보통";
            }
        }

        /// <summary>편지 진행 상태 표시명.</summary>
        public static string LetterState(ELetterProgressState state)
        {
            switch (state)
            {
                case ELetterProgressState.New: return "분류 전";
                case ELetterProgressState.Waiting: return "배달 대기";
                case ELetterProgressState.Delivering: return "배달 중";
                case ELetterProgressState.Completed: return "배달 완료";
                default: return "-";
            }
        }

        /// <summary>
        /// 시설 효과 문구. isCurrent=false면 "업그레이드 후 …" 형태로 바꾼다.
        /// (시설 시스템 UI 연동 명세서 v1.2 §9 규칙)
        /// </summary>
        public static string FacilityEffect(FacilityLevelData data, bool isCurrent)
        {
            if (data == null) return "효과 없음";

            switch (data.EffectType)
            {
                case EFacilityEffectType.DeliveryTimeReduction:
                {
                    int pct = UnityEngine.Mathf.RoundToInt(data.EffectValue * 100f);
                    return isCurrent ? $"배달 시간 {pct}% 감소" : $"업그레이드 후 {pct}% 감소";
                }
                case EFacilityEffectType.LetterCapacityIncrease:
                {
                    int n = UnityEngine.Mathf.FloorToInt(data.EffectValue);
                    return isCurrent ? $"편지 보유 한도 +{n}통" : $"업그레이드 후 +{n}통";
                }
                default:
                    return "적용 효과 없음";
            }
        }

        /// <summary>초 단위 시간을 "3분 20초" 형태로. 0 이하면 "곧 도착".</summary>
        public static string Duration(long seconds)
        {
            if (seconds <= 0) return "곧 도착";
            if (seconds < 60) return $"{seconds}초";

            long m = seconds / 60;
            long s = seconds % 60;
            if (m < 60) return s == 0 ? $"{m}분" : $"{m}분 {s}초";

            long h = m / 60;
            m %= 60;
            return m == 0 ? $"{h}시간" : $"{h}시간 {m}분";
        }
    }
}
