using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using LanTalk.Core.Models;

namespace LanTalk.Network.Files;

public static class ProtectedFileTransfer
{
    public const string MetadataEncryptionAlgorithm = "FILE-METADATA-AES-256-GCM";
    public const string StreamEncryptionAlgorithm = "FILE-STREAM-AES-256-GCM";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static EncryptedMessagePayload EncryptMetadata(byte[] key, string fileId, string metadataJson)
    {
        var nonce = DeriveNonce(key, "metadata", fileId, 0);
        var plaintext = Encoding.UTF8.GetBytes(metadataJson);
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        var associatedData = BuildAssociatedData("metadata", fileId, 0, plaintext.Length);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, cipherText, tag, associatedData);

        CryptographicOperations.ZeroMemory(plaintext);
        return new EncryptedMessagePayload(
            MetadataEncryptionAlgorithm,
            "metadata",
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipherText),
            Convert.ToBase64String(tag));
    }

    public static string DecryptMetadata(byte[] key, string fileId, EncryptedMessagePayload payload)
    {
        var nonce = Convert.FromBase64String(payload.Nonce);
        var cipherText = Convert.FromBase64String(payload.CipherText);
        var tag = Convert.FromBase64String(payload.Tag);
        var plaintext = new byte[cipherText.Length];
        var associatedData = BuildAssociatedData("metadata", fileId, 0, plaintext.Length);

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plaintext, associatedData);
        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static long GetEncryptedWireSize(long plainLength, int chunkSize)
    {
        if (plainLength <= 0)
        {
            return TagSize;
        }

        var actualChunkSize = chunkSize > 0 ? chunkSize : 64 * 1024;
        var chunkCount = (plainLength + actualChunkSize - 1) / actualChunkSize;
        return plainLength + chunkCount * TagSize;
    }

    public static async Task WriteEncryptedAsync(
        Stream source,
        Stream destination,
        string fileId,
        long plainLength,
        byte[] key,
        int chunkSize,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var actualChunkSize = chunkSize > 0 ? chunkSize : 64 * 1024;
        var buffer = ArrayPool<byte>.Shared.Rent(actualChunkSize);
        long sentPlainBytes = 0;
        var chunkIndex = 0L;

        progress?.Report(plainLength == 0 ? 100 : 0);

        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, actualChunkSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var nonce = DeriveNonce(key, "chunk", fileId, chunkIndex);
                var cipherText = new byte[read];
                var tag = new byte[TagSize];
                var associatedData = BuildAssociatedData("chunk", fileId, chunkIndex, read);

                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, buffer.AsSpan(0, read), cipherText, tag, associatedData);

                await destination.WriteAsync(cipherText, cancellationToken).ConfigureAwait(false);
                await destination.WriteAsync(tag, cancellationToken).ConfigureAwait(false);
                sentPlainBytes += read;
                chunkIndex++;
                progress?.Report(plainLength == 0 ? 100 : sentPlainBytes * 100d / plainLength);
            }

            if (plainLength == 0)
            {
                var nonce = DeriveNonce(key, "chunk", fileId, 0);
                var empty = Array.Empty<byte>();
                var tag = new byte[TagSize];
                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, empty, empty, tag, BuildAssociatedData("chunk", fileId, 0, 0));
                await destination.WriteAsync(tag, cancellationToken).ConfigureAwait(false);
                progress?.Report(100);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static Stream CreateDecryptingWriteStream(
        Stream destination,
        string fileId,
        long plainLength,
        byte[] key,
        int chunkSize)
    {
        return new EncryptedFileReceiveStream(destination, fileId, plainLength, key, chunkSize);
    }

    private static byte[] DeriveNonce(byte[] key, string scope, string fileId, long chunkIndex)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{scope}|{fileId}|{chunkIndex}"));
        return hash[..NonceSize];
    }

    private static byte[] BuildAssociatedData(string scope, string fileId, long chunkIndex, int plainLength)
    {
        return Encoding.UTF8.GetBytes($"{scope}|{fileId}|{chunkIndex}|{plainLength}|{StreamEncryptionAlgorithm}");
    }

    private sealed class EncryptedFileReceiveStream : Stream
    {
        private readonly Stream _destination;
        private readonly string _fileId;
        private readonly long _plainLength;
        private readonly byte[] _key;
        private readonly int _chunkSize;
        private readonly MemoryStream _pending = new();
        private long _plainWritten;
        private long _chunkIndex;
        private bool _disposed;

        public EncryptedFileReceiveStream(Stream destination, string fileId, long plainLength, byte[] key, int chunkSize)
        {
            _destination = destination;
            _fileId = fileId;
            _plainLength = Math.Max(0, plainLength);
            _key = key.ToArray();
            _chunkSize = chunkSize > 0 ? chunkSize : 64 * 1024;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _plainWritten;

        public override long Position
        {
            get => _plainWritten;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _destination.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer.AsMemory(offset, count), CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _pending.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            await ProcessPendingAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                EnsureComplete();
                _pending.Dispose();
                _destination.Dispose();
                CryptographicOperations.ZeroMemory(_key);
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                await base.DisposeAsync().ConfigureAwait(false);
                return;
            }

            EnsureComplete();
            _pending.Dispose();
            await _destination.DisposeAsync().ConfigureAwait(false);
            CryptographicOperations.ZeroMemory(_key);
            _disposed = true;
            await base.DisposeAsync().ConfigureAwait(false);
        }

        private async Task ProcessPendingAsync(CancellationToken cancellationToken)
        {
            while (TryGetExpectedCipherChunkLength(out var expectedLength) && _pending.Length >= expectedLength)
            {
                _pending.Position = 0;
                var cipherText = new byte[expectedLength - TagSize];
                var tag = new byte[TagSize];
                await _pending.ReadExactlyAsync(cipherText, cancellationToken).ConfigureAwait(false);
                await _pending.ReadExactlyAsync(tag, cancellationToken).ConfigureAwait(false);

                var remaining = _pending.Length - _pending.Position;
                if (remaining > 0)
                {
                    var tail = new byte[remaining];
                    await _pending.ReadExactlyAsync(tail, cancellationToken).ConfigureAwait(false);
                    _pending.SetLength(0);
                    _pending.Write(tail, 0, tail.Length);
                }
                else
                {
                    _pending.SetLength(0);
                }

                var plain = new byte[cipherText.Length];
                var nonce = DeriveNonce(_key, "chunk", _fileId, _chunkIndex);
                using var aes = new AesGcm(_key, TagSize);
                aes.Decrypt(nonce, cipherText, tag, plain, BuildAssociatedData("chunk", _fileId, _chunkIndex, cipherText.Length));
                await _destination.WriteAsync(plain, cancellationToken).ConfigureAwait(false);
                _plainWritten += plain.Length;
                _chunkIndex++;
            }
        }

        private bool TryGetExpectedCipherChunkLength(out long expectedLength)
        {
            if (_plainWritten > _plainLength)
            {
                throw new IOException("解密后的附件长度超过预期。");
            }

            if (_plainLength == 0 && _chunkIndex == 0)
            {
                expectedLength = TagSize;
                return true;
            }

            if (_plainWritten == _plainLength)
            {
                expectedLength = 0;
                return false;
            }

            var remainingPlain = _plainLength - _plainWritten;
            var plainChunkLength = (int)Math.Min(_chunkSize, remainingPlain);
            expectedLength = plainChunkLength + TagSize;
            return true;
        }

        private void EnsureComplete()
        {
            if (_pending.Length > 0 || _plainWritten != _plainLength)
            {
                throw new IOException("加密附件流未完整接收。");
            }
        }
    }
}
