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
    /// <summary>
    /// LocalIpc base class.
    /// </summary>
    public abstract class LocalIpcBase : IDisposable
    {

        private bool _isDisposed;
        private readonly PipeStream _sendPipeStream;
        private readonly PipeStream _receivePipeStream;
        private readonly ISerializer _serializer;
        private bool _isInitialized;
        private bool _isReceiveEventsEnabled;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Event raised when object is disposed.
        /// </summary>
        public EventHandler Disposed;

        /// <summary>
        /// Event raised when an object is received (only raised when <see cref="IsReceiveEventsEnabled"/> is <c>true</c>).
        /// </summary>
        public EventHandler<ReceivedEventArgs> Received;

        protected LocalIpcBase(PipeStream sendPipeStream, PipeStream receivePipeStream, ISerializer serializer)
        {
            (_sendPipeStream, _receivePipeStream, _serializer) = (sendPipeStream, receivePipeStream, serializer ?? new DefaultSerializer());
        }

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

        /// <summary>
        /// Sets a value indicating whether Receive events are raised.
        /// </summary>
        public bool IsReceiveEventsEnabled
        {
            set
            {
                if (!_isInitialized)
                {
                    throw new LocalIpcNotInitializedException();
                }

                switch (value)
                {
                    case true when !_isReceiveEventsEnabled:
                        _isReceiveEventsEnabled = true;

                        _cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    object value = await ReceiveObjectAsync<object>(_cancellationTokenSource.Token).ConfigureAwait(false);
                                    Received?.Invoke(this, new ReceivedEventArgs(value));
                                }
                                catch (TaskCanceledException)
                                {
                                    break;
                                }
                            }
                        });
                        break;

                    case false when _isReceiveEventsEnabled:
                        _isReceiveEventsEnabled = false;

                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                        break;
                }
            }

            get
            {
                return _isReceiveEventsEnabled;
            }
        }

        /// <summary>
        /// Send an object.
        /// </summary>
        /// <param name="value">The object to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><see cref="Task"/>.</returns>
        public Task SendAsync(object value, CancellationToken cancellationToken = default)
        {
            return SendAsync<object>(value, cancellationToken);
        }

        /// <summary>
        /// Send an object.
        /// </summary>
        /// <param name="value">The object to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task SendAsync<T>(T value, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Receive an object.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><see cref="Task"/>.</returns>
        public Task<object> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return ReceiveAsync<object>(cancellationToken);
        }

        /// <summary>
        /// Receive an object.
        /// </summary>
        /// <typeparam name="T">The type of object to receive.</typeparam>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A instance of <see cref="{T}"/>.</returns>
        public Task<T> ReceiveAsync<T>(CancellationToken cancellationToken = default)        
        {
            if (!_isInitialized)
            {
                throw new LocalIpcNotInitializedException();
            }

            if (IsReceiveEventsEnabled)
            {
                throw new InvalidOperationException("Cannot manually receive an object while Receive events are enabled.");
            }

            return ReceiveObjectAsync<T>(cancellationToken);
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

        private async Task<T> ReceiveObjectAsync<T>(CancellationToken cancellationToken = default)
        {
            byte[] lengthBytes = await ReadBytesAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
            int length = BitConverter.ToInt32(lengthBytes);

            byte[] valueBytes = await ReadBytesAsync(length, cancellationToken).ConfigureAwait(false);
            T value = _serializer.Deserialize<T>(valueBytes);
            return value;
        }

        private async Task<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken)
        {
            try
            {
                byte[] bytes = new byte[length];
                int bytesRead = await _receivePipeStream.ReadAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    throw new PipeBrokenException();
                }

                if (bytesRead < bytes.Length)
                {
                    throw new EndOfStreamException("Unexpected end of stream on receive pipe.");
                }

                return bytes;
            }
            catch (Exception e) when (IsPipeBrokenException(e))
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
            catch (Exception e) when (IsPipeBrokenException(e))
            {
                throw new PipeBrokenException();
            }
        }

        private static bool IsPipeBrokenException(Exception e)
            => e is IOException && e.Message == "Pipe is broken.";
    }
}
