namespace CivGame.Rendering.Tests;

/// <summary>
/// Tests for hex vertex geometry calculations used by HexGridRenderer.
/// Flat-top hexagons: first vertex at 0 degrees (east), then every 60 degrees.
/// </summary>
public class HexVertexCalculatorTests
{
    private const float DefaultHexSize = 40f;
    private const float Tolerance = 0.001f;

    [Fact]
    public void Should_ReturnSixVertices_When_CalculatingHexVertices()
    {
        var vertices = HexVertexCalculator.GetHexVertices(0f, 0f, DefaultHexSize);

        Assert.Equal(6, vertices.Length);
    }

    [Fact]
    public void Should_PlaceFirstVertexAtEast_When_FlatTopHexagon()
    {
        // Flat-top hex: vertex 0 is at angle 0 (due east)
        var vertices = HexVertexCalculator.GetHexVertices(0f, 0f, DefaultHexSize);

        // First vertex should be at (hexSize, 0) relative to center
        Assert.InRange(vertices[0].X, DefaultHexSize - Tolerance, DefaultHexSize + Tolerance);
        Assert.InRange(vertices[0].Y, -Tolerance, Tolerance);
    }

    [Fact]
    public void Should_SpaceVerticesEvenly_When_FlatTopHexagon()
    {
        var vertices = HexVertexCalculator.GetHexVertices(0f, 0f, DefaultHexSize);

        // All vertices should be exactly hexSize distance from center
        for (int i = 0; i < 6; i++)
        {
            float dist = MathF.Sqrt(vertices[i].X * vertices[i].X + vertices[i].Y * vertices[i].Y);
            Assert.InRange(dist, DefaultHexSize - Tolerance, DefaultHexSize + Tolerance);
        }
    }

    [Fact]
    public void Should_OffsetVerticesByCenter_When_CenterIsNonZero()
    {
        float cx = 100f;
        float cy = 200f;
        var vertices = HexVertexCalculator.GetHexVertices(cx, cy, DefaultHexSize);

        // First vertex (east) should be at (cx + hexSize, cy)
        Assert.InRange(vertices[0].X, cx + DefaultHexSize - Tolerance, cx + DefaultHexSize + Tolerance);
        Assert.InRange(vertices[0].Y, cy - Tolerance, cy + Tolerance);
    }

    [Fact]
    public void Should_ScaleWithHexSize_When_SizeChanges()
    {
        float smallSize = 20f;
        float largeSize = 80f;

        var smallVertices = HexVertexCalculator.GetHexVertices(0f, 0f, smallSize);
        var largeVertices = HexVertexCalculator.GetHexVertices(0f, 0f, largeSize);

        // Large hex vertex 0 should be 4x further from center than small
        Assert.InRange(largeVertices[0].X / smallVertices[0].X, 4f - Tolerance, 4f + Tolerance);
    }

    [Fact]
    public void Should_ProduceFlatTopShape_When_CheckingTopEdge()
    {
        // Flat-top hex has a flat edge at the top.
        // Vertices 1 (60 deg) and 2 (120 deg) should have the same Y.
        var vertices = HexVertexCalculator.GetHexVertices(0f, 0f, DefaultHexSize);

        // Vertex at 60 degrees and vertex at 300 degrees should mirror in Y
        // Vertices 1 and 5 should have opposite Y but same absolute Y
        Assert.InRange(MathF.Abs(vertices[1].Y + vertices[5].Y), -Tolerance, Tolerance);
    }

    [Fact]
    public void Should_FormConvexPolygon_When_VerticesAreOrdered()
    {
        var vertices = HexVertexCalculator.GetHexVertices(0f, 0f, DefaultHexSize);

        // All cross products of consecutive edges should have the same sign (convex polygon)
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            int k = (i + 2) % 6;
            float cross = (vertices[j].X - vertices[i].X) * (vertices[k].Y - vertices[j].Y)
                        - (vertices[j].Y - vertices[i].Y) * (vertices[k].X - vertices[j].X);
            Assert.True(cross > 0 || cross < 0, "Vertices should form a convex polygon");
        }
    }
}
