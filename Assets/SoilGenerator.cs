using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class SoilGenerator : MonoBehaviour
{
    [Header("Terrain Gen")] [SerializeField]
    public int Width, Height;

    [SerializeField] public float Smoothness;

    [Header("SpecialGen")] [SerializeField]
    public int ChanceOfSpecial;

    [Header("Tile")] [SerializeField] public TileBase GroundTile,
        TreeTile,
        RootTile,
        RootWithNutrientTile,
        NutrientTile,
        HeartTile,
        CoinTile,
        DinoTile,
        BoneTile,
        RocksTile,
        CanTile,
        SpiderTile,
        SlugTile;

    [SerializeField] public Tilemap GroundTileMap, SpecialTileMap, TreeTileMap, RootTileMap;

    [Header("Lives")] [SerializeField] public int playerLivesInit;
    [SerializeField] public Image[] hearts;
    [SerializeField] public Canvas Instructions, EndScreen;
    [SerializeField] public TMP_Text ScoreText, FinalScoreText;
    [SerializeField] public int WaitingPeriod;

    private TileType[,] _map;
    private int _camZPos = -10;
    private float _seed;
    private Vector2Int _treeLocation;
    private Vector2Int _playerLocation;
    private int _playerLives, _score;
    private int _wait;
    private bool _isMoving = false;

    public enum TileType
    {
        Air = 0,
        Soil = 1,
        Tree,
        Root,
        RootWithNutrient,

        // Specials
        Heart,
        Nutrient,
        Coin,
        Dino,
        Bone,
        Rocks,
        Can,
        Spider,
        Slug,
    };

    public enum Direction
    {
        Up, Down, Left, Right
    }

    private Direction _playerDirection;

    // Start is called before the first frame update
    void Start()
    {
        _playerLives = playerLivesInit;
        Instructions.enabled = true;
        EndScreen.enabled = false;
        _isMoving = false;
        Generation();
        _score = 0;
        ScoreText.text = "" + _score;
        FinalScoreText.text = "" + _score;
        _wait = 100;
    }

    void FixedUpdate()
    {

        if (Instructions.enabled)
        {
            if (Input.anyKeyDown)
            {
                Instructions.enabled = false;
                _isMoving = true;
            }

            return;
        }

        if (_playerLives <= 0)
        {
            _isMoving = false;
            FinalScoreText.text = "" + _score;
            EndScreen.enabled = true;
            if (_wait <= 0  && Input.anyKeyDown)
            {
                this.Start();
            }

            _wait--;
            return;
        }


        if (!_isMoving)
        {
            return;
        }

        if (_wait != 0)
        {
            _wait--;
            return;
        }

        var originalPlayerLocation = _playerLocation;
        bool playerMoved = false;
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _playerDirection = Direction.Down;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _playerDirection = Direction.Up;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _playerDirection = Direction.Left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _playerDirection = Direction.Right;
        }

        switch (_playerDirection)
        {
            case Direction.Down:
                _playerLocation.y -= 1;
                break;
            case Direction.Up:
                _playerLocation.y += 1;
                break;
            case Direction.Left:
                _playerLocation.x -= 1;
                break;
            case Direction.Right:
                _playerLocation.x += 1;
                break;
        }

        playerMoved = true;

        if (_playerLocation.x > Width - 1)
        {
            _playerLocation.x = Width - 1;
            playerMoved = false;
        }

        if (_playerLocation.x < 0)
        {
            _playerLocation.x = 0;
            playerMoved = false;
        }

        if (_playerLocation.y > Height - 1)
        {
            _playerLocation.y = Height - 1;
            playerMoved = false;
        }

        if (_playerLocation.y < 0)
        {
            _playerLocation.y = 0;
            playerMoved = false;
        }

        if (playerMoved)
        {
            Instructions.enabled = false;
            var newLocationType = _map[_playerLocation.x, _playerLocation.y];
            if (newLocationType == TileType.Soil)
            {
                _map[_playerLocation.x, _playerLocation.y] = TileType.Root;
                _score++;
            }

            else if (newLocationType == TileType.Nutrient)
            {
                _map[_playerLocation.x, _playerLocation.y] = TileType.RootWithNutrient;
                _score += 5;
            }

            else if (newLocationType == TileType.Heart)
            {
                _playerLives++;
                print("Gained a life, lives remaining " + _playerLives);
                _map[_playerLocation.x, _playerLocation.y] = TileType.Root;
            }
            else // if (newLocationType == TileType.Root || newLocationType == TileType.RootWithNutrient)
            {
                _playerLives--;
                print("Lost a life, lives remaining " + _playerLives);
                do
                {
                    _playerLocation = new Vector2Int(Random.Range(0, Width), Random.Range(0, Height));
                } while (!(_map[_playerLocation.x, _playerLocation.y] == TileType.Soil && OnlyOneSurroundingTileIsRoot(_playerLocation.x, _playerLocation.y, _map)));
                _map[_playerLocation.x, _playerLocation.y] = TileType.Root;
            }
            // else
            // {
            //     _playerLocation = originalPlayerLocation;
            // }

            RenderMap(_map);
        }

        Camera.main.gameObject.transform.position = new Vector3(_playerLocation.x, _playerLocation.y, _camZPos);

        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < _playerLives)
            {
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }


        ScoreText.text = "" + _score;
        _wait = WaitingPeriod;
    }

    private bool OnlyOneSurroundingTileIsRoot(int x, int y, TileType[,] map)
    {
        int rootCount = 0;
        if (x > 0 && map[x - 1, y] == TileType.Root) rootCount++;
        if (x < Width -1 && map[x + 1, y] == TileType.Root) rootCount++;
        if (y > 0 && map[x, y - 1] == TileType.Root) rootCount++;
        if (y < Height - 1 && map[x, y + 1] == TileType.Root) rootCount++;
        return rootCount == 1;
    }

    void Generation()
    {
        _seed = Random.Range(0, 1000);
        GroundTileMap.ClearAllTiles();
        SpecialTileMap.ClearAllTiles();
        TreeTileMap.ClearAllTiles();
        RootTileMap.ClearAllTiles();
        _map = GenerateArray();
        _map = TerrainGeneration(_map);
        RenderMap(_map);
    }

    public TileType[,] GenerateArray()
    {
        TileType[,] map = new TileType[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                map[x, y] = TileType.Air;
            }
        }

        return map;
    }

    public TileType[,] TerrainGeneration(TileType[,] map)
    {
        int perlinHeight;
        for (int x = 0; x < Width; x++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(x / Smoothness, _seed) * Height / 2);
            perlinHeight += Height / 2;
            for (int y = 0; y <= perlinHeight; y++)
            {
                if (y > 0 && y < perlinHeight && x > 0 && x < (Width - 1))
                {
                    TileType specialTile = (Random.Range(0, ChanceOfSpecial) == 1)
                        ? (TileType) Random.Range((int) TileType.Heart, (int) TileType.Slug)
                        : TileType.Soil;
                    map[x, y] = specialTile;
                }
                else
                {
                    map[x, y] = TileType.Soil;
                }
            }

            if (x == Mathf.RoundToInt(Width / 2))
            {
                _treeLocation = new Vector2Int(x, perlinHeight + 1);
                _playerLocation = new Vector2Int(x, perlinHeight - 1);
                for (int y = perlinHeight + 1; y < Height; y++)
                {
                    map[x, y] = TileType.Tree;
                }

                map[x, perlinHeight] = TileType.Root;
                map[x, perlinHeight - 1] = TileType.Root;
            }
        }

        return map;
    }

    public void RenderMap(TileType[,] map)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                switch (map[x, y])
                {
                    case TileType.Air:
                        break;
                    case TileType.Soil:
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Root:
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        RootTileMap.SetTile(new Vector3Int(x, y, 0), RootTile);
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), null);
                        break;
                    case TileType.RootWithNutrient:
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        RootTileMap.SetTile(new Vector3Int(x, y, 0), RootTile);
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), RootWithNutrientTile);
                        break;
                    case TileType.Tree:
                        TreeTileMap.SetTile(new Vector3Int(x, y, 0), TreeTile);
                        break;
                    case TileType.Bone:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), BoneTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Can:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), CanTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Coin:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), CoinTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Dino:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), DinoTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Rocks:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), RocksTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Spider:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), SpiderTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Nutrient:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), NutrientTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Heart:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), HeartTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                    case TileType.Slug:
                        SpecialTileMap.SetTile(new Vector3Int(x, y, 0), SlugTile);
                        GroundTileMap.SetTile(new Vector3Int(x, y, 0), GroundTile);
                        break;
                }
            }
        }
    }
}