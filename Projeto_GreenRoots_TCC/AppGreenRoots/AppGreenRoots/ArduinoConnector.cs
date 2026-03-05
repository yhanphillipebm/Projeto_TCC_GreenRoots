using System;
using System.IO.Ports;

namespace GreenRootsApp
{
    public class ArduinoConnector
    {
        private SerialPort? _serialPort;
        public event Action<string>? DataReceived;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;
        public string ConnectedPortName { get; private set; } = string.Empty;

        public bool Connect(string portName, int baudRate = 115200)
        {
            if (IsConnected) Disconnect();
            try
            {
                _serialPort = new SerialPort(portName, baudRate);
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                ConnectedPortName = portName;
                return true;
            }
            catch (Exception) { return false; }
        }

        public void Disconnect()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
                ConnectedPortName = string.Empty;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    while (_serialPort.BytesToRead > 0)
                    {
                        string data = _serialPort.ReadLine();
                        DataReceived?.Invoke(data);
                    }
                }
            }
            catch (Exception) { /* Ignora erros comuns de fechamento de porta */ }
        }
    }
}