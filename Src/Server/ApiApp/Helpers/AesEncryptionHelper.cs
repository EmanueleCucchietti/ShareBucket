using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ApiApp.Helpers;

public interface IAesEncryptionService
{
    Task DecryptStreamAsync(Stream input, Stream output, byte[] password, byte[] salt, int bufferSize = 1024 * 1024);
    Task EncryptStreamAsync(Stream input, Stream output, byte[] password, byte[] salt, int bufferSize = 1024 * 1024);
    byte[] RandomByteArray(int length);
}

public class AesEncryptionService : IAesEncryptionService
{
    private const int AES256KeySize = 256;
    private const int BufferSize = 1024 * 1024;

    public byte[] RandomByteArray(int length)
    {

        byte[] result = new byte[length];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(result);
        }

        return result;
    }
    private async Task ExecuteEncryptionDecription(Stream input, Stream output, CryptoStreamMode cryptoStreamMode, byte[] password, byte[] salt,  int bufferSize)
    {
        using (var key = GenerateKey(password, salt))
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;


            var pool = ArrayPool<byte>.Shared;
            var buffer = pool.Rent(bufferSize + aes.BlockSize / 8);

            //var buffer = new byte[BufferSize + aes.BlockSize / 8];
            var blockSize = aes.BlockSize / 8;

            int bytesRead;

            try
            {
                Debug.WriteLine($"Input stream length: {input.Length}");

                using (var csStream = new CryptoStream(output, (cryptoStreamMode == CryptoStreamMode.Write) ? aes.CreateEncryptor() : aes.CreateDecryptor(), CryptoStreamMode.Write, (cryptoStreamMode == CryptoStreamMode.Write) ? false : true))
                {
                    while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        Debug.WriteLine($"Bytes read: {bytesRead}");
                        await csStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    }

                    pool.Return(buffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
    public async Task EncryptStreamAsync(Stream input, Stream output, byte[] password, byte[] salt, int bufferSize = BufferSize) =>
        await ExecuteEncryptionDecription(input, output, CryptoStreamMode.Write, password, salt, bufferSize);
    public async Task DecryptStreamAsync(Stream input, Stream output, byte[] password, byte[] salt, int bufferSize = BufferSize) =>
        await ExecuteEncryptionDecription(input, output, CryptoStreamMode.Read, password, salt, bufferSize);
    private Rfc2898DeriveBytes GenerateKey(byte[] password, byte[] salt)
    {
        return new Rfc2898DeriveBytes(password, salt, 52768);
    }
    private bool CheckPassword(byte[] password, byte[] salt, byte[] key)
    {
        using (Rfc2898DeriveBytes r = GenerateKey(password, salt))
        {
            byte[] newKey = r.GetBytes(AES256KeySize / 8);
            return newKey.SequenceEqual(key);
        }

    }


}