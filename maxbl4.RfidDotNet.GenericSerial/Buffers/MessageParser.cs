using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using maxbl4.Infrastructure.Extensions.ByteArrayExt;
using maxbl4.RfidDotNet.GenericSerial.Model;
using maxbl4.RfidDotNet.GenericSerial.Packets;
using Serilog;

namespace maxbl4.RfidDotNet.GenericSerial.Buffers
{
    public class MessageParser
    {
        private static readonly ILogger Logger = Log.ForContext<MessageParser>();
        public static async Task<PacketResult> ReadPacket(Stream stream, Stopwatch sw = null)
        {
            byte[] responseBuffer = new byte[100 + 5];
            var read = 0;
            Logger.Warning("ReadPacket read packet length (1 byte)");
            if (sw == null)
                sw = Stopwatch.StartNew();
            //var smallBuf = new byte[1];
            //var read = stream.Read(smallBuf, 0, smallBuf.Length);
            //var packetLength = smallBuf[0];
            //Logger.Debug("ReadPacket packetLength={packetLength}", packetLength);
            //if (read < 1)
            //{
            //    Logger.Debug("ReadPacket Could not read packet length, return Timeout");
            //    return PacketResult.Timeout();
            //}
            var packetLength = responseBuffer.Length;
            var totalRead = 0;
            var data = new byte[packetLength + 1];
            //data[0] = packetLength;
            while (totalRead < packetLength)
            {
                read = stream.Read(data, totalRead + 1, packetLength - totalRead);
                if (read == 0)
                {
                    Logger.Debug("ReadPacket Could not complete reading of packet");
                    return PacketResult.WrongSize();
                }

                totalRead += read;
            }
            sw.Stop();

            if (!Crc16.CheckCrc16(data, packetLength))
            {
                Logger.Warning("ReadPacket CRC check failed.");
                return PacketResult.WrongCrc();
            }
            
            Logger.Warning($"ReadPacket success: {data.ToHexString(" ")}");
            return PacketResult.FromData(data, sw.Elapsed);
        }

        public static bool ShouldReadMore(ResponseDataPacket responseDataPacket)
        {
            bool response = false;
            switch (responseDataPacket.Command)
            {
                case ReaderCommand.TagInventory:
                    response = responseDataPacket.Status == ResponseStatusCode.InventoryMoreFramesPending
                               || responseDataPacket.Status == ResponseStatusCode.InventoryStatisticsDelivery;
                    break;
                case ReaderCommand.RealtimeInventoryResponse:
                    response = true;
                    break;
            }
            Logger.Warning("{ShouldReadMore}", response);
            return response;
        }
    }
}