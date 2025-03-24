using System.Runtime.CompilerServices;

public class GridIndex<T>
{
    private readonly Dictionary<(int, int), T> grid = new();
    private readonly List<int> horizontalLines;
    private readonly List<int> verticalLines;
    private bool drawVerticalLine = true;
    private (int x, int y)? previousPoint = null;

    public GridIndex(int maximumX, int maximumY)
    {
        horizontalLines = new List<int> { 0, maximumX };
        verticalLines = new List<int> { 0, maximumY };
    }

    public void AddPoint(int x, int y, T value)
    {
        var cell = (x, y);

        grid[cell] = value;

        if (previousPoint.HasValue)
        {
            var (prevX, prevY) = previousPoint.Value;

            // Přidání podmínek pro kontrolu stejných souřadnic
            if (prevX == x)
            {
                drawVerticalLine = false;
            }
            else if (prevY == y)
            {
                drawVerticalLine = true;
            }

            var nearestXLines = GetNearestLines(horizontalLines, y);
            var nearestYLines = GetNearestLines(verticalLines, x);
            int pointsCount = PointsCountBetweenExactLines(nearestXLines.left, nearestXLines.right, nearestYLines.left, nearestYLines.right);

            if (pointsCount > 1)
            {
                DrawLine(prevX, prevY, x, y);
            }
        }
        previousPoint = (x, y);
    }


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
            int midX = ((x1 + x2) / 2) + 1;
            if (!verticalLines.Contains(midX))
            {
                verticalLines.Add(midX);
                verticalLines.Sort();
                Console.WriteLine($"Vertikální čára na x = {midX} mezi ({x1}, {y1}) a ({x2}, {y2})");
            }
        }
        else
        {
            int midY = ((y1 + y2) / 2) + 1;
            if (!horizontalLines.Contains(midY))
            {
                horizontalLines.Add(midY);
                horizontalLines.Sort();
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

        foreach (var (key, value) in grid)
        {
            array[key.Item1 - minX, key.Item2 - minY] = value.ToString();
        }

        return array;
    }

    public string ToCompactString()
    {
        var array = ToArray();
        var output = new StringWriter();

        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                output.Write(array[i, j]);
            }
            output.WriteLine();
        }

        return output.ToString();
    }

    public void PrintXLines()
    {
        foreach (var line in horizontalLines)
        {
            Console.WriteLine($"Horizontální čára na y = {line}");
        }
    }

    public void PrintYLines()
    {
        foreach (var line in verticalLines)
        {
            Console.WriteLine($"Vertikální čára na x = {line}");
        }
    }
    public T FindPointBetweenLines(int x, int y)
    {
        // Zjistíme, jestli existuje bod přesně na zadaných souřadnicích
        if (grid.TryGetValue((x, y), out var value))
        {
            return value;
        }

        // Pokud žádný bod nenalezen, vrátíme null
        return default(T);
    }



    public List<(T Id, int X, int Y)> FindPointsInArea(int x1, int y1, int x2, int y2)
    {
        var nearestXLines = GetNearestLinesOutsideInterval(horizontalLines, y1, y2);
        var nearestYLines = GetNearestLinesOutsideInterval(verticalLines, x1, x2);

        var points = grid.Keys
            .Where(k => k.Item1 >= nearestYLines.left && k.Item1 <= nearestYLines.right
                        && k.Item2 >= nearestXLines.left && k.Item2 <= nearestXLines.right
                        && k.Item1 >= x1 && k.Item1 <= x2
                        && k.Item2 >= y1 && k.Item2 <= y2)
            .Select(k => (Id: grid[k], X: k.Item1, Y: k.Item2))
            .ToList();

        return points;
    }

    private (int left, int right) GetNearestLinesOutsideInterval(List<int> lines, int start, int end)
    {
        int left = lines.Where(l => l < start).DefaultIfEmpty(int.MinValue).Max();
        int right = lines.Where(l => l > end).DefaultIfEmpty(int.MaxValue).Min();

        return (left, right);
    }

}




