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
        private readonly ISerializer _serializer;
        private readonly PipeStream _sendPipeStream;
        private readonly PipeStream _receivePipeStream;
        private bool _isDisposed;

        private bool _receiveEventsEnabled;
        private CancellationTokenSource _cancellationTokenSource;

        private const string PipeBrokenMessage = "Pipe is broken.";         // message returned in IOException when pipe is broken.

        protected LocalIpcBase(PipeStream sendPipeStream, PipeStream receivePipeStream, ISerializer serializer)
        {
            _sendPipeStream = sendPipeStream;
            _receivePipeStream = receivePipeStream;
            _serializer = serializer ?? new DefaultSerializer();
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

        public EventHandler Disposed;
        public EventHandler<ReceivedEventArgs> Received;

        public bool EnableReceiveEvents
        {
            set
            {
                if (_receiveEventsEnabled != value)
                {
                    _receiveEventsEnabled = value;

                    if (_receiveEventsEnabled)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    object value = await ReceiveAsync(_cancellationTokenSource.Token);
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

            get => _receiveEventsEnabled;
        }

        public async Task SendAsync(object value, CancellationToken cancellationToken = default)
        {
            byte[] valueBytes = _serializer.Serialize(value);
            byte[] lengthBytes = BitConverter.GetBytes(valueBytes.Length);

            await WriteBytesAsync(lengthBytes, cancellationToken);
            await WriteBytesAsync(valueBytes, cancellationToken);
        }

        public Task<object> ReceiveAsync(CancellationToken cancellationToken = default) =>
            ReceiveAsync<object>(cancellationToken);

        public async Task<T> ReceiveAsync<T>(CancellationToken cancellationToken = default)        
        {
            byte[] lengthBytes = await ReadBytesAsync(sizeof(int), cancellationToken);
            int length = BitConverter.ToInt32(lengthBytes);

            byte[] valueBytes = await ReadBytesAsync(length, cancellationToken);
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
                    EnableReceiveEvents = false;

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
                int bytesRead = await _receivePipeStream.ReadAsync(bytes.AsMemory(0, bytes.Length), cancellationToken);

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
                await _sendPipeStream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken);            
            }
            catch (IOException e) when (e.Message == PipeBrokenMessage)
            {
                throw new PipeBrokenException();
            }
        }
    }
}
