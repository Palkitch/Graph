using System;
using System.Collections.Generic;


// V souboru, kde máte AreaSearchResult<T>

using System;
using System.Collections.Generic;

public class AreaSearchResult<T>
{

    public List<GridNode<T>> FoundPoints { get; }


    public List<(int XIndex, int YIndex)> CheckedCellIndices { get; }

    public AreaSearchResult()
    {
        FoundPoints = new List<GridNode<T>>();
        CheckedCellIndices = new List<(int XIndex, int YIndex)>();
    }
}