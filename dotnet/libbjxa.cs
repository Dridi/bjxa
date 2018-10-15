/*-
 * Copyright (C) 2018  Dridi Boukelmoune
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Reflection;

using static System.Diagnostics.Trace;

[assembly:AssemblyVersionAttribute("0.0")]

namespace bjxa {

	internal static class LittleEndian {
		private static uint Read(byte[] buf, int off, int len) {
			Assert(buf != null);
			Assert(off >= 0);
			Assert(len > 0);
			Assert(off + len / 8 <= buf.Length);

			uint res = 0;
			int shl = 0;
			while (len > 0) {
				res += (uint)buf[off] << shl;
				shl += 8;
				len -= 8;
				off++;
			}

			return (res);
		}

		internal static short ReadShort(byte[] buf, int off) {
			return ((short)ReadUShort(buf, off));
		}

		internal static ushort ReadUShort(byte[] buf, int off) {
			return ((ushort)Read(buf, off, 16));
		}

		internal static uint ReadUInt(byte[] buf, int off) {
			return (Read(buf, off, 32));
		}

		internal static void Write(byte[] buf, int off, long val,
		    int len) {
			Write(buf, off, (ulong)val, len);
		}

		internal static void Write(byte[] buf, int off, ulong val,
		    int len) {
			Assert(buf != null);
			Assert(off >= 0);
			Assert(len > 0);
			Assert(off + len / 8 <= buf.Length);

			while (len > 0) {
				buf[off] = (byte)val;
				val >>= 8;
				len -= 8;
				off++;
			}
		}
	}

	public class Format {
		public const int HEADER_SIZE_XA = 32;
		public const int HEADER_SIZE_RIFF = 44;

		internal const uint HEADER_MAGIC = 0x3144574b;
		internal const uint BLOCK_SAMPLES = 32;
		internal const uint WAVE_HEADER_LEN = 16;
		internal const ushort WAVE_FORMAT_PCM = 1;

		public long	DataLengthPcm;
		public long	Blocks;
		public uint	BlockSizePcm;
		public uint	BlockSizeXa;
		public ushort	SamplesRate;
		public uint	SampleBits;
		public uint	Channels;

		public long RiffHeaderLength {
			get { return (HEADER_SIZE_RIFF - 8 + DataLengthPcm); }
		}

		public uint WaveHeaderLength {
			get { return (WAVE_HEADER_LEN); }
		}

		public uint WaveFormatPcm {
			get { return (WAVE_FORMAT_PCM); }
		}

		public uint WaveByteRate {
			get {
				return (SamplesRate * BlockSizePcm /
				    BLOCK_SAMPLES);
			}
		}

		public uint WaveBlockAlign {
			get { return (Channels * SampleBits / 8); }
		}

		internal Format() { }

		public byte[] WritePcm(byte[] dst, short[] pcm) {
			throw new NotImplementedException("WritePcm");
		}

		public int WritePcm(Stream wav, short[] pcm) {
			throw new NotImplementedException("WritePcm");
		}
	}

	public sealed class FormatError: Exception {
		internal FormatError(String message): base(message) { }
	}

	internal delegate byte Inflate(DecoderState dec, short[] dst, int off,
	    byte[] src);

	internal class ChannelState {
		internal short	Prev0;
		internal short	Prev1;
	}

	internal class DecoderState {
		internal uint		DataLength;
		internal uint		Samples;
		internal ushort		SamplesRate;
		internal uint		BlockSize;
		internal uint		Channels;
		internal ChannelState	Left;
		internal ChannelState	Right;
		internal Inflate	Inflate;

		internal DecoderState() {
			Left = new ChannelState();
			Right = new ChannelState();
		}

		internal bool IsValid() {
			long blocks, maxSamples;

			if (DataLength == 0 || Samples == 0 ||
			    SamplesRate == 0 || BlockSize == 0)
				return (false);

			if (Channels != 1 && Channels != 2)
				return (false);

			blocks = DataLength / BlockSize;
			maxSamples = (Format.BLOCK_SAMPLES * DataLength) /
			    (BlockSize * Channels);

			if (blocks * BlockSize != DataLength)
				return (false);

			if (Samples > maxSamples)
				return (false);

			if (maxSamples - Samples >= Format.BLOCK_SAMPLES)
				return (false);

			if (Inflate == null)
				return (false);

			return (true);
		}

		internal Format ToFormat() {
			Assert(IsValid());

			Format fmt = new Format();
			fmt.DataLengthPcm = Samples * Channels *
			    sizeof(short);
			fmt.SamplesRate = SamplesRate;
			fmt.SampleBits = 16;
			fmt.Channels = Channels;
			fmt.BlockSizeXa = BlockSize * Channels;
			fmt.BlockSizePcm = Format.BLOCK_SAMPLES * Channels *
			    sizeof(short);
			fmt.Blocks = DataLength / fmt.BlockSizeXa;

			Assert(fmt.Blocks * fmt.BlockSizeXa == DataLength);

			return (fmt);
		}
	}

	public class Decoder {
		DecoderState state;

		static Inflate BlockInflater(uint bits) {
			if (bits == 4)
				return ((dec, dst, off, src) => {
					throw new NotImplementedException(
						"4bit inflater");
				});

			if (bits == 6)
				return ((dec, dst, off, src) => {
					throw new NotImplementedException(
						"6bit inflater");
				});

			if (bits == 8)
				return ((dec, dst, off, src) => {
					throw new NotImplementedException(
						"8bit inflater");
				});

			return (null);
		}

		public Format ReadHeader(byte[] xa) {
			if (xa == null)
				throw new ArgumentNullException(
				    "bjxa.Decoder.ReadHeader: xa");
			if (xa.Length < Format.HEADER_SIZE_XA)
				throw new ArgumentException(
				    "buffer too small");

			DecoderState tmp = new DecoderState();
			uint magic = LittleEndian.ReadUInt(xa, 0);
			tmp.DataLength = LittleEndian.ReadUInt(xa, 4);
			tmp.Samples = LittleEndian.ReadUInt(xa, 8);
			tmp.SamplesRate = LittleEndian.ReadUShort(xa, 12);
			uint bits = xa[14];
			tmp.Channels = xa[15];
			/* XXX: skipping loop ptr field for now */
			tmp.Left.Prev0 = LittleEndian.ReadShort(xa, 20);
			tmp.Left.Prev1 = LittleEndian.ReadShort(xa, 22);
			tmp.Right.Prev0 = LittleEndian.ReadShort(xa, 24);
			tmp.Right.Prev1 = LittleEndian.ReadShort(xa, 26);
			/* XXX: ignoring padding for now */

			tmp.Inflate = BlockInflater(bits);
			tmp.BlockSize = bits * 4 + 1;

			if (magic != Format.HEADER_MAGIC || !tmp.IsValid())
				throw new FormatError("Invalid XA header");

			state = tmp;

			return (state.ToFormat());
		}

		public Format ReadHeader(Stream xa) {
			if (xa == null)
				throw new ArgumentNullException(
				    "bjxa.Decoder.ReadHeader: xa");

			byte[] hdr = new byte[Format.HEADER_SIZE_XA];
			if (xa.Read(hdr, 0, hdr.Length) != hdr.Length)
				throw new EndOfStreamException("XA header");

			return ReadHeader(hdr);
		}

		public byte[] WriteRiffHeader() {
			if (state == null)
				throw new InvalidOperationException(
				    "Decoder not ready");
			Format fmt = state.ToFormat();
			byte[] hdr = new byte[Format.HEADER_SIZE_RIFF];
			LittleEndian.Write(hdr,  0, 0x46464952, 32); /* RIFF */
			LittleEndian.Write(hdr,  4, fmt.RiffHeaderLength, 32);
			LittleEndian.Write(hdr,  8, 0x45564157, 32); /* WAVE */
			LittleEndian.Write(hdr, 12, 0x20746d66, 32); /* fmt  */
			LittleEndian.Write(hdr, 16, fmt.WaveHeaderLength, 32);
			LittleEndian.Write(hdr, 20, fmt.WaveFormatPcm, 16);
			LittleEndian.Write(hdr, 22, fmt.Channels, 16);
			LittleEndian.Write(hdr, 24, fmt.SamplesRate, 32);
			LittleEndian.Write(hdr, 28, fmt.WaveByteRate, 32);
			LittleEndian.Write(hdr, 32, fmt.WaveBlockAlign, 16);
			LittleEndian.Write(hdr, 34, fmt.SampleBits, 16);
			LittleEndian.Write(hdr, 36, 0x61746164, 32); /* data */
			LittleEndian.Write(hdr, 40, fmt.DataLengthPcm, 32);
			return (hdr);
		}

		public int WriteRiffHeader(Stream wav) {
			if (state == null)
				throw new InvalidOperationException(
				    "Decoder not ready");
			if (wav == null)
				throw new ArgumentNullException(
				    "bjxa.Decoder.WriteRiffHeader: wav");

			byte[] hdr = WriteRiffHeader();
			wav.Write(hdr, 0, hdr.Length);
			return (hdr.Length);
		}

		public int Decode(byte[] xa, short[] pcm) {
			throw new NotImplementedException("Decode");
		}
	}
}
