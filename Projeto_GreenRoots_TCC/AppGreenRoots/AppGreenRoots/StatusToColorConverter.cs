using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GreenRootsApp
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Concluído" => Brushes.Green,
                    "Pendente" => Brushes.Orange,
                    "Erro" => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing; 
        }
        
    }
}