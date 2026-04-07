using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerInputSender : NetworkBehaviour
{
    public void SendChoiceToServer(RPS choice, int playerNum)
    {
        SubmitChoiceServerRpc(choice, playerNum);
    }

    public void SendRestartRequestToServer()
    {
        RequestRestartServerRpc();
    }

    [ServerRpc]
    private void SubmitChoiceServerRpc(RPS choice, int playerNum, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameManager.Instance.SetPlayerChoice(senderId, choice, playerNum);
    }

    [ServerRpc]
    private void RequestRestartServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameManager.Instance.RequestRestartFromClient(senderId);
    }

}