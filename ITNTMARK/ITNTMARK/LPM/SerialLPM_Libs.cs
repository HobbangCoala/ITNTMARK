using System;
using System.Threading;

using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Diagnostics;
//using NLog;

namespace SerialPortLib
{

    /// DataBits enum.
    public enum DataBits
    {

        Five = 5,
        Six,
        Seven,
        Eight,
    }

    /// Serial port I/O
    public class SerialPortInput
    {

        #region Private Fields

//        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private SerialPort _serialPort;

        private string _portName    = "";
        private int _baudRate       = 115200;
        private StopBits _stopBits  = StopBits.One;
        private Parity _parity      = Parity.None;
        private DataBits _dataBits  = DataBits.Eight;

        // Read/Write error state variable
        private bool _gotReadWriteError = true;

        // Serial port reader task
        private Thread _reader;
        private CancellationTokenSource _readerCts;
        // Serial port connection watcher
        private Thread _connectionWatcher;
        private CancellationTokenSource _connectionWatcherCts;

        private readonly object _accessLock = new object();
        private bool _disconnectRequested;

        #endregion

        #region Public Events

        /// Connected state changed event.
        public delegate void ConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs args);

        /// Occurs when connected state changed.
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// Message received event.
        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

        /// Occurs when message received.
        public event MessageReceivedEventHandler MessageReceived;

        #endregion

        #region Public Members

        public SerialPortInput()
        {
            _connectionWatcherCts = new CancellationTokenSource();
            _readerCts = new CancellationTokenSource();
        }

        /// Connect to the serial port.
        public bool Connect()
        {
            if (_disconnectRequested)
            {
                return false;
            }
            lock (_accessLock)
            {
                Disconnect();
                Open();
                _connectionWatcherCts = new CancellationTokenSource();
                _connectionWatcher = new Thread(ConnectionWatcherTask) { IsBackground = true };
                _connectionWatcher.Start(_connectionWatcherCts.Token);
            }
            return IsConnected;
        }

        /// Disconnect the serial port.
        public void Disconnect()
        {
            if (_disconnectRequested)
            {
                return;
            }
            _disconnectRequested = true;
            Close();
            lock (_accessLock)
            {
                if (_connectionWatcher != null)
                {
                    if (!_connectionWatcher.Join(5000))
                    {
                        _connectionWatcherCts.Cancel();
                    }
                    _connectionWatcher = null;
                }
                _disconnectRequested = false;
            }
        }


        /// Gets a value indicating whether the serial port is connected.

        /// value true if connected; otherwise, false. value
        public bool IsConnected
        {
            get { return _serialPort != null && !_gotReadWriteError && !_disconnectRequested; }
        }

        /// Sets the serial port options.

        public void SetPort(string portName, int baudRate = 115200, StopBits stopBits = StopBits.One, Parity parity = Parity.None, DataBits dataBits = DataBits.Eight)
        {
            if (_portName != portName || _baudRate != baudRate || stopBits != _stopBits || parity != _parity || dataBits != _dataBits)
            {
                // Change Parameters request
                // Take into account immediately the new connection parameters
                // (do not use the ConnectionWatcher, otherwise strange things will occurs !)
                _portName = portName;
                _baudRate = baudRate;
                _stopBits = stopBits;
                _parity = parity;
                _dataBits = dataBits;
                if (IsConnected)
                {
                    Connect();      // Take into account immediately the new connection parameters
                }
                LogDebug(string.Format("Port parameters changed (port name {0} / baudrate {1} / stopbits {2} / parity {3} / databits {4})", portName, baudRate, stopBits, parity, dataBits));
            }
        }

        /// Sends the message.
        /// returns true, if message was sent, false otherwise.
        public bool SendMessage(byte[] message)
        {
            bool success = false;
            if (IsConnected)
            {
                try
                {
                    _serialPort.Write(message, 0, message.Length);
                    success = true;
                    LogDebug(BitConverter.ToString(message));
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            return success;
        }

        /// Gets/Sets serial port reconnection delay in milliseconds.
        public int ReconnectDelay { get; set; } = 1000;

        #endregion

        #region Private members

        #region Serial Port handling

        private bool Open()
        {
            bool success = false;
            lock (_accessLock)
            {
                Close();
                try
                {
                    bool tryOpen = true;

                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        tryOpen = (tryOpen && System.IO.File.Exists(_portName));
                    }
                    if (tryOpen)
                    {
                        _serialPort = new SerialPort();
                        _serialPort.ErrorReceived += HandleErrorReceived;
                        _serialPort.PortName = _portName;
                        _serialPort.BaudRate = _baudRate;
                        _serialPort.StopBits = _stopBits;
                        _serialPort.Parity = _parity;
                        _serialPort.DataBits = (int)_dataBits;

                        // We are not using serialPort.DataReceived event for receiving data since this is not working under Linux/Mono.
                        // We use the readerTask instead (see below).
                        _serialPort.Open();

                        success = true;
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    Close();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _gotReadWriteError = false;
                    // Start the Reader task
                    _readerCts = new CancellationTokenSource();
                    _reader = new Thread(ReaderTask) { IsBackground = true };
                    _reader.Start(_readerCts.Token);

                    OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(true));
                }
            }
            return success;
        }

        private void Close()
        {
            lock (_accessLock)
            {
                // Stop the Reader task
                if (_reader != null)
                {
                    if (!_reader.Join(5000))
                        _readerCts.Cancel();
                    _reader = null;
                }
                if (_serialPort != null)
                {
                    _serialPort.ErrorReceived -= HandleErrorReceived;
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs(false));
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                _gotReadWriteError = true;
            }
        }

        private void HandleErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LogError(e.EventType);
        }

        #endregion

        #region Background Tasks

        private void ReaderTask(object data)
        {
            var ct = (CancellationToken) data;
            while (IsConnected && !ct.IsCancellationRequested)
            {
                int msglen = 0;
                //
                try
                {
                    msglen = _serialPort.BytesToRead;
                    if (msglen > 0)
                    {
                        byte[] message = new byte[msglen];
                        //
                        int readBytes = 0;
                        while (readBytes <= 0)
                            readBytes = _serialPort.Read(message, readBytes, msglen - readBytes); // noop
                        if (MessageReceived != null)
                        {
                            OnMessageReceived(new MessageReceivedEventArgs(message));
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                    _gotReadWriteError = true;
                    Thread.Sleep(1000);
                }
            }
        }

        private void ConnectionWatcherTask(object data)
        {
            var ct = (CancellationToken) data;
            // This task takes care of automatically reconnecting the interface
            // when the connection is drop or if an I/O error occurs
            while (!_disconnectRequested && !ct.IsCancellationRequested)
            {
                if (_gotReadWriteError)
                {
                    try
                    {
                        Close();
                        // wait "ReconnectDelay" seconds before reconnecting
                        Thread.Sleep(ReconnectDelay);
                        if (!_disconnectRequested)
                        {
                            try
                            {
                                Open();
                            }
                            catch (Exception e)
                            {
                                LogError(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }
                }
                if (!_disconnectRequested)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void LogDebug(string message)
        {
            //_logger.Debug(message);
            Debug.WriteLine(message);
        }

        private void LogError(Exception ex)
        {
            //_logger.Error(ex, null);
            Debug.WriteLine(ex, null);
        }

        private void LogError(SerialError error)
        {
            //_logger.Error("SerialPort ErrorReceived: {0}", error);
            Debug.WriteLine(String.Format("SerialPort ErrorReceived: {0}", error));
        }

        #endregion

        #region Events Raising

        /// Raises the connected state changed event.
        /// <param name="args">Arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            LogDebug(args.Connected.ToString());
            if (ConnectionStatusChanged != null)
            {
                ConnectionStatusChanged(this, args);
            }
        }

        /// Raises the message received event.
        /// <param name="args">Arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            //LogDebug(BitConverter.ToString(args.Data));
            if (MessageReceived != null)
            {
                MessageReceived(this, args);
            }
        }

        #endregion

        #endregion

    }

}
