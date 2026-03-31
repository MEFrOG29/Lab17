using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab17.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO.Compression;

namespace Lab17.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        [ObservableProperty]
        private FileItem? _selectedItem;

        public ObservableCollection<FileItem> Items { get; } = new ObservableCollection<FileItem>();

        public MainViewModel()
        {
            LoadDirectory(CurrentPath);
        }

        private void AddParentDirectory(string path)
        {
            var parent = Directory.GetParent(path);
            if (parent != null)
            {
                Items.Add(new FileItem
                {
                    Name = "..",
                    FullPath = parent.FullName,
                    IsDirectory = true,
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png")),
                    DateModified = parent.LastWriteTime,
                    Size = ""
                });
            }
        }

        public void LoadDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                Items.Clear();
                AddParentDirectory(path);

                foreach (var dir in Directory.GetDirectories(path))
                {
                    var info = new DirectoryInfo(dir);
                    Items.Add(new FileItem
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        DateModified = info.LastWriteTime,
                        Size = "",
                        IsDirectory = true,
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png"))
                    });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var info = new FileInfo(file);
                    Items.Add(new FileItem
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        DateModified = info.LastWriteTime,
                        Size = FormatSize(info.Length),
                        IsDirectory = false,
                        Icon = GetIcon(info.Extension)
                    });
                }
                _currentPath = path;
                OnPropertyChanged(nameof(CurrentPath));
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет доступа к этой папке.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "Б", "Кб", "Мб", "Гб", "Тб" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private ImageSource GetIcon(string ext)
        {
            string iconName = "text.png";

            switch (ext.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                    iconName = "photo.png";
                    break;
                case ".mp4":
                    iconName = "video.png";
                    break;
                case ".mp3":
                    iconName = "music.png";
                    break;
                default:
                    iconName = "text.png";
                    break;
            }

            return new BitmapImage(new Uri($"pack://application:,,,/Resources/{iconName}"));
        }

        [RelayCommand]
        public void OpenItem()
        {
            if (SelectedItem == null) return;

            if (SelectedItem.IsDirectory)
            {
                LoadDirectory(SelectedItem.FullPath);
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(SelectedItem.FullPath)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Не удалось запустить файл: {ex.Message}");
                }
            }
        }

        partial void OnCurrentPathChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
            {
                LoadDirectory(value);
            }
        }

        [RelayCommand]
        public void CopyItem()
        {
            if (SelectedItem == null || SelectedItem.Name == "..") return;

            StringCollection fileList = new StringCollection();
            fileList.Add(SelectedItem.FullPath);

            Clipboard.SetFileDropList(fileList);
        }

        [RelayCommand]
        public void PasteItem()
        {
            if (!Clipboard.ContainsFileDropList()) return;

            StringCollection fileList = Clipboard.GetFileDropList();

            try
            {
                foreach (string srcPath in fileList)
                {
                    string fileName = Path.GetFileName(srcPath);
                    string destPath = Path.Combine(CurrentPath, fileName);

                    if (File.Exists(srcPath))
                    {
                        File.Copy(srcPath, destPath, true);
                    }
                    else if (Directory.Exists(srcPath))
                    {
                        CopyDirectory(srcPath, destPath);
                    }

                }
                LoadDirectory(CurrentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вставке: {ex.Message}");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        [RelayCommand]
        public void CompressItem()
        {
            if (SelectedItem == null || !SelectedItem.IsDirectory || SelectedItem.Name == "..")
            {
                MessageBox.Show("Выберите папку для сжатия.");
                return;
            }
            try
            {
                string zipPath = SelectedItem.FullPath + ".zip";

                if (File.Exists(zipPath))
                {
                    var result = MessageBox.Show("Архив уже существует." +
                        " Перезаписать?", "Внимание", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) return;
                    File.Delete(zipPath);
                }
                ZipFile.CreateFromDirectory(SelectedItem.FullPath, zipPath);
                LoadDirectory(CurrentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сжатии: {ex.Message}");
            }
        }

        [RelayCommand]
        public void DecompressItem()
        {
            if (SelectedItem == null ||
                SelectedItem.IsDirectory ||
                !SelectedItem.FullPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Выберите ZIP-архив для извлечения.");
                return;
            }
            try
            {
                string destDir = Path.Combine(CurrentPath,
                    Path.GetFileNameWithoutExtension(SelectedItem.FullPath));

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                ZipFile.ExtractToDirectory(SelectedItem.FullPath, destDir);
                LoadDirectory(CurrentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сжатии: {ex.Message}");
            }
        }
    }
}