// Common use functions for fluid dynamics shaders

// Cross product of 2d vectors
float cross2d(float2 a, float2 b)
{
    return a.x * b.y - a.y * b.x;
}

float RandomFloat(in float2 uv)
{
    float2 noise = (frac(sin(dot(uv ,float2(12.9898,78.233)*2.0)) * 43758.5453));
    return abs(noise.x + noise.y) * 0.5;
}

float2 RandomFloat2(in float2 uv)
{
    return float2(RandomFloat(uv), RandomFloat(float2(uv.y, uv.x)));
}

// Return the vector capped to a magnitude (length)
float2 CapMagnitude(float2 v, float maxMagnitude)
{
    float l = length(v);
    if (l > maxMagnitude)
    {
        return normalize(v)*maxMagnitude;
    }
    return v;
}


// Return the intersection point of two line segments in 2D space
bool IntersectLineSegments2D(float2 p1start, float2 p1end, float2 p2start, float2 p2end, out float2 intersection)
{
        float2 p = p1start;
        float2 r = p1end - p1start;
        float2 q = p2start;
        float2 s = p2end - p2start;
        float2 qminusp = q - p;

        if (abs(cross2d(r,s)) < 0.000001)
        {
            // Parallel lines
            if (abs(cross2d(qminusp, r)) < 0.000001)
            {
                // Co-linear lines, could overlap
                float rdotr = dot(r, r);
                float sdotr = dot(s, r);
                // this means lines are co-linear
                // they may or may not be overlapping
                float t0 = dot(qminusp, r / rdotr);
                float t1 = t0 + sdotr / rdotr;
                if (sdotr < 0)
                {
                    // lines were facing in different directions so t1 > t0, swap to simplify check
                    float temp = t0;
                    t0 = t1;
                    t1 = temp;
                }
                if (t0 <= 1 && t1 >= 0)
                {
                    // Nice half-way point intersection
                    float t = lerp(max(0, t0), min(1, t1), 0.5f);
                    intersection = p + t * r;
                    return true;
                } 
                // Co-linear but disjoint
                intersection = float2(0,0);
                return false;
            } 
            // Just parallel in different places, cannot intersect
            intersection = float2(0,0);
            return false;
        }
        // Not parallel, calculate t and u
        float t = cross2d(qminusp, s) / cross2d(r, s);
        float u = cross2d(qminusp, r) / cross2d(r, s);
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = p + t * r;
            return true;
        }
        // Lines only cross outside segment range
        intersection = float2(0, 0);
        return false;
}

// Return a point on a line that goes through b and c that is closest to point a
float2 NearestPointOnLine(in float2 a, in float2 b, in float2 c)
{
    float2 lineDir = normalize(b - c);
    return b + lineDir * dot(a - b, lineDir);
}

// Return true if infinite line defined by it's two points linePointA and linePointB intersects with circle defined by center and radius
// If true, intersectionA and intersectionB will contain the two intersection points
bool LineCircleIntersection(in float2 circleCenter, in float radius, in float2 linePointA, in float2 linePointB, out float2 intersectionA, out float2 intersectionB)
{
    float2 nearest = NearestPointOnLine(circleCenter, linePointA, linePointB);
    float2 toCircle = circleCenter - nearest;
    if (length(toCircle) > radius) return false;
       
    float chordHalf = sqrt(radius * radius - dot(toCircle,toCircle)); //dot product = length squared

    intersectionA = nearest - chordHalf * normalize(linePointB - linePointA);
    intersectionB = nearest + chordHalf * normalize(linePointB - linePointA);
    return true;
}

bool IsLinePointBetween(in float2 p, in float2 end1, in float2 end2)
{
    return dot(p - end1, p - end2) <= 0;
}

/// Return true if the line segment intersects the circle. Gives the intersection that is closer to linePointA if there are two intersections. 
bool LineSegmentCircleIntersection(in float2 circleCenter, in float radius, in float2 linePointA, in float2 linePointB, out float2 intersection)
{
    float2 intersection1, intersection2;
    bool intersects = LineCircleIntersection(circleCenter, radius, linePointA, linePointB, intersection1, intersection2);
    if (!intersects) return false;
    
    if (IsLinePointBetween(intersection1, linePointA, linePointB))
    {
        intersection = intersection1;
        return true;
    }
    if (IsLinePointBetween(intersection2, linePointA, linePointB))
    {
        intersection = intersection2;
        return true;
    }

    return false;
}

