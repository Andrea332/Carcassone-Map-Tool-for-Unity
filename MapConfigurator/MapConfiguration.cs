using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OneHourStudio
{
    [CreateAssetMenu(fileName = "Map Configuration", menuName = "ScriptableObjects/Game Resource/Map Configuration", order = 1)]
    
    public class MapConfiguration : ScriptableObject
    {
        public int gridWidth = 10;
        public int gridHeight = 10;
        public List<TileConfiguration> tiles = new();
        public List<Tile> deckTiles = new();
        public MapConfiguration(List<TileConfiguration> tileConfigurations, List<Tile> deckTiles, int width, int height)
        {
            this.gridWidth = width;
            this.gridHeight = height;
            this.tiles = new List<TileConfiguration>(tileConfigurations);
            this.deckTiles = new List<Tile>(deckTiles);
        }
    }

    [Serializable]
    public class MapConfigurationDataWrapper
    {
        // Used to read and write MapConfiguration into json
        public int gridWidth = 10;
        public int gridHeight = 10;
        public List<TileConfiguration> tiles = new();
        public List<string> deckTiles = new();
    }

    [Serializable]
    public class TileConfiguration
    {
        public Tile tile;               // used for custom inspector
        public string id;               // used for load/save
        public TileRotation rotation;
        public Vector2Int position;
        public string cityName;
        public string cityOwner;
        public TileConfiguration(string id, float tileRotation, Vector3 position, string cityName="", string cityOwner ="")
        {
            this.id = id;
            this.rotation = (TileRotation)tileRotation;
            this.position = new Vector2Int((int)position.x, (int)position.z);
            this.cityName = cityName;
            this.cityOwner = cityOwner;
        }
    }
}
