# 밤에 오는 편지 (Night Post)

낮에는 편지를 정리하고 배달을 준비하며,  
밤에는 게임을 종료해도 배달이 계속 진행되는 **감성 우체국 방치형 게임**입니다.

Unity 기반 모바일 게임으로 제작했으며, 단순한 방치형 수치 성장보다  
**편지 수신 → 배달부·노선 배정 → 오프라인 배달 → 결과 확인 → 새로운 콘텐츠 해금**이
하나의 흐름으로 이어지는 시스템 구조를 설계하는 데 집중했습니다.

또한 Android 홈 화면에서도 배달 진행 상황을 확인할 수 있도록
**Android 위젯과 Unity 데이터를 연동**했습니다.

---

## 프로젝트 개요

| 항목 | 내용 |
| --- | --- |
| 게임명 | 밤에 오는 편지 (Night Post) |
| 장르 | 감성 방치형 / 우체국 운영 |
| 플랫폼 | Android |
| 개발 환경 | Unity / C# |
| 개발 인원 | 개발 2인 / 디자인 1인 |
| 담당 | 메인 클라이언트 개발 / 시스템 설계 |
| 프로젝트 성격 | NHN 게임 공모전 출품 프로젝트 |
| 주요 목표 | 오프라인 진행과 편지 콘텐츠가 연결되는 방치형 시스템 구현 |

---

## 핵심 구현

- 편지 수신 및 진행 상태 관리
- 배달부 / 노선 배정 시스템
- 실제 경과 시간 기반 오프라인 배달
- 배달 결과 및 답장 흐름
- 조건 기반 편지 / 배달부 / 노선 해금
- 시설 강화 시스템
- ScriptableObject 기반 정적 데이터 관리
- SQLite 기반 플레이 데이터 저장
- Service / UI 이벤트 기반 책임 분리
- Unity ↔ Android 홈 위젯 데이터 연동

---

## 게임 흐름

우체국에 새로운 편지가 도착하면 내용을 확인하고,
배달 조건에 맞는 배달부와 노선을 선택합니다.

배달을 시작한 뒤 게임을 종료해도 실제 시간이 계속 흐르며,
다시 접속하면 경과 시간을 계산해 배달 결과를 확인할 수 있습니다.

```text
편지 도착
→ 편지 확인 및 분류
→ 배달부 선택
→ 노선 선택
→ 배달 시작
→ 오프라인 진행
→ 배달 결과 확인
→ 답장 / 보상 획득
→ 새로운 콘텐츠 해금
주요 시스템
1. 편지 진행 시스템

LetterService를 중심으로 각 편지의 현재 상태를 관리하도록 구성했습니다.

편지가 처음 수신되는 순간부터 읽기, 배달 대기, 배달 진행,
배달 완료까지의 상태를 하나의 흐름으로 관리합니다.

구현 내용
편지별 진행 데이터 관리
신규 편지 수신
읽음 상태 관리
배달 대기 상태 전환
배달 진행 / 완료 상태 연동
현재 이용 가능한 편지 목록 조회
정적 편지 데이터와 플레이 진행 데이터 분리
상태 흐름
New
→ Waiting
→ Delivering
→ Completed
설계 의도

편지의 제목, 내용, 목적지 같은 변하지 않는 정보와
현재 읽었는지, 배달 중인지 등의 플레이 상태를 분리했습니다.

정적 정보는 LetterStaticData,
진행 상태는 LetterProgressData에서 관리하도록 구성해
콘텐츠 데이터와 플레이 데이터를 독립적으로 관리할 수 있도록 했습니다.

2. 배달 시스템

편지와 배달부, 노선을 조합해 실제 배달을 시작하는 시스템입니다.

배달부의 특성과 노선 환경을 비교해 배달 조건을 판단하고,
배달 시작 이후에는 진행 데이터를 별도로 관리하도록 구성했습니다.

구현 내용
배달 대상 편지 선택
배달부 선택
노선 선택
배달 가능 여부 검사
배달 시작 시간 기록
완료 예정 시간 계산
복수 배달 진행 데이터 관리
편지 상태와 배달 상태 연동
배달 흐름
Letter
   ↓
Courier 선택
   ↓
Route 선택
   ↓
조건 검사
   ↓
ActiveDelivery 생성
   ↓
배달 진행
설계 의도

편지 자체가 배달 시간을 관리하도록 하지 않고,
실제 진행 중인 배달은 별도의 ActiveDeliveryData로 관리했습니다.

이를 통해 여러 편지가 동시에 배달 중인 상황에서도
각 배달의 시작 시간과 완료 시간을 독립적으로 관리할 수 있도록 했습니다.

3. 오프라인 진행 시스템

게임이 종료되어 있어도 배달 시간이 흐르는 방치형 시스템을 구현했습니다.

게임을 종료할 때 진행 중인 배달 정보를 저장하고,
재접속 시 현재 시간과 완료 예정 시간을 비교해
배달 완료 여부를 판단합니다.

구현 내용
배달 시작 시 완료 예정 시각 저장
게임 종료 후 실제 경과 시간 반영
재접속 시 진행 중인 배달 검사
완료된 배달 자동 처리
복수 배달 동시 진행 지원
배달 결과 시스템과 연동
처리 흐름
배달 시작
→ 시작 시각 / 완료 예정 시각 저장
→ 게임 종료
→ 실제 시간 경과
→ 게임 재실행
→ 현재 시간과 비교
→ 완료 배달 처리
설계 의도

게임 내부의 Update()에 의존해 시간을 감소시키는 방식이 아니라,
절대 시간 기준으로 완료 예정 시각을 저장하도록 구성했습니다.

이를 통해 앱이 실행되지 않는 동안에도
실제 경과 시간을 그대로 게임 진행에 반영할 수 있도록 했습니다.

4. 진행 및 콘텐츠 해금 시스템

게임 진행에 따라 새로운 편지와 콘텐츠가 자동으로 열리도록
ProgressionService를 구성했습니다.

초기에는 편지를 직접 호출해 수신하는 방식이었지만,
게임 진행 조건과 연결해 자동으로 새로운 편지가 들어오도록 개선했습니다.

구현 내용
기본 해금 콘텐츠 적용
누적 배달 완료 횟수 기반 해금
새로운 편지 자동 수신
배달부 해금
노선 해금
정적 해금 조건과 플레이 진행 데이터 연결
예시
게임 시작
→ 기본 편지 수신
→ 배달 완료
→ 누적 완료 횟수 증가
→ 해금 조건 검사
→ 새로운 편지 / 노선 / 배달부 활성화
설계 의도

UI나 개별 콘텐츠에서 직접 다음 콘텐츠를 열지 않고,
ProgressionService에서 진행 조건을 검사하도록 분리했습니다.

이를 통해 해금 조건이 변경되더라도
각 UI나 개별 시스템을 수정하지 않고 진행 규칙만 변경할 수 있도록 했습니다.

5. 시설 강화 시스템

획득한 재화를 사용해 우체국 시설을 강화하고,
배달 관련 효과를 얻을 수 있도록 구현했습니다.

시설마다 레벨별 비용과 효과를 정적 데이터로 관리하고,
현재 플레이어가 보유한 시설 레벨은 별도의 플레이 데이터에서 관리합니다.

구현 내용
시설별 현재 레벨 관리
업그레이드 비용 검사
재화 차감
레벨 증가
레벨별 누적 효과 적용
최대 레벨 처리
시설 UI와 실시간 연동
데이터 구조
FacilityStaticData
 ├─ Facility ID
 ├─ Name
 ├─ Description
 ├─ Sprite
 └─ FacilityLevelData[]
      ├─ Level
      ├─ UpgradeCost
      ├─ EffectType
      └─ EffectValue
설계 의도

시설 강화 효과를 코드 안에 직접 고정하지 않고
FacilityLevelData로 분리했습니다.

이를 통해 시설별 레벨 수, 강화 비용, 효과 값을
코드를 수정하지 않고 데이터에서 조정할 수 있도록 구성했습니다.

6. 정적 데이터 관리

편지, 배달부, 노선, 시설, 답장 등
게임 콘텐츠 데이터를 ScriptableObject 기반으로 관리했습니다.

StaticDataCatalog에서 각 데이터를 ID 기준으로 조회할 수 있도록 구성하고,
CSV Importer를 통해 콘텐츠 데이터를 생성할 수 있도록 했습니다.

구현 내용
LetterStaticData
CourierStaticData
RouteStaticData
FacilityStaticData
ReplyStaticData
StaticDataCatalog
List → Dictionary 조회 구조
ID 기반 데이터 조회
CSV → ScriptableObject Import
구조
CSV
 ↓
StaticDataCSVImporter
 ↓
ScriptableObject
 ↓
StaticDataCatalog
 ↓
각 Service에서 조회
설계 의도

게임 로직과 콘텐츠 데이터를 분리해
편지나 배달부가 추가될 때마다 코드 수정이 발생하지 않도록 했습니다.

또한 모든 시스템이 각 ScriptableObject를 직접 탐색하지 않고,
StaticDataCatalog를 통해 필요한 데이터를 조회하도록 구성했습니다.

7. UI / 시스템 이벤트 구조

UI가 Service나 저장 데이터를 직접 변경하지 않도록
데이터 변경 책임과 화면 표시 책임을 분리했습니다.

UI는 공개된 요청 함수를 통해 동작을 요청하고,
실제 데이터가 변경되면 이벤트를 받아 화면을 갱신합니다.

구조
UI
 ↓ 요청
Service
 ↓
PlayerData 변경
 ↓
GameEvents
 ↓
Presenter / UI 갱신
구현 방향
UI에서 PlayerSaveData 직접 수정 금지
실제 데이터 변경은 Service에서 수행
UI는 공개 API를 통해 요청
상태 변경 후 GameEvents 발생
Presenter가 이벤트를 받아 UI 재바인딩
배달 / 편지 / 시설 / 재화 이벤트 분리
설계 의도

UI와 핵심 로직의 의존도를 낮추기 위해
UI가 데이터 구조를 직접 알고 수정하는 방식을 사용하지 않았습니다.

이를 통해 UI가 변경되어도 핵심 게임 로직에 미치는 영향을 줄이고,
서브 개발자가 UI를 연결할 때도 정해진 API와 이벤트만 사용할 수 있도록 했습니다.

8. Android 홈 위젯

게임을 실행하지 않아도 홈 화면에서
현재 배달 진행 상황을 확인할 수 있도록 Android 위젯을 구현했습니다.

Unity에서 현재 배달 데이터를 Android Native 영역으로 전달하고,
위젯에서는 전달받은 값을 기반으로 화면을 갱신합니다.

표시 정보
배달 대기 중인 편지 수
도착 완료된 편지 수
진행 중인 배달 완료 예정 시간
연동 흐름
Unity
 ↓
AndroidWidgetBridge
 ↓
Android Native Plugin
 ↓
Widget Data 저장
 ↓
AppWidget 갱신
설계 의도

위젯 자체에서 게임 데이터를 계산하지 않고,
게임에서 계산된 결과만 Android 영역으로 전달하도록 구성했습니다.

이를 통해 핵심 게임 로직은 Unity에 유지하고,
Android 위젯은 표시 역할에 집중하도록 책임을 분리했습니다.

데이터 구조

게임 데이터는 크게 정적 데이터와 플레이 데이터로 분리했습니다.

Static Data

게임 콘텐츠 자체를 정의합니다.

LetterStaticData
CourierStaticData
RouteStaticData
FacilityStaticData
ReplyStaticData
Player Data

현재 플레이 진행 상태를 저장합니다.

LetterProgressData
CourierProgressData
RouteProgressData
FacilityProgressData
ActiveDeliveryData
데이터 흐름
StaticData
     ↓
 Service
 ↙       ↘
조회      PlayerData 변경
             ↓
          SaveData
             ↓
           SQLite
저장 시스템

플레이 진행 데이터는 로컬 SQLite를 사용해 저장하도록 구성했습니다.

각 Service가 SQLite에 직접 접근하지 않고,
저장 시스템을 통해 플레이 데이터를 관리하도록 역할을 분리했습니다.

설계 원칙
Service에서 DB 직접 접근 금지
정적 데이터는 ScriptableObject에서 관리
플레이 데이터만 SQLite에 저장
저장 구조와 게임 로직 분리
ID 기반으로 StaticData와 진행 데이터 연결
담당 구현

본 프로젝트에서 메인 클라이언트 개발을 담당하며
핵심 게임 시스템의 구조 설계와 구현을 진행했습니다.

전체 게임 시스템 구조 설계
정적 데이터 구조 설계
StaticDataCatalog
CSV Importer
편지 진행 시스템
배달 시스템
오프라인 진행 시스템
배달 결과 처리
콘텐츠 해금 구조
시설 강화 시스템
저장 데이터 구조
UI / Service 이벤트 구조 설계
Android 위젯 연동
플레이어 이동 및 상호작용 구조
기술 스택
Unity 6
C#
ScriptableObject
SQLite
Android Native Plugin
Android AppWidget
Gradle
Unity UI
TextMeshPro
Git / GitHub
협업 구조

개발자 2명과 디자이너 1명으로 구성된 팀 프로젝트로 진행했습니다.

메인 개발자는 게임 데이터와 핵심 시스템 구조를 담당하고,
서브 개발자가 UI 기능을 연결할 수 있도록
공개 API와 이벤트 명세를 먼저 정의하는 방식으로 협업했습니다.

이벤트 명세

각 이벤트마다 다음 항목을 정리해 공유했습니다.

이벤트명
전달값
발생 위치
발생 시점
UI 반응
실패 / 중복 처리

이를 통해 UI 구현자가 내부 저장 구조나 Service 구현을 직접 수정하지 않고도
정해진 인터페이스를 통해 기능을 연결할 수 있도록 했습니다.

회고

이 프로젝트를 통해 방치형 게임에서 단순히 시간이 흐르는 기능을 구현하는 것보다,
여러 시스템이 하나의 게임 흐름으로 연결되는 구조를 설계하는 과정이 중요하다는 것을 경험했습니다.

편지 수신, 배달부와 노선 배정, 배달 시작, 오프라인 진행,
배달 결과, 보상, 콘텐츠 해금은 각각 별개의 기능처럼 보이지만
실제 플레이에서는 하나의 연속된 흐름으로 동작해야 했습니다.

따라서 각 시스템의 책임을 분리하면서도
LetterService, DeliveryService, ProgressionService,
시설 및 저장 시스템이 서로 필요한 정보만 주고받도록 구조화하는 데 집중했습니다.

특히 UI가 게임 데이터를 직접 수정하지 않고
Service와 이벤트를 통해 변경 사항을 전달하도록 구성하면서,
기능 구현뿐 아니라 팀원이 안전하게 기능을 연결할 수 있는 구조의 중요성도 경험했습니다.

또한 Android 홈 위젯을 추가하면서
Unity 내부 기능에만 머무르지 않고 Native Android 영역과 데이터를 연결하는 과정도 경험했습니다.

이번 프로젝트를 통해
콘텐츠 데이터 관리, 런타임 상태 관리, 오프라인 진행, UI 이벤트 구조,
Native 기능 연동까지 하나의 게임 구조로 설계하고 연결하는 경험을 쌓을 수 있었습니다.

# 🌱 Git Commit Convention

프로젝트의 모든 커밋은 아래 컨벤션을 따릅니다.

## 📌 Commit Message Format

```text
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

### Examples

```text
feat(player): 플레이어 점프 기능 추가
fix(enemy): 보스 AI가 벽을 통과하는 문제 수정
refactor(inventory): 아이템 관리 로직 개선
docs(readme): 프로젝트 실행 방법 추가
scene(lobby): 로비 레벨 수정
asset(character): 플레이어 모델 추가
```

---

# 📖 Commit Types

| Type | Description |
| :--- | :---------- |
| **feat** | 새로운 기능 추가 |
| **fix** | 버그 수정 |
| **refactor** | 기능 변경 없이 코드 구조 개선 |
| **perf** | 성능 개선 |
| **style** | 코드 스타일 수정 (포맷팅, 공백, 세미콜론 등) |
| **design** | UI 및 디자인 변경 |
| **docs** | README, 문서, 주석 수정 |
| **test** | 테스트 코드 추가 및 수정 |
| **chore** | 빌드, 설정, 패키지 등 기타 작업 |
| **build** | 빌드 시스템 및 의존성 변경 |
| **ci** | CI/CD 설정 변경 |
| **rename** | 파일 또는 폴더 이름 변경 |
| **remove** | 코드, 파일 삭제 |

---

# 🎮 Game Project Types

게임 프로젝트에서 사용하는 커밋 타입입니다.

| Type | Description |
| :--- | :---------- |
| **asset** | 에셋 추가 및 수정 (Prefab, FBX, Material, Texture 등) |
| **scene** | Scene 또는 Level 수정 |
| **anim** | Animation 추가 및 수정 |
| **audio** | Sound, BGM 추가 및 수정 |
| **shader** | Shader 및 Material 관련 수정 |
| **vfx** | 파티클 및 시각 효과 추가/수정 |
| **ui** | UI 시스템 및 위젯 수정 |
| **localization** | 다국어(Localization) 수정 |
| **save** | 저장 시스템 수정 |
| **network** | 멀티플레이 및 네트워크 기능 수정 |

---

# 📂 Scope

변경된 기능이나 모듈을 작성합니다.

### Example

```text
player
enemy
inventory
quest
ui
audio
scene
network
save
animation
shader
database
login
```


# 🚀 Branch Naming Convention

| Branch | Description |
| :------ | :---------- |
| **main** | 운영 브랜치 |
| **develop** | 개발 브랜치 |
| **feature/** | 새로운 기능 개발 |
| **fix/** | 버그 수정 |
| **hotfix/** | 긴급 수정 |
| **release/** | 배포 준비 |
| **refactor/** | 리팩토링 |
| **docs/** | 문서 수정 |
| **test/** | 테스트 |

### Examples

```text
feature/player-movement
feature/inventory-system
fix/enemy-ai
hotfix/login-error
refactor/fsm
docs/readme
release/v1.0.0
```
