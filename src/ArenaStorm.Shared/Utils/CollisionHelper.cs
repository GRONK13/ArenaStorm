namespace ArenaStorm.Shared.Utils;

/// <summary>
/// Geometric collision detection and distance utility methods.
/// </summary>
public static class CollisionHelper
{
    /// <summary>
    /// Computes the Euclidean distance between two points.
    /// </summary>
    /// <param name="x1">X coordinate of first point.</param>
    /// <param name="y1">Y coordinate of first point.</param>
    /// <param name="x2">X coordinate of second point.</param>
    /// <param name="y2">Y coordinate of second point.</param>
    /// <returns>The Euclidean distance.</returns>
    public static float Distance(float x1, float y1, float x2, float y2)
    {
        return MathF.Sqrt(DistanceSquared(x1, y1, x2, y2));
    }

    /// <summary>
    /// Computes the squared Euclidean distance between two points.
    /// </summary>
    /// <param name="x1">X coordinate of first point.</param>
    /// <param name="y1">Y coordinate of first point.</param>
    /// <param name="x2">X coordinate of second point.</param>
    /// <param name="y2">Y coordinate of second point.</param>
    /// <returns>The squared distance.</returns>
    public static float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>
    /// Checks whether two circles overlap.
    /// </summary>
    /// <param name="x1">X center of first circle.</param>
    /// <param name="y1">Y center of first circle.</param>
    /// <param name="r1">Radius of first circle.</param>
    /// <param name="x2">X center of second circle.</param>
    /// <param name="y2">Y center of second circle.</param>
    /// <param name="r2">Radius of second circle.</param>
    /// <returns><c>true</c> if the circles intersect; otherwise, <c>false</c>.</returns>
    public static bool CheckCircleCollision(float x1, float y1, float r1, float x2, float y2, float r2)
    {
        float radiusSum = r1 + r2;
        return DistanceSquared(x1, y1, x2, y2) <= (radiusSum * radiusSum);
    }

    /// <summary>
    /// Checks whether a point is contained within an axis-aligned rectangle.
    /// </summary>
    /// <param name="px">Point X coordinate.</param>
    /// <param name="py">Point Y coordinate.</param>
    /// <param name="rx">Rectangle X position (top-left or bottom-left).</param>
    /// <param name="ry">Rectangle Y position (top-left or bottom-left).</param>
    /// <param name="rw">Rectangle width.</param>
    /// <param name="rh">Rectangle height.</param>
    /// <returns><c>true</c> if the point lies inside the rectangle; otherwise, <c>false</c>.</returns>
    public static bool CheckPointInRect(float px, float py, float rx, float ry, float rw, float rh)
    {
        return px >= rx && px <= (rx + rw) && py >= ry && py <= (ry + rh);
    }

    /// <summary>
    /// Checks whether a circle intersects an axis-aligned bounding box (AABB).
    /// </summary>
    /// <param name="cx">Circle center X.</param>
    /// <param name="cy">Circle center Y.</param>
    /// <param name="cr">Circle radius.</param>
    /// <param name="rx">Rectangle min X position.</param>
    /// <param name="ry">Rectangle min Y position.</param>
    /// <param name="rw">Rectangle width.</param>
    /// <param name="rh">Rectangle height.</param>
    /// <returns><c>true</c> if the circle and rectangle intersect; otherwise, <c>false</c>.</returns>
    public static bool CheckCircleRect(float cx, float cy, float cr, float rx, float ry, float rw, float rh)
    {
        float closestX = Math.Clamp(cx, rx, rx + rw);
        float closestY = Math.Clamp(cy, ry, ry + rh);
        return DistanceSquared(cx, cy, closestX, closestY) <= (cr * cr);
    }
}
