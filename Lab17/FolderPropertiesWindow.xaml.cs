using System.IO;
using System.Windows;


namespace Lab17
{
    /// <summary>
    /// Логика взаимодействия для FolderPropertiesWindow.xaml
    /// </summary>
    public partial class FolderPropertiesWindow : Window
    {
        public FolderPropertiesWindow(string folderPath)
        {
            InitializeComponent();
            CalculateFolderInfo(folderPath);
        }

        private void CalculateFolderInfo(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

                var resultData = new FolderInfoViewModel();
                DirectoryInfo rootDir = new DirectoryInfo(path);
                List<FileInfo> allFiles = new List<FileInfo>();
                int foldersCount = 0;

                // Итеративный обход (защита от вылета по стеку)
                Stack<DirectoryInfo> stack = new Stack<DirectoryInfo>();
                stack.Push(rootDir);

                while (stack.Count > 0)
                {
                    DirectoryInfo current = stack.Pop();
                    try
                    {
                        var files = current.GetFiles();
                        if (files != null) allFiles.AddRange(files);

                        var dirs = current.GetDirectories();
                        if (dirs != null)
                        {
                            foreach (var d in dirs)
                            {
                                foldersCount++;
                                stack.Push(d);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { } // Пропускаем системные папки
                }

                long totalSize = 0;
                foreach (var f in allFiles) totalSize += f.Length;

                // Расчет процента
                string root = Path.GetPathRoot(path);
                if (!string.IsNullOrEmpty(root))
                {
                    DriveInfo drive = new DriveInfo(root);
                    if (drive.IsReady)
                        resultData.PercentOfDrive = ((double)totalSize / drive.TotalSize) * 100;
                }

                // Заполнение модели
                resultData.FolderName = rootDir.Name;
                resultData.FullPath = rootDir.FullName;
                resultData.FileCount = allFiles.Count;
                resultData.FolderCount = foldersCount;
                resultData.TotalSizeDisplay = FormatSize(totalSize);
                resultData.TopFiles = allFiles.OrderByDescending(f => f.Length)
                                              .Take(5)
                                              .Select(f => new TopFileItem
                                              {
                                                  SizeStr = FormatSize(f.Length),
                                                  Path = f.FullName
                                              }).ToList();

                // Установка DataContext типизированным объектом
                this.DataContext = resultData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}");
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

        private void SafeWalk(DirectoryInfo root, List<FileInfo> files, ref int dirs)
        {
            try
            {
                // Получаем файлы только в текущей папке
                var currentFiles = root.GetFiles();
                if (currentFiles != null)
                {
                    files.AddRange(currentFiles);
                }

                // Получаем папки только в текущей папке
                var currentDirs = root.GetDirectories();
                if (currentDirs != null)
                {
                    foreach (var d in currentDirs)
                    {
                        dirs++;
                        // Рекурсия пошла глубже
                        SafeWalk(d, files, ref dirs);
                    }
                }
            }
            catch (UnauthorizedAccessException) { /* Игнорируем закрытые папки */ }
            catch (IOException) { /* Игнорируем ошибки чтения диска */ }
            catch (Exception) { /* Любая другая ошибка в конкретной подпапке не должна ронять всё */ }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TopFileItem
    {
        public string? SizeStr { get; set; }
        public string? Path { get; set; }
    }

    public class FolderInfoViewModel
    {
        public string FolderName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public string TotalSizeDisplay { get; set; } = "";
        public double PercentOfDrive { get; set; }
        public List<TopFileItem> TopFiles { get; set; } = new();
    }
}
