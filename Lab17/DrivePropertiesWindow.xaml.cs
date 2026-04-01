using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lab17
{
    /// <summary>
    /// Логика взаимодействия для DrivePropertiesWindow.xaml
    /// </summary>
    public partial class DrivePropertiesWindow : Window
    {
        public DrivePropertiesWindow(DriveInfo drive)
        {
            InitializeComponent();

            DataContext = new
            {
                Name = $"{drive.Name} ({drive.VolumeLabel})",
                FileSystem = drive.DriveFormat,
                UsedBytes = drive.TotalSize - drive.TotalFreeSpace,
                UsedGb = FormatToGb(drive.TotalSize - drive.TotalFreeSpace),
                FreeBytes = drive.TotalFreeSpace,
                FreeGb = FormatToGb(drive.TotalFreeSpace),
                TotalBytes = drive.TotalSize,
                TotalGb = FormatToGb(drive.TotalSize),
            };
        }

        private string FormatToGb(long bytes)
        {
            double gb = bytes / 1024.0 / 1024.0 / 1024.0;
            return $"{gb:F2} ГБ";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
