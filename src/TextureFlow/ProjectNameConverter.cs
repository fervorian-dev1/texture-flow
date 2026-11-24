using System.Globalization;
using System.IO;
using System.Windows.Data;
namespace TextureFlow;
public sealed class ProjectNameConverter : IValueConverter
{
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
 {
  if(value is not string path)return "Choose a project";
  try{var dir=new DirectoryInfo(path);return dir.Name.Equals("Project",StringComparison.OrdinalIgnoreCase)?(dir.Parent?.Name??dir.Name):dir.Name;}catch{return path;}
 }
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
