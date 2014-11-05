using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MP3Testing.Control;
using MP3Testing.Player;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Shapes;
using DataObject = System.Windows.DataObject;

namespace MP3Testing
{
    public delegate void NextPlayDelegate(int index, MediaInfo info);

    public class Item
    {
        public string Artist;
        public string Title;
        public string TotalTime;
    }
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        private Mp3Player _player;
        private DispatcherTimer _timer;
        private DateTime _totalTime;
        private TimeSpan TotalTime
        {
            set
            {
                _totalTime = DateTime.MinValue.Add(value);
                TotalTimeLb.Content = _totalTime.ToString("mm:ss");
            }
        }
        private DateTime _currentTime;

        public TimeSpan CurrentTime
        {
            set
            {

                foreach (KeyValuePair<TimeSpan, String> keyValue in currentlyics)
                {
                    if (value.Seconds == keyValue.Key.Seconds && value.Minutes == keyValue.Key.Minutes)
                    {
                        LyicsLabel.Content = keyValue.Value;
                        break;
                    }
                }

                _currentTime = DateTime.MinValue.Add(_player.CurrentTime);
                CurrentTimeLb.Content = _currentTime.ToString("mm:ss");
            }
        }
        public MainWindow()
        {
            _player = new Mp3Player();
            InitializeComponent();
            AlbumControl.spectrumAnalyzer.RegisterSoundPlayer(_player);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _player.PlayFileList.ForEach(audioFile =>
            {
                bool result = File.Exists(@audioFile);
                if (result)
                {
                    var playList = _player.PlayList;
                    var playInfo = playList.MediaList.Find(i => i.FilePath == audioFile);

                    var item = new FileListItem();
                    {
                        item.Info.Artist = playInfo.Artist;
                        item.Info.Title = playInfo.Title;
                        item.Info.TotalTime = playInfo.TotalTime;
                    };
                    FileList.Items.Add(item);
                }
            });

            _player.NextPlayEvent += NextPlayEventHandler;

        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _player.Close();
        }

        private void OpenFileFolderHandler(object sender, RoutedEventArgs e)
        {
            // folder browser dialog는 wpf에 제공되지 않음
            var dlg = new FolderBrowserDialog();

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = dlg.SelectedPath;
                if (Directory.Exists(path))
                {
                    var info = new DirectoryInfo(path);
                    var files = info.EnumerateFiles();
                    _player.AddFolderList(files.ToList());
                    var playList = _player.PlayList;
                    foreach (var fileInfo in files.Where(fileInfo => fileInfo.Extension.Equals(".mp3")))
                    {
                        var playInfo = playList.MediaList.Find(i => i.FilePath == fileInfo.Name);
                        var item = new FileListItem();
                        {
                            item.Info.Artist = playInfo.Artist;
                            item.Info.Title = playInfo.Title;
                            item.Info.TotalTime = playInfo.TotalTime;
                        };
                        FileList.Items.Add(item);
                    }
                }
            }
        }


        public void NextPlayEventHandler(int index, MediaInfo info)
        {
            try
            {
                var item = FileList.Items[index];
                FileList.SelectedItem = item;

                /* 스크롤 진행 */
                FileList.ScrollIntoView(item);  // scroll 이동
            }
            catch (Exception)
            {

                throw;
            }

            // 전체 시간 업데이트
            TotalTime = info.TotalTime;

            // 배경 업데이트
            UpdateBackground((BitmapSource)info.Image);

            // 가사 업데이트
            GetLyics(info.FilePath);
            // 곡 정보 업데이트
            SongInfoCtrl.Info = info;

            // 탐색 바 최대치 변경
            SeekBar.Maximum = _player.TotalLength;

            // 타이머 시작
            TimerStart();
        }

        private void TimerStart()
        {
            LyicsLabel.Content = "";
            if (_timer != null)
            {
                _timer.Stop();
            }

            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 30) };
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (!_posChange)
            {
                CurrentTime = _player.CurrentTime;
                SeekBar.Value = _player.Position;   // Seekbar 의 포지션을 옮긴다
            }
        }
        Dictionary<TimeSpan, String> currentlyics;
        private void PrevButton_OnClick(object sender, RoutedEventArgs e)
        {

            if (_timer != null)
            {
                _timer.Stop();
            }

            int index = _player.Backward();
            try
            {
                var item = FileList.Items[index];
                FileList.SelectedItem = item;

                /* 스크롤 진행 */
                FileList.ScrollIntoView(item);  // scroll 이동
            }
            catch (Exception)
            {

                throw;
            }

            if (!(_player.State is PauseState))
            {
                var info = _player.GetMediaInfo(index);

                TotalTime = info.TotalTime;
                Title = info.Title + "::" + info.Artist;

                GetLyics(info.FilePath);

                UpdateBackground((BitmapSource)info.Image);

                SongInfoCtrl.Info = info;
            }

            TimerStart();

            SeekBar.Maximum = _player.TotalLength;
            Play.IsChecked = false;
            Play.Content = "■";
        }

        private void ForwardButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
            }

            int index = _player.Forward();
            try
            {
                var item = FileList.Items[index];
                FileList.SelectedItem = item;

                /* 스크롤 진행 */
                FileList.ScrollIntoView(item);  // scroll 이동
            }
            catch (Exception)
            {

                throw;
            }

            if (!(_player.State is PauseState))
            {
                var info = _player.GetMediaInfo(index);

                TotalTime = info.TotalTime;
                Title = info.Title + "::" + info.Artist;

                GetLyics(info.FilePath);

                UpdateBackground((BitmapSource)info.Image);

                SongInfoCtrl.Info = info;
            }

            TimerStart();

            SeekBar.Maximum = _player.TotalLength;
            Play.IsChecked = false;
            Play.Content = "■";
        }

        private void PlayButtonHandler(object sender, RoutedEventArgs e)
        {
            play(FileList.SelectedIndex);
        }

        private void play(int index)
        {
            try
            {
                SeekBar.Value = 0;
                CurrentTimeLb.Content = "00:00";

                if (index >= 0)
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                    }

                    if (!(_player.State is PauseState))
                    {
                        var info = _player.GetMediaInfo(index);

                        TotalTime = info.TotalTime;
                        Title = info.Title + "::" + info.Artist;

                        GetLyics(info.FilePath);

                        UpdateBackground((BitmapSource)info.Image);

                        SongInfoCtrl.Info = info;
                    }

                    TimerStart();

                    _player.Play(index);
                    SeekBar.Maximum = _player.TotalLength;
                    Play.Content = "■";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void GetLyics(string path)
        {
            if (currentlyics != null)
            {
                currentlyics.Clear();
                currentlyics = null;
            }
            using (Stream tmpStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                object o = eLyrics_windows.eLyrics.getLyric(tmpStream);

                if (o.GetType().ToString().IndexOf("KeyValuePair") != -1)
                {
                    // 오류
                    MessageBox.Show(((KeyValuePair<String, String>)o).Value);
                }
                else
                {
                    currentlyics = new Dictionary<TimeSpan, string>();
                    // 정상적인 리턴
                    foreach (KeyValuePair<String, String> keyValue in (Dictionary<String, String>)o)
                    {
                        ConvertTime(keyValue.Key, keyValue.Value);
                        Console.WriteLine(keyValue.Key + " " + keyValue.Value);
                    }
                }
            }

        }
        private void ConvertTime(string data, string lyics)
        {
            //data 구조 [분:초.밀리초] 가사
            var seperator = new char[] { '[', ']' };
            var resultStr = data.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            var resultTime = TimeSpan.Parse("00:00:" + resultStr[0]);

            currentlyics.Add(resultTime, lyics);

        }
        private void PauseButtonHandler(object sender, RoutedEventArgs e)
        {
            _player.Pause();
            Play.Content = "▶";
        }

        private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_player.SetVolumeDelegate != null)
            {
                _player.SetVolumeDelegate((float)e.NewValue);
            }
        }


        private void RepeatButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            RepeatButton.Content = "UnRepeat";
            _player.Repeat = RepeatState.UnRepeat;

        }

        private void RepeatButton_OnChecked(object sender, RoutedEventArgs e)
        {
            RepeatButton.Content = "Repeat";
            _player.Repeat = RepeatState.Repeat;
        }

        private RepeatState _repeatState = RepeatState.UnRepeat;
        private void RandomButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            RandomButton.Content = "Normal";
            _player.RandomState = false;
        }

        private void RandomButton_OnChecked(object sender, RoutedEventArgs e)
        {
            RandomButton.Content = "Random";
            _player.RandomState = true;
        }

        private bool _posChange = false;
        private void SeekBar_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_posChange)
            {
                _posChange = true;
            }
        }
        private void SeekBar_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_posChange)
            {
                if (_player != null && _timer != null)
                {
                    var convertValue = (long)(e.GetPosition(SeekBar).X / (SeekBar.ActualWidth / SeekBar.Maximum));
                    SeekBar.Value = convertValue;
                    _player.Seek(convertValue);
                }
                _posChange = false;
            }
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MainBorder_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MovingWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void UpdateBackground(BitmapSource source)
        {
            var fcb = new FormatConvertedBitmap();
            fcb.BeginInit();
            fcb.Source = source;
            fcb.DestinationFormat = PixelFormats.Gray32Float;
            fcb.EndInit();
            MainBorder.Background = new ImageBrush(fcb);
        }


        private void OpenFileHandler(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveFileHandler(object sender, RoutedEventArgs e)
        {

        }
        private AdornerLayer _layer;
        private bool _dragIsOutOfScope = false;
        private DragAdorner _adorner;
        private void FileList_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void FileList_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    BeginDrag(e);
                }
            }
        }
        private void BeginDrag(MouseEventArgs e)
        {
            ListView listView = FileList;
            FileListItem listViewItem =
                FindAnchestor<FileListItem>((DependencyObject)e.OriginalSource);

            if (listViewItem == null)
                return;

            // get the data for the ListViewItem
            var name = listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            //setup the drag adorner.
            InitialiseAdorner(listViewItem);

            //add handles to update the adorner.
            listView.PreviewDragOver += ListViewDragOver;
            listView.DragLeave += ListViewDragLeave;
            listView.DragEnter += ListViewDragEnter;

            DataObject data = new DataObject("myFormat", name);
            DragDropEffects de = DragDrop.DoDragDrop(FileList, data, DragDropEffects.Move);

            //cleanup 
            listView.PreviewDragOver -= ListViewDragOver;
            listView.DragLeave -= ListViewDragLeave;
            listView.DragEnter -= ListViewDragEnter;

            if (_adorner != null)
            {
                AdornerLayer.GetAdornerLayer(listView).Remove(_adorner);
                _adorner = null;
            }
        }
        void ListViewDragOver(object sender, DragEventArgs args)
        {
            if (_adorner != null)
            {
                _adorner.OffsetLeft = args.GetPosition(FileList).X;
                _adorner.OffsetTop = args.GetPosition(FileList).Y - _startPoint.Y;
            }
        }
        void ListViewDragLeave(object sender, DragEventArgs e)
        {
            if (e.OriginalSource == FileList)
            {
                Point p = e.GetPosition(FileList);
                Rect r = VisualTreeHelper.GetContentBounds(FileList);
                if (!r.Contains(p))
                {
                    this._dragIsOutOfScope = true;
                    e.Handled = true;
                }
            }
        }
        private void ListViewDragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") ||
                sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void InitialiseAdorner(FileListItem listViewItem)
        {
            VisualBrush brush = new VisualBrush(listViewItem);
            _adorner = new DragAdorner((UIElement)listViewItem, listViewItem.RenderSize, brush);
            _adorner.Opacity = 0.5;
            _layer = AdornerLayer.GetAdornerLayer(FileList as Visual);
            _layer.Add(_adorner);
        }

        private Point _startPoint;
        private void FileList_OnDrop(object sender, DragEventArgs e)
        {
            MediaInfo droppedData = e.Data.GetData(typeof(MediaInfo)) as MediaInfo;
            FileListItem target = FindAnchestor<FileListItem>((DependencyObject)e.OriginalSource);
            if (target != null)
            {
                var nameToReplace = FileList.ItemContainerGenerator.ItemFromContainer(target);
                int index = FileList.Items.IndexOf(nameToReplace);

                if (index >= 0)
                {
//                     FileList.Items.Remove(name);
//                     FileList.Items.Insert(index, name);
                }
            }
            else
            {
//                 FileList.Items.Remove(name);
//                 FileList.Items.Add(name);
            }

        }
        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

       
    }

    class DragAdorner : Adorner
    {
        private Rectangle child = null;
        private double offsetLeft = 0;
        private double offsetTop = 0;

        /// <summary>
        /// Initializes a new instance of DragVisualAdorner.
        /// </summary>
        /// <param name="adornedElement">The element being adorned.</param>
        /// <param name="size">The size of the adorner.</param>
        /// <param name="brush">A brush to with which to paint the adorner.</param>
        public DragAdorner(UIElement adornedElement, Size size, Brush brush)
            : base(adornedElement)
        {
            Rectangle rect = new Rectangle();
            rect.Fill = brush;
            rect.Width = size.Width;
            rect.Height = size.Height;
            rect.IsHitTestVisible = false;
            this.child = rect;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(this.offsetLeft, this.offsetTop));
            return result;
        }


        /// <summary>
        /// Gets/sets the horizontal offset of the adorner.
        /// </summary>
        public double OffsetLeft
        {
            get { return this.offsetLeft; }
            set
            {
                this.offsetLeft = value;
                UpdateLocation();
            }
        }


        /// <summary>
        /// Updates the location of the adorner.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public void SetOffsets(double left, double top)
        {
            this.offsetLeft = left;
            this.offsetTop = top;
            this.UpdateLocation();
        }


        /// <summary>
        /// Gets/sets the vertical offset of the adorner.
        /// </summary>
        public double OffsetTop
        {
            get { return this.offsetTop; }
            set
            {
                this.offsetTop = value;
                UpdateLocation();
            }
        }

        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            this.child.Measure(constraint);
            return this.child.DesiredSize;
        }

        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override Visual GetVisualChild(int index)
        {
            return this.child;
        }

        /// <summary>
        /// Override.  Always returns 1.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }


        private void UpdateLocation()
        {
            AdornerLayer adornerLayer = this.Parent as AdornerLayer;
            if (adornerLayer != null)
                adornerLayer.Update(this.AdornedElement);
        }
    }
}
