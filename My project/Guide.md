# New Unity Project Setup & CopyIt Architecture Learning Guide

## 목적
이 문서는 빈 Unity 프로젝트를 새로 만들고, `CopyIt`에서 사용된 아키텍처를 학습하는 데 필요한 가이드를 제공합니다.

- 새 Unity 프로젝트에서 사용할 초기 `.gitignore` 설정
- `CopyIt` 아키텍처에서 핵심 패턴
- 새 프로젝트에서 구현하는 순서와 방법

> 이 문서는 `CopyIt` 프로젝트의 내부 구현이 아니라, 새 프로젝트에서 동일한 구조를 학습하고 재현하는 데 필요한 내용을 설명합니다.

## 1. 새 Unity 프로젝트 초기 설정

### 1.1 프로젝트 생성

1. Unity Hub에서 새 프로젝트를 생성합니다.
2. 가능한 경우 `Unity 2024.3.x` 버전을 사용합니다.
3. 프로젝트 경로는 Git 저장소 루트 아래에 두는 것이 일반적입니다.

> 이 프로젝트에서는 Unity Editor Console을 사용하지 않고, 필요한 로그/에러는 별도 UI 또는 로그 파일로 처리하는 방향으로 진행합니다.

### 1.2 초기 `.gitignore` 설정

새 Unity 프로젝트의 루트에 아래 내용을 `.gitignore`로 추가합니다.

```gitignore
# macOS
.DS_Store
.DS_Store?
.AppleDouble
.LSOverride
**/.DS_Store
Thumbs.db
*.lscache

# IDE
.vscode/
.idea/
*.sln*
*.csproj
*.unityproj
*.user
*.userprefs
*.pidb
*.pidb.meta
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.log

# Unity generated folders
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
/[Rr]ecordings/
/sysinfo.txt

# Addressables
/[Aa]ssets/[Aa]ddressable[Aa]ssets[Dd]ata/*/*.bin*

# Android temp assets
/[Aa]ssets/[Ss]treamingAssets/aa.meta
/[Aa]ssets/[Ss]treamingAssets/aa/*

# Package/tool folders
/[Aa]ssets/[Ll]ayer Lab/
```

- `.gitignore`는 프로젝트를 열기 전에 커밋하는 것을 권장합니다.
- 이렇게 하면 `Library`, `Temp`, `Obj`, `Build`, `Logs` 등 불필요한 파일이 저장소에 들어가지 않습니다.

## 2. CopyIt 아키텍처 핵심 패턴

`CopyIt` 아키텍처는 다음 요소로 구성됩니다.

### 2.1 계층 구조

- `Base`: 공통 싱글톤 매니저 베이스
- `Common`: 서비스 초기화, 인증, 원격 설정, 공통 유틸
- `Data`: 도메인 모델, 설정 데이터, 직렬화 클래스
- `Event`: ScriptableObject 기반 이벤트 채널
- `Game`: 게임 상태 및 로직, 매니저, 컨트롤러
- `UI`: 뷰와 사용자 입력 처리
- `VFX`: 시각 효과 관련 요소

### 2.2 싱글톤 매니저 패턴

- 모든 전역 서비스는 `ManagerBase<T>` 형태의 베이스 클래스를 상속합니다.
- `Awake()`에서 인스턴스를 설정하고 중복을 방지합니다.
- `shouldDontDestroyOnLoad`를 사용해 필요한 매니저를 씬 간 유지합니다.

### 2.3 ScriptableObject 설정 객체

- `GameConfigs`와 같은 설정 객체를 `ScriptableObject`로 만듭니다.
- Inspector에서 값을 편집하고, 코드에서는 public getter/메서드로 접근합니다.
- 밸런스, 비용, 확률, 보상 계산 등은 SO 내부에서 처리합니다.

### 2.4 이벤트 채널 아키텍처

- `EventChannelContainer`는 여러 이벤트 채널을 한 곳에서 관리하는 컨테이너입니다.
- 각 채널은 `ScriptableObject`이며 `UnityEvent`를 포함합니다.
- `Register/Unregister/Invoke` 메서드로 발행자-구독자를 분리합니다.
- UI는 이벤트를 구독하고, 게임 로직은 이벤트를 발행합니다.

### 2.5 상태 기반 게임 흐름

- `BootState`부터 `TitleState`, `HomeState` 등 상태 객체로 씬과 흐름을 관리합니다.
- `GameManager`가 상태를 생성하고 전환합니다.
- 각 상태는 Enter/Exit/Update를 분리하여 유지보수를 쉽도록 합니다.

### 2.6 서비스 초기화 분리

- `UGSManager` 같은 초기화 전용 매니저를 둡니다.
- Unity Gaming Services, Remote Config, 인증 등을 비동기 초기화합니다.
- `ConfigManager`는 UGS 초기화 완료 후 원격 설정 값을 가져옵니다.

## 3. 새 프로젝트에서 구현하는 방법

### 3.1 추천 폴더 구조

```
Assets/
  Scripts/
    Base/
    Common/
    Event/
    Data/
    Game/
    UI/
    VFX/
```

### 3.2 핵심 클래스 및 스크립트

- `Assets/Scripts/Base/ManagerBase.cs`
  - 싱글톤 `MonoBehaviour` 베이스
- `Assets/Scripts/Common/UGSManager.cs`
  - UGS 초기화 및 서비스 준비
- `Assets/Scripts/Common/ConfigManager.cs`
  - 원격 설정 및 로컬 기본값 관리
- `Assets/Scripts/Game/GameConfigs.cs`
  - 게임 밸런스 설정 `ScriptableObject`
- `Assets/Scripts/Event/EventChannelContainer.cs`
  - 여러 이벤트 채널 레퍼런스 보관
- `Assets/Scripts/Event/WorldEventChannel.cs`
  - `UnityEvent` 기반 이벤트 채널
- `Assets/Scripts/Game/GameState/BootState.cs`
  - 부트 흐름 실행
- `Assets/Scripts/Game/Manager/GameManager.cs`
  - 상태 전환과 서비스 연결

> 이 프로젝트에서는 Unity Console 출력에 의존하지 않습니다. 디버그와 상태 확인은 in-game UI, 로그 파일, 또는 자체 상태 표시 방식으로 구현합니다.

### 3.3 구현 순서

1. `ManagerBase<T>`를 구현하여 싱글톤 구조를 준비합니다.
2. `UGSManager`를 만들고 `UnityServices.InitializeAsync()`를 호출할 수 있도록 합니다.
3. `ConfigManager`를 구현하고, 설정 조회 및 원격 설정 로직을 분리합니다.
4. `GameConfigs` `ScriptableObject`를 생성하여 게임 밸런스 및 설정값을 저장합니다.
5. `EventChannelContainer`와 적어도 `WorldEventChannel` 하나를 구현합니다.
6. `BootState`를 작성하여 부팅 단계별 처리를 순서대로 실행합니다.
7. `GameManager`에서 상태 객체를 생성하고 초기 상태로 부팅 상태를 전환합니다.
8. Unity 씬에 `GameManager`를 배치하고, 필요한 SO를 할당합니다.

### 3.4 학습 포인트

- 이벤트 채널로 발행자/구독자 분리
- `ScriptableObject`로 설정과 데이터 분리
- 상태 객체로 게임 흐름 관리
- 초기화 로직과 비즈니스 로직 분리

## 4. 새 프로젝트 Git 연동 요약

- 프로젝트 루트에 `.gitignore`를 추가합니다.
- `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/` 를 무시합니다.
- 초기 커밋 전에 `.gitignore`를 먼저 추가하면 불필요한 파일이 들어가지 않습니다.

## 5. 구현 확인 체크리스트

- [ ] `ManagerBase<T>`가 정상 동작하는가
- [ ] `UGSManager`가 정상 초기화 되는가
- [ ] `ConfigManager`가 설정값을 제공하는가
- [ ] `ScriptableObject`와 이벤트 채널이 분리되어 있는가
- [ ] `BootState`가 순차적으로 상태를 진행하는가
- [ ] UI가 이벤트 구독 형태로 구현되어 있는가
- [ ] `.gitignore`가 불필요한 캐시/빌드 파일을 제외하는가
