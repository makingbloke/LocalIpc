// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc.Exceptions;
using DotDoc.LocalIpc.Serializers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;

namespace DotDoc.LocalIpc;

/// <summary>
/// LocalIpc base class.
/// </summary>
public abstract class LocalIpcBase : IDisposable
{
    private const string _ioExceptionPipeBrokenMessage = "Pipe is broken.";
    private readonly PipeStream _sendPipeStream;
    private readonly PipeStream _receivePipeStream;
    private readonly ISerializer _serializer;
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isReceiveEventsEnabled;
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "This is disposed by setting IsReceiveEventsEnabled = false in the Dispose method.")]
    private CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalIpcBase"/> class.
    /// </summary>
    /// <param name="sendPipeStream">The send <see cref="PipeStream"/>.</param>
    /// <param name="receivePipeStream">The receive <see cref="PipeStream"/>.</param>
    /// <param name="serializer">An optional serializer, if none is specified then <see cref="DefaultSerializer"/> is used.</param>
    protected LocalIpcBase(PipeStream sendPipeStream, PipeStream receivePipeStream, ISerializer serializer)
    {
        (this._sendPipeStream, this._receivePipeStream, this._serializer) = (sendPipeStream, receivePipeStream, serializer ?? new DefaultSerializer());
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="LocalIpcBase"/> class.
    /// </summary>
    ~LocalIpcBase()
    {
        this.Dispose(false);
    }

    /// <summary>
    /// Gets or sets the event raised when object is disposed.
    /// </summary>
    public EventHandler Disposed { get; set; }

    /// <summary>
    /// Gets or sets the event raised when an object is received (only raised when <see cref="IsReceiveEventsEnabled"/> is <see langword="true"/>).
    /// </summary>
    public EventHandler<ReceivedEventArgs> Received { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Receive events are raised.
    /// </summary>
    public bool IsReceiveEventsEnabled
    {
        get
        {
            return this._isReceiveEventsEnabled;
        }

        set
        {
            if (!this._isInitialized)
            {
                throw new LocalIpcNotInitializedException();
            }

            if (value != this._isReceiveEventsEnabled)
            {
                this._isReceiveEventsEnabled = value;

                if (this._isReceiveEventsEnabled)
                {
                    this._cancellationTokenSource = new CancellationTokenSource();
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                object value = await this.ReceiveObjectAsync<object>(this._cancellationTokenSource.Token).ConfigureAwait(false);
                                this.Received?.Invoke(this, new ReceivedEventArgs(value));
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
                    this._isReceiveEventsEnabled = false;

                    this._cancellationTokenSource.Cancel();
                    this._cancellationTokenSource.Dispose();
                    this._cancellationTokenSource = null;
                }
            }
        }
    }

    /// <summary>
    /// Release resources used by this class.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Initialize method.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/>.</returns>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._isInitialized)
        {
            throw new LocalIpcAlreadyInitializedException();
        }

        this._isInitialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send an object.
    /// </summary>
    /// <param name="value">The object to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/>.</returns>
    public async Task SendAsync(object value, CancellationToken cancellationToken = default)
    {
        if (!this._isInitialized)
        {
            throw new LocalIpcNotInitializedException();
        }

        byte[] valueBytes = this._serializer.Serialize(value);
        byte[] lengthBytes = BitConverter.GetBytes(valueBytes.Length);

        await this.SendBytesAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
        await this.SendBytesAsync(valueBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Receive an object.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task<object> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return this.ReceiveAsync<object>(cancellationToken);
    }

    /// <summary>
    /// Receive an object.
    /// </summary>
    /// <typeparam name="T">The type of object to receive.</typeparam>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public Task<T> ReceiveAsync<T>(CancellationToken cancellationToken = default)
    {
        if (!this._isInitialized)
        {
            throw new LocalIpcNotInitializedException();
        }

        if (this.IsReceiveEventsEnabled)
        {
            throw new InvalidOperationException("Cannot manually receive an object while Receive events are enabled.");
        }

        return this.ReceiveObjectAsync<T>(cancellationToken);
    }

    /// <summary>
    /// Releases the unmanaged resources used by this
    /// class and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this._isDisposed)
        {
            this._isDisposed = true;

            if (disposing)
            {
                if (this._isInitialized)
                {
                    this.IsReceiveEventsEnabled = false;
                }

                this._sendPipeStream.Dispose();
                this._receivePipeStream.Dispose();

                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Receive and deserialize an object.
    /// </summary>
    /// <typeparam name="T">The type of object to receive.</typeparam>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    private async Task<T> ReceiveObjectAsync<T>(CancellationToken cancellationToken = default)
    {
        byte[] lengthBytes = await this.ReceiveBytesAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
        int length = BitConverter.ToInt32(lengthBytes);

        byte[] valueBytes = await this.ReceiveBytesAsync(length, cancellationToken).ConfigureAwait(false);
        T value = (T)this._serializer.Deserialize(valueBytes);
        return value;
    }

    /// <summary>
    /// Receive a fixed length byte array.
    /// </summary>
    /// <param name="length">The number of bytes to receive.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the data read.</returns>
    private async Task<byte[]> ReceiveBytesAsync(int length, CancellationToken cancellationToken)
    {
        try
        {
            byte[] bytes = new byte[length];
            int bytesRead = await this._receivePipeStream.ReadAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);

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
        catch (IOException e) when (e.Message == _ioExceptionPipeBrokenMessage)
        {
            throw new PipeBrokenException();
        }
    }

    /// <summary>
    /// Send a fixed length byte array.
    /// </summary>
    /// <param name="bytes">The byte array to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/>.</returns>
    private async Task SendBytesAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            await this._sendPipeStream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException e) when (e.Message == _ioExceptionPipeBrokenMessage)
        {
            throw new PipeBrokenException();
        }
    }
}
