/**
MIT License

Copyright (c) 2019 Zhentar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModAPI.Utils
{
	public static class xxHash64
	{
		private const ulong PRIME64_1 = 11400714785074694791UL;
		private const ulong PRIME64_2 = 14029467366897019727UL;
		private const ulong PRIME64_3 = 1609587929392839161UL;
		private const ulong PRIME64_4 = 9650029242287828579UL;
		private const ulong PRIME64_5 = 2870177450012600261UL;

		[StructLayout(LayoutKind.Sequential)]
		private struct QuadUlong
		{
			public ulong v1;
			public ulong v2;
			public ulong v3;
			public ulong v4;
		}

		public static ulong Hash(in ReadOnlySpan<byte> buffer)
		{
			unchecked
			{
				var remainingBytes = buffer;
				var bulkVals = remainingBytes.PopAll<QuadUlong>();

				var h64 = !bulkVals.IsEmpty ? BulkStride(bulkVals) : PRIME64_5;

				h64 += (uint)buffer.Length;

				var ulongSpan = remainingBytes.PopAll<ulong>();
				for (int i = 0; i < ulongSpan.Length; i++)
				{
					var val = ulongSpan[i] * PRIME64_2;
					val = RotateLeft(val, 31);
					val *= PRIME64_1;
					h64 ^= val;
					h64 = RotateLeft(h64, 27) * PRIME64_1;
					h64 += PRIME64_4;
				}

				ref byte remaining = ref MemoryMarshal.GetReference(remainingBytes);
				if (remainingBytes.Length >= sizeof(uint))
				{
					h64 ^= Unsafe.As<byte, uint>(ref remaining) * PRIME64_1;
					h64 = RotateLeft(h64, 23) * PRIME64_2;
					h64 += PRIME64_3;
					remaining = ref Unsafe.Add(ref remaining, sizeof(uint));
				}

				switch (remainingBytes.Length % sizeof(uint))
				{
					case 3:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						remaining = ref Unsafe.Add(ref remaining, 1);
						goto case 2;
					case 2:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						remaining = ref Unsafe.Add(ref remaining, 1);
						goto case 1;
					case 1:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						break;
				}

				h64 ^= h64 >> 33;
				h64 *= PRIME64_2;
				h64 ^= h64 >> 29;
				h64 *= PRIME64_3;
				h64 ^= h64 >> 32;

				return h64;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static ulong BulkStride(in ReadOnlySpan<QuadUlong> bulkVals)
		{
			unchecked
			{
				ulong acc1 = 0 + PRIME64_1 + PRIME64_2;
				ulong acc2 = 0 + PRIME64_2;
				ulong acc3 = 0 + 0;
				ulong acc4 = 0 - PRIME64_1;

				for (int i = 0; i < bulkVals.Length; i++)
				{
					ref readonly QuadUlong val = ref bulkVals[i];

					acc1 += val.v1 * PRIME64_2;
					acc2 += val.v2 * PRIME64_2;
					acc3 += val.v3 * PRIME64_2;
					acc4 += val.v4 * PRIME64_2;

					acc1 = RotateLeft(acc1, 31);
					acc2 = RotateLeft(acc2, 31);
					acc3 = RotateLeft(acc3, 31);
					acc4 = RotateLeft(acc4, 31);

					acc1 *= PRIME64_1;
					acc2 *= PRIME64_1;
					acc3 *= PRIME64_1;
					acc4 *= PRIME64_1;
				}

				return MergeValues(acc1, acc2, acc3, acc4);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong RotateLeft(ulong val, int bits) => (val << bits) | (val >> (64 - bits));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong MergeValues(ulong v1, ulong v2, ulong v3, ulong v4)
		{
			var acc = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
			acc = MergeAccumulator(acc, v1);
			acc = MergeAccumulator(acc, v2);
			acc = MergeAccumulator(acc, v3);
			acc = MergeAccumulator(acc, v4);
			return acc;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong MergeAccumulator(ulong accMain, ulong accN)
		{
			accN = (accN * PRIME64_2);
			accN = RotateLeft(accN, 31);
			accN = accN * PRIME64_1;
			accMain ^= accN;
			accMain *= PRIME64_1;
			return accMain + PRIME64_4;
		}

	}

	internal static class Extensions
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> PopAll<TTo>(this ref ReadOnlySpan<byte> @this) where TTo : struct
		{
#if NETCOREAPP3_0
			var totBytes = @this.Length;
			var toLength = (totBytes / Unsafe.SizeOf<TTo>());
			var sliceLength = toLength * Unsafe.SizeOf<TTo>();
			ref var thisRef = ref MemoryMarshal.GetReference(@this);
			@this = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref thisRef, sliceLength), totBytes - sliceLength);
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, TTo>(ref thisRef), toLength);
#else
			return @this.PopAll<TTo, byte>();
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> PopAll<TTo, TFrom>(this ref ReadOnlySpan<TFrom> @this) where TFrom : struct where TTo : struct
		{
			var totBytes = @this.Length * Unsafe.SizeOf<TFrom>();
			var toLength = (totBytes / Unsafe.SizeOf<TTo>());
			var sliceLength = toLength * Unsafe.SizeOf<TTo>() / Unsafe.SizeOf<TFrom>();

#if NETSTANDARD2_0
			var result = MemoryMarshal.Cast<TFrom, TTo>(@this);
#else
			var result = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(@this)), toLength);
#endif
			@this = @this.Slice(sliceLength);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint AsLittleEndian(this uint @this)
		{
			if (BitConverter.IsLittleEndian) { return @this; }
			return BinaryPrimitives.ReverseEndianness(@this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong AsLittleEndian(this ulong @this)
		{
			if (BitConverter.IsLittleEndian) { return @this; }
			return BinaryPrimitives.ReverseEndianness(@this);
		}

		public static bool TryPop<TTo>(this ref ReadOnlySpan<byte> @this, int count, out ReadOnlySpan<TTo> popped) where TTo : struct
		{
			var byteCount = count * Unsafe.SizeOf<TTo>();
			if (@this.Length >= byteCount)
			{
				popped = MemoryMarshal.Cast<byte, TTo>(@this.Slice(0, byteCount));
				@this = @this.Slice(byteCount);
				return true;
			}
			popped = default;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo First<TTo>(this ReadOnlySpan<byte> @this) where TTo : struct
		{
			return ref MemoryMarshal.Cast<byte, TTo>(@this)[0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo Last<TTo>(this ReadOnlySpan<byte> @this) where TTo : struct
		{
			return ref MemoryMarshal.Cast<byte, TTo>(@this.Slice(@this.Length - Unsafe.SizeOf<TTo>()))[0];
		}

		public static ref readonly TTo First<TFrom, TTo>(this ReadOnlySpan<TFrom> @this) where TTo : struct where TFrom : struct
		{
#if NETSTANDARD2_0
			return ref MemoryMarshal.Cast<TFrom, TTo>(@this)[0];
#else
			//TODO: is this version actually any faster/better at all?
			return ref MemoryMarshal.AsRef<TTo>(MemoryMarshal.AsBytes(@this));
#endif
		}

	}

	public static class Safeish
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo As<TFrom, TTo>(in TFrom from) where TTo : struct where TFrom : struct
		{
			if (Unsafe.SizeOf<TFrom>() < Unsafe.SizeOf<TTo>()) { throw new InvalidCastException(); }
			return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(from));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo AsMut<TFrom, TTo>(ref TFrom from) where TTo : struct where TFrom : struct
		{
			if (Unsafe.SizeOf<TFrom>() < Unsafe.SizeOf<TTo>()) { throw new InvalidCastException(); }
			return ref Unsafe.As<TFrom, TTo>(ref from);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> AsSpan<TFrom, TTo>(in TFrom from) where TTo : struct where TFrom : struct
		{
#if NETSTANDARD2_0
			var asSpan = CreateReadOnlySpan(ref Unsafe.AsRef(from));
#else
			var asSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(from), 1);
#endif
			return MemoryMarshal.Cast<TFrom, TTo>(asSpan);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<TTo> AsMutableSpan<TFrom, TTo>(ref TFrom from) where TTo : struct where TFrom : struct
		{
#if NETSTANDARD2_0
			var asSpan = CreateSpan(ref Unsafe.AsRef(from));
#else
			var asSpan = MemoryMarshal.CreateSpan(ref from, 1);
#endif
			return MemoryMarshal.Cast<TFrom, TTo>(asSpan);
		}

#if NETSTANDARD2_0
		private static unsafe Span<T> CreateSpan<T>(ref T from) where T : struct
		{
			void* ptr = Unsafe.AsPointer(ref from);
			return new Span<T>(ptr, 1);
		}
		
		private static unsafe ReadOnlySpan<T> CreateReadOnlySpan<T>(ref T from) where T : struct
		{
			void* ptr = Unsafe.AsPointer(ref from);
			return new ReadOnlySpan<T>(ptr, 1);
		}
#endif
	}
}