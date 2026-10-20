# PlatformCoinLab

변경된 게임플랫폼 16일 과정을 확인하는 교사용 Unity 참고 프로젝트입니다. 학생은 완성본이나 브랜치를 가져오지 않고 csharp_basic의 학습문서만 따라 자신의 프로젝트를 만듭니다.

- Unity 6000.6.6f1 / URP 17.6.0 / Input System 1.20.1 / Addressables 2.11.2
- 시작 씬: `Assets/Scenes/PlatformMain.unity`
- `day01`~`day16`은 해당 일차 완료 상태이며 각 브랜치는 전날 커밋을 이어받습니다. `main`은 최종 상태입니다.
- 1일차에는 Assets 안에 C# 스크립트와 게임 UI가 없습니다. 2일차부터 한 판을 진행하고, 3일차부터 키보드·게임패드·화면 버튼으로 이동합니다.
- 방향 UI는 3일차, 최고 점수·저장은 4일차, 종료는 5일차, 재로딩은 6일차, 설정 읽기는 13일차에 추가됩니다.
- 브랜치를 전환하기 전에 Play를 종료하고 Unity를 닫습니다. 비교할 때는 별도 clone을 사용합니다.

| 완료 브랜치 | 학습 내용 |
| --- | --- |
| day01 | 플랫폼 요구 사항과 개발 환경 |
| day02 | 게임 진행 기록과 한 판의 흐름 구현 |
| day03 | 키보드·게임패드·터치 입력 |
| day04 | 최고 점수 저장과 화면 대응 |
| day05 | 플랫폼 코드와 앱 생명주기 |
| day06 | Addressables로 스테이지 준비하기 |
| day07 | 로컬 콘텐츠 빌드와 Player 연결 |
| day08 | ASP.NET Core HTTPS 파일 서버 |
| day09 | 원격 콘텐츠 배포와 로드 오류 해결 |
| day10 | PC 빌드, 로그, 배포 |
| day11 | Android 빌드와 실기기 디버깅 |
| day12 | Web 빌드와 브라우저 디버깅 |
| day13 | 설정 파일을 다음 라운드에 적용하기 |
| day14 | 실제 게임 UI의 프로파일 분석과 개선 |
| day15 | 플랫폼별 성능과 콘텐츠 최적화 |
| day16 | 통합 테스트와 프로젝트 인수인계 |

## 실행과 콘텐츠 빌드

2일차에는 Inspector에서 Player를 옮겨 수집을 확인합니다. 3일차부터 Start → 이동·수집 → 시간 종료 → 결과 → Restart → 메뉴를 진행합니다. 6일차부터 StageMain을 Addressables로 로드합니다. Editor의 기본 Play Mode Script는 Use Asset Database입니다. 7일차부터 Player를 빌드하기 전에 Addressables Groups의 Build > New Build > Default Build Script로 대상 플랫폼의 로컬 콘텐츠를 빌드합니다.

8일차에는 Assets 밖의 ContentServer 폴더에서 ASP.NET Core HTTPS 서버를 만듭니다. 서버의 .NET 프로젝트 파일을 Assets로 옮기지 않습니다. 9일차의 원격 비교는 [원격 배포 안내](Docs/RemoteDelivery.md)를 따릅니다. 기본 완료본은 로컬 콘텐츠로 유지합니다. 12일차 Web은 Compression Disabled 출력을 같은 서버로 제공합니다. 학생 Android 도구는 Unity Hub에서 해당 Editor와 함께 설치한 SDK·NDK·OpenJDK를 사용합니다.

[일차별 변경표](Docs/CURRICULUM_MAP.md), [검증 현황](Docs/VALIDATION.md), [교사용 검증 도구](Docs/VERIFICATION_TOOLS.md)를 함께 확인합니다. Tools 아래 코드는 Assets 밖에 있어 평소 컴파일되거나 Player에 포함되지 않습니다. 원격 저장소에 게시하지 않은 로컬 저장소입니다.
