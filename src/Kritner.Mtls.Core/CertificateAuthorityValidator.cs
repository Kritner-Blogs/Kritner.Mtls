using Microsoft.Extensions.Logging;
using System;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace Kritner.Mtls.Core
{
    public class CertificateAuthorityValidator : ICertificateAuthorityValidator
    {
        private readonly ILogger<CertificateAuthorityValidator> _logger;

        // this should probably be injected via config or loaded from the cert
        // Apparently the bytes are in the reverse order when using this BigInteger parse method,
        // hence the reverse
        private readonly byte[] _caCertSubjectKeyIdentifier = BigInteger.Parse(
            "e9be86f64eb53bc12c1b5fe0f63df450274811da",
            System.Globalization.NumberStyles.HexNumber
        ).ToByteArray().Reverse().ToArray();

        private const string AuthorityKeyIdentifier = "Authority Key Identifier";
        
        public CertificateAuthorityValidator(ILogger<CertificateAuthorityValidator> logger)
        {
            _logger = logger;
        }

        public bool IsValid(X509Certificate2 clientCert)
        {
            _logger.LogInformation($"Validating certificate within the {nameof(CertificateAuthorityValidator)}");
            
            if (clientCert == null)
                return false;
            foreach (var extension in clientCert.Extensions)
            {
                if (extension.Oid.FriendlyName.Equals(AuthorityKeyIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var authorityKeyIdentifier = new byte[_caCertSubjectKeyIdentifier.Length];
                        // Copy from the extension raw data, starting at the index that should be after the "KeyID=" bytes
                        Array.Copy(
                            extension.RawData, extension.RawData.Length - _caCertSubjectKeyIdentifier.Length, 
                            authorityKeyIdentifier, 0, 
                            authorityKeyIdentifier.Length);

                        if (_caCertSubjectKeyIdentifier.SequenceEqual(authorityKeyIdentifier))
                        {
                            _logger.LogInformation("Successfully validated the certificate came from the intended CA.");
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"Client cert with subject '{clientCert.Subject}' not signed by our CA.");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, string.Empty);
                        return false;
                    }
                }
            }
            
            _logger.LogError($"'{clientCert.Subject}' did not contain the extension to check for CA validity.");
            return false;
        }
    }
}
