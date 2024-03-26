using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PacketBase
{
    public static readonly int HEADERSIZE = 4;
}

public enum PacketId : short
{
    ClientConnect = 1000,

    ReqCreateRoom = 1100,
    ResCreateRoom = 1101,
    ReqEnterRoom = 1102,
    ResEnterRoom = 1103,
    ReqRoomPlayers = 1104,
    ResRoomPlayers = 1105,

    S2CNewPlayer = 1300,

    C2SPlayerInfo = 1500,
    S2CPlayerInfo = 1501,

    C2SEchoChat = 1600,
    S2CEchoChat = 1601,

    C2SVoice = 1700,
    S2CVoice = 1701,

    ClientDisconnect = 1900
}

public class PlayerInfo
{
    public string NickName;
    public float PosX;
    public float PosY;
    public float PosZ;
    public float ForX;
    public float ForY;
    public float ForZ;
}

public class PlayerVoice
{
    public string NickName;
    public float[] VoiceData;
}