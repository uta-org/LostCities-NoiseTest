using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private int m_x = -1;

    [SerializeField]
    private int m_z = -1;

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

    [HideInInspector]
    public float turbulence = 0f;

    public int perlinOctaves = 6;

    [HideInInspector] public double displacement = 4;

    [Range(0.01f, 8f)] public double frequency = 2;
    [Range(.05f, 4)] public double persistence = 1.0;
    [Range(.05f, 4)] public double lacunarity = 2.0;

    public int seed;

    public Color validColor = Color.red;
    public Color noValidColor = Color.blue;

    private float[] prefill;

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
                seed = Random.Range(0, 100);
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
                defPerlin.Seed = seed;
                defPerlin.OctaveCount = perlinOctaves;
                defPerlin.Frequency = frequency;
                defPerlin.Lacunarity = lacunarity;
                defPerlin.Persistence = persistence;
                moduleBase = defPerlin;

                break;
        }

        var sw = Stopwatch.StartNew();

        int w = chunks * 16;
        int h = chunks * 16;

        float xOffset = m_x == -1 ? offset : m_x;
        float zOffset = m_z == -1 ? offset : m_z;

        var noise = new Noise2D(w, h, moduleBase);
        /*
        noise.GeneratePlanar(
            xOffset + -1 * 1 / zoom,
            xOffset + xOffset + 1 * 1 / zoom,
            zOffset + -1 * 1 / zoom,
            zOffset + 1 * 1 / zoom);
            */

        noise.GeneratePlanar(
            offset + -1 * 1 / zoom,
            offset + offset + 1 * 1 / zoom,
            offset + -1 * 1 / zoom,
            offset + 1 * 1 / zoom);

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
            // prefill = Enumerable.Repeat(-1f, chunks * chunks).ToArray();
            prefill = new float[chunks * chunks];

            int l = cs.Length;
            for (int i = 0; i < l; i++)
            {
                // cs[i] = Color.red;

                int lx = i % w;
                int lh = i / w;

                int x = lx / 16;
                int z = lh / 16;

                int j = x * chunks + z;

                /*
                int fv = (int)prefill[j];
                float v = fv == -1 ? noise[x, z] : fv;
                if (fv == -1) prefill[j] = v;
                */

                float v = Mathf.Clamp01((float)moduleBase.GetValue(m_x + x - chunks / 2, 0, m_z + z - chunks / 2));
                // noise[x, z];
                // noise[m_x + x - chunks / 2, m_z + z - chunks / 2];

                //float v = i % 256 == 0 ? noise[x, z] : lv;

                prefill[j] = v;

                /*
                if (i == l / 2)
                {
                    cs[i] = Color.green;
                    continue;
                }
                */

                cs[i] = v >= tolerance ? validColor : noValidColor;
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

        // Debug.Log($"Min: {prefill.Min()}; Max: {prefill.Max()}");
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