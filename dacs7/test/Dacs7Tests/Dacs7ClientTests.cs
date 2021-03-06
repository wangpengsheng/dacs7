//#define TEST_PLC

using Dacs7;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;


using Dacs7.Control;
using Dacs7.Metadata;
using Dacs7.Helper;
using Dacs7.Domain;

namespace Dacs7Tests
{



#if TEST_PLC || REAL_PLC

    public class Dacs7ClientTests
    {
        private ILoggerFactory _loggerFactory = new LoggerFactory().AddConsole();
        private const string Ip = "127.0.0.1";//"127.0.0.1";
        //private const string Ip = "192.168.0.148";
        //private const string Ip = "192.168.1.17";//"127.0.0.1";
        //private const string ConnectionString = "Data Source=" + Ip + ":102,0,2;PduSize=240"; //"Data Source=192.168.1.10:102,0,2";
        public const string ConnectionString = "Data Source=" + Ip + ":102,0,2;Connect Timeout=10000"; //"Data Source=192.168.1.10:102,0,2";
        private const int TestDbNr = 250;
        private const int TestByteOffset = 524;
        private const int TestByteOffset2 = 525;
        private const int TestBitOffset = 16 * 8; // DBX16.0
        private const int TestBitOffset2 = 16 * 8 + 1; // DBX16.1
        private const int LongDbNumer = 558;


        public Dacs7ClientTests()
        {
            ////Manually instantiate all Ack types, because we have a different executing assembly in the test framework and so this will not be done automatically
            //new S7AckDataProtocolPolicy();
            //new S7ReadJobAckDataProtocolPolicy();
            //new S7WriteJobAckDataProtocolPolicy();
        }

        [Fact]
        public void ValidateBoolReadLength()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);

            var boolValue1 = client.ReadAny<bool>(TestDbNr, TestBitOffset, 8).ToList();
            Assert.Equal(8, boolValue1.Count);


            client.Disconnect();
        }

        [Fact]
        public void ConnectionStringTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            const string connectionString = "Data Source = " + Ip + ":102,0,2";
            client.Connect(connectionString);
            Assert.True(client.IsConnected);
        }

        [Fact]
        public void ConnectDisconnectTest()
        {
            ushort pduSize = 960;
            var client = new Dacs7Client(_loggerFactory);
            var connectionString = ConnectionString + $";PduSize={pduSize}";
            client.Connect(connectionString);
            Assert.Equal(Ip == "127.0.0.1" ? pduSize : pduSize / 2, client.PduSize); // RTX onyl supports 960 / 2
            Assert.True(client.IsConnected, $"NotConnected with PduSize={pduSize}");
            client.Disconnect();
            Assert.False(client.IsConnected, $"NotDisconnected with PduSize={pduSize}");

            pduSize /=2;
            connectionString = ConnectionString + $";PduSize={pduSize}";
            client.Connect(connectionString);
            Assert.True(client.IsConnected, $"NotConnected with PduSize={pduSize}");
            Assert.Equal(pduSize, client.PduSize);
            client.Disconnect();
            Assert.False(client.IsConnected, $"NotDisconnected with PduSize={pduSize}");

        }

        [Fact]
        public async Task ConnectDisconnectAsyncTest()
        {
            ushort pduSize = 960;
            var client = new Dacs7Client(_loggerFactory);
            var connectionString = ConnectionString + $";PduSize={pduSize}";
            await client.ConnectAsync(connectionString);
            Assert.Equal(Ip == "127.0.0.1" ? pduSize : pduSize / 2, client.PduSize); // RTX onyl supports 960 / 2
            Assert.True(client.IsConnected, $"NotConnected with PduSize={pduSize}");
            await client.DisconnectAsync();
            Assert.False(client.IsConnected, $"NotDisconnected with PduSize={pduSize}");

            pduSize /= 2;
            connectionString = ConnectionString + $";PduSize={pduSize}";
            await client.ConnectAsync(connectionString);
            Assert.True(client.IsConnected, $"NotConnected with PduSize={pduSize}");
            Assert.Equal(pduSize, client.PduSize);
            await client.DisconnectAsync();
            Assert.False(client.IsConnected, $"NotDisconnected with PduSize={pduSize}");

        }


        [Fact]
        public void TestGenericReadWriteAny()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);

            for (int i = 0; i < 8; i++)
            {
                var offset = TestBitOffset + i;

                //Set to false and read
                client.WriteAny<bool>(TestDbNr, offset, false);
                var boolValue1 = client.ReadAny<bool>(TestDbNr, offset);

                //Set to true and read
                client.WriteAny(TestDbNr, offset, true);
                var boolValue2 = client.ReadAny<bool>(TestDbNr, offset);

                Assert.NotEqual(boolValue1, boolValue2);

                client.WriteAny<int>(TestDbNr, TestByteOffset, 512);
                var intValue1 = client.ReadAny<int>(TestDbNr, TestByteOffset);

                client.WriteAny<int>(TestDbNr, TestByteOffset, i);
                var intValue2 = client.ReadAny<int>(TestDbNr, TestByteOffset);

                Assert.NotEqual(intValue1, intValue2);
                Assert.Equal(512, intValue1);
                Assert.Equal(i, intValue2);

                client.WriteAny(TestDbNr, TestByteOffset, "TEST", 4);
                var strValue1 = client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                var writeVal = i.ToString().PadRight(4, 'X');
                client.WriteAny(TestDbNr, TestByteOffset, writeVal, 4);
                var strValue2 = client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                Assert.NotEqual(strValue1, strValue2);
                Assert.Equal("TEST", strValue1);
                Assert.Equal(writeVal, strValue2);

                var firstWriteVal = "TEST".ToCharArray();
                client.WriteAny(TestDbNr, TestByteOffset, firstWriteVal, 4);
                var charValue1 = client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                var secondWriteVal = i.ToString().PadRight(4, 'X').ToCharArray();
                client.WriteAny(TestDbNr, TestByteOffset, secondWriteVal, 4);
                var charValue2 = client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                Assert.False(charValue1.SequenceEqual(charValue2));
                Assert.True(firstWriteVal.SequenceEqual(charValue1));
                Assert.True(secondWriteVal.SequenceEqual(charValue2));
            }

            client.Disconnect();

        }

        [Fact]
        public async Task TestGenericReadWriteAnyAsync()
        {
            var client = new Dacs7Client(_loggerFactory);
            await client.ConnectAsync(ConnectionString);

            for (int i = 0; i < 8; i++)
            {
                var offset = TestBitOffset + i;

                //Set to false and read
                await client.WriteAnyAsync<bool>(TestDbNr, offset, false);


                var boolValue1 = await client.ReadAnyAsync<bool>(TestDbNr, offset);

                //Set to true and read
                await client.WriteAnyAsync(TestDbNr, offset, true);
                var boolValue2 = await client.ReadAnyAsync<bool>(TestDbNr, offset);

                Assert.NotEqual(boolValue1, boolValue2);

                await client.WriteAnyAsync<int>(TestDbNr, TestByteOffset, 512);
                var intValue1 = await client.ReadAnyAsync<int>(TestDbNr, TestByteOffset);

                await client.WriteAnyAsync<int>(TestDbNr, TestByteOffset, i);
                var intValue2 = await client.ReadAnyAsync<int>(TestDbNr, TestByteOffset);

                Assert.NotEqual(intValue1, intValue2);
                Assert.Equal(512, intValue1);
                Assert.Equal(i, intValue2);

                await client.WriteAnyAsync(TestDbNr, TestByteOffset, "TEST", 4);
                var strValue1 = (await client.ReadAnyAsync<string>(TestDbNr, TestByteOffset, 4))?.FirstOrDefault();

                var writeVal = i.ToString().PadRight(4, 'X');
                await client.WriteAnyAsync(TestDbNr, TestByteOffset, writeVal, 4);
                var strValue2 = (await client.ReadAnyAsync<string>(TestDbNr, TestByteOffset, 4))?.FirstOrDefault();

                Assert.NotEqual(strValue1, strValue2);
                Assert.Equal("TEST", strValue1);
                Assert.Equal(writeVal, strValue2);

                var firstWriteVal = "TEST".ToCharArray();
                await client.WriteAnyAsync(TestDbNr, TestByteOffset, firstWriteVal, 4);
                var charValue1 = await client.ReadAnyAsync<char>(TestDbNr, TestByteOffset, 4);

                var secondWriteVal = i.ToString().PadRight(4, 'X').ToCharArray();
                await client.WriteAnyAsync(TestDbNr, TestByteOffset, secondWriteVal, 4);
                var charValue2 = await client.ReadAnyAsync<char>(TestDbNr, TestByteOffset, 4);

                Assert.False(charValue1.SequenceEqual(charValue2));
                Assert.True(firstWriteVal.SequenceEqual(charValue1));
                Assert.True(secondWriteVal.SequenceEqual(charValue2));
            }

            await client.DisconnectAsync();

        }

        [Fact]
        public void TestReadWriteAnyBigData()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            client.WriteAny(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            var result = client.ReadAny(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            client.WriteAny(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            result = client.ReadAny(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public async Task TestReadWriteAnyAsyncBigData()
        {
            var client = new Dacs7Client(_loggerFactory);
            await client.ConnectAsync(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            await client.WriteAnyAsync(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            var result = await client.ReadAnyAsync(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            await client.WriteAnyAsync(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            result = await client.ReadAnyAsync(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            await client.DisconnectAsync();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadAnyRaw()
        {
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}}
            };

            var writeOperations = new List<WriteOperationParameter>
            {
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
            };
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            client.WriteAny(writeOperations);
            var result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count, result.Count());
            Assert.Equal((byte)0x05, result.First()[0]);
            Assert.Equal((byte)0x01, result.Last()[0]);
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadAny()
        {
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}}
            };

            var writeOperations = new List<WriteOperationParameter>
            {
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
            };
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            client.WriteAny(writeOperations);
            var result = client.ReadAny(operations);
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadWriteAnyBigData()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            var length = 3534;
            var buffer = new byte[length];

            //Write 0
            client.WriteAny(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            var result = client.ReadAny(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            client.WriteAny(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            result = client.ReadAny(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public async Task TestMultipleReadWriteAnyAsyncBigData()
        {
            var client = new Dacs7Client(_loggerFactory);
            await client.ConnectAsync(ConnectionString);
            var length = 3534;
            var buffer = new byte[length];

            //Write 0
            await client.WriteAnyAsync(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            var result = await client.ReadAnyAsync(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            await client.WriteAnyAsync(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            result = await client.ReadAnyAsync(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            await client.DisconnectAsync();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestaBunchOfMultiReads()
        {
            var db = 10;
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= 0, Type=typeof(byte), Args = new int[]{1, db}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= 1, Type=typeof(bool), Args = new int[]{1, db}},
            };

            // There is a bug in the snap7 server!!
            for (int i = 0; i < ((Ip == "127.0.0.1") ? 18 : 100); i++)
            {
                operations.Add(new ReadOperationParameter { Area = PlcArea.DB, Offset = 1 + i, Type = typeof(bool), Args = new int[] { 1, db } });
            }

            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            var result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count(), result.Count());

            operations.RemoveAt(0);
            result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count(), result.Count());
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestaBunchOfMultiWrites()
        {
            var db = 11;
            var operations = new List<WriteOperationParameter>();
            var readOperations = new List<ReadOperationParameter>();


            // There is a bug in the snap7 server!!
            for (int i = 0; i < ((Ip == "127.0.0.1") ? 18 : 100); i++)
            {
                operations.Add(new WriteOperationParameter { Area = PlcArea.DB, Offset = i, Type = typeof(bool), Args = new int[] { 1, db }, Data = false });
                readOperations.Add(new ReadOperationParameter { Area = PlcArea.DB, Offset = i, Type = typeof(bool), Args = new int[] { 1, db } });
            }

            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);


            //Reset to false
            client.WriteAny(operations);
            var result = client.ReadAny(readOperations).ToList();
            for (int i = 0; i < operations.Count; i++)
            {
                operations[i].Data = !((bool)result[i]);
            }

            client.WriteAny(operations);
            result = client.ReadAny(readOperations).ToList();
            for (int i = 0; i < operations.Count; i++)
            {
                Assert.Equal((bool)operations[i].Data, ((bool)result[i]));
            }


            operations.RemoveAt(0);
            client.WriteAny(operations);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestReadWriteAny()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);


            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public async void TestReadWriteAnyAsync()
        {
            var client = new Dacs7Client(_loggerFactory);
            await client.ConnectAsync(ConnectionString);
            Assert.True(client.IsConnected);

            await client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = await client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = await client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = await client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = await client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = await client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = await client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = await client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            await client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = await client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr })as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            await client.DisconnectAsync();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyDoubleTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            var client2 = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);
            client2.Connect(ConnectionString);
            Assert.True(client2.IsConnected);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.Disconnect();
            Assert.False(client.IsConnected);

            client2.Disconnect();
            Assert.False(client2.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyAsyncDoubleTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            var client2 = new Dacs7Client(_loggerFactory);
            var t = new Task[2];
            t[0] = client.ConnectAsync(ConnectionString);
            t[1] = client2.ConnectAsync(ConnectionString);

            Task.WaitAll(t);

            Assert.True(client.IsConnected);
            Assert.True(client2.IsConnected);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x05, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            var t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            var bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x00, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, true, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, false, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = client.DisconnectAsync();
            t[1] = client2.DisconnectAsync();

            Task.WaitAll(t);

            Assert.False(client.IsConnected);
            Assert.False(client2.IsConnected);
        }

        [Fact]
        public async Task ReadBlockInfoAsyncTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            await client.ConnectAsync(ConnectionString);
            Assert.True(client.IsConnected);


            await client.ConnectAsync(ConnectionString);
            var blkInfo = await client.ReadBlockInfoAsync(PlcBlockType.Db, TestDbNr);
            Assert.Equal(TestDbNr, blkInfo.BlockNumber);


            await client.DisconnectAsync();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadWriteMoreThanOnePduTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0xFF;

            var sw = new Stopwatch();
            sw.Start();
            client.WriteAny(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

            client.Disconnect();
            Assert.False(client.IsConnected);

        }

        [Fact]
        public void ReadWriteMoreThanOnePduParallelTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            //client.OnLogEntry += Console.WriteLine;
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0x01;

            var sw = new Stopwatch();

            sw.Start();
            client.WriteAnyParallel(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = client.ReadAnyParallel(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

            client.Disconnect();
            Assert.False(client.IsConnected);

        }

        [Fact]
        public void ReadNotExistingItem()
        {
            var client = new Dacs7Client(_loggerFactory);
            //client.OnLogEntry += Console.WriteLine;
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = ushort.MaxValue;
            Assert.Throws<Dacs7ContentException>(() => client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void Convertest()
        {
            Single s = (Single)5.4;

            var b = s.SetNoSwap();
            var c = b.GetNoSwap<Single>();


            var d = s.SetSwap();
            var e = d.GetSwap<Single>();
        }

        [Fact]
        public void GetPlcTimeTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetPlcTime();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void SetPlcTimeTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.SetPlcTime(new DateTime(2017, 10, 22, 18, 0, 0, 10));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void CreatePlcStopTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.StopPlc();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void CreatePlcStartTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.StartPlc();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void CreatePlcColdStartTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.StartPlc(true);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


        [Fact]
        public void GetPlcStateTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var state = client.GetPlcState();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


        [Fact]
        public void CopyRamToRomTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.CopyRamToRom();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void CompressMemoryTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.CompressMemory();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }
    }

#endif
}
