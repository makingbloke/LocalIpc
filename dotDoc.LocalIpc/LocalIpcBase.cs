// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc.Exceptions;
using DotDoc.LocalIpc.Serializers;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace DotDoc.LocalIpc
{
    public abstract class LocalIpcBase : IDisposable
    {
        private bool _isDisposed;
        private readonly PipeStream _sendPipeStream;
        private readonly PipeStream _receivePipeStream;
        private readonly ISerializer _serializer;
        private bool _isInitialized;
        private bool _isReceiveEventsEnabled;
        private CancellationTokenSource _cancellationTokenSource;

        private const string PipeBrokenMessage = "Pipe is broken.";         // message returned in IOException when pipe is broken.

        public EventHandler Disposed;
        public EventHandler<ReceivedEventArgs> Received;

        protected LocalIpcBase(PipeStream sendPipeStream, PipeStream receivePipeStream, ISerializer serializer) =>
            (_sendPipeStream, _receivePipeStream, _serializer) = (sendPipeStream, receivePipeStream, serializer ?? new DefaultSerializer());

        ~LocalIpcBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                throw new LocalIpcAlreadyInitializedException();
            }

            _isInitialized = true;
            return Task.CompletedTask;
        }

        public bool IsReceiveEventsEnabled
        {
            set
            {
                if (!_isInitialized)
                {
                    throw new LocalIpcNotInitializedException();
                }

                if (_isReceiveEventsEnabled != value)
                {
                    _isReceiveEventsEnabled = value;

                    if (_isReceiveEventsEnabled)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    object value = await ReceiveAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                                    Received?.Invoke(this, new ReceivedEventArgs(value));
                                }
                                catch (TaskCanceledException)
                                {
                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                    }
                }
            }

            get => _isReceiveEventsEnabled;
        }

        public async Task SendAsync(object value, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                throw new LocalIpcNotInitializedException();
            }

            byte[] valueBytes = _serializer.Serialize(value);
            byte[] lengthBytes = BitConverter.GetBytes(valueBytes.Length);

            await WriteBytesAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
            await WriteBytesAsync(valueBytes, cancellationToken).ConfigureAwait(false);
        }

        public Task<object> ReceiveAsync(CancellationToken cancellationToken = default) =>
            ReceiveAsync<object>(cancellationToken);

        public async Task<T> ReceiveAsync<T>(CancellationToken cancellationToken = default)        
        {
            if (!_isInitialized)
            {
                throw new LocalIpcNotInitializedException();
            }

            byte[] lengthBytes = await ReadBytesAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
            int length = BitConverter.ToInt32(lengthBytes);

            byte[] valueBytes = await ReadBytesAsync(length, cancellationToken).ConfigureAwait(false);
            T value = _serializer.Deserialize<T>(valueBytes);
            return value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    if (_isInitialized)
                    {
                        IsReceiveEventsEnabled = false;
                    }

                    _sendPipeStream.Dispose();
                    _receivePipeStream.Dispose();

                    Disposed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private async Task<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken)
        {
            try
            {
                byte[] bytes = new byte[length];
                int bytesRead = await _receivePipeStream.ReadAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0) throw new PipeBrokenException();
                if (bytesRead < bytes.Length) throw new EndOfStreamException("Unexpected end of stream on receive pipe.");
                return bytes;
            }
            catch (IOException e) when (e.Message == PipeBrokenMessage)
            {
                throw new PipeBrokenException();
            }
        }

        private async Task WriteBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            try
            {
                await _sendPipeStream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);            
            }
            catch (IOException e) when (e.Message == PipeBrokenMessage)
            {
                throw new PipeBrokenException();
            }
        }
    }
}
