using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine.SocialPlatforms.Impl;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioSource audioSource;
    public static int move;
    public static bool hasNextMove = false;

    public Row[] rows;
    public Tile[,] Tiles { get; private set; }

    public int width => Tiles.GetLength(0);
    public int height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new List<Tile>();
    private const float TweenDuration = 0.25f;

    private void Awake() => Instance = this;

    private bool _isProgressing = false;

    private void Start() 
    {
        Tiles = new Tile[rows.Max(row => row.tiles.Length), rows.Length];

        Tiles = Shuffle();

        CheckForNextMove(Tiles);

        if (!CheckForNextMove(Tiles) || CanPop())
        {
            while (!CheckForNextMove(Tiles) || CanPop()) {
                Debug.Log("Shuffle at the start");
                Tiles = Shuffle();
            }
        }

        move = 20;
    }

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

                // tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, 4)];

                Tiles[x, y] = tile;
            }  
        }

        return Tiles;
    }

    public void Update() 
    {
        if (!CheckForNextMove(Tiles)) 
        {
            CreateNewBoard();
        }
    }

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

    public async void Select(Tile tile) 
    {
        if (_isProgressing) return;

        _isProgressing = true;

        if (!_selection.Contains(tile)) 
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
                
                if (CanPop() || 
                    _selection[0].Item.type != "Ordinary" ||
                    _selection[1].Item.type != "Ordinary") 
                {
                    if (_selection[0].Item.type == "FourPiece")
                    {
                        await FourTilesPiece(_selection[0]);
                    }

                    if (_selection[1].Item.type == "FourPiece")
                    {
                        await FourTilesPiece(_selection[1]);
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

    public void SwapInCheck(Tile tile1, Tile tile2)
    {
        var tile1Item = tile1.Item;
        
        tile1.Item = tile2.Item;
        tile2.Item = tile1Item;
    }

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
            }
        }

        return false;
    }

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

                var connectedTiles = new List<Tile> {};

                if (connectedTilesHorizontal.Skip(1).Count() >= 2) 
                {
                    foreach (var connectedTile in connectedTilesHorizontal) 
                    {
                        connectedTiles.Add(connectedTile);

                        localVerticalList = connectedTile.GetConnectedTilesVertical();

                        if (localVerticalList.Skip(1).Count() >= 2) {
                            connectedTiles.AddRange(localVerticalList );
                        }
                    }
                }

                else if (connectedTilesVertical.Skip(1).Count() >= 2) 
                {
                    foreach (var connectedTile in connectedTilesVertical) 
                    {
                        connectedTiles.Add(connectedTile);

                        localHorizontalList = connectedTile.GetConnectedTilesHorizontal();

                        if (localHorizontalList.Skip(1).Count() >= 2) {
                            connectedTiles.AddRange(localHorizontalList);
                        }
                    }
                }

                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deleteSequence = DOTween.Sequence();

                var tempTiles = GetAllConnectedTiles(connectedTiles);

                connectedTiles.AddRange(tempTiles);

                foreach (var connectedtile in connectedTiles) 
                {
                    deleteSequence.Join(connectedtile.icon.transform.DOScale(Vector2.zero, TweenDuration));
                }

                audioSource.PlayOneShot(popSound);

                ScoreCounter.Instance.Score += tile.Item.value*connectedTiles.Count;

                await deleteSequence.Play().AsyncWaitForCompletion(); //

                var createdSequence = DOTween.Sequence();

                foreach (var connectedTile in connectedTiles) 
                {
                    connectedTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, 4)];
                    createdSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                // Need to add tags to each items
                Debug.Log($"ConnectedTiles:{connectedTiles.Count}");
                Debug.Log($"connectedTilesVertical:{connectedTilesVertical.Count}");
                Debug.Log($"connectedTilesHorizontal:{connectedTilesHorizontal.Count}");
                Debug.Log($"localVerticalList:{localVerticalList.Count}");
                Debug.Log($"localHorizontalList:{localHorizontalList.Count}");

                // 4 tile piece
                if (connectedTilesHorizontal.Count == 4 || connectedTilesVertical.Count == 4) {
                    connectedTiles[1].Item = ItemDatabase.Items[4];
                    createdSequence.Join(connectedTiles[1].icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                
                // 5 tile piece
                if (connectedTilesVertical.Count >= 5 || connectedTilesHorizontal.Count >= 5) {
                    connectedTiles[connectedTiles.Count/2 + 1].Item = ItemDatabase.Items[5];
                    createdSequence.Join
                    (connectedTiles[connectedTilesVertical.Count/2].icon.transform.DOScale(Vector3.one, TweenDuration));
                }
                
                // 2-1-2 tile piece
                if ((connectedTilesVertical.Count >= 3 && localHorizontalList.Count >= 3) ||
                    (connectedTilesHorizontal.Count >= 3 && localVerticalList.Count >= 3) ||
                    (connectedTilesVertical.Count >= 3 && connectedTilesHorizontal.Count >= 3)) 
                {
                    connectedTiles[connectedTiles.Count/2].Item = ItemDatabase.Items[6];
                    createdSequence.Join
                    (connectedTiles[connectedTiles.Count/2].icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await createdSequence.Play().AsyncWaitForCompletion(); ///

                x = 0;
                y = 0;
            }
        }
    }

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
                result.AddRange(tile.GetEveryTilesHorizontal());
                result.AddRange(tile.GetEveryTilesVertical());
            }

            else if (tile.Item.type == "UniversalPiece")
            {
                result.AddRange(tile.GetEverySameTiles(Tiles));
            }

            else if (tile.Item.type == "DoubleThreePiece") 
            {
                result.AddRange(tile.GetEveryTilesHorizontal());
            }

            exclude.Add(tile);
            result.AddRange(GetAllConnectedTiles(result, exclude));
        }

        if (result == null)
            return connectedTiles;

        return result;
    }

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

    public async Task FourTilesPiece(Tile tile) 
    {
        var deleteSequence = DOTween.Sequence();
        var createdSequence = DOTween.Sequence();

        var deleteTiles = tile.GetEveryTilesVertical();
        deleteTiles.AddRange(tile.GetEveryTilesHorizontal());

        deleteTiles.AddRange(GetAllConnectedTiles(deleteTiles));

        var totalScore = tile.Item.value;

        foreach (var deleteTile in deleteTiles)
        {
            deleteSequence.Join(deleteTile.icon.transform.DOScale(Vector2.zero, TweenDuration));
            totalScore += deleteTile.Item.value;
            deleteTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0,4)];
            createdSequence.Join(deleteTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await deleteSequence.Play().AsyncWaitForCompletion();

        audioSource.PlayOneShot(popSound);

        ScoreCounter.Instance.Score += totalScore;

        await createdSequence.Play().AsyncWaitForCompletion();
    }

    public void FiveTilesPiece()
    {
        return;
    }

    public void DoubleTwoTilePiece()
    {
        return;
    }
}
