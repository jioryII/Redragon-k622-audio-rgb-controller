using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;

namespace K622RGBController.Services;

/// <summary>
/// Captures system audio via WASAPI Loopback and analyzes frequency bands.
/// Uses NAudio WasapiLoopbackCapture — captures ALL system audio output
/// regardless of the playback device (local speakers, Bluetooth, etc.).
/// </summary>
public class AudioAnalyzer : IDisposable
{
    private const int NumBands = 16;
    private const int FftSize = 2048;
    private const double NoiseGate = 0.003;

    // 3-band ranges (Hz)
    private const double BassLow = 20, BassHigh = 250;
    private const double MidLow = 250, MidHigh = 2000;
    private const double TrebleLow = 2000, TrebleHigh = 16000;

    private WasapiLoopbackCapture? _capture;
    private MMDeviceEnumerator? _enumerator;
    private DeviceNotificationClient? _notifClient;
    private readonly object _lock = new();
    private volatile bool _running;
    
    public event Action<string>? DeviceChanged;

    // 3-band levels (legacy, for meters)
    private double _bass, _mids, _treble, _overall;

    // 16-band spectrum
    private readonly double[] _spectrum = new double[NumBands];
    private readonly double[] _spectrumPeaks = new double[NumBands];
    private readonly double[] _bandPeaksNorm;
    private const double BandPeakDecay = 0.9997;

    // Logarithmic band edges
    private readonly double[] _bandEdges;

    // 3-band peak tracking
    private double _peakBass = 0.1, _peakMids = 0.1, _peakTreble = 0.1;
    private const double PeakDecay = 0.9995;

    // Audio buffer for accumulating samples
    private readonly List<float> _sampleBuffer = new(FftSize * 2);
    private int _sampleRate = 48000;

    // Public settings
    public double Sensitivity { get; set; } = 1.0;
    public double Smoothing { get; set; } = 0.3;
    public double BarDecay { get; set; } = 0.75;
    public bool IsRunning => _running;
    public string DeviceName { get; private set; } = "Ninguno";

    public AudioAnalyzer()
    {
        // Build logarithmic band edges: 20Hz to 16000Hz
        _bandEdges = new double[NumBands + 1];
        double logMin = Math.Log10(20);
        double logMax = Math.Log10(16000);
        for (int i = 0; i <= NumBands; i++)
        {
            _bandEdges[i] = Math.Pow(10, logMin + (logMax - logMin) * i / NumBands);
        }

        _bandPeaksNorm = new double[NumBands];
        Array.Fill(_bandPeaksNorm, 0.1);
    }

    public (double Bass, double Mids, double Treble, double Overall) GetLevels()
    {
        return (_bass, _mids, _treble, _overall);
    }

    public (double[] Spectrum, double[] Peaks) GetSpectrum()
    {
        lock (_lock)
        {
            return ((double[])_spectrum.Clone(), (double[])_spectrumPeaks.Clone());
        }
    }

    public void Start()
    {
        if (_running) return;

        try
        {
            _enumerator = new MMDeviceEnumerator();
            _notifClient = new DeviceNotificationClient(this);
            _enumerator.RegisterEndpointNotificationCallback(_notifClient);

            StartCapture();
            _running = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Audio Start failed: {ex.Message}");
            _running = false;
        }
    }

    private void StartCapture()
    {
        lock (_lock)
        {
            try
            {
                _capture?.StopRecording();
                _capture?.Dispose();

                _capture = new WasapiLoopbackCapture();
                _sampleRate = _capture.WaveFormat.SampleRate;

                try
                {
                    using var defaultDevice = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    DeviceName = defaultDevice.FriendlyName;
                }
                catch
                {
                    DeviceName = "Audio del sistema";
                }

                DeviceChanged?.Invoke(DeviceName);

                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();

                Debug.WriteLine($"Audio analyzer capturing: {DeviceName} ({_sampleRate}Hz)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio capture failed: {ex.Message}");
            }
        }
    }

    public void HandleDeviceChange()
    {
        if (!_running) return;
        Debug.WriteLine("Default audio device changed. Restarting capture...");
        // Re-start on background thread to avoid freezing NAudio message pump
        Task.Run(() => StartCapture());
    }

    public void Stop()
    {
        _running = false;

        if (_enumerator != null && _notifClient != null)
        {
            try { _enumerator.UnregisterEndpointNotificationCallback(_notifClient); } catch { }
        }

        lock (_lock)
        {
            try { _capture?.StopRecording(); } catch { }
            try { _capture?.Dispose(); } catch { }
            _capture = null;
        }

        try { _enumerator?.Dispose(); } catch { }
        _enumerator = null;
        _notifClient = null;

        _bass = _mids = _treble = _overall = 0;
        Array.Clear(_spectrum);
        Array.Clear(_spectrumPeaks);
        DeviceName = "Ninguno";
        DeviceChanged?.Invoke(DeviceName);

        Debug.WriteLine("Audio analyzer stopped");
    }

    private class DeviceNotificationClient : IMMNotificationClient
    {
        private readonly AudioAnalyzer _parent;

        public DeviceNotificationClient(AudioAnalyzer parent)
        {
            _parent = parent;
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                _parent.HandleDeviceChange();
            }
        }

        public void OnDeviceAdded(string pwstrDeviceId) { }
        public void OnDeviceRemoved(string deviceId) { }
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_running || e.BytesRecorded == 0) return;

        try
        {
            var waveFormat = _capture!.WaveFormat;
            int channels = waveFormat.Channels;
            int bytesPerSample = waveFormat.BitsPerSample / 8;
            int sampleCount = e.BytesRecorded / (bytesPerSample * channels);

            // Convert to mono float samples
            var monoSamples = new float[sampleCount];

            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat && bytesPerSample == 4)
            {
                // 32-bit float (most common with WASAPI)
                for (int i = 0; i < sampleCount; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (i * channels + ch) * 4;
                        if (offset + 3 < e.BytesRecorded)
                        {
                            sum += BitConverter.ToSingle(e.Buffer, offset);
                        }
                    }
                    monoSamples[i] = sum / channels;
                }
            }
            else
            {
                // 16-bit PCM fallback
                for (int i = 0; i < sampleCount; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int offset = (i * channels + ch) * 2;
                        if (offset + 1 < e.BytesRecorded)
                        {
                            sum += BitConverter.ToInt16(e.Buffer, offset) / 32768f;
                        }
                    }
                    monoSamples[i] = sum / channels;
                }
            }

            // Accumulate in buffer
            lock (_lock)
            {
                _sampleBuffer.AddRange(monoSamples);

                // Process when we have enough samples
                while (_sampleBuffer.Count >= FftSize)
                {
                    var block = _sampleBuffer.GetRange(0, FftSize).ToArray();
                    _sampleBuffer.RemoveRange(0, FftSize);
                    ProcessFft(block);
                }

                // Prevent buffer from growing too large
                if (_sampleBuffer.Count > FftSize * 4)
                {
                    _sampleBuffer.RemoveRange(0, _sampleBuffer.Count - FftSize);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Audio processing error: {ex.Message}");
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            Debug.WriteLine($"Recording stopped with error: {e.Exception.Message}");
        }
    }

    private void ProcessFft(float[] samples)
    {
        int n = samples.Length;
        if (n < 64) return;

        // Apply Hann window
        var windowed = new double[n];
        for (int i = 0; i < n; i++)
        {
            double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (n - 1)));
            windowed[i] = samples[i] * window;
        }

        // FFT
        double[] fftMagnitude;
        try
        {
            var fftComplex = FftSharp.FFT.Forward(windowed);
            fftMagnitude = FftSharp.FFT.Magnitude(fftComplex);
        }
        catch
        {
            return;
        }

        double freqResolution = (double)_sampleRate / n;

        // ── Legacy 3-band (for level meters) ──
        double rawBass = GetBandEnergy(fftMagnitude, freqResolution, BassLow, BassHigh);
        double rawMids = GetBandEnergy(fftMagnitude, freqResolution, MidLow, MidHigh);
        double rawTreble = GetBandEnergy(fftMagnitude, freqResolution, TrebleLow, TrebleHigh);

        rawBass *= Sensitivity;
        rawMids *= Sensitivity;
        rawTreble *= Sensitivity;

        _peakBass = Math.Max(rawBass, _peakBass * PeakDecay);
        _peakMids = Math.Max(rawMids, _peakMids * PeakDecay);
        _peakTreble = Math.Max(rawTreble, _peakTreble * PeakDecay);

        double normBass = _peakBass > 0.01 ? Math.Min(1.0, rawBass / _peakBass) : 0.0;
        double normMids = _peakMids > 0.01 ? Math.Min(1.0, rawMids / _peakMids) : 0.0;
        double normTreble = _peakTreble > 0.01 ? Math.Min(1.0, rawTreble / _peakTreble) : 0.0;

        double s = Smoothing;
        _bass = _bass * s + normBass * (1.0 - s);
        _mids = _mids * s + normMids * (1.0 - s);
        _treble = _treble * s + normTreble * (1.0 - s);
        _overall = (_bass + _mids + _treble) / 3.0;

        // ── 16-band spectrum ──
        for (int band = 0; band < NumBands; band++)
        {
            double lo = _bandEdges[band];
            double hi = _bandEdges[band + 1];
            double rawEnergy = GetBandRmsEnergy(fftMagnitude, freqResolution, lo, hi) * Sensitivity;

            if (rawEnergy < NoiseGate) rawEnergy = 0.0;

            // Auto-normalize
            _bandPeaksNorm[band] = Math.Max(rawEnergy, _bandPeaksNorm[band] * BandPeakDecay);
            double peak = Math.Max(0.1, _bandPeaksNorm[band]);
            double normalized = rawEnergy > 0 ? Math.Min(1.0, rawEnergy / peak) : 0.0;

            // Smooth: fast rise, gentle fall
            double current = _spectrum[band];
            if (normalized > current)
                _spectrum[band] = current * 0.2 + normalized * 0.8;
            else
                _spectrum[band] = current * BarDecay + normalized * (1.0 - BarDecay);

            if (_spectrum[band] < 0.008) _spectrum[band] = 0.0;

            // Peak hold
            if (normalized > _spectrumPeaks[band])
                _spectrumPeaks[band] = normalized;
            else
            {
                _spectrumPeaks[band] *= 0.93;
                if (_spectrumPeaks[band] < 0.008) _spectrumPeaks[band] = 0.0;
            }
        }
    }

    private static double GetBandEnergy(double[] magnitude, double freqRes, double lo, double hi)
    {
        int loIdx = Math.Max(0, (int)(lo / freqRes));
        int hiIdx = Math.Min(magnitude.Length - 1, (int)(hi / freqRes));
        if (loIdx >= hiIdx) return 0;

        double sum = 0;
        int count = 0;
        for (int i = loIdx; i <= hiIdx; i++)
        {
            sum += magnitude[i];
            count++;
        }
        return count > 0 ? sum / count : 0;
    }

    private static double GetBandRmsEnergy(double[] magnitude, double freqRes, double lo, double hi)
    {
        int loIdx = Math.Max(0, (int)(lo / freqRes));
        int hiIdx = Math.Min(magnitude.Length - 1, (int)(hi / freqRes));
        if (loIdx >= hiIdx) return 0;

        double sumSq = 0;
        int count = 0;
        for (int i = loIdx; i <= hiIdx; i++)
        {
            sumSq += magnitude[i] * magnitude[i];
            count++;
        }
        return count > 0 ? Math.Sqrt(sumSq / count) : 0;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
