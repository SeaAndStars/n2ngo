using System.Collections.Generic;
using System.Linq;
using System;

namespace N2NGO.Utils;

internal static class N2NEdgeOutputHelper
{
    public enum EdgeDeviceAllocating
    {
        IP, Mask, MAC
    }
    public enum EdgeAnalysisResults
    {
        None = 0,
        Connected = 1 << 0,
        NoWindowsTapDevices = 1 << 1,
        DeviceOperationFailure = 1 << 2
    }

    public static Dictionary<EdgeDeviceAllocating, string?> GetEdgeDeviceAllocation(string output)
    {
        Dictionary<EdgeDeviceAllocating, string?> result = new()
            {
                { EdgeDeviceAllocating.IP, null },
                { EdgeDeviceAllocating.Mask, null },
                { EdgeDeviceAllocating.MAC, null }
            };

        foreach (var line in output.Split(Environment.NewLine).Reverse())
        {
            if (line.Contains("created local tap device IP:"))
            {
                string ip = line[(line.IndexOf("IP: ") + "IP: ".Length)..];
                result[EdgeDeviceAllocating.IP] = ip.Remove(ip.IndexOf(", Mask:"));

                string mask = line[(line.IndexOf("Mask: ") + "Mask: ".Length)..];
                result[EdgeDeviceAllocating.Mask] = mask.Remove(mask.IndexOf(", MAC:"));

                result[EdgeDeviceAllocating.MAC] = line[(line.IndexOf("MAC: ") + "MAC: ".Length)..];
            }
        }

        return result;
    }

    public static EdgeAnalysisResults Analyze(string output)
    {
        EdgeAnalysisResults results = EdgeAnalysisResults.None;

        if (output.Contains("[OK] edge <<< ================ >>> supernode"))
            results |= EdgeAnalysisResults.Connected;

        if (output.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
            results |= EdgeAnalysisResults.NoWindowsTapDevices;

        if (output.Contains("WARNING: Unable to set device"))
            results |= EdgeAnalysisResults.DeviceOperationFailure;

        return results;
    }
}
