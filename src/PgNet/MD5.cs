using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PgNet
{
    internal sealed class MD5
    {
        public const int MD5HashByteSize = 16;
        public const int MD5HashHexByteSize = MD5HashByteSize * 2;

        private const int MD5_S11 = 7;
        private const int MD5_S12 = 12;
        private const int MD5_S13 = 17;
        private const int MD5_S14 = 22;
        private const int MD5_S21 = 5;
        private const int MD5_S22 = 9;
        private const int MD5_S23 = 14;
        private const int MD5_S24 = 20;
        private const int MD5_S31 = 4;
        private const int MD5_S32 = 11;
        private const int MD5_S33 = 16;
        private const int MD5_S34 = 23;
        private const int MD5_S41 = 6;
        private const int MD5_S42 = 10;
        private const int MD5_S43 = 15;
        private const int MD5_S44 = 21;

        public static MD5 Instance = new MD5();

        private MD5()
        {
        }

        public void TryComputeHash(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Span<byte> temp = stackalloc byte[64 + 4 * sizeof(uint)];
            var working = temp.Slice(0, 64);
            var state = MemoryMarshal.Cast<byte, uint>(temp.Slice(64, 4 * sizeof(uint)));

            state[3] = 0x10325476;
            state[2] = 0x98badcfe;
            state[1] = 0xefcdab89;
            state[0] = 0x67452301;

            var length = source.Length * 8;

            //We pass in the input array by block, the final block of data must be handled specialy for padding & length embeding
            var originalSource = source;
            var i = 0;
            while (i <= originalSource.Length - 64)
            {
                source = originalSource.Slice(i, 64);
                Transform(state, source);

                i += 64;
            }

            source = originalSource.Slice(i);
            HashFinal(state, source, destination, length, working);
        }

        private static void HashFinal(Span<uint> state, ReadOnlySpan<byte> data, Span<byte> destination, long len, Span<byte> working)
        {
            //Padding is a single bit 1, followed by the number of 0s required to make size congruent to 448 modulo 512. Step 1 of RFC 1321  
            //The CLR ensures that our buffer is 0-assigned, we don't need to explicitly set it. This is why it ends up being quicker to just
            //use a temporary array rather then doing in-place assignment (5% for small inputs)
            var cbSize = data.Length;
            data.CopyTo(working);
            working[cbSize] = 0x80;


            //We have enough room to store the length in this chunk
            if (cbSize < 56)
            {
                BitConverter.TryWriteBytes(working.Slice(56), len);
                Transform(state, working);
            }
            else //We need an additional chunk to store the length
            {
                Transform(state, working);

                //Create an entirely new chunk due to the 0-assigned trick mentioned above, to avoid an extra function call clearing the array
                Span<byte> temp = stackalloc byte[64];
                BitConverter.TryWriteBytes(temp.Slice(56), len);
                Transform(state, temp);
            }

            MemoryMarshal.Cast<uint, byte>(state).CopyTo(destination);
        }

        private static void Transform(Span<uint> state, ReadOnlySpan<byte> data)
        {
            var d = state[3];
            var c = state[2];
            var b = state[1];
            var a = state[0];

            var temp = MemoryMarshal.Cast<byte, uint>(data);
            ref var ptr = ref MemoryMarshal.GetReference(temp);

            // Round 1
            a = FF(a, b, c, d, Unsafe.Add(ref ptr, 0), MD5_S11, 0xd76aa478);
            d = FF(d, a, b, c, Unsafe.Add(ref ptr, 1), MD5_S12, 0xe8c7b756);
            c = FF(c, d, a, b, Unsafe.Add(ref ptr, 2), MD5_S13, 0x242070db);
            b = FF(b, c, d, a, Unsafe.Add(ref ptr, 3), MD5_S14, 0xc1bdceee);
            a = FF(a, b, c, d, Unsafe.Add(ref ptr, 4), MD5_S11, 0xf57c0faf);
            d = FF(d, a, b, c, Unsafe.Add(ref ptr, 5), MD5_S12, 0x4787c62a);
            c = FF(c, d, a, b, Unsafe.Add(ref ptr, 6), MD5_S13, 0xa8304613);
            b = FF(b, c, d, a, Unsafe.Add(ref ptr, 7), MD5_S14, 0xfd469501);
            a = FF(a, b, c, d, Unsafe.Add(ref ptr, 8), MD5_S11, 0x698098d8);
            d = FF(d, a, b, c, Unsafe.Add(ref ptr, 9), MD5_S12, 0x8b44f7af);
            c = FF(c, d, a, b, Unsafe.Add(ref ptr, 10), MD5_S13, 0xffff5bb1);
            b = FF(b, c, d, a, Unsafe.Add(ref ptr, 11), MD5_S14, 0x895cd7be);
            a = FF(a, b, c, d, Unsafe.Add(ref ptr, 12), MD5_S11, 0x6b901122);
            d = FF(d, a, b, c, Unsafe.Add(ref ptr, 13), MD5_S12, 0xfd987193);
            c = FF(c, d, a, b, Unsafe.Add(ref ptr, 14), MD5_S13, 0xa679438e);
            b = FF(b, c, d, a, Unsafe.Add(ref ptr, 15), MD5_S14, 0x49b40821);

            // Round 2
            a = GG(a, b, c, d, Unsafe.Add(ref ptr, 1), MD5_S21, 0xf61e2562);
            d = GG(d, a, b, c, Unsafe.Add(ref ptr, 6), MD5_S22, 0xc040b340);
            c = GG(c, d, a, b, Unsafe.Add(ref ptr, 11), MD5_S23, 0x265e5a51);
            b = GG(b, c, d, a, Unsafe.Add(ref ptr, 0), MD5_S24, 0xe9b6c7aa);
            a = GG(a, b, c, d, Unsafe.Add(ref ptr, 5), MD5_S21, 0xd62f105d);
            d = GG(d, a, b, c, Unsafe.Add(ref ptr, 10), MD5_S22, 0x02441453);
            c = GG(c, d, a, b, Unsafe.Add(ref ptr, 15), MD5_S23, 0xd8a1e681);
            b = GG(b, c, d, a, Unsafe.Add(ref ptr, 4), MD5_S24, 0xe7d3fbc8);
            a = GG(a, b, c, d, Unsafe.Add(ref ptr, 9), MD5_S21, 0x21e1cde6);
            d = GG(d, a, b, c, Unsafe.Add(ref ptr, 14), MD5_S22, 0xc33707d6);
            c = GG(c, d, a, b, Unsafe.Add(ref ptr, 3), MD5_S23, 0xf4d50d87);
            b = GG(b, c, d, a, Unsafe.Add(ref ptr, 8), MD5_S24, 0x455a14ed);
            a = GG(a, b, c, d, Unsafe.Add(ref ptr, 13), MD5_S21, 0xa9e3e905);
            d = GG(d, a, b, c, Unsafe.Add(ref ptr, 2), MD5_S22, 0xfcefa3f8);
            c = GG(c, d, a, b, Unsafe.Add(ref ptr, 7), MD5_S23, 0x676f02d9);
            b = GG(b, c, d, a, Unsafe.Add(ref ptr, 12), MD5_S24, 0x8d2a4c8a);

            // Round 3
            a = HH(a, b, c, d, Unsafe.Add(ref ptr, 5), MD5_S31, 0xfffa3942);
            d = HH(d, a, b, c, Unsafe.Add(ref ptr, 8), MD5_S32, 0x8771f681);
            c = HH(c, d, a, b, Unsafe.Add(ref ptr, 11), MD5_S33, 0x6d9d6122);
            b = HH(b, c, d, a, Unsafe.Add(ref ptr, 14), MD5_S34, 0xfde5380c);
            a = HH(a, b, c, d, Unsafe.Add(ref ptr, 1), MD5_S31, 0xa4beea44);
            d = HH(d, a, b, c, Unsafe.Add(ref ptr, 4), MD5_S32, 0x4bdecfa9);
            c = HH(c, d, a, b, Unsafe.Add(ref ptr, 7), MD5_S33, 0xf6bb4b60);
            b = HH(b, c, d, a, Unsafe.Add(ref ptr, 10), MD5_S34, 0xbebfbc70);
            a = HH(a, b, c, d, Unsafe.Add(ref ptr, 13), MD5_S31, 0x289b7ec6);
            d = HH(d, a, b, c, Unsafe.Add(ref ptr, 0), MD5_S32, 0xeaa127fa);
            c = HH(c, d, a, b, Unsafe.Add(ref ptr, 3), MD5_S33, 0xd4ef3085);
            b = HH(b, c, d, a, Unsafe.Add(ref ptr, 6), MD5_S34, 0x04881d05);
            a = HH(a, b, c, d, Unsafe.Add(ref ptr, 9), MD5_S31, 0xd9d4d039);
            d = HH(d, a, b, c, Unsafe.Add(ref ptr, 12), MD5_S32, 0xe6db99e5);
            c = HH(c, d, a, b, Unsafe.Add(ref ptr, 15), MD5_S33, 0x1fa27cf8);
            b = HH(b, c, d, a, Unsafe.Add(ref ptr, 2), MD5_S34, 0xc4ac5665);

            // Round 4
            a = II(a, b, c, d, Unsafe.Add(ref ptr, 0), MD5_S41, 0xf4292244);
            d = II(d, a, b, c, Unsafe.Add(ref ptr, 7), MD5_S42, 0x432aff97);
            c = II(c, d, a, b, Unsafe.Add(ref ptr, 14), MD5_S43, 0xab9423a7);
            b = II(b, c, d, a, Unsafe.Add(ref ptr, 5), MD5_S44, 0xfc93a039);
            a = II(a, b, c, d, Unsafe.Add(ref ptr, 12), MD5_S41, 0x655b59c3);
            d = II(d, a, b, c, Unsafe.Add(ref ptr, 3), MD5_S42, 0x8f0ccc92);
            c = II(c, d, a, b, Unsafe.Add(ref ptr, 10), MD5_S43, 0xffeff47d);
            b = II(b, c, d, a, Unsafe.Add(ref ptr, 1), MD5_S44, 0x85845dd1);
            a = II(a, b, c, d, Unsafe.Add(ref ptr, 8), MD5_S41, 0x6fa87e4f);
            d = II(d, a, b, c, Unsafe.Add(ref ptr, 15), MD5_S42, 0xfe2ce6e0);
            c = II(c, d, a, b, Unsafe.Add(ref ptr, 6), MD5_S43, 0xa3014314);
            b = II(b, c, d, a, Unsafe.Add(ref ptr, 13), MD5_S44, 0x4e0811a1);
            a = II(a, b, c, d, Unsafe.Add(ref ptr, 4), MD5_S41, 0xf7537e82);
            d = II(d, a, b, c, Unsafe.Add(ref ptr, 11), MD5_S42, 0xbd3af235);
            c = II(c, d, a, b, Unsafe.Add(ref ptr, 2), MD5_S43, 0x2ad7d2bb);
            b = II(b, c, d, a, Unsafe.Add(ref ptr, 9), MD5_S44, 0xeb86d391);

            state[3] = unchecked(state[3] + d);
            state[2] = unchecked(state[2] + c);
            state[1] = unchecked(state[1] + b);
            state[0] = unchecked(state[0] + a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FF(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            return unchecked(b + BitOperations.RotateLeft(a + (((d ^ c) & b) ^ d) + x + t, s));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GG(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            return unchecked(b + BitOperations.RotateLeft(a + (((b ^ c) & d) ^ c) + x + t, s));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HH(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            return unchecked(b + BitOperations.RotateLeft(a + (b ^ c ^ d) + x + t, s));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint II(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            return unchecked(b + BitOperations.RotateLeft(a + (c ^ (b | ~d )) + x + t, s));
        }
    }
}
