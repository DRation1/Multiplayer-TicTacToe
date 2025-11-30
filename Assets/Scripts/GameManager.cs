using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayableTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;

    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }

    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPositon;
        public Orientation orientation;

    }

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType =
        new NetworkVariable<PlayerType>(); //Network Variable은 즉시 초기화가 필요함
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;

    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one Gamemanager instance");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line>
        {
            //Horizontal
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(1,0), new Vector2Int(2,0), },
                centerGridPositon = new Vector2Int(1, 0),
                orientation = Orientation.Horizontal,

            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 1), new Vector2Int(1,1), new Vector2Int(2,1), },
                centerGridPositon = new Vector2Int(1, 1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 2), new Vector2Int(1,2), new Vector2Int(2,2), },
                centerGridPositon = new Vector2Int(1, 2),
                orientation = Orientation.Horizontal,
            },

            //Vertical
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(0,1), new Vector2Int(0,2), },
                centerGridPositon = new Vector2Int(0, 1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(1, 0), new Vector2Int(1,1), new Vector2Int(1,2), },
                centerGridPositon = new Vector2Int(1, 1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(2, 0), new Vector2Int(2,1), new Vector2Int(2,2), },
                centerGridPositon = new Vector2Int(2, 1),
                orientation = Orientation.Vertical,
            },

            //Diagonal
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 0), new Vector2Int(1,1), new Vector2Int(2,2), },
                centerGridPositon = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalA,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0, 2), new Vector2Int(1,1), new Vector2Int(2,0), },
                centerGridPositon = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalB,
            },
        };
    }

    public override void OnNetworkSpawn() //이거의 실행조건 알아보기
    {
        Debug.Log("On Network Spawn: " + NetworkManager.Singleton.LocalClientId);
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayableTypeChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            //Start Game
            currentPlayablePlayerType.Value = PlayerType.Cross; //cross 선
            TriggerOnGameStartedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);

    }


    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)  //서버에서 실행된
    {
        Debug.Log("Clicked On Grid Position " + x + ", " + y);
        Debug.Log("Local: " + localPlayerType);
        Debug.Log("Current Playable type" + currentPlayablePlayerType);
        if (playerType != currentPlayablePlayerType.Value)
        {
            //서버에서 턴 여부 검사. 타입이 플레이어블 타입이 아니면 거부
            return;
        }

        if (playerTypeArray[x, y] != PlayerType.None)
        {
            //확인용 구문, 배치하려는 위치가 차있는 상태
            return;
        }

        playerTypeArray[x, y] = playerType; //배치
        TriggerOnPlacedObjectRpc();

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        });

        switch (currentPlayablePlayerType.Value) //턴 전환. 위에서 배치했으니 턴 넘겨줌
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }

        TestWinner(); //이런건 서버에서만 실행되어야 함. 이미 해당 메소드 자체가 rpc이므로 문제 X
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }

    public PlayerType GetCurrentcurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }

    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
            );
    }
    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;
    }
    private void TestWinner()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];
            if (TestWinnerLine(line))
            {
                //Win
                Debug.Log("Winner!");
                currentPlayablePlayerType.Value = PlayerType.None;
                PlayerType winPlayerType = playerTypeArray[line.centerGridPositon.x, line.centerGridPositon.y];
                switch(winPlayerType)
                {
                    case PlayerType.Cross:
                        playerCrossScore.Value++;
                        break;
                    case PlayerType.Circle:
                        playerCircleScore.Value++;
                        break;
                }
                TriggerOnGameWinRpc(i, winPlayerType);
                return;
            }
        }

        //무승부 있음?
        bool hasTie = true;
        for(int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for(int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x,y] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }
        }

        if(hasTie)
        {
            TriggerOnGameTiedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType,
        });
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x,y] = PlayerType.None;
            }
        }
        currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}
