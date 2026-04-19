using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Wrapper for JSON array parsing since JsonUtility doesn't support top-level arrays.
/// </summary>
[Serializable]
public class TelemetryDataArray
{
    public TelemetryData[] packets;
}

/// <summary>
/// A mock network client that reads telemetry data from a local JSON file.
/// Useful for demo purposes when the backend is not available.
/// </summary>
public class FileNetworkClient : INetworkClient
{
    public event Action<TelemetryData> OnTelemetryReceived;

    private SynchronizationContext mainThreadContext;
    private CancellationTokenSource cts;
    private bool isStreaming;
    private float updateInterval = 1.0f;

    public FileNetworkClient(float updateInterval = 1.0f)
    {
        this.updateInterval = updateInterval;
        mainThreadContext = SynchronizationContext.Current;
        
        if (mainThreadContext == null)
        {
            Debug.LogWarning("FileNetworkClient: SynchronizationContext.Current is null. Events might not fire on the main thread.");
        }
    }

    public void Connect(string filePath)
    {
        if (isStreaming) return;

        // In this implementation, ipAddress is treated as the file path or filename in StreamingAssets
        string absolutePath = filePath;
        if (!File.Exists(absolutePath))
        {
            absolutePath = Path.Combine(Application.streamingAssetsPath, filePath);
        }

        if (File.Exists(absolutePath))
        {
            Debug.Log($"FileNetworkClient: Loading mock data from {absolutePath}");
            try
            {
                string jsonText = File.ReadAllText(absolutePath);
                Debug.Log($"FileNetworkClient: Read {jsonText.Length} characters from file.");
                
                TelemetryDataArray dataArray = JsonUtility.FromJson<TelemetryDataArray>(jsonText);

                if (dataArray != null && dataArray.packets != null && dataArray.packets.Length > 0)
                {
                    Debug.Log($"FileNetworkClient: Successfully parsed {dataArray.packets.Length} packets. Starting stream...");
                    StartStreaming(dataArray.packets);
                }
                else
                {
                    string errorMsg = "FileNetworkClient: JSON file is empty, invalid format, or mismatch. Expected { \"packets\": [...] }";
                    if (dataArray != null && dataArray.packets == null) errorMsg += " (packets array was null)";
                    Debug.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FileNetworkClient: Failed to read/parse mock file: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"FileNetworkClient: Mock file NOT found at {absolutePath}. Please check your StreamingAssets folder.");
        }
    }

    public void Disconnect()
    {
        StopStreaming();
    }

    public void SendOperatorCommand(string command)
    {
        Debug.Log($"FileNetworkClient (Mock): Sending command: {command}");
    }

    public void SendAudioBlob(byte[] wavData)
    {
        Debug.Log($"FileNetworkClient (Mock): Sending audio blob ({wavData.Length} bytes)");
    }

    private void StartStreaming(TelemetryData[] packets)
    {
        cts = new CancellationTokenSource();
        isStreaming = true;
        Task.Run(() => StreamingLoop(packets, cts.Token));
    }

    private void StopStreaming()
    {
        isStreaming = false;
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private async Task StreamingLoop(TelemetryData[] packets, CancellationToken token)
    {
        int index = 0;
        while (!token.IsCancellationRequested)
        {
            TelemetryData currentData = packets[index];
            
            // Post to main thread
            if (mainThreadContext != null)
            {
                mainThreadContext.Post(_ => OnTelemetryReceived?.Invoke(currentData), null);
            }
            else
            {
                // Fallback for cases where context wasn't captured
                OnTelemetryReceived?.Invoke(currentData);
            }

            index = (index + 1) % packets.Length;

            try
            {
                await Task.Delay((int)(updateInterval * 1000), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
