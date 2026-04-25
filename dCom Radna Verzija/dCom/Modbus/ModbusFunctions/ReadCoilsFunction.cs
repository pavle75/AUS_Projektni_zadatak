using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction // Digitalni izlaz
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
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

            ParseCoilData(response, result);
            return result;
        }

        private void WriteShortToBuffer(byte[] buffer, int offset, short value)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)),
                0, buffer, offset, 2
            );
        }

        private void ParseCoilData(byte[] response, Dictionary<Tuple<PointType, ushort>, ushort> result)
        {
            var readParams = (ModbusReadCommandParameters)CommandParameters;
            int byteCount = response[8];
            int totalPoints = readParams.Quantity;
            ushort address = readParams.StartAddress;
            int parsed = 0;

            for (int byteIndex = 0; byteIndex < byteCount; byteIndex++)
            {
                byte currentByte = response[9 + byteIndex];

                for (int bit = 0; bit < 8; bit++)
                {
                    ushort coilValue = (ushort)(currentByte & 1);
                    currentByte >>= 1;

                    var key = Tuple.Create(PointType.DIGITAL_OUTPUT, address);
                    result.Add(key, coilValue);

                    address++;
                    parsed++;

                    if (parsed == totalPoints) return;
                }
            }
        }
    }
}