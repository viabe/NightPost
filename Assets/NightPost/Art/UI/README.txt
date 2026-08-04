NIGHT POST — UI 스프라이트 팩 (Unity 6 / 모바일 가로 1920x1080)
=================================================================

■ 파일 형식
- PNG-32, 배경 투명, 2x 해상도로 저장됨 (파일 픽셀 = 화면 표시 크기 x2)
- 텍스트는 굽지 않았습니다. 모든 문구는 Unity에서 TextMeshPro로 얹어 주세요.

■ Unity 임포트 설정 (공통)
Texture Type      : Sprite (2D and UI)
Sprite Mode       : Single
Pixels Per Unit   : 100
Mesh Type         : Full Rect        ← 9-slice 쓰려면 반드시 Full Rect
Filter Mode       : Bilinear
Compression       : None 또는 RGBA 32bit (UI는 압축 아티팩트가 잘 보입니다)
Generate Mip Maps : 끄기
Max Size          : 2048

■ 9-slice Border 값
Sprite Editor에서 Border 를 아래대로 입력하면 어떤 크기로 늘려도 모서리가 깨지지
않습니다. 값은 2x 파일 기준입니다 (L, B, R, T 순서 = Left, Bottom, Right, Top).

panel_popup.png            L72  B72  R72  T72
panel_inner.png            L60  B60  R60  T60
panel_paper.png            L60  B60  R60  T60
panel_dark.png             L60  B60  R60  T60
panel_header.png           L56  B0   R56  T56
btn_primary_normal.png     L52  B0   R52  T0
btn_primary_pressed.png    L52  B0   R52  T0
btn_primary_disabled.png   L52  B0   R52  T0
btn_secondary_normal.png   L52  B0   R52  T0
btn_secondary_pressed.png  L52  B0   R52  T0
row_normal.png             L56  B0   R56  T0
row_selected.png           L56  B0   R56  T0
row_disabled.png           L56  B0   R56  T0
chip_on.png                L64  B0   R64  T0
chip_off.png               L64  B0   R64  T0
chip_locked.png            L64  B0   R64  T0
badge_red.png              L40  B0   R40  T0
badge_brown.png            L40  B0   R40  T0
badge_outline.png          L40  B0   R40  T0
hud_pill.png               L70  B0   R70  T0
toast_bg.png               L88  B0   R88  T0
bar_track.png              L24  B0   R24  T0
bar_fill.png               L24  B0   R24  T0

Border 없이 그대로 쓰는 스프라이트 (Image Type = Simple):
btn_close, badge_count, dot_new, toggle_on, toggle_off, checkbox_on,
checkbox_off, bar_handle, icon_coin, icon_stamp, icon_lock, icon_arrow,
envelope_normal, envelope_faded, envelope_open, stamp_postmark

■ 컴포넌트 조립 예시
[주 버튼]      Image(btn_primary_normal, Sliced) + TMP(22px, #F7EFDF, 중앙)
               Button > Transition: Sprite Swap
               Pressed = btn_primary_pressed / Disabled = btn_primary_disabled
[리스트 행]    Image(row_normal, Sliced), 높이 96 고정, VerticalLayoutGroup spacing 12
               선택 시 row_selected 로 스왑
[분류 칩]      Toggle + Image(chip_off ↔ chip_on), 높이 64
[경험치 바]    Image(bar_track, Sliced) 안에 Image(bar_fill, Sliced, Type=Filled,
               Fill Method=Horizontal) 을 자식으로
[볼륨 슬라이더] Slider: Background=bar_track / Fill=bar_fill / Handle=bar_handle
[토스트]       Image(toast_bg, Sliced) + TMP, CanvasGroup 알파 페이드 0.2s

■ 색상 팔레트
종이 밝은 면   #F6EEDD
종이 그늘      #EADCC0
테두리         #C9B896
나무           #8C6A4A
우편 빨강      #C1453A   ← 화면당 확정 액션 1개에만
잉크 (텍스트)  #3D3229
비활성         #D6C7AA / 텍스트 #A2957D
코인 금색      #E3B94F

■ 터치 규격
주 버튼 높이 88 / 리스트 행 높이 96 / 최소 터치 영역 72x72
(2x 파일이므로 Unity 상 Rect 값은 위 수치 그대로, 스프라이트만 2배 해상도)

■ 원칙
상태는 색이 아니라 형태로 구분합니다.
  선택   = 빨간 테두리 3px
  잠김   = 점선 테두리 + icon_lock 오버레이
  비활성 = 채도 낮춤 + 아래 두께 제거
