using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace WorldTime;
public sealed record CitySearchResult(City City,string Language){
 public string Title{get{string local=Catalog.CityName(City,Language);return local==City.En?local:local+" · "+City.En;}}
 public string Region=>Catalog.CountryName(City,Language)+" · "+Places033.AdminName(City,Language)+" ("+City.Code+")";
 public string Zone=>City.Iana=="Asia/Urumqi"?L10n.Text(Language,"Choose China Standard Time or Xinjiang local time explicitly."):City.Zone.Length==0?L10n.Text(Language,"Time zone unavailable; choose a verified zone manually."):City.Iana;
 public override string ToString()=>Title+" · "+Region+" · "+Zone;
}
public sealed record CivilZoneChoice(string Label,string Zone,string Iana);
// Shared, data-bound search with recycled rows; no automatic city selection.
public sealed class CitySearchControl : StackPanel {
 readonly Host host;readonly DispatcherTimer debounce=new(){Interval=TimeSpan.FromMilliseconds(100)};
 readonly SemaphoreSlim serial=new(1,1);CancellationTokenSource? pending;int generation;bool composing,closed,countriesReady,settingCountries;
 public readonly TextBox SearchBox=new(){Height=34,VerticalContentAlignment=VerticalAlignment.Center};
 public readonly ComboBox CountrySelector=new(){DisplayMemberPath="Label",Height=28,Margin=new Thickness(0,6,0,8)};
 public readonly ListBox CityList=new(){Height=284};
 public readonly TextBlock Results=new(){Margin=new Thickness(0,8,0,8),TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray};
 public readonly ComboBox ZoneSelector=new(){DisplayMemberPath="Id",Height=28};
 public readonly TextBox ManualName=new(){MaxLength=80,Height=28};
 public readonly Expander Manual=new();
 public readonly ComboBox CivilZone=new(){DisplayMemberPath="Label",Height=30,Margin=new Thickness(0,0,0,8),Visibility=Visibility.Collapsed};
 public event Action? SelectionChanged;public event Action? ConfirmRequested;
 public City? SelectedCity=>(CityList.SelectedItem as CitySearchResult)?.City;
 public bool CanAdd=>!composing&&(SelectedCity is {Zone.Length:>0} city&&(city.Iana!="Asia/Urumqi"||CivilZone.SelectedItem is CivilZoneChoice)||ZoneSelector.SelectedItem is TimeZoneInfo);
 public Task LastSearch{get;private set;}=Task.CompletedTask;
 public CitySearchControl(Host host){
  this.host=host;
  Children.Add(new TextBlock{Text=host.T("Search cities, countries or regions"),Margin=new Thickness(0,0,0,5)});Children.Add(SearchBox);
  System.Windows.Automation.AutomationProperties.SetName(SearchBox,host.T("Search cities, countries or regions"));SearchBox.ToolTip=host.T("Try a few letters, another language, pinyin or an alias.");
  CountrySelector.ItemsSource=new[]{new CountryChoice("",host.T("All countries and regions"))}.Concat(Catalog.Countries(host.S.Language)).ToArray();CountrySelector.SelectedIndex=0;
  Children.Add(CountrySelector);Children.Add(CityList);Children.Add(Results);Children.Add(CivilZone);
  CivilZone.ItemsSource=new[]{new CivilZoneChoice(host.T("China Standard Time (UTC+8)"),"China Standard Time","Asia/Shanghai"),new CivilZoneChoice(host.T("Xinjiang local time (UTC+6)"),"Central Asia Standard Time","Asia/Urumqi")};CivilZone.SelectionChanged+=(_,_)=>SelectionChanged?.Invoke();
  VirtualizingPanel.SetIsVirtualizing(CityList,true);VirtualizingPanel.SetVirtualizationMode(CityList,VirtualizationMode.Recycling);ScrollViewer.SetCanContentScroll(CityList,true);
  CityList.ItemsPanel=new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
  var root=new FrameworkElementFactory(typeof(StackPanel));root.SetValue(MarginProperty,new Thickness(5,5,5,7));
  foreach(var field in new[]{"Title","Region","Zone"}){var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding(field));text.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);if(field=="Title")text.SetValue(TextBlock.FontWeightProperty,FontWeights.SemiBold);else{text.SetValue(TextBlock.FontSizeProperty,12d);text.SetValue(TextBlock.ForegroundProperty,Brushes.SlateGray);}root.AppendChild(text);}
  CityList.ItemTemplate=new DataTemplate{VisualTree=root};
  Manual.Header=host.T("Can't find a place? Add manually");var manualPanel=new StackPanel();manualPanel.Children.Add(new TextBlock{Text=host.T("Display name"),Margin=new Thickness(0,8,0,4)});manualPanel.Children.Add(ManualName);manualPanel.Children.Add(new TextBlock{Text=host.T("Or choose a Windows time zone"),Margin=new Thickness(0,8,0,4)});manualPanel.Children.Add(ZoneSelector);ZoneSelector.ItemsSource=TimeZoneInfo.GetSystemTimeZones();Manual.Content=manualPanel;Children.Add(Manual);
  Children.Add(new TextBlock{Text=host.T("Offline place data: GeoNames (CC BY 4.0). Names are not available in every language."),FontSize=11,Foreground=Brushes.SlateGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,0)});
  CityList.SelectionChanged+=(_,_)=>{CivilZone.SelectedIndex=-1;CivilZone.Visibility=SelectedCity?.Iana=="Asia/Urumqi"?Visibility.Visible:Visibility.Collapsed;if(SelectedCity!=null)ZoneSelector.SelectedIndex=-1;SelectionChanged?.Invoke();};ZoneSelector.SelectionChanged+=(_,_)=>{if(ZoneSelector.SelectedItem!=null){CityList.SelectedIndex=-1;Manual.IsExpanded=true;}SelectionChanged?.Invoke();};
  SearchBox.TextChanged+=(_,_)=>Queue();CountrySelector.SelectionChanged+=(_,_)=>Queue();
  TextCompositionManager.AddPreviewTextInputStartHandler(SearchBox,(_,_)=>{composing=true;pending?.Cancel();debounce.Stop();ClearSelection();});
  TextCompositionManager.AddPreviewTextInputUpdateHandler(SearchBox,(_,_)=>{composing=true;});TextCompositionManager.AddPreviewTextInputHandler(SearchBox,(_,_)=>{composing=false;Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(Queue));});
  SearchBox.PreviewKeyDown+=(_,e)=>{
   if(composing||e.ImeProcessedKey!=Key.None)return;
   if(e.Key is Key.Down or Key.Up){if(CityList.Items.Count>0){CityList.SelectedIndex=Math.Clamp(CityList.SelectedIndex+(e.Key==Key.Down?1:-1),0,CityList.Items.Count-1);CityList.ScrollIntoView(CityList.SelectedItem);}e.Handled=true;}
   else if(e.Key==Key.Enter){if(CanAdd)ConfirmRequested?.Invoke();e.Handled=true;}
   else if(e.Key==Key.Escape){ClearSelection();pending?.Cancel();debounce.Stop();CityList.ItemsSource=null;Results.Text=host.T("Type to search again.");e.Handled=true;}
  };
  CityList.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Enter&&CanAdd){ConfirmRequested?.Invoke();e.Handled=true;}};
  CityList.MouseDoubleClick+=(_,e)=>{if(CanAdd&&e.OriginalSource is DependencyObject d&&ItemsControl.ContainerFromElement(CityList,d) is ListBoxItem)ConfirmRequested?.Invoke();};
  debounce.Tick+=(_,_)=>{debounce.Stop();LastSearch=RunSearch();};Queue();
 }
 void ClearSelection(){CityList.SelectedIndex=-1;ZoneSelector.SelectedIndex=-1;SelectionChanged?.Invoke();}
 void Queue(){if(closed||settingCountries)return;generation++;pending?.Cancel();debounce.Stop();ClearSelection();CityList.ItemsSource=null;Results.Text=host.T("Searching offline…");if(!composing)debounce.Start();}
 public Task RefreshAsync(){debounce.Stop();return LastSearch=RunSearch();}
 async Task RunSearch(){
  if(closed||composing)return;pending?.Cancel();var cts=new CancellationTokenSource();pending=cts;int current=++generation;string query=SearchBox.Text,country=(CountrySelector.SelectedItem as CountryChoice)?.Code??"";
  try{
   await serial.WaitAsync(cts.Token);City[] cities;
   try{cities=await Task.Run(()=>Places033.SearchAsync(query,country,cts.Token),cts.Token);}finally{serial.Release();}
   if(closed||current!=generation||cts.IsCancellationRequested)return;
   if(!countriesReady){var choices=await Places033.CountriesAsync(host.S.Language);if(closed||current!=generation||cts.IsCancellationRequested)return;settingCountries=true;CountrySelector.ItemsSource=new[]{new CountryChoice("",host.T("All countries and regions"))}.Concat(choices).ToArray();CountrySelector.SelectedItem=CountrySelector.Items.Cast<CountryChoice>().FirstOrDefault(c=>c.Code==country);settingCountries=false;countriesReady=true;}
   CityList.ItemsSource=cities.Select(c=>new CitySearchResult(c,host.S.Language)).ToArray();CityList.SelectedIndex=-1;
   Results.Text=cities.Length==0?host.T("No matching places. Try another language, a shorter name, or add manually."):string.Format(host.T("{0:N0} matches · scroll for all results · select a place to add"),cities.Length);SelectionChanged?.Invoke();
  }catch(OperationCanceledException){}catch(Exception){if(!closed&&current==generation)Results.Text=host.T("Offline place data could not be loaded. Manual time zone selection is still available.");}
  finally{if(ReferenceEquals(pending,cts))pending=null;cts.Dispose();}
 }
 public Row? MakeSelection(){
  if(!CanAdd)return null;if(SelectedCity is City city){var row=host.Make(city);if(city.Iana=="Asia/Urumqi"&&CivilZone.SelectedItem is CivilZoneChoice choice){row.Zone=choice.Zone;row.Iana=choice.Iana;}return row;}
  if(ZoneSelector.SelectedItem is TimeZoneInfo zone)return new Row{City=ManualName.Text.Trim().Length>0?ManualName.Text.Trim():zone.Id,Zone=zone.Id,Code=(CountrySelector.SelectedItem as CountryChoice)?.Code is string c&&c.Length>0?c:"◎"};return null;
 }
 public void Stop(){closed=true;generation++;debounce.Stop();pending?.Cancel();}
}
