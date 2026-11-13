// Zen Snip & OCR - Ekran Alıntısı, Resimden Yazı Kopyalama & Renk Damlalığı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ZenSnip
{
    public static class OcrEngineRunner
    {
        public static string RecognizeText(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            string tempFile = Path.Combine(Path.GetTempPath(), "zensnip_ocr_" + Guid.NewGuid().ToString("N") + ".png");

            try
            {
                bitmap.Save(tempFile, ImageFormat.Png);

                string script = string.Format(@"
[Windows.Media.Ocr.OcrEngine, Windows.Foundation.UniversalApiContract, ContentType = WindowsRuntime] > $null
[Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation.UniversalApiContract, ContentType = WindowsRuntime] > $null
[Windows.Storage.StorageFile, Windows.Foundation.UniversalApiContract, ContentType = WindowsRuntime] > $null
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {{ $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' }} | Select-Object -First 1

function Await($asyncOp, $type) {{
    $method = $asTask.MakeGenericMethod($type)
    $task = $method.Invoke($null, @($asyncOp))
    $task.Wait()
    return $task.Result
}}

$file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync('{0}')) ([Windows.Storage.StorageFile])
$stream = Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
$decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
$sBmp = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if ($engine -ne $null) {{
    $result = Await ($engine.RecognizeAsync($sBmp)) ([Windows.Media.Ocr.OcrResult])
    if ($result -ne $null -and $result.Text) {{
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        Write-Output $result.Text
    }}
}}
", tempFile.Replace("'", "''"));

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "\\\"") + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(4000);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        return output.Trim();
                    }
                }
            }
            catch { }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }

            return null;
        }
    }
}
