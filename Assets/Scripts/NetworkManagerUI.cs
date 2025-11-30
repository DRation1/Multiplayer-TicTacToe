using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField]
    private Button startHostButton;
    [SerializeField]
    private Button startClientButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        startHostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            Hide();
        });
        startClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            Hide();
        });

    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
