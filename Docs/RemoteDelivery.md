# HTTPS 원격 스테이지 배포

기본 완료본은 로컬 콘텐츠입니다. 8일차에는 ContentServer를 만들고 9일차에는 같은 PC의 Windows Player로 원격 번들을 비교합니다.

1. ContentServer 폴더에서 `dotnet dev-certs https --check --trust`로 개발 인증서를 확인합니다. 준비하지 않았다면 `dotnet dev-certs https --trust`를 실행합니다.
2. `dotnet run --no-launch-profile`로 실행한 뒤 `https://localhost:5443/health`와 `/platform-config.json`의 45초 응답을 확인합니다. 서버를 Ctrl+C로 종료합니다.
3. Addressables Profiles에서 Default를 복제한 RemotePractice를 선택합니다. RemoteBuildPath는 `ServerData/[BuildTarget]`, RemoteLoadPath는 `https://localhost:5443/[BuildTarget]`입니다. CourseStage의 경로를 원격 변수로 바꾸고 Build Remote Catalog는 끕니다.
4. Windows용 콘텐츠를 먼저 빌드하고 Windows Player를 `Builds/WindowsRemote`에 만듭니다. ContentServer 폴더에서 `dotnet run --no-launch-profile -- --FilesRoot ../ServerData`로 제공합니다.
5. 캐시가 없는 조건에서 실패·서버 복구·Reload Stage를 확인합니다. 서버의 no-cache는 Unity AssetBundle 캐시 삭제가 아닙니다.
6. Default·로컬 그룹 경로를 복원하고 로컬 콘텐츠를 다시 빌드합니다. Editor의 Play Mode Script는 Use Asset Database로 되돌립니다.

12일차에는 `dotnet run --no-launch-profile -- --FilesRoot ../Builds/Web`로 Compression Disabled Web 출력을 제공합니다. HTML 파일을 직접 더블클릭하지 않습니다. Android의 localhost는 기기 자신이며 별도 주소·인증서 이름·장치 신뢰 구성이 필요합니다. 인증서 검증을 끄지 않습니다.
