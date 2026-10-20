# 일차 재구성

자료 설계와 한 판 구현을 2일차로 통합하고, 서버 구축과 원격 배포를 8·9일차로 분리했습니다. 16일 × 6시간과 보완 4시간을 유지합니다.

| 새 일차 | 이전 완료 상태 | 조정 |
| --- | --- | --- |
| 01 | 01 | 교사용·템플릿 C# 제거, 바닥·카메라·프로필만 준비 |
| 02 | 02 + 03 | CoinRound·GameSession·CoinPickup과 기본 UI |
| 03 | 04 | 입력과 MobileControls 추가 |
| 04 | 05 | 최고 점수·SaveButton·Safe Area 추가 |
| 05 | 06 | QuitButton·종료·학생용 Editor 진단 코드 추가 |
| 06 | 07 | StageLoader·ReloadStageButton 추가 |
| 07 | 08 | 로컬 콘텐츠 빌드 |
| 08 | 08 상태 유지 + 서버 | ASP.NET Core HTTPS 파일 서버, JSON·404·연결 실패 |
| 09 | 09 | HTTPS 원격 번들 배포·캐시·실패 복구 |
| 10~16 | 같은 번호 | 새 번호와 도구 분리에 맞춰 안내 갱신 |

기존 브랜치 전체는 재구성 전에 별도 BeforeRebuild.bundle로 보관했습니다. 예전 생성물과 실행 결과를 이번 소스의 검증 결과로 간주하지 않습니다. 현재 검증 범위는 VALIDATION.md에서 확인합니다.
