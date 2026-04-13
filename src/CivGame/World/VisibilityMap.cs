using CivGame.Cities;
using CivGame.Units;

namespace CivGame.World;

public sealed class VisibilityMap
{
    public const int DefaultSightRadius = 2;

    private readonly HexGrid _grid;
    private readonly Dictionary<int, VisibilityState[,]> _stateByPlayer = new();

    public VisibilityMap(HexGrid grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
    }

    public VisibilityState IsAt(int ownerId, HexCoord coord)
    {
        if (!_grid.InBounds(coord)) return VisibilityState.Unseen;
        if (!_stateByPlayer.TryGetValue(ownerId, out var arr)) return VisibilityState.Unseen;
        return arr[coord.Q, coord.R];
    }

    public bool IsVisibleTo(int ownerId, HexCoord coord) =>
        IsAt(ownerId, coord) == VisibilityState.Visible;

    public bool IsExplored(int ownerId, HexCoord coord)
    {
        var s = IsAt(ownerId, coord);
        return s == VisibilityState.Visible || s == VisibilityState.Explored;
    }

    public void Recompute(int ownerId, IEnumerable<HexCoord> observerPositions, int sightRadius = DefaultSightRadius)
    {
        if (observerPositions is null) throw new ArgumentNullException(nameof(observerPositions));
        if (sightRadius < 0) throw new ArgumentOutOfRangeException(nameof(sightRadius));

        if (!_stateByPlayer.TryGetValue(ownerId, out var arr))
        {
            arr = new VisibilityState[_grid.Width, _grid.Height];
            _stateByPlayer[ownerId] = arr;
        }

        // Step 1: demote existing Visible -> Explored.
        for (int q = 0; q < _grid.Width; q++)
            for (int r = 0; r < _grid.Height; r++)
            {
                if (arr[q, r] == VisibilityState.Visible)
                    arr[q, r] = VisibilityState.Explored;
            }

        // Step 2: mark every in-bounds cell within sightRadius of any observer as Visible.
        foreach (var obs in observerPositions)
        {
            for (int q = 0; q < _grid.Width; q++)
                for (int r = 0; r < _grid.Height; r++)
                {
                    var coord = new HexCoord(q, r);
                    if (obs.DistanceTo(coord) <= sightRadius)
                        arr[q, r] = VisibilityState.Visible;
                }
        }
    }

    public void RecomputeForPlayer(int ownerId, UnitManager units, CityManager cities, int sightRadius = DefaultSightRadius)
    {
        if (units is null) throw new ArgumentNullException(nameof(units));
        if (cities is null) throw new ArgumentNullException(nameof(cities));

        var positions = new List<HexCoord>();
        foreach (var u in units.UnitsOwnedBy(ownerId)) positions.Add(u.Position);
        foreach (var c in cities.CitiesOwnedBy(ownerId)) positions.Add(c.Position);

        Recompute(ownerId, positions, sightRadius);
    }
}
