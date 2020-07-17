using System;
using System.Security.Cryptography.X509Certificates;

namespace Kritner.Mtls.Core
{
    public interface ICertificateAuthorityValidator
    {
        bool IsValid(X509Certificate2 clientCert);
    }
}
