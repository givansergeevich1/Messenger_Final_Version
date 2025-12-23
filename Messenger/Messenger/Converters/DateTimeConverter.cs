using System;
using System.Globalization;
using System.Windows.Data;

namespace Messenger.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var time = dateTime.ToLocalTime();

                if (time.Date == now.Date)
                {
                    // Сегодня - показываем время
                    return time.ToString("HH:mm");
                }
                else if (time.Date == now.Date.AddDays(-1))
                {
                    // Вчера
                    return "Вчера " + time.ToString("HH:mm");
                }
                else if (time.Year == now.Year)
                {
                    // В этом году
                    return time.ToString("dd MMM HH:mm");
                }
                else
                {
                    // Более года назад
                    return time.ToString("dd.MM.yyyy HH:mm");
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToLocalTime().ToString("HH:mm");
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var time = dateTime.ToLocalTime();

                if (time.Date == now.Date)
                {
                    return "Сегодня";
                }
                else if (time.Date == now.Date.AddDays(-1))
                {
                    return "Вчера";
                }
                else if (time.Year == now.Year)
                {
                    return time.ToString("dd MMM");
                }
                else
                {
                    return time.ToString("dd.MM.yyyy");
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}