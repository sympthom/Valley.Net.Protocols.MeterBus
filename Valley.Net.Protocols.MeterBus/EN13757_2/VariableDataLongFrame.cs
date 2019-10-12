using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VariableDataLongFrame : LongFrame
    {
        public byte[] Manufr { get; } = new byte[2];

        public byte Version { get; }

        public byte[] Signature { get; } = new byte[2];

        public IList<Part> Parts { get; private set; } = new List<Part>();

        public VariableDataLongFrame(byte control, byte controlInformation, byte address, byte[] data, byte length) : base(control, controlInformation, address, data, length)
        {
            if ((ControlInformation)controlInformation != ControlInformation.RESP_VARIABLE)
                throw new InvalidDataException();

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.Read(IdentificationNo, 0, IdentificationNo.Length) != IdentificationNo.Length)
                    throw new InvalidDataException();

                if (reader.Read(Manufr, 0, Manufr.Length) != Manufr.Length)
                    throw new InvalidDataException();

                Version = reader.ReadByte();
                DeviceType = reader.ReadByte();
                TransmissionCounter = reader.ReadByte();
                Status = reader.ReadByte();

                if (reader.Read(Signature, 0, Signature.Length) != Signature.Length)
                    throw new InvalidDataException();

                while (!reader.EOF())
                {
                    var type = reader.ReadByte();

                    switch ((VariableDataRecordType)type)
                    {
                        case VariableDataRecordType.MBUS_DIB_DIF_IDLE_FILLER:
                        case VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW:
                            {
                                Parts.Add(new DIF(type));
                            }
                            break;
                        case VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC:
                            {
                                Parts.Add(new ManufactureSpecific(type, reader.ReadAllBytes()));
                            }
                            break;
                        default:
                            {
                                var dif = new DIF(type);
                                Parts.Add(dif);

                                var extension = dif.Extension;

                                for (int i = 0; extension; i++)
                                {
                                    if (i > 10)
                                        throw new InvalidDataException();

                                    var dife = new DIFE(reader.ReadByte());
                                    Parts.Add(dife);

                                    extension = dife.Extension;
                                }

                                var vif = new VIF(reader.ReadByte());
                                Parts.Add(vif);

                                extension = vif.Extension;

                                for (int i = 0; extension; i++)
                                {
                                    if (i > 10)
                                        throw new InvalidDataException();

                                    switch (vif.Type)
                                    {
                                        case VIF.VifType.PrimaryVIF:
                                        case VIF.VifType.PlainTextVIF:
                                        case VIF.VifType.AnyVIF:
                                        case VIF.VifType.ManufacturerSpecific:
                                            {
                                                var vife = new VIFE(reader.ReadByte());
                                                Parts.Add(vife);

                                                extension = vife.Extension;
                                            }
                                            break;
                                        case VIF.VifType.LinearVIFExtensionFD:
                                            {
                                                var vife = new VIFE_FD(reader.ReadByte());
                                                Parts.Add(vife);

                                                extension = vife.Extension;
                                            }
                                            break;
                                        case VIF.VifType.LinearVIFExtensionFB:
                                            {
                                                var vife = new VIFE_FB(reader.ReadByte());
                                                Parts.Add(vife);

                                                extension = vife.Extension;
                                            }
                                            break;
                                    }
                                }

                                var valueLength = dif.DataType == DataTypes._variable_length ? -1 : LenghtsInBitsTable[dif.DataType] / 8;

                                var value = new Value(valueLength > 0 ? reader.ReadBytes(valueLength) : null);

                                Parts.Add(value);
                            }
                            break;
                    }
                }
            }
        }
    }
}
