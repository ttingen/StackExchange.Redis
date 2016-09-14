using Channels;
using Channels.Text.Primitives;
using System;
using System.Numerics;

namespace RedisCore
{
    struct RawResult
    {
        public override string ToString()
        {
            switch (Type)
            {
                case ResultType.SimpleString:
                case ResultType.Integer:
                case ResultType.Error:
                    return $"{Type}: {Buffer.GetAsciiString()}";
                case ResultType.BulkString:
                    return $"{Type}: {Buffer.Length} bytes";
                case ResultType.MultiBulk:
                    return $"{Type}: {Items.Length} items";
                default:
                    return "(unknown)";
            }
        }
        internal string GetAsciiString() => Buffer.Length == 0 ? "" : Buffer.GetAsciiString();
        internal string GetUtf8String() => Buffer.Length == 0 ? "" : Buffer.GetUtf8String();
        // runs on uv thread
        public static unsafe bool TryParse(ref ReadableBuffer buffer, out RawResult result)
        {
            if (buffer.Length < 3)
            {
                result = default(RawResult);
                return false;
            }
            byte resultType = (byte)buffer.Peek();
            switch (resultType)
            {
                case (byte)'+': // simple string
                    return TryReadLineTerminatedString(ResultType.SimpleString, ref buffer, out result);
                case (byte)'-': // error
                    return TryReadLineTerminatedString(ResultType.Error, ref buffer, out result);
                case (byte)':': // integer
                    return TryReadLineTerminatedString(ResultType.Integer, ref buffer, out result);
                case (byte)'$': // bulk string
                    return TryReadBulkString(ref buffer, out result);
                case (byte)'*': // array
                    throw new NotImplementedException();
                //return ReadArray(buffer, ref offset, ref count);
                default:
                    throw new InvalidOperationException("Unexpected response prefix: " + (char)resultType);
            }
        }

        private static bool TryReadLineTerminatedString(ResultType resultType, ref ReadableBuffer buffer, out RawResult result)
        {
            ReadableBuffer before, after;
            if (TryFindCRLF(ref buffer, out before, out after))
            {
                result = new RawResult(resultType, before.Slice(1));
                buffer = after;
                return true;
            }
            result = default(RawResult);
            return false;
        }
        static bool TryFindCRLF(ref ReadableBuffer buffer, out ReadableBuffer before, out ReadableBuffer after)
        {
            ReadCursor index;
            if(buffer.Length >= 2 && buffer.TrySliceTo((byte)'\r', (byte)'\n', out before, out index))
            {
                after = buffer.Slice(index).Slice(2);
                return true;
            }
            before = after = default(ReadableBuffer);
            return false;
        }

        static readonly byte[] MinusOne = { (byte)'-', (byte)'1' };
        private static bool TryReadBulkString(ref ReadableBuffer buffer, out RawResult result)
        {
            ReadableBuffer before, after;
            if (!TryFindCRLF(ref buffer, out before, out after))
            {
                result = default(RawResult);
                return false;
            }
            before = before.Slice(1);
            if (before.Peek() == (byte)'-')
            {
                if (before.Equals(MinusOne))
                {
                    throw new NotImplementedException("Null bulk string");
                    // result = 
                    // return true;
                }
                throw new InvalidOperationException("Protocol exception; negative length not expected except -1");
            }
            var ulen = ReadableBufferExtensions.GetUInt64(before);
            if (ulen > int.MaxValue) throw new OverflowException();
            var len = (int)ulen;

            // check that the final CRLF is well formed
            if (after.Length < len + 2)
            {
                // not enough data
                result = default(RawResult);
                return false;
            }

            if(!after.Slice(len, 2).Equals(RedisConnection.CRLF))
            { 
                throw new InvalidOperationException("Protocol exception; expected crlf after bulk string");
            }

            // all looks good, yay!
            result = new RawResult(ResultType.BulkString, after.Slice(0, len));
            buffer = after.Slice(len + 2);
            return true;
        }

        public RawResult(RawResult[] items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Type = ResultType.MultiBulk;
            Items = items;
            Buffer = default(ReadableBuffer);
        }
        private RawResult(ResultType resultType, ReadableBuffer buffer)
        {
            switch (resultType)
            {
                case ResultType.SimpleString:
                case ResultType.Error:
                case ResultType.Integer:
                case ResultType.BulkString:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultType));
            }
            Type = resultType;
            Buffer = buffer;
            Items = null;
        }
        public readonly ResultType Type;
        public
#if DEBUG
            readonly
#endif
            ReadableBuffer Buffer;
        private readonly RawResult[] Items;
    }
}
