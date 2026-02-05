using UnityEngine;
using System.Collections;

public enum RPS { None, Rock, Paper, Scissors }

public class GameManager : MonoBehaviour
{
    public PlayerController player1;
    public PlayerController player2;

    public float roundDuration = 3.0f;
    public int maxRounds = 30;

    private int currentRound = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PlayRounds());
    }

    IEnumerator PlayRounds()
    {
        yield return new WaitForSeconds(2.0f); // Initial delay before starting rounds

        while (currentRound < maxRounds)
        {
            currentRound++;

            player1.ResetChoice();
            player2.ResetChoice();

            // make input available

            yield return new WaitForSeconds(roundDuration);

            int winner = FindWinner();

            if (winner == 1)
            {
                player1.Move(1);
                player2.Move(1);
            }
            else if (winner == 2)
            {
                player1.Move(-1);
                player2.Move(-1);
            }

            if (CheckGameOver())
            {
                break;
            }
        }
    }

    private int FindWinner()
    {
        RPS p1Choice = player1.GetChoice();
        RPS p2Choice = player2.GetChoice();

        if (p1Choice == p2Choice)
        {
            return 0;
        }
        else if ((p1Choice == RPS.Rock && p2Choice == RPS.Scissors) ||
                 (p1Choice == RPS.Paper && p2Choice == RPS.Rock) ||
                 (p1Choice == RPS.Scissors && p2Choice == RPS.Paper) ||
                 (p1Choice != RPS.None && p2Choice == RPS.None))
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }

    private bool CheckGameOver()
    {
        if (player1.transform.position.x >= 10.0f)
        {
            Debug.Log("Player 1 Wins!");
            return true;
        }
        else if (player2.transform.position.x <= -10.0f)
        {
            Debug.Log("Player 2 Wins!");
            return true;
        }
        return false;
    }
    
}
