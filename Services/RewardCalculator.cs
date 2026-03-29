using TicTacToeAI.Models;

namespace TicTacToeAI.Services;

/// <summary>
/// Calculates rewards for agent actions based on the reward system rules.
/// Reward System:
///   +1.0  for completing a line (winning)
///   -1.0  for allowing opponent to win (not blocking)
///   +0.5  for playing a position that can lead to a win next move
///   -0.5  for playing a position that cannot lead to a win next move
///   -0.25  for playing along a line occupied by opponent
/// </summary>
public class RewardCalculator
{
    /// <summary>
    /// Calculate the reward for an agent action.
    /// </summary>
    /// <param name="boardBefore">Board state before the agent's move</param>
    /// <param name="action">Position where agent placed its piece</param>
    /// <param name="boardAfter">Board state after the agent's move</param>
    /// <param name="agentPiece">The piece the agent is playing ('X' or 'O')</param>
    /// <param name="opponentPiece">The piece the opponent is playing</param>
    public double CalculateReward(GameBoard boardBefore, int action, GameBoard boardAfter,
        char agentPiece = 'X', char opponentPiece = 'O')
    {
        // Rule 1: +1.0 for completing a line (winning)
        if (boardAfter.HasPlayerWon(agentPiece))
        {
            return 1.0;
        }

        double reward = 0.0;

        // Rule 2: Check if opponent had a winning move that agent didn't block
        var opponentWinningMoves = boardBefore.GetWinningMoves(opponentPiece);
        if (opponentWinningMoves.Count > 0 && !opponentWinningMoves.Contains(action))
        {
            // Agent didn't block opponent's winning move
            reward += -1.0;
            return reward; // This is a critical mistake, return immediately
        }

        // Rule 3: +0.5 for playing a position that can lead to a win next move
        if (boardBefore.MoveCreatesWinThreat(action, agentPiece))
        {
            reward += 0.5;
        }

        // Rule 4: -0.5 for playing a position that cannot lead to a win next move
        if (!boardBefore.MoveContributesToWinnableLine(action, agentPiece))
        {
            reward += -0.5;
        }

        // Rule 5: -0.25 for playing along a line that contains opponent pieces
        if (boardBefore.MoveIsOnOpponentLine(action, agentPiece))
        {
            reward += -0.25;
        }

        return reward;
    }

    /// <summary>
    /// Calculate reward when the game ends (for the final state).
    /// </summary>
    /// <param name="winner">The winner character ('X', 'O', or 'D')</param>
    /// <param name="agentPiece">The piece the agent is playing</param>
    public double CalculateEndGameReward(char winner, char agentPiece = 'X')
    {
        if (winner == agentPiece)
            return 1.0;   // Agent won
        if (winner == 'D')
            return 0.5;   // Draw — positive signal so agent learns to pursue draws
        return -1.0;      // Agent lost
    }
}

