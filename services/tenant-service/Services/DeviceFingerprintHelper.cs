using System.Security.Cryptography;
using System.Text;

namespace BiSoyle.Tenant.Service.Services;

/// <summary>
/// Device fingerprint tolerance matching algorithm.
/// 
/// Calculates similarity between two device fingerprints using field-by-field comparison.
/// Accepts if match percentage >= threshold.
/// </summary>
public static class DeviceFingerprintHelper
{
    /// <summary>
    /// Calculate match percentage between two device fingerprints.
    /// 
    /// Fingerprint format: field1|field2|field3|... (pipe-separated)
    /// Compares each field and calculates overall match percentage.
    /// </summary>
    public static double CalculateMatchPercentage(string fingerprint1, string fingerprint2)
    {
        if (string.IsNullOrEmpty(fingerprint1) || string.IsNullOrEmpty(fingerprint2))
            return 0.0;
        
        if (fingerprint1 == fingerprint2)
            return 1.0; // 100% match
        
        var fields1 = fingerprint1.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var fields2 = fingerprint2.Split('|', StringSplitOptions.RemoveEmptyEntries);
        
        if (fields1.Length == 0 || fields2.Length == 0)
            return 0.0;
        
        int matches = 0;
        int totalFields = Math.Max(fields1.Length, fields2.Length);
        
        // Compare each field
        for (int i = 0; i < Math.Min(fields1.Length, fields2.Length); i++)
        {
            if (string.Equals(fields1[i], fields2[i], StringComparison.OrdinalIgnoreCase))
            {
                matches++;
            }
        }
        
        // If one fingerprint has more fields, penalize
        int fieldDifference = Math.Abs(fields1.Length - fields2.Length);
        double penalty = fieldDifference * 0.1; // 10% penalty per missing field
        
        double matchRatio = (double)matches / totalFields;
        double finalScore = Math.Max(0.0, matchRatio - penalty);
        
        return finalScore;
    }
    
    /// <summary>
    /// Check if fingerprints match within tolerance threshold.
    /// </summary>
    public static bool IsMatch(string fingerprint1, string fingerprint2, double toleranceThreshold)
    {
        var matchPercentage = CalculateMatchPercentage(fingerprint1, fingerprint2);
        return matchPercentage >= toleranceThreshold;
    }
    
    /// <summary>
    /// Compute SHA-256 hash of device fingerprint data.
    /// Used for consistent fingerprint storage.
    /// </summary>
    public static string ComputeHash(string fingerprint)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(fingerprint);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}






