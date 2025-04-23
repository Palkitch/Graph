// V souboru, kde máte AreaSearchResult<T>
using Graph.Grid;
using System.Collections.Generic;
namespace Graph
{
    public class AreaSearchResult<T>
    {
        /// <summary>
        /// Seznam nalezených položek. Každá položka obsahuje uzel (GridNode)
        /// a informace o buňce, ze které byl načten.
        /// </summary>
        public List<(GridNode<T> Node, int XIndex, int YIndex, long Offset)> FoundPoints { get; } // <<< ZMĚNA ZDE

        /// <summary>
        /// Seznam indexů všech buněk mřížky, které byly při hledání zkontrolovány.
        /// </summary>
        public List<(int XIndex, int YIndex)> CheckedCellIndices { get; }

        public AreaSearchResult()
        {
            // Inicializace správného typu seznamu
            FoundPoints = new List<(GridNode<T> Node, int XIndex, int YIndex, long Offset)>(); // <<< ZMĚNA ZDE
            CheckedCellIndices = new List<(int XIndex, int YIndex)>();
        }
    }
}