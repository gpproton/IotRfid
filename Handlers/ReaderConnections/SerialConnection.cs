﻿//
// SerialConnection.cs
//
// Author:
//       Godwin peter .O <me@godwin.dev>
//
// Copyright (c) 2020 MIT
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO.Ports;
using System.Threading;
using uhf_rfid_catch.Helpers;
using uhf_rfid_catch.Protocols;

namespace uhf_rfid_catch.Handlers.ReaderConnections
{
    public class SerialConnection
    {
        private static readonly ConfigContext SettingsContext = new ConfigContext();
        private readonly MainLogger _logger = new MainLogger();
        private readonly ConsoleOnlyLogger _consoleOnlyLogger = new ConsoleOnlyLogger();
        
        public readonly string Sportname = SettingsContext.Resolve("ReaderSerialPortName");
        public readonly int Sbaudrate = Convert.ToInt32(SettingsContext.Resolve("ReaderSerialBaudRate"));
        public readonly int Sdatabits = Convert.ToInt32(SettingsContext.Resolve("ReaderSerialDataBits"));
        public readonly int Smaxretry = Convert.ToInt32(SettingsContext.Resolve("ReaderConnectionRetries"));
        public readonly int Smaxtimeout = Convert.ToInt32(SettingsContext.Resolve("ReaderConnectionTimeout"));
        public bool AutoRead = Convert.ToBoolean(SettingsContext.Resolve("ReaderAutoReadMode"));

        public SerialConnection()
        {
        }

        public SerialPort BuildConnection(string knownPortName)
        {
            var portName = knownPortName == "null" ? SuggestPort() : knownPortName;

            var parity = Parity.None;
            var stopBits = StopBits.One;
            var srp = new SerialPort(portName, Sbaudrate, parity, Sdatabits, stopBits)
            {
                DtrEnable = true, RtsEnable = true, ReadTimeout = 500, WriteTimeout = 500
            };
            return srp;
        }

        public string SuggestPort()
        {
            var selectedPort = "/dev/tty.usb_serial";

            foreach (string portName in ListConnection())
            {
                if(portName.Contains("serial") || portName.Contains("uart"))
                {
                    selectedPort = portName;
                    break;
                }
            }

            return selectedPort;
        }

        public byte ConnectionChannel(SerialPort builtConnection)
        {
            var receivedByte = (byte)builtConnection.ReadByte();
            return receivedByte;
        }

        public void AutoReadData(SerialPort builtConnection, IReaderProtocol protoInfo)
        {
            var localByteSize = 0;
            var localMaxByteSize = protoInfo.AutoReadLength;
            byte[] decodedBytes = new byte[localMaxByteSize];
            while (AutoRead)
            {
#if DEBUG
                if (true)
                {
#endif
#if !DEBUG
                    if (builtConnection.IsOpen)
                    {
#endif
                    if(protoInfo.DirectAutoRead)
                    {
                        // Start decode part of the process.
                        ////
                        if (builtConnection.IsOpen && builtConnection.BytesToRead > 0)
                        {
                            var _returnedData = ConnectionChannel(builtConnection);
                            try
                            {
                                decodedBytes[localByteSize] = _returnedData;
                            }
                            catch (Exception e)
                            {
                                _logger.Trigger("Error", e.ToString());
                            }
                            
                            if (localMaxByteSize - 1 == localByteSize)
                            {
                                if (builtConnection.IsOpen)
                                {
                                    protoInfo.ReceivedBytes = decodedBytes;
                                }
                                _consoleOnlyLogger.Push("Info", " Received HEX: " + protoInfo.seeData().Replace("-", String.Empty));
                            }
                            ++localByteSize;
                        }
                        else
                        {
                            localByteSize = 0;
                        }
                        
#if DEBUG
                        if (!builtConnection.IsOpen)
                        {
                            protoInfo.ReceivedBytes =
                                CheckSum.HexToByteArray("CCFFFF10320D01E2000016370402410910C2E9AC");

                            _consoleOnlyLogger.Push("Info",
                                " Received HEX: " + protoInfo.seeData().Replace("-", String.Empty));
                            Thread.Sleep(5000);
                        }
#endif
                        
                        
                    }
                    else
                    {
                        Console.WriteLine("Implement non auto read mode.");
                    }
                    
                }
                else
                {
////                    RequestStop();
                    _logger.Trigger("Error", $"Serial connection failed to open, retrying now.");
                }
                
            }
        }

        private string[] ListConnection()
        {
            return SerialPort.GetPortNames();
        }
        public void ShowPorts()
        {
            string startLine = $"---- {ListConnection().Length} Serial ports available ----";
            Console.WriteLine(startLine);
            foreach (string portName in ListConnection())
            {
                Console.WriteLine(portName);
            }
        }
    }
}
