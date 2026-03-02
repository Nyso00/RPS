using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    public Transform player1, player2;

    public void MoveCamera()
    {
        transform.position = new Vector3((player1.position.x + player2.position.x) / 2, transform.position.y, transform.position.z);
    }
}
