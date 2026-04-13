using Godot;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around CityRenderer. Draws all cities as brown squares
/// with name labels above them.
/// Unseen cities are hidden; Explored cities render at half brightness.
/// </summary>
public partial class CityRendererNode : Node2D
{
    public CityRenderer Data { get; } = new();

    private VisibilityMap? _visibility;
    private int _viewerOwnerId;

    public void Initialize(CityManager cityManager, HexGrid grid, float hexSize,
        VisibilityMap? visibility = null, int viewerOwnerId = 0)
    {
        Data.Initialize(cityManager, grid, hexSize);
        _visibility = visibility;
        _viewerOwnerId = viewerOwnerId;
    }

    public void Refresh()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Data.Manager is null || Data.Grid is null) return;

        float half = CityRenderer.CityHalfSize;

        foreach (var city in Data.Manager.AllCities)
        {
            var renderState = _visibility is not null
                ? TileRenderStateResolver.Resolve(_visibility.IsAt(_viewerOwnerId, city.Position))
                : TileRenderState.Full;

            if (renderState == TileRenderState.Hidden)
                continue;

            var (cr, cg, cb) = CityRenderer.CityColor;
            if (renderState == TileRenderState.Dim)
                (cr, cg, cb) = HexColorMapper.Dim((cr, cg, cb), FogOfWarConstants.DefaultDimFactor);

            var (px, py) = HexGrid.HexToPixel(city.Position, Data.HexSize);

            // Draw filled square
            var rect = new Rect2(px - half, py - half, half * 2, half * 2);
            DrawRect(rect, new Color(cr, cg, cb));

            // Draw city name label above the square
            DrawString(
                ThemeDB.FallbackFont,
                new Vector2(px - half, py - half - 5f),
                city.Name,
                HorizontalAlignment.Left,
                -1,
                12,
                Colors.White);
        }
    }
}
