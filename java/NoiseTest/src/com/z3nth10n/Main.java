package com.z3nth10n;

// import com.flowpowered.noise.module.source.Perlin;

import org.spongepowered.noise.module.source.Perlin;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.DoubleStream;

public class Main {

    // private static byte[] values;

    private static final int m_x = 3739019,
                             m_z = -214261;

    private static int cx = m_x / 16, cz = m_z / 16;

    private static final int chunks = 64;

    private static List<Double> values = new ArrayList<>();

    public static void main(String[] args) {
	// write your code here

        // values = new byte[chunks * chunks];

        StringBuilder b = new StringBuilder();

        Perlin perlin = new Perlin();
        perlin.setSeed(1337);
        perlin.setOctaveCount(5);
        perlin.setFrequency(0.1);
        perlin.setPersistence(0.8);
        perlin.setLacunarity(1.25);

        for (int x = 0; x < chunks; ++x) {
            for(int z = 0; z < chunks; ++z) {
                //int i = x * chunks + z;
                // values[i] = perlin.

                double d = interpolate(perlin, perlin.getValue(cx + x - chunks / 2, 0, cz + z - chunks / 2));
                values.add(d);

                int v = d >= 0.5 ? 1 : 0;
                b.append(v);
            }
            b.append("\n");
        }

        String cwd = new File("").getAbsolutePath();
        String path = cwd+"/file.txt";
        try {
            Files.write( Paths.get(path), b.toString().getBytes());
        } catch (IOException e) {
            e.printStackTrace();
        }

        double max = getStream(values).max().getAsDouble();
        double min = getStream(values).min().getAsDouble();

        System.out.println(max);
        System.out.println(min);

        System.out.println();
        System.out.println(perlin.getMaxValue());
    }

    public static double interpolate(Perlin perlin, double d) {
        // (map_arr - map_arr.min()) / (map_arr.max() - map_arr.min())
        double max = perlin.getMaxValue();
        double min = 0;
                // -max;

        return (d - min) / (max - min);
    }

    public static double clamp01(double val) {
        return clamp(val, 0, 1);
    }

    public static double clamp(double val, double min, double max) {
        if (val - min < 0) return min;
        else if (val + max> 0) return max;
        else return val;
    }

    public static DoubleStream getStream(List<Double> list) {
        return list.stream().mapToDouble(d -> d);
    }

    /*
    public static <T extends Comparable<T>> T clamp(T val, T min, T max) {
        if (val.compareTo(min) < 0) return min;
        else if (val.compareTo(max) > 0) return max;
        else return val;
    }
    */
}