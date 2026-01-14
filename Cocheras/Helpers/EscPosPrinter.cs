using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Cocheras.Helpers
{
    /// <summary>
    /// Clase para generar comandos ESC/POS para impresoras térmicas (Hasar MIS1785)
    /// </summary>
    public class EscPosPrinter
    {
        private readonly List<byte> _buffer;
        private readonly string _printerName;

        public EscPosPrinter(string printerName)
        {
            _printerName = printerName ?? throw new ArgumentNullException(nameof(printerName));
            _buffer = new List<byte>();
            Initialize();
        }

        /// <summary>
        /// Inicializa la impresora (reset, encoding UTF-8)
        /// </summary>
        private void Initialize()
        {
            // ESC @ - Inicializar impresora
            AddCommand(new byte[] { 0x1B, 0x40 });
            
            // Configurar codificación (UTF-8)
            // ESC t - Seleccionar tabla de caracteres
            AddCommand(new byte[] { 0x1B, 0x74, 0x10 }); // UTF-8
        }

        /// <summary>
        /// Agrega comandos al buffer
        /// </summary>
        private void AddCommand(byte[] command)
        {
            _buffer.AddRange(command);
        }

        /// <summary>
        /// Agrega texto al buffer
        /// </summary>
        private void AddText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _buffer.AddRange(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Establece alineación (0=izquierda, 1=centro, 2=derecha)
        /// </summary>
        public EscPosPrinter SetAlignment(byte alignment)
        {
            AddCommand(new byte[] { 0x1B, 0x61, alignment });
            return this;
        }

        /// <summary>
        /// Establece tamaño de fuente (0=normal, 1=doble ancho, 2=doble alto, 3=doble ancho y alto)
        /// </summary>
        public EscPosPrinter SetFontSize(byte size)
        {
            AddCommand(new byte[] { 0x1D, 0x21, size });
            return this;
        }

        /// <summary>
        /// Establece fuente en negrita
        /// </summary>
        public EscPosPrinter SetBold(bool bold)
        {
            AddCommand(new byte[] { 0x1B, 0x45, (byte)(bold ? 1 : 0) });
            return this;
        }

        /// <summary>
        /// Trunca texto si es muy largo para evitar cortes (máx 48 caracteres para 80mm)
        /// </summary>
        private string TruncateText(string text, int maxLength = 48)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Imprime texto centrado
        /// </summary>
        public EscPosPrinter PrintCenter(string text, bool bold = false, byte fontSize = 0)
        {
            if (string.IsNullOrEmpty(text)) return this;
            
            SetAlignment(1); // Centro
            if (bold) SetBold(true);
            if (fontSize > 0) SetFontSize(fontSize);
            
            // Truncar texto según tamaño de fuente para evitar cortes
            int maxLen = fontSize > 0 ? 24 : 48; // Si es doble tamaño, mitad de caracteres
            string truncated = TruncateText(text, maxLen);
            AddText(truncated);
            AddText("\n");
            
            if (bold) SetBold(false);
            if (fontSize > 0) SetFontSize(0);
            SetAlignment(0); // Volver a izquierda
            return this;
        }

        /// <summary>
        /// Imprime línea vacía
        /// </summary>
        public EscPosPrinter PrintEmptyLine(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                AddText("\n");
            }
            return this;
        }

        /// <summary>
        /// Imprime código de barras Code128
        /// </summary>
        public EscPosPrinter PrintBarcode(string data, int height = 80, byte width = 2, byte position = 2)
        {
            if (string.IsNullOrEmpty(data)) return this;

            // GS H - Posición del código de texto (0=ninguno, 1=arriba, 2=abajo)
            AddCommand(new byte[] { 0x1D, 0x48, position });
            
            // GS h - Altura del código de barras (1-255)
            AddCommand(new byte[] { 0x1D, 0x68, (byte)Math.Min(255, Math.Max(1, height)) });
            
            // GS w - Ancho del código de barras (2-6, 2=normal)
            AddCommand(new byte[] { 0x1D, 0x77, (byte)Math.Min(6, Math.Max((int)2, (int)width)) });
            
            // GS k - Imprimir código de barras Code128
            // 73 = Code128 (formato: GS k m n d1...dk)
            byte[] dataBytes = Encoding.ASCII.GetBytes(data); // Code128 usa ASCII
            if (dataBytes.Length > 255) return this; // Limitar tamaño
            
            List<byte> barcodeCmd = new List<byte> { 0x1D, 0x6B, 0x49, (byte)dataBytes.Length };
            barcodeCmd.AddRange(dataBytes);
            AddCommand(barcodeCmd.ToArray());
            
            AddText("\n");
            return this;
        }

        /// <summary>
        /// Avanza papel
        /// </summary>
        public EscPosPrinter FeedLines(int lines)
        {
            AddCommand(new byte[] { 0x1B, 0x64, (byte)lines });
            return this;
        }

        /// <summary>
        /// Corta el papel (parcial o completo)
        /// </summary>
        public EscPosPrinter CutPaper(bool fullCut = true)
        {
            // Avanzar papel antes de cortar (asegura que el contenido esté completamente impreso)
            FeedLines(3);
            
            // GS V - Corte de papel
            // Para Hasar MIS1785: m=0 o 48 = corte completo, m=1 o 49 = corte parcial
            // Usamos 0x30 (48) para corte completo o 0x31 (49) para parcial
            byte cutMode = fullCut ? (byte)0x30 : (byte)0x31;
            AddCommand(new byte[] { 0x1D, 0x56, cutMode });
            
            return this;
        }

        /// <summary>
        /// Activa/desactiva modo de color invertido (reverse video)
        /// </summary>
        public EscPosPrinter SetInverseColors(bool inverse)
        {
            // ESC { n - Reverse video (n=1 activa, n=0 desactiva)
            AddCommand(new byte[] { 0x1B, 0x7B, (byte)(inverse ? 1 : 0) });
            return this;
        }

        /// <summary>
        /// Imprime texto con fondo negro (rectángulo negro) - la placa
        /// Usa reverse video para texto blanco sobre fondo negro
        /// </summary>
        public EscPosPrinter PrintInverse(string text, bool bold = false, byte fontSize = 0)
        {
            if (string.IsNullOrEmpty(text)) return this;
            
            SetAlignment(1); // Centro
            
            int maxLen = fontSize > 0 ? 18 : 40;
            string truncated = TruncateText(text, maxLen);
            
            // Activar reverse video (fondo negro, texto blanco)
            SetInverseColors(true);
            SetBold(true);
            if (fontSize > 0) SetFontSize(fontSize);
            
            // Calcular padding para centrar el texto
            int totalWidth = 48;
            int textWidth = truncated.Length;
            int padding = (totalWidth - textWidth) / 2;
            
            // Imprimir línea superior del rectángulo (espacios que se verán como fondo negro)
            AddText(new string(' ', totalWidth));
            AddText("\n");
            
            // Imprimir línea con texto centrado
            AddText(new string(' ', padding));
            AddText(truncated);
            AddText(new string(' ', totalWidth - textWidth - padding));
            AddText("\n");
            
            // Imprimir línea inferior del rectángulo
            AddText(new string(' ', totalWidth));
            AddText("\n");
            
            // Desactivar reverse video
            SetInverseColors(false);
            SetBold(false);
            if (fontSize > 0) SetFontSize(0);
            SetAlignment(0);
            return this;
        }

        /// <summary>
        /// Imprime un círculo simulado con texto (para el logo E)
        /// </summary>
        public EscPosPrinter PrintCircleWithText(string text, bool bold = true, byte fontSize = 3)
        {
            if (string.IsNullOrEmpty(text)) return this;
            
            SetAlignment(1); // Centro
            
            // Activar reverse video para el círculo (fondo negro, texto blanco)
            SetInverseColors(true);
            
            // Crear un círculo más redondeado con caracteres
            // Línea superior del círculo
            AddText("   ██████   ");
            AddText("\n");
            
            // Líneas laterales con texto en el centro
            AddText("  ██");
            SetBold(bold);
            if (fontSize > 0) SetFontSize(fontSize);
            AddText("  " + text + "  ");
            SetBold(false);
            if (fontSize > 0) SetFontSize(0);
            AddText("██  ");
            AddText("\n");
            
            // Línea inferior del círculo
            AddText("   ██████   ");
            AddText("\n");
            
            // Desactivar reverse video
            SetInverseColors(false);
            SetAlignment(0);
            return this;
        }

        /// <summary>
        /// Envía todos los comandos a la impresora
        /// </summary>
        public bool Print()
        {
            if (_buffer.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("EscPosPrinter: Buffer vacío, no hay nada que imprimir");
                return false;
            }
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"EscPosPrinter: Intentando imprimir en '{_printerName}', {_buffer.Count} bytes");
                bool result = SendBytesToPrinter(_printerName, _buffer.ToArray());
                if (!result)
                {
                    System.Diagnostics.Debug.WriteLine($"EscPosPrinter: Error al enviar datos a la impresora '{_printerName}'");
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EscPosPrinter: Excepción al imprimir: {ex.Message}");
                return false;
            }
        }

        // Métodos para comunicación directa con la impresora
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName = "ESC/POS Print";
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile = null!;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType = "RAW";
        }

        private static bool SendBytesToPrinter(string printerName, byte[] data)
        {
            IntPtr hPrinter = IntPtr.Zero;
            bool success = false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Abriendo impresora '{printerName}'");
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Error al abrir impresora. Código de error: {error}");
                    return false;
                }

                DOCINFOA di = new DOCINFOA();

                System.Diagnostics.Debug.WriteLine("SendBytesToPrinter: Iniciando documento");
                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Error al iniciar documento. Código: {error}");
                    ClosePrinter(hPrinter);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("SendBytesToPrinter: Iniciando página");
                if (!StartPagePrinter(hPrinter))
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Error al iniciar página. Código: {error}");
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Escribiendo {data.Length} bytes");
                IntPtr pBytes = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, pBytes, data.Length);
                success = WritePrinter(hPrinter, pBytes, data.Length, out int written);
                Marshal.FreeHGlobal(pBytes);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Error al escribir. Código: {error}, bytes escritos: {written}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Escritos {written} bytes exitosamente");
                }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter: Excepción: {ex.Message}");
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
        /// Limpia el buffer
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
            Initialize();
        }
    }
}
