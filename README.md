# 🎮 TicTacToe AI

A cross-platform mobile & desktop app built with **.NET MAUI** that lets you train and play against a **Q-Learning reinforcement learning agent** in Tic-Tac-Toe.

![Platform](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20macOS-blue)
![Framework](https://img.shields.io/badge/.NET-10.0%20MAUI-purple)
![License](https://img.shields.io/badge/license-CC%20BY--NC%204.0-green)

---

## 📸 Screenshots

<img width="1920" height="1080" alt="TicTacToeAI" src="https://github.com/user-attachments/assets/474d1557-4b0d-4d06-b2c5-31992293edc7" />


---

## ✨ Features

- 🤖 **Q-Learning AI** — A reinforcement learning agent trained via Bellman's equation that progressively improves through self-play against heuristic opponents
- 🏋️ **Training Center** — Train any of 5 pre-configured agents with configurable iterations, and watch real-time learning progress on a live graph
- 🎮 **Play Page** — Challenge the trained AI as either X or O, with a live win/loss/draw scoreboard
- ⚡ **5 Agent Personalities** — Each with unique hyperparameters (learning rate α & discount factor γ):

  | Agent | α (Learning Rate) | γ (Discount Factor) | Personality |
  |---|---|---|---|
  | Balanced | 0.5 | 0.5 | Well-rounded learner |
  | Quick Myopic | 0.95 | 0.05 | Learns fast, short-sighted |
  | Quick Overplanner | 0.95 | 0.95 | Learns fast, plans far ahead |
  | Slow Myopic | 0.05 | 0.05 | Learns slowly, short-sighted |
  | Slow Overplanner | 0.05 | 0.95 | Learns slowly, plans far ahead |

- 🎯 **Training Modes** — Train the agent as X (first mover), O (second mover), or both
- 📈 **Learning Progress Graph** — Visualises win rate over time during training
- 💾 **Persistent Q-Tables** — Trained models are saved locally and reloaded automatically

---

## 🧠 How Q-Learning Works

The agent uses **Q-Learning**, a model-free reinforcement learning algorithm:

1. The **state** is the current board configuration (a 9-character string of `X`, `O`, `E`)
2. For each state, the agent stores **Q-values** for every legal action (0–8)
3. After each move, Q-values are updated using the **Bellman equation**:

   ```
   Q(s, a) ← Q(s, a) + α · [r + γ · max Q(s', a') − Q(s, a)]
   ```

   - **α** — learning rate (how quickly new info overwrites old)
   - **γ** — discount factor (how much future rewards are valued)
   - **r** — immediate reward (+1 win, −1 loss, +0.5 draw, small penalties/rewards for tactical moves)

4. Training uses **ε-greedy exploration** — the agent occasionally tries random moves to discover new strategies

---

## 🗂️ Project Structure

```
TicTacToeAI/
├── Models/
│   ├── GameBoard.cs          # Board logic, move validation, win detection
│   └── QLearningAgent.cs     # Q-Learning agent (Q-tables, action selection, Bellman update)
├── Pages/
│   ├── TrainingPage.xaml/.cs # Training UI: config, progress, live graph
│   └── PlayPage.xaml/.cs     # Play UI: human vs AI game
├── Players/                  # Heuristic training opponents
│   ├── RandomPlayer.cs       # Plays randomly
│   ├── CenterCornerPlayer.cs # Prefers center and corners
│   ├── BlockerPlayer.cs      # Blocks opponent's winning moves
│   └── TrianglePlayer.cs     # Corner-fork strategy (hardest)
├── Services/
│   ├── TrainingService.cs    # Orchestrates training loops against all 4 players
│   ├── RewardCalculator.cs   # Computes rewards for each game outcome
│   ├── QTableGenerator.cs    # Saves/loads Q-tables as JSON
│   └── LearningGraphDrawable.cs # Custom canvas graph renderer
└── *.json                    # Pre-trained Q-table snapshots
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [.NET MAUI workload](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)
- Android SDK / Xcode (for mobile targets) or macOS for Mac Catalyst

### Clone & Run

```bash
git clone https://github.com/<your-username>/TicTacToeAI.git
cd TicTacToeAI

# Android
dotnet build -f net10.0-android
dotnet run -f net10.0-android

# iOS (requires macOS + Xcode)
dotnet build -f net10.0-ios

# Mac Catalyst
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

### Using Pre-Trained Models

The repository includes pre-trained Q-table snapshots (`.json` files) for all 5 agent personalities. On first launch, if no trained model is found in the app's data directory, you can train from scratch using the **Training Center**.

---

## 🕹️ Usage

1. **Launch the app** — you'll see the agent selection screen
2. **Select an agent** (e.g., *Balanced*)
3. **(Optional) Train the agent** — tap *Train Agent*, configure the number of games per opponent, choose a training mode (X / O / Both), then tap *▶️ Start Full Training*
4. **Play against the AI** — tap *Play*, choose your side (X or O), and tap a cell to make your move

---

## 🏗️ Training Pipeline

The agent trains sequentially against 4 heuristic players of increasing difficulty:

| Stage | Opponent | Strategy |
|---|---|---|
| 1 | Random Player | Plays completely at random |
| 2 | Center & Corner Player | Prefers center, then corners |
| 3 | Blocker Player | Blocks opponent's winning threats |
| 4 | Triangle Player | Corner-fork strategy — creates unblockable dual threats |

Each stage runs for the configured number of iterations (default: 2 000). The agent's Q-table is updated after **every individual move** during training.

---

## 📄 License

This project is licensed under the **Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)** license.

You are free to share and adapt this work for **non-commercial purposes**, provided you give appropriate credit.

See the [LICENSE](LICENSE) file for full details, or visit [creativecommons.org/licenses/by-nc/4.0](https://creativecommons.org/licenses/by-nc/4.0/).

---

## 🙏 Acknowledgements

- [.NET MAUI](https://github.com/dotnet/maui) — cross-platform UI framework
- Q-Learning algorithm based on Watkins & Dayan (1992)

