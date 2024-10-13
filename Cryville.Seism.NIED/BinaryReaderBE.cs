using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Cryville.Seism.NIED {
	sealed class BinaryReaderBE : BinaryReader {
		public BinaryReaderBE(Stream input) : base(input) { }
		public BinaryReaderBE(Stream input, Encoding encoding) : base(input, encoding) { }
		public BinaryReaderBE(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

		readonly byte[] _buffer = new byte[8];
		new void FillBuffer(int num) {
			for (int i = 0; i < num;) {
				int readNum = BaseStream.Read(_buffer, i, num - i);
				if (readNum == 0) throw new EndOfStreamException();
				i += readNum;
			}
		}

		public override short ReadInt16() {
			FillBuffer(sizeof(short));
			return BinaryPrimitives.ReadInt16BigEndian(_buffer);
		}
		public override int ReadInt32() {
			FillBuffer(sizeof(int));
			return BinaryPrimitives.ReadInt32BigEndian(_buffer);
		}
		public override long ReadInt64() {
			FillBuffer(sizeof(long));
			return BinaryPrimitives.ReadInt64BigEndian(_buffer);
		}
		public override ushort ReadUInt16() {
			FillBuffer(sizeof(ushort));
			return BinaryPrimitives.ReadUInt16BigEndian(_buffer);
		}
		public override uint ReadUInt32() {
			FillBuffer(sizeof(uint));
			return BinaryPrimitives.ReadUInt32BigEndian(_buffer);
		}
		public override ulong ReadUInt64() {
			FillBuffer(sizeof(ulong));
			return BinaryPrimitives.ReadUInt64BigEndian(_buffer);
		}
		public override float ReadSingle() {
			FillBuffer(sizeof(float));
			int r = MemoryMarshal.Read<int>(_buffer);
			if (BitConverter.IsLittleEndian)
				r = BinaryPrimitives.ReverseEndianness(r);
			return BitConverter.Int32BitsToSingle(r);
		}
		public override double ReadDouble() {
			FillBuffer(sizeof(double));
			long r = MemoryMarshal.Read<long>(_buffer);
			if (BitConverter.IsLittleEndian)
				r = BinaryPrimitives.ReverseEndianness(r);
			return BitConverter.Int64BitsToDouble(r);
		}
	}
}
