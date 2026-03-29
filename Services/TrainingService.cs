using System.Diagnostics;
using TicTacToeAI.Models;
using TicTacToeAI.Players;

namespace TicTacToeAI.Services;

/// <summary>
/// Training mode: which side(s) the agent trains as.
/// </summary>
public enum TrainingMode
{
    AsX,   // Agent plays X (first mover) — original behavior
    AsO,   // Agent plays O (second mover)
    Both   // Agent trains as both X and O
}

/// <summary>
/// Training progress data for UI updates.
/// </summary>
public class TrainingProgress
{
    public string PlayerName { get; set; } = "";
    public int CurrentIteration { get; set; }
    public int TotalIterations { get; set; }
    public int PlayerIndex { get; set; } // 1-4
    public int TotalPlayers { get; set; } = 4;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public string TrainingModeLabel { get; set; } = "as X";
    public char AgentPiece { get; set; } = 'X';
    /// <summary>Result of the last game: 'X', 'O', or 'D'. '\0' if not a per-game update.</summary>
    public char LastGameResult { get; set; }
    /// <summary>True when a new training phase (opponent) has just started.</summary>
    public bool IsNewPhase { get; set; }
    public double ProgressPercent => TotalIterations > 0 ? (double)CurrentIteration / TotalIterations * 100 : 0;
    public string StatusMessage => $"Training vs {PlayerName} ({TrainingModeLabel}): Game {CurrentIteration}/{TotalIterations} (W:{Wins} L:{Losses} D:{Draws})";
}

/// <summary>
/// Orchestrates agent training against the 4 heuristic players.
/// Supports training the agent as X (first mover), O (second mover), or both.
/// Q-values are updated after EVERY agent action using Bellman's equation.
/// </summary>
public class TrainingService
{
    private readonly RewardCalculator _rewardCalculator;
    private readonly QTableGenerator _qTableGenerator;

    public TrainingService(RewardCalculator rewardCalculator, QTableGenerator qTableGenerator)
    {
        _rewardCalculator = rewardCalculator;
        _qTableGenerator = qTableGenerator;
    }

    /// <summary>
    /// Train an agent against all 4 players sequentially.
    /// </summary>
    public async Task TrainAgentAsync(
        QLearningAgent agent,
        int iterationsPerPlayer,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default,
        TrainingMode mode = TrainingMode.AsX)
    {
        var players = new ITrainingPlayer[]
        {
            new RandomPlayer(),
            new CenterCornerPlayer(),
            new BlockerPlayer(),
            new TrianglePlayer()
        };

        if (mode == TrainingMode.Both)
        {
            // Train as X first, then as O — each uses its own Q-table
            int totalPlayers = players.Length * 2;

            // Phase 1: Train as X
            agent.SetActiveSide('X');
            if (agent.QTableX.Count == 0)
            {
                Debug.WriteLine("Generating Q-table for X...");
                agent.QTableX = await Task.Run(() => _qTableGenerator.GenerateQTable('X'), cancellationToken);
                agent.SetActiveSide('X'); // re-point after replacing dict
                Debug.WriteLine($"X Q-table generated with {agent.QTableX.Count} states");
            }

            for (int playerIdx = 0; playerIdx < players.Length; playerIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var player = players[playerIdx];

                double startEpsilon = 0.3 - (playerIdx * 0.05);
                double endEpsilon = 0.05;

                await TrainAgainstPlayer(agent, player, playerIdx + 1, totalPlayers,
                    iterationsPerPlayer, startEpsilon, endEpsilon, 'X', 'O', "as X",
                    progress, cancellationToken);
            }

            await Task.Run(() => agent.SaveQTable('X'), cancellationToken);

            // Phase 2: Train as O
            agent.SetActiveSide('O');
            if (agent.QTableO.Count == 0)
            {
                Debug.WriteLine("Generating Q-table for O...");
                agent.QTableO = await Task.Run(() => _qTableGenerator.GenerateQTable('O'), cancellationToken);
                agent.SetActiveSide('O'); // re-point after replacing dict
                Debug.WriteLine($"O Q-table generated with {agent.QTableO.Count} states");
            }

            for (int playerIdx = 0; playerIdx < players.Length; playerIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var player = players[playerIdx];

                double startEpsilon = 0.3 - (playerIdx * 0.05);
                double endEpsilon = 0.05;

                await TrainAgainstPlayer(agent, player, players.Length + playerIdx + 1, totalPlayers,
                    iterationsPerPlayer, startEpsilon, endEpsilon, 'O', 'X', "as O",
                    progress, cancellationToken);
            }

            await Task.Run(() => agent.SaveQTable('O'), cancellationToken);
        }
        else
        {
            char agentPiece = mode == TrainingMode.AsX ? 'X' : 'O';
            char opponentPiece = mode == TrainingMode.AsX ? 'O' : 'X';
            string modeLabel = mode == TrainingMode.AsX ? "as X" : "as O";

            agent.SetActiveSide(agentPiece);

            // Generate Q-table for the specific side if empty
            var currentQTable = agentPiece == 'X' ? agent.QTableX : agent.QTableO;
            if (currentQTable.Count == 0)
            {
                Debug.WriteLine($"Generating Q-table for {agentPiece}...");
                var generated = await Task.Run(() => _qTableGenerator.GenerateQTable(agentPiece), cancellationToken);
                if (agentPiece == 'X')
                    agent.QTableX = generated;
                else
                    agent.QTableO = generated;
                agent.SetActiveSide(agentPiece); // re-point after replacing dict
                Debug.WriteLine($"Q-table generated with {generated.Count} states");
            }

            for (int playerIdx = 0; playerIdx < players.Length; playerIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var player = players[playerIdx];
                double startEpsilon = 0.3 - (playerIdx * 0.05);
                double endEpsilon = 0.05;

                await TrainAgainstPlayer(agent, player, playerIdx + 1, players.Length,
                    iterationsPerPlayer, startEpsilon, endEpsilon, agentPiece, opponentPiece, modeLabel,
                    progress, cancellationToken);
            }

            await Task.Run(() => agent.SaveQTable(agentPiece), cancellationToken);
        }

        Debug.WriteLine("Training complete! Q-table(s) saved.");
    }

    /// <summary>
    /// Train an agent against a specific player for a given number of iterations.
    /// </summary>
    public async Task TrainAgainstSinglePlayer(
        QLearningAgent agent,
        ITrainingPlayer player,
        int playerIndex,
        int iterations,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default,
        TrainingMode mode = TrainingMode.AsX)
    {
        char agentPiece = mode == TrainingMode.AsX ? 'X' : 'O';
        char opponentPiece = mode == TrainingMode.AsX ? 'O' : 'X';
        string modeLabel = mode == TrainingMode.AsX ? "as X" : "as O";

        agent.SetActiveSide(agentPiece);
        var currentQTable = agentPiece == 'X' ? agent.QTableX : agent.QTableO;
        if (currentQTable.Count == 0)
        {
            var generated = await Task.Run(() => _qTableGenerator.GenerateQTable(agentPiece), cancellationToken);
            if (agentPiece == 'X')
                agent.QTableX = generated;
            else
                agent.QTableO = generated;
            agent.SetActiveSide(agentPiece);
        }

        await TrainAgainstPlayer(agent, player, playerIndex, 1, iterations, 0.2, 0.05,
            agentPiece, opponentPiece, modeLabel, progress, cancellationToken);
        await Task.Run(() => agent.SaveQTable(agentPiece), cancellationToken);
    }

    private async Task TrainAgainstPlayer(
        QLearningAgent agent,
        ITrainingPlayer player,
        int playerIndex,
        int totalPlayers,
        int iterations,
        double startEpsilon,
        double endEpsilon,
        char agentPiece,
        char opponentPiece,
        string modeLabel,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken)
    {
        int wins = 0, losses = 0, draws = 0;

        // Report new phase
        progress.Report(new TrainingProgress
        {
            PlayerName = player.Name,
            CurrentIteration = 0,
            TotalIterations = iterations,
            PlayerIndex = playerIndex,
            TotalPlayers = totalPlayers,
            Wins = 0,
            Losses = 0,
            Draws = 0,
            TrainingModeLabel = modeLabel,
            AgentPiece = agentPiece,
            IsNewPhase = true,
            LastGameResult = '\0'
        });

        await Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Decay epsilon linearly
                double epsilon = startEpsilon - (startEpsilon - endEpsilon) * ((double)i / iterations);
                agent.SetEpsilon(epsilon);

                var result = PlayTrainingGame(agent, player, agentPiece, opponentPiece);

                if (result == agentPiece) wins++;
                else if (result == opponentPiece) losses++;
                else if (result == 'D') draws++;

                // Report progress every 10 games or at the end
                if (i % 10 == 0 || i == iterations - 1)
                {
                    progress.Report(new TrainingProgress
                    {
                        PlayerName = player.Name,
                        CurrentIteration = i + 1,
                        TotalIterations = iterations,
                        PlayerIndex = playerIndex,
                        TotalPlayers = totalPlayers,
                        Wins = wins,
                        Losses = losses,
                        Draws = draws,
                        TrainingModeLabel = modeLabel,
                        AgentPiece = agentPiece,
                        LastGameResult = result,
                        IsNewPhase = false
                    });
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Play a single training game. X always moves first.
    /// The agent can play as either X or O.
    /// Q-values are updated after EVERY agent action.
    /// Returns winner: 'X', 'O', or 'D'.
    /// </summary>
    private char PlayTrainingGame(QLearningAgent agent, ITrainingPlayer opponent, char agentPiece, char opponentPiece)
    {
        var board = new GameBoard();

        if (agentPiece == 'X')
        {
            return PlayGameAgentFirst(agent, opponent, board, agentPiece, opponentPiece);
        }
        else
        {
            return PlayGameAgentSecond(agent, opponent, board, agentPiece, opponentPiece);
        }
    }

    /// <summary>
    /// Agent plays X (moves first).
    /// </summary>
    private char PlayGameAgentFirst(QLearningAgent agent, ITrainingPlayer opponent, GameBoard board,
        char agentPiece, char opponentPiece)
    {
        while (true)
        {
            // --- Agent's turn (X, first) ---
            string stateBefore = board.GetStateString();
            int agentAction = agent.ChooseAction(board, isTraining: true);

            var boardBefore = board.Clone();
            board.MakeMove(agentAction, agentPiece);
            var boardAfter = board.Clone();

            // Calculate reward for agent's action
            double reward = _rewardCalculator.CalculateReward(boardBefore, agentAction, boardAfter, agentPiece, opponentPiece);

            // Check if game is over after agent's move
            char winner = board.CheckWinner();
            if (winner != 'N')
            {
                agent.UpdateQ(stateBefore, agentAction, reward, null);
                return winner;
            }

            // --- Opponent's turn (O, second) ---
            int opponentAction = opponent.ChooseMove(board, opponentPiece, agentPiece);
            board.MakeMove(opponentAction, opponentPiece);

            // Check if game is over after opponent's move
            winner = board.CheckWinner();
            string stateAfterOpponent = board.GetStateString();

            if (winner != 'N')
            {
                double endReward;
                if (winner == opponentPiece)
                    endReward = -1.0;       // Agent lost
                else if (winner == 'D')
                    endReward = 0.3;         // Draw — small positive reward
                else
                    endReward = reward;      // Shouldn't happen here, but safe fallback
                agent.UpdateQ(stateBefore, agentAction, endReward, null);
                return winner;
            }

            // Game continues - update Q with the next state (after opponent moved)
            agent.UpdateQ(stateBefore, agentAction, reward, stateAfterOpponent);
        }
    }

    /// <summary>
    /// Agent plays O (moves second). Opponent plays X (first).
    /// The loop alternates: opponent moves, then agent moves.
    /// Q-values are updated after each agent action using Bellman's equation.
    /// </summary>
    private char PlayGameAgentSecond(QLearningAgent agent, ITrainingPlayer opponent, GameBoard board,
        char agentPiece, char opponentPiece)
    {
        string? prevAgentState = null;
        int prevAgentAction = -1;
        double prevReward = 0;

        while (true)
        {
            // --- Opponent's turn (X, first mover) ---
            int opponentAction = opponent.ChooseMove(board, opponentPiece, agentPiece);
            board.MakeMove(opponentAction, opponentPiece);

            // Check if game is over after opponent's move
            char winner = board.CheckWinner();
            if (winner != 'N')
            {
                // If agent had a previous action, do final Q-update
                if (prevAgentState != null)
                {
                    double endReward;
                    if (winner == opponentPiece)
                        endReward = -1.0;       // Agent lost
                    else if (winner == 'D')
                        endReward = 0.3;         // Draw — small positive reward
                    else
                        endReward = prevReward;  // Shouldn't happen here, but safe fallback
                    agent.UpdateQ(prevAgentState, prevAgentAction, endReward, null);
                }
                return winner;
            }

            // --- Agent's turn (O, second mover) ---
            // First, if agent had a previous move, update its Q-value now
            // (the "next state" is the current board, after opponent responded)
            if (prevAgentState != null)
            {
                agent.UpdateQ(prevAgentState, prevAgentAction, prevReward, board.GetStateString());
            }

            string stateBefore = board.GetStateString();
            int agentAction = agent.ChooseAction(board, isTraining: true);

            var boardBefore = board.Clone();
            board.MakeMove(agentAction, agentPiece);
            var boardAfter = board.Clone();

            // Calculate reward for agent's action
            double reward = _rewardCalculator.CalculateReward(boardBefore, agentAction, boardAfter, agentPiece, opponentPiece);

            // Check if game is over after agent's move
            winner = board.CheckWinner();
            if (winner != 'N')
            {
                agent.UpdateQ(stateBefore, agentAction, reward, null);
                return winner;
            }

            // Save state for deferred Q-update (will be updated after opponent responds)
            prevAgentState = stateBefore;
            prevAgentAction = agentAction;
            prevReward = reward;
        }
    }
}

