
#ifndef VORONOI_3D_INCLUDED
#define VORONOI_3D_INCLUDED

// Hash 3D: generates a pseudo-random value for each cell
float3 Hash33(float3 p)
{
    p = float3(
        dot(p, float3(127.1, 311.7, 74.7)),
        dot(p, float3(269.5, 183.3, 246.1)),
        dot(p, float3(113.5, 271.9, 124.6))
    );

    return frac(sin(p) * 43758.5453);
}

// Smooth minimum
float SmoothMin(float a, float b, float k)
{
    if (k <= 0.00001)
        return min(a, b);

    float h = max(k - abs(a - b), 0.0) / k;

    return min(a, b) - h * h * k * 0.25;
}

// 3D Voronoi F1
void Voronoi3D_float(
    float3 Position,
    float Scale,
    float Randomness,
    float Smoothness,
    out float Distance,
    out float3 CellColor
)
{
    float3 p = Position * Scale;

    float3 baseCell = floor(p);
    float3 localPos = frac(p);

    float minDist = 100000.0;
    float3 closestColor = 0.0;

    float smoothDist = 100000.0;

    // Search neighboring cells (3x3x3)
    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int z = -1; z <= 1; z++)
            {
                float3 offset = float3(x, y, z);
                float3 cell = baseCell + offset;

                // Random feature point
                float3 randomPoint = Hash33(cell);

                float3 featurePoint =
                    offset + 0.5 +
                    (randomPoint - 0.5) * Randomness;

                // Euclidean distance
                float3 diff = localPos - featurePoint;
                float dist = length(diff);

                // Standard F1 distance
                if (dist < minDist)
                {
                    minDist = dist;
                    closestColor = Hash33(cell + 17.31);
                }

                // Smooth F1 distance
                smoothDist = SmoothMin(
                    smoothDist,
                    dist,
                    Smoothness
                );
            }
        }
    }

    Distance = smoothDist;
    CellColor = closestColor;
}


// 3D Gradient Noise (Perlin-style)
float GradientNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);

    // Quintic interpolation
    float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float3 g000 = normalize(Hash33(i + float3(0, 0, 0)) * 2.0 - 1.0);
    float3 g100 = normalize(Hash33(i + float3(1, 0, 0)) * 2.0 - 1.0);
    float3 g010 = normalize(Hash33(i + float3(0, 1, 0)) * 2.0 - 1.0);
    float3 g110 = normalize(Hash33(i + float3(1, 1, 0)) * 2.0 - 1.0);

    float3 g001 = normalize(Hash33(i + float3(0, 0, 1)) * 2.0 - 1.0);
    float3 g101 = normalize(Hash33(i + float3(1, 0, 1)) * 2.0 - 1.0);
    float3 g011 = normalize(Hash33(i + float3(0, 1, 1)) * 2.0 - 1.0);
    float3 g111 = normalize(Hash33(i + float3(1, 1, 1)) * 2.0 - 1.0);

    float n000 = dot(g000, f - float3(0, 0, 0));
    float n100 = dot(g100, f - float3(1, 0, 0));
    float n010 = dot(g010, f - float3(0, 1, 0));
    float n110 = dot(g110, f - float3(1, 1, 0));

    float n001 = dot(g001, f - float3(0, 0, 1));
    float n101 = dot(g101, f - float3(1, 0, 1));
    float n011 = dot(g011, f - float3(0, 1, 1));
    float n111 = dot(g111, f - float3(1, 1, 1));

    float nx00 = lerp(n000, n100, u.x);
    float nx10 = lerp(n010, n110, u.x);
    float nx01 = lerp(n001, n101, u.x);
    float nx11 = lerp(n011, n111, u.x);

    float nxy0 = lerp(nx00, nx10, u.y);
    float nxy1 = lerp(nx01, nx11, u.y);

    float nxyz = lerp(nxy0, nxy1, u.z);

    return saturate(0.5 + nxyz * 1.5);
}

// Shader Graph Custom Function
void Noise3D_float(
    float3 Position,
    float Scale,
    out float Noise
)
{
    Noise = GradientNoise3D(Position * Scale);
}


float SoftLightChannel(float a, float b)
{
    if (b <= 0.5)
    {
        return a - (1.0 - 2.0 * b) * a * (1.0 - a);
    }
    else
    {
        float d;

        if (a <= 0.25)
        {
            d = ((16.0 * a - 12.0) * a + 4.0) * a;
        }
        else
        {
            d = sqrt(max(a, 0.0));
        }

        return a + (2.0 * b - 1.0) * (d - a);
    }
}

void SoftLight_float(
    float3 A,
    float3 B,
    out float3 Result
)
{
    Result.r = SoftLightChannel(A.r, B.r);
    Result.g = SoftLightChannel(A.g, B.g);
    Result.b = SoftLightChannel(A.b, B.b);
}


float OverlayChannel(float a, float b)
{
    if (a < 0.5)
        return 2.0 * a * b;

    return 1.0 - 2.0 * (1.0 - a) * (1.0 - b);
}

void Overlay_float(
    float3 A,
    float3 B,
    out float3 Result
)
{
    Result.r = OverlayChannel(A.r, B.r);
    Result.g = OverlayChannel(A.g, B.g);
    Result.b = OverlayChannel(A.b, B.b);
}


float ColorDodgeChannel(float base, float blend)
{
    return min(1.0, base / max(1.0 - blend, 0.0001));
}

void ColorDodge_float(
    float3 A,
    float B,
    out float3 Result
)
{
    Result.r = ColorDodgeChannel(A.r, B);
    Result.g = ColorDodgeChannel(A.g, B);
    Result.b = ColorDodgeChannel(A.b, B);
}

#endif
