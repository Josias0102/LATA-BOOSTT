using LibreHardwareMonitor.Hardware;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LataBoost;

public partial class MainWindow
{
    private CancellationTokenSource? thermalSession;
    private Task thermalWorker = Task.CompletedTask;
    private record SensorReading(string Id, string Device, string Name, SensorType Type, float? Value);

    private void StopThermal() => thermalSession?.Cancel();

    private async void Thermal(object sender, RoutedEventArgs e)
    {
        Heading("Monitor térmico", "Sensores reais • atualização a cada 2 segundos • somente leitura");
        var status = new TextBlock { Text = "Monitor parado. Clique em iniciar.", Margin = new Thickness(0, 8, 0, 8) };
        var controls = new StackPanel { Orientation = Orientation.Horizontal };
        var start = new Button { Content = "Iniciar / reiniciar" };
        var stop = new Button { Content = "Pausar", IsEnabled = false, Margin = new Thickness(10,4,0,4) };
        controls.Children.Add(start); controls.Children.Add(stop); Panel.Children.Add(controls);
        var admin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        Say(admin ? "Permissão administrativa detectada. O acesso ainda depende do driver e do firmware." : "Modo sem administrador. Alguns sensores exigem elevação e driver compatível.");
        Say("Alertas de referência: 80 °C e 90 °C para CPU/GPU. Não representam limites oficiais do componente. Nenhum ajuste automático.");
        Panel.Children.Add(status);
        var rows = new StackPanel(); Panel.Children.Add(rows);
        stop.Click += (_, _) => { StopThermal(); stop.IsEnabled = false; status.Text = "Pausando…"; };
        async Task Start()
        {
            start.IsEnabled = false;
            try { await thermalWorker; } catch { /* Previous session reports its own error. */ }
            if (!Panel.Children.Contains(rows)) return;
            var session = new CancellationTokenSource(); thermalSession = session;
            stop.IsEnabled = true; rows.Children.Clear(); status.Text = "Detectando sensores…";
            var history = new Dictionary<string, (float Min, float Max)>();
            var progress = new Progress<List<SensorReading>>(readings =>
            {
                if (session.IsCancellationRequested || thermalSession != session || !Panel.Children.Contains(rows)) return;
                rows.Children.Clear();
                status.Text = $"Última leitura: {DateTime.Now:HH:mm:ss} • {readings.Count(r => r.Value.HasValue)} sensores com dados";
                if (readings.Count == 0 || !readings.Any(r => r.Value.HasValue))
                    status.Text += "\nNenhum dado disponível. Verifique permissões, driver e compatibilidade. Não desative proteções do Windows.";
                foreach (var group in readings.GroupBy(r => r.Device))
                {
                    rows.Children.Add(new TextBlock { Text = group.Key, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, Margin = new Thickness(0,12,0,4) });
                    foreach (var r in group)
                    {
                        string unit = r.Type switch { SensorType.Temperature => "°C", SensorType.Fan => "RPM", SensorType.Load => "%", SensorType.Clock => "MHz", SensorType.Power => "W", SensorType.Voltage => "V", _ => "" };
                        string value = "Indisponível"; var color = Foreground;
                        if (r.Value is float v)
                        {
                            var h = history.TryGetValue(r.Id, out var old) ? (Math.Min(old.Min,v), Math.Max(old.Max,v)) : (v,v);
                            history[r.Id] = h;
                            value = $"{v:F1} {unit}   |   mín. {h.Item1:F1}   máx. {h.Item2:F1}";
                            if (r.Type == SensorType.Temperature && (r.Device.StartsWith("Cpu:") || r.Device.StartsWith("Gpu")))
                            {
                                if (v >= 90) { color = Brushes.OrangeRed; value += " • temperatura elevada"; }
                                else if (v >= 80) { color = Brushes.Gold; value += " • atenção"; }
                            }
                        }
                        rows.Children.Add(new TextBlock { Text = $"{r.Name} — {value}", Foreground = color, Margin = new Thickness(8,2,0,2), TextWrapping = TextWrapping.Wrap });
                    }
                }
            });
            thermalWorker = Task.Run(() => ReadSensors(progress, session.Token));
            try { await thermalWorker; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { status.Text = "Falha de leitura: " + ex.Message; rows.Children.Clear(); }
            finally
            {
                if (thermalSession == session) thermalSession = null;
                if (session.IsCancellationRequested) { status.Text = "Monitor pausado. Valores anteriores ocultados."; rows.Children.Clear(); }
                session.Dispose(); start.IsEnabled = true; stop.IsEnabled = false;
            }
        }
        start.Click += async (_, _) => await Start();
        await Start();
    }

    private static async Task ReadSensors(IProgress<List<SensorReading>> progress, CancellationToken token)
    {
        var computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true, IsMemoryEnabled = true, IsStorageEnabled = true, IsControllerEnabled = true };
        try
        {
            token.ThrowIfCancellationRequested();
            computer.Open();
            while (!token.IsCancellationRequested)
            {
                var list = new List<SensorReading>();
                foreach (var hardware in computer.Hardware) Collect(hardware, list, "", token);
                progress.Report(list);
                await Task.Delay(2000, token).ConfigureAwait(false);
            }
        }
        finally { computer.Close(); }
    }

    private static void Collect(IHardware hardware, List<SensorReading> list, string parent, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        hardware.Update();
        string label = string.IsNullOrEmpty(parent) ? $"{hardware.HardwareType}: {hardware.Name}" : $"{parent} / {hardware.Name}";
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType is not (SensorType.Temperature or SensorType.Fan or SensorType.Load or SensorType.Clock or SensorType.Power or SensorType.Voltage)) continue;
            float? v = sensor.Value;
            if (v.HasValue && !float.IsFinite(v.Value)) v = null;
            list.Add(new(sensor.Identifier.ToString(), label, sensor.Name, sensor.SensorType, v));
        }
        foreach (var child in hardware.SubHardware) Collect(child, list, label, token);
    }

    protected override void OnClosed(EventArgs e)
    {
        StopThermal();
        base.OnClosed(e);
    }
}
