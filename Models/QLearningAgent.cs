using System.Diagnostics;
using System.Text.Json;

namespace TicTacToeAI.Models;

/// <summary>
/// Represents a Q-Learning agent with specific hyperparameters.
/// The agent can play as either 'X' (first mover) or 'O' (second mover).
/// Separate Q-tables are maintained for each side.
/// </summary>
public class QLearningAgent
{
    public string Name { get; }
    public double Alpha { get; } // Learning rate
    public double Gamma { get; } // Discount factor

    /// <summary>Q-table file path for X-side training.</summary>
    public string QTableFilePathX { get; }

    /// <summary>Q-table file path for O-side training.</summary>
    public string QTableFilePathO { get; }

    /// <summary>Legacy single-path property — returns X path for backward compatibility.</summary>
    public string QTableFilePath => QTableFilePathX;

    /// <summary>Q-table for when the agent plays as X.</summary>
    public Dictionary<string, Dictionary<int, double>> QTableX { get; set; }

    /// <summary>Q-table for when the agent plays as O.</summary>
    public Dictionary<string, Dictionary<int, double>> QTableO { get; set; }

    /// <summary>
    /// Active Q-table — points to QTableX or QTableO depending on current side.
    /// Used by ChooseAction/UpdateQ during training and play.
    /// </summary>
    public Dictionary<string, Dictionary<int, double>> QTable
    {
        get => _activeQTable;
        set => _activeQTable = value;
    }

    private Dictionary<string, Dictionary<int, double>> _activeQTable;
    private readonly Random _random;
    private double _epsilon; // Exploration rate for training

    public QLearningAgent(string name, double alpha, double gamma, string qTableFilePathX, string qTableFilePathO)
    {
        Name = name;
        Alpha = alpha;
        Gamma = gamma;
        QTableFilePathX = qTableFilePathX;
        QTableFilePathO = qTableFilePathO;
        QTableX = new Dictionary<string, Dictionary<int, double>>();
        QTableO = new Dictionary<string, Dictionary<int, double>>();
        _activeQTable = QTableX;
        _random = new Random();
        _epsilon = 0.1;
    }

    /// <summary>
    /// Set which side's Q-table is active ('X' or 'O').
    /// </summary>
    public void SetActiveSide(char side)
    {
        _activeQTable = side == 'O' ? QTableO : QTableX;
    }

    /// <summary>
    /// Create a named agent with preset hyperparameters.
    /// </summary>
    public static QLearningAgent Create(string agentName)
    {
        var dir = FileSystem.Current.AppDataDirectory;
        var sanitized = agentName.Replace(" ", "");
        var filePathX = Path.Combine(dir, $"{sanitized}_X_qTable.json");
        var filePathO = Path.Combine(dir, $"{sanitized}_O_qTable.json");

        return agentName switch
        {
            "Balanced" => new QLearningAgent("Balanced", 0.5, 0.5, filePathX, filePathO),
            "Quick Myopic" => new QLearningAgent("Quick Myopic", 0.95, 0.05, filePathX, filePathO),
            "Quick Overplanner" => new QLearningAgent("Quick Overplanner", 0.95, 0.95, filePathX, filePathO),
            "Slow Myopic" => new QLearningAgent("Slow Myopic", 0.05, 0.05, filePathX, filePathO),
            "Slow Overplanner" => new QLearningAgent("Slow Overplanner", 0.05, 0.95, filePathX, filePathO),
            _ => throw new ArgumentException($"Unknown agent: {agentName}")
        };
    }

    public static string[] AgentNames => ["Balanced", "Quick Myopic", "Quick Overplanner", "Slow Myopic", "Slow Overplanner"];

    /// <summary>
    /// Check if a Q-table file exists for the given side.
    /// </summary>
    public bool HasQTable(char side) => File.Exists(side == 'O' ? QTableFilePathO : QTableFilePathX);

    /// <summary>
    /// Check if any Q-table file exists (X or O).
    /// </summary>
    public bool HasAnyQTable() => HasQTable('X') || HasQTable('O');

    /// <summary>
    /// Choose the best action for the current board state.
    /// During training, uses epsilon-greedy exploration.
    /// During play, always picks the best action.
    /// </summary>
    public int ChooseAction(GameBoard board, bool isTraining = false)
    {
        var state = board.GetStateString();
        var availableMoves = board.GetAvailableMoves();

        if (availableMoves.Count == 0)
            throw new InvalidOperationException("No available moves");

        // Epsilon-greedy during training
        if (isTraining && _random.NextDouble() < _epsilon)
        {
            return availableMoves[_random.Next(availableMoves.Count)];
        }

        // Try to find the best action from Q-table
        if (_activeQTable.TryGetValue(state, out var actions))
        {
            // Filter to only available (empty) moves with Q-value > -1.0
            var validActions = actions
                .Where(a => availableMoves.Contains(a.Key))
                .ToList();

            if (validActions.Count > 0)
            {
                double maxQ = validActions.Max(a => a.Value);
                var bestActions = validActions.Where(a => Math.Abs(a.Value - maxQ) < 1e-9).ToList();
                return bestActions[_random.Next(bestActions.Count)].Key;
            }
        }

        // Fallback: random move
        return availableMoves[_random.Next(availableMoves.Count)];
    }

    /// <summary>
    /// Update Q-value using Bellman's equation:
    /// Q(s,a) = Q(s,a) + α * (reward + γ * max(Q(s',a')) - Q(s,a))
    /// </summary>
    public void UpdateQ(string state, int action, double reward, string? nextState)
    {
        // Ensure state exists in Q-table
        if (!_activeQTable.ContainsKey(state))
        {
            _activeQTable[state] = new Dictionary<int, double>();
            for (int i = 0; i < 9; i++)
                _activeQTable[state][i] = 0.0;
        }

        if (!_activeQTable[state].ContainsKey(action))
            _activeQTable[state][action] = 0.0;

        double maxNextQ = 0.0;
        if (nextState != null && _activeQTable.TryGetValue(nextState, out var nextActions) && nextActions.Count > 0)
        {
            // Only consider valid (non-occupied) positions for max Q
            var validNextActions = nextActions.Where(a => nextState[a.Key] == 'E').ToList();
            if (validNextActions.Count > 0)
                maxNextQ = validNextActions.Max(a => a.Value);
        }

        double oldQ = _activeQTable[state][action];
        double newQ = oldQ + Alpha * (reward + Gamma * maxNextQ - oldQ);
        _activeQTable[state][action] = newQ;
    }

    /// <summary>
    /// Load Q-table for a specific side from its JSON file.
    /// </summary>
    public void LoadQTable(char side)
    {
        var filePath = side == 'O' ? QTableFilePathO : QTableFilePathX;
        var loaded = LoadQTableFromFile(filePath);
        if (side == 'O')
            QTableO = loaded;
        else
            QTableX = loaded;
        // Keep active pointer in sync
        SetActiveSide(side == 'O' ? 'O' : 'X');
    }

    /// <summary>
    /// Load both X and O Q-tables if they exist.
    /// Sets active side to X by default.
    /// </summary>
    public void LoadQTable()
    {
        QTableX = LoadQTableFromFile(QTableFilePathX);
        QTableO = LoadQTableFromFile(QTableFilePathO);
        SetActiveSide('X');
    }

    private Dictionary<string, Dictionary<int, double>> LoadQTableFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(json);
                if (loaded != null)
                {
                    var qTable = new Dictionary<string, Dictionary<int, double>>();
                    foreach (var kvp in loaded)
                    {
                        qTable[kvp.Key] = new Dictionary<int, double>();
                        foreach (var action in kvp.Value)
                        {
                            if (int.TryParse(action.Key, out int key))
                                qTable[kvp.Key][key] = action.Value;
                        }
                    }
                    Debug.WriteLine($"Q-table loaded: {qTable.Count} states from {filePath}");
                    return qTable;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Q-table: {ex.Message}");
            }
        }
        else
        {
            Debug.WriteLine($"Q-table file not found: {filePath}");
        }
        return new Dictionary<string, Dictionary<int, double>>();
    }

    /// <summary>
    /// Save Q-table for a specific side to its JSON file.
    /// </summary>
    public void SaveQTable(char side)
    {
        var qTable = side == 'O' ? QTableO : QTableX;
        var filePath = side == 'O' ? QTableFilePathO : QTableFilePathX;
        SaveQTableToFile(qTable, filePath);
    }

    /// <summary>
    /// Save both Q-tables.
    /// </summary>
    public void SaveQTable()
    {
        if (QTableX.Count > 0)
            SaveQTableToFile(QTableX, QTableFilePathX);
        if (QTableO.Count > 0)
            SaveQTableToFile(QTableO, QTableFilePathO);
    }

    private void SaveQTableToFile(Dictionary<string, Dictionary<int, double>> qTable, string filePath)
    {
        try
        {
            var serializable = new Dictionary<string, Dictionary<string, double>>();
            foreach (var kvp in qTable)
            {
                serializable[kvp.Key] = new Dictionary<string, double>();
                foreach (var action in kvp.Value)
                {
                    serializable[kvp.Key][action.Key.ToString()] = action.Value;
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(serializable, options);
            File.WriteAllText(filePath, json);
            Debug.WriteLine($"Q-table saved: {qTable.Count} states to {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving Q-table: {ex.Message}");
        }
    }

    /// <summary>
    /// Set epsilon for exploration during training. Decreases over training.
    /// </summary>
    public void SetEpsilon(double epsilon)
    {
        _epsilon = Math.Clamp(epsilon, 0.0, 1.0);
    }
}

