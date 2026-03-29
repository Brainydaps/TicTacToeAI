using TicTacToeAI.Models;

namespace TicTacToeAI.Players;

/// <summary>
/// Player 2: Favors center first, then corners, then random.
/// Used in the second stage of training (iterations 2001-4000).
/// </summary>
public class CenterCornerPlayer : ITrainingPlayer
{
    private readonly Random _random = new();

    public string Name => "Center & Corner Player";
    public string Description => "Favors center, then corners, then plays randomly";

    public int ChooseMove(GameBoard board, char myPiece, char opponentPiece)
    {
        var available = board.GetAvailableMoves();
        if (available.Count == 0)
            throw new InvalidOperationException("No available moves");

        // Prefer center
        if (available.Contains(GameBoard.Center))
            return GameBoard.Center;

        // Prefer corners
        var availableCorners = GameBoard.Corners.Where(c => available.Contains(c)).ToList();
        if (availableCorners.Count > 0)
            return availableCorners[_random.Next(availableCorners.Count)];

        // Otherwise random
        return available[_random.Next(available.Count)];
    }
}

