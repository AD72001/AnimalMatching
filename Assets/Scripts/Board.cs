// Match Three Game Board Logic.

// The goal is to match three or more tiles together to earn points. A goal is set at the beginning of the stage.
// The game is lost when the player run out of moves without achieving the goal.

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioSource audioSource;
    public static int move; // number of moves.
    public static int goal; // goal of the stage.
    public static bool hasNextMove = false;

    public Row[] rows;
    public Tile[,] Tiles { get; private set; }

    public int width => Tiles.GetLength(0);
    public int height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new List<Tile>(); // contains tiles that are currently selected.
    private const float TweenDuration = 0.25f;

    private void Awake() => Instance = this;

    private bool _isProgressing = false; // Return true when the swapping action is being performed.

    private bool _firstShuffle = true;

    // Creates a random new board, if the new board doesn't contain any legit move then shuffle the board.
    private void Start()
    {
        // GameObject.FindGameObjectWithTag("BGM").GetComponent<BGMusic>().PlayMusic();

        Tiles = new Tile[rows.Max(row => row.tiles.Length), rows.Length];

        Tiles = Shuffle();

        _firstShuffle = false;

        CheckForNextMove(Tiles);

        if (!CheckForNextMove(Tiles) || CanPop())
        {
            while (!CheckForNextMove(Tiles) || CanPop()) {
                Debug.Log("Shuffle at the start");
                Tiles = Shuffle();
            }
        }

        move = MoveCounter.Instance.Move;
        goal = Goal.Instance.GoalScore;
    }

    // The function to shuffle the board. Doesn't shuffle the tiles with a special item.
    // Returns the board as a list of tiles.
    public Tile[,] Shuffle() 
    {
        Tiles = new Tile[rows.Max(row => row.tiles.Length), rows.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++) 
            {
                var tile = rows[y].tiles[x];

                tile.x = x;
                tile.y = y;

                if (tile.Item != null && tile.Item.type != "Ordinary" && _firstShuffle == false)
                    continue;
                tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, 4)];

                Tiles[x, y] = tile;
            }  
        }

        return Tiles;
    }

    // Check if the board contains any move. Shuffle the board in case of no move possible left.
    public void Update()
    {
        if (!CheckForNextMove(Tiles))
        {
            CreateNewBoard();
        }

        // Switch to Game Over scene
        if (move <= 0 && !_isProgressing)
        {
            Debug.Log("You Lose!");
            SceneManager.LoadScene("GameOverScene");
        }

        // Switch to Victory scene
        if (ScoreCounter.Instance.Score >= goal && !_isProgressing)
        {
            Debug.Log("You Win!");
            SceneManager.LoadScene("VictoryScene");
        }
    }

    // Animation for the creating new board process.
    public async void CreateNewBoard() 
    {
        var newBoard = Tiles;

        while (!CheckForNextMove(newBoard)) {
            newBoard = Shuffle();
        }

        var deleteSequence = DOTween.Sequence();

        foreach (var tile in Tiles) {
            deleteSequence.Join(tile.icon.transform.DOScale(Vector2.zero, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        var createdSequence = DOTween.Sequence();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Tiles[x, y].Item = newBoard[x, y].Item;

                createdSequence.Join(Tiles[x, y].icon.transform.DOScale(Vector3.one, TweenDuration));
            }
        }

        await createdSequence.Play().AsyncWaitForCompletion();
    }

    // Selecting tiles.
    public async void Select(Tile tile) 
    {
        if (_isProgressing) return; // Only able to select tiles when the previous action is completed.

        _isProgressing = true;

        if (!_selection.Contains(tile)) // Add tiles to the selection list.
        {
            if (_selection.Count > 0)
            {
                if (Array.IndexOf(_selection[0].Neighbors, tile) != -1)
                {
                    _selection.Add(tile);
                }
                else {
                    _selection.Clear();
                }
            }
            else
            {
                _selection.Add(tile);
            }
        }

        if (_selection.Count == 2)
        {
            try {
                Debug.Log($"Selecting tiles: ({_selection[0].x}, {_selection[0].y}), ({_selection[1].x}, {_selection[1].y})");

                await Swap(_selection[0], _selection[1]);

                /* 
                Check if player's move lead to a pop. Conditions for pop are:
                    -- Three or more tiles are connected vertically or horizontally.
                    -- Universal tile is chosen.
                After the pop is finished. Numbers of moves are reduced by one.

                Swap the tiles to the original positions if conditions are not met.
                */
                if (CanPop()) 
                {
                    if (_selection[0].Item.type == "UniversalPiece" 
                        && _selection[1].Item.type == "Ordinary")
                    {
                        await UniversalPiece(_selection[1]);
                    }
                    else if (_selection[0].Item.type == "UniversalPiece" 
                        && _selection[1].Item.type != "Ordinary")
                    {
                        await GetAllTilesPiece(_selection[0]);
                    }

                    if (_selection[0].Item.type == _selection[1].Item.type && _selection[1].Item.type == "UniversalPiece")
                    {
                        await GetAllTilesPiece(tile);
                        await GetAllTilesPiece(tile);
                    }

                    if (_selection[1].Item.type == "UniversalPiece"
                        && _selection[0].Item.type == "Ordinary")
                    {
                        await UniversalPiece(_selection[0]);
                    }
                    else if (_selection[1].Item.type == "UniversalPiece" 
                        && _selection[0].Item.type != "Ordinary")
                    {
                        await GetAllTilesPiece(_selection[1]);
                    }
    
                    await Pop();
                    move -= 1;
                }
                else {
                    await Swap(_selection[0], _selection[1]);
                }

                MoveCounter.Instance.Move = move;
            }
            catch (ArgumentOutOfRangeException) {
                Debug.Log("Please wait for the progress to complete");
            }

            _selection.Clear();
        }

        _isProgressing = false;
    }

    // Animation for swapping the tiles. Swap the icons, the items between two tiles.
    public async Task Swap(Tile tile1, Tile tile2)
    {
        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        var icon1Transform = icon1.transform;
        var icon2Transform = icon2.transform;

        var sequence = DOTween.Sequence();

        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration))
                .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        var tile1Item = tile1.Item;

        tile1.Item = tile2.Item;
        tile2.Item = tile1Item;
    }

    // Swapping function without animation, used in checking next available move.
    public void SwapInCheck(Tile tile1, Tile tile2)
    {
        var tile1Item = tile1.Item;
        
        tile1.Item = tile2.Item;
        tile2.Item = tile1Item;
    }

    // Check if the board have a Pop.
    private bool CanPop() 
    {
        for (var y = 0; y < height; y++) 
        {
            for (var x = 0; x < width; x++) 
            {
                if (Tiles[x, y].GetConnectedTilesHorizontal().Skip(1).Count() >= 2 
                || Tiles[x, y].GetConnectedTilesVertical().Skip(1).Count() >= 2)  
                {
                    return true;
                }

                if (Tiles[x, y].Item.type == "UniversalPiece")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /* 
        Pop function. 

        Check the board from (0, 0) to see if tiles are in the position to be pop.
        If the pop tiles form a special shape: 4, 5 or T, L,... then a special item is spawned.
    */
    private async Task Pop() 
    {
        for (var y = 0; y < height; y++) 
        {
            for (var x = 0; x < width; x++) 
            {
                var tile = Tiles[x, y];

                var connectedTilesHorizontal = tile.GetConnectedTilesHorizontal();
                var connectedTilesVertical = tile.GetConnectedTilesVertical();

                var localVerticalList = new List<Tile> { };
                var localHorizontalList = new List<Tile> { };

                var maxLocalVerticalList = new List<Tile> { };;
                var maxLocalHorizontalList = new List<Tile> { };;

                var connectedTiles = new List<Tile> {};

                // Get the maximum number of connected tiles and the shape formed from the tiles.

                if (connectedTilesHorizontal.Skip(1).Count() >= 2) 
                {
                    foreach (var connectedTile in connectedTilesHorizontal) 
                    {
                        connectedTiles.Add(connectedTile);

                        localVerticalList = connectedTile.GetConnectedTilesVertical();

                        if (localVerticalList.Count > maxLocalVerticalList.Count)
                        {
                            maxLocalVerticalList = localVerticalList;
                        }

                        if (localVerticalList.Skip(1).Count() >= 2) {
                            foreach (var localTile in localVerticalList) 
                            {
                                if (!connectedTiles.Contains(localTile))
                                {
                                    connectedTiles.Add(localTile);
                                }
                            }
                        }
                    }

                    localVerticalList = maxLocalVerticalList;
                }

                else if (connectedTilesVertical.Skip(1).Count() >= 2) 
                {
                    foreach (var connectedTile in connectedTilesVertical) 
                    {
                        connectedTiles.Add(connectedTile);

                        localHorizontalList = connectedTile.GetConnectedTilesHorizontal();

                        if (localHorizontalList.Count > maxLocalHorizontalList.Count)
                        {
                            maxLocalHorizontalList = localHorizontalList;
                        }

                        if (localHorizontalList.Skip(1).Count() >= 2) {
                            foreach (var localTile in localHorizontalList)
                            {
                                if (!connectedTiles.Contains(localTile))
                                {
                                    connectedTiles.Add(localTile);
                                }
                            }
                        }

                        localHorizontalList = maxLocalHorizontalList;
                    }
                }

                // Continue if there are not enough connected tiles.
                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deleteSequence = DOTween.Sequence();

                // Get all tiles that would be affected if the current tiles are pop. In case of special tiles are pop.

                var tempTiles = GetAllConnectedTiles(connectedTiles);

                connectedTiles.AddRange(tempTiles);

                var tileName = connectedTiles[0].Item.name; // Gets the name of the tiles being pop.

                foreach (var connectedtile in connectedTiles) 
                {
                    deleteSequence.Join(connectedtile.icon.transform.DOScale(Vector2.zero, TweenDuration));
                }

                // Play the audio and calculate the score.
                audioSource.PlayOneShot(popSound);

                ScoreCounter.Instance.Score += tile.Item.value*connectedTiles.Count;

                await deleteSequence.Play().AsyncWaitForCompletion(); //

                var createdSequence = DOTween.Sequence();

                foreach (var connectedTile in connectedTiles) 
                {
                    connectedTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, 4)];
                    createdSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                // Need to add type to each items

                // 4 tile piece
                if (connectedTilesHorizontal.Count == 4 || connectedTilesVertical.Count == 4 ||
                    localHorizontalList.Count == 4 || localVerticalList.Count == 4) {
    
                    var itemIndex = findIndexItem(ItemDatabase.FourPieceItems, tileName + "_FourPiece");

                    connectedTiles[1].Item = ItemDatabase.FourPieceItems[itemIndex];
                    // connectedTiles[1].Item = ItemDatabase.Items[4];
                    createdSequence.Join(connectedTiles[1].icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                
                // 5 tile piece or higher
                if (connectedTilesVertical.Count >= 5 || connectedTilesHorizontal.Count >= 5 ||
                    localVerticalList.Count >= 5 || localHorizontalList.Count >= 5) {
                    connectedTiles[connectedTiles.Count/2 + 1].Item = ItemDatabase.Items[5];
                    createdSequence.Join
                    (connectedTiles[connectedTilesVertical.Count/2 + 1].icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                
                // 2-1-2 tile piece, L shape, T shape, cross shape
                if ((connectedTilesVertical.Count >= 3 && localHorizontalList.Count() >= 3) ||
                    (connectedTilesHorizontal.Count >= 3 && localVerticalList.Count() >= 3) ||
                    (connectedTilesVertical.Count >= 3 && connectedTilesHorizontal.Count >= 3)) 
                {
                    Debug.Log(tileName + "_DoubleThree");
                    var itemIndex = findIndexItem(ItemDatabase.DoubleThreeItems, tileName + "_DoubleThree");

                    connectedTiles[0].Item = ItemDatabase.DoubleThreeItems[itemIndex];
                    createdSequence.Join
                    (connectedTiles[0].icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await createdSequence.Play().AsyncWaitForCompletion(); ///

                x = 0;
                y = 0;
            }
        }
    }

    // Recursive function to get all involved tiles when pop a special tile.
    public List<Tile> GetAllConnectedTiles(List<Tile> connectedTiles = null, List<Tile> exclude = null)
    {
        var result = new List<Tile> {};

        exclude ??= new List<Tile>() {};

        foreach (var tile in connectedTiles) 
        {
            if (tile == null || exclude.Contains(tile)) continue;

            if (tile.Item.type == "Ordinary") continue;

            else if (tile.Item.type == "FourPiece")
            {
                result.AddRange(tile.GetEveryTilesVertical());
            }

            else if (tile.Item.type == "UniversalPiece")
            {
                result.AddRange(tile.GetEverySameTiles());
            }

            else if (tile.Item.type == "DoubleThreePiece") 
            {
                result.AddRange(tile.GetEveryTilesHorizontal());
            }

            exclude.Add(tile);
            result.AddRange(GetAllConnectedTiles(result, exclude));
        }

        return result;
    }

    // Check if next move exists in the current board (Tiles) by swapping tiles with the adjacent ones.
    // Returns true if CanPop() is true after a swap. If there are no pop available, returns false.
    public bool CheckForNextMove(Tile[,] Tiles) 
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (x < width - 1) {
                    SwapInCheck(Tiles[x, y], Tiles[x + 1, y]);

                    if (CanPop()) {
                        SwapInCheck(Tiles[x, y], Tiles[x + 1, y]);

                        return true;
                    }
                    else {
                        SwapInCheck(Tiles[x, y], Tiles[x + 1, y]);
                    }
                }

                if (y < height - 1) {
                    SwapInCheck(Tiles[x, y], Tiles[x, y + 1]);

                    if (CanPop()) {
                        SwapInCheck(Tiles[x, y], Tiles[x, y + 1]);

                        return true;
                    }
                    else {
                        SwapInCheck(Tiles[x, y], Tiles[x, y + 1]);
                    } 
                }
            }
        }

        return false;
    }

    // Process the special four-tiles item. When a four-tiles item is triggered, a column of items is cleared.
    public async Task FourTilesPiece(Tile tile) 
    {
        var deleteSequence = DOTween.Sequence();
        var createSequence = DOTween.Sequence();

        var deleteTiles = tile.GetEveryTilesVertical();

        deleteTiles.AddRange(GetAllConnectedTiles(deleteTiles));

        var totalScore = tile.Item.value;

        foreach (var deleteTile in deleteTiles)
        {
            deleteSequence.Join(deleteTile.icon.transform.DOScale(Vector2.zero, TweenDuration));
            totalScore += deleteTile.Item.value;
            deleteTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0,4)];
            createSequence.Join(deleteTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(popSound);

        ScoreCounter.Instance.Score += totalScore;

        await createSequence.Play().AsyncWaitForCompletion();
    }

    /* 
    -- Process the special universal-tiles item. When a universal-tiles item is triggered with an ordinary item, 
    all items that is the same as the ordinary item are cleared.
    -- If a special item is paired with universal-tiles item, clear entire board.
    */
    public async Task UniversalPiece(Tile tile)
    {
        var deleteSequence = DOTween.Sequence();
        var createSequence = DOTween.Sequence();

        var deleteTiles = tile.GetEverySameTiles();

        deleteTiles.Add(tile);

        var totalScore = tile.Item.value;

        foreach (var deleteTile in deleteTiles)
        {
            deleteSequence.Join(deleteTile.icon.transform.DOScale(Vector2.zero, TweenDuration));
            totalScore += deleteTile.Item.value;
            deleteTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0,4)];
            createSequence.Join(deleteTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(popSound);

        ScoreCounter.Instance.Score += totalScore;

        await createSequence.Play().AsyncWaitForCompletion();
    }

    // Clear the entire board, triggered when paired an universal-tiles item and a special item.
    public async Task GetAllTilesPiece(Tile tile = null)
    {
        var deleteSequence = DOTween.Sequence();
        var createSequence = DOTween.Sequence();

        var totalScore = 0;

        if (tile != null)
            totalScore = tile.Item.value;

        foreach (var deleteTile in Tiles)
        {
            deleteSequence.Join(deleteTile.icon.transform.DOScale(Vector2.zero, TweenDuration));
            totalScore += deleteTile.Item.value;
            deleteTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0,4)];
            createSequence.Join(deleteTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(popSound);

        ScoreCounter.Instance.Score += totalScore;

        await createSequence.Play().AsyncWaitForCompletion();
    }

    // Process the special double-three item (2-1-2, T, L). When this item is triggered, a row of items is cleared.
    public async Task DoubleThreePiece(Tile tile)
    {
        var deleteSequence = DOTween.Sequence();
        var createSequence = DOTween.Sequence();

        var deleteTiles = tile.GetEveryTilesHorizontal();

        deleteTiles.AddRange(GetAllConnectedTiles(deleteTiles));

        var totalScore = tile.Item.value;

        foreach (var deleteTile in deleteTiles)
        {
            deleteSequence.Join(deleteTile.icon.transform.DOScale(Vector2.zero, TweenDuration));
            totalScore += deleteTile.Item.value;
            deleteTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0,4)];
            createSequence.Join(deleteTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(popSound);

        ScoreCounter.Instance.Score += totalScore;

        await createSequence.Play().AsyncWaitForCompletion();
    }

    // Find index of item in ItemDatabase' lists.
    public static int findIndexItem(Item[] Items, string name) 
    {
        Debug.Log($"Name: {name}");
        for (var index=0; index < Items.Count(); index++)
        {
            Debug.Log($"IName: {Items[index].name}");
            if (Items[index].name == name) return index;
        }
        return -1;
    }
}
