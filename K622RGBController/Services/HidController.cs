using HidLibrary;
using K622RGBController.Models;
using System.Diagnostics;

namespace K622RGBController.Services;

/// <summary>
/// Thread-safe HID controller for Redragon K622 Sinowealth keyboards.
/// Sends per-key RGB data as a 382-byte feature report.
/// </summary>
public class HidController : IDisposable
{
    private const int VendorId = 0x258A;
    private const int ProductId = 0x0049;
    private static readonly byte[] PacketHeader = { 0x08, 0x0A, 0x7A, 0x01 };
    private const int PacketLength = 382;

    private HidDevice? _device;
    private readonly object _lock = new();
    private bool _connected;

    public bool Connected => _connected;
    public string DeviceName { get; private set; } = "No conectado";
    public int NumKeys => KeyboardLayout.TotalKeys;

    public bool Connect()
    {
        lock (_lock)
        {
            if (_connected) return true;

            try
            {
                // Find all matching devices
                var devices = HidDevices.Enumerate(VendorId, ProductId).ToList();
                Debug.WriteLine($"Found {devices.Count} HID devices for {VendorId:X4}:{ProductId:X4}");

                HidDevice? target = null;

                // Prefer the vendor-specific interface (usage page 0xFF00, usage 0x0001)
                foreach (var dev in devices)
                {
                    try
                    {
                        var caps = dev.Capabilities;
                        Debug.WriteLine($"  Device: UsagePage=0x{caps.UsagePage:X4}, Usage=0x{caps.Usage:X4}, " +
                                        $"FeatureLen={caps.FeatureReportByteLength}");

                        if ((ushort)caps.UsagePage == 0xFF00 && caps.Usage == 0x0001)
                        {
                            target = dev;
                            break;
                        }
                    }
                    catch
                    {
                        // Skip devices we can't query
                    }
                }

                // Fallback: find any device with a large enough feature report
                if (target == null)
                {
                    foreach (var dev in devices)
                    {
                        try
                        {
                            if (dev.Capabilities.FeatureReportByteLength >= PacketLength)
                            {
                                target = dev;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (target == null)
                {
                    Debug.WriteLine("No suitable keyboard HID device found");
                    return false;
                }

                target.OpenDevice();

                if (!target.IsOpen)
                {
                    Debug.WriteLine("Failed to open HID device");
                    return false;
                }

                _device = target;
                _connected = true;

                DeviceName = "Redragon K622";

                Debug.WriteLine($"Connected: {DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection failed: {ex.Message}");
                _connected = false;
                return false;
            }
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            if (_device != null)
            {
                try { _device.CloseDevice(); }
                catch { }
                _device = null;
                _connected = false;
                DeviceName = "No conectado";
                Debug.WriteLine("Disconnected — firmware resumes native lighting");
            }
        }
    }

    /// <summary>
    /// Send per-key RGB colors to the keyboard.
    /// Iterates in COLUMN-MAJOR order to match Sinowealth firmware.
    /// </summary>
    public bool SendColors((byte R, byte G, byte B)[] colors, double intensity = 1.0)
    {
        intensity = Math.Clamp(intensity, 0.01, 1.0);

        // Build the packet: header + color data + padding
        var packet = new byte[PacketLength];
        Array.Copy(PacketHeader, 0, packet, 0, PacketHeader.Length);

        int byteIdx = PacketHeader.Length;
        int colorIdx = 0;

        // Column-major iteration to match Sinowealth packet order
        for (int col = 0; col < KeyboardLayout.NumCols; col++)
        {
            for (int row = 0; row < KeyboardLayout.NumRows; row++)
            {
                if (byteIdx + 2 >= PacketLength) break;

                string key = KeyboardLayout.Layout[row, col];
                if (key == "NAN")
                {
                    packet[byteIdx++] = 0;
                    packet[byteIdx++] = 0;
                    packet[byteIdx++] = 0;
                }
                else
                {
                    if (colorIdx < colors.Length)
                    {
                        packet[byteIdx++] = (byte)Math.Clamp((int)(colors[colorIdx].R * intensity), 0, 255);
                        packet[byteIdx++] = (byte)Math.Clamp((int)(colors[colorIdx].G * intensity), 0, 255);
                        packet[byteIdx++] = (byte)Math.Clamp((int)(colors[colorIdx].B * intensity), 0, 255);
                    }
                    else
                    {
                        packet[byteIdx++] = 0;
                        packet[byteIdx++] = 0;
                        packet[byteIdx++] = 0;
                    }
                    colorIdx++;
                }
            }
        }

        lock (_lock)
        {
            if (!_connected)
            {
                if (!Connect()) return false;
            }

            try
            {
                return _device!.WriteFeatureData(packet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Send failed: {ex.Message} — attempting reconnect");
                _connected = false;
                try { _device?.CloseDevice(); } catch { }
                _device = null;

                // Auto-reconnect once
                if (Connect())
                {
                    try
                    {
                        return _device!.WriteFeatureData(packet);
                    }
                    catch (Exception ex2)
                    {
                        Debug.WriteLine($"Send failed after reconnect: {ex2.Message}");
                    }
                }
                return false;
            }
        }
    }

    public bool SendSolidColor(byte r, byte g, byte b, double intensity = 1.0)
    {
        var colors = new (byte R, byte G, byte B)[NumKeys];
        Array.Fill(colors, (r, g, b));
        return SendColors(colors, intensity);
    }

    public bool SendBlack() => SendSolidColor(0, 0, 0);

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
