# MemoTag

MemoTag는 Windows에서 현재 활성 앱 창에 색상 테두리와 메모를 붙여두는 작은 WPF 트레이 앱입니다.

닫으면 안 되는 창, 나중에 다시 확인해야 하는 창, 작업 중인 앱을 눈에 띄게 표시할 때 사용할 수 있습니다. 표시된 테두리와 메모는 대상 창을 이동하거나 크기를 바꿀 때 함께 따라갑니다.

## 주요 기능

- 현재 활성 창에 색상 테두리와 메모 표시
- 같은 단축키로 기존 메모 수정
- 전체화면 창에서도 보이도록 내부 테두리와 topmost overlay 처리
- 대상 창 이동/크기 변경 시 자동 위치 갱신
- 대상 창 최소화 시 표시 숨김, 다시 복원 시 표시 재개
- 대상 창이 닫히면 표시 자동 제거
- 시스템 트레이에서 표시 추가/해제/전체 해제/정보/종료 실행

## 단축키

| 동작 | 단축키 |
| --- | --- |
| 현재 창에 메모 표시 또는 수정 | `Ctrl+Alt+M` |
| 현재 창 표시 해제 | `Ctrl+Alt+Shift+M` |

## 사용법

1. MemoTag를 실행합니다.
2. 표시할 창을 활성화합니다.
3. `Ctrl+Alt+M`을 누릅니다.
4. 메모 내용과 테두리 색상을 선택한 뒤 저장합니다.
5. 표시를 지우려면 해당 창을 활성화한 뒤 `Ctrl+Alt+Shift+M`을 누릅니다.

트레이 아이콘을 우클릭하면 다음 메뉴를 사용할 수 있습니다.

- 현재 창에 메모 표시
- 현재 창 표시 해제
- 모든 표시 해제
- 정보
- 종료

## 빌드 및 실행

필요 환경:

- Windows
- .NET 8 SDK

빌드:

```powershell
dotnet build .\MemoTag.sln -c Release
```

실행:

```powershell
dotnet run --project .\MemoTag\MemoTag.csproj -c Release
```

빌드된 실행 파일:

```text
MemoTag\bin\Release\net8.0-windows\MemoTag.exe
```

## 설치 파일 만들기

필요 환경:

- Windows
- .NET 8 SDK
- Inno Setup 6

설치 파일 생성:

```powershell
.\build-installer.ps1
```

생성된 설치 파일:

```text
dist\MemoTagSetup-1.0.0.exe
```

설치 중 추가 작업 화면에서 `Windows 시작 시 MemoTag 실행`을 선택하면 현재 사용자 시작 프로그램에 MemoTag가 등록됩니다.
이때 Windows 로그인 후 `MemoTag.exe --startup`으로 실행되며, 앱은 창을 띄우지 않고 트레이에 조용히 표시됩니다.

## 동작 방식

MemoTag는 대상 창을 직접 수정하지 않습니다. 대상 창의 위치와 크기를 주기적으로 읽고, 별도의 overlay 창으로 테두리와 메모를 표시합니다.

전체화면 창에서는 화면 밖으로 밀려나는 테두리를 창 내부로 보정하고, overlay를 topmost로 올려 보이도록 처리합니다.

## 제한 사항

- 메모는 현재 Windows 세션에서만 유지됩니다.
- 앱을 재시작하면 이전 메모를 자동 복원하지 않습니다.
- 관리자 권한으로 실행 중인 앱 위에 표시하려면 MemoTag도 관리자 권한으로 실행해야 할 수 있습니다.
- 일부 독점 전체화면 게임이나 보안 프로그램 위에서는 Windows overlay가 보이지 않을 수 있습니다.

## 정보

- Email: `hsn103@gmail.com`
- GitHub: `https://github.com/hanmarco/MemoTag`
