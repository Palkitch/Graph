using System.Runtime.CompilerServices;

public class GridIndex<T>
{
    private readonly Dictionary<(int, int), List<T>> grid = new();
    private readonly List<int> xLines;
    private readonly List<int> yLines;
    private bool drawVerticalLine = true;
    private (int x, int y)? previousPoint = null;

    public GridIndex(int maximumX, int maximumY)
    {
        xLines = new List<int> { 0, maximumX };
        yLines = new List<int> { 0, maximumY };
    }

    public void AddPoint(int x, int y, T value)
    {
        var cell = (x, y);
        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<T>();
        }
        grid[cell].Add(value);

        if (previousPoint.HasValue)
        {
            var (prevX, prevY) = previousPoint.Value;
            var nearestXLines = GetNearestLines(xLines, y);
            var nearestYLines = GetNearestLines(yLines, x);
            int pointsCount = PointsCountBetweenExactLines(nearestXLines.left, nearestXLines.right, nearestYLines.left, nearestYLines.right);

            if (pointsCount > 1)
            {
                DrawLine(prevX, prevY, x, y);
            }
        }
        previousPoint = (x, y);
    }

    /*
     public void AddPoint(int x, int y, T value) Add poinbt který přidává čáry mezi nejbližší body v oblasti
{
    var cell = (x, y);
    if (!grid.ContainsKey(cell))
    {
        grid[cell] = new List<T>();
    }
    grid[cell].Add(value);

    var nearestXLines = GetNearestLines(xLines, y);
    var nearestYLines = GetNearestLines(yLines, x);

    // Najdeme jiný bod ve stejné oblasti mezi čárami
    var regionPoints = grid.Keys
        .Where(k => k.Item1 > nearestYLines.left && k.Item1 < nearestYLines.right
                 && k.Item2 > nearestXLines.left && k.Item2 < nearestXLines.right
                 && k != cell) // Abychom nebrali právě přidávaný bod
        .ToList();

    if (regionPoints.Any())
    {
        var closestPoint = regionPoints
            .OrderBy(p => Math.Abs(p.Item1 - x) + Math.Abs(p.Item2 - y)) // Manhattan vzdálenost
            .First();

        DrawLine(closestPoint.Item1, closestPoint.Item2, x, y);
    }
}

     */

    private (int left, int right) GetNearestLines(List<int> lines, int point)
    {
        int left = lines.Where(l => l <= point).DefaultIfEmpty(int.MinValue).Max();
        int right = lines.Where(l => l >= point).DefaultIfEmpty(int.MaxValue).Min();

        return (left, right);
    }




    private void DrawLine(int x1, int y1, int x2, int y2)
    {
        if (drawVerticalLine)
        {
            int midX = (x1 + x2) / 2;
            if (!yLines.Contains(midX))
            {
                yLines.Add(midX);
                yLines.Sort();
                Console.WriteLine($"Vertikální čára na x = {midX} mezi ({x1}, {y1}) a ({x2}, {y2})");
            }
        }
        else
        {
            int midY = (y1 + y2) / 2;
            if (!xLines.Contains(midY))
            {
                xLines.Add(midY);
                xLines.Sort();
                Console.WriteLine($"Horizontální čára na y = {midY} mezi ({x1}, {y1}) a ({x2}, {y2})");
            }
        }
        drawVerticalLine = !drawVerticalLine;
    }

    private int PointsCountBetweenExactLines(int yLeft, int yRight, int xLeft, int xRight)
    {
        return grid.Keys
            .Where(k => k.Item1 > xLeft && k.Item1 < xRight && k.Item2 > yLeft && k.Item2 < yRight)
            .Count();
    }




    private List<(int, int)> GetNeighbors((int, int) cell)
    {
        return new List<(int, int)>
        {
            (cell.Item1 - 1, cell.Item2),
            (cell.Item1 + 1, cell.Item2),
            (cell.Item1, cell.Item2 - 1),
            (cell.Item1, cell.Item2 + 1)
        };
    }

    public string[,] ToArray()
    {
        int minX = grid.Keys.Min(k => k.Item1);
        int minY = grid.Keys.Min(k => k.Item2);
        int maxX = grid.Keys.Max(k => k.Item1);
        int maxY = grid.Keys.Max(k => k.Item2);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        var array = new string[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                array[i, j] = "-";
            }
        }

        foreach (var (key, values) in grid)
        {
            foreach (var value in values)
            {
                array[key.Item1 - minX, key.Item2 - minY] = value.ToString();
            }
        }

        return array;
    }

    public void PrintXLines()
    {
        Console.WriteLine("Čáry na ose X:");
        foreach (var line in xLines)
        {
            Console.WriteLine($"Horizontální čára na y = {line}");
        }
    }

    public void PrintYLines()
    {
        Console.WriteLine("Čáry na ose Y:");
        foreach (var line in yLines)
        {
            Console.WriteLine($"Vertikální čára na x = {line}");
        }
    }
}
