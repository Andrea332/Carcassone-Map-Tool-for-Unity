using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace OneHourStudio
{
    [CustomEditor(typeof(MapConfiguration))]
    public class MapConfigurationEditor : Editor
    {
        public VisualTreeAsset m_InspectorXML;
        public VisualTreeAsset m_tileXML;
        public VisualTreeAsset m_cityTileXML;
        public VisualTreeAsset m_gridTileButtonXML;
        private MapConfiguration mapConfiguration;
        private VisualElement gridView;
        private ObjectField tileSelector;
        
        //EDITOR DATA
        private Dictionary<Button, TileButtonData> tileButtonDatas;
        private Button currentGridButton;
        private TileButtonData currentTileButtonData;
        private List<string> Cities
        {
            get
            {
                List<string> cities = new();
                foreach (var tileButtonData in tileButtonDatas)
                {
                    if(!string.IsNullOrEmpty(tileButtonData.Value.cityName))
                    {
                        cities.Add(tileButtonData.Value.cityName);
                    }
                }
                cities.Add(tileNotOwned);
                return cities;
            }
        }
        private string tileNotOwned = "Not Owned";

        //TILE VIEWER VISUAL ELEMENTS
        private Foldout gridFoldOut;
        private Foldout deckFoldOut;
        private VisualElement tileViewer;
        private VisualElement twImage;
        private EnumField twRotation;
        private TextElement twPositionY;
        private TextElement twPositionX;
        private TextElement twId;
        private TextField twCityName;
        private DropdownField twOwnerCity;
        private Button twDeleteTileButton;
        private Button twRegenerateCityName;
        
        public override VisualElement CreateInspectorGUI()
        {
            mapConfiguration = (MapConfiguration)target;
            VisualElement customInspector = m_InspectorXML.Instantiate();
            gridFoldOut = customInspector.Q<Foldout>("Grid");
            deckFoldOut = customInspector.Q<Foldout>("Deck");
            InitializeGridValues();
            BuildGrid();
            //BuildDeckTiles();
            return customInspector;
        }
        
        private void InitializeGridValues()
        {
            var gridWidth = gridFoldOut.Q<IntegerField>("Grid_Width");
            var gridHeight = gridFoldOut.Q<IntegerField>("Grid_Height");

            gridWidth.SetValueWithoutNotify(mapConfiguration.gridWidth);
            gridHeight.SetValueWithoutNotify(mapConfiguration.gridHeight);
            
            gridWidth.RegisterValueChangedCallback(OnGridWidthChanged);
            gridHeight.RegisterValueChangedCallback(OnGridHeightChanged);
        }
        private void BuildGrid()
        {
            if (gridView == null)
            {
                gridView = new VisualElement();
                gridFoldOut.Add(gridView);
            }
            
            gridView.Clear();
           
            tileButtonDatas = new();
            
            for (int heightIndex = 0; heightIndex < mapConfiguration.gridHeight; heightIndex++)
            {
                VisualElement horizontalView = new()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row
                    }
                };
                gridView.Add(horizontalView);
                for (int widthIndex = 0; widthIndex < mapConfiguration.gridWidth; widthIndex++)
                {
                    VisualElement gridTile = m_gridTileButtonXML.Instantiate();
                    horizontalView.Add(gridTile);
                    var gridTileButton = gridTile.Q<Button>();
                    gridTileButton.RegisterCallback<MouseUpEvent>(OnGridTileButtonClicked);
                    tileButtonDatas.Add(gridTileButton, new TileButtonData(null, new Vector2Int(widthIndex,heightIndex), TileRotation.Up));
                }
            }

            foreach (var tileConfiguration in mapConfiguration.tiles)
            {
                foreach (var tileButtonData in tileButtonDatas)
                {
                    if (tileButtonData.Value.position == tileConfiguration.position)
                    {
                        tileButtonData.Value.tile = tileConfiguration.tile;
                        if (tileButtonData.Value.tile != null)
                        {
                            tileButtonData.Key.style.backgroundImage = new StyleBackground(tileButtonData.Value.tile.GetSprite());
                        }
                        tileButtonData.Value.rotation = tileConfiguration.rotation;
                        tileButtonData.Key.transform.rotation = Quaternion.Euler(new Vector3(0,0,(int)tileConfiguration.rotation));
                        tileButtonData.Value.cityName = tileConfiguration.cityName;
                        tileButtonData.Value.cityOwner = tileConfiguration.cityOwner;
                    }
                }
            }
           
        }

        private void BuildDeckTiles()
        {
            var deckTiles = deckFoldOut.Q<ListView>("deckTiles");
            Func<VisualElement> makeItem = () => new ObjectField();
            void BindItem(VisualElement visualElement, int i)
            {
                var deckTile = (ObjectField)visualElement;
                deckTile.objectType = typeof(Tile);
                deckTile.value = mapConfiguration.deckTiles[i];
            }

            deckTiles.makeItem = makeItem;
            deckTiles.bindItem = BindItem;
            deckTiles.itemsSource = mapConfiguration.deckTiles;
            deckTiles.RefreshItems();
        }

        private void BuildTileSelector()
        {
            tileSelector = new ObjectField
            {
                name = "TileSelector",
                label = "Tile:",
                objectType = typeof(Tile)
            };
            gridFoldOut.Add(tileSelector);
            tileSelector.RegisterValueChangedCallback(OnTilePrefabSelected);
        }
        private void SetTileSelector(Tile tile, bool sendCallback = true)
        {
            if (sendCallback)
            {
                tileSelector.value = tile;
                return;
            }

            tileSelector.SetValueWithoutNotify(tile);
        }
        
    
        
    #region TILE VIEW FUNCTIONS
        private void RemoveTileView()
        {
            if (tileViewer != null)
            {
                tileViewer.RemoveFromHierarchy();
                tileViewer = null;
            }
        }
        private void BuildTileView(TileButtonData tileButtonData)
        {
            if (tileButtonData.tile is City)
            {
                tileViewer = m_cityTileXML.Instantiate();
            }
            else
            {
                tileViewer = m_tileXML.Instantiate();
            }
            
            gridFoldOut.Add(tileViewer);
            
            twImage = tileViewer.Q<VisualElement>("TileImage");
            twId = tileViewer.Q<TextElement>("id");
            twPositionX = tileViewer.Q<TextElement>("positionX");
            twPositionY = tileViewer.Q<TextElement>("positionY");
            twRotation = tileViewer.Q<EnumField>("rotation");
            twCityName = tileViewer.Q<TextField>("cityName");
            twOwnerCity = tileViewer.Q<DropdownField>("ownerCity");
            twRegenerateCityName = tileViewer.Q<Button>("regenerateCityName");
            twDeleteTileButton = tileViewer.Q<Button>("deleteTile");
            
            twRotation.Init(TileRotation.Up);
            twRotation.RegisterValueChangedCallback(OnTileRotationChanged);
            twDeleteTileButton.RegisterCallback<MouseUpEvent>(OnDeleteTileButton);
            twRegenerateCityName?.RegisterCallback<MouseUpEvent>(OnRegenerateCityName);
            twCityName?.RegisterValueChangedCallback(OnCityNameChanged);
            twOwnerCity?.RegisterValueChangedCallback(OnTileOwnerCityChanged);
        }

      

        private void SetTileView(TileButtonData tileButtonData)
        {
            SetTileViewPosition(tileButtonData.position);
            SetTileViewRotation(tileButtonData.rotation, false);
            
            if (tileButtonData.tile == null)
            {
                SetTileViewId("Tile not selected");
                return;
            }
           
            SetTileViewId(tileButtonData.tile.id);
            SetTileSelector(tileButtonData.tile,false);
            SetTileViewImage(tileButtonData.tile.GetSprite(), tileButtonData.rotation);
            
            switch (tileButtonData.tile)
            {
                case City:
                {
                    SetTileViewCityName(tileButtonData.cityName, false);
                    break;
                }
                case not null:
                {
                    twOwnerCity.choices.Clear();
                    twOwnerCity.choices.AddRange(Cities);
                    SetTileViewOwnerCity(tileButtonData.cityOwner, false);
                    break;
                }
                   
            }
        }
        private void SetTileViewPosition(Vector2Int position)
        {
            twPositionX.text = position.x.ToString();
            twPositionY.text = position.y.ToString();
        }
        private void SetTileViewRotation(TileRotation rotation, bool sendCallback = true)
        {
            if (sendCallback)
            {
                twRotation.value = rotation;
                return;
            }

            twRotation.SetValueWithoutNotify(rotation);
        }
        private void SetTileViewId(string id)
        {
            twId.text = id;
        }
        private void SetTileViewImage(Sprite sprite, TileRotation tileRotation)
        {
            twImage.style.backgroundImage = new StyleBackground(sprite);
            twImage.transform.rotation = Quaternion.Euler(new Vector3(0,0,(int)tileRotation));
        }
        private void SetTileViewCityName(string nameToSet, bool sendCallback = true)
        {
            if (string.IsNullOrEmpty(nameToSet))
            {
                nameToSet = NameGenerator.GetRandomName();
            }
            
            if (sendCallback)
            {
                twCityName.value = nameToSet;
                return;
            }

            twCityName.SetValueWithoutNotify(nameToSet);
            currentTileButtonData.cityName = nameToSet;
        }
        private void SetTileViewOwnerCity(string ownerCity, bool sendCallback = true)
        { 
            if (sendCallback)
            {
                twOwnerCity.value = ownerCity;
                return;
            }

            if (string.IsNullOrEmpty(ownerCity))
            {
                twOwnerCity.SetValueWithoutNotify(tileNotOwned);
            }
            else
            {
                twOwnerCity.SetValueWithoutNotify(ownerCity);
            }
            
            currentTileButtonData.cityOwner = ownerCity;
        }

    #endregion

    #region GRID TILE BUTTON FUNCTIONS

        private void ClearGridTileButton()
        {
            SetGridTileButtonImage(null, TileRotation.Up);
            currentTileButtonData.tile = null;
            currentTileButtonData.rotation = TileRotation.Up;
            currentTileButtonData.cityName = string.Empty;
            currentTileButtonData.cityOwner = string.Empty;
        }

        private void SetGridTileButtonImage(Sprite sprite, TileRotation tileRotation)
        {
            currentGridButton.style.backgroundImage = new StyleBackground(sprite);
            currentGridButton.transform.rotation = Quaternion.Euler(new Vector3(0,0,(int)tileRotation));
        }
        
    #endregion
    
    #region UI CALLBACKS

        private void OnGridWidthChanged(ChangeEvent<int> changeEvent)
        {
            RemoveTileView();
            mapConfiguration.tiles.Clear();
            mapConfiguration.gridWidth = changeEvent.newValue;
            BuildGrid();
        }
        private void OnGridHeightChanged(ChangeEvent<int> changeEvent)
        {
            RemoveTileView();
            mapConfiguration.tiles.Clear();
            mapConfiguration.gridHeight = changeEvent.newValue;
            BuildGrid();
        }
        private void OnGridTileButtonClicked(MouseUpEvent mouseUpEvent)
        {
            if (tileSelector == null)
            {
                BuildTileSelector();
            }

         
            if (currentGridButton != null)
            {
                currentGridButton.style.borderBottomColor = new Color(0.14f,0.14f,0.14f);
                currentGridButton.style.borderTopColor = new Color(0.19f,0.19f,0.19f);
                currentGridButton.style.borderRightColor = new Color(0.19f,0.19f,0.19f);
                currentGridButton.style.borderLeftColor = new Color(0.19f,0.19f,0.19f);
            }
            
            currentGridButton = (Button)mouseUpEvent.target;
            
            currentGridButton.style.borderBottomColor = Color.red;
            currentGridButton.style.borderTopColor = Color.red;
            currentGridButton.style.borderRightColor = Color.red;
            currentGridButton.style.borderLeftColor = Color.red;
            
            tileButtonDatas.TryGetValue(currentGridButton, out TileButtonData tileButtonData);
            currentTileButtonData = tileButtonData;

            RemoveTileView();
            
            if (currentTileButtonData.tile == null)
            {
                SetTileSelector(null, false);
                return;
            }
            
            BuildTileView(currentTileButtonData);
            SetTileView(currentTileButtonData);
        }
        private void OnTilePrefabSelected(ChangeEvent<Object> evt)
        {
            currentTileButtonData.rotation = TileRotation.Up;
            currentTileButtonData.cityName = string.Empty;
            currentTileButtonData.cityOwner = string.Empty;
            
            if (evt.newValue == null)
            {
                RemoveTileFromScriptableObject(currentTileButtonData.position);
                RemoveTileView();
                ClearGridTileButton();
                return;
            }
            
            RemoveTileFromScriptableObject(currentTileButtonData.position);
            RemoveTileView();
            currentTileButtonData.tile = (Tile)evt.newValue;
            BuildTileView(currentTileButtonData);
            SetTileView(currentTileButtonData);
            AddTileToScriptableObject(currentTileButtonData);
            SetGridTileButtonImage(currentTileButtonData.tile.GetSprite(), currentTileButtonData.rotation);
            EditorUtility.SetDirty(target);
        }
       
        private void OnCityNameChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrEmpty(evt.newValue))
            {
                Debug.LogWarning("City must have a name!");
                return;
            }

            var mapTile = mapConfiguration.tiles.Find(tile => tile.position == currentTileButtonData.position);
            mapTile.cityName = evt.newValue;
            currentTileButtonData.cityName = mapTile.cityName;
            
            var ownedMapTiles = mapConfiguration.tiles.FindAll(tile => tile.cityOwner == evt.previousValue);
            foreach (var tileConfiguration in ownedMapTiles)
            {
                tileConfiguration.cityOwner = currentTileButtonData.cityName;
                foreach (var tileButtonData in tileButtonDatas)
                {
                    if (tileButtonData.Value.position == tileConfiguration.position)
                    {
                        tileButtonData.Value.cityOwner = currentTileButtonData.cityName;
                    }
                }
            }
            
            EditorUtility.SetDirty(target);
        }
        private void OnTileRotationChanged(ChangeEvent<Enum> evt)
        {
            currentTileButtonData.rotation = (TileRotation)evt.newValue;
            var mapTile = mapConfiguration.tiles.Find(tile => tile.position == currentTileButtonData.position);
            mapTile.rotation = currentTileButtonData.rotation;
            SetGridTileButtonImage(currentTileButtonData.tile.GetSprite(),currentTileButtonData.rotation);
            SetTileViewImage(currentTileButtonData.tile.GetSprite(), currentTileButtonData.rotation);
            EditorUtility.SetDirty(target);
        }
        private void OnTileOwnerCityChanged(ChangeEvent<string> evt)
        {
            var mapTile = mapConfiguration.tiles.Find(tile => tile.position == currentTileButtonData.position);
            if (evt.newValue == tileNotOwned)
            {
                mapTile.cityOwner = string.Empty;
                currentTileButtonData.cityOwner = string.Empty;
            }
            else
            {
                mapTile.cityOwner = evt.newValue;
                currentTileButtonData.cityOwner = mapTile.cityOwner;
            }
         
            EditorUtility.SetDirty(target);
        }
        private void OnDeleteTileButton(MouseUpEvent mouseUpEvent)
        {
            tileSelector.value = null;
        }
        private void OnRegenerateCityName(MouseUpEvent mouseUpEvent)
        {
            SetTileViewCityName(string.Empty);
        }
        
        

    #endregion

    #region SCRIPTABLEOBJECT FUNCTIONS

        private void AddTileToScriptableObject(TileButtonData tileButtonData)
        {
            var tileToSave = new TileConfiguration(
                id: tileButtonData.tile.id,
                tileRotation:(float)tileButtonData.rotation,
                position: new Vector3((int) tileButtonData.position.x, 0 ,tileButtonData.position.y),
                cityName: tileButtonData.cityName,
                cityOwner: tileButtonData.cityOwner
                );
            mapConfiguration.tiles.Add(tileToSave);
        }

        private void RemoveTileFromScriptableObject(Vector2Int positionToRemove)
        {
            if (mapConfiguration.tiles.Count == 0) return;
                
            var tileConfiguration = mapConfiguration.tiles.Find(tile => tile.position == positionToRemove);
            if (tileConfiguration != null)
            {
                mapConfiguration.tiles.Remove(tileConfiguration);
            }
        }

    #endregion
    }
    
    [Serializable]
    public class TileButtonData
    {
        public Tile tile;
        public Vector2Int position;
        public TileRotation rotation;
        public string cityName;
        public string cityOwner;

        public TileButtonData(Tile tile, Vector2Int position, TileRotation rotation)
        {
            this.tile = tile;
            this.position = position;
            this.rotation = rotation;
        }
    }
}
