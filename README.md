# 🎮 Unity Netcode Tic-Tac-Toe (멀티플레이 틱택토)

**Unity Netcode for GameObjects를 활용하여 개발한 1vs1 실시간 멀티플레이 틱택토 게임입니다.**

### 📄 Project Overview (프로젝트 개요)

이 프로젝트는 Unity의 최신 네트워킹 스택인 **Netcode for GameObjects(NGO)**를 사용하여 구현된 2인용 보드게임입니다. Host-Client 구조를 통해 두 명의 플레이어가 실시간으로 접속하여 턴제(Turn-based) 게임을 즐길 수 있습니다. 서버 권한(Server Authoritative) 로직을 통해 게임의 상태를 동기화하고, 승패 판정 및 점수 시스템을 관리합니다.

* **개발 기간**: 2024.12 ~ 2025.02
* **개발 인원**: 1명 (개인 프로젝트)
* **주요 기능**: 실시간 매칭(Host/Client), 턴 동기화, 승리 라인 시각화, 점수 기록, 재경기 기능

### 🛠 Tech Stack (기술 스택)

| Category | Stack |
| :--- | :--- |
| **Engine** | Unity 6 (6000.1.12f1) |
| **Language** | C# |
| **Network** | Unity Netcode for GameObjects (NGO) |
| **UI** | TextMeshPro (UGUI) |
| **Testing** | NUnit (Editor Test) |

### ✨ Key Features (핵심 기능)

#### 1. 🌐 실시간 멀티플레이 환경 구축
* **기능**: `NetworkManagerUI`를 통해 Host(서버+클라이언트)와 Client 모드를 선택하여 접속 가능합니다.
* **구현**: `NetworkManager.Singleton`을 활용하여 연결을 관리하며, 접속 시 `NetworkManager_OnClientConnectedCallback`을 통해 게임 시작 조건을 확인합니다(2인 접속 시 게임 시작).

#### 2. ⚔️ 턴제 로직 및 동기화 (Server Authoritative)
* **기능**: 플레이어(Cross/Circle) 간의 턴을 관리하고, 올바르지 않은 턴에서의 입력을 차단합니다.
* **구현**:
    * `NetworkVariable<PlayerType>`을 사용하여 현재 턴인 플레이어를 모든 클라이언트에 동기화합니다.
    * `ClickedOnGridPositionRpc` (Server RPC)를 통해 클라이언트의 입력을 서버에서 검증하고 처리합니다.

#### 3. 🏆 승리 판정 및 시각화
* **기능**: 가로, 세로, 대각선 방향의 승리 조건을 실시간으로 체크하고 승리 시 라인을 그려줍니다.
* **구현**:
    * `GameManager`에서 3x3 그리드 데이터를 관리하며 매 턴마다 승리 알고리즘을 수행합니다.
    * 승리 확정 시 `GameVisualManager`가 승리한 라인의 좌표와 방향(Horizontal, Vertical, Diagonal)을 계산하여 시각적인 선(Line Prefab)을 생성합니다.

#### 4. 📊 점수 및 UI 시스템
* **기능**: 각 플레이어의 점수를 추적하고, 현재 누구의 턴인지 화살표로 직관적으로 보여줍니다.
* **구현**:
    * `NetworkVariable`로 Cross/Circle 플레이어의 점수를 관리하여 실시간 동기화합니다.
    * `PlayerUI`와 `GameOverUI`는 이벤트를 구독(Observer Pattern)하여 점수 변경, 승리/패배/무승부 시 즉각적으로 UI를 업데이트합니다.

#### 5. 🔄 재경기 (Rematch) 시스템
* **기능**: 게임 종료 후 재경기 버튼을 통해 점수는 유지한 채 보드만 초기화하여 게임을 이어갈 수 있습니다.
* **구현**: `RematchRpc`를 통해 서버가 보드 상태를 초기화하고 모든 클라이언트의 비주얼 오브젝트(X, O 마크)를 삭제하도록 지시합니다.

### 📂 File Structure (주요 스크립트 구조)

* `GameManager.cs`: 게임의 핵심 로직(턴 관리, 승리 판정, RPC 통신)을 담당하는 싱글톤 네트워크 동작 스크립트.
* `GameVisualManager.cs`: 네트워크 상에서 X, O 마크 생성 및 승리 선(Line) 처리를 담당.
* `GridPosition.cs`: 각 그리드 칸의 클릭 입력을 감지하여 매니저에게 전달.
* `NetworkManagerUI.cs`: Host/Client 시작 버튼 등 네트워크 연결 진입점 처리.
* `SoundManager.cs`: 게임 상황(착수, 승리, 패배)에 따른 효과음 재생.

