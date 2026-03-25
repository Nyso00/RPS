using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI; // UI 사용을 위해 추가
using TMPro; // TextMeshPro 사용을 위해 추가

public class NetworkUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private TextMeshProUGUI _statusText; // 상태나 참가 코드를 보여줄 텍스트
    [SerializeField] private TextMeshProUGUI _joinCodeDisplayText; // 방장이 참가 코드를 보여줄 텍스트

    [Header("게임 매니저 UI (접속 후 끌 것들)")]
    [SerializeField] private GameObject _networkUIPanel; // 이 UI들을 묶어둔 부모 패널 (접속 후 숨기기 위함)

    private async void Start()
    {
        // 1. 서버에 로그인하기 전까지는 버튼을 못 누르게 막아둡니다.
        _hostButton.interactable = false;
        _clientButton.interactable = false;
        _statusText.text = "Connecting to Unity Server...";

        // 2. 유니티 익명 로그인 진행
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // 3. 로그인이 완료되면 버튼을 활성화합니다.
        _statusText.text = "Server connection complete! Create a room or enter a code.";
        _hostButton.interactable = true;
        _clientButton.interactable = true;

        // 4. 버튼을 누르면 함수가 실행되도록 연결해 줍니다. (인스펙터에서 OnClick 안 해줘도 됨!)
        _hostButton.onClick.AddListener(StartHostWithRelay);
        _clientButton.onClick.AddListener(() => StartClientWithRelay(_joinCodeInput.text));
    }

    private async void StartHostWithRelay()
    {
        _statusText.text = "Creating room...";
        _hostButton.interactable = false;
        _clientButton.interactable = false;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string myJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            _joinCodeDisplayText.text = $"Join Code\n{myJoinCode}";

            RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            // (선택) 접속이 완료되면 로비 UI 패널을 숨겨버립니다.
            _networkUIPanel.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            _statusText.text = "Failed to create room.";
            Debug.LogError($"Failed to create room: {e}");
            _hostButton.interactable = true;
            _clientButton.interactable = true;
        }
    }

    private async void StartClientWithRelay(string code)
    {
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
            RelayServerData relayServerData = joinAllocation.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            // (선택) 접속이 완료되면 로비 UI 패널을 숨겨버립니다.
            _networkUIPanel.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            _statusText.text = "Failed to connect to room. Please check the code.";
            Debug.LogError($"Failed to connect to room: {e}");
            _hostButton.interactable = true;
            _clientButton.interactable = true;
        }
    }
}