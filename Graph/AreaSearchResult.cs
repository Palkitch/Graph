using System;
using System.Collections.Generic;


public class AreaSearchResult<T>
{
    public List<T> FoundPoints { get; }

    public List<(int XIndex, int YIndex)> CheckedCellIndices { get; }

    public AreaSearchResult()
    {
        FoundPoints = new List<T>();
        CheckedCellIndices = new List<(int XIndex, int YIndex)>();
    }
}