using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;

    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    private List<GameObject> visualGameObjectList;

    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;

    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        foreach (GameObject visualGameObject in visualGameObjectList)
        {
            Destroy(visualGameObject);
        }
        visualGameObjectList.Clear();
    }
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if(!NetworkManager.Singleton.IsServer)
        {
            return;
        }
        float eulerZ = 0f;
        switch(e.line.orientation)
        {
            default:
            case GameManager.Orientation.Horizontal:
                eulerZ = 0f; break;
            case GameManager.Orientation.Vertical:
                eulerZ = 90f; break;
            case GameManager.Orientation.DiagonalA:
                eulerZ = 45f; break;
            case GameManager.Orientation.DiagonalB:
                eulerZ = -45f; break;
        }
        Transform lineCompleteTransform = Instantiate(lineCompletePrefab, GetGridWorldPosition(e.line.centerGridPositon.x, e.line.centerGridPositon.y), Quaternion.Euler(0, 0, eulerZ));
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }
    private void GameManager_OnClickedGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        Debug.Log("GameManager_OnClickedGridPosition");
        SpawnObjectRpc(e.x, e.y, e.playerType);
    }


    [Rpc(SendTo.Server)]
    //rpc이기때문에 만약 GameManager에서 localPlayerType을 가져오려고 하면
    //언제나 서버의 타입(X)를 가져오게됨
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    {
        Debug.Log("Spawn Object");
        Transform prefab;
        switch(playerType)
        {
            default:
            case GameManager.PlayerType.Cross:
                prefab = crossPrefab;
                break;
            case GameManager.PlayerType.Circle:
                prefab = circlePrefab;
                break;
        }

        Transform spawnedCrossTransform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
        spawnedCrossTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(spawnedCrossTransform.gameObject);
    }

    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
