using Unity.Netcode;
using UnityEngine;

public class PlayerInputSender : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // 내 컴퓨터(로컬)에 생성된 우체부일 경우에만 입력 매니저와 연결
        if (IsOwner && NetworkInputManager.Instance != null)
        {
            NetworkInputManager.Instance.SetSender(this);
        }
    }

    public void SendChoiceToServer(RPS choice)
    {
        SubmitChoiceServerRpc(choice);
    }

    [ServerRpc]
    private void SubmitChoiceServerRpc(RPS choice, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        NetworkGameManager.Instance.SetPlayerChoice(senderId, choice);
    }
}