using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class StreamQueue
    {
        private readonly object lockObject = new object();

        public int dataCnt;

        private byte[] buf;
        private int readIndex;
        private int writeIndex;
        private int bufSize;
        private int emptySize;

        public StreamQueue(int size)
        {
            buf = new byte[size];
            readIndex = 0;
            writeIndex = 0;
            dataCnt = 0;
            bufSize = size;
            emptySize = size;
        }

        public bool WriteData(byte[] data, int dataLength)
        {
            lock (lockObject)
            {
                if (emptySize < dataLength)
                    return false;

                if (bufSize - writeIndex >= dataLength)
                {
                    Buffer.BlockCopy(data, 0, buf, writeIndex, dataLength);
                }
                else
                {
                    Buffer.BlockCopy(data, 0, buf, writeIndex, bufSize - writeIndex);
                    Buffer.BlockCopy(data, bufSize - writeIndex, buf, 0, dataLength - (bufSize - writeIndex));
                }

                writeIndex += dataLength;
                if (writeIndex >= bufSize)
                    writeIndex -= bufSize;

                dataCnt += dataLength;
                emptySize -= dataLength;
            }

            return true;
        }

        public byte[] ReadData()
        {
            lock (lockObject)
            {
                // 패킷 헤더 크기 확인 (현재는 4바이트로 가정)
                int headerSize = 4;
                int totalSize = 6;

                if (dataCnt < headerSize)
                {
                    // 읽을 데이터가 헤더 크기보다 작으면 처리 불가
                    // 예외 처리 또는 다른 방식으로 처리 필요
                    return null;
                }

                byte[] retData = new byte[headerSize];
                if (bufSize - readIndex >= headerSize)
                {
                    Buffer.BlockCopy(buf, readIndex, retData, 0, headerSize);
                }
                else
                {
                    Buffer.BlockCopy(buf, readIndex, retData, 0, bufSize - readIndex);
                    Buffer.BlockCopy(buf, 0, retData, bufSize - readIndex, headerSize - (bufSize - readIndex));
                }

                int pacSize = BitConverter.ToInt32(retData, 0);
                totalSize += pacSize;

                if (dataCnt < totalSize)
                {
                    // 버퍼에 전체 패킷이 없는 경우 처리 불가
                    // 예외 처리 또는 다른 방식으로 처리 필요
                    return null;
                }

                byte[] packetData = new byte[totalSize];

                if (bufSize - readIndex >= totalSize)
                {
                    Buffer.BlockCopy(buf, readIndex, packetData, 0, totalSize);
                }
                else
                {
                    Buffer.BlockCopy(buf, readIndex, packetData, 0, bufSize - readIndex);
                    Buffer.BlockCopy(buf, 0, packetData, bufSize - readIndex, totalSize - (bufSize - readIndex));
                }
                readIndex += totalSize;

                if (readIndex >= bufSize)
                    readIndex -= bufSize;

                dataCnt -= totalSize;
                emptySize += totalSize;

                return packetData;
            }
        }
    }
}
