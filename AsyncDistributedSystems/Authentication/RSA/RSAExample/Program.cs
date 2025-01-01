using System.Security.Cryptography;
using System.Text;

RSA rsaA = GenerateRSAKeyPairModern();
RSA rsaB = GenerateRSAKeyPairModern();

string message = "This is a secret message from Program A to Program B.";

byte[] signature = SignMessageModern(message, rsaA);

bool isValid = VerifySignatureModern(message, signature, rsaA);

if (isValid)
{
    Console.WriteLine("Signature is valid.");
}
else
{
    Console.WriteLine("Signature is not valid.");
}

// Generate Key Pair
static RSA GenerateRSAKeyPairModern()
{
    return RSA.Create(2048);
}

// Sign Message
static byte[] SignMessageModern(string message, RSA rsa)
{
    byte[] data = Encoding.UTF8.GetBytes(message);
    return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
}

// Verify Signature
static bool VerifySignatureModern(string message, byte[] signature, RSA rsa)
{
    byte[] data = Encoding.UTF8.GetBytes(message);
    return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
}