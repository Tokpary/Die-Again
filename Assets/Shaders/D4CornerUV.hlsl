void D4CornerUV_float(
    float2 FaceUV,
    float Corner,
    out float2 CornerFaceUV)
{
    float2 center;
    float angle;

    if (Corner < 0.5)
    {
        center = float2(0.50, 0.76); // esquina superior
        angle = 0.0;
    }
    else if (Corner < 1.5)
    {
        center = float2(0.28, 0.38); // esquina inferior izquierda
        angle = -2.0943951; // -120 grados: la parte superior mira a la esquina
    }
    else
    {
        center = float2(0.72, 0.38); // esquina inferior derecha
        angle = 2.0943951; // +120 grados: la parte superior mira a la esquina
    }

    // Gira las coordenadas para que la parte superior del número apunte
    // hacia su vértice y luego acerca el número a esa esquina.
    float sine = sin(angle);
    float cosine = cos(angle);
    float2 offset = FaceUV - center;
    float2 rotated = float2(
        cosine * offset.x - sine * offset.y,
        sine * offset.x + cosine * offset.y);

    CornerFaceUV = rotated * 1.5 + float2(0.5, 0.5);
}
