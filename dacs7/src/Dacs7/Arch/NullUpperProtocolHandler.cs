﻿using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7
{
    internal class NullUpperProtocolHandler : IUpperProtocolHandler
    {
        private static IUpperProtocolHandler _staticHandler;
        public static IUpperProtocolHandler Instance
        {
            get { return _staticHandler ?? (_staticHandler = new NullUpperProtocolHandler()); }
        }

        public Func<byte[], Task<SocketError>> SocketSendFunc { get; set; }
        public void Connect()
        {
        }

        public void Shutdown()
        {
        }

        public byte[] AddUpperProtocolFrame(byte[] txData)
        {
            return txData;
        }

        public IEnumerable<byte[]> RemoveUpperProtocolFrame(byte[] rxData, int count)
        {
            return new List<byte[]> { rxData.SubArray(0, count) };
        }

        public bool Connected { get { return false; } }
    }
}
