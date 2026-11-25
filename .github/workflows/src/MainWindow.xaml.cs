using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace LataBoost;
public partial class MainWindow : Window {
 record Tweak(string Path,string Name,int Value,string Label);
 record Saved(string Path,string Name,bool Exists,int Value);
 static readonly string Home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LataBoost");
 // Preserve existing backups from the previous brand, without overwriting newer backups.
 static void MigrateBackup(){var old=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JarvisBoost");foreach(var name in new[]{"backup.json","power-backup.txt"}){var source=Path.Combine(old,name);var target=Path.Combine(Home,name);if(File.Exists(source)&&!File.Exists(target)){Directory.CreateDirectory(Home);File.Copy(source,target,false);}}}
 static readonly string Backup=Path.Combine(Home,"backup.json");
 static readonly Tweak[] Tweaks={new(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",0,"Desativar transparência"),new(@"System\GameConfigStore","GameDVR_Enabled",0,"Desativar captura Game DVR"),new(@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AppCaptureEnabled",0,"Desativar captura de aplicativos"),new(@"Software\Microsoft\GameBar","AutoGameModeEnabled",1,"Ativar modo de jogo")};
 public MainWindow(){InitializeComponent(); try{MigrateBackup();}catch(Exception ex){MessageBox.Show("Não foi possível importar backup anterior: "+ex.Message);} Overview(this,new RoutedEventArgs());}
 void Heading(string title,string subtitle){StopThermal();Panel.Children.Clear();Panel.Children.Add(new TextBlock{Text=title,FontSize=30,Foreground=new SolidColorBrush(Color.FromRgb(85,217,239)),Margin=new Thickness(0,0,0,10)}); Say(subtitle);}
 void Say(string s){Panel.Children.Add(new TextBlock{Text=s,Margin=new Thickness(0,5,0,10),FontSize=15});}
 void Action(string label,System.Action a){var b=new Button{Content=label};b.Click+=(_,_)=>{try{a();}catch(Exception ex){MessageBox.Show(ex.Message,"LATA BOOST");}};Panel.Children.Add(b);}
 void Overview(object s,RoutedEventArgs e){Heading("Central de desempenho","Diagnóstico local • nenhuma alteração automática"); Say($"Windows: {Environment.OSVersion}\nProcessadores lógicos: {Environment.ProcessorCount}\nArquitetura: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}\nCPU: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}");foreach(var d in DriveInfo.GetDrives().Where(d=>d.IsReady&&d.DriveType==DriveType.Fixed))Say($"{d.Name} — {d.AvailableFreeSpace/1073741824.0:F1} GB livres de {d.TotalSize/1073741824.0:F1} GB");Say("Nenhum serviço residente. Feche o programa após aplicar os ajustes.");}
 List<Saved> ReadBackup()=>File.Exists(Backup)?JsonSerializer.Deserialize<List<Saved>>(File.ReadAllText(Backup))??throw new IOException("Backup inválido"):new();
 void Save(List<Saved> data){Directory.CreateDirectory(Home);File.WriteAllText(Backup+".tmp",JsonSerializer.Serialize(data));File.Move(Backup+".tmp",Backup,true);}
 void Apply(Tweak t){if(MessageBox.Show(t.Label+"? O valor original será salvo.","Confirmar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;var data=ReadBackup();using var k=Registry.CurrentUser.OpenSubKey(t.Path);var old=k?.GetValue(t.Name);if(old!=null&&old is not int)throw new InvalidOperationException("Tipo de registro não suportado.");if(!data.Any(x=>x.Path==t.Path&&x.Name==t.Name)){data.Add(new(t.Path,t.Name,old!=null,old is int i?i:0));Save(data);}using var w=Registry.CurrentUser.CreateSubKey(t.Path);w.SetValue(t.Name,t.Value,RegistryValueKind.DWord);MessageBox.Show("Seeeeco Seco!\nAjuste aplicado. Pode exigir novo login ou reinício do aplicativo afetado.");}
 void Optimize(object s,RoutedEventArgs e){Heading("Otimizações","Ajustes individuais reversíveis. Ganhos variam conforme o gargalo.");foreach(var t in Tweaks)Action(t.Label,()=>Apply(t));Action("Abrir aplicativos de inicialização",()=>Open("ms-settings:startupapps"));Action("Abrir configurações de energia",()=>Open("ms-settings:powersleep"));Action("Abrir armazenamento",()=>Open("ms-settings:storagesense"));}
 static void Open(string uri)=>Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});
 static readonly string PowerBackup=Path.Combine(Home,"power-backup.txt");
 List<HardwareScan.Section>? inventory;
 async void Hardware(object s,RoutedEventArgs e){
  Heading("Diagnóstico e compatibilidade","Inventário local via CIM/WMI. Nenhuma alteração automática. Identificar um componente não prova que ele aceita ajustes físicos.");
  var page=new StackPanel();Panel.Children.Add(page);
  var status=new TextBlock{Text="Analisando a Lata...",Margin=new Thickness(0,8,0,8)};page.Children.Add(status);
  try{inventory??=await HardwareScan.Scan();var classification=await HardwareScan.Classify();status.Text=classification+" — Leitura concluída. Dados ausentes são tratados como desconhecidos.";
   foreach(var item in inventory){var exp=new Expander{Header=item.Name,Foreground=Foreground,Margin=new Thickness(0,5,0,5),Content=new TextBlock{Text=item.Data,Margin=new Thickness(10),TextWrapping=TextWrapping.Wrap}};page.Children.Add(exp);}
   var refresh=new Button{Content="Refazer diagnóstico"};refresh.Click+=(_,_)=>{inventory=null;Hardware(this,new RoutedEventArgs());};page.Children.Add(refresh);
  }catch(Exception ex){status.Text=ex.Message;}
  var thermalButton=new Button{Content="Abrir monitor térmico"};thermalButton.Click+=(_,_)=>Thermal(this,new RoutedEventArgs());page.Children.Add(thermalButton);
  foreach(var feature in new[]{"CPU: tensão / PL1 / PL2 / multiplicador","GPU: tensão / clock / limite de potência","RAM: tensão / timings / XMP / EXPO","Fans: PWM / curvas / tensão"}){page.Children.Add(new Button{Content=feature+" — bloqueado: backend não implementado",IsEnabled=false});}
 }
 async void Energy(object s,RoutedEventArgs e){
  Heading("Energia e CPU","Somente planos existentes e detectados no Windows. Alterar plano não altera diretamente tensão ou limites físicos.");
  var page=new StackPanel();Panel.Children.Add(page);var status=new TextBlock{Text="Consultando planos…"};page.Children.Add(status);
  try{var plans=await HardwareScan.Plans();var active=await HardwareScan.Active();status.Text="Plano ativo: "+active;
   foreach(var plan in plans){var b=new Button{Content=plan.Label,IsEnabled=plan.Id!=active};page.Children.Add(b);b.Click+=async (_,_)=>{if(MessageBox.Show("Ativar este plano? Pode aumentar consumo e temperatura.","Confirmar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;page.IsEnabled=false;try{Directory.CreateDirectory(Home);if(!File.Exists(PowerBackup))File.WriteAllText(PowerBackup,(await HardwareScan.Active()).ToString());await HardwareScan.SetPlan(plan.Id);MessageBox.Show("Seeeeco Seco! Plano confirmado pelo Windows.");Energy(this,new RoutedEventArgs());}catch(Exception ex){MessageBox.Show(ex.Message);}finally{page.IsEnabled=true;}};}
  }catch(Exception ex){status.Text="Controle bloqueado: "+ex.Message;}
 }
 async void RestorePower(){try{if(!File.Exists(PowerBackup)){MessageBox.Show("Não há plano salvo.");return;}if(MessageBox.Show("Restaurar plano de energia original?","Confirmar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;await HardwareScan.SetPlan(Guid.Parse(File.ReadAllText(PowerBackup)));File.Delete(PowerBackup);MessageBox.Show("Plano restaurado.");}catch(Exception ex){MessageBox.Show(ex.Message);}}
 void SystemTools(object s,RoutedEventArgs e){Heading("Ferramentas do Windows","Abertura de ferramentas oficiais. Nenhum ajuste destrutivo ou automático.");Action("GPU — preferências gráficas",()=>Open("ms-settings:display-advancedgraphics"));Action("RAM — memória virtual",()=>Open("SystemPropertiesPerformance.exe"));Action("Discos — otimizar unidades",()=>Open("dfrgui.exe"));Action("Rede — diagnóstico e configurações",()=>Open("ms-settings:network-status"));Action("Aplicativos instalados",()=>Open("ms-settings:appsfeatures"));Action("Inicialização",()=>Open("ms-settings:startupapps"));}
 void Processes(object s,RoutedEventArgs e){Heading("Processos","Snapshot de memória residente. Nenhum processo é encerrado automaticamente.");foreach(var p in Process.GetProcesses().Select(p=>{using(p){try{return (Name:p.ProcessName,Bytes:p.WorkingSet64);}catch{return (Name:"",Bytes:0L);}}}).OrderByDescending(p=>p.Bytes).Take(15))Say($"{p.Name} — {p.Bytes/1048576.0:F1} MB");Action("Atualizar",()=>Processes(this,new RoutedEventArgs()));}
 async void Benchmark(object s,RoutedEventArgs e){Heading("Benchmark","Teste SHA-256 de uma thread, por três segundos. Não representa FPS.");var b=new Button{Content="Executar teste"};Panel.Children.Add(b);var result=new TextBlock{Margin=new Thickness(0,10,0,0)};Panel.Children.Add(result);b.Click+=async (_,_)=>{b.IsEnabled=false;try{var score=await Task.Run(()=>{var bytes=new byte[65536];var sw=Stopwatch.StartNew();long n=0;while(sw.Elapsed.TotalSeconds<3){System.Security.Cryptography.SHA256.HashData(bytes);n+=bytes.Length;}return n/1048576.0/sw.Elapsed.TotalSeconds;});result.Text=$"Resultado: {score:F1} MiB/s. Compare sob condições iguais.";}catch(Exception ex){result.Text=ex.Message;}finally{b.IsEnabled=true;}};await Task.CompletedTask;}
 void Restore(object s,RoutedEventArgs e){Heading("Restauração","Reverte somente os valores alterados pelo LATA para esta conta. Restaure antes de desinstalar.");Action("Restaurar plano de energia",RestorePower);Action("Restaurar ajustes de registro",()=>{if(MessageBox.Show("Restaurar valores originais?","Confirmar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;var data=ReadBackup();foreach(var x in data.ToArray()){using var k=Registry.CurrentUser.CreateSubKey(x.Path);if(x.Exists)k.SetValue(x.Name,x.Value,RegistryValueKind.DWord);else k.DeleteValue(x.Name,false);data.Remove(x);Save(data);}MessageBox.Show("Restauração concluída.");});}
 void Support(object s,RoutedEventArgs e){Heading("Apoie o projeto","Software gratuito. Apoio voluntário, sem funções bloqueadas.");var file=Path.Combine(AppContext.BaseDirectory,"pix.txt");var pix=File.Exists(file)?File.ReadAllText(file).Trim():"";if(string.IsNullOrWhiteSpace(pix)){Say("Chave Pix ainda não configurada pelo mantenedor.");return;}Say(pix);Action("Copiar chave Pix",()=>Clipboard.SetText(pix));}
}
