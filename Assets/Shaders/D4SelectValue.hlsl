void D4SelectValue_float(
    float EncodedVertexID,
    float4 D4Values,
    out float FaceValue)
{
    float id = clamp(floor(EncodedVertexID * 3.0 + 0.5), 0.0, 3.0);

    if (id < 0.5)
        FaceValue = D4Values.x;
    else if (id < 1.5)
        FaceValue = D4Values.y;
    else if (id < 2.5)
        FaceValue = D4Values.z;
    else
        FaceValue = D4Values.w;
}