using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Formats.Asn1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BiSoyle.Tenant.Service.Services;

/// <summary>
/// RSA-based license signing and verification service.
/// 
/// Production Recommendations:
/// - Use Azure Key Vault or HSM for private key storage (never export private keys)
/// - Use certificate-based key loading from secure storage
/// - Implement key rotation policies
/// - Enable audit logging for all signing/verification operations
/// - Use separate keys for signing vs. encryption (if needed)
/// </summary>
public class RsaLicenseService
{
    private readonly ILogger<RsaLicenseService> _logger;
    private readonly IConfiguration _configuration;
    private RSA? _signingKey;
    private RSA? _verificationKey;

    public RsaLicenseService(ILogger<RsaLicenseService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        try
        {
            InitializeKeys();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RSA License Service initialization failed, continuing with demo keys");
            // Continue with demo keys if initialization fails
            _signingKey = RSA.Create(2048);
            _verificationKey = RSA.Create();
            _verificationKey.ImportParameters(_signingKey.ExportParameters(false));
        }
    }

    /// <summary>
    /// Initialize RSA keys from configuration or generate demo keys.
    /// 
    /// Production: Load from Key Vault, HSM, or secure certificate store.
    /// </summary>
    private void InitializeKeys()
    {
        try
        {
            // Option 1: Load from PFX certificate file (Production recommended)
            var certPath = _configuration["LicenseSigning:CertificatePath"];
            var certPassword = _configuration["LicenseSigning:CertificatePassword"];
            
            if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
            {
                var cert = new X509Certificate2(certPath, certPassword, 
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                
                _signingKey = cert.GetRSAPrivateKey();
                _verificationKey = cert.GetRSAPublicKey();
                
                _logger.LogInformation("License signing key loaded from certificate: {Path}", certPath);
                return;
            }
            
            // Option 2: Load from PEM files (Development)
            var defaultKeysDir = Path.Combine(Directory.GetCurrentDirectory(), "keys");
            var defaultPrivateKeyPath = Path.Combine(defaultKeysDir, "license-private-key.pem");
            var defaultPublicKeyPath = Path.Combine(defaultKeysDir, "license-public-key.pem");

            var privateKeyPath = _configuration["LicenseSigning:PrivateKeyPath"];
            var publicKeyPath = _configuration["LicenseSigning:PublicKeyPath"];

            if (string.IsNullOrWhiteSpace(privateKeyPath))
            {
                privateKeyPath = defaultPrivateKeyPath;
            }

            if (string.IsNullOrWhiteSpace(publicKeyPath))
            {
                publicKeyPath = defaultPublicKeyPath;
            }
            
            if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath) &&
                !string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
            {
                _signingKey = LoadPrivateKeyFromPem(privateKeyPath);
                _verificationKey = LoadPublicKeyFromPem(publicKeyPath);
                
                _logger.LogInformation("License signing keys loaded from PEM files (private: {PrivatePath}, public: {PublicPath})", privateKeyPath, publicKeyPath);
                return;
            }
            
            // Option 3: Generate demo keys (Development only - NOT FOR PRODUCTION!)
            _logger.LogWarning("No certificate/PEM files found. Generating demo keys. NOT FOR PRODUCTION!");
            _signingKey = RSA.Create(2048);
            _verificationKey = RSA.Create();
            _verificationKey.ImportParameters(_signingKey.ExportParameters(false));
            
            // Save demo keys for client verification (Development only)
            SaveDemoKeys(defaultKeysDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing license signing keys");
            throw;
        }
    }

    /// <summary>
    /// Sign license JSON data with RSA private key.
    /// </summary>
    public string SignLicense(string licenseJson)
    {
        if (_signingKey == null)
            throw new InvalidOperationException("Signing key not initialized");

        var jsonBytes = Encoding.UTF8.GetBytes(licenseJson);
        var signatureBytes = _signingKey.SignData(jsonBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verify license signature with RSA public key.
    /// </summary>
    public bool VerifySignature(string licenseJson, string signatureBase64)
    {
        if (_verificationKey == null)
            throw new InvalidOperationException("Verification key not initialized");

        try
        {
            var jsonBytes = Encoding.UTF8.GetBytes(licenseJson);
            var signatureBytes = Convert.FromBase64String(signatureBase64);
            
            return _verificationKey.VerifyData(jsonBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying license signature");
            return false;
        }
    }

    /// <summary>
    /// Export public key in PEM format (for client verification).
    /// </summary>
    public string ExportPublicKeyPem()
    {
        if (_verificationKey == null)
            throw new InvalidOperationException("Verification key not initialized");

        var parameters = _verificationKey.ExportParameters(false);
        
        // Convert to PEM format (simplified - use proper ASN.1 encoding in production)
        var keyBytes = ExportRSAPublicKey(parameters);
        var base64 = Convert.ToBase64String(keyBytes);
        
        return $"-----BEGIN PUBLIC KEY-----\n{base64}\n-----END PUBLIC KEY-----";
    }

    /// <summary>
    /// Export public key in DER format (binary).
    /// </summary>
    public byte[] ExportPublicKeyDer()
    {
        if (_verificationKey == null)
            throw new InvalidOperationException("Verification key not initialized");

        var parameters = _verificationKey.ExportParameters(false);
        return ExportRSAPublicKey(parameters);
    }

    // Helper methods

    private RSA LoadPrivateKeyFromPem(string path)
    {
        var pemContent = File.ReadAllText(path);
        var rsa = RSA.Create();
        
        // Remove PEM headers and decode
        var base64 = pemContent
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();
        
        var keyBytes = Convert.FromBase64String(base64);
        rsa.ImportRSAPrivateKey(keyBytes, out _);
        
        return rsa;
    }

    private RSA LoadPublicKeyFromPem(string path)
    {
        var pemContent = File.ReadAllText(path);
        var rsa = RSA.Create();
        
        var base64 = pemContent
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();
        
        var keyBytes = Convert.FromBase64String(base64);
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        
        return rsa;
    }

    private byte[] ExportRSAPublicKey(RSAParameters parameters)
    {
        // Use System.Formats.Asn1 for proper ASN.1 encoding (available in .NET 5+)
        var writer = new AsnWriter(AsnEncodingRules.DER);
        
        using (writer.PushSequence()) // SubjectPublicKeyInfo
        {
            using (writer.PushSequence()) // AlgorithmIdentifier
            {
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.1"); // rsaEncryption
                writer.WriteNull(); // parameters
            }
            
            // RSAPublicKey SEQUENCE
            byte[] rsaPublicKey;
            var rsaWriter = new AsnWriter(AsnEncodingRules.DER);
            using (rsaWriter.PushSequence())
            {
                rsaWriter.WriteInteger(parameters.Modulus ?? throw new InvalidOperationException("Modulus is null"));
                rsaWriter.WriteInteger(parameters.Exponent ?? throw new InvalidOperationException("Exponent is null"));
            }
            rsaPublicKey = rsaWriter.Encode();
            
            // Write as bit string
            writer.WriteBitString(rsaPublicKey);
        }
        
        return writer.Encode();
    }

    private void SaveDemoKeys(string keysDir)
    {
        try
        {
            Directory.CreateDirectory(keysDir);
            
            // Save public key for client
            var publicKeyPem = ExportPublicKeyPem();
            File.WriteAllText(Path.Combine(keysDir, "license-public-key.pem"), publicKeyPem);
            
            // Save private key for dev convenience (never do this in production)
            var privateKeyPem = ExportPrivateKeyPem();
            File.WriteAllText(Path.Combine(keysDir, "license-private-key.pem"), privateKeyPem);
            
            _logger.LogInformation("Demo license keys saved under {KeysDir}", keysDir);
            _logger.LogWarning("Demo private key saved locally for development. NEVER use this pattern in production!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving demo keys");
        }
    }

    private string ExportPrivateKeyPem()
    {
        if (_signingKey == null)
            throw new InvalidOperationException("Signing key not initialized");

        var privateKeyBytes = _signingKey.ExportRSAPrivateKey();
        var base64 = Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN RSA PRIVATE KEY-----\n{base64}\n-----END RSA PRIVATE KEY-----";
    }
}

