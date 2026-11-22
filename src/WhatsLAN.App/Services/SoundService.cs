using System.IO;
using System.Media;

namespace WhatsLAN.App.Services;

public class SoundService
{
    private static SoundService? _instance;
    public static SoundService Instance => _instance ??= new SoundService();

    public bool IsEnabled { get; set; } = true;

    private readonly byte[] _messageReceivedWav;
    private readonly byte[] _messageSentWav;

    public SoundService()
    {
        _messageReceivedWav = GenerateToneWav(frequency: 880, durationMs: 45, attackMs: 5, decayMs: 35);
        _messageSentWav = GenerateChirpWav(startFreq: 520, endFreq: 780, durationMs: 35);
    }

    public void PlayMessageReceived()
    {
        if (!IsEnabled) return;
        PlayWavAsync(_messageReceivedWav);
    }

    public void PlayMessageSent()
    {
        if (!IsEnabled) return;
        PlayWavAsync(_messageSentWav);
    }

    private static void PlayWavAsync(byte[] wavBytes)
    {
        Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream(wavBytes);
                using var player = new SoundPlayer(ms);
                player.Play();
            }
            catch
            {
                // Ignorer silencieusement si la carte son est occupée
            }
        });
    }

    private static byte[] GenerateToneWav(double frequency, int durationMs, int attackMs, int decayMs)
    {
        int sampleRate = 44100;
        int totalSamples = (sampleRate * durationMs) / 1000;
        int attackSamples = (sampleRate * attackMs) / 1000;
        int decaySamples = (sampleRate * decayMs) / 1000;

        short[] samples = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = 1.0;
            if (i < attackSamples && attackSamples > 0)
            {
                envelope = (double)i / attackSamples;
            }
            else if (i >= totalSamples - decaySamples && decaySamples > 0)
            {
                envelope = (double)(totalSamples - i) / decaySamples;
            }

            double sample = Math.Sin(2.0 * Math.PI * frequency * t) * envelope;
            samples[i] = (short)(sample * 16000); // Volume modéré
        }

        return CreateWav(samples, sampleRate);
    }

    private static byte[] GenerateChirpWav(double startFreq, double endFreq, int durationMs)
    {
        int sampleRate = 44100;
        int totalSamples = (sampleRate * durationMs) / 1000;
        short[] samples = new short[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            double progress = (double)i / totalSamples;
            double currentFreq = startFreq + (endFreq - startFreq) * progress;
            double t = (double)i / sampleRate;
            double envelope = Math.Sin(Math.PI * progress); // Envelope en cloche

            double sample = Math.Sin(2.0 * Math.PI * currentFreq * t) * envelope;
            samples[i] = (short)(sample * 14000);
        }

        return CreateWav(samples, sampleRate);
    }

    private static byte[] CreateWav(short[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int subChunk2Size = samples.Length * 2;
        int chunkSize = 36 + subChunk2Size;

        // RIFF header
        bw.Write("RIFF"u8.ToArray());
        bw.Write(chunkSize);
        bw.Write("WAVE"u8.ToArray());

        // fmt subchunk
        bw.Write("fmt "u8.ToArray());
        bw.Write(16); // SubChunk1Size (PCM = 16)
        bw.Write((short)1); // AudioFormat (1 = PCM)
        bw.Write((short)1); // NumChannels (1 = Mono)
        bw.Write(sampleRate); // SampleRate
        bw.Write(sampleRate * 2); // ByteRate
        bw.Write((short)2); // BlockAlign
        bw.Write((short)16); // BitsPerSample

        // data subchunk
        bw.Write("data"u8.ToArray());
        bw.Write(subChunk2Size);
        foreach (var sample in samples)
        {
            bw.Write(sample);
        }

        return ms.ToArray();
    }
}
