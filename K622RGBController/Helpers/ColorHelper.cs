namespace K622RGBController.Helpers;

/// <summary>
/// Color math utilities: gradient LUT, lerp, HSL conversion.
/// </summary>
public static class ColorHelper
{
    public static (byte R, byte G, byte B) LerpColor((byte R, byte G, byte B) c1, (byte R, byte G, byte B) c2, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return (
            (byte)Math.Clamp((int)(c1.R + (c2.R - c1.R) * t), 0, 255),
            (byte)Math.Clamp((int)(c1.G + (c2.G - c1.G) * t), 0, 255),
            (byte)Math.Clamp((int)(c1.B + (c2.B - c1.B) * t), 0, 255)
        );
    }

    /// <summary>
    /// Build a 256-step gradient LUT from 4 anchor colors.
    /// </summary>
    public static (byte R, byte G, byte B)[] BuildGradientLut(int[][] colors4, int steps = 256)
    {
        while (colors4.Length < 4)
        {
            var extended = new int[colors4.Length + 1][];
            Array.Copy(colors4, extended, colors4.Length);
            extended[colors4.Length] = new[] { 255, 255, 255 };
            colors4 = extended;
        }

        var anchors = new (double Pos, (byte R, byte G, byte B) Color)[]
        {
            (0.0,       ((byte)colors4[0][0], (byte)colors4[0][1], (byte)colors4[0][2])),
            (1.0 / 3.0, ((byte)colors4[1][0], (byte)colors4[1][1], (byte)colors4[1][2])),
            (2.0 / 3.0, ((byte)colors4[2][0], (byte)colors4[2][1], (byte)colors4[2][2])),
            (1.0,       ((byte)colors4[3][0], (byte)colors4[3][1], (byte)colors4[3][2])),
        };

        var lut = new (byte R, byte G, byte B)[steps];
        for (int i = 0; i < steps; i++)
        {
            double t = (double)i / Math.Max(1, steps - 1);
            int lower = 0;
            for (int j = 0; j < anchors.Length - 1; j++)
            {
                if (t >= anchors[j].Pos && t <= anchors[j + 1].Pos)
                {
                    lower = j;
                    break;
                }
            }

            double aPos = anchors[lower].Pos;
            double bPos = anchors[lower + 1].Pos;
            double seg = bPos - aPos;
            double lt = seg > 0 ? (t - aPos) / seg : 0.0;
            lut[i] = LerpColor(anchors[lower].Color, anchors[lower + 1].Color, lt);
        }

        return lut;
    }
}
