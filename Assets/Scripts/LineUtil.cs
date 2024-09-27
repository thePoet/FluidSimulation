using UnityEngine;

public static class LineUtil 
{
  
    
    public static float CrossProduct2D(Vector2 a, Vector2 b) 
    {
        return a.x * b.y - b.x * a.y;
    }

    public static Vector2 NearestPointOnLine(Vector2 linePointA, Vector2 linePointB, Vector2 point)
    {
        Vector2 lineDir = (linePointA - linePointB).normalized;
        var v = point - linePointA;
        var d = Vector3.Dot(v, lineDir);
        return linePointA + lineDir * d;
    }
  
/// <summary>
/// Intersection of an infinite line and a circle.
/// </summary>
/// <param name="circleCenter"></param>
/// <param name="radius"></param>
/// <param name="linePointA">A point of the line</param>
/// <param name="linePointB">A different point of the line</param>
/// <returns>The intersection points - both are null if there is no intersection.</returns>
    public static (Vector2? interection1, Vector2? intersection2) LineCircleIntersection(Vector2 circleCenter, float radius, Vector2 linePointA, Vector2 linePointB)
    {
        Vector2 nearest = NearestPointOnLine(linePointA, linePointB, circleCenter);
        Vector2 toCircle = circleCenter - nearest;
        if (toCircle.magnitude > radius) return (null,null);
       
        float chordHalf = Mathf.Sqrt(radius * radius - toCircle.sqrMagnitude);

        return
        (
            nearest - chordHalf * (linePointB - linePointA).normalized,
            nearest + chordHalf * (linePointB - linePointA).normalized
        );
    }
   
    
    /// <summary>
    /// Intersection of a line segment and a circle. 
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="radius"></param>
    /// <param name="linePointA">Line segment end point</param>
    /// <param name="linePointB">Line segment end point</param>
    /// <returns>The two intersection points, one or both may be null.</returns>
    public static (Vector2? intersection1, Vector2? intersection2) LineSegmentCircleIntersection(Vector2 circleCenter, float radius, Vector2 linePointA,
        Vector2 linePointB)
    {
        (Vector2? intersection1, Vector2? intersection2) = LineCircleIntersection(circleCenter, radius, linePointA, linePointB);
        
        if (intersection1.HasValue && !IsLinePointBetween(intersection1.Value, linePointA, linePointB)) intersection1 = null;
        if (intersection2.HasValue && !IsLinePointBetween(intersection2.Value, linePointA, linePointB)) intersection2 = null;

        return (intersection1, intersection2);

        bool IsLinePointBetween(Vector2 point, Vector2 end1, Vector2 end2) => Vector2.Dot(point - end1, point - end2) <= 0f;
    }
    
    
    /// <summary>
    /// Determine whether 2 lines intersect, and give the intersection point if so.
    /// </summary>
    /// <param name="p1start">Start point of the first line</param>
    /// <param name="p1end">End point of the first line</param>
    /// <param name="p2start">Start point of the second line</param>
    /// <param name="p2end">End point of the second line</param>
    /// <returns>Intersection point if the lines intersect, null otherwise.</returns>
    public static Vector2? IntersectLineSegments2D(Vector2 p1start, Vector2 p1end, Vector2 p2start, Vector2 p2end) 
    {
        var p = p1start;
        var r = p1end - p1start;
        var q = p2start;
        var s = p2end - p2start;
        var qminusp = q - p;

        float cross_rs = CrossProduct2D(r, s);

        if (Approximately(cross_rs, 0f)) 
        {
            // Parallel lines
            if (Approximately(CrossProduct2D(qminusp, r), 0f)) 
            {
                // Co-linear lines, could overlap
                float rdotr = Vector2.Dot(r, r);
                float sdotr = Vector2.Dot(s, r);
                // this means lines are co-linear
                // they may or may not be overlapping
                float t0 = Vector2.Dot(qminusp, r / rdotr);
                float t1 = t0 + sdotr / rdotr;
                if (sdotr < 0) 
                {
                    // lines were facing in different directions so t1 > t0, swap to simplify check
                    Swap(ref t0, ref t1);
                }

                if (t0 <= 1 && t1 >= 0) 
                {
                    // Nice half-way point intersection
                    float a = Mathf.Lerp(Mathf.Max(0, t0), Mathf.Min(1, t1), 0.5f);
                    return p + a * r;
                    
                }
                // Co-linear but disjoint
                return null;
            }
            // Just parallel in different places, cannot intersect
            return null;
        }

        // Not parallel, calculate t and u
        float t = CrossProduct2D(qminusp, s) / cross_rs;
        float u = CrossProduct2D(qminusp, r) / cross_rs;
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            return p + t * r;
        }

        // Lines only cross outside segment range
        return null;
        
        void Swap<T>(ref T lhs, ref T rhs) 
        {
            (lhs, rhs) = (rhs, lhs);
        }

        bool Approximately(float a, float b, float tolerance = 1e-5f) 
        {
            return Mathf.Abs(a - b) <= tolerance;
        }
    }
}