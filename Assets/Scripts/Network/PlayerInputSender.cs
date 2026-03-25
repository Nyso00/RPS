using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerInputSender : NetworkBehaviour
{
    // public override void OnNetworkSpawn()
    // {
    //     if (!IsOwner) return;

    //     // 1. 만약 이미 매니저가 있다면 (손님이 늦게 접속했을 때) 바로 연결
    //     if (SceneManager.GetActiveScene().name == "OnlineScene")
    //     {
    //         NetworkInputManager.Instance.SetSender(this);
    //     }
    //     else
    //     {
    //         // 2. 매니저가 아직 없다면 (방장이 MainScene에서 생성됐을 때), 
    //         // 씬 이동이 끝날 때 알려달라고 '예약(구독)'을 걸어둡니다.
    //         SceneManager.sceneLoaded += OnSceneLoaded;
    //     }
    // }

    // private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    // {
    //     if (scene.name == "OnlineScene")
    //     {
    //         NetworkInputManager.Instance.SetSender(this);
            
    //         // 볼일 끝났으니 구독 취소 (메모리 누수 방지)
    //         SceneManager.sceneLoaded -= OnSceneLoaded; 
    //     }
    // }

    public void SendChoiceToServer(RPS choice)
    {
        SubmitChoiceServerRpc(choice);
    }

    public void SendRestartRequestToServer()
    {
        RequestRestartServerRpc();
    }

    [ServerRpc]
    private void SubmitChoiceServerRpc(RPS choice, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        NetworkGameManager.Instance.SetPlayerChoice(senderId, choice);
    }

    [ServerRpc]
    private void RequestRestartServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        NetworkGameManager.Instance.RequestRestartFromClient(senderId);
    }

}