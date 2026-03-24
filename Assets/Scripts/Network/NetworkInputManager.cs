using System;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkInputManager : Singleton<NetworkInputManager>
{
    private GameControls _controls;
    private PlayerInputSender _mySender;

    protected override void Awake()
    {
        base.Awake();

        _controls = new GameControls();
        _controls.Player1.Rock.performed += ctx => TrySend(RPS.Rock);
        _controls.Player1.Paper.performed += ctx => TrySend(RPS.Paper);
        _controls.Player1.Scissors.performed += ctx => TrySend(RPS.Scissors);
    }

    private void Start()
    {
        NetworkGameManager.Instance.State.OnValueChanged += (oldState, newState) =>
        {
            if (newState == GameState.Playing)
            {
                _controls.Enable();
            }
            else
            {
                _controls.Disable();
            }
        };
    }

    public void SetSender(PlayerInputSender sender)
    {
        _mySender = sender;
    }

    private void TrySend(RPS choice)
    {
        if (NetworkGameManager.Instance.State.Value != GameState.Playing)
        {
            return;
        }
        _mySender.SendChoiceToServer(choice);
    }

    private void OnDestroy()
    {
        _controls?.Dispose();
    }
}
