using Avalonia.Data.Converters;
using Cashere.ViewModels.Mobile;
using System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Converters
{
    public class EnumToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MobileSection section)
            {
                return section switch
                {
                    MobileSection.Pairing => "PAIRING",
                    MobileSection.Scanning => "SCANNING",
                    MobileSection.Labeling => "LABELING",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}