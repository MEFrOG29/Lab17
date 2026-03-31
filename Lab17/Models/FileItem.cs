using System;
using System.Windows.Media;
namespace Lab17.Models
{
    public class FileItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public DateTime DateModified { get; set; }
        public string Size { get; set; }
        public bool IsDirectory { get; set; }
        public ImageSource Icon { get; set; }
    }
}
