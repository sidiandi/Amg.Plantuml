using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Plantuml
{
    public static class StreamExtensions
    {
        public static Task<int> CopyUntilAsync(this Stream input, string separator, Stream output)
            => Task.Factory.StartNew(() =>
            {
                return input.CopyUntil(separator, output);
            }, TaskCreationOptions.LongRunning);

        /// <summary>
        /// Copies input to output until separator is found or input is at the end. Does not copy the separator.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="separator"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int CopyUntil(this Stream input, string separator, Stream output)
        {
            var sepBytes = Encoding.ASCII.GetBytes(separator);
            int separatorLength = sepBytes.Length;
            var buffer = new byte[separatorLength];
            var iw = 0;
            var ir = 0;
            var bytesRead = 0;

            int Advance(int i) => (i + 1) % buffer.Length;

            void WriteToOutput()
            {
                output.Write(buffer, ir, 1);
                ir = Advance(ir);
                ++bytesRead;
            }

            while (true)
            {
                if (0 == input.Read(buffer, iw, 1))
                {
                    // end of input => empty buffer into output
                    for (;ir != iw;)
                    {
                        WriteToOutput();
                    }
                    break;
                }
                iw = Advance(iw);

                bool IsSeparator()
                {
                    int i = ir;
                    int iSeparator = 0;
                    while (true)
                    {
                        if (buffer[i] != sepBytes[iSeparator])
                        {
                            return false;
                        }
                        i = Advance(i);
                        ++iSeparator;
                        if (iSeparator == separatorLength)
                        {
                            return true;
                        }
                        if (i == iw)
                        {
                            return false;
                        }
                    }
                }

                if (IsSeparator())
                {
                    break;
                }

                if (ir == iw)
                {
                    WriteToOutput();
                }
            }
            return bytesRead;
        }
    }
}
