using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read input registers functions/requests.
    /// </summary>
    public class ReadInputRegistersFunction : ModbusFunction // Čitanje analognog ulaza
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadInputRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadInputRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            var readParams = (ModbusReadCommandParameters)CommandParameters;
            byte[] request = new byte[12];

            WriteShortToBuffer(request, 0, (short)CommandParameters.TransactionId);
            WriteShortToBuffer(request, 2, (short)CommandParameters.ProtocolId);
            WriteShortToBuffer(request, 4, (short)CommandParameters.Length);

            request[6] = CommandParameters.UnitId;
            request[7] = CommandParameters.FunctionCode;

            WriteShortToBuffer(request, 8, (short)readParams.StartAddress);
            WriteShortToBuffer(request, 10, (short)readParams.Quantity);

            return request;
        }

        /// <inheritdoc/>
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            bool isError = response[7] == CommandParameters.FunctionCode + 0x80;
            if (isError)
            {
                HandeException(response[8]);
                return result;
            }

            ParseRegisterData(response, result);
            return result;
        }

        // --- Helpers ---

        private void WriteShortToBuffer(byte[] buffer, int offset, short value)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)),
                0, buffer, offset, 2
            );
        }

        private void ParseRegisterData(byte[] response, Dictionary<Tuple<PointType, ushort>, ushort> result)
        {
            int byteCount = response[8]; // Ukupan broj bajtova podataka (svaki registar = 2 bajta)
            ushort address = ((ModbusReadCommandParameters)CommandParameters).StartAddress;

            for (int i = 0; i < byteCount; i += 2)
            {
                ushort rawValue = BitConverter.ToUInt16(response, 9 + i);
                ushort hostValue = (ushort)IPAddress.NetworkToHostOrder((short)rawValue);

                var key = Tuple.Create(PointType.ANALOG_INPUT, address);
                result.Add(key, hostValue);

                address++;
            }
        }
    }
}