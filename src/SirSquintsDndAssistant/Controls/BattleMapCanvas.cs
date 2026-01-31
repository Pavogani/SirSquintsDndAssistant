using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SirSquintsDndAssistant.Models.BattleMap;
using SirSquintsDndAssistant.Rendering;
using SirSquintsDndAssistant.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SirSquintsDndAssistant.Controls;

/// <summary>
/// Custom canvas control for rendering and interacting with the battle map.
/// </summary>
public class BattleMapCanvas : SKCanvasView
{
    private readonly MapRenderer _renderer;

    // Touch/mouse state
    private SKPoint _lastTouchPosition;
    private SKPoint _panStartPosition;
    private bool _isPanning;
    private bool _isDraggingToken;
    private MapToken? _draggedToken;

    #region Bindable Properties

    public static readonly BindableProperty MapProperty =
        BindableProperty.Create(nameof(Map), typeof(BattleMap), typeof(BattleMapCanvas),
            propertyChanged: OnMapChanged);

    public static readonly BindableProperty TokensProperty =
        BindableProperty.Create(nameof(Tokens), typeof(ObservableCollection<MapToken>), typeof(BattleMapCanvas),
            propertyChanged: OnTokensChanged);

    public static readonly BindableProperty SelectedTokenProperty =
        BindableProperty.Create(nameof(SelectedToken), typeof(MapToken), typeof(BattleMapCanvas),
            propertyChanged: OnSelectedTokenChanged);

    public static readonly BindableProperty CurrentToolProperty =
        BindableProperty.Create(nameof(CurrentTool), typeof(MapTool), typeof(BattleMapCanvas),
            defaultValue: MapTool.Select, propertyChanged: OnCurrentToolChanged);

    public static readonly BindableProperty ShowGridProperty =
        BindableProperty.Create(nameof(ShowGrid), typeof(bool), typeof(BattleMapCanvas),
            defaultValue: true, propertyChanged: OnShowGridChanged);

    public static readonly BindableProperty UseFogOfWarProperty =
        BindableProperty.Create(nameof(UseFogOfWar), typeof(bool), typeof(BattleMapCanvas),
            defaultValue: false, propertyChanged: OnUseFogOfWarChanged);

    public static readonly BindableProperty IsDmViewProperty =
        BindableProperty.Create(nameof(IsDmView), typeof(bool), typeof(BattleMapCanvas),
            defaultValue: true, propertyChanged: OnIsDmViewChanged);

    public static readonly BindableProperty ZoomLevelProperty =
        BindableProperty.Create(nameof(ZoomLevel), typeof(float), typeof(BattleMapCanvas),
            defaultValue: 1.0f, propertyChanged: OnZoomLevelChanged);

    public BattleMap? Map
    {
        get => (BattleMap?)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    public ObservableCollection<MapToken>? Tokens
    {
        get => (ObservableCollection<MapToken>?)GetValue(TokensProperty);
        set => SetValue(TokensProperty, value);
    }

    public MapToken? SelectedToken
    {
        get => (MapToken?)GetValue(SelectedTokenProperty);
        set => SetValue(SelectedTokenProperty, value);
    }

    public MapTool CurrentTool
    {
        get => (MapTool)GetValue(CurrentToolProperty);
        set => SetValue(CurrentToolProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool UseFogOfWar
    {
        get => (bool)GetValue(UseFogOfWarProperty);
        set => SetValue(UseFogOfWarProperty, value);
    }

    public bool IsDmView
    {
        get => (bool)GetValue(IsDmViewProperty);
        set => SetValue(IsDmViewProperty, value);
    }

    public float ZoomLevel
    {
        get => (float)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    #endregion

    #region Events

    /// <summary>Fired when a cell is tapped.</summary>
    public event EventHandler<MapCellEventArgs>? CellTapped;

    /// <summary>Fired when a token is selected.</summary>
    public event EventHandler<MapTokenEventArgs>? TokenSelected;

    /// <summary>Fired when a token is dragged to a new position.</summary>
    public event EventHandler<MapTokenDragEventArgs>? TokenDragged;

    /// <summary>Fired when the viewport changes (pan/zoom).</summary>
    public event EventHandler<MapViewportEventArgs>? ViewportChanged;

    /// <summary>Fired when fog of war cells are revealed or hidden.</summary>
    public event EventHandler<MapFogEventArgs>? FogChanged;

    /// <summary>Fired when a measurement is completed.</summary>
    public event EventHandler<MapMeasurementEventArgs>? MeasurementCompleted;

    #endregion

    public BattleMapCanvas()
    {
        _renderer = new MapRenderer();

        // Enable touch/mouse events
        EnableTouchEvents = true;

        // Set default cell size
        _renderer.CellPixelSize = 40f;
    }

    #region Property Changed Handlers

    private static void OnMapChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.SetMap(newValue as BattleMap);
        canvas.InvalidateSurface();
    }

    private static void OnTokensChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;

        if (oldValue is ObservableCollection<MapToken> oldCollection)
        {
            oldCollection.CollectionChanged -= canvas.OnTokensCollectionChanged;
        }

        if (newValue is ObservableCollection<MapToken> newCollection)
        {
            newCollection.CollectionChanged += canvas.OnTokensCollectionChanged;
            canvas._renderer.SetTokens(newCollection);
        }
        else
        {
            canvas._renderer.SetTokens(null);
        }

        canvas.InvalidateSurface();
    }

    private void OnTokensCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _renderer.SetTokens(Tokens);
        InvalidateSurface();
    }

    private static void OnSelectedTokenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.SelectedToken = newValue as MapToken;
        canvas.InvalidateSurface();
    }

    private static void OnCurrentToolChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.CurrentTool = (MapTool)newValue;
        canvas.InvalidateSurface();
    }

    private static void OnShowGridChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.ShowGrid = (bool)newValue;
        canvas.InvalidateSurface();
    }

    private static void OnUseFogOfWarChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.UseFogOfWar = (bool)newValue;
        canvas.InvalidateSurface();
    }

    private static void OnIsDmViewChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.IsDmView = (bool)newValue;
        canvas.InvalidateSurface();
    }

    private static void OnZoomLevelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var canvas = (BattleMapCanvas)bindable;
        canvas._renderer.ZoomLevel = (float)newValue;
        canvas.InvalidateSurface();
    }

    #endregion

    #region Rendering

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        _renderer.Render(e.Surface.Canvas, e.Info);
    }

    #endregion

    #region Touch Handling

    protected override void OnTouch(SKTouchEventArgs e)
    {
        base.OnTouch(e);

        var position = new SKPoint(e.Location.X, e.Location.Y);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                HandleTouchPressed(position, e);
                break;
            case SKTouchAction.Moved:
                HandleTouchMoved(position, e);
                break;
            case SKTouchAction.Released:
                HandleTouchReleased(position, e);
                break;
            case SKTouchAction.Cancelled:
                HandleTouchCancelled();
                break;
        }

        e.Handled = true;
    }

    private void HandleTouchPressed(SKPoint position, SKTouchEventArgs e)
    {
        _lastTouchPosition = position;
        _panStartPosition = position;

        var (gridX, gridY) = _renderer.ScreenToGrid(position);
        var token = _renderer.GetTokenAtScreen(position);

        switch (CurrentTool)
        {
            case MapTool.Pan:
                _isPanning = true;
                break;

            case MapTool.Select:
                if (token != null)
                {
                    SelectedToken = token;
                    TokenSelected?.Invoke(this, new MapTokenEventArgs(token));
                }
                else
                {
                    SelectedToken = null;
                }
                CellTapped?.Invoke(this, new MapCellEventArgs(gridX, gridY));
                break;

            case MapTool.Move:
                if (token != null)
                {
                    _isDraggingToken = true;
                    _draggedToken = token;
                    SelectedToken = token;
                }
                break;

            case MapTool.MeasureLine:
            case MapTool.MeasureCone:
            case MapTool.MeasureCircle:
            case MapTool.MeasureSquare:
                _renderer.MeasureStart = (gridX, gridY);
                _renderer.MeasureEnd = (gridX, gridY);
                break;

            case MapTool.RevealFog:
                RevealFogAt(gridX, gridY);
                break;

            case MapTool.HideFog:
                HideFogAt(gridX, gridY);
                break;

            case MapTool.PlaceToken:
                CellTapped?.Invoke(this, new MapCellEventArgs(gridX, gridY));
                break;
        }

        // Update hover
        _renderer.HoverCell = (gridX, gridY);
        InvalidateSurface();
    }

    private void HandleTouchMoved(SKPoint position, SKTouchEventArgs e)
    {
        var (gridX, gridY) = _renderer.ScreenToGrid(position);

        if (_isPanning)
        {
            float deltaX = position.X - _lastTouchPosition.X;
            float deltaY = position.Y - _lastTouchPosition.Y;
            _renderer.Pan(deltaX, deltaY);
        }
        else if (_isDraggingToken && _draggedToken != null)
        {
            // Update hover to show where token will be placed
            _renderer.HoverCell = (gridX, gridY);
        }
        else if (CurrentTool >= MapTool.MeasureLine && CurrentTool <= MapTool.MeasureSquare)
        {
            _renderer.MeasureEnd = (gridX, gridY);
        }
        else if (CurrentTool == MapTool.RevealFog)
        {
            RevealFogAt(gridX, gridY);
        }
        else if (CurrentTool == MapTool.HideFog)
        {
            HideFogAt(gridX, gridY);
        }
        else
        {
            _renderer.HoverCell = (gridX, gridY);
        }

        _lastTouchPosition = position;
        InvalidateSurface();
    }

    private void HandleTouchReleased(SKPoint position, SKTouchEventArgs e)
    {
        var (gridX, gridY) = _renderer.ScreenToGrid(position);

        if (_isPanning)
        {
            _isPanning = false;
            ViewportChanged?.Invoke(this, new MapViewportEventArgs(
                _renderer.ViewportOffset.X, _renderer.ViewportOffset.Y, _renderer.ZoomLevel));
        }
        else if (_isDraggingToken && _draggedToken != null)
        {
            // Complete token drag
            int oldX = _draggedToken.GridX;
            int oldY = _draggedToken.GridY;

            if (gridX != oldX || gridY != oldY)
            {
                TokenDragged?.Invoke(this, new MapTokenDragEventArgs(_draggedToken, oldX, oldY, gridX, gridY));
            }

            _isDraggingToken = false;
            _draggedToken = null;
        }
        else if (CurrentTool >= MapTool.MeasureLine && CurrentTool <= MapTool.MeasureSquare)
        {
            if (_renderer.MeasureStart.HasValue && _renderer.MeasureEnd.HasValue)
            {
                var start = _renderer.MeasureStart.Value;
                var end = _renderer.MeasureEnd.Value;

                MeasurementCompleted?.Invoke(this, new MapMeasurementEventArgs(
                    start.x, start.y, end.x, end.y, CurrentTool));

                _renderer.MeasureStart = null;
                _renderer.MeasureEnd = null;
            }
        }
        else if (CurrentTool == MapTool.RevealFog || CurrentTool == MapTool.HideFog)
        {
            // Fog changes are complete, fire event
            FogChanged?.Invoke(this, new MapFogEventArgs(_renderer.GetRevealedCells()));
        }

        InvalidateSurface();
    }

    private void HandleTouchCancelled()
    {
        _isPanning = false;
        _isDraggingToken = false;
        _draggedToken = null;
        _renderer.MeasureStart = null;
        _renderer.MeasureEnd = null;
        InvalidateSurface();
    }

    #endregion

    #region Fog of War Helpers

    private void RevealFogAt(int gridX, int gridY)
    {
        // Reveal a 3x3 area (brush)
        _renderer.RevealCells(gridX - 1, gridY - 1, 3, 3);
    }

    private void HideFogAt(int gridX, int gridY)
    {
        // Hide a 3x3 area (brush)
        _renderer.HideCells(gridX - 1, gridY - 1, 3, 3);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Pan the viewport by a delta amount.
    /// </summary>
    public void Pan(float deltaX, float deltaY)
    {
        _renderer.Pan(deltaX, deltaY);
        InvalidateSurface();
    }

    /// <summary>
    /// Zoom the viewport.
    /// </summary>
    public void Zoom(float zoomDelta)
    {
        var center = new SKPoint((float)Width / 2, (float)Height / 2);
        _renderer.Zoom(zoomDelta, center);
        InvalidateSurface();
    }

    /// <summary>
    /// Center the viewport on a grid position.
    /// </summary>
    public void CenterOn(int gridX, int gridY)
    {
        _renderer.CenterOnGrid(gridX, gridY, (float)Width, (float)Height);
        InvalidateSurface();
    }

    /// <summary>
    /// Reset the viewport to default position and zoom.
    /// </summary>
    public void ResetViewport()
    {
        _renderer.ResetViewport();
        InvalidateSurface();
    }

    /// <summary>
    /// Get the current revealed cells for saving.
    /// </summary>
    public HashSet<string> GetRevealedCells()
    {
        return _renderer.GetRevealedCells();
    }

    /// <summary>
    /// Load a background image for the map.
    /// </summary>
    public async Task LoadBackgroundImageAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            _renderer.SetBackgroundImage(null);
            return;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            var bitmap = SKBitmap.Decode(stream);
            _renderer.SetBackgroundImage(bitmap);
            InvalidateSurface();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading background image: {ex.Message}");
        }
    }

    #endregion
}

#region Event Args

public class MapCellEventArgs : EventArgs
{
    public int GridX { get; }
    public int GridY { get; }

    public MapCellEventArgs(int gridX, int gridY)
    {
        GridX = gridX;
        GridY = gridY;
    }
}

public class MapTokenEventArgs : EventArgs
{
    public MapToken Token { get; }

    public MapTokenEventArgs(MapToken token)
    {
        Token = token;
    }
}

public class MapTokenDragEventArgs : EventArgs
{
    public MapToken Token { get; }
    public int OldGridX { get; }
    public int OldGridY { get; }
    public int NewGridX { get; }
    public int NewGridY { get; }

    public MapTokenDragEventArgs(MapToken token, int oldX, int oldY, int newX, int newY)
    {
        Token = token;
        OldGridX = oldX;
        OldGridY = oldY;
        NewGridX = newX;
        NewGridY = newY;
    }
}

public class MapViewportEventArgs : EventArgs
{
    public float OffsetX { get; }
    public float OffsetY { get; }
    public float Zoom { get; }

    public MapViewportEventArgs(float offsetX, float offsetY, float zoom)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        Zoom = zoom;
    }
}

public class MapFogEventArgs : EventArgs
{
    public HashSet<string> RevealedCells { get; }

    public MapFogEventArgs(HashSet<string> revealedCells)
    {
        RevealedCells = revealedCells;
    }
}

public class MapMeasurementEventArgs : EventArgs
{
    public int StartX { get; }
    public int StartY { get; }
    public int EndX { get; }
    public int EndY { get; }
    public MapTool MeasurementType { get; }

    public MapMeasurementEventArgs(int startX, int startY, int endX, int endY, MapTool type)
    {
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        MeasurementType = type;
    }
}

#endregion
