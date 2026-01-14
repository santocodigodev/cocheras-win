using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Controls.Primitives;
using Cocheras.Models;
using Cocheras.Services;

namespace Cocheras.Windows
{
    public partial class SimuladorTarifasWindow : Window
    {
        private readonly DatabaseService _dbService;
        private bool _modoOscuro = false;

        // Colores modo oscuro
        private static readonly SolidColorBrush ColorFondoOscuro = new SolidColorBrush(Color.FromRgb(15, 23, 42));          // más profundo
        private static readonly SolidColorBrush ColorFondoSecundarioOscuro = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        private static readonly SolidColorBrush ColorTextoOscuro = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        private static readonly SolidColorBrush ColorTextoSecundarioOscuro = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        private static readonly SolidColorBrush ColorBordeOscuro = new SolidColorBrush(Color.FromRgb(71, 85, 105));
        private static readonly SolidColorBrush ColorInputOscuro = new SolidColorBrush(Color.FromRgb(30, 41, 59));

        // Colores modo claro
        private static readonly SolidColorBrush ColorFondoClaro = Brushes.White;
        private static readonly SolidColorBrush ColorTextoClaro = new SolidColorBrush(Color.FromRgb(17, 24, 39));
        private static readonly SolidColorBrush ColorTextoSecundarioClaro = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private static readonly SolidColorBrush ColorBordeClaro = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        private static readonly SolidColorBrush ColorInputClaro = Brushes.White;

        public SimuladorTarifasWindow(DatabaseService dbService, bool modoOscuro = false)
        {
            InitializeComponent();
            _dbService = dbService;
            _modoOscuro = modoOscuro;
            
            // Asegurar que la ventana se ajuste al contenido
            this.SizeToContent = SizeToContent.WidthAndHeight;
            
            CargarDatos();
            
            // Aplicar tema y cálculo cuando la UI esté cargada para que tome todos los controles
            this.Loaded += (s, e) =>
            {
                AplicarTema(_modoOscuro);
                CalcularImporte();
                this.SizeToContent = SizeToContent.WidthAndHeight;
            };
        }

        private void CargarDatos()
        {
            // Cargar tarifas (solo no mensuales para el simulador)
            var tarifas = _dbService.ObtenerTarifas()
                .Where(t => t.Tipo != TipoTarifa.Mensual)
                .OrderBy(t => t.Id)
                .ToList();

            CmbTipoTarifa.Items.Clear();
            foreach (var tarifa in tarifas)
            {
                var item = new ComboBoxItem
                {
                    Content = tarifa.Nombre,
                    Tag = tarifa
                };
                CmbTipoTarifa.Items.Add(item);
            }
            if (CmbTipoTarifa.Items.Count > 0)
                CmbTipoTarifa.SelectedIndex = 0;

            // Cargar categorías
            var categorias = _dbService.ObtenerCategorias().OrderBy(c => c.Orden).ToList();
            CmbCategoria.Items.Clear();
            foreach (var categoria in categorias)
            {
                var item = new ComboBoxItem
                {
                    Content = categoria.Nombre,
                    Tag = categoria
                };
                CmbCategoria.Items.Add(item);
            }
            if (CmbCategoria.Items.Count > 0)
                CmbCategoria.SelectedIndex = 0;

            // Configurar fecha y hora inicial
            DpFechaInicio.SelectedDate = DateTime.Now;
            CargarHoras();
            CargarMinutos();
            if (CmbHoraInicio.Items.Count > 0)
                CmbHoraInicio.SelectedIndex = DateTime.Now.Hour;
            if (CmbMinutoInicio.Items.Count > 0)
                CmbMinutoInicio.SelectedIndex = DateTime.Now.Minute;

            // Eventos para recalcular
            CmbTipoTarifa.SelectionChanged += (s, e) => CalcularImporte();
            CmbCategoria.SelectionChanged += (s, e) => CalcularImporte();
            DpFechaInicio.SelectedDateChanged += (s, e) => CalcularImporte();
            CmbHoraInicio.SelectionChanged += (s, e) => CalcularImporte();
            CmbMinutoInicio.SelectionChanged += (s, e) => CalcularImporte();
            TxtDias.TextChanged += (s, e) => CalcularImporte();
            TxtHoras.TextChanged += (s, e) => CalcularImporte();
            TxtMinutos.TextChanged += (s, e) => CalcularImporte();
        }

        private void CargarHoras()
        {
            CmbHoraInicio.Items.Clear();
            for (int i = 0; i < 24; i++)
            {
                var item = new ComboBoxItem
                {
                    Content = i.ToString("00"),
                    Tag = i
                };
                CmbHoraInicio.Items.Add(item);
            }
        }

        private void CargarMinutos()
        {
            CmbMinutoInicio.Items.Clear();
            for (int i = 0; i < 60; i++)
            {
                var item = new ComboBoxItem
                {
                    Content = i.ToString("00"),
                    Tag = i
                };
                CmbMinutoInicio.Items.Add(item);
            }
        }

        private void CalcularImporte()
        {
            try
            {
                if (CmbTipoTarifa.SelectedItem == null || CmbCategoria.SelectedItem == null)
                {
                    TxtDetalleCalculo.Text = "0 x Hora ($0) = $0";
                    TxtTotalPagar.Text = "El cliente paga: $0";
                    return;
                }

                var tarifa = (CmbTipoTarifa.SelectedItem as ComboBoxItem)?.Tag as Tarifa;
                var categoria = (CmbCategoria.SelectedItem as ComboBoxItem)?.Tag as Categoria;

                if (tarifa == null || categoria == null)
                {
                    TxtDetalleCalculo.Text = "0 x Hora ($0) = $0";
                    TxtTotalPagar.Text = "El cliente paga: $0";
                    return;
                }

                // Obtener precio
                var precio = _dbService.ObtenerPrecio(tarifa.Id, categoria.Id);
                decimal precioUnitario = precio?.Monto ?? 0;

                // Calcular tiempo total desde los campos de duración
                int dias = int.TryParse(TxtDias.Text, out int d) ? d : 0;
                int horas = int.TryParse(TxtHoras.Text, out int h) ? h : 0;
                int minutos = int.TryParse(TxtMinutos.Text, out int m) ? m : 0;

                // Calcular unidades según el tipo de tarifa
                decimal unidades = 0;
                string unidadTexto = tarifa.Nombre;

                switch (tarifa.Tipo)
                {
                    case TipoTarifa.PorHora:
                        // Calcular horas totales (días * 24 + horas + minutos/60)
                        unidades = dias * 24 + horas + (minutos / 60.0m);
                        if (unidades < 0) unidades = 0;
                        unidadTexto = tarifa.Nombre;
                        break;
                    case TipoTarifa.PorTurno:
                        // Calcular cuántos turnos completos caben en el tiempo
                        // Un turno = duración de la tarifa (Dias, Horas, Minutos)
                        decimal horasTurno = tarifa.Dias * 24 + tarifa.Horas + (tarifa.Minutos / 60.0m);
                        decimal horasTotales = dias * 24 + horas + (minutos / 60.0m);
                        if (horasTurno > 0)
                        {
                            unidades = Math.Ceiling(horasTotales / horasTurno);
                        }
                        else
                        {
                            unidades = 1;
                        }
                        if (unidades < 0) unidades = 0;
                        unidadTexto = tarifa.Nombre;
                        break;
                    case TipoTarifa.PorEstadia:
                        // Calcular cuántas estadías completas caben en el tiempo
                        // Una estadía = duración de la tarifa (Dias, Horas, Minutos)
                        decimal horasEstadia = tarifa.Dias * 24 + tarifa.Horas + (tarifa.Minutos / 60.0m);
                        decimal horasTotalesEstadia = dias * 24 + horas + (minutos / 60.0m);
                        if (horasEstadia > 0)
                        {
                            unidades = Math.Ceiling(horasTotalesEstadia / horasEstadia);
                        }
                        else
                        {
                            unidades = 1;
                        }
                        if (unidades < 0) unidades = 0;
                        unidadTexto = tarifa.Nombre;
                        break;
                }

                decimal total = unidades * precioUnitario;

                // Formatear unidades según el tipo
                string unidadesTexto = tarifa.Tipo == TipoTarifa.PorHora 
                    ? unidades.ToString("F2") 
                    : unidades.ToString("F0");

                // Mostrar detalle
                TxtDetalleCalculo.Text = $"{unidadesTexto} x {unidadTexto} (${precioUnitario:F2}) = ${total:F2}";
                TxtTotalPagar.Text = $"El cliente paga: ${total:F2}";
            }
            catch
            {
                TxtDetalleCalculo.Text = "0 x Hora ($0) = $0";
                TxtTotalPagar.Text = "El cliente paga: $0";
            }
        }

        private void TxtDias_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void TxtHoras_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox txt)
            {
                e.Handled = !char.IsDigit(e.Text, 0);
                if (!e.Handled && int.TryParse(txt.Text.Insert(txt.SelectionStart, e.Text), out int valor))
                {
                    if (valor > 23) e.Handled = true;
                }
            }
        }

        private void TxtMinutos_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox txt)
            {
                e.Handled = !char.IsDigit(e.Text, 0);
                if (!e.Handled && int.TryParse(txt.Text.Insert(txt.SelectionStart, e.Text), out int valor))
                {
                    if (valor > 59) e.Handled = true;
                }
            }
        }

        private void BtnDiasUp_Click(object sender, RoutedEventArgs e)
        {
            int valor = int.TryParse(TxtDias.Text, out int d) ? d : 0;
            TxtDias.Text = (valor + 1).ToString();
        }

        private void BtnDiasDown_Click(object sender, RoutedEventArgs e)
        {
            int valor = int.TryParse(TxtDias.Text, out int d) ? d : 0;
            if (valor > 0)
                TxtDias.Text = (valor - 1).ToString();
        }

        private void BtnHorasUp_Click(object sender, RoutedEventArgs e)
        {
            int horas = int.TryParse(TxtHoras.Text, out int h) ? h : 0;
            int minutos = int.TryParse(TxtMinutos.Text, out int m) ? m : 0;
            
            minutos += 1;
            if (minutos >= 60)
            {
                minutos = 0;
                horas += 1;
            }
            if (horas >= 24)
            {
                horas = 0;
            }

            TxtHoras.Text = horas.ToString("00");
            TxtMinutos.Text = minutos.ToString("00");
        }

        private void BtnHorasDown_Click(object sender, RoutedEventArgs e)
        {
            int horas = int.TryParse(TxtHoras.Text, out int h) ? h : 0;
            int minutos = int.TryParse(TxtMinutos.Text, out int m) ? m : 0;
            
            minutos -= 1;
            if (minutos < 0)
            {
                minutos = 59;
                horas -= 1;
            }
            if (horas < 0)
            {
                horas = 23;
            }

            TxtHoras.Text = horas.ToString("00");
            TxtMinutos.Text = minutos.ToString("00");
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AplicarTema(bool modoOscuro)
        {
            if (modoOscuro)
            {
                AplicarModoOscuro();
            }
            else
            {
                AplicarModoClaro();
            }
        }

        private void AplicarModoOscuro()
        {
            this.Background = Brushes.Transparent;

            // Border principal
            var borderPrincipal = FindVisualChild<Border>(this, "BorderPrincipal");
            if (borderPrincipal != null)
            {
                borderPrincipal.Background = ColorFondoOscuro;
                borderPrincipal.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 99));
                
                // Buscar el grid dentro del border principal
                var gridInterno = borderPrincipal.Child as Grid;
                if (gridInterno != null && gridInterno.Children.Count > 0)
                {
                    var headerBorder = gridInterno.Children[0] as Border;
                    if (headerBorder != null)
                    {
                        headerBorder.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                    }
                }
            }
            
            // Asegurar que el fondo del contenido sea oscuro
            var gridPrincipal = this.Content as Grid;
            if (gridPrincipal != null)
            {
                gridPrincipal.Background = Brushes.Transparent;
            }

            // ScrollViewer y contenido
            var scrollViewer = FindVisualChild<ScrollViewer>(this);
            if (scrollViewer != null)
            {
                scrollViewer.Background = ColorFondoOscuro;
            }

            // Aplicar tema a controles
            AplicarTemaControles(true);
            
            // Aplicar tema a botones de incremento
            AplicarTemaBotones(true);
        }

        private void AplicarModoClaro()
        {
            this.Background = Brushes.Transparent;

            // Border principal
            var borderPrincipal = FindVisualChild<Border>(this, "BorderPrincipal");
            if (borderPrincipal != null)
            {
                borderPrincipal.Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
                borderPrincipal.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                
                // Buscar el grid dentro del border principal
                var gridInterno = borderPrincipal.Child as Grid;
                if (gridInterno != null && gridInterno.Children.Count > 0)
                {
                    var headerBorder = gridInterno.Children[0] as Border;
                    if (headerBorder != null)
                    {
                        headerBorder.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                    }
                }
            }
            
            // Asegurar que el fondo del contenido sea claro
            var gridPrincipal = this.Content as Grid;
            if (gridPrincipal != null)
            {
                gridPrincipal.Background = Brushes.Transparent;
            }

            // ScrollViewer y contenido
            var scrollViewer = FindVisualChild<ScrollViewer>(this);
            if (scrollViewer != null)
            {
                scrollViewer.Background = ColorFondoClaro;
            }

            // Aplicar tema a controles
            AplicarTemaControles(false);
            
            // Aplicar tema a botones de incremento
            AplicarTemaBotones(false);
        }

        private void AplicarTemaControles(bool modoOscuro)
        {
            var colorFondo = modoOscuro ? ColorInputOscuro : ColorInputClaro;
            var colorTexto = modoOscuro ? ColorTextoOscuro : ColorTextoClaro;
            var colorTextoSecundario = modoOscuro ? ColorTextoSecundarioOscuro : ColorTextoSecundarioClaro;
            var colorBorde = modoOscuro ? ColorBordeOscuro : ColorBordeClaro;
            var colorFondoDetalle = modoOscuro ? new SolidColorBrush(Color.FromRgb(31, 41, 55)) : new SolidColorBrush(Color.FromRgb(243, 244, 246));
            var colorBordeDetalle = modoOscuro ? new SolidColorBrush(Color.FromRgb(55, 65, 81)) : new SolidColorBrush(Color.FromRgb(229, 231, 235));

            // ComboBoxes - aplicar colores y actualizar popup/flecha sin que queden con el tema anterior
            AplicarTemaComboBox(CmbTipoTarifa, modoOscuro, colorFondo, colorTexto, colorBorde);
            AplicarTemaComboBox(CmbCategoria, modoOscuro, colorFondo, colorTexto, colorBorde);
            AplicarTemaComboBox(CmbHoraInicio, modoOscuro, colorFondo, colorTexto, colorBorde);
            AplicarTemaComboBox(CmbMinutoInicio, modoOscuro, colorFondo, colorTexto, colorBorde);

            // DatePicker - aplicar tema mediante estilo
            if (DpFechaInicio != null)
            {
                // El DatePicker necesita un estilo personalizado para cambiar colores
                var datePickerStyle = new Style(typeof(DatePicker));
                datePickerStyle.Setters.Add(new Setter(Control.BackgroundProperty, colorFondo));
                datePickerStyle.Setters.Add(new Setter(Control.ForegroundProperty, colorTexto));
                datePickerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, colorBorde));
                DpFechaInicio.Style = datePickerStyle;
                
                // También aplicar al TextBox interno del DatePicker
                DpFechaInicio.Loaded += (s, e) =>
                {
                    var textBox = FindVisualChild<TextBox>(DpFechaInicio);
                    if (textBox != null)
                    {
                        textBox.Background = colorFondo;
                        textBox.Foreground = colorTexto;
                        textBox.BorderBrush = colorBorde;
                    }
                };
            }

            // TextBoxes
            if (TxtDias != null)
            {
                TxtDias.Background = colorFondo;
                TxtDias.Foreground = colorTexto;
                TxtDias.BorderBrush = colorBorde;
            }
            if (TxtHoras != null)
            {
                TxtHoras.Background = colorFondo;
                TxtHoras.Foreground = colorTexto;
                TxtHoras.BorderBrush = colorBorde;
            }
            if (TxtMinutos != null)
            {
                TxtMinutos.Background = colorFondo;
                TxtMinutos.Foreground = colorTexto;
                TxtMinutos.BorderBrush = colorBorde;
            }

            // TextBlocks
            CambiarColorTextosRecursivo(this, colorTexto, colorTextoSecundario);

            // Guardar estado del modo para usar en CambiarColorBorderDetalle
            modoOscuroDetalle = modoOscuro;
            
            // Border de detalle - buscar recursivamente
            CambiarColorBorderDetalle(this, colorFondoDetalle, colorBordeDetalle);
        }

        private void CambiarColorTextosRecursivo(DependencyObject parent, SolidColorBrush colorPrincipal, SolidColorBrush colorSecundario)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock)
                {
                    // No cambiar el color del título del header (blanco)
                    if (textBlock.Text == "Simulador de Tarifas")
                    {
                        textBlock.Foreground = Brushes.White;
                    }
                    // Textos descriptivos en gris secundario
                    else if (textBlock.FontStyle == FontStyles.Italic || 
                        textBlock.Name == "TxtDetalleCalculo" ||
                        textBlock.Text.Contains("Establece"))
                    {
                        textBlock.Foreground = colorSecundario;
                    }
                    // Textos principales en color principal
                    else
                    {
                        textBlock.Foreground = colorPrincipal;
                    }
                }
                
                CambiarColorTextosRecursivo(child, colorPrincipal, colorSecundario);
            }
        }
        
        private void AplicarTemaBotones(bool modoOscuro)
        {
            var colorFondo = modoOscuro ? new SolidColorBrush(Color.FromRgb(55, 65, 81)) : Brushes.White;
            var colorTexto = modoOscuro ? ColorTextoOscuro : new SolidColorBrush(Color.FromRgb(51, 65, 85));
            var colorBorde = modoOscuro ? new SolidColorBrush(Color.FromRgb(75, 85, 99)) : new SolidColorBrush(Color.FromRgb(209, 213, 219));
            
            // Buscar todos los botones de incremento/decremento
            CambiarColorBotonesRecursivo(this, colorFondo, colorTexto, colorBorde);
        }
        
        private void CambiarColorBotonesRecursivo(DependencyObject parent, SolidColorBrush fondo, SolidColorBrush texto, SolidColorBrush borde)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && (button.Content?.ToString() == "▲" || button.Content?.ToString() == "▼"))
                {
                    button.Background = fondo;
                    button.Foreground = texto;
                    button.BorderBrush = borde;
                }
                
                CambiarColorBotonesRecursivo(child, fondo, texto, borde);
            }
        }

        private bool modoOscuroDetalle = false;
        
        private void CambiarColorBorderDetalle(DependencyObject parent, SolidColorBrush fondo, SolidColorBrush borde)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Border border)
                {
                    // Buscar el border que contiene "Detalle para el Cliente"
                    var stackPanel = border.Child as StackPanel;
                    if (stackPanel != null)
                    {
                        foreach (var item in stackPanel.Children)
                        {
                            if (item is TextBlock tb && tb.Text == "Detalle para el Cliente")
                            {
                                border.Background = fondo;
                                border.BorderBrush = borde;
                                
                                // Cambiar colores de los TextBlocks dentro del detalle
                                foreach (var childItem in stackPanel.Children)
                                {
                                    if (childItem is TextBlock textBlock)
                                    {
                                        if (textBlock.Text == "Detalle para el Cliente" || textBlock.Name == "TxtTotalPagar")
                                        {
                                            // Título y total en color principal (blanco en oscuro)
                                            textBlock.Foreground = modoOscuroDetalle ? ColorTextoOscuro : new SolidColorBrush(Color.FromRgb(31, 41, 55));
                                        }
                                        else if (textBlock.Name == "TxtDetalleCalculo")
                                        {
                                            // Detalle en color secundario
                                            textBlock.Foreground = modoOscuroDetalle ? ColorTextoSecundarioOscuro : new SolidColorBrush(Color.FromRgb(100, 116, 139));
                                        }
                                    }
                                }
                                return;
                            }
                        }
                    }
                }
                
                CambiarColorBorderDetalle(child, fondo, borde);
            }
        }

        private void AplicarTemaComboBox(ComboBox? comboBox, bool modoOscuro, SolidColorBrush fondo, SolidColorBrush texto, SolidColorBrush borde)
        {
            if (comboBox == null) return;

            // Colores base
            comboBox.Background = fondo;
            comboBox.Foreground = texto;
            comboBox.BorderBrush = borde;

            // Limpiar estilos previos para evitar arrastre de modo anterior
            comboBox.ItemContainerStyle = null;

            // Override de colores de sistema para que el popup no quede azul/negro por defecto
            var hoverBg = modoOscuro ? new SolidColorBrush(Color.FromRgb(55, 65, 81)) : new SolidColorBrush(Color.FromRgb(243, 244, 246));
            var selectedBg = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            comboBox.Resources[SystemColors.HighlightBrushKey] = selectedBg;
            comboBox.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
            comboBox.Resources[SystemColors.ControlBrushKey] = fondo;
            comboBox.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = hoverBg;

            // Estilo de items del dropdown (sin borde, con hover/selected controlados)
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, fondo));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, texto));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 6, 8, 6)));
            itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, hoverBg));
            hoverTrigger.Setters.Add(new Setter(Control.ForegroundProperty, texto));
            itemStyle.Triggers.Add(hoverTrigger);

            var selectedTrigger = new Trigger { Property = System.Windows.Controls.Primitives.Selector.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, selectedBg));
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Triggers.Add(selectedTrigger);

            comboBox.ItemContainerStyle = itemStyle;

            // Actualizar partes visuales
            comboBox.ApplyTemplate();
            ActualizarPartesCombo(comboBox, fondo, texto, borde);

            comboBox.Loaded += (s, e) => ActualizarPartesCombo(comboBox, fondo, texto, borde);
        }

        private void ActualizarPartesCombo(ComboBox comboBox, SolidColorBrush fondo, SolidColorBrush texto, SolidColorBrush borde)
        {
            comboBox.ApplyTemplate();
            var toggle = comboBox.Template.FindName("ToggleButton", comboBox) as System.Windows.Controls.Primitives.ToggleButton;
            var border = comboBox.Template.FindName("Border", comboBox) as Border;
            var arrow = comboBox.Template.FindName("Arrow", comboBox) as System.Windows.Shapes.Path;
            var popup = comboBox.Template.FindName("PART_Popup", comboBox) as Popup;

            if (toggle != null)
            {
                toggle.Background = fondo;
                toggle.BorderBrush = borde;
            }
            if (border != null)
            {
                border.Background = fondo;
                border.BorderBrush = borde;
            }
            if (arrow != null)
            {
                arrow.Fill = texto;
            }
            if (popup != null && popup.Child is Border popBorder)
            {
                popBorder.Background = fondo;
                popBorder.BorderBrush = borde;
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name = "") where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    if (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name))
                    {
                        return result;
                    }
                }
                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}

