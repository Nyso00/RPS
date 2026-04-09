using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEngine.Rendering;

// -----------------------------------------------------------------------------------------
// 게임의 주요 스테이지 연출과 플레이어 이동을 담당하는 스크립트입니다. GameManager의 상태 변화에 따라 플레이어 위치를 이동시키고 긴장감 연출을 위한 효과를 제어합니다.
// -----------------------------------------------------------------------------------------

public class StageManager : MonoBehaviour
{
    [Header("플레이어 오브젝트")]
    [SerializeField] private Transform _myCharacter;
    [SerializeField] private Transform _enemyCharacter;

    [Header("긴장감 연출")]
    [SerializeField] private Volume _tensionVolume;
    [SerializeField] private float _heartbeatSpeed = 5.0f;
    private Coroutine _heartbeatCoroutine;
    private Coroutine _moveCoroutine;

    private GameManager _gm;
    private bool IsPlayer1 => NetworkManager.Singleton.LocalClientId == _gm.P1ClientId.Value;

    private int CurrentScore => IsPlayer1 ? _gm.GameScore : -_gm.GameScore;

    private void Start()
    {
        _gm = GameManager.Instance;
        _gm.OnStateChanged += UpdateStageState;
        _gm.OnExecuteBlockDestroy += ExecuteBlockDestroy;

        _myCharacter.position = new Vector3(Bridge.Instance.GetBlockX(0, true), _myCharacter.position.y, _myCharacter.position.z);
        _enemyCharacter.position = new Vector3(Bridge.Instance.GetBlockX(0, false), _enemyCharacter.position.y, _enemyCharacter.position.z);
    }

    private void UpdateStageState(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                if (_gm.IsDestroyPhase.Value)
                {
                    Bridge.Instance.BeforeDestroyBlock();
                }

                if (_gm.IsExtraRound())
                {
                    StartHeartbeat();
                }
                break;

            case GameState.Result:
                StopHeartbeat();
                break;

            case GameState.Move:
                MovePlayers();
                break;
        }
    }

    private void ExecuteBlockDestroy()
    {
        Bridge.Instance.DestroyBlock();
    }

    private void MovePlayers()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        int score = CurrentScore;
        Vector3 myStartPos = _myCharacter.position;
        Vector3 enemyStartPos = _enemyCharacter.position;

        float myTargetX = Bridge.Instance.GetBlockX(score, true);
        Vector3 myTargetPos = new(myTargetX, myStartPos.y, myStartPos.z);

        float enemyTargetX = Bridge.Instance.GetBlockX(score, false);
        Vector3 enemyTargetPos = new(enemyTargetX, enemyStartPos.y, enemyStartPos.z);

        float elapsedTime = 0f;
        while (elapsedTime < _gm.MoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _gm.MoveDuration);

            t = t * t * (3f - 2f * t);

            _myCharacter.position = Vector3.Lerp(myStartPos, myTargetPos, t);
            _enemyCharacter.position = Vector3.Lerp(enemyStartPos, enemyTargetPos, t);
            CameraController.Instance.MoveCamera();
            yield return null;
        }
    }

    private void StartHeartbeat()
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
        }

        _tensionVolume.gameObject.SetActive(true);
        _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());
    }

    private void StopHeartbeat()
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        _tensionVolume.weight = 0f;
    }

    private IEnumerator HeartbeatRoutine()
    {
        while (true)
        {
            float pulse = (Mathf.Sin(Time.time * _heartbeatSpeed) + 1f) / 2f;

            _tensionVolume.weight = Mathf.Lerp(0.2f, 1.0f, pulse);

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (_gm == null) return;
        _gm.OnStateChanged -= UpdateStageState;
        _gm.OnExecuteBlockDestroy -= ExecuteBlockDestroy;
    }
}