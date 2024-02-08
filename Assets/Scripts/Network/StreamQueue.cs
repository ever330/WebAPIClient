using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            /// todo
            /// ��Ŷ ���� ������ �պκ����� �߶� �����;��ϴ� ������ ���� �Ǻ� �� �ش� ����ŭ�� ������ ���� ���ֵ��� ��������.

            // ��Ŷ ��� ũ�� Ȯ�� (����� 4����Ʈ�� ����)
            int headerSize = 4;
            int totalSize = 6;

            if (dataCnt < headerSize)
            {
                // ���� �����Ͱ� ��� ũ�⺸�� ������ ó�� �Ұ�
                // ���� ó�� �Ǵ� �ٸ� ������� ó�� �ʿ�
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
                // ���ۿ� ��ü ��Ŷ�� ���� ��� ó�� �Ұ�
                // ���� ó�� �Ǵ� �ٸ� ������� ó�� �ʿ�
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
