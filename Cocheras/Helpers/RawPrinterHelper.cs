using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Cocheras.Helpers
{
    /// <summary>
    /// Helper para enviar datos RAW directamente a la impresora (comandos ESC/POS)
    /// </summary>
    public class RawPrinterHelper
    {
        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        /// <summary>
        /// Envía datos RAW (bytes) directamente a la impresora
        /// </summary>
        public static bool SendBytesToPrinter(string printerName, byte[] data)
        {
            IntPtr hPrinter = IntPtr.Zero;
            bool success = false;

            try
            {
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    return false;
                }

                DOCINFOA di = new DOCINFOA
                {
                    pDocName = "ESC/POS Print",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    ClosePrinter(hPrinter);
                    return false;
                }

                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                IntPtr pBytes = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, pBytes, data.Length);
                success = WritePrinter(hPrinter, pBytes, data.Length, out int written);
                Marshal.FreeHGlobal(pBytes);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            catch
            {
                success = false;
            }
            finally
            {
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                }
            }

            return success;
        }

        /// <summary>
        /// Envía un string directamente a la impresora
        /// </summary>
        public static bool SendStringToPrinter(string printerName, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            return SendBytesToPrinter(printerName, data);
        }
    }
}
