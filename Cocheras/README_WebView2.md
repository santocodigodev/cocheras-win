# Configuración de WebView2 y Solución de Errores de JavaScript

## Estado Actual

La aplicación **ya está usando WebView2** (Microsoft Edge/Chromium) en lugar del control WebBrowser antiguo (basado en IE). Esto resuelve automáticamente los problemas de compatibilidad con JavaScript moderno.

## Paquete NuGet Requerido

El paquete ya está instalado en el proyecto:
- **Microsoft.Web.WebView2** versión 1.0.2903.40

## Configuración Implementada

### 1. Supresión de Errores de Script

La aplicación está configurada para suprimir completamente los errores de JavaScript:

```csharp
// En PantallaClienteWindow.xaml.cs
settings.AreDefaultScriptDialogsEnabled = false; // Suprimir diálogos de script
WebViewPantalla.CoreWebView2.ScriptDialogOpening += (s, args) =>
{
    args.Handled = true; // Suprimir todos los diálogos
};
```

### 2. Manejo de Localhost vs IP

La aplicación convierte automáticamente las IPs locales a `localhost` para evitar problemas de zona de seguridad de Intranet:

- IPs privadas (192.168.x.x, 10.x.x.x, 172.16-31.x.x) se convierten a `localhost`
- Esto evita problemas de seguridad de Internet Explorer/Edge

### 3. Inicialización Correcta

WebView2 se inicializa correctamente antes de cargar cualquier URL:

```csharp
if (WebViewPantalla.CoreWebView2 == null)
{
    await WebViewPantalla.EnsureCoreWebView2Async();
}
```

## Si Necesitas Volver a WebBrowser (No Recomendado)

Si por alguna razón necesitas usar el control WebBrowser antiguo, puedes usar el helper incluido:

### Opción 1: Código C# (Requiere permisos de administrador)

```csharp
using Cocheras.Helpers;

// Configurar emulación IE11
InternetExplorerEmulationHelper.SetIE11Emulation();
```

### Opción 2: Archivo .reg

1. Edita el archivo `ConfigurarIE11Emulacion.reg`
2. Reemplaza "Cocheras.exe" con el nombre real de tu ejecutable
3. Haz doble clic en el archivo para aplicar
4. Reinicia la aplicación

### Opción 3: Configurar WebBrowser directamente (Solo si usas WebBrowser, no WebView2)

Si usas el control WebBrowser antiguo (System.Windows.Controls.WebBrowser), agrega esta propiedad:

```csharp
// Solo para System.Windows.Controls.WebBrowser (NO para WebView2)
webBrowser.ScriptErrorsSuppressed = true;
```

**Nota:** WebView2 NO tiene esta propiedad porque usa un motor moderno que no muestra errores de script por defecto cuando `AreDefaultScriptDialogsEnabled = false`.

## Verificación

Para verificar que WebView2 está funcionando correctamente:

1. Abre la aplicación
2. Abre la ventana de pantalla cliente
3. Verifica en la consola de debug que aparezca: "WebView2 inicializado correctamente con supresión de errores"
4. No deberían aparecer errores de JavaScript en la interfaz

## Solución de Problemas

### Error: "WebView2 runtime no encontrado"

WebView2 requiere el runtime de Microsoft Edge. Descárgalo desde:
https://developer.microsoft.com/microsoft-edge/webview2/

### Error: "No se puede inicializar WebView2"

1. Verifica que el paquete NuGet esté instalado
2. Asegúrate de que el runtime de WebView2 esté instalado
3. Revisa los logs de debug para más detalles

### Los errores de JavaScript siguen apareciendo

1. Verifica que `AreDefaultScriptDialogsEnabled = false`
2. Verifica que el evento `ScriptDialogOpening` esté manejado
3. Revisa la consola de debug para ver si hay errores de inicialización

## Ventajas de WebView2 sobre WebBrowser

- ✅ Motor moderno (Chromium) compatible con JavaScript ES6+
- ✅ Mejor rendimiento
- ✅ Mejor seguridad
- ✅ Soporte para tecnologías web modernas
- ✅ Actualizaciones automáticas del motor
- ✅ No requiere configuración del registro
