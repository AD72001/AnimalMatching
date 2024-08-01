using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class Tile : MonoBehaviour
{
    public int x, y;
    private Item _item;

    public Item Item {
        get => _item;
        set {
            if (_item == value) return;

            _item = value;
            icon.sprite = _item.sprite;
        }
    }
    public Image icon;
    public Button button;

    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y]: null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1]: null;
    public Tile Right => x < Board.Instance.width - 1 ? Board.Instance.Tiles[x + 1, y]: null;
    public Tile Bottom => y < Board.Instance.height - 1 ? Board.Instance.Tiles[x, y + 1]: null;

    public Tile[] Neighbors => new[] {
        Left, 
        Right,
        Top,
        Bottom
    };

    public Tile[] Verticals => new[] {
        Top,
        Bottom
    };

    public Tile[] Horizontals => new[] {
        Left,
        Right
    };

    public List<Tile> GetConnectedTilesHorizontal(List<Tile> exclude = null)
    {
        var result = new List<Tile> {this,};

        if (exclude == null) {
            exclude = new List<Tile> {this,};
        }
        else {
            exclude.Add(this);
        }

        foreach (var tile in Horizontals) {
            if (tile == null || exclude.Contains(tile) || (tile.Item != Item)) continue;

            result.AddRange(tile.GetConnectedTilesHorizontal(exclude));
        }

        return result;
    }

    public List<Tile> GetEveryTilesHorizontal(List<Tile> exclude = null)
    {
        var result = new List<Tile> {this,};

        if (exclude == null) {
            exclude = new List<Tile> {this,};
        }
        else {
            exclude.Add(this);
        }

        foreach (var tile in Horizontals) {
            if (tile == null || exclude.Contains(tile)) continue;

            result.AddRange(tile.GetEveryTilesHorizontal(exclude));
        }

        return result;
    }

    public List<Tile> GetConnectedTilesVertical(List<Tile> exclude = null)
    {
        var result = new List<Tile> {this,};

        if (exclude == null) {
            exclude = new List<Tile> {this,};
        }
        else {
            exclude.Add(this);
        }

        foreach (var tile in Verticals) {
            if (tile == null || exclude.Contains(tile) || (tile.Item != Item)) continue;
 
            result.AddRange(tile.GetConnectedTilesVertical(exclude));
        }
        
        return result;
    }

    public List<Tile> GetEveryTilesVertical(List<Tile> exclude = null)
    {
        var result = new List<Tile> {this,};

        if (exclude == null) {
            exclude = new List<Tile> {this,};
        }
        else {
            exclude.Add(this);
        }

        foreach (var tile in Verticals) {
            if (tile == null || exclude.Contains(tile)) continue;
 
            result.AddRange(tile.GetEveryTilesVertical(exclude));
        }
        
        return result;
    }
    
    public List<Tile> GetEverySameTiles() 
    {
        var result = new List<Tile> {};

        for (var y = 0; y < Board.Instance.height; y++) 
        {
            for (var x = 0; x < Board.Instance.width; x++)
            {
                var tile = Board.Instance.Tiles[x, y];

                if (tile.Item == Item) {
                    result.Add(tile);
                }
            }
        }

        return result;
    }

    private void Start() 
    {
        button.onClick.AddListener(() => Board.Instance.Select(this));
    }
}
