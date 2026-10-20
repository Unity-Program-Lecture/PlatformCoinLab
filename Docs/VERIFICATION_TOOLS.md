# 교사용 검증 도구

Tools 아래 스크립트는 Assets 밖에 있습니다. Unity CLI의 Pipeline run_script로 명시적으로 실행할 때만 메모리에서 컴파일됩니다. 학생의 실습 준비 조건이 아니며 Player에 포함되지 않습니다.

```powershell
unity command run_script --project-path . --caller plugin --skill unity-cli -- --file Tools/Teacher/CourseCheckpoint.cs --entry CourseCheckpoint.Inspect --args '[16]'
unity command set_autotick --project-path . --caller plugin --skill unity-cli
unity command editor_play --project-path . --caller plugin --skill unity-cli
unity command run_script --project-path . --caller plugin --skill unity-cli -- --file Tools/Verification/CourseFlowProbe.cs --entry CourseFlowProbe.Main --args '[16]' --timeout_ms 55000
unity command editor_stop --project-path . --caller plugin --skill unity-cli
```

검사가 실패해도 Play를 종료합니다. 검사 인수는 해당 브랜치 일차로 바꿉니다. 입력 검사는 가상 장치와 화면 버튼 이벤트를 사용하고, 일시정지는 메서드 호출로 재현하므로 실기기 입력·OS 전환의 증거가 아닙니다. 검사 중 점수와 PlayerPrefs를 바꾸므로 테스트 복사본에서 실행합니다.

CourseCheckpoint.Prepare는 씬을 재구성할 때만 사용하는 교사 작업입니다. 완료 브랜치를 열 때 자동 실행하지 않습니다. 일반 실행과 빌드는 학습문서의 Unity 메뉴를 사용합니다.
