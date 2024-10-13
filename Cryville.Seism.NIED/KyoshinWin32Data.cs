using Cryville.Common.Compat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Cryville.Seism.NIED {
	/// <summary>
	/// <see href="https://www.kyoshin.bosai.go.jp/kyoshin/man/knetbinary.html">Kyoshin WIN32 data (強震WIN32形式データ)</see> defined by NIED.
	/// </summary>
	public class KyoshinWin32Data {
		/// <summary>
		/// The organization.
		/// </summary>
		public KyoshinOrganization Organization { get; private set; }
		/// <summary>
		/// The network.
		/// </summary>
		public KyoshinNetwork Network { get; private set; }
		/// <summary>
		/// The station ID.
		/// </summary>
		public ushort StationId { get; private set; }

		/// <summary>
		/// The station information.
		/// </summary>
		public KyoshinStationInfo? StationInfo { get; private set; }
		/// <summary>
		/// The hypocenter information.
		/// </summary>
		/// <remarks>
		/// <see langword="null" /> for instant data.
		/// </remarks>
		public KyoshinHypocenterInfo? HypocenterInfo { get; private set; }
		/// <summary>
		/// Second block data.
		/// </summary>
		public IReadOnlyList<KyoshinSecond> Seconds { get; private set; }

		/// <summary>
		/// Reads Kyoshin WIN32 data from a stream of the K-NET binary format.
		/// </summary>
		/// <param name="stream">The input stream.</param>
		/// <returns>The Kyoshin WIN32 data read.</returns>
		public static KyoshinWin32Data FromStream(Stream stream) {
			using var reader = new BinaryReaderBE(stream, Encoding.ASCII, true);
			return new(reader);
		}

		/// <summary>
		/// Creates an instance of the <see cref="KyoshinWin32Data" /> class from a big-endian binary reader that is reading a stream of the K-NET binary format.
		/// </summary>
		/// <param name="reader">The big-endian binary reader that is reading the input data.</param>
		/// <exception cref="FormatException">The data is not in the K-NET binary format.</exception>
		/// <remarks>To construct a big-endian binary reader automatically, call <see cref="FromStream(Stream)" /> instead.</remarks>
		public KyoshinWin32Data(BinaryReader reader) {
			ThrowHelper.ThrowIfNull(reader, nameof(reader));
			if (reader.ReadByte() != 0x0a) throw new FormatException("Invalid format ID.");
			if (reader.ReadByte() != 0x02) throw new FormatException("Invalid format sub ID.");
			reader.ReadByte();
			reader.ReadByte();

			if (reader.ReadByte() != 0x0c) throw new FormatException("Invalid info block.");
			reader.ReadByte();
			reader.ReadByte();
			reader.ReadByte();
			Organization = (KyoshinOrganization)reader.ReadByte();
			Network = (KyoshinNetwork)reader.ReadByte();
			StationId = reader.ReadUInt16();
			uint len = reader.ReadUInt32();
			uint readLen = 0;
			while (readLen < len) {
				ushort type = reader.ReadUInt16();
				readLen += reader.ReadUInt16() + 4u;
				switch (type) {
					case 0xe000: StationInfo = new(reader, false); break;
					case 0xe001: StationInfo = new(reader, true); break;
					case 0xe020: HypocenterInfo = new(reader); break;
					default: break;
				}
			}
			List<KyoshinSecond> seconds = [];
			while (reader.PeekChar() != -1) {
				seconds.Add(new(reader));
			}
			Seconds = seconds;
		}
	}

	/// <summary>
	/// Organizations defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinOrganization {
		/// <summary>
		/// Unknown.
		/// </summary>
		Unknown = 0x00,
		/// <summary>
		/// NIED (防災科研).
		/// </summary>
		NIED = 0x01,
	}
	/// <summary>
	/// Networks defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinNetwork {
		/// <summary>
		/// Unknown.
		/// </summary>
		Unknown = 0x00,
		/// <summary>
		/// K-NET.
		/// </summary>
		KNET = 0x10,
		/// <summary>
		/// KiK-net.
		/// </summary>
		KiKnet = 0x11,
	}

	/// <summary>
	/// Station information defined in the Kyoshin WIN32 format.
	/// </summary>
	public class KyoshinStationInfo {
		/// <summary>
		/// The latitude of the station (degrees), or <see langword="null" /> if undefined.
		/// </summary>
		/// <remarks>
		/// Positive (north) latitudes are as is, while negative (south) latitudes are added by 90. For example 36°S is indicated with <c>126</c>.
		/// </remarks>
		public ScaledNumber? Latitude { get; private set; }
		/// <summary>
		/// The longitude of the station (degrees), or <see langword="null" /> if undefined.
		/// </summary>
		/// <remarks>
		/// Positive (east) longitudes are as is, while negative (west) longitudes are added by 180. For example 140°W is indicated with <c>320</c>.
		/// </remarks>
		public ScaledNumber? Longitude { get; private set; }
		/// <summary>
		/// The altitude of the station (metres), or <see langword="null" /> if undefined.
		/// </summary>
		public ScaledNumber? Altitude { get; private set; }
		/// <summary>
		/// The altitude of the underground sensor (metres), or <see langword="null" /> if undefined or the station does not have an underground sensor.
		/// </summary>
		public ScaledNumber? UndergroundSensorAltitude { get; private set; }
		/// <summary>
		/// The code of the station.
		/// </summary>
		public string Code { get; private set; }
		/// <summary>
		/// The start time of the data (Japan Standard Time).
		/// </summary>
		public DateTime DataStartTime { get; private set; }
		/// <summary>
		/// The duration of the data.
		/// </summary>
		public TimeSpan MeasuringDuration { get; private set; }
		/// <summary>
		/// The fixing time of the last time (Japan Standard Time).
		/// </summary>
		public DateTime LastTimeFixingTime { get; private set; }
		/// <summary>
		/// The time fixing method.
		/// </summary>
		public KyoshinFixingMethod FixingMethod { get; private set; }
		/// <summary>
		/// The geodetic system of the coordinates of the station.
		/// </summary>
		public KyoshinGeodeticSystem GeodeticSystem { get; private set; }
		/// <summary>
		/// The type of the station.
		/// </summary>
		public KyoshinStationType StationType { get; private set; }
		/// <summary>
		/// The sampling rate (Hz).
		/// </summary>
		public ushort SampleRate { get; private set; }
		/// <summary>
		/// The count of components.
		/// </summary>
		public byte ComponentCount { get; private set; }
		/// <summary>
		/// A value that increments by one on each redeployment, or <c>0</c> if unset.
		/// </summary>
		public byte RedeployedFlag { get; private set; }
		/// <summary>
		/// Information about the components.
		/// </summary>
		public IReadOnlyList<KyoshinStationComponent> Components { get; private set; }

		internal KyoshinStationInfo(BinaryReader reader, bool hasUndergroundSensor) {
			Latitude = reader.ReadCoordinateBCD();
			Longitude = reader.ReadCoordinateBCD();
			Altitude = reader.ReadAltitudeBCD(5);
			if (hasUndergroundSensor) UndergroundSensorAltitude = reader.ReadAltitudeBCD(5);
			Code = new string(reader.ReadChars(12)).TrimEnd('\0');
			DataStartTime = reader.ReadTimeBCD();
			MeasuringDuration = TimeSpan.FromSeconds(reader.ReadUInt32() * 0.1);
			LastTimeFixingTime = reader.ReadTimeBCD();
			FixingMethod = (KyoshinFixingMethod)reader.ReadByte();
			GeodeticSystem = (KyoshinGeodeticSystem)reader.ReadByte();
			StationType = (KyoshinStationType)reader.ReadUInt16();
			SampleRate = reader.ReadUInt16();
			ComponentCount = reader.ReadByte();
			RedeployedFlag = reader.ReadByte();
			var components = new KyoshinStationComponent[ComponentCount];
			for (int i = 0; i < ComponentCount; i++) components[i] = new(reader);
			Components = components;
		}
	}
	/// <summary>
	/// Time fixing methods defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinFixingMethod {
		/// <summary>
		/// Unset.
		/// </summary>
		Unknown = 0x00,
		/// <summary>
		/// GPS.
		/// </summary>
		GPS = 0x01,
		/// <summary>
		/// Radio (ラジオ).
		/// </summary>
		Radio = 0x02,
		/// <summary>
		/// NTP.
		/// </summary>
		NTP = 0x03,
	}
	/// <summary>
	/// Geodetic systems defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinGeodeticSystem {
		/// <summary>
		/// Unset.
		/// </summary>
		Unknown = 0x00,
		/// <summary>
		/// The Japanese Datum.
		/// </summary>
		Japan = 0x01,
		/// <summary>
		/// The World Geodetic System.
		/// </summary>
		World = 0x02,
	}
	/// <summary>
	/// Station types defined in the Kyoshin WIN32 format.
	/// </summary>
	[SuppressMessage("Design", "CA1027", Justification = "Not flags")]
	public enum KyoshinStationType {
		/// <summary>
		/// Unset.
		/// </summary>
		Unknown = 0x0000,
		/// <summary>
		/// K-NET95.
		/// </summary>
		KNET95 = 0x0001,
		/// <summary>
		/// SMAC-MDU.
		/// </summary>
		SMACMDU = 0x0002,
		/// <summary>
		/// K-NET02.
		/// </summary>
		KNET02 = 0x0004,
		/// <summary>
		/// K-NET02A.
		/// </summary>
		KNET02A = 0x0005,
		/// <summary>
		/// K-NET11.
		/// </summary>
		KNET11 = 0x0006,
		/// <summary>
		/// K-NET11A.
		/// </summary>
		KNET11A = 0x0007,
		/// <summary>
		/// K-NET11B.
		/// </summary>
		KNET11B = 0x0008,
		/// <summary>
		/// K-NET11C.
		/// </summary>
		KNET11C = 0x0009,
		/// <summary>
		/// K-NET18.
		/// </summary>
		KNET18 = 0x000a,
		/// <summary>
		/// SMAC-K/MDK.
		/// </summary>
		SMACKMDK = 0x000b,
		/// <summary>
		/// KiK-net06a.
		/// </summary>
		KiKnet06a = 0x000d,
		/// <summary>
		/// KiK-net06.
		/// </summary>
		KiKnet06 = 0x000e,
		/// <summary>
		/// KiK-net11.
		/// </summary>
		KiKnet11 = 0x000f,
		/// <summary>
		/// KiK-net11A.
		/// </summary>
		KiKnet11A = 0x0010,
		/// <summary>
		/// KiK-net11B.
		/// </summary>
		KiKnet11B = 0x0011,
		/// <summary>
		/// KiK-net11C.
		/// </summary>
		KiKnet11C = 0x0012,
		/// <summary>
		/// KiK-net18.
		/// </summary>
		KiKnet18 = 0x0013,
	}

	/// <summary>
	/// Station component information defined in the Kyoshin WIN32 format.
	/// </summary>
	public class KyoshinStationComponent {
		/// <summary>
		/// The organization.
		/// </summary>
		public KyoshinOrganization Organization { get; private set; }
		/// <summary>
		/// The network.
		/// </summary>
		public KyoshinNetwork Network { get; private set; }
		/// <summary>
		/// The channel ID.
		/// </summary>
		/// <remarks>
		/// For K-NET stations:
		/// <list type="bullet">
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 1: the NS component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 2: the EW component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 3: the UD component.</item>
		/// </list>
		/// For KiK-net stations:
		/// <list type="bullet">
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 1: the NS1 component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 2: the EW1 component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 3: the UD1 component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 4: the NS2 component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 5: the EW2 component;</item>
		/// <item><see cref="KyoshinWin32Data.StationId" /> * 10 + 6: the UD2 component.</item>
		/// </list>
		/// NS1, EW1, and UD1 are underground sensors; NS2, EW2, and UD2 are surface sensors.
		/// </remarks>
		public ushort ChannelId { get; private set; }
		/// <summary>
		/// The numerator of the scale factor.
		/// </summary>
		public short ScaleFactorNumerator { get; private set; }
		/// <summary>
		/// The gain.
		/// </summary>
		public byte Gain { get; private set; }
		/// <summary>
		/// The unit.
		/// </summary>
		public KyoshinComponentUnit Unit { get; private set; }
		/// <summary>
		/// The denominator of the scale factor.
		/// </summary>
		public int ScaleFactorDenominator { get; private set; }
		/// <summary>
		/// The offset.
		/// </summary>
		public int Offset { get; private set; }
		/// <summary>
		/// The measurement range.
		/// </summary>
		public int MeasurementRange { get; private set; }

		internal KyoshinStationComponent(BinaryReader reader) {
			Organization = (KyoshinOrganization)reader.ReadByte();
			Network = (KyoshinNetwork)reader.ReadByte();
			ChannelId = reader.ReadUInt16();
			ScaleFactorNumerator = reader.ReadInt16();
			Gain = reader.ReadByte();
			Unit = new(reader.ReadByte());
			ScaleFactorDenominator = reader.ReadInt32();
			Offset = reader.ReadInt32();
			MeasurementRange = reader.ReadInt32();
		}
		/// <summary>
		/// Converts a raw digital value in the channel to its physical value (the unit is defined by <see cref="Unit" />).
		/// </summary>
		/// <param name="digitalValue"></param>
		/// <returns></returns>
		public double ToPhysicalValue(int digitalValue) => (double)ScaleFactorNumerator / ScaleFactorDenominator * (digitalValue - Offset) / Gain;
		/// <summary>
		/// The physical measurement range (the unit is defined by <see cref="Unit" />).
		/// </summary>
		public double PhysicalMeasurementRange => ToPhysicalValue(MeasurementRange);
	}
	/// <summary>
	/// The unit in the scale factor information defined in the Kyoshin WIN32 format.
	/// </summary>
	/// <param name="Scale">The scale of the unit, where a value N indicates a scale of 10^(-N).</param>
	/// <param name="Type">The type of the unit.</param>
	/// <remarks>
	/// A value N of <see cref="Scale" /> indicates a scale of 10^(-N). For example, if the unit is gal (i.e. 10^(-2) m/s/s), then <see cref="Scale" /> is <c>2</c> and <see cref="Type" /> is <see cref="KyoshinComponentUnitType.MetresPerSecondSquared" />.
	/// </remarks>
	public record struct KyoshinComponentUnit(byte Scale, KyoshinComponentUnitType Type) {
		internal KyoshinComponentUnit(byte serializedValue) : this((byte)(serializedValue >> 4), (KyoshinComponentUnitType)(serializedValue & 0x0f)) { }
		/// <summary>
		/// The real scale that is used in the multiplication.
		/// </summary>
		public readonly double PhysicalScale => Math.Pow(10, -Scale);
	}
	/// <summary>
	/// Unit types in the scale factor information defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinComponentUnitType {
		/// <summary>
		/// Reserved.
		/// </summary>
		None = 0,
		/// <summary>
		/// Metres (m).
		/// </summary>
		Metres = 0x1,
		/// <summary>
		/// Metres per second (m/s).
		/// </summary>
		MetresPerSecond = 0x2,
		/// <summary>
		/// Metres per second squared (m/s/s).
		/// </summary>
		MetresPerSecondSquared = 0x3,
	}

	/// <summary>
	/// Hypocenter information defined in the Kyoshin WIN32 format.
	/// </summary>
	public class KyoshinHypocenterInfo {
		/// <summary>
		/// The origin time of the earthquake (Japan Standard Time).
		/// </summary>
		public DateTime OriginTime { get; private set; }
		/// <summary>
		/// The latitude of the hypocenter (degrees), or <see langword="null" /> if undefined.
		/// </summary>
		/// <remarks>
		/// Positive (north) latitudes are as is, while negative (south) latitudes are added by 90. For example 36°S is indicated with <c>126</c>.
		/// </remarks>
		public ScaledNumber? Latitude { get; private set; }
		/// <summary>
		/// The longitude of the hypocenter (degrees), or <see langword="null" /> if undefined.
		/// </summary>
		/// <remarks>
		/// Positive (east) longitudes are as is, while negative (west) longitudes are added by 180. For example 140°W is indicated with <c>320</c>.
		/// </remarks>
		public ScaledNumber? Longitude { get; private set; }
		/// <summary>
		/// The depth of the hypocenter (kilometres), or <see langword="null" /> if undefined.
		/// </summary>
		public ScaledNumber? Depth { get; private set; }
		/// <summary>
		/// The magnitude of the earthquake, or <see langword="null" /> if undefined.
		/// </summary>
		public ScaledNumber? Magnitude { get; private set; }
		/// <summary>
		/// The geodetic system of the coordinates of the hypocenter.
		/// </summary>
		public KyoshinGeodeticSystem GeodeticSystem { get; private set; }
		/// <summary>
		/// The type of the hypocenter.
		/// </summary>
		public KyoshinHypocenterType HypocenterType { get; private set; }

		internal KyoshinHypocenterInfo(BinaryReader reader) {
			OriginTime = reader.ReadTimeBCD();
			Latitude = reader.ReadCoordinateBCD();
			Longitude = reader.ReadCoordinateBCD();
			Depth = reader.ReadAltitudeBCD(4);
			Magnitude = reader.ReadMagnitudeBCD();
			GeodeticSystem = (KyoshinGeodeticSystem)reader.ReadByte();
			HypocenterType = (KyoshinHypocenterType)reader.ReadByte();
			reader.ReadByte();
		}
	}
	/// <summary>
	/// Hypocenter types defined in the Kyoshin WIN32 format.
	/// </summary>
	public enum KyoshinHypocenterType {
		/// <summary>
		/// Unset.
		/// </summary>
		Unknown = 0x00,
		/// <summary>
		/// Preliminary hypocenter reported by JMA.
		/// </summary>
		JMAPreliminary = 0x01,
		/// <summary>
		/// Reviewed hypocenter reported by JMA.
		/// </summary>
		JMAReviewed = 0x02,
	}

	/// <summary>
	/// Second block data defined in the Kyoshin WIN32 format.
	/// </summary>
	public class KyoshinSecond {
		/// <summary>
		/// The time of the first data in this second block (Japan Standard Time).
		/// </summary>
		public DateTime SamplingStartTime { get; private set; }
		/// <summary>
		/// The duration covered by this second block.
		/// </summary>
		public TimeSpan FrameDuration { get; private set; }
		/// <summary>
		/// The data of the channels.
		/// </summary>
		public IReadOnlyList<KyoshinChannelData> Channels { get; private set; }

		internal KyoshinSecond(BinaryReader reader) {
			SamplingStartTime = reader.ReadTimeBCD();
			FrameDuration = TimeSpan.FromSeconds(reader.ReadUInt32() * 0.1f);
			uint len = reader.ReadUInt32();
			uint readLen = 0;
			List<KyoshinChannelData> channels = [];
			while (readLen < len) {
				var channel = new KyoshinChannelData(reader);
				channels.Add(channel);
				readLen += channel.InternalLength;
			}
			Channels = channels;
		}
	}

	/// <summary>
	/// Channel block data defined in the Kyoshin WIN32 format.
	/// </summary>
	public class KyoshinChannelData {
		internal uint InternalLength { get; private set; } = 10;
		/// <summary>
		/// The organization.
		/// </summary>
		public KyoshinOrganization Organization { get; private set; }
		/// <summary>
		/// The network.
		/// </summary>
		public KyoshinNetwork Network { get; private set; }
		/// <summary>
		/// The channel ID.
		/// </summary>
		/// <seealso cref="KyoshinStationComponent.ChannelId" />
		public ushort ChannelId { get; private set; }
		/// <summary>
		/// The raw digital data of the channel block.
		/// </summary>
		/// <remarks>
		/// Convert the data into physical values with <see cref="KyoshinStationComponent.ToPhysicalValue(int)" />.
		/// </remarks>
		public IReadOnlyList<int> Data { get; private set; }

		internal KyoshinChannelData(BinaryReader reader) {
			Organization = (KyoshinOrganization)reader.ReadByte();
			Network = (KyoshinNetwork)reader.ReadByte();
			ChannelId = reader.ReadUInt16();

			ushort sampleMeta = reader.ReadUInt16();
			int sampleCount = sampleMeta & 0x0fff;
			uint diffSampleCount = (uint)sampleCount - 1;
			List<int> data = new(sampleCount);
			int currentSampleValue = reader.ReadInt32();
			data.Add(currentSampleValue);
			switch (sampleMeta >> 12) {
				case 0:
					for (uint i = 0; i < diffSampleCount / 2; i++) {
						byte pack = reader.ReadByte();
						data.Add(currentSampleValue += UInt4ToInt4(pack >> 4));
						data.Add(currentSampleValue += UInt4ToInt4(pack & 0x0f));
					}
					InternalLength += diffSampleCount / 2;
					if (diffSampleCount % 2 != 0) {
						data.Add(currentSampleValue += UInt4ToInt4(reader.ReadByte() >> 4));
						InternalLength += 1;
					}
					break;
				case 1:
					for (uint i = 0; i < diffSampleCount; i++) {
						data.Add(currentSampleValue += reader.ReadSByte());
					}
					InternalLength += diffSampleCount;
					break;
				case 2:
					for (uint i = 0; i < diffSampleCount; i++) {
						data.Add(currentSampleValue += reader.ReadInt16());
					}
					InternalLength += diffSampleCount * 2;
					break;
				case 3:
					for (uint i = 0; i < diffSampleCount; i++) {
						data.Add(currentSampleValue += reader.ReadInt24BE());
					}
					InternalLength += diffSampleCount * 3;
					break;
				case 4:
					for (uint i = 0; i < diffSampleCount; i++) {
						data.Add(currentSampleValue += reader.ReadInt32());
					}
					InternalLength += diffSampleCount * 4;
					break;
				default: throw new FormatException("Unsupported pack format.");
			}
			Data = data;
		}

		static int UInt4ToInt4(int v) {
			unchecked {
				return (v & 0x8) == 0 ? v : (int)((uint)v | 0xfffffff0);
			}
		}
	}

	static class KyoshinWin32Extensions {
		public static int ReadInt24BE(this BinaryReader reader) {
			unchecked {
				uint b2 = reader.ReadByte(), b1 = reader.ReadByte(), b0 = reader.ReadByte();
				uint ret = b0 | b1 << 8 | b2 << 16;
				return (int)(b2 >> 7 == 0 ? ret : ret | 0xff000000);
			}
		}
		public static ScaledNumber? ReadCoordinateBCD(this BinaryReader reader) {
			Span<byte> buffer = stackalloc byte[8];
			ReadBCD(reader, buffer);
			if (buffer[0] == 0xb) return null;
			int s = 3;
			int n = ExtractBCD(buffer, ref s);
			return new(n, s);
		}
		public static ScaledNumber? ReadAltitudeBCD(this BinaryReader reader, int integralDigits) {
			Span<byte> buffer = stackalloc byte[8];
			ReadBCD(reader, buffer);
			int sign;
			switch (buffer[0]) {
				case 0xb: return null;
				case 0xc: sign = 1; break;
				case 0xd: sign = -1; break;
				default: throw new FormatException("Invalid altitude BCD.");
			}
			int s = integralDigits;
			int n = ExtractBCD(buffer[1..], ref s);
			return new(n * sign, s);
		}
		public static DateTime ReadTimeBCD(this BinaryReader reader) {
			Span<byte> buffer = stackalloc byte[16];
			ReadBCD(reader, buffer);
			return new(
				ExtractBCD(buffer[0..4]), ExtractBCD(buffer[4..6]), ExtractBCD(buffer[6..8]),
				ExtractBCD(buffer[8..10]), ExtractBCD(buffer[10..12]), ExtractBCD(buffer[12..14]), ExtractBCD(buffer[14..16])
			);
		}
		public static ScaledNumber? ReadMagnitudeBCD(this BinaryReader reader) {
			Span<byte> buffer = stackalloc byte[2];
			ReadBCD(reader, buffer);
			if (buffer[0] == 0xb) return null;
			int s = 1;
			int n = ExtractBCD(buffer, ref s);
			return new(n, s);
		}
		static void ReadBCD(BinaryReader reader, Span<byte> digits) {
			Debug.Assert(digits.Length % 2 == 0);
			for (int i = 0; i < digits.Length;) {
				byte t = reader.ReadByte();
				digits[i++] = (byte)(t >> 4);
				digits[i++] = (byte)(t & 0x0f);
			}
		}
		static int ExtractBCD(ReadOnlySpan<byte> buffer) {
			int n = 0;
			for (int i = 0; i < buffer.Length; i++) {
				byte d = buffer[i];
				if (d >= 0xa) throw new InvalidOperationException("Invalid digit.");
				n *= 10;
				n += d;
			}
			return n;
		}
		static int ExtractBCD(ReadOnlySpan<byte> buffer, ref int scale) {
			int n = 0;
			for (int i = 0; i < buffer.Length; i++) {
				byte d = buffer[i];
				if (d == 0xe) break;
				if (d >= 0xa) throw new InvalidOperationException("Invalid digit.");
				n *= 10;
				n += d;
				scale--;
			}
			return n;
		}
	}
}
