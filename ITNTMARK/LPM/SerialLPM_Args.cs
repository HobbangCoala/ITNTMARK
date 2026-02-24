using System;

namespace SerialPortLib
{

    /// Connected state changed event arguments.
    public class ConnectionStatusChangedEventArgs
    {
        /// The connected state.
        public readonly bool Connected;

        /// Initializes a new instance of the <see cref="SerialPortLib.ConnectionStatusChangedEventArgs"/> class.
        /// <param name="state">State of the connection (true = connected, false = not connected).</param>
        public ConnectionStatusChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }

    /// Message received event arguments.
    public class MessageReceivedEventArgs
    {
        /// The data.
        public readonly byte[] Data;

        /// Initializes a new instance of the <see cref="SerialPortLib.MessageReceivedEventArgs"/> class.
        /// <param name="data">Data.</param>
        public MessageReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
