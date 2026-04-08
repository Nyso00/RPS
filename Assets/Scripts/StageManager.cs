using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System;
using UnityEngine.Rendering;

public class StageManager : MonoBehaviour
{
    [Header("플레이어 오브젝트")]
    [SerializeField] private Transform _myCharacter;
    [SerializeField] private Transform _enemyCharacter;

    [Header("긴장감 연출")]
    [SerializeField] private Volume _tensionVolume;
    [SerializeField] private float _heartbeatSpeed = 5.0f;
    private Coroutine _heartbeatCoroutine;

    private GameManager _gm;
    private bool IsPlayer1 => NetworkManager.Singleton.LocalClientId == _gm.P1ClientId.Value;

    private int CurrentScore => IsPlayer1 ? _gm.GameScore : -_gm.GameScore;

    private void Start()
    {
        _gm = GameManager.Instance;
        _gm.OnStateChanged += UpdateStageState;

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
                if (_gm.IsDestroyPhase.Value && Mathf.Abs(CurrentScore) <= _gm.ScoreToWin.Value)
                {
                    Bridge.Instance.DestroyBlock();
                }
                break;
        }
    }

    private void MovePlayers()
    {
        StartCoroutine(MoveCoroutine());
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
        _gm.OnStateChanged -= UpdateStageState;
    }
}