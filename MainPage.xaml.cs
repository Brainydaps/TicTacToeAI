using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

namespace TicTacToeAI
{
    public partial class MainPage : ContentPage
    {
        private string[] board;
        private bool isPlayerTurn;
        private Dictionary<string, Dictionary<int, double>> qTable;
        private Random random;
        private QLearningAgent? selectedAgent;

        private Dictionary<string, string> agentFiles = new Dictionary<string, string>
        {
            {"Quick Overplanner", Path.Combine(FileSystem.Current.AppDataDirectory, "QuickOverplanner_qTable.json")},
            {"Quick Myopic", Path.Combine(FileSystem.Current.AppDataDirectory, "QuickMyopic_qTable.json")},
            {"Balanced", Path.Combine(FileSystem.Current.AppDataDirectory, "Balanced_qTable.json")},
            {"Slow Overplanner", Path.Combine(FileSystem.Current.AppDataDirectory, "SlowOverplanner_qTable.json")},
            {"Slow Myopic", Path.Combine(FileSystem.Current.AppDataDirectory, "SlowMyopic_qTable.json")}
        };

        public MainPage()
        {
            InitializeComponent();
            random = new Random();
            qTable = new Dictionary<string, Dictionary<int, double>>();
            board = new string[9];
            selectedAgent = null;
            Debug.WriteLine("MainPage initialized");

            // Ensure permissions for file access
            EnsurePermissionsAsync();
        }

        private async Task EnsurePermissionsAsync()
        {
            var readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
            if (readStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied", "Read permission is required to access files.", "OK");
                return;
            }

            var writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (writeStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied", "Write permission is required to update files.", "OK");
                return;
            }

            Debug.WriteLine("File permissions granted.");
        }

        private async void OnSelectAgentClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Select Agent button clicked");
            await SelectAgent();
        }

        private async Task SelectAgent()
        {
            Debug.WriteLine("SelectAgent method started");

            string[] agents = { "Quick Overplanner", "Quick Myopic", "Balanced", "Slow Overplanner", "Slow Myopic" };

            try
            {
                string chosenAgent = await DisplayActionSheet("Choose an agent to play against:", "Cancel", null, agents);
                Debug.WriteLine($"DisplayActionSheet result: {chosenAgent}");

                if (string.IsNullOrEmpty(chosenAgent))
                {
                    Debug.WriteLine("No agent selected, exiting");
                    return;
                }

                Debug.WriteLine($"Chosen agent: {chosenAgent}");

                // Create agent based on selection
                switch (chosenAgent)
                {
                    case "Quick Overplanner":
                        selectedAgent = new QLearningAgent(0.95, 0.95, agentFiles[chosenAgent], random);
                        break;
                    case "Quick Myopic":
                        selectedAgent = new QLearningAgent(0.95, 0.05, agentFiles[chosenAgent], random);
                        break;
                    case "Balanced":
                        selectedAgent = new QLearningAgent(0.5, 0.5, agentFiles[chosenAgent], random);
                        break;
                    case "Slow Overplanner":
                        selectedAgent = new QLearningAgent(0.05, 0.95, agentFiles[chosenAgent], random);
                        break;
                    case "Slow Myopic":
                        selectedAgent = new QLearningAgent(0.05, 0.05, agentFiles[chosenAgent], random);
                        break;
                }

                Debug.WriteLine($"Selected agent: {chosenAgent} with LearningRate {selectedAgent.LearningRate} and DiscountFactor {selectedAgent.DiscountFactor}");

                LoadQTable(selectedAgent.QTableFileName);
                NewGame();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DisplayActionSheet: {ex.Message}");
            }
        }

        private async void UpdateBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                var button = (Button)FindByName($"Button{i}");
                button.Text = board[i] ?? string.Empty;
                button.IsEnabled = string.IsNullOrEmpty(board[i]);
            }

            var winner = CheckWinner();
            if (winner != null)
            {
                // Capture last state before the game ends
                string lastState = string.Join(",", board.Select(b => b ?? "E"));

                // Update Q-values based on the winner
                UpdateQValues(winner, lastState);

                await DisplayAlert("Game Over", winner == "Draw" ? "It's a draw!" : $"{winner} wins!", "New Game");
                SaveQTable();
                NewGame();
            }
        }

        private void NewGame()
        {
            Debug.WriteLine("NewGame method started");

            board = new string[9];
            isPlayerTurn = true;

            Debug.WriteLine("New game initialized, board cleared");
            UpdateBoard();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            int index = int.Parse(button.ClassId);

            Debug.WriteLine($"Button {index} clicked");
            if (isPlayerTurn)
            {
                board[index] = "X";
                isPlayerTurn = false;
                Debug.WriteLine("Player made a move, updating board");
                UpdateBoard();
                ComputerMove();
            }
        }

        private void ComputerMove()
        {
            var winner = CheckWinner();
            if (winner != null) return;

            int? bestMove = selectedAgent?.FindBestMove(board, qTable);

            if (!bestMove.HasValue || board[bestMove.Value] != null)
            {
                Debug.WriteLine("Best move not found, picking a random move.");
                var availableMoves = Enumerable.Range(0, board.Length).Where(i => board[i] == null).ToList();

                if (availableMoves.Count > 0)
                {
                    bestMove = availableMoves[random.Next(availableMoves.Count)];
                }
                else
                {
                    Debug.WriteLine("No available moves, game should be over.");
                    return;
                }
            }

            Debug.WriteLine($"Computer move: {bestMove}");

            if (bestMove.HasValue)
            {
                board[bestMove.Value] = "O";
                selectedAgent.LastMove = bestMove.Value; // Update LastMove to the current computer move
                isPlayerTurn = true;
                UpdateBoard();
            }
        }

        private string CheckWinner()
        {
            string[,] winningCombinations = new string[,]
            {
                { "0", "1", "2" },
                { "3", "4", "5" },
                { "6", "7", "8" },
                { "0", "3", "6" },
                { "1", "4", "7" },
                { "2", "5", "8" },
                { "0", "4", "8" },
                { "2", "4", "6" }
            };

            for (int i = 0; i < winningCombinations.GetLength(0); i++)
            {
                string a = board[int.Parse(winningCombinations[i, 0])];
                string b = board[int.Parse(winningCombinations[i, 1])];
                string c = board[int.Parse(winningCombinations[i, 2])];

                if (a != null && a == b && a == c)
                {
                    Debug.WriteLine($"Winner found: {a}");
                    return a;
                }
            }

            return board.All(b => b != null) ? "Draw" : null;
        }

        private void LoadQTable(string fileName)
        {
            Debug.WriteLine($"Attempting to load Q-table from file: {fileName}");

            if (File.Exists(fileName))
            {
                var json = File.ReadAllText(fileName);
                qTable = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, double>>>(json);
                Debug.WriteLine("Q-table loaded successfully");
            }
            else
            {
                Debug.WriteLine("Q-table file not found, starting with empty Q-table");
                qTable = new Dictionary<string, Dictionary<int, double>>();
            }
        }

        private void SaveQTable()
        {
            var json = JsonSerializer.Serialize(qTable);
            File.WriteAllText(selectedAgent.QTableFileName, json);
            Debug.WriteLine("Q-table saved");
        }

        private void UpdateQValues(string winner, string lastState)
        {
            Debug.WriteLine($"Updating Q-values based on winner: {winner}");

            // Get the last move based on who made the last winning move
            int lastMove = winner == "X" ? (board.ToList().IndexOf("X")) : selectedAgent?.LastMove ?? -1;

            if (selectedAgent == null || lastMove == -1)
            {
                Debug.WriteLine("Selected agent or last move is invalid.");
                return;
            }

            if (!qTable.ContainsKey(lastState))
            {
                qTable[lastState] = new Dictionary<int, double>();
            }

            double reward = winner == "O" ? 1 : winner == "X" ? -1 : 0;

            if (!qTable[lastState].ContainsKey(lastMove))
            {
                qTable[lastState][lastMove] = 0.0;
            }

            double oldQValue = qTable[lastState][lastMove];
            double newQValue = oldQValue + selectedAgent.LearningRate * (reward + selectedAgent.DiscountFactor * GetMaxQValue(lastState) - oldQValue);
            qTable[lastState][lastMove] = newQValue;

            Debug.WriteLine($"Updated Q-value for state '{lastState}', action '{lastMove}': {newQValue}");
        }

        private double GetMaxQValue(string state)
        {
            if (qTable.TryGetValue(state, out var actions))
            {
                return actions.Values.Max();
            }
            return 0.0;
        }
    }

    public class QLearningAgent
    {
        public double LearningRate { get; }
        public double DiscountFactor { get; }
        public string QTableFileName { get; }
        public int LastMove { get; set; }

        private Random random;

        public QLearningAgent(double learningRate, double discountFactor, string qTableFileName, Random random)
        {
            LearningRate = learningRate;
            DiscountFactor = discountFactor;
            QTableFileName = qTableFileName;
            LastMove = -1;
            this.random = random;
        }

        public int? FindBestMove(string[] board, Dictionary<string, Dictionary<int, double>> qTable)
        {
            var currentState = string.Join(",", board.Select(b => b ?? "E"));

            if (qTable.TryGetValue(currentState, out var actions))
            {
                int bestAction = actions.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                Debug.WriteLine($"Best move found: {bestAction} for state {currentState}");
                return bestAction;
            }

            return null;
        }
    }
}
