using System;
using System.Collections.Generic;
using System.Linq;

public class GridIndex<T>
{
    private readonly int cellSizeX, cellSizeY;
    private readonly Dictionary<(int, int), List<T>> grid = new();

    public GridIndex(int cellSizeX, int cellSizeY)
    {
        this.cellSizeX = cellSizeX;
        this.cellSizeY = cellSizeY;
    }

    private (int, int) GetCell(int x, int y) => (x / cellSizeX, y / cellSizeY);

    public void AddPoint(int x, int y, T value)
    {
        var cell = GetCell(x, y);
        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<T>();
        }
        grid[cell].Add(value);
    }

    public T GetPoint(int x, int y)
    {
        var cell = GetCell(x, y);
        return grid.TryGetValue(cell, out var values) && values.Count > 0 ? values[0] : default;
    }

    public List<T> GetPointsInCell(int x, int y)
    {
        var cell = GetCell(x, y);
        return grid.TryGetValue(cell, out var values) ? values : new List<T>();
    }

    public List<T> GetPointsInRegion(int x1, int y1, int x2, int y2)
    {
        var result = new List<T>();
        for (int x = x1 / cellSizeX; x <= x2 / cellSizeX; x++)
        {
            for (int y = y1 / cellSizeY; y <= y2 / cellSizeY; y++)
            {
                if (grid.TryGetValue((x, y), out var values))
                {
                    result.AddRange(values);
                }
            }
        }
        return result;
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
}
