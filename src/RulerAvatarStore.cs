using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorldTime;
public sealed record RulerAvatarChoice(int Preset=-1,string File="");
public sealed class RulerAvatarStore {
 readonly string directory,manifest;Dictionary<string,RulerAvatarChoice> choices=new();readonly Dictionary<string,ImageSource> cache=new();
 public string LoadError {get;private set;}="";bool writable=true;
 static bool Valid(RulerAvatarChoice c)=>c.Preset>=-1&&c.Preset<48&&(c.File.Length==0||Regex.IsMatch(c.File,"^[A-F0-9]{64}\\.png$"));
 public RulerAvatarStore(string root){directory=Path.Combine(root,"avatars");manifest=Path.Combine(root,"ruler-avatars.json");try{if(System.IO.File.Exists(manifest)){var data=JsonSerializer.Deserialize<Dictionary<string,RulerAvatarChoice>>(ReadLimited(manifest,4*1024*1024))??throw new InvalidDataException("Empty avatar manifest");if(data.Count>10000||data.Any(p=>p.Key.Length>128||p.Value==null||!Valid(p.Value)))throw new InvalidDataException("Invalid avatar manifest");choices=data;}}catch(Exception e){LoadError=e.Message;writable=false;}}
 public RulerAvatarChoice? Choice(string id)=>choices.GetValueOrDefault(id);
 public ImageSource For(string id)=>Image(id,Choice(id));
 public ImageSource Image(string id,RulerAvatarChoice? choice){
  if(choice==null)return PersonAvatar.For(id);if(choice.File.Length==0)return choice.Preset<0?PersonAvatar.For(id):PersonAvatar.Preset(choice.Preset);
  if(cache.TryGetValue(choice.File,out var cached))return cached;
  try{string path=Path.Combine(directory,choice.File);if(!Valid(choice)||new FileInfo(path).Length>1024*1024)throw new InvalidDataException("Invalid saved avatar");var bytes=ReadLimited(path,1024*1024);if(Convert.ToHexString(SHA256.HashData(bytes))+".png"!=choice.File)throw new InvalidDataException("Avatar checksum mismatch");using(var meta=new MemoryStream(bytes,false)){var decoder=BitmapDecoder.Create(meta,BitmapCreateOptions.IgnoreColorProfile,BitmapCacheOption.OnDemand);if(decoder is not PngBitmapDecoder||decoder.Frames.Count!=1||decoder.Frames[0].PixelWidth!=128||decoder.Frames[0].PixelHeight!=128)throw new InvalidDataException("Unexpected saved avatar");}using var stream=new MemoryStream(bytes,false);var image=new BitmapImage();image.BeginInit();image.StreamSource=stream;image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=128;image.CreateOptions=BitmapCreateOptions.IgnoreColorProfile;image.EndInit();if(image.PixelWidth!=128||image.PixelHeight!=128)throw new InvalidDataException("Unexpected avatar dimensions");image.Freeze();if(cache.Count>=64)cache.Remove(cache.Keys.First());cache[choice.File]=image;return image;}catch{return PersonAvatar.For(id);}
 }
 public RulerAvatarChoice Import(string source){
  if(!writable)throw new InvalidDataException(LoadError);string extension=Path.GetExtension(source).ToLowerInvariant();if(extension is not (".png" or ".jpg" or ".jpeg"))throw new InvalidDataException("Choose a PNG or JPEG image.");var info=new FileInfo(source);if(!info.Exists||info.Length<=0||info.Length>12*1024*1024)throw new InvalidDataException("Image must be no larger than 12 MB.");
  byte[] bytes=ReadLimited(source,12*1024*1024);int width,height;using(var metadata=new MemoryStream(bytes,false)){var decoder=BitmapDecoder.Create(metadata,BitmapCreateOptions.IgnoreColorProfile,BitmapCacheOption.OnDemand);if(decoder is not (PngBitmapDecoder or JpegBitmapDecoder)||decoder.Frames.Count!=1)throw new InvalidDataException("Choose a single PNG or JPEG image.");width=decoder.Frames[0].PixelWidth;height=decoder.Frames[0].PixelHeight;if(width<=0||height<=0||width>16000||height>16000||(long)width*height>16_000_000)throw new InvalidDataException("Image must contain at most 16 megapixels.");}
  using var input=new MemoryStream(bytes,false);var decoded=new BitmapImage();decoded.BeginInit();decoded.CacheOption=BitmapCacheOption.OnLoad;decoded.CreateOptions=BitmapCreateOptions.IgnoreColorProfile;decoded.StreamSource=input;if(width>=height)decoded.DecodePixelWidth=Math.Min(256,width);else decoded.DecodePixelHeight=Math.Min(256,height);decoded.EndInit();decoded.Freeze();int side=Math.Min(decoded.PixelWidth,decoded.PixelHeight);var crop=new CroppedBitmap(decoded,new Int32Rect((decoded.PixelWidth-side)/2,(decoded.PixelHeight-side)/2,side,side));
  var visual=new DrawingVisual();using(var d=visual.RenderOpen())d.DrawImage(crop,new Rect(0,0,128,128));var canonical=new RenderTargetBitmap(128,128,96,96,PixelFormats.Pbgra32);canonical.Render(visual);canonical.Freeze();var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(canonical));using var output=new MemoryStream();encoder.Save(output);byte[] png=output.ToArray();string name=Convert.ToHexString(SHA256.HashData(png))+".png";Directory.CreateDirectory(directory);string destination=Path.Combine(directory,name);if(!System.IO.File.Exists(destination)){if(new DirectoryInfo(directory).EnumerateFiles("*.png").Sum(f=>f.Length)+png.Length>64*1024*1024)throw new InvalidDataException("Avatar storage limit reached.");System.IO.File.WriteAllBytes(destination,png);}return new(-1,name);
 }
 static byte[] ReadLimited(string path,int maximum){using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);if(stream.Length<=0||stream.Length>maximum)throw new InvalidDataException("Image size limit");var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);return bytes;}
 public void Set(string id,RulerAvatarChoice? choice){if(!writable)throw new InvalidDataException(LoadError);if(id.Length>128||choice!=null&&!Valid(choice))throw new InvalidDataException("Invalid avatar choice");var old=Choice(id);if(choice==null)choices.Remove(id);else choices[id]=choice;try{string temporary=manifest+".tmp";System.IO.File.WriteAllText(temporary,JsonSerializer.Serialize(choices,new JsonSerializerOptions{WriteIndented=true}));if(System.IO.File.Exists(manifest))System.IO.File.Replace(temporary,manifest,manifest+".bak");else System.IO.File.Move(temporary,manifest);}catch{if(old==null)choices.Remove(id);else choices[id]=old;throw;}}
}
