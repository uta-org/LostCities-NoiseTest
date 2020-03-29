using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum NoiseType
{
    Perlin,
    Billow,
    RidgedMultifractal,
    Voronoi,
    Mix,
    Practice
}

public class Generate : MonoBehaviour
{
    [SerializeField]
    private int m_x;

    [SerializeField]
    private int m_z;

    private int chunkX => m_x / 16;
    private int chunkZ => m_z / 16;

    [SerializeField]
    private int chunks = 64;

    // private float[] values;

    private Texture2D texture;

    public bool fill;

    public NoiseType noiseType;

    [Range(.02f, 4)]
    public float zoom = 1f;

    [Range(0, 1)]
    public float tolerance = .5f;

    public float offset = 0f;
    public float turbulence = 0f;
    public int perlinOctaves = 6;
    public double displacement = 4;
    public double frequency = 2;
    public int seed = 0;

    public Color validColor = Color.red;
    public Color noValidColor = Color.blue;

    // Start is called before the first frame update
    private void Start()
    {
        // values = new float[chunks * chunks];

        // Create the module network
        ModuleBase moduleBase;
        switch (noiseType)
        {
            case NoiseType.Billow:
                moduleBase = new Billow();
                break;

            case NoiseType.RidgedMultifractal:
                moduleBase = new RidgedMultifractal();
                break;

            case NoiseType.Voronoi:
                // moduleBase = new Voronoi();
                seed = UnityEngine.Random.Range(0, 100);
                moduleBase = new Voronoi(frequency, displacement, seed, false);

                break;

            case NoiseType.Mix:
                Perlin perlin = new Perlin();
                var rigged = new RidgedMultifractal();
                moduleBase = new Add(perlin, rigged);
                break;

            case NoiseType.Practice:
                var bill = new Billow();
                bill.Frequency = frequency;
                moduleBase = new Turbulence(turbulence / 10, bill);

                break;

            default:
                var defPerlin = new Perlin();
                defPerlin.OctaveCount = perlinOctaves;
                moduleBase = defPerlin;

                break;
        }

        var sw = Stopwatch.StartNew();

        int w = chunks * 16;
        int h = chunks * 16;

        var noise = new Noise2D(w, h, moduleBase);
        noise.GeneratePlanar(
            offset + -1 * 1 / zoom,
            offset + offset + 1 * 1 / zoom,
            offset + -1 * 1 / zoom,
            offset + 1 * 1 / zoom, true);

        texture = new Texture2D(w, h);

        int length = chunks * chunks;
        int total = length * 256;
        int pixels = 0;
        Color[] cs = new Color[texture.width * texture.height];

        if (!fill)
        {
            for (int i = 0; i < length; i++)
            {
                int x = i % chunks;
                int z = i / chunks;

                float v = perlin(chunkX + x - chunks / 2, chunkZ + z - chunks / 2, w, h);

                //if (v < .9f)
                //    continue;

                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        int _x = x * 16 + j;
                        int _z = z * 16 + k;

                        int l = _x * h + _z;

                        // if (i == 10000) Debug.Log($"({_x}, {_z})");

                        // texture.SetPixel(_x, _z, Color.black);
                        // texture.SetPixel(_x, h - _z - 1, Color.Lerp(Color.white, Color.black, v));

                        cs[l] = Color.Lerp(Color.white, Color.black, v);
                        ++pixels;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < cs.Length; i++)
            {
                // cs[i] = Color.red;

                int lx = i % w;
                int lh = i / w;

                int x = lx / 16;
                int z = lh / 16;

                float v = noise[x, z];
                // perlin(chunkX + x - chunks / 2, chunkZ + z - chunks / 2, w, h);

                cs[i] = v >= tolerance ? validColor : noValidColor;
                // cs[i] = Color.Lerp(Color.white, Color.black, v);
            }
        }

        texture.SetPixels(cs);
        texture.Apply();

        Debug.Log($"Applied texture on {sw.ElapsedMilliseconds} ms!");

        if (!Directory.Exists(Application.streamingAssetsPath))
            Directory.CreateDirectory(Application.streamingAssetsPath);

        string path = Path.Combine(Application.streamingAssetsPath, "test.png");
        File.WriteAllBytes(path, texture.EncodeToPNG());

        sw.Stop();

        Debug.Log($"[{pixels} / {total}] Elapsed {sw.ElapsedMilliseconds / 1000f:F4} seconds!");
    }

    private static float perlin(float x, float z, int w, int h, float scale = 1.0f)
    {
        float xCoord = x / w * scale;
        float zCoord = z / h * scale;
        return Mathf.PerlinNoise(xCoord, zCoord);
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void OnGUI()
    {
        if (chunks > 64) return;

        if (texture != null)
            GUI.DrawTexture(new Rect(0, 0, chunks * 16, chunks * 16), texture);
    }
}