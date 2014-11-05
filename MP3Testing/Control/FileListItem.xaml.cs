using System.Windows.Controls;

namespace MP3Testing.Control
{
    /// <summary>
    /// FileListItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileListItem : UserControl
    {
        private MediaInfo _info = new MediaInfo();

        public MediaInfo Info
        {
            get
            {
                return _info;
            }
        }

        public FileListItem()
        {
            InitializeComponent();
            DataContext = _info;
        }
    }
}
