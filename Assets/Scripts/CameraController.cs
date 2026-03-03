using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] private Transform player1, player2;

    /// <summary>
    /// 플레이어들의 위치를 기반으로 카메라의 위치를 중앙으로 이동시키는 함수입니다.
    /// </summary>
    public void MoveCamera()
    {
        transform.position = new Vector3((player1.position.x + player2.position.x) / 2, transform.position.y, transform.position.z);
    }
}
