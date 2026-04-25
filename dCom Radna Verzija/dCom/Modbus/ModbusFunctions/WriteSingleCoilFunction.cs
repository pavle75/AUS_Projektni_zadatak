using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction // Digitalni izlaz
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            var writeParams = (ModbusWriteCommandParameters)CommandParameters;
            byte[] request = new byte[12];

            WriteShortToBuffer(request, 0, (short)CommandParameters.TransactionId);
            WriteShortToBuffer(request, 2, (short)CommandParameters.ProtocolId);
            WriteShortToBuffer(request, 4, (short)CommandParameters.Length);

            request[6] = CommandParameters.UnitId;
            request[7] = CommandParameters.FunctionCode;

            WriteShortToBuffer(request, 8, (short)writeParams.OutputAddress);
            WriteShortToBuffer(request, 10, (short)writeParams.Value);

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

            ParseWriteResponse(response, result);
            return result;
        }

        private void WriteShortToBuffer(byte[] buffer, int offset, short value)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)),
                0, buffer, offset, 2
            );
        }

        private void ParseWriteResponse(byte[] response, Dictionary<Tuple<PointType, ushort>, ushort> result)
        {
            ushort address = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 8));
            ushort value = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 10));

            var key = Tuple.Create(PointType.DIGITAL_OUTPUT, address);
            result.Add(key, value);
        }
    }
}