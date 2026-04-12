using Godot;
using CivGame.Cities;
using CivGame.World;

namespace CivGame.Rendering;

/// <summary>
/// Godot Node2D wrapper around CityRenderer. Draws all cities as brown squares
/// with name labels above them.
/// </summary>
public partial class CityRendererNode : Node2D
{
    public CityRenderer Data { get; } = new();

    public void Initialize(CityManager cityManager, HexGrid grid, float hexSize)
    {
        Data.Initialize(cityManager, grid, hexSize);
    }

    public void Refresh()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Data.Manager is null || Data.Grid is null) return;

        var (cr, cg, cb) = CityRenderer.CityColor;
        var cityColor = new Color(cr, cg, cb);
        float half = CityRenderer.CityHalfSize;

        foreach (var city in Data.Manager.AllCities)
        {
            var (px, py) = HexGrid.HexToPixel(city.Position, Data.HexSize);

            // Draw filled square
            var rect = new Rect2(px - half, py - half, half * 2, half * 2);
            DrawRect(rect, cityColor);

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
