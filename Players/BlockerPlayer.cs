using TicTacToeAI.Models;

namespace TicTacToeAI.Players;

/// <summary>
/// Player 3: Plays randomly until the opponent is about to win, then blocks.
/// Used in the third stage of training.
/// Just focuses on blocking, not actively trying to win.
/// </summary>
public class BlockerPlayer : ITrainingPlayer
{
    private readonly Random _random = new();

    public string Name => "Blocker Player";
    public string Description => "Plays randomly but blocks the opponent's winning moves";

    public int ChooseMove(GameBoard board, char myPiece, char opponentPiece)
    {
        var available = board.GetAvailableMoves();
        if (available.Count == 0)
            throw new InvalidOperationException("No available moves");

        // Check if opponent has any winning moves and block them
        var opponentWinningMoves = board.GetWinningMoves(opponentPiece);
        if (opponentWinningMoves.Count > 0)
        {
            // Block the first winning move found
            return opponentWinningMoves[0];
        }

        // Otherwise play randomly
        return available[_random.Next(available.Count)];
    }
}

