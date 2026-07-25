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
