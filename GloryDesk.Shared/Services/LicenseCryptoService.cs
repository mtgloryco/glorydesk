using System;
using System.Security.Cryptography;
using System.Text;

namespace InventoryManagementSystem.Services
{
    public class LicenseCryptoService
    {
        // RSA Public Key (SubjectPublicKeyInfo) from the server
        private const string PublicKeyBase64 =
            "MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAwHgB0bk9b/jb/1nADpB4" +
            "b0fK70c5aFqNK8S2VwgNdqeBVWSnnxwy8jFl2D1AnYDXQJOOyM8m3BbcXSXnQPkb" +
            "ZOycnwc4xK2tu0+FbWdzzx85VhjqqbKA1RwSdyhoeKEsb2qgw6xJtOgWLGmR5NqO" +
            "x7mYg1o1OQrWE3TTeLRdCErfRHCMxyQq+LmOLk8O+5pBCUZSuCdA8Q6uMlPoS6I/" +
            "97trY2UrQiUnSot8/eV3tTvU3bc+BoTrryu3rsRabVpxSSTm35cczwab/Xu9OCq2" +
            "kOJuLbFBr30wVqPIOBQE3VaaD0h7z9ovjY5pGsR6XDhQ7MVsI4vE91fdiVtHArhb" +
            "svpgMl8SOoLT7opTO8zzQ/yRzJoQ6ndF6plP6kXqf91jdsl9wZARxO6a96aIllyw" +
            "1yP9/vMf5HG2DxGjr+9gmTVy5KIgbfQbAOoOtRHkn7Wamgs1ndOUPmLumZE9g6E8" +
            "MOmlLtjrexUh/io9+ljD43c3ykAHXb4naGHGNpNTmmPdAgMBAAE=";

        public bool VerifySignature(string canonicalPayloadJson, string signatureBase64)
        {
            try
            {
                byte[] publicKeyBytes = Convert.FromBase64String(PublicKeyBase64);
                byte[] dataBytes = Encoding.UTF8.GetBytes(canonicalPayloadJson);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                return rsa.VerifyData(
                    dataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );
            }
            catch
            {
                // Fail closed — any error invalidates license
                return false;
            }
        }
    }
}
