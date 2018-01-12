﻿using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Double))]
    internal class DoubleConverter : IPacketConverter, IPacketConverter<Double>
    {
        public int Length => sizeof(Double);

        public byte[] GetBytes(Double value) => BitConverter.GetBytes(value);

        public Double GetValue(byte[] buffer, int offset, int length) => BitConverter.ToDouble(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Double)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToDouble(buffer, offset);
    }
}