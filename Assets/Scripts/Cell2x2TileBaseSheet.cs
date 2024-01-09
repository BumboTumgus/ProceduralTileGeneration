using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Cell2x2TileBaseSheet : ScriptableObject
{
    [Range(0,2)]
    public int CellTileHeight = 0;
    public Cell2x2TileBase[] Cell2x2TileBases;

    public Cell2x2TileBase Grab2x2Tile(int index)
    {
        if (index >= Cell2x2TileBases.Length)
            Debug.LogError("index is greater than the Cell2x2TileBases entry count. Please revaluate passed in index.");

        return Cell2x2TileBases[index];
    }

    public Cell2x2TileBase GrabRandom2x2Tile()
    {
        if (Cell2x2TileBases.Length == 0)
            Debug.LogError("Cell2x2TileBases is empty. Please fill it with the relevant Cell2x2TileBase entries before accessing it");

        return Cell2x2TileBases[Random.Range(0, Cell2x2TileBases.Length)];
    }

    public Cell2x2TileBase GrabWeightedRandom2x2Tile()
    {
        if (Cell2x2TileBases.Length == 0)
            Debug.LogError("Cell2x2TileBases is empty. Please fill it with the relevant Cell2x2TileBase entries before accessing it");

        float totalTileRollWeights = 0;
        foreach (Cell2x2TileBase cell in Cell2x2TileBases)
            totalTileRollWeights += cell.WeightedRandomChance;

        float rolledTileWeight = Random.Range(0f, totalTileRollWeights);
        foreach(Cell2x2TileBase cell in Cell2x2TileBases)
        {
            rolledTileWeight -= cell.WeightedRandomChance;
            if (rolledTileWeight < 0f)
                return cell;
        }

        return null;
    }
}
