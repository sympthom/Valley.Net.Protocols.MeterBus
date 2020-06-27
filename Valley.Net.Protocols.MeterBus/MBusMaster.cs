using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valley.Net.Bindings;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus
{
    public sealed class MBusMaster
    {
        private readonly IEndPointBinding _binding;

        public event EventHandler<MeterEventArgs> Meter;

        public MBusMaster(IEndPointBinding binding)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        /// <summary>
        /// Initialization of slave.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> Ping(byte address, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case AckFrame frame:
                        {
                            resetEvent.Set();
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));

                return resetEvent.WaitOne(timeout);
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        public async Task SetMeterAddress(byte address, byte newaddress)
        {
            var length = (byte)0x03;

            var data = new byte[length];
            data[0] = 0x01;
            data[1] = 0x7a;
            data[2] = newaddress;

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.DATA_SEND, address, data, length));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        //public async Task SetBaudRate(byte address, byte bauderate)
        //{
        //    var payload = new byte[1];
        //    payload[0] = bauderate;

        //    await _endpoint.Send(new LongMeterBusPackage(ControlCommand.SND_UD, address, payload));
        //}

        public async Task SetId(byte address)
        {
            var length = (byte)0x06;

            var data = new byte[length];
            data[0] = 0x0c;
            data[1] = 0x79;
            data[2] = 0x01;
            data[3] = 0x02;
            data[4] = 0x03;
            data[5] = 0x04;

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.DATA_SEND, address, data, length));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        public async Task ResetApplication(byte address)
        {
            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ControlFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.APPLICATION_RESET, address));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        /// <summary>
        /// Request for Class 1 Data.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Packet> RequestAlarm(byte address, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            Packet packet = null;

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case FixedDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                    case VariableDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.REQ_UD1, address));

                if (!resetEvent.WaitOne(timeout))
                    throw new TimeoutException();
            }
            finally
            {
                await _binding.DisconnectAsync();
            }

            return packet;
        }

        /// <summary>
        /// Request for Class 2 Data.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Packet> RequestData(byte address, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            Packet packet = null;

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case FixedDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                    case VariableDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.REQ_UD2, address)); // request data

                if (!resetEvent.WaitOne(timeout))
                    throw new TimeoutException();
            }
            finally
            {
                await _binding.DisconnectAsync();
            }

            return packet;
        }

        public async Task Initialize(byte address)
        {
            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        public async Task Scan(byte[] addresses, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case AckFrame frame:
                        {
                            resetEvent.Set();
                        }
                        break;
                    case LongFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, new MeterEventArgs(frame.Address));
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                foreach (var address in addresses)
                {
                    await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));

                    if (!resetEvent.WaitOne(timeout))
                        continue;

                    await _binding.SendAsync(new ShortFrame(0x7b, address)); //request data.

                    if (!resetEvent.WaitOne(timeout))
                        throw new TimeoutException();
                }
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        /// <summary>
        /// Method for sending data to an MBus device.
        /// </summary>
        /// <param name="address">Primary Address for device that will receive the data that is sent.</param>
        /// <param name="data">The data to be sent.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns></returns>
        public async Task SendData(byte address, byte[] data, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            var length = (byte)(data.Length);

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case AckFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, new MeterEventArgs());
                        }
                        break;
                    case LongFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, new MeterEventArgs(frame.Address));
                        }
                        break;
                }
            };


            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.DATA_SEND, address, data, length));

                if (!resetEvent.WaitOne(timeout))
                    throw new TimeoutException();

            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        /// <summary>
        /// Method for MBus device selection when performing communication using devices Secondary Address (Manufacturer Id).
        /// </summary>
        /// <param name="address">Primary Address for selecting device to perform Secondary Address operation upon. Should be 0xfd (253) for proper operation.</param>
        /// <param name="data">Secondary Address (Identification No + Manufacturer Id + Device Id) for device selection. All fields are represented as hex-string. 
        /// When searching for a device with a known Identification No, use Manufacturer Id = 0xFFFF, Version = 0xFF and Device Id = 0xFF.
        /// 
        /// Secondary address is supposed to be stated in Little-Endian byte order for each part.</param>
        /// 
        /// Eg when selecting 94231245 0444 01 02 data have to be encoded like [ 0x45 0x12 0x23 0x94 0x44 0x04 0x01 0x02 ].
        /// <param name="endian">Set to true to use Big-Endian byte order and let the method perform an internal re-ordering of bytes in data.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns></returns>
        public async Task SelectSlave(byte address, byte[] data, bool endian, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            if (data.Length < 4)
            {
                //left pad 0x00 up til 4 bytes
            }

            if (data.Length > 4 && data.Length < 8)
            {
                // right pad 0xFF up til 8 bytes

            }

            var length = (byte)(data.Length);

            if (endian)
            {
                byte[] _data = new byte[length];
                _data[0] = data[3];
                _data[1] = data[2];
                _data[2] = data[1];
                _data[3] = data[0];
                _data[4] = data[5];
                _data[5] = data[4];
                _data[6] = data[6];
                _data[7] = data[7];

                data = _data;
            }

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case AckFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, (MeterEventArgs)new EventArgs());
                        }
                        break;
                    case LongFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, new MeterEventArgs(frame.Address));
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.SELECT_SLAVE, address, data, length));

                if (!resetEvent.WaitOne(timeout))
                    throw new TimeoutException();

            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }
    }
}
