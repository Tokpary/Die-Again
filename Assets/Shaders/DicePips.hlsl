// Procedural ink dots for a standard six-sided die.
// FaceUV is read from UV1; FaceValue is read from UV2.x.
void DicePips_float(float2 FaceUV, float FaceValue, float Radius, float Softness, out float Mask)
{
    float value = floor(FaceValue + 0.5);

    float2 p1 = float2(0.5, 0.5);
    float2 p2a = float2(0.28, 0.72);
    float2 p2b = float2(0.72, 0.28);
    float2 p4a = float2(0.28, 0.28);
    float2 p4b = float2(0.72, 0.72);
    float2 p6a = float2(0.28, 0.5);
    float2 p6b = float2(0.72, 0.5);

    float r = max(Radius, 0.001);
    float s = max(Softness, 0.0005);

    float c1 = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p1));
    float c2a = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p2a));
    float c2b = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p2b));
    float c4a = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p4a));
    float c4b = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p4b));
    float c6a = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p6a));
    float c6b = 1.0 - smoothstep(r - s, r + s, distance(FaceUV, p6b));

    float d2 = saturate(c2a + c2b);
    float d3 = saturate(d2 + c1);
    float d4 = saturate(c4a + c4b + c2a + c2b);
    float d5 = saturate(d4 + c1);
    float d6 = saturate(d4 + c6a + c6b);

    Mask = 0.0;
    if (value == 1.0) Mask = c1;
    else if (value == 2.0) Mask = d2;
    else if (value == 3.0) Mask = d3;
    else if (value == 4.0) Mask = d4;
    else if (value == 5.0) Mask = d5;
    else if (value == 6.0) Mask = d6;
}
