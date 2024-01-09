using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TerrainWaveFunctionCollapser : MonoBehaviour
{
    private const float TREE_TOTAL_CELL_COUNT = 25;
    private const float BUSH_TOTAL_CELL_COUNT = 4;
    private const float ROCK_TOTAL_CELL_COUNT = 4;
    private const float LONGGRASS_TOTAL_CELL_COUNT = 6;

    [Serializable]
    // Is a data container so we may compare neighbouring grid cells when evaluating where tiles should be placed.
    // Stores info like the current height of the tile, the grass height of the tile, and whether a prop is present here or not.
    private class GridCell
    {
        public Vector3Int CellPosition;

        public int CellHeight = 0;
        public int GrassHeight = 0;
        public bool EnvironmentPropPresent = false;

        public GridCell(int cellHeight, int grassHeight, Vector3Int cellPosition)
        {
            CellHeight = cellHeight;
            GrassHeight = grassHeight;
            CellPosition = cellPosition;
        }
    }

    [SerializeField] private GridCell[,] waveCollapseFunctionGridCells;

    [Header("-- References --")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Cell2x2TileBaseCatalogue groundTile2x2Catalogue;

    [SerializeField] private Cell2x2TileBaseCatalogue cliffTile2x2Catalogue;
    [SerializeField] private Cell2x2TileBaseCatalogue cliffFaceTile2x2Catalogue;
    [SerializeField] private Tilemap[] cliffTilemaps;

    [SerializeField] private Cell2x2TileBaseCatalogue lowGrassTile2x2Catalogue;
    [SerializeField] private Cell2x2TileBaseCatalogue lowGrassEdgeTile2x2Catalogue;
    [SerializeField] private Tilemap[] lowGrassTilemaps;
    [SerializeField] private Cell2x2TileBaseCatalogue highGrassTile2x2Catalogue;
    [SerializeField] private Cell2x2TileBaseCatalogue highGrassEdgeTile2x2Catalogue;
    [SerializeField] private Tilemap[] highGrassTilemaps;

    [SerializeField] private Tilemap environmentTilemap;
    [SerializeField] private Tilemap environmentTilemapWithCollisions;
    [SerializeField] private Tilemap foliageTilemap;
    [SerializeField] private TileBase rockTileBase, tallgrasssTileBase;
    [SerializeField] private TileBase[] bushesTileBases;
    [SerializeField] private TileBase[] treeTrunkTileBases;
    [SerializeField] private TileBase[] treeFoliageTileBases;

    [Space(25)][Header("-- Generation Parameters --")]
    [Tooltip("The length of the grid that will be generated at runtime and when hotkeys are pressed.")]
    [Range(50, 400)][SerializeField] private int gridTargetLength = 50;
    [Tooltip("The height of the grid that will be generated at runtime and when hotkeys are pressed.")]
    [Range(50, 400)][SerializeField] private int gridTargetHeight = 50;

    [Tooltip("The percentage split of hopw many of the total tiles should be of each height value for the dirt. Values range from 0 [0f] to 100 [1f] percent. Tiles are painted and overpainted from the lowest level up.")]
    [Range(0f, 1f)][SerializeField] private float[] cellHeightCoveragePercentage = { 0.6f, 0.4f, 0.4f };
    [Tooltip("The ideal height a tile at this layer will try to be placed at. Values of zero favor the bottom of the tilemap whiel vcalues of 1 favor the top.")]
    [Range(0f, 1f)][SerializeField] private float[] cellHeightIdealHeight = { 0f, 0.5f, 1f };
    [Tooltip("The number of random positions that are rolled and compared per dirt tile chunk during generation to find the one closest to the ideal height. Higher values result in more consistent generation but increases computation time.")]
    [Range(2, 6)][SerializeField] private int cellHeightTotalRolls = 5;
    [Tooltip("The percentage chance we start painting a new tile chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellHeightNewChunkChance = 0.2f;
    private int cellHeightSteps = 3;

    [Tooltip("The percentage of the tile map each layer of painetd grass will try to fill in to occupy, starting from the low grass to the high.")]
    [Range(0f, 0.5f)][SerializeField] private float[] cellGrassCoveragePercentage = { 0.6f, 0.4f };
    [Tooltip("The percentage chance we start painting a new grass chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellGrassNewChunkChance = 0.2f;
    private int cellGrassSteps = 2;

    [Tooltip("The percentage of the tile map we will attempt to paint rocks over. Lower values [0.3f or less] since rocks bushes tall grass and trees share a tilemap")]
    [Range(0f, 0.3f)][SerializeField] private float cellRockCoveragePercentage = 0.05f; 
    [Tooltip("The percentage of the tile map we will attempt to paint bushes over. Lower values [0.3f or less] since rocks bushes tall grass and trees share a tilemap")]
    [Range(0f, 0.3f)][SerializeField] private float cellBushCoveragePercentage = 0.05f;
    [Tooltip("The percentage of the tile map we will attempt to paint Tall grass over over. Lower values [0.3f or less] since rocks bushes tall grass and trees share a tilemap")]
    [Range(0f, 0.3f)][SerializeField] private float cellTallGrassCoveragePercentage = 0.05f;
    [Tooltip("The percentage of the tile map we will attempt to paint varying trees over. Lower values [0.3f or less] since rocks bushes tall grass and trees share a tilemap")]
    [Range(0f, 0.3f)][SerializeField] private float cellTreeCoveragePercentage = 0.05f;

    [Tooltip("The percentage chance we start painting a new rock chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellRockNewChunkChance = 0.2f;
    [Tooltip("The percentage chance we start painting a new bush chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellBushNewChunkChance = 0.2f;
    [Tooltip("The percentage chance we start painting a new tall grass chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellTallGrassNewChunkChance = 0.2f;
    [Tooltip("The percentage chance we start painting a new tree chunk somewhere else on the tile map after placing a tile. Low values [0f - 0.1f] result in large masses and high values [0.5f+] result in spread out splotches")]
    [Range(0f, 1f)][SerializeField] private float cellTreeNewChunkChance = 0.2f;
    [Tooltip("The percentage chance that a spawned tree will be a round tree. low values [0f] favor pine trees whereas high values [0.9f+] favor round trees")]
    [Range(0f, 1f)][SerializeField] private float cellRoundTreeRollChance = 0.6f;

    private int gridCellCount = 0;
    private int gridLength = 0;
    private int gridHeight = 0;

    private int currentStepCountInGeneration = 0;

    #region User Inputs

    // Handles inputs for the user to generate a new level step by step to see the process or all at once.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Start();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            switch (currentStepCountInGeneration)
            {
                case 0:
                    InitializeGrid(gridTargetLength, gridTargetHeight);
                    SetGridCellHeights();
                    DrawDebugDirtTiles();
                    break;
                case 1:
                    PlugHolesInGridCellHeights(2);
                    DrawDebugDirtTiles();
                    break;
                case 2:
                    PlugHolesInGridCellHeights(1);
                    DrawDebugDirtTiles();
                    break;
                case 3:
                    DrawUniqueGroundTiles();
                    break;
                case 4:
                    DrawCliffOutlines(1);
                    break;
                case 5:
                    DrawCliffOutlines(2);
                    break;
                case 6:
                    SetGridCellGrassHeight();
                    PlugHolesInGridCellGrassHeights(2);
                    PlugHolesInGridCellGrassHeights(1);
                    PlugHolesInGridCellGrassHeights(0);
                    DrawUniqueGrassTiles();
                    break;
                case 7:
                    DrawGrassOutlines(0);
                    break;
                case 8:
                    DrawGrassOutlines(1);
                    break;
                case 9:
                    DrawGrassOutlines(2);
                    break;
                case 10:
                    DrawEnvironmentTrees();
                    break;
                case 11:
                    DrawEnvironmentLongGrass();
                    break;
                case 12:
                    DrawEnvironmentRocks();
                    break;
                case 13:
                    DrawEnvironmentBushes();
                    break;
                default:
                    break;
            }
            currentStepCountInGeneration++;
            if (currentStepCountInGeneration >= 14)
                currentStepCountInGeneration = 0;
        }
    }

    #endregion

    #region Initialization 

    // Generates a Level at Runtime.
    private void Start()
    {
        currentStepCountInGeneration = 0;

        InitializeGrid(gridTargetLength, gridTargetHeight);

        // Ground Generation
        SetGridCellHeights();
        PlugHolesInGridCellHeights(2);
        PlugHolesInGridCellHeights(1);
        PlugHolesInGridCellHeights(0);
        DrawUniqueGroundTiles();

        // Cliff Generation
        DrawCliffOutlines(1);
        DrawCliffOutlines(2);

        // Grass Generation
        SetGridCellGrassHeight();
        PlugHolesInGridCellGrassHeights(2);
        PlugHolesInGridCellGrassHeights(1);
        PlugHolesInGridCellGrassHeights(0);
        DrawUniqueGrassTiles();
        DrawGrassOutlines(0);
        DrawGrassOutlines(1);
        DrawGrassOutlines(2);

        // Environment Prop Generation
        DrawEnvironmentTrees();
        DrawEnvironmentLongGrass();
        DrawEnvironmentRocks();
        DrawEnvironmentBushes();
    }

    /// <summary>
    /// Reset every single tile map and the gridCells 2D array with new grid cells so we may store data in them during generation.
    /// </summary>
    /// <param name="length">The length of the new desired grid.</param>
    /// <param name="height">the height of the new desired grid.</param>
    private void InitializeGrid(int length, int height)
    {
        waveCollapseFunctionGridCells = new GridCell[length, height]; 
        gridLength = length;
        gridHeight = height;
        gridCellCount = length * height;

        // Assign new gridcells so we may manipulate them and store data in them during level generation.
        for (int x = 0; x < gridLength; x++)
            for (int y = 0; y < gridHeight; y++)
                waveCollapseFunctionGridCells[x, y] = new GridCell(0, 0, new Vector3Int(x, y));

        Vector3Int tilemapSize = new Vector3Int(length, height, 0);

        // Reset Every single tilemap to be empty, our desired size, and centered.
        ResetTilemap(groundTilemap, tilemapSize);
        foreach (Tilemap cliffTilemap in cliffTilemaps)
            ResetTilemap(cliffTilemap, tilemapSize);
        foreach (Tilemap lowGrassTilemap in lowGrassTilemaps)
            ResetTilemap(lowGrassTilemap, tilemapSize);
        foreach (Tilemap highGrassTilemap in highGrassTilemaps)
            ResetTilemap(highGrassTilemap, tilemapSize);
        ResetTilemap(environmentTilemap, tilemapSize);
        ResetTilemap(environmentTilemapWithCollisions, tilemapSize);
        ResetTilemap(foliageTilemap, tilemapSize);
    }

    /// <summary>
    /// Clear the tilemap and reset it;s size and recenter it.
    /// </summary>
    /// <param name="tilemap">the tilemap to reset</param>
    /// <param name="tilemapSize">the new desired size of the tilemap.</param>
    private void ResetTilemap(Tilemap tilemap, Vector3Int tilemapSize)
    {
        tilemap.ClearAllTiles();
        tilemap.size = tilemapSize;
        tilemap.transform.position = new Vector3(gridLength * -0.5f, gridHeight * -0.5f, 0);
    }

    #endregion

    #region Drawing Tiles

        #region Debug Drawing

    /// <summary>
    /// Solely used in the step by step generation. This fills in a 2x2 tile on the ground tilemap with color so we may visualize the heights of the dirt tiles.
    /// </summary>
    private void DrawDebugDirtTiles()
    {
        GridCell currentCell;
        for (int x = 0; x < gridLength; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                currentCell = waveCollapseFunctionGridCells[x, y];
                groundTilemap.SetTile(currentCell.CellPosition, GetBaseDirtTileByHeight(currentCell.CellHeight));
            }
    }

    /// <summary>
    /// Grabs the unique 2x2 debug dirt tile based on height to draw with no texture for visualizing dirt maps in step by step generation.
    /// </summary>
    /// <param name="cellHeight">the hegiht value of the debug dirt tile we want.</param>
    /// <returns></returns>
    private TileBase GetBaseDirtTileByHeight(int cellHeight)
    {
        TileBase tileToSet;
        switch (cellHeight)
        {
            case 0:
                tileToSet = groundTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(0).Grab2x2Tile(0).TileBase2x2[0];
                break;
            case 1:
                tileToSet = groundTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(1).Grab2x2Tile(0).TileBase2x2[0];
                break;
            case 2:
                tileToSet = groundTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(2).Grab2x2Tile(0).TileBase2x2[0];
                break;
            default:
                tileToSet = groundTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(0).Grab2x2Tile(0).TileBase2x2[0];
                break;
        }
        return tileToSet;
    }

    #endregion

        #region Ground and Grass Tiles

    /// <summary>
    /// Iterate through our grid cellHeights and draw a random weighted 2x2 tile from our catalogue on the apropriate tilemap.
    /// </summary>
    private void DrawUniqueGroundTiles()
    {
        Vector3Int targetCell;

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;

            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                targetCell = new Vector3Int(x, y);

                Draw2x2Cell(targetCell, groundTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(waveCollapseFunctionGridCells[targetCell.x, targetCell.y].CellHeight).GrabWeightedRandom2x2Tile().TileBase2x2, groundTilemap);
            }
        }
    }

    /// <summary>
    /// Iterate through our grid grassHeights and draw a random weighted 2x2 tile from our catalogue on the apropriate tilemap.
    /// </summary>
    private void DrawUniqueGrassTiles()
    {
        Vector3Int targetCell;
        int cellHeight = 0;
        Cell2x2TileBaseCatalogue cellTile2x2Catalogue;
        Tilemap tilemap;

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;

            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].GrassHeight == 0)
                    continue;

                targetCell = new Vector3Int(x, y);
                cellHeight = waveCollapseFunctionGridCells[x, y].CellHeight;
                cellTile2x2Catalogue = waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassTile2x2Catalogue : highGrassTile2x2Catalogue;
                tilemap = waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassTilemaps[cellHeight] : highGrassTilemaps[cellHeight];

                Draw2x2Cell(targetCell, cellTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).GrabWeightedRandom2x2Tile().TileBase2x2, tilemap);
            }
        }
    }

    #endregion

        #region Cliffs And Grass Outlines

    /// <summary>
    /// Evaluate the target cell to see what cliffs outlines we need to draw around it based on surrounding cells. There are many unique cases for this so this function is CHUNKY
    /// </summary>
    /// <param name="cellHeight">The cell height we are drawing cliffs faces around.</param>
    private void DrawCliffOutlines(int cellHeight)
    {
        Cell2x2TileBase cliffOutlineHorizontal = cliffTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(0);
        Cell2x2TileBase cliffOutlineVertical = cliffTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1);
        Cell2x2TileBase cliffOutlineOuterCorner = cliffTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2);
        Cell2x2TileBase cliffOutlineInnerCorner = cliffTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3);

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;

            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].CellHeight != cellHeight)
                    continue;

                // We have a cell that matches height here. Check cells around it to see if we have a border and if we need to draw a cliff.
                // Left check to see if it;s a real cell and lower then our cellHEight followed by inner corner check
                if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight)
                {
                    DrawOutlineTilesVertical(new Vector3Int(x - 2, y), false, cliffOutlineVertical, cliffTilemaps[cellHeight]);
                    if (y - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x - 2, y), true, false, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                    if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x - 2, y), true, true, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                }
                // Right check to see if it;s a real cell and lower then our cellHEight
                if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight)
                {
                    DrawOutlineTilesVertical(new Vector3Int(x + 2, y), true, cliffOutlineVertical, cliffTilemaps[cellHeight]);
                    if (y - 2 >= 0 && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x + 2, y), false, false, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                    if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x + 2, y), false, true, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                }
                // Down check to see if it;s a real cell and lower then our cellHEight
                if (y - 2 >= 0 && waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight)
                {
                    DrawOutlineTilesHorizontal(new Vector3Int(x, y - 2), false, cliffOutlineHorizontal, cliffTilemaps[cellHeight]);
                    if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y - 2), false, true, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                    if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y - 2), true, true, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);

                    // Draw the walls below this edge.
                    if (y - 4 < 0)
                    {
                        // Draw a half wall below us for the case of us being at the very bottom of the screen and out of index
                        Draw2x2Cell(new Vector3Int(x, y - 2), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1).TileBase2x2, cliffTilemaps[cellHeight - 1]);
                    }
                    else if (waveCollapseFunctionGridCells[x, y - 4].CellHeight < cellHeight)
                    {
                        // Draw a single wall or check below to see if i need to adda double.
                        if (y - 6 < 0 && waveCollapseFunctionGridCells[x, y - 4].CellHeight < waveCollapseFunctionGridCells[x, y].CellHeight - 1)
                        {
                            // we are at the bottom of the screen and need to only paint 3 lines

                            Draw2x2CellSideDependantOverpaint(new Vector3Int(x, y - 3), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1).TileBase2x2, cliffTilemaps[cellHeight], cliffTilemaps[cellHeight - 1],
                                !(x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight || x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight == cellHeight),
                                !(x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight == cellHeight));
                            Draw2x2Cell(new Vector3Int(x, y - 5), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2).TileBase2x2, cliffTilemaps[cellHeight - 1]);
                            cliffTilemaps[cellHeight - 1].SetTile(new Vector3Int(x, y - 5), null);
                            cliffTilemaps[cellHeight - 1].SetTile(new Vector3Int(x + 1, y - 5), null);
                        }
                        else if (y - 6 >= 0 && waveCollapseFunctionGridCells[x, y - 6].CellHeight < cellHeight - 1 && waveCollapseFunctionGridCells[x, y - 4].CellHeight == waveCollapseFunctionGridCells[x, y - 6].CellHeight)
                        {
                            // double wall
                            Draw2x2CellSideDependantOverpaint(new Vector3Int(x, y - 3), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1).TileBase2x2, cliffTilemaps[cellHeight], cliffTilemaps[cellHeight - 1],
                                !(x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight || x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight == cellHeight),
                                !(x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight == cellHeight));


                            if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight >= cellHeight - 1 || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight >= cellHeight - 1 ||
                                x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight ||
                                x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 6].CellHeight >= cellHeight - 1 || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 6].CellHeight >= cellHeight - 1)
                                Draw2x2CellNoOverpaint(new Vector3Int(x, y - 5), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2).TileBase2x2, cliffTilemaps[cellHeight - 1]);
                            else
                                Draw2x2Cell(new Vector3Int(x, y - 5), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2).TileBase2x2, cliffTilemaps[cellHeight - 1]);
                        }
                        else
                        {
                            // single wall
                            Draw2x2CellSideDependantOverpaint(new Vector3Int(x, y - 3), cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(0).TileBase2x2, cliffTilemaps[cellHeight], cliffTilemaps[cellHeight - 1],
                                !(x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight || x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight == cellHeight),
                                !(x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight == cellHeight));
                        }
                    }

                }
                // Up check to see if it;s a real cell and lower then our cellHEight
                if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight)
                {
                    DrawOutlineTilesHorizontal(new Vector3Int(x, y + 2), true, cliffOutlineHorizontal, cliffTilemaps[cellHeight]);
                    foreach (Tilemap cliffTilemap in cliffTilemaps)
                        DrawOutlineTilesHorizontal(new Vector3Int(x, y), false, null, cliffTilemap);
                    if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y + 2), false, false, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                    if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight == cellHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y + 2), true, false, cliffOutlineInnerCorner, cliffTilemaps[cellHeight]);
                }

                // Outer corners - check if three tiles are legal and if they are the proper height
                // Bottom left
                if (y - 2 >= 0 && x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight < cellHeight && waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight)
                {
                    DrawOutlineTileOuterCorner(new Vector3Int(x - 2, y - 2), true, true, cliffOutlineOuterCorner, cliffTilemaps[cellHeight]);
                    // Draw the cliff face below this // Draw the walls below this edge.

                    if (y - 4 < 0)
                    {
                        // Draw a half wall below us
                        DrawCliffCornerTiles(new Vector3Int(x - 2, y - 2), true, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(4), false, cliffTilemaps[cellHeight]);
                        cliffTilemaps[cellHeight].SetTile(new Vector3Int(x - 1, y - 3), null);
                    }
                    else if (y - 4 >= 0 && waveCollapseFunctionGridCells[x, y - 4].CellHeight < cellHeight)
                    {
                        if (waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight == cellHeight)
                            continue;
                        // Draw a single wall or check below to see if i need to adda double.
                        if (y - 6 < 0 && waveCollapseFunctionGridCells[x, y - 4].CellHeight < waveCollapseFunctionGridCells[x, y].CellHeight - 1)
                        {
                            // Draw a half wall below us
                            DrawCliffCornerTiles(new Vector3Int(x - 2, y - 2), true, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(4), true, cliffTilemaps[cellHeight]);
                            cliffTilemaps[cellHeight].SetTile(new Vector3Int(x - 1, y - 5), null);
                        }
                        else if (y - 6 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 6].CellHeight < cellHeight - 1 &&
                            (waveCollapseFunctionGridCells[x, y - 2].CellHeight != waveCollapseFunctionGridCells[x, y].CellHeight - 1 && waveCollapseFunctionGridCells[x, y - 4].CellHeight != waveCollapseFunctionGridCells[x, y].CellHeight - 1) &&
                            waveCollapseFunctionGridCells[x - 2, y - 4].CellHeight == waveCollapseFunctionGridCells[x - 2, y - 6].CellHeight &&
                            waveCollapseFunctionGridCells[x, y - 6].CellHeight == waveCollapseFunctionGridCells[x - 2, y - 6].CellHeight)
                        {
                            // Double wall
                            DrawCliffCornerTiles(new Vector3Int(x - 2, y - 2), true, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(4), true, cliffTilemaps[cellHeight]);
                        }
                        else
                        {
                            // Single wall
                            DrawCliffCornerTiles(new Vector3Int(x - 2, y - 2), true, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(4), false, cliffTilemaps[cellHeight]);
                        }
                    }
                }
                // Top Left
                if (y + 2 < gridHeight && x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight && waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight < cellHeight && waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight)
                    DrawOutlineTileOuterCorner(new Vector3Int(x - 2, y + 2), true, false, cliffOutlineOuterCorner, cliffTilemaps[cellHeight]);
                // Bottom right
                if (y - 2 >= 0 && x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight < cellHeight && waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight)
                {
                    DrawOutlineTileOuterCorner(new Vector3Int(x + 2, y - 2), false, true, cliffOutlineOuterCorner, cliffTilemaps[cellHeight]);
                    // Draw the cliff face below this // Draw the walls below this edge.
                    if (y - 4 < 0)
                    {
                        // Draw a half wall below us
                        DrawCliffCornerTiles(new Vector3Int(x + 2, y - 2), false, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3), false, cliffTilemaps[cellHeight]);
                        cliffTilemaps[cellHeight].SetTile(new Vector3Int(x + 2, y - 3), null);
                    }
                    else if (y - 4 >= 0 && waveCollapseFunctionGridCells[x, y - 4].CellHeight < cellHeight)
                    {
                        if (waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight == cellHeight)
                            continue;
                        // Draw a single wall or check below to see if i need to adda double.//draw a single wall or check below to see if i need to adda double.
                        if (y - 6 < 0 && waveCollapseFunctionGridCells[x, y - 4].CellHeight < waveCollapseFunctionGridCells[x, y].CellHeight - 1)
                        {
                            // Draw a half wall below us
                            DrawCliffCornerTiles(new Vector3Int(x + 2, y - 2), false, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3), true, cliffTilemaps[cellHeight]);
                            cliffTilemaps[cellHeight].SetTile(new Vector3Int(x + 2, y - 5), null);
                        }
                        else if (y - 6 >= 0 && waveCollapseFunctionGridCells[x + 2, y - 6].CellHeight < cellHeight - 1 &&
                            (waveCollapseFunctionGridCells[x, y - 2].CellHeight != waveCollapseFunctionGridCells[x, y].CellHeight - 1 && waveCollapseFunctionGridCells[x, y - 4].CellHeight != waveCollapseFunctionGridCells[x, y].CellHeight - 1) &&
                            waveCollapseFunctionGridCells[x + 2, y - 4].CellHeight == waveCollapseFunctionGridCells[x + 2, y - 6].CellHeight &&
                            waveCollapseFunctionGridCells[x, y - 6].CellHeight == waveCollapseFunctionGridCells[x + 2, y - 6].CellHeight)
                        {
                            // Double wall
                            DrawCliffCornerTiles(new Vector3Int(x + 2, y - 2), false, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3), true, cliffTilemaps[cellHeight]);
                        }
                        else
                        {
                            // Single wall
                            DrawCliffCornerTiles(new Vector3Int(x + 2, y - 2), false, cliffFaceTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3), false, cliffTilemaps[cellHeight]);
                        }
                    }
                }
                // Top right
                if (y + 2 < gridHeight && x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight && waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight < cellHeight && waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight)
                    DrawOutlineTileOuterCorner(new Vector3Int(x + 2, y + 2), false, false, cliffOutlineOuterCorner, cliffTilemaps[cellHeight]);

            }
        }
    }

    /// <summary>
    /// Evaluate the target cell to see what grass outlines we need to draw around it based on surrounding cells. Grass outlines are drawn if the tiles is lower than us
    /// OR if the tile is the same height as us but a different grass height.
    /// </summary>
    /// <param name="cellHeight"></param>
    private void DrawGrassOutlines(int cellHeight)
    {
        Cell2x2TileBase lowGrassOutlineHorizontal = lowGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(0);
        Cell2x2TileBase lowGrassOutlineVertical = lowGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1);
        Cell2x2TileBase lowGrassOutlineOuterCorner = lowGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2);
        Cell2x2TileBase lowGrassOutlineInnerCorner = lowGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3);
        Cell2x2TileBase highGrassOutlineHorizontal = highGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(0);
        Cell2x2TileBase highGrassOutlineVertical = highGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(1);
        Cell2x2TileBase highGrassOutlineOuterCorner = highGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(2);
        Cell2x2TileBase highGrassOutlineInnerCorner = highGrassEdgeTile2x2Catalogue.Grab2x2TileBaseSheetWithHeightValue(cellHeight).Grab2x2Tile(3);

        Tilemap grassTilemap;
        int grassHeight = 0;

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;

            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].CellHeight != cellHeight)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].GrassHeight == 0)
                    continue;

                grassHeight = waveCollapseFunctionGridCells[x, y].GrassHeight;
                grassTilemap = waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassTilemaps[cellHeight] : highGrassTilemaps[cellHeight];

                // We have a cell that matches height here. Check cells around it to see if we have a border and if we need to draw a cliff.
                // Left check to see if it;s a real cell and lower then our cellHEight followed by inner corner check
                if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight || x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].CellHeight == cellHeight && x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y].GrassHeight != grassHeight)
                {
                    DrawOutlineTilesVertical(new Vector3Int(x - 2, y), false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineVertical : highGrassOutlineVertical, grassTilemap);
                    if (y - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight && y - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x - 2, y), true, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                    if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight == cellHeight && y + 2 < gridHeight && waveCollapseFunctionGridCells[x - 2, y + 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x - 2, y), true, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                }
                // Right check to see if it;s a real cell and lower then our cellHEight
                if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight || x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].CellHeight == cellHeight && x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y].GrassHeight != grassHeight)
                {
                    DrawOutlineTilesVertical(new Vector3Int(x + 2, y), true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineVertical : highGrassOutlineVertical, grassTilemap);
                    if (y - 2 >= 0 && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight && y - 2 >= 0 && waveCollapseFunctionGridCells[x + 2, y - 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x + 2, y), false, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                    if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight == cellHeight && y + 2 < gridHeight && waveCollapseFunctionGridCells[x + 2, y + 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x + 2, y), false, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                }
                // Down check to see if it;s a real cell and lower then our cellHEight
                if (y - 2 >= 0 && waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight || y - 2 >= 0 && waveCollapseFunctionGridCells[x, y - 2].CellHeight == cellHeight && y - 2 >= 0 && waveCollapseFunctionGridCells[x, y - 2].GrassHeight != grassHeight)
                {
                    DrawOutlineTilesHorizontal(new Vector3Int(x, y - 2), false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineHorizontal : highGrassOutlineHorizontal, grassTilemap);
                    if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight && x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y - 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y - 2), false, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                    if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight && x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y - 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y - 2), true, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                }
                // Up check to see if it;s a real cell and lower then our cellHEight
                if (y + 2 < gridHeight && waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight || y + 2 < gridHeight && waveCollapseFunctionGridCells[x, y + 2].CellHeight == cellHeight && y + 2 < gridHeight && waveCollapseFunctionGridCells[x, y + 2].GrassHeight != grassHeight)
                {
                    DrawOutlineTilesHorizontal(new Vector3Int(x, y + 2), true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineHorizontal : highGrassOutlineHorizontal, grassTilemap);
                    if (x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight == cellHeight && x - 2 >= 0 && waveCollapseFunctionGridCells[x - 2, y + 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y + 2), false, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                    if (x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight == cellHeight && x + 2 < gridLength && waveCollapseFunctionGridCells[x + 2, y + 2].GrassHeight == grassHeight)
                        DrawOutlineTileInnerCorner(new Vector3Int(x, y + 2), true, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineInnerCorner : highGrassOutlineInnerCorner, grassTilemap);
                }

                // Outer corners - check if three tiles are legal and if they are the proper height
                // Bottom left
                if (x - 2 >= 0 && y - 2 >= 0 && (waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight || waveCollapseFunctionGridCells[x - 2, y].CellHeight == cellHeight && waveCollapseFunctionGridCells[x - 2, y].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x - 2, y - 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x - 2, y - 2].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x, y - 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x, y - 2].GrassHeight < grassHeight))
                    DrawOutlineTileOuterCorner(new Vector3Int(x - 2, y - 2), true, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineOuterCorner : highGrassOutlineOuterCorner, grassTilemap);
                // Top Left
                if (x - 2 >= 0 && y + 2 < gridHeight && (waveCollapseFunctionGridCells[x - 2, y].CellHeight < cellHeight || waveCollapseFunctionGridCells[x - 2, y].CellHeight == cellHeight && waveCollapseFunctionGridCells[x - 2, y].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x - 2, y + 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x - 2, y + 2].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x, y + 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x, y + 2].GrassHeight < grassHeight))
                    DrawOutlineTileOuterCorner(new Vector3Int(x - 2, y + 2), true, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineOuterCorner : highGrassOutlineOuterCorner, grassTilemap);
                // Bottom right
                if (x + 2 < gridLength && y - 2 >= 0 && (waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight || waveCollapseFunctionGridCells[x + 2, y].CellHeight == cellHeight && waveCollapseFunctionGridCells[x + 2, y].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x + 2, y - 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x + 2, y - 2].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x, y - 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x, y - 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x, y - 2].GrassHeight < grassHeight))
                    DrawOutlineTileOuterCorner(new Vector3Int(x + 2, y - 2), false, true, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineOuterCorner : highGrassOutlineOuterCorner, grassTilemap);
                // Top right
                if (x + 2 < gridLength && y + 2 < gridHeight && (waveCollapseFunctionGridCells[x + 2, y].CellHeight < cellHeight || waveCollapseFunctionGridCells[x + 2, y].CellHeight == cellHeight && waveCollapseFunctionGridCells[x + 2, y].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x + 2, y + 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x + 2, y + 2].GrassHeight < grassHeight) && (waveCollapseFunctionGridCells[x, y + 2].CellHeight < cellHeight || waveCollapseFunctionGridCells[x, y + 2].CellHeight == cellHeight && waveCollapseFunctionGridCells[x, y + 2].GrassHeight < grassHeight))
                    DrawOutlineTileOuterCorner(new Vector3Int(x + 2, y + 2), false, false, waveCollapseFunctionGridCells[x, y].GrassHeight == 1 ? lowGrassOutlineOuterCorner : highGrassOutlineOuterCorner, grassTilemap);
            }
        }
    }

    /// <summary>
    /// Draw the corenr cliff tiles below a cliff corner 
    /// </summary>
    /// <param name="targetCoordinates">the target we are drawing the corner tiles from.</param>
    /// <param name="rightside"> are the cliff corner tiles on the righside of the 2x2 cells</param>
    /// <param name="cornerTiles"> the tilebases we are using to draw this corner tile</param>
    /// <param name="doubleTall"> IS this cliff corner tile 2 high</param>
    /// <param name="cliffTilemap">The tilemap we are drawing on</param>
    private void DrawCliffCornerTiles(Vector3Int targetCoordinates, bool rightside, Cell2x2TileBase cornerTiles, bool doubleTall, Tilemap cliffTilemap)
    {
        if (rightside)
        {
            cliffTilemap.SetTile(targetCoordinates + Vector3Int.right, cornerTiles.TileBase2x2[3]);
            if (doubleTall)
            {
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.right + Vector3Int.down, cornerTiles.TileBase2x2[2]);
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.right + Vector3Int.down * 2, cornerTiles.TileBase2x2[1]);
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.right + Vector3Int.down * 3, cornerTiles.TileBase2x2[0]);
            }
            else
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.right + Vector3Int.down, cornerTiles.TileBase2x2[0]);
        }
        else
        {
            cliffTilemap.SetTile(targetCoordinates, cornerTiles.TileBase2x2[3]);
            if (doubleTall)
            {
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.down, cornerTiles.TileBase2x2[2]);
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.down * 2, cornerTiles.TileBase2x2[1]);
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.down * 3, cornerTiles.TileBase2x2[0]);
            }
            else
                cliffTilemap.SetTile(targetCoordinates + Vector3Int.down, cornerTiles.TileBase2x2[0]);
        }
    }

        #endregion

        #region Basic Tile Drawing

    /// <summary>
    /// Draws our tilebase at the target coordinate and the three other connected cells. Overpaints on any tiles already present
    /// </summary>
    /// <param name="targetCoordinates">the target to start drawing at</param>
    /// <param name="tileBases">the array of tile bases we are darawing</param>
    /// <param name="tilemapToPaint">the tilemap we are drawing on</param>
    private void Draw2x2Cell(Vector3Int targetCoordinates, TileBase[] tileBases, Tilemap tilemapToPaint)
    {
        tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up, tileBases[0]);
        tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, tileBases[1]);
        tilemapToPaint.SetTile(targetCoordinates, tileBases[2]);
        tilemapToPaint.SetTile(targetCoordinates + Vector3Int.right, tileBases[3]);
    }

    /// <summary>
    /// Draws our tilebases at the target coordinate and the three other connected cells. Does not overpaint on any tile that is already present on the tilemap
    /// </summary>
    /// <param name="targetCoordinates">the target to start drawing at</param>
    /// <param name="tileBases">the array of tile bases we are drawing at</param>
    /// <param name="tilemapToPaint">the tilemap we are drawing on</param>
    private void Draw2x2CellNoOverpaint(Vector3Int targetCoordinates, TileBase[] tileBases, Tilemap tilemapToPaint)
    {
        if (!(tilemapToPaint.HasTile(targetCoordinates + Vector3Int.up) && tilemapToPaint.HasTile(targetCoordinates)))
        {
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up, tileBases[0]);
            tilemapToPaint.SetTile(targetCoordinates, tileBases[2]);
        }
        if (!(tilemapToPaint.HasTile(targetCoordinates + Vector3Int.up + Vector3Int.right) && tilemapToPaint.HasTile(targetCoordinates + Vector3Int.right)))
        {
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, tileBases[1]);
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.right, tileBases[3]);
        }
    }

    /// <summary>
    /// Draws our 2x2 cell at the target location, but if a side would overpaint, paints that side on a different background tilemap if present
    /// </summary>
    /// <param name="targetCoordinates">the target coordinate we are painting the tiles at</param>
    /// <param name="tileBases">the tile bases we are paitning on the tilemap</param>
    /// <param name="tilemapToPaint">the primary tilemap to paint. If it's already painted and we don't overpaint we ignore it.</param>
    /// <param name="backgroundTilemap">the secondary tilemap to paint on if given and we choose top ignore overpaint</param>
    /// <param name="leftSideOverpaints">the boolean telling us if the left side should overpaint or not.</param>
    /// <param name="rightSideOverpaints">the boolean telling us if the right side should overpaint or not.</param>
    private void Draw2x2CellSideDependantOverpaint(Vector3Int targetCoordinates, TileBase[] tileBases, Tilemap tilemapToPaint, Tilemap backgroundTilemap, bool leftSideOverpaints, bool rightSideOverpaints)
    {
        if (!(tilemapToPaint.HasTile(targetCoordinates + Vector3Int.up) && tilemapToPaint.HasTile(targetCoordinates)) || leftSideOverpaints)
        {
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up, tileBases[0]);
            tilemapToPaint.SetTile(targetCoordinates, tileBases[2]);
        }
        else if (backgroundTilemap != null)
        {
            backgroundTilemap.SetTile(targetCoordinates + Vector3Int.up, tileBases[0]);
            backgroundTilemap.SetTile(targetCoordinates, tileBases[2]);
        }
        if (!(tilemapToPaint.HasTile(targetCoordinates + Vector3Int.up + Vector3Int.right) && tilemapToPaint.HasTile(targetCoordinates + Vector3Int.right)) || rightSideOverpaints)
        {
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, tileBases[1]);
            tilemapToPaint.SetTile(targetCoordinates + Vector3Int.right, tileBases[3]);
        }
        else if (backgroundTilemap != null)
        {
            backgroundTilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, tileBases[1]);
            backgroundTilemap.SetTile(targetCoordinates + Vector3Int.right, tileBases[3]);
        }
    }

        #endregion

        #region Environmental Prop Painting

    /// <summary>
    /// Iterates through our tree count, drawing trees in valid location in chunks until we run out of trees.
    /// Randomly chooses which tree to paint, and paints the canapies of the trees on their own unique layer.
    /// </summary>
    private void DrawEnvironmentTrees()
    {
        int targetTreeCount = (int)(gridCellCount * cellTreeCoveragePercentage / TREE_TOTAL_CELL_COUNT);

        while (targetTreeCount > 0)
        {
            Vector3Int chosenChunkSpawn = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

            bool layingTilesInChunk = true;

            while (layingTilesInChunk && targetTreeCount > 0)
            {
                Vector3Int[] tilesToEvalAndPaint = { new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x + 2, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x - 1, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x - 2, chosenChunkSpawn.y),
                                                     new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y + 1), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y + 1), new Vector3Int(chosenChunkSpawn.x + 2, chosenChunkSpawn.y + 1), new Vector3Int(chosenChunkSpawn.x - 1, chosenChunkSpawn.y + 1), new Vector3Int(chosenChunkSpawn.x - 2, chosenChunkSpawn.y + 1)};
                // Check if this spot is legal for a tree.
                if (CheckIfLegalEnvironmentPropSpot(tilesToEvalAndPaint))
                {
                    int treeIndex = UnityEngine.Random.Range(0f, 1f) < cellRoundTreeRollChance ? 0 : 1;

                    DrawEnvironmentProp(treeTrunkTileBases[treeIndex], environmentTilemapWithCollisions, tilesToEvalAndPaint);
                    DrawEnvironmentProp(treeFoliageTileBases[treeIndex], foliageTilemap, tilesToEvalAndPaint);
                    targetTreeCount--;
                }

                if (UnityEngine.Random.Range(0, 1f) < cellTreeNewChunkChance)
                    layingTilesInChunk = false;
                else
                    chosenChunkSpawn = GetVariableNextChunkSpawnByPrevious(chosenChunkSpawn, 5, 5);
            }
        }
    }

    /// <summary>
    /// Iterates through our bush count, drawing bushes by picking a random tilebase from an array and placing it in valid location in chunks until we run out of bushes.
    /// </summary>
    private void DrawEnvironmentBushes()
    {
        int environmentTileCount = (int)(gridCellCount * cellBushCoveragePercentage / BUSH_TOTAL_CELL_COUNT);

        while (environmentTileCount > 0)
        {
            Vector3Int chosenChunkSpawn = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

            bool layingTilesInChunk = true;

            while (layingTilesInChunk && environmentTileCount > 0)
            {
                Vector3Int[] tilesToEvalAndPaint = { new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y) };
                // Check if this spot is legal for a tree.
                if (CheckIfLegalEnvironmentPropSpot(tilesToEvalAndPaint))
                {
                    int bushTileBaseIndex = UnityEngine.Random.Range(0, bushesTileBases.Length);

                    DrawEnvironmentProp(bushesTileBases[bushTileBaseIndex], environmentTilemap, tilesToEvalAndPaint);
                    environmentTileCount--;
                }

                if (UnityEngine.Random.Range(0, 1f) < cellBushNewChunkChance)
                    layingTilesInChunk = false;
                else
                    chosenChunkSpawn = GetVariableNextChunkSpawnByPrevious(chosenChunkSpawn, 5, 5);
            }
        }
    }

    /// <summary>
    /// Iterates through our rock count, drawing rocks in valid location in chunks until we run out of rocks.
    /// </summary>
    private void DrawEnvironmentRocks()
    {
        int environmentTileCount = (int)(gridCellCount * cellRockCoveragePercentage / ROCK_TOTAL_CELL_COUNT);

        while (environmentTileCount > 0)
        {
            Vector3Int chosenChunkSpawn = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

            bool layingTilesInChunk = true;

            while (layingTilesInChunk && environmentTileCount > 0)
            {
                Vector3Int[] tilesToEvalAndPaint = { new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y) };
                // Check if this spot is legal for a tree.
                if (CheckIfLegalEnvironmentPropSpot(tilesToEvalAndPaint))
                {
                    DrawEnvironmentProp(rockTileBase, environmentTilemapWithCollisions, tilesToEvalAndPaint);
                    environmentTileCount--;
                }

                if (UnityEngine.Random.Range(0, 1f) < cellRockNewChunkChance)
                    layingTilesInChunk = false;
                else
                    chosenChunkSpawn = GetVariableNextChunkSpawnByPrevious(chosenChunkSpawn, 1, 1);
            }
        }
    }

    /// <summary>
    /// Iterates through our long grass count, drawing long grass patches in valid location in chunks until we run out of long grass.
    /// </summary>
    private void DrawEnvironmentLongGrass()
    {
        int environmentTileCount = (int)(gridCellCount * cellTallGrassCoveragePercentage / LONGGRASS_TOTAL_CELL_COUNT);

        while (environmentTileCount > 0)
        {
            Vector3Int chosenChunkSpawn = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

            bool layingTilesInChunk = true;

            while (layingTilesInChunk && environmentTileCount > 0)
            {
                Vector3Int[] tilesToEvalAndPaint = { new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y), new Vector3Int(chosenChunkSpawn.x, chosenChunkSpawn.y + 1), new Vector3Int(chosenChunkSpawn.x + 1, chosenChunkSpawn.y + 1) };
                // Check if this spot is legal for a tree.
                if (CheckIfLegalEnvironmentPropSpot(tilesToEvalAndPaint))
                {
                    DrawEnvironmentProp(tallgrasssTileBase, environmentTilemap, tilesToEvalAndPaint);
                    environmentTileCount--;
                }

                if (UnityEngine.Random.Range(0, 1f) < cellTallGrassNewChunkChance)
                    layingTilesInChunk = false;
                else
                    chosenChunkSpawn = GetVariableNextChunkSpawnByPrevious(chosenChunkSpawn, 1, 1);
            }
        }
    }

    /// <summary>
    /// Draws the chosen tilebase onto the chosen tilemap then sets the corresponding grid bools to ensure no other props are painted there.
    /// </summary>
    /// <param name="tileToDraw">The tileBase to draw</param>
    /// <param name="tileMapToDrawOn">The tileMap to draw on</param>
    /// <param name="GridCoordinatesToTickOff">The array of grid coordinates to update the bools on.</param>
    private void DrawEnvironmentProp(TileBase tileToDraw, Tilemap tileMapToDrawOn, Vector3Int[] GridCoordinatesToTickOff)
    {
        tileMapToDrawOn.SetTile(GridCoordinatesToTickOff[0], tileToDraw);

        foreach (Vector3Int Cell in GridCoordinatesToTickOff)
            waveCollapseFunctionGridCells[Cell.x, Cell.y].EnvironmentPropPresent = true;
    }

    #endregion

        #region Outline Drawing

    /// <summary>
    /// Draws a vertical outline tile on the chosen 2x2 tile on either the left or right side.
    /// </summary>
    /// <param name="targetCoordinates">the 2x2 cell we are targetting to paint</param>
    /// <param name="rightside">are we painting on the rightside of this 2x2 or the left.</param>
    /// <param name="outlineTiles">The tilebases we want to paint with</param>
    /// <param name="tilemap">the tilemap we are painting on</param>
    private void DrawOutlineTilesVertical(Vector3Int targetCoordinates, bool rightside, Cell2x2TileBase outlineTiles, Tilemap tilemap)
    {
        if (rightside)
        {
            tilemap.SetTile(targetCoordinates, outlineTiles.TileBase2x2[1]);
            tilemap.SetTile(targetCoordinates + Vector3Int.up, outlineTiles.TileBase2x2[3]);
        }
        else
        {
            tilemap.SetTile(targetCoordinates + Vector3Int.right, outlineTiles.TileBase2x2[0]);
            tilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, outlineTiles.TileBase2x2[2]);
        }

    }

    /// <summary>
    /// Draws a horizontal outline tile on the chosen 2x2 tile on either the top or bottom side.
    /// </summary>
    /// <param name="targetCoordinates">the 2x2 cell we are targetting to paint</param>
    /// <param name="topside">are we painting on the topside of this 2x2 or the bottom</param>
    /// <param name="outlineTiles">The tilebases we want to paint with</param>
    /// <param name="tilemap">the tilemap we are painting on</param>
    private void DrawOutlineTilesHorizontal(Vector3Int targetCoordinates, bool topside, Cell2x2TileBase outlineTiles, Tilemap tilemap)
    {
        if (topside)
        {
            if (outlineTiles != null)
            {
                tilemap.SetTile(targetCoordinates, outlineTiles.TileBase2x2[2]);
                tilemap.SetTile(targetCoordinates + Vector3Int.right, outlineTiles.TileBase2x2[3]);
            }
            else
            {
                tilemap.SetTile(targetCoordinates, null);
                tilemap.SetTile(targetCoordinates + Vector3Int.right, null);
            }
        }
        else
        {
            if (outlineTiles != null)
            {
                tilemap.SetTile(targetCoordinates + Vector3Int.up, outlineTiles.TileBase2x2[0]);
                tilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, outlineTiles.TileBase2x2[1]);
            }
            else
            {
                tilemap.SetTile(targetCoordinates + Vector3Int.up, null);
                tilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, null);
            }
        }

    }

    /// <summary>
    /// Draw an outer corner tile on the proper tile in this 2x2 cluster.
    /// </summary>
    /// <param name="targetCoordinates">the 2x2 cell we are targetting to paint</param>
    /// <param name="rightside">are we painting on the rightside of this 2x2 or the left.</param>
    /// <param name="topside">are we painting on the topside of this 2x2 or the bottom</param>
    /// <param name="outlineTiles">The tilebases we want to paint with</param>
    /// <param name="tilemap">the tilemap we are painting on</param>
    private void DrawOutlineTileOuterCorner(Vector3Int targetCoordinates, bool rightside, bool topside, Cell2x2TileBase outlineTiles, Tilemap tilemap)
    {
        if (rightside)
        {
            if (topside)
                tilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, outlineTiles.TileBase2x2[0]);
            else
                tilemap.SetTile(targetCoordinates + Vector3Int.right, outlineTiles.TileBase2x2[2]);
        }
        else
        {
            if (topside)
                tilemap.SetTile(targetCoordinates + Vector3Int.up, outlineTiles.TileBase2x2[1]);
            else
                tilemap.SetTile(targetCoordinates, outlineTiles.TileBase2x2[3]);
        }
    }

    /// <summary>
    /// Draw an inner corner tile on the proper tile in this 2x2 cluster.
    /// </summary>
    /// <param name="targetCoordinates">the 2x2 cell we are targetting to paint</param>
    /// <param name="rightside">are we painting on the rightside of this 2x2 or the left.</param>
    /// <param name="topside">are we painting on the topside of this 2x2 or the bottom</param>
    /// <param name="outlineTiles">The tilebases we want to paint with</param>
    /// <param name="tilemap">the tilemap we are painting on</param>
    private void DrawOutlineTileInnerCorner(Vector3Int targetCoordinates, bool rightside, bool topside, Cell2x2TileBase outlineTiles, Tilemap tilemap)
    {
        if (rightside)
        {
            if (topside)
                tilemap.SetTile(targetCoordinates + Vector3Int.up + Vector3Int.right, outlineTiles.TileBase2x2[3]);
            else
                tilemap.SetTile(targetCoordinates + Vector3Int.right, outlineTiles.TileBase2x2[1]);
        }
        else
        {
            if (topside)
                tilemap.SetTile(targetCoordinates + Vector3Int.up, outlineTiles.TileBase2x2[2]);
            else
                tilemap.SetTile(targetCoordinates, outlineTiles.TileBase2x2[0]);
        }
    }

        #endregion

    #endregion

    #region Grid Data Generation

    /// <summary>
    /// Starts laying down layers of new cell heights into our grid. Does not draw anything on the tilemaps.
    /// </summary>
    private void SetGridCellHeights()
    {
        for (int heightLayer = 1; heightLayer < cellHeightSteps; heightLayer++)
        {
            int targetCellsToPaint = (int)(cellHeightCoveragePercentage[heightLayer] * gridCellCount);

            while (targetCellsToPaint > 0)
            {
                // Roll the initial cells
                Vector3Int[] initialChunkSpawns = new Vector3Int[cellHeightTotalRolls];
                for (int i = 0; i < cellHeightTotalRolls; i++)
                    initialChunkSpawns[i] = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

                // Pick the most desirable cell closest to the target position on the grid based on height.
                Vector3Int chosenChunkSpawn = initialChunkSpawns[0];
                for (int spawnIndex = 1; spawnIndex < initialChunkSpawns.Length; spawnIndex++)
                {
                    if (Mathf.Abs(chosenChunkSpawn.y - cellHeightIdealHeight[heightLayer] * gridHeight) > Mathf.Abs(initialChunkSpawns[spawnIndex].y - cellHeightIdealHeight[heightLayer] * gridHeight))
                    {
                        chosenChunkSpawn = initialChunkSpawns[spawnIndex];
                    }
                }

                // Start laying down 2x2 tiles of this cell height until we run out of tiles to place or roll a new chunk
                bool layingTilesInChunk = true;
                while (layingTilesInChunk && targetCellsToPaint > 0)
                {
                    if (SetCellAndConnectedToGroundHeight(chosenChunkSpawn, heightLayer))
                        targetCellsToPaint -= 4;

                    if (UnityEngine.Random.Range(0, 1f) < cellHeightNewChunkChance)
                        layingTilesInChunk = false;
                    else
                        chosenChunkSpawn = GetNextChunkSpawnByPrevious(chosenChunkSpawn, 2, 2);
                }
            }
        }
    }

    /// <summary>
    /// Starts laying down grass Height layers into our grid. Does not draw any of the grass heights yet.
    /// </summary>
    private void SetGridCellGrassHeight()
    {
        for (int heightLayer = 1; heightLayer < cellGrassSteps + 1; heightLayer++)
        {
            int targetCellsToPaint = (int)(cellGrassCoveragePercentage[heightLayer - 1] * gridCellCount);

            while (targetCellsToPaint > 0)
            {
                // Roll the initial cell
                Vector3Int chosenChunkSpawn = new Vector3Int(UnityEngine.Random.Range(0, (gridLength - 1) / 2) * 2, UnityEngine.Random.Range(0, (gridHeight - 1) / 2) * 2, 0);

                // Start laying down 2x2 tiles of this grass height until we run out of tiles to place or roll a new chunk
                bool layingTilesInChunk = true;
                while (layingTilesInChunk && targetCellsToPaint > 0)
                {
                    if (SetCellAndConnectedToGrassHeight(chosenChunkSpawn, heightLayer))
                        targetCellsToPaint -= 4;

                    if (UnityEngine.Random.Range(0, 1f) < cellGrassNewChunkChance)
                        layingTilesInChunk = false;
                    else
                        chosenChunkSpawn = GetNextChunkSpawnByPrevious(chosenChunkSpawn, 2, 2);
                }
            }
        }
    }

    /// <summary>
    /// Goes through every 2x2 cell in our grid to see if this is a single cell with less than 1 matching surrounding neighbours. If there is one or less matching neighbour
    /// This cell is then painted to join the height level were painting at and we cascade through until we hit multiple matching neighbours or all cells are painted.
    /// This ensures we avoid single outlier cells and makles the ground generation more uniform.
    /// </summary>
    /// <param name="cellHeight"> The current cell height we are paitning single tiles to match to.</param>
    private void PlugHolesInGridCellHeights(int cellHeight)
    {
        // Iterate through the 2x2 chunks, if one is alone and surrounded by the color we want to replace it with, color it in.
        // These are cached here instead of in the for loop to avoid excessive garbage collection through massive iteration of new variables that are then disposed of.
        Vector3Int targetCell;
        List<Vector3Int> neighbouringTiles;
        List<Vector3Int> neighbouringTilesColorMatch;
        bool finishedHoleFillingCascade = false;
        bool neighbouringTileMatchesTargetHeight = false;

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;
            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].CellHeight == cellHeight)
                    continue;

                targetCell = new Vector3Int(x, y);

                // Before we cascade we need to see if this unmatching tile has at least one neighbour thats matches target height.
                neighbouringTileMatchesTargetHeight = false;
                neighbouringTiles = GrabEdgeConciousNeighbourTiles(targetCell);
                foreach (Vector3Int neighbourTile in neighbouringTiles)
                {
                    if (waveCollapseFunctionGridCells[neighbourTile.x, neighbourTile.y].CellHeight == cellHeight)
                        neighbouringTileMatchesTargetHeight = true;
                }
                if (!neighbouringTileMatchesTargetHeight)
                    continue;

                // We then check if the neighbouring tiles has only one single matching color tile to itself.
                // If so we paint this tile our new color and continue our evaluation of the next tile in the cascade.
                finishedHoleFillingCascade = false;
                while (!finishedHoleFillingCascade)
                {
                    neighbouringTiles = GrabEdgeConciousNeighbourTiles(targetCell);

                    neighbouringTilesColorMatch = new List<Vector3Int>();
                    foreach (Vector3Int neighbourCoordinates in neighbouringTiles)
                        if (waveCollapseFunctionGridCells[neighbourCoordinates.x, neighbourCoordinates.y].CellHeight == waveCollapseFunctionGridCells[targetCell.x, targetCell.y].CellHeight)
                            neighbouringTilesColorMatch.Add(neighbourCoordinates);

                    // Set the tile data and set our exit conditions depending on count of leftover tiles to paint.
                    if (neighbouringTilesColorMatch.Count < 2)
                    {
                        SetCellAndConnectedToGroundHeight(targetCell, cellHeight);
                        if (neighbouringTilesColorMatch.Count == 1)
                            targetCell = neighbouringTilesColorMatch[0];
                        else
                            finishedHoleFillingCascade = true;
                    }
                    else
                        finishedHoleFillingCascade = true;
                }
            }
        }
    }

    /// <summary>
    /// Goes through every 2x2 cell in our grid to see if this is a single cell with less than 1 matching surrounding neighbours. If there is one or less matching neighbour
    /// This cell is then painted to join the grass level were painting at and we cascade through until we hit multiple matching neighbours or all cells are painted.
    /// This ensures we avoid single outlier cells and makles the ground generation more uniform.
    /// </summary>
    /// <param name="grassHeight">The current grass height we are paitning single tiles to match to.</param>
    private void PlugHolesInGridCellGrassHeights(int grassHeight)
    {
        // Iterate through the 2x2 chunks, if one is alone and surrounded by the color we want to replace it with, color it in.
        // These are cached here instead of in the for loop to avoid excessive garbage collection through massive iteration of new variables that are then disposed of.
        Vector3Int targetCell;
        List<Vector3Int> neighbouringTiles;
        List<Vector3Int> neighbouringTilesColorMatch;
        bool finishedHoleFillingCascade = false;
        bool neighbouringTileMatchesTargetHeight = false;

        for (int x = 0; x < gridLength; x++)
        {
            if (x % 2 == 1)
                continue;

            for (int y = 0; y < gridHeight; y++)
            {
                if (y % 2 == 1)
                    continue;
                if (waveCollapseFunctionGridCells[x, y].GrassHeight == grassHeight)
                    continue;

                targetCell = new Vector3Int(x, y);

                // Before we cascade we need to see if this unmatching tile has at least one neighbour thats matches target height.
                neighbouringTileMatchesTargetHeight = false;
                neighbouringTiles = GrabEdgeConciousNeighbourTiles(targetCell);
                foreach (Vector3Int neighbourTile in neighbouringTiles)
                {
                    if (waveCollapseFunctionGridCells[neighbourTile.x, neighbourTile.y].GrassHeight == grassHeight)
                        neighbouringTileMatchesTargetHeight = true;
                }
                if (!neighbouringTileMatchesTargetHeight)
                    continue;

                // We then check if the neighbouring tiles has only one single matching color tile to itself.
                // If so we paint this tile our new color and continue our evaluation of the next tile in the cascade.
                finishedHoleFillingCascade = false;
                while (!finishedHoleFillingCascade)
                {
                    neighbouringTiles = GrabEdgeConciousNeighbourTiles(targetCell);

                    neighbouringTilesColorMatch = new List<Vector3Int>();
                    foreach (Vector3Int neighbourCoordinates in neighbouringTiles)
                        if (waveCollapseFunctionGridCells[neighbourCoordinates.x, neighbourCoordinates.y].GrassHeight == waveCollapseFunctionGridCells[targetCell.x, targetCell.y].GrassHeight && waveCollapseFunctionGridCells[neighbourCoordinates.x, neighbourCoordinates.y].CellHeight == waveCollapseFunctionGridCells[targetCell.x, targetCell.y].CellHeight)
                            neighbouringTilesColorMatch.Add(neighbourCoordinates);

                    // Set the tile data and set our exit conditions depending on count of leftover tiles to paint.
                    if (neighbouringTilesColorMatch.Count < 2)
                    {
                        SetCellAndConnectedToGrassHeight(targetCell, grassHeight);
                        if (neighbouringTilesColorMatch.Count == 1)
                            targetCell = neighbouringTilesColorMatch[0];
                        else
                            finishedHoleFillingCascade = true;
                    }
                    else
                        finishedHoleFillingCascade = true;
                }
            }
        }
    }

    /// <summary>
    /// Sets the 2x2 cell in question the the target cell height. Returns true if the cell was successfully painted and false if the cell was already the target cell Height
    /// </summary>
    /// <param name="chosenCell">The chosen starting cell in the grid</param>
    /// <param name="grassHeight">The cell Height we will paint it to.</param>
    /// <returns></returns>
    private bool SetCellAndConnectedToGroundHeight(Vector3Int chosenCell, int cellHeight)
    {
        bool cellsSuccesfullyPainted = true;
        if (waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y].CellHeight == cellHeight)
        {
            cellsSuccesfullyPainted = false;
            return cellsSuccesfullyPainted;
        }

        waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y].CellHeight = cellHeight;
        if (chosenCell.x + 1 < gridLength)
            waveCollapseFunctionGridCells[chosenCell.x + 1, chosenCell.y].CellHeight = cellHeight;
        if (chosenCell.y + 1 < gridHeight)
            waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y + 1].CellHeight = cellHeight;
        if (chosenCell.y + 1 < gridHeight && chosenCell.x + 1 < gridLength)
            waveCollapseFunctionGridCells[chosenCell.x + 1, chosenCell.y + 1].CellHeight = cellHeight;
        return cellsSuccesfullyPainted;
    }

    /// <summary>
    /// Sets the 2x2 cell in question the the target grass height. Returns true if the cell was successfully painted and false if the cell was already the target grass Height
    /// </summary>
    /// <param name="chosenCell">The chosen starting cell in the grid</param>
    /// <param name="grassHeight">The grass Height we will paint it to.</param>
    /// <returns></returns>
    private bool SetCellAndConnectedToGrassHeight(Vector3Int chosenCell, int grassHeight)
    {
        bool cellsSuccesfullyPainted = true;
        if (waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y].GrassHeight == grassHeight)
        {
            cellsSuccesfullyPainted = false;
            return cellsSuccesfullyPainted;
        }

        waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y].GrassHeight = grassHeight;
        if (chosenCell.x + 1 < gridLength)
            waveCollapseFunctionGridCells[chosenCell.x + 1, chosenCell.y].GrassHeight = grassHeight;
        if (chosenCell.y + 1 < gridHeight)
            waveCollapseFunctionGridCells[chosenCell.x, chosenCell.y + 1].GrassHeight = grassHeight;
        if (chosenCell.y + 1 < gridHeight && chosenCell.x + 1 < gridLength)
            waveCollapseFunctionGridCells[chosenCell.x + 1, chosenCell.y + 1].GrassHeight = grassHeight;
        return cellsSuccesfullyPainted;
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Grabs a list of our tiles in the cardinal directions, up, down , left , right, and removes any that are out of index from the list then returns it.
    /// </summary>
    /// <param name="targetCell"> The cell whose neighbours we are evaluating</param>
    /// <returns>A trimmed list of our neighbours without the ones that are out of index.</returns>
    private List<Vector3Int> GrabEdgeConciousNeighbourTiles(Vector3Int targetCell)
    {
        List<Vector3Int> neighbouringTiles = new List<Vector3Int> { new Vector3Int(targetCell.x + 2, targetCell.y), new Vector3Int(targetCell.x, targetCell.y + 2), new Vector3Int(targetCell.x - 2, targetCell.y), new Vector3Int(targetCell.x, targetCell.y - 2), };

        //cleanupo neighbouring tiles, null out values that are outside the index
        for (int i = 0; i < neighbouringTiles.Count; i++)
            if (neighbouringTiles[i].x < 0 || neighbouringTiles[i].x >= gridLength || neighbouringTiles[i].y < 0 || neighbouringTiles[i].y >= gridHeight)
            {
                neighbouringTiles.RemoveAt(i);
                i--;
            }

        return neighbouringTiles;
    }

    /// <summary>
    /// Grabs and returns the coordinates of a 2x2 cell directly touching this cell. Used in chunk generation when laying down cellHeights and GrassHeights.
    /// Also used when painting various envuironmental props.
    /// </summary>
    /// <param name="currentSpawn">The coordinates of the starting cell</param>
    /// <param name="xMovement">the x movement left or right allowed</param>
    /// <param name="yMovement">the y movement up or down allowed</param>
    /// <returns> a cell that is in a cardinal direction x or y units away.</returns>
    private Vector3Int GetNextChunkSpawnByPrevious(Vector3Int currentSpawn, int xMovement, int yMovement)
    {
        Vector3Int nextChosenSpawn = currentSpawn;
        bool validChosenSpawn = false;
        int nextDirection = UnityEngine.Random.Range(0, 4);

        while (!validChosenSpawn)
        {
            switch (nextDirection)
            {
                case 0:
                    nextChosenSpawn = currentSpawn - Vector3Int.left * xMovement;
                    break;
                case 1:
                    nextChosenSpawn = currentSpawn - Vector3Int.up * yMovement;
                    break;
                case 2:
                    nextChosenSpawn = currentSpawn - Vector3Int.right * xMovement;
                    break;
                case 3:
                    nextChosenSpawn = currentSpawn - Vector3Int.down * yMovement;
                    break;
                default:
                    nextChosenSpawn = currentSpawn - Vector3Int.left * xMovement;
                    break;
            }

            if (nextChosenSpawn.x >= 0 && nextChosenSpawn.x < gridLength && nextChosenSpawn.y >= 0 && nextChosenSpawn.y < gridHeight)
                validChosenSpawn = true;
            else
            {
                nextDirection++;
                if (nextDirection > 3)
                    nextDirection = 0;
            }
        }
        return nextChosenSpawn;
    }

    /// <summary>
    /// Grabs a returns a cell in a random non cardinal direction from the primary cell.
    /// </summary>
    /// <param name="currentSpawn">the primary cell we are moving from</param>
    /// <param name="xMovement">the maximum x translation allowed in either direction on the x axis.</param>
    /// <param name="yMovement">the maximum y translation allwoed in either direction on the y axis</param>
    /// <returns>a random valid coordinate within the parameters that is in the index range</returns>
    private Vector3Int GetVariableNextChunkSpawnByPrevious(Vector3Int currentSpawn, int xMovement, int yMovement)
    {
        Vector3Int nextChosenSpawn = currentSpawn;
        bool validChosenSpawn = false;
        int nextDirection = UnityEngine.Random.Range(0, 4);

        while (!validChosenSpawn)
        {
            nextChosenSpawn = currentSpawn;
            nextChosenSpawn.x += UnityEngine.Random.Range(-xMovement - 1, xMovement);
            nextChosenSpawn.y += UnityEngine.Random.Range(-yMovement - 1, yMovement);

            if (nextChosenSpawn.x >= 0 && nextChosenSpawn.x < gridLength && nextChosenSpawn.y >= 0 && nextChosenSpawn.y < gridHeight)
                validChosenSpawn = true;
        }
        return nextChosenSpawn;
    }

    /// <summary>
    /// Checks if the coordinate array is clear for props tol be placed in. we check to see if all the coordinates are in index, free of cliffs, and other props.
    /// </summary>
    /// <param name="GridCoordinatesToCheck">the list of coordinates were checking</param>
    /// <returns>true if the list of coordinates are all green and good for a prop, flase if anything went wrong</returns>
    private bool CheckIfLegalEnvironmentPropSpot(Vector3Int[] GridCoordinatesToCheck)
    {
        // Check if we already have a prop at this array of coordinates or if the cliff tile maps has tiles in any of the tilemaps.
        foreach (Vector3Int Cell in GridCoordinatesToCheck)
        {
            if (Cell.x < 0 || Cell.x >= gridLength || Cell.y < 0 || Cell.y >= gridHeight)
                return false;
            if (waveCollapseFunctionGridCells[Cell.x, Cell.y].EnvironmentPropPresent)
                return false;
            foreach (Tilemap cliffTIlemap in cliffTilemaps)
                if (cliffTIlemap.HasTile(Cell))
                    return false;
        }
        return true;
    }

    #endregion






}
