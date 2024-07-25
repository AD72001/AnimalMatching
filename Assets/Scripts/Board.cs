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

                tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];

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
                
                if (CanPop()) {
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

                var connectedTiles = new List<Tile> {};

                if (connectedTilesHorizontal.Skip(1).Count() >= 2) {
                    foreach (var connectedTile in connectedTilesHorizontal) {
                        connectedTiles.Add(connectedTile);
                    }
                }

                if (connectedTilesVertical.Skip(1).Count() >= 2) {
                    foreach (var connectedTile in connectedTilesVertical) {
                        connectedTiles.Add(connectedTile);
                    }
                }

                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deleteSequence = DOTween.Sequence();

                foreach (var connectedtile in connectedTiles) {
                    deleteSequence.Join(connectedtile.icon.transform.DOScale(Vector2.zero, TweenDuration));
                }

                audioSource.PlayOneShot(popSound);

                ScoreCounter.Instance.Score += tile.Item.value*connectedTiles.Count;

                await deleteSequence.Play().AsyncWaitForCompletion(); //

                var createdSequence = DOTween.Sequence();

                foreach (var connectedtile in connectedTiles) {
                    connectedtile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                    createdSequence.Join(connectedtile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await createdSequence.Play().AsyncWaitForCompletion(); ///

                x = 0;
                y = 0;
            }
        }
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
        Debug.Log("No swap");
        return false;
    }
}
