using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Cell2x2TileBaseCatalogue : ScriptableObject
{

    [SerializeField] private Cell2x2TileBaseSheet[] cellTileBase2x2Sheets;

    public Cell2x2TileBaseSheet[] CellTileBase2x2Sheets { get => cellTileBase2x2Sheets; }

    public Cell2x2TileBaseSheet Grab2x2TileBaseSheet(int index)
    {
        if (index >= cellTileBase2x2Sheets.Length)
            Debug.LogError("index is greater than the CellTileBase2x2Sheets entry count. Please revaluate passed in index.");

        return cellTileBase2x2Sheets[index];
    }

    public Cell2x2TileBaseSheet Grab2x2TileBaseSheetWithHeightValue(int heightValue)
    {
        if (cellTileBase2x2Sheets.Length == 0)
            Debug.LogError("CellTileBase2x2Sheets is empty. Please fill it with the relevant Cell2x2TileBaseSheet entries before accessing it");

        foreach(Cell2x2TileBaseSheet tileSheet in cellTileBase2x2Sheets)
            if(tileSheet.CellTileHeight == heightValue)
                return tileSheet;

        if (cellTileBase2x2Sheets.Length == 0)
            Debug.LogWarning("CellTileBase2x2Sheets has no entries with a CellTileHeight Value of " + heightValue);

        return null;
    }
}
