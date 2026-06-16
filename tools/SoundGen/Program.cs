using System.Text;

// Synthesizes the bundled, license-free soundscapes as seamlessly-looping mono WAV files.
//   ocean : brown noise under a slow wave-swell envelope
//   rain  : softly low-passed white noise (steady hiss)

const int SampleRate = 44100;

string outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

GenerateOcean(Path.Combine(outDir, "ocean.wav"), seconds: 30.0);
GenerateRain(Path.Combine(outDir, "rain.wav"), seconds: 30.0);
Console.WriteLine($"Wrote ocean.wav and rain.wav to {Path.GetFullPath(outDir)}");

void GenerateOcean(string path, double seconds)
{
    int n = (int)(SampleRate * seconds);
    int fade = SampleRate / 4;
    var rng = new Random(20240617);

    var s = new double[n + fade];
    double last = 0, maxAbs = 1e-9;
    for (int i = 0; i < s.Length; i++)
    {
        double white = rng.NextDouble() * 2 - 1;
        last = (last + 0.02 * white) / 1.02;   // leaky integrator -> brown noise
        s[i] = last;
        maxAbs = Math.Max(maxAbs, Math.Abs(s[i]));
    }
    Scale(s, 0.9 / maxAbs);

    var outp = CrossfadeLoop(s, n, fade);

    // Slow swell; period divides the duration so the envelope also loops seamlessly.
    const double period = 6.0;
    for (int i = 0; i < n; i++)
    {
        double t = (double)i / SampleRate;
        double env = 0.55 + 0.45 * Math.Sin(2 * Math.PI * t / period - Math.PI / 2);
        outp[i] *= env;
    }

    WriteWav(path, outp);
}

void GenerateRain(string path, double seconds)
{
    int n = (int)(SampleRate * seconds);
    int fade = SampleRate / 4;
    var rng = new Random(99887766);

    var s = new double[n + fade];
    double lp = 0, maxAbs = 1e-9;
    for (int i = 0; i < s.Length; i++)
    {
        double white = rng.NextDouble() * 2 - 1;
        lp += 0.45 * (white - lp);              // one-pole low-pass -> soft hiss
        s[i] = lp;
        maxAbs = Math.Max(maxAbs, Math.Abs(s[i]));
    }
    Scale(s, 0.6 / maxAbs);

    WriteWav(path, CrossfadeLoop(s, n, fade));
}

static void Scale(double[] buffer, double factor)
{
    for (int i = 0; i < buffer.Length; i++) buffer[i] *= factor;
}

// Produces an n-sample loop whose seam is continuous: output[0] == src[n] (adjacent to
// src[n-1], the last output sample), with a short crossfade smoothing the splice.
static double[] CrossfadeLoop(double[] src, int n, int fade)
{
    var outp = new double[n];
    for (int i = 0; i < n; i++)
    {
        if (i < fade)
        {
            double a = (double)i / fade;
            outp[i] = src[i] * a + src[i + n] * (1 - a);
        }
        else
        {
            outp[i] = src[i];
        }
    }
    return outp;
}

void WriteWav(string path, double[] samples)
{
    using var fs = new FileStream(path, FileMode.Create);
    using var bw = new BinaryWriter(fs);

    int dataBytes = samples.Length * 2;
    bw.Write(Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + dataBytes);
    bw.Write(Encoding.ASCII.GetBytes("WAVE"));
    bw.Write(Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16);
    bw.Write((short)1);            // PCM
    bw.Write((short)1);            // mono
    bw.Write(SampleRate);
    bw.Write(SampleRate * 2);      // byte rate
    bw.Write((short)2);            // block align
    bw.Write((short)16);           // bits per sample
    bw.Write(Encoding.ASCII.GetBytes("data"));
    bw.Write(dataBytes);

    foreach (var x in samples)
        bw.Write((short)(Math.Clamp(x, -1.0, 1.0) * short.MaxValue));
}
