using System.Collections;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 접속 직후 아침 보고를 한 번 띄운다.
    ///
    /// 타이밍이 중요하다. GameBootstrap은 Awake에서 서비스를 초기화하고,
    /// 오프라인 정산(OfflineProgressService.CheckOffline)은 Start에서 수행한다.
    /// 즉 "밤사이 완료된 배달"은 Start가 끝나야 결과 데이터로 존재한다.
    /// 컴포넌트 간 Start 순서는 보장되지 않으므로, 한 프레임을 넘긴 뒤에 조회한다.
    ///
    /// 보고할 내용이 없으면 ShowIfHasReport가 알아서 열지 않는다.
    /// </summary>
    public class MorningReportAutoOpener : MonoBehaviour
    {
        [SerializeField] private MorningReportUIController _report;

        [Tooltip("초기화 완료를 기다릴 대상. 비워도 동작하지만 연결하면 더 안전하다.")]
        [SerializeField] private GameBootstrap _bootstrap;

        [Tooltip("화면이 자리를 잡은 뒤 뜨도록 주는 여유 시간.")]
        [SerializeField, Min(0f)] private float _delaySeconds = 0.4f;

        [Tooltip("초기화를 기다리는 최대 시간. 넘으면 포기하고 열지 않는다.")]
        [SerializeField, Min(0.5f)] private float _timeoutSeconds = 5f;

        private IEnumerator Start()
        {
            if (_report == null)
            {
                Debug.LogError("[MorningReportAuto] MorningReportUIController 미연결", this);
                yield break;
            }

            // GameBootstrap.Start(오프라인 정산)가 끝나도록 한 프레임 넘긴다.
            yield return null;

            // 초기화가 늦어질 수 있으므로 완료될 때까지 기다린다(무한 대기는 하지 않는다).
            if (_bootstrap != null)
            {
                float waited = 0f;
                while (!_bootstrap.IsInitialized)
                {
                    if (waited >= _timeoutSeconds)
                    {
                        Debug.LogWarning("[MorningReportAuto] 초기화 대기 시간이 지나 아침 보고를 열지 않는다.");
                        yield break;
                    }
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (_delaySeconds > 0f) yield return new WaitForSecondsRealtime(_delaySeconds);

            // 완료된 배달·안 읽은 답장·해금 가능한 노선이 하나도 없으면 열리지 않는다.
            _report.ShowIfHasReport();
        }
    }
}
