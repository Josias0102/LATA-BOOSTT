using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
namespace LataBoost;
internal static class HardwareScan {
 internal record Section(string Name,string Data);
 internal record Plan(Guid Id,string Label);
 internal static async Task<string> Run(string exe, params string[] args) {
  using var p=new Process{StartInfo=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,exe)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
  foreach(var a in args)p.StartInfo.ArgumentList.Add(a);
  if(!p.Start())throw new IOException("Não foi possível iniciar a ferramenta do Windows.");
  var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(25));
  try{await p.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){try{p.Kill(true);}catch{}throw new TimeoutException("Tempo limite da consulta.");}
  var output=await stdout;var error=await stderr;
  if(p.ExitCode!=0)throw new IOException(string.IsNullOrWhiteSpace(error)?$"Falha ({p.ExitCode}): {output}":error);
  return output.Trim();
 }
 internal static async Task<List<Section>> Scan(){
  var queries=new (string,string)[]{
   ("Computador / placa-mãe","Get-CimInstance Win32_ComputerSystem | Select Manufacturer,Model,SystemType,TotalPhysicalMemory; Get-CimInstance Win32_BaseBoard | Select Manufacturer,Product"),
   ("BIOS / firmware","Get-CimInstance Win32_BIOS | Select Manufacturer,SMBIOSBIOSVersion,ReleaseDate"),
   ("Processador","Get-CimInstance Win32_Processor | Select Name,Manufacturer,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed,CurrentClockSpeed"),
   ("GPU / driver","Get-CimInstance Win32_VideoController | Select Name,DriverVersion,VideoProcessor,Status"),
   ("Memória física","Get-CimInstance Win32_PhysicalMemory | Select BankLabel,Capacity,Speed,ConfiguredClockSpeed,Manufacturer,PartNumber"),
   ("Armazenamento","Get-CimInstance Win32_DiskDrive | Select Model,Size,InterfaceType,Status; Get-PhysicalDisk | Select FriendlyName,MediaType,HealthStatus,OperationalStatus"),
   ("Rede","Get-CimInstance Win32_NetworkAdapter | Where-Object {$_.PhysicalAdapter} | Select Name,Speed,NetEnabled"),
   ("Bateria / refrigeração","Get-CimInstance Win32_Battery | Select Name,BatteryStatus,EstimatedChargeRemaining; Get-CimInstance Win32_Fan | Select Name,Status,DesiredSpeed"),
   ("Windows / memória virtual","Get-CimInstance Win32_OperatingSystem | Select Caption,Version,BuildNumber,FreePhysicalMemory,TotalVisibleMemorySize; Get-CimInstance Win32_ComputerSystem | Select AutomaticManagedPagefile; Get-CimInstance Win32_PageFileUsage | Select Name,AllocatedBaseSize,CurrentUsage")};
  var result=new List<Section>();
  foreach(var (name,query) in queries){try{var script="$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; & { "+query+" } | Format-List | Out-String -Width 160"; var data=await Run(@"WindowsPowerShell\v1.0\powershell.exe","-NoProfile","-NonInteractive","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));result.Add(new(name,string.IsNullOrWhiteSpace(data)?"Nenhum dado exposto pelo Windows.":data));}catch(Exception ex){result.Add(new(name,"Consulta indisponível: "+ex.Message));}}
  return result;
 }
 internal static async Task<string> Classify(){
  try{
   var script="$ErrorActionPreference='Stop'; $c=Get-CimInstance Win32_ComputerSystem; $p=Get-CimInstance Win32_Processor; $cores=($p | Measure-Object NumberOfCores -Sum).Sum; if($null -eq $c.TotalPhysicalMemory -or $cores -le 0){throw 'Dados ausentes'}; if($c.TotalPhysicalMemory -lt 12GB -or $cores -lt 4){'Latinha Seca'}else{'Latinha Nutela'}";
   var result=await Run(@"WindowsPowerShell\v1.0\powershell.exe","-NoProfile","-NonInteractive","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
   return result is "Latinha Seca" or "Latinha Nutela" ? result : "Lata não classificada";
  }catch{return "Lata não classificada";}
 }
 internal static Guid ParseGuid(string text){var m=Regex.Match(text,@"\b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\b");return m.Success?Guid.Parse(m.Value):throw new IOException("GUID não encontrado.");}
 internal static async Task<List<Plan>> Plans(){var output=await Run("powercfg.exe","/list");var list=new List<Plan>();foreach(var line in output.Split('\n')){try{var id=ParseGuid(line);list.Add(new(id,line.Trim()));}catch(IOException){}}return list;}
 internal static async Task<Guid> Active()=>ParseGuid(await Run("powercfg.exe","/getactivescheme"));
 internal static async Task SetPlan(Guid id){if(!(await Plans()).Any(p=>p.Id==id))throw new IOException("Plano indisponível.");await Run("powercfg.exe","/setactive",id.ToString());if(await Active()!=id)throw new IOException("O Windows não confirmou a alteração.");}
}
