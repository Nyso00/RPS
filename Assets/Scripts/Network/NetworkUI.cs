using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        // 화면 좌측 상단에 버튼들을 그립니다.
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));

        // 아직 아무데도 접속하지 않은 상태일 때만 버튼을 보여줍니다.
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (방장 + 1P)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartHost();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Start Client (손님 접속)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartClient();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Start Server (순수 심판)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartServer();
            }
        }

        GUILayout.EndArea();
    }
}