using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class MainUI : MonoBehaviour
{
    [HideInInspector] public static string JoinCode = ""; // 방 입장 시 사용할 참가 코드

    [SerializeField] private GameObject _modeSelectPanel; // 모드 선택 패널
    [SerializeField] private GameObject _networkPanel; // 네트워크 UI 패널

    [Header("모드 선택 UI")]
    [SerializeField] private Button _localModeButton;
    [SerializeField] private Button _onlineModeButton;


    [Header("네트워크 UI")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private TextMeshProUGUI _statusText;

    private bool _cancelConnection = false;

    private void Start()
    {
        _modeSelectPanel.SetActive(true);
        _networkPanel.SetActive(false);

        _localModeButton.onClick.AddListener(() => SceneManager.LoadScene("LocalScene"));
        _onlineModeButton.onClick.AddListener(OnOnlineModeSelected);
        _backButton.onClick.AddListener(OnBackButtonClicked);

        _hostButton.onClick.AddListener(StartHostWithRelay);
        _clientButton.onClick.AddListener(() => StartClientWithRelay(_joinCodeInput.text));
    }

    private async void OnOnlineModeSelected()
    {
        _cancelConnection = false;
        _modeSelectPanel.SetActive(false);
        _networkPanel.SetActive(true);

        // 1. 서버에 로그인하기 전까지는 버튼 비활성화
        _hostButton.interactable = false;
        _clientButton.interactable = false;
        _backButton.interactable = true;
        _statusText.text = "Connecting to Unity Server...";

        // 2. 유니티 익명 로그인 진행
        try
        {
            // 앱 켜고 최초 1회만 유니티 서비스 초기화 진행
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (_cancelConnection) return;

            // 로그인이 안 되어 있다면 익명 로그인 진행
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (_cancelConnection)
            {
                AuthenticationService.Instance.SignOut();
                return;
            }
        }
        catch (Exception e)
        {
            _statusText.text = "Server connection failed.";
            Debug.LogError($"Server connection failed: {e}");
        }

        // 3. 로그인이 완료시 버튼 활성화
        _statusText.text = "Successfully connected to server.";
        _hostButton.interactable = true;
        _clientButton.interactable = true;
    }

    private void OnBackButtonClicked()
    {
        _cancelConnection = true;
        _networkPanel.SetActive(false);
        _modeSelectPanel.SetActive(true);

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 서비스가 초기화되어 있고, 로그인 상태일 때만 로그아웃 진행
        if (UnityServices.State != ServicesInitializationState.Uninitialized && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Logged out of Unity Server.");
        }
    }

    private async void StartHostWithRelay()
    {
        _statusText.text = "Creating room...";
        _hostButton.interactable = false;
        _clientButton.interactable = false;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            if (_cancelConnection) return;

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (_cancelConnection) return;

            //_joinCodeDisplayText.text = $"Join Code\n{myJoinCode}";

            RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("OnlineScene", LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            if (_cancelConnection) return;
            _statusText.text = "Failed to create room.";
            Debug.LogError($"Failed to create room: {e}");
            _hostButton.interactable = true;
            _clientButton.interactable = true;
        }
    }

    private async void StartClientWithRelay(string code)
    {
        code = code.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            _statusText.text = "Please enter a code.";
            return;
        }

        _statusText.text = "Connecting to room...";
        _hostButton.interactable = false;
        _clientButton.interactable = false;

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            if (_cancelConnection) return;

            RelayServerData relayServerData = joinAllocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            if (_cancelConnection) return;
            _statusText.text = "Please check your code and try again.";
            Debug.LogError($"Failed to connect to room: {e}");
            _hostButton.interactable = true;
            _clientButton.interactable = true;
        }
    }
}
