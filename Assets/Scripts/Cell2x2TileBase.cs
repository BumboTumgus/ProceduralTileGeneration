using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class Cell2x2TileBase
{
    public float WeightedRandomChance = 1f;
    public TileBase[] TileBase2x2 = new TileBase[4];
}
