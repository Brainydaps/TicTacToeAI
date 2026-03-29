namespace TicTacToeAI.Services;

/// <summary>
/// Custom drawable that renders a learning progress graph.
/// Y-axis: Learning % (0-100)
///   - Win = 1 point, Draw = 0.5 points, Loss = 0 points
///   - Learning % = total points / total games × 100
/// X-axis: Game number (data point index)
/// </summary>
public class LearningGraphDrawable : IDrawable
{
    private readonly List<float> _learningPercentages = new();
    private readonly List<string> _phaseLabels = new();
    private readonly List<int> _phaseBoundaries = new();
    private readonly object _lock = new();

    /// <summary>
    /// Record a batch of games at the current learning percentage.
    /// </summary>
    public void RecordBatch(int gameCount, double learningPct)
    {
        lock (_lock)
        {
            _learningPercentages.Add((float)learningPct);
        }
    }

    /// <summary>
    /// Mark the start of a new training phase (new opponent).
    /// </summary>
    public void MarkPhase(string label)
    {
        lock (_lock)
        {
            _phaseBoundaries.Add(_learningPercentages.Count);
            _phaseLabels.Add(label);
        }
    }

    /// <summary>
    /// Clear all data for a fresh training run.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _learningPercentages.Clear();
            _phaseLabels.Clear();
            _phaseBoundaries.Clear();
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        lock (_lock)
        {
            DrawGraph(canvas, dirtyRect);
        }
    }

    private void DrawGraph(ICanvas canvas, RectF dirtyRect)
    {
        float width = dirtyRect.Width;
        float height = dirtyRect.Height;

        if (width <= 0 || height <= 0) return;

        float marginLeft = 45;
        float marginRight = 15;
        float marginTop = 15;
        float marginBottom = 30;

        float graphWidth = width - marginLeft - marginRight;
        float graphHeight = height - marginTop - marginBottom;

        if (graphWidth <= 0 || graphHeight <= 0) return;

        // Background
        canvas.FillColor = Color.FromArgb("#0a0a1a");
        canvas.FillRoundedRectangle(0, 0, width, height, 10);

        // Grid lines and Y-axis labels
        canvas.FontSize = 9;
        canvas.FontColor = Color.FromArgb("#555555");
        canvas.StrokeColor = Color.FromArgb("#1a1a3a");
        canvas.StrokeSize = 1;

        for (int pct = 0; pct <= 100; pct += 25)
        {
            float y = marginTop + graphHeight - (pct / 100f * graphHeight);
            canvas.DrawLine(marginLeft, y, marginLeft + graphWidth, y);
            canvas.DrawString($"{pct}%", 2, y - 6, marginLeft - 6, 12, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // X-axis label
        canvas.FontColor = Color.FromArgb("#666666");
        canvas.FontSize = 8;
        canvas.DrawString("Games →", marginLeft + graphWidth / 2 - 20, height - 5, 60, 12,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Phase boundary lines
        int totalPoints = _learningPercentages.Count;
        if (totalPoints > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#333355");
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };

            for (int i = 0; i < _phaseBoundaries.Count; i++)
            {
                int boundary = _phaseBoundaries[i];
                float x = marginLeft + (float)boundary / totalPoints * graphWidth;
                canvas.DrawLine(x, marginTop, x, marginTop + graphHeight);

                // Phase label
                canvas.FontSize = 7;
                canvas.FontColor = Color.FromArgb("#555577");

                float labelX = x + 3;
                if (i < _phaseLabels.Count)
                {
                    canvas.DrawString(_phaseLabels[i], labelX, marginTop + 2, 80, 10,
                        HorizontalAlignment.Left, VerticalAlignment.Top);
                }
            }
            canvas.StrokeDashPattern = null;
        }

        // Draw the learning curve
        if (totalPoints < 2) return;

        // Downsample if too many points for the pixel width
        int step = Math.Max(1, totalPoints / (int)graphWidth);

        var path = new PathF();
        bool started = false;

        for (int i = 0; i < totalPoints; i += step)
        {
            float x = marginLeft + (float)i / (totalPoints - 1) * graphWidth;
            float y = marginTop + graphHeight - (_learningPercentages[i] / 100f * graphHeight);

            if (!started)
            {
                path.MoveTo(x, y);
                started = true;
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        // Ensure last point is included
        {
            float x = marginLeft + graphWidth;
            float y = marginTop + graphHeight - (_learningPercentages[^1] / 100f * graphHeight);
            path.LineTo(x, y);
        }

        // Draw gradient fill under the curve
        var fillPath = new PathF(path);
        fillPath.LineTo(marginLeft + graphWidth, marginTop + graphHeight);
        fillPath.LineTo(marginLeft, marginTop + graphHeight);
        fillPath.Close();

        canvas.FillColor = Color.FromArgb("#1a3355");
        canvas.Alpha = 0.4f;
        canvas.FillPath(fillPath);
        canvas.Alpha = 1.0f;

        // Draw the line
        canvas.StrokeColor = Color.FromArgb("#4fc3f7");
        canvas.StrokeSize = 2;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);

        // Current value label
        float currentPct = _learningPercentages[^1];
        canvas.FontSize = 12;
        canvas.FontColor = Color.FromArgb("#4fc3f7");

        float labelY = marginTop + graphHeight - (currentPct / 100f * graphHeight) - 16;
        if (labelY < marginTop) labelY = marginTop + 2;

        canvas.DrawString($"{currentPct:F1}%", marginLeft + graphWidth - 50, labelY, 50, 14,
            HorizontalAlignment.Right, VerticalAlignment.Center);
    }
}

