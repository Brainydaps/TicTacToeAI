using TicTacToeAI.Models;

namespace TicTacToeAI.Players;

/// <summary>
/// Player 4: Tries to win via triangle corner strategy.
/// Plays in corners to create two simultaneous winning threats
/// that are impossible for the opponent to block both.
/// Falls back to winning moves, then blocking, then corners, then random.
/// Used in the fourth stage of training.
/// </summary>
public class TrianglePlayer : ITrainingPlayer
{
    private readonly Random _random = new();

    // Pairs of corners that share a winning line through center or edge
    // Triangle strategy: occupy two corners such that placing a third creates two win threats
    private static readonly int[][] CornerPairs =
    [
        [0, 8], // diagonal
        [2, 6], // anti-diagonal
        [0, 2], // top row corners
        [6, 8], // bottom row corners
        [0, 6], // left column corners
        [2, 8]  // right column corners
    ];

    public string Name => "Triangle Player";
    public string Description => "Uses triangle corner strategy to create unblockable dual threats";

    public int ChooseMove(GameBoard board, char myPiece, char opponentPiece)
    {
        var available = board.GetAvailableMoves();
        if (available.Count == 0)
            throw new InvalidOperationException("No available moves");

        // First: if we can win, win immediately
        var winningMoves = board.GetWinningMoves(myPiece);
        if (winningMoves.Count > 0)
            return winningMoves[0];

        // Second: block opponent if they can win
        var opponentWinningMoves = board.GetWinningMoves(opponentPiece);
        if (opponentWinningMoves.Count > 0)
            return opponentWinningMoves[0];

        // Third: try to create a triangle (fork) - play a corner that creates two threats
        var forkMove = FindForkMove(board, available, myPiece);
        if (forkMove.HasValue)
            return forkMove.Value;

        // Fourth: try to set up for a triangle by taking strategic corners
        var setupMove = FindTriangleSetupMove(board, available, myPiece);
        if (setupMove.HasValue)
            return setupMove.Value;

        // Fifth: take any corner
        var availableCorners = GameBoard.Corners.Where(c => available.Contains(c)).ToList();
        if (availableCorners.Count > 0)
            return availableCorners[_random.Next(availableCorners.Count)];

        // Fallback: random
        return available[_random.Next(available.Count)];
    }

    /// <summary>
    /// Find a move that creates a fork (two winning threats at once).
    /// </summary>
    private int? FindForkMove(GameBoard board, List<int> available, char myPiece)
    {
        foreach (int move in available)
        {
            var clone = board.Clone();
            clone.MakeMove(move, myPiece);

            // Count how many winning moves we would have after this move
            var winningMovesAfter = clone.GetWinningMoves(myPiece);
            if (winningMovesAfter.Count >= 2)
            {
                return move;
            }
        }
        return null;
    }

    /// <summary>
    /// Find a corner move that sets up a triangle pattern.
    /// Look for a corner where we already have the opposite corner or an adjacent corner.
    /// </summary>
    private int? FindTriangleSetupMove(GameBoard board, List<int> available, char myPiece)
    {
        var myCorners = GameBoard.Corners.Where(c => board.Cells[c] == myPiece).ToList();

        if (myCorners.Count == 0)
        {
            // Take any available corner to start setup
            var freeCorners = GameBoard.Corners.Where(c => available.Contains(c)).ToList();
            if (freeCorners.Count > 0)
                return freeCorners[_random.Next(freeCorners.Count)];
        }
        else
        {
            // Find a corner that, combined with existing corners, creates the best setup
            foreach (var pair in CornerPairs)
            {
                int c1 = pair[0], c2 = pair[1];
                if (myCorners.Contains(c1) && available.Contains(c2))
                {
                    // Check that the line between them isn't blocked by opponent
                    var clone = board.Clone();
                    clone.MakeMove(c2, myPiece);
                    if (clone.GetWinningMoves(myPiece).Count >= 1)
                        return c2;
                    // Even without immediate win, take it for triangle setup
                    return c2;
                }
                if (myCorners.Contains(c2) && available.Contains(c1))
                {
                    var clone = board.Clone();
                    clone.MakeMove(c1, myPiece);
                    if (clone.GetWinningMoves(myPiece).Count >= 1)
                        return c1;
                    return c1;
                }
            }
        }

        return null;
    }
}

