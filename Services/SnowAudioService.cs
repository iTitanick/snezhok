using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SnowDesktop.Services;

/// <summary>
/// Real sampled WAV audio playback via low-latency Windows waveOut PCM voices.
/// Plays embedded mono 44.1 kHz 16-bit WAV clips for train whistle, continuous looped
/// locomotive movement, and snowball shatter impacts.
/// </summary>
public sealed class SnowAudioService : IDisposable
{
    private const int SampleRate = 44100;
    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint WhdrBeginLoop = 0x00000004;
    private const uint WhdrEndLoop = 0x00000008;

    private readonly short[] _whistleSamples;
    private readonly short[] _trainLoopSamples;
    private readonly short[] _shatterSamples;

    private readonly VoiceSlot _whistleVoice;
    private readonly VoiceSlot _trainLoopVoice;
    private readonly VoiceSlot[] _shatterVoices;
    private readonly object _sync = new();

    private int _shatterVoiceCursor;
    private bool _trainLoopPlaying;
    private float _trainLoopGain;
    private bool _disposed;

    public SnowAudioService()
    {
        _whistleSamples = LoadEmbeddedWavMono("Assets/Sounds/whistle.wav");
        _trainLoopSamples = LoadEmbeddedWavMono("Assets/Sounds/train_loop.wav");
        _shatterSamples = LoadEmbeddedWavMono("Assets/Sounds/snow_hit.wav");

        _whistleVoice = new VoiceSlot(_whistleSamples.Length);
        _trainLoopVoice = new VoiceSlot(_trainLoopSamples.Length);
        _shatterVoices = new[]
        {
            new VoiceSlot(_shatterSamples.Length),
            new VoiceSlot(_shatterSamples.Length),
            new VoiceSlot(_shatterSamples.Length),
            new VoiceSlot(_shatterSamples.Length)
        };
    }

    public void PlayTrainWhistle(float volume)
    {
        if (_disposed || volume <= 0.01f || _whistleSamples.Length == 0)
            return;

        lock (_sync)
        {
            _whistleVoice.Play(_whistleSamples, Math.Clamp(volume, 0f, 1f), loop: false);
        }
    }

    public void SetTrainMovement(bool active, float volume)
    {
        if (_disposed)
            return;

        float clampedVol = Math.Clamp(volume, 0f, 1f);
        bool shouldPlay = active && clampedVol > 0.01f && _trainLoopSamples.Length > 0;

        lock (_sync)
        {
            if (!shouldPlay)
            {
                if (_trainLoopPlaying)
                {
                    _trainLoopVoice.Stop();
                    _trainLoopPlaying = false;
                    _trainLoopGain = 0f;
                }
                return;
            }

            if (!_trainLoopPlaying || MathF.Abs(clampedVol - _trainLoopGain) >= 0.12f)
            {
                _trainLoopVoice.Play(_trainLoopSamples, clampedVol, loop: true);
                _trainLoopPlaying = true;
                _trainLoopGain = clampedVol;
            }
        }
    }

    public void PlaySnowballShatter(float volume)
    {
        if (_disposed || volume <= 0.01f || _shatterSamples.Length == 0)
            return;

        lock (_sync)
        {
            int idx = _shatterVoiceCursor++;
            VoiceSlot voice = _shatterVoices[idx % _shatterVoices.Length];
            // Subtle per-shot gain variance so clustered impacts stay natural without clipping
            float subtleFactor = 1f - 0.06f * (idx % 3);
            voice.Play(_shatterSamples, Math.Clamp(volume * subtleFactor, 0f, 1f), loop: false);
        }
    }

    public void StopAll()
    {
        if (_disposed)
            return;

        lock (_sync)
        {
            _whistleVoice.Stop();
            _trainLoopVoice.Stop();
            _trainLoopPlaying = false;
            _trainLoopGain = 0f;
            for (int i = 0; i < _shatterVoices.Length; i++)
            {
                _shatterVoices[i].Stop();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        lock (_sync)
        {
            _whistleVoice.Dispose();
            _trainLoopVoice.Dispose();
            for (int i = 0; i < _shatterVoices.Length; i++)
            {
                _shatterVoices[i].Dispose();
            }
        }
    }

    private static short[] LoadEmbeddedWavMono(string relativePackPath)
    {
        try
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            var uri = new Uri($"pack://application:,,,/{relativePackPath}", UriKind.Absolute);
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo?.Stream is null)
                return Array.Empty<short>();

            using Stream stream = streamInfo.Stream;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] bytes = ms.ToArray();
            if (bytes.Length < 44 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE")
                return Array.Empty<short>();

            int pos = 12;
            short channels = 1;
            short bitsPerSample = 16;
            int dataOffset = -1;
            int dataSize = 0;

            while (pos + 8 <= bytes.Length)
            {
                string chunkId = Encoding.ASCII.GetString(bytes, pos, 4);
                int chunkSize = BitConverter.ToInt32(bytes, pos + 4);
                int payloadOffset = pos + 8;
                if (chunkSize < 0 || payloadOffset + chunkSize > bytes.Length)
                    break;

                if (chunkId == "fmt " && chunkSize >= 16)
                {
                    channels = BitConverter.ToInt16(bytes, payloadOffset + 2);
                    bitsPerSample = BitConverter.ToInt16(bytes, payloadOffset + 14);
                }
                else if (chunkId == "data")
                {
                    dataOffset = payloadOffset;
                    dataSize = chunkSize;
                    break;
                }

                pos = payloadOffset + chunkSize + (chunkSize & 1);
            }

            if (dataOffset < 0 || dataSize <= 0 || bitsPerSample != 16 || channels < 1)
                return Array.Empty<short>();

            int totalPcmShorts = dataSize / 2;
            if (channels == 1)
            {
                var mono = new short[totalPcmShorts];
                Buffer.BlockCopy(bytes, dataOffset, mono, 0, mono.Length * 2);
                return mono;
            }

            int frameCount = totalPcmShorts / channels;
            var downmixed = new short[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                int sum = 0;
                int baseByte = dataOffset + f * channels * 2;
                for (int c = 0; c < channels; c++)
                {
                    sum += BitConverter.ToInt16(bytes, baseByte + c * 2);
                }
                downmixed[f] = (short)(sum / channels);
            }
            return downmixed;
        }
        catch
        {
            return Array.Empty<short>();
        }
    }

    private sealed class VoiceSlot : IDisposable
    {
        private readonly byte[] _byteBuffer;
        private readonly GCHandle _pinnedBuffer;
        private readonly IntPtr _headerPtr;
        private IntPtr _handle;
        private bool _prepared;
        private bool _openFailed;
        private bool _disposed;

        public VoiceSlot(int maxSamples)
        {
            _byteBuffer = new byte[Math.Max(2, maxSamples * 2)];
            _pinnedBuffer = GCHandle.Alloc(_byteBuffer, GCHandleType.Pinned);
            _headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHdr>());
        }

        public void Play(short[] samples, float gain, bool loop)
        {
            if (_disposed || samples.Length == 0 || !EnsureOpen())
                return;

            Stop();

            int count = Math.Min(samples.Length, _byteBuffer.Length / 2);
            int fixedGain = Math.Clamp((int)(gain * 256f), 0, 256);

            for (int i = 0; i < count; i++)
            {
                int scaled = (samples[i] * fixedGain) >> 8;
                short s = (short)Math.Clamp(scaled, -32767, 32767);
                int idx = i * 2;
                _byteBuffer[idx] = (byte)(s & 0xFF);
                _byteBuffer[idx + 1] = (byte)((s >> 8) & 0xFF);
            }

            var hdr = new WaveHdr
            {
                lpData = _pinnedBuffer.AddrOfPinnedObject(),
                dwBufferLength = (uint)(count * 2),
                dwBytesRecorded = 0,
                dwUser = IntPtr.Zero,
                dwFlags = loop ? (WhdrBeginLoop | WhdrEndLoop) : 0u,
                dwLoops = loop ? 0xFFFFFFFFu : 0u,
                lpNext = IntPtr.Zero,
                reserved = IntPtr.Zero
            };

            Marshal.StructureToPtr(hdr, _headerPtr, fDeleteOld: false);
            int hdrSize = Marshal.SizeOf<WaveHdr>();
            if (waveOutPrepareHeader(_handle, _headerPtr, hdrSize) == 0)
            {
                _prepared = true;
                waveOutWrite(_handle, _headerPtr, hdrSize);
            }
        }

        public void Stop()
        {
            if (_handle == IntPtr.Zero)
                return;

            waveOutReset(_handle);
            if (_prepared)
            {
                waveOutUnprepareHeader(_handle, _headerPtr, Marshal.SizeOf<WaveHdr>());
                _prepared = false;
            }
        }

        private bool EnsureOpen()
        {
            if (_handle != IntPtr.Zero)
                return true;
            if (_openFailed)
                return false;

            if (waveOutGetNumDevs() == 0)
            {
                _openFailed = true;
                return false;
            }

            var fmt = new WaveFormatEx
            {
                wFormatTag = 1, // PCM
                nChannels = 1,
                nSamplesPerSec = SampleRate,
                nAvgBytesPerSec = SampleRate * 2,
                nBlockAlign = 2,
                wBitsPerSample = 16,
                cbSize = 0
            };

            int mmr = waveOutOpen(out _handle, WaveMapper, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
            if (mmr != 0 || _handle == IntPtr.Zero)
            {
                _openFailed = true;
                _handle = IntPtr.Zero;
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Stop();
            if (_handle != IntPtr.Zero)
            {
                waveOutClose(_handle);
                _handle = IntPtr.Zero;
            }

            Marshal.FreeHGlobal(_headerPtr);
            if (_pinnedBuffer.IsAllocated)
            {
                _pinnedBuffer.Free();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct WaveFormatEx
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    [DllImport("winmm.dll")]
    private static extern uint waveOutGetNumDevs();

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(
        out IntPtr hWaveOut,
        uint uDeviceID,
        ref WaveFormatEx lpFormat,
        IntPtr dwCallback,
        IntPtr dwInstance,
        uint dwFlags);

    [DllImport("winmm.dll")]
    private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr hWaveOut);
}
