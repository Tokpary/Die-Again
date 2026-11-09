// Maps centered face UVs to two digit cells in a 5x2 (0-9) atlas.
// The source PNG is arranged with 0-4 on the top row and 5-9 on the bottom.
void DiceNumberAtlasUV_float(
    float2 FaceUV,
    float FaceValue,
    out float2 TensAtlasUV,
    out float2 UnitsAtlasUV,
    out float TensMask,
    out float UnitsMask)
{
    float2 face = saturate((FaceUV - float2(0.05, 0.05)) / 0.90);
    float value = clamp(floor(FaceValue + 0.5), 0.0, 99.0);
    float hasTens = step(10.0, value);
    float tens = floor(value / 10.0);
    float units = value - tens * 10.0;

    // The source atlas cells are tall, so use narrower face regions to
    // preserve the digits' proportions when they are projected on the die.
    float2 singleMin = float2(0.375, 0.18);
    float2 singleMax = float2(0.625, 0.82);
    // Two-digit labels use smaller inset regions so they stay inside
    // the narrower pentagonal faces of dice such as the D20.
    float2 leftMin = float2(0.32, 0.25);
    float2 leftMax = float2(0.52, 0.75);
    float2 rightMin = float2(0.48, 0.25);
    float2 rightMax = float2(0.68, 0.75);

    float2 leftCoord = saturate((face - leftMin) / (leftMax - leftMin));
    float2 rightMinForValue = lerp(singleMin, rightMin, hasTens);
    float2 rightMaxForValue = lerp(singleMax, rightMax, hasTens);
    float2 rightCoord = saturate((face - rightMinForValue) / (rightMaxForValue - rightMinForValue));

    float inLeft = step(leftMin.x, face.x) * step(face.x, leftMax.x)
                 * step(leftMin.y, face.y) * step(face.y, leftMax.y);
    float inRight = step(rightMinForValue.x, face.x) * step(face.x, rightMaxForValue.x)
                  * step(rightMinForValue.y, face.y) * step(face.y, rightMaxForValue.y);

    float tensColumn = tens - floor(tens / 5.0) * 5.0;
    float tensRow = floor(tens / 5.0);
    float unitsColumn = units - floor(units / 5.0) * 5.0;
    float unitsRow = floor(units / 5.0);

    TensAtlasUV = float2((tensColumn + leftCoord.x) / 5.0,
                         tensRow < 0.5 ? 0.5 + leftCoord.y * 0.5 : leftCoord.y * 0.5);
    UnitsAtlasUV = float2((unitsColumn + rightCoord.x) / 5.0,
                          unitsRow < 0.5 ? 0.5 + rightCoord.y * 0.5 : rightCoord.y * 0.5);

    TensMask = hasTens * inLeft;
    UnitsMask = inRight;
}
