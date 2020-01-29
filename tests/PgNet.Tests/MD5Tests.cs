using System;
using System.Buffers;
using System.Text;
using Xunit;

namespace PgNet.Tests
{
    public class MD5Tests
    {
        [Theory]
        [InlineData("637D2FE925C07C113800509964FB0E06", "For every action there is an equal and opposite government program.")]
        [InlineData("DE3A4D2FD6C73EC2DB2ABAD23B444281", "There is no reason for any individual to have a computer in their home. -Ken Olsen, 1977")]
        [InlineData("ACF203F997E2CF74EA3AFF86985AEFAF", "It's a tiny change to the code and not completely disgusting. - Bob Manchek")]
        [InlineData("CDF7AB6C1FD49BD9933C43F3EA5AF185", "Give me a rock, paper and scissors and I will move the world.  CCFestoon")]
        [InlineData("277CBE255686B48DD7E8F389394D9299", "It's well we cannot hear the screams/That we create in others' dreams.")]
        [InlineData("FD3FB0A7FFB8AF16603F3D3AF98F8E1F", "You remind me of a TV show, but that's all right: I watch it anyway.")]
        [InlineData("63EB3A2F466410104731C4B037600110", "Even if I could be Shakespeare, I think I should still choose to be Faraday. - A. Huxley")]
        [InlineData("72C2ED7592DEBCA1C90FC0100F931A2F", "The fugacity of a constituent in a mixture of gases at a given temperature is proportional to its mole fraction.  Lewis-Randall Rule")]
        [InlineData("E8A48653851E28C69D0506508FB27FC5", "postgres")]
        [InlineData("D41D8CD98F00B204E9800998ECF8427E", "")]
        public void CalculateHashTest(string expectedHexHash, string input)
        {
            Span<byte> hashBuffer = stackalloc byte[MD5.MD5HashByteSize];
            var utf8Encoding = Encoding.UTF8;
            var inputBytesCount = utf8Encoding.GetByteCount(input);
            var inputBufferBytes = ArrayPool<byte>.Shared.Rent(inputBytesCount);

            try
            {
                var inputBuffer = new Span<byte>(inputBufferBytes).Slice(0, inputBytesCount);
                utf8Encoding.GetBytes(input, inputBuffer);

                MD5.Instance.TryComputeHash(inputBuffer, hashBuffer);
                var hash = HashToString(hashBuffer);
                Assert.Equal(expectedHexHash, hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputBufferBytes);
            }
        }

        private static string HashToString(ReadOnlySpan<byte> hash)
        {
            var length1 = hash.Length * 2;
            var charHashArray = new char[length1];
            var num1 = 0;
            var index = 0;
            while (index < length1)
            {
                var num2 = hash[num1++];
                charHashArray[index] = GetHexValue(num2 / 16);
                charHashArray[index + 1] = GetHexValue(num2 % 16);
                index += 2;
            }

            return new string(charHashArray, 0, length1);


            /*return string.Create(hash.Length * 2, hash, (chars, h) =>
            {
                var num1 = 0;
                var index = 0;
                while (index < chars.Length)
                {
                    var num2 = h[num1++];
                    chars[index] = GetHexValue(num2 / 16);
                    chars[index + 1] = GetHexValue(num2 % 16);
                    index += 2;
                }
            });*/

            static char GetHexValue(int i)
            {
                if (i < 10)
                {
                    return (char)(i + 48);
                }

                return (char)(i - 10 + 65);
            }
        }

    }
}
