using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CellTileBase2x2Sheet : ScriptableObject
{
    [Range(0,2)]
    public int CellTileHeight = 0;
    public Cell2x2TileBase[] Cell2x2TileBases;

    public Cell2x2TileBase Grab2x2TileAtIndex(int index)
    {
        if (index >= Cell2x2TileBases.Length)
            Debug.LogError("index is greater than the TIleBase2x2Sheet entry count. Please revaluate passed in index.");

        return Cell2x2TileBases[index];
    }

    public Cell2x2TileBase GrabRandom2x2Tile()
    {
        if (Cell2x2TileBases.Length == 0)
            Debug.LogError("TIleBase2x2Sheet is empty. Please fill it with the relevant CellTileBase2x2 entires before accessing it");

        return Cell2x2TileBases[Random.Range(0, Cell2x2TileBases.Length)];
    }
}
