using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [Header("플레이어")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private Image timerGauge;
    [SerializeField] private Image player1ChoiceImage;
    [SerializeField] private Image player2ChoiceImage;

    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Sprite paperSprite;
    [SerializeField] private Sprite scissorsSprite;


    [Header("시간 변수")]
    [SerializeField] private float startDelay = 1.0f;
    [SerializeField] private float roundDuration = 3.0f;
    [SerializeField] private float revealToMoveDelay = 0.5f;
    [SerializeField] private float moveToNextRoundDelay = 0.5f;
    [SerializeField] private float extraRoundStartDelay = 1.0f;
    [SerializeField] private float extraRoundDuration = 5.0f;
    [SerializeField] private float extraRoundDurationDecrement = 0.9f;

    [Header("라운드 변수")]
    [SerializeField] private int maxRounds = 30;
    [SerializeField] private int maxExtraRounds = 10;
    [SerializeField] private int[] blockDestroyRounds = new int[4];

    private int currentRound = 0;
    private int currentDestroyPhase = 0;
    private enum RoundResult { Draw, Player1Win, Player2Win }

    private void Start()
    {
        StartCoroutine(PlayRounds());
    }

    private IEnumerator PlayRounds()
    {
        yield return new WaitForSeconds(startDelay);

        while (currentRound < maxRounds + maxExtraRounds)
        {
            currentRound++;

            // 1. 라운드 준비: 라운드 시간, UI 설정, 블록 파괴 준비
            yield return StartCoroutine(SetupRoundRoutine());

            // 2. 타이머 시작: 플레이어 입력 허용, 게이지 UI
            yield return StartCoroutine(TimerRoutine());

            // 3. 라운드 결과 판정: 패 공개, 플레이어 이동
            yield return StartCoroutine(EvaluateRoutine());

            // 4. 1차 게임 오버 체크
            if (CheckGameOver())
            {
                HideChoiceImages();
                yield break;
            }

            // 5. 블록 파괴 후 2차 게임 오버 체크
            if (HandleBlockDestruction())
            {
                HideChoiceImages();
                yield break;
            }

            yield return new WaitForSeconds(moveToNextRoundDelay);
            HideChoiceImages();
        }

        roundText.text = "Draw!";
    }

    private IEnumerator SetupRoundRoutine()
    {
        timerGauge.fillAmount = 0f;
        player1.ResetChoice();
        player2.ResetChoice();

        if (currentRound == maxRounds + 1)
        {
            roundText.text = "Ready for Extra Round...";
            roundDuration = extraRoundDuration;
            yield return new WaitForSeconds(extraRoundStartDelay);
            roundText.text = "Extra Round!";
        }
        else if (currentRound > maxRounds)
        {
            roundDuration *= extraRoundDurationDecrement;
        }
        else
        {
            roundText.text = $"Round {currentRound}";
        }

        if (ShouldDestroyBlock())
        {
            Bridge.Instance.BeforeDestroyBlock();
        }
    }

    private IEnumerator TimerRoutine()
    {
        InputManager.Instance.SetInputAvailable(true);

        float passedTime = 0f;
        while (passedTime < roundDuration)
        {
            passedTime += Time.deltaTime;

            timerGauge.fillAmount = passedTime / roundDuration;

            yield return null;
        }
        timerGauge.fillAmount = 1f;

        InputManager.Instance.SetInputAvailable(false);
    }

    private IEnumerator EvaluateRoutine()
    {
        RoundResult winner = FindWinner();

        yield return new WaitForSeconds(revealToMoveDelay);

        if (winner == RoundResult.Player1Win)
        {
            player1.Move(1);
            player2.Move(1);
        }
        else if (winner == RoundResult.Player2Win)
        {
            player1.Move(-1);
            player2.Move(-1);
        }

        CameraController.Instance.MoveCamera();
    }

    private bool ShouldDestroyBlock()
    {
        return currentDestroyPhase < blockDestroyRounds.Length && currentRound == blockDestroyRounds[currentDestroyPhase];
    }

    private bool HandleBlockDestruction()
    {
        if (ShouldDestroyBlock())
        {
            Bridge.Instance.DestroyBlock();
            currentDestroyPhase++;

            return CheckGameOver();
        }
        return false;
    }

    private void HideChoiceImages()
    {
        player1ChoiceImage.gameObject.SetActive(false);
        player2ChoiceImage.gameObject.SetActive(false);
    }

    private RoundResult FindWinner()
    {
        RPS p1Choice = player1.GetChoice();
        RPS p2Choice = player2.GetChoice();

        if (p1Choice != RPS.None)
        {
            player1ChoiceImage.sprite = GetChoiceSprite(p1Choice);
            player1ChoiceImage.gameObject.SetActive(true);
        }
        if (p2Choice != RPS.None)
        {
            player2ChoiceImage.sprite = GetChoiceSprite(p2Choice);
            player2ChoiceImage.gameObject.SetActive(true);
        }

        if (p1Choice == p2Choice)
        {
            Debug.Log($"It's a tie! Both players chose {p1Choice}");
            return RoundResult.Draw;
        }
        else if ((p1Choice == RPS.Rock && p2Choice == RPS.Scissors) ||
                 (p1Choice == RPS.Paper && p2Choice == RPS.Rock) ||
                 (p1Choice == RPS.Scissors && p2Choice == RPS.Paper) ||
                 (p1Choice != RPS.None && p2Choice == RPS.None))
        {
            Debug.Log($"Player 1 wins the round! ({p1Choice} beats {p2Choice})");
            return RoundResult.Player1Win;
        }
        else
        {
            Debug.Log($"Player 2 wins the round! ({p2Choice} beats {p1Choice})");
            return RoundResult.Player2Win;
        }
    }

    private bool CheckGameOver()
    {
        if (player1.HasLost())
        {
            Debug.Log("Player 2 Wins!");
            roundText.text = "Player 2 Wins!";
            return true;
        }
        else if (player2.HasLost())
        {
            Debug.Log("Player 1 Wins!");
            roundText.text = "Player 1 Wins!";
            return true;
        }
        return false;
    }

    private Sprite GetChoiceSprite(RPS choice)
    {
        return choice switch
        {
            RPS.Rock => rockSprite,
            RPS.Paper => paperSprite,
            RPS.Scissors => scissorsSprite,
            _ => null,
        };
    }
}
