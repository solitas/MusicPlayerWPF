using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mp3Lib;
using MP3Testing.Player;
using Application = System.Windows.Application;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using ListViewItem = System.Windows.Controls.ListViewItem;
namespace MP3Testing
{
    public delegate void NextPlayDelegate(int index, MediaInfo info);
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
                
                foreach(KeyValuePair<TimeSpan, String> keyValue in currentlyics)
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
            _player.PlayList.ForEach(audioFile =>
            {
                bool result = File.Exists(@audioFile);
                if (result)
                {
                    var fileInfo = new FileInfo(audioFile);
                    var item = new ListViewItem { Content = fileInfo.Name };
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

                    foreach (var fileInfo in files.Where(fileInfo => fileInfo.Extension.Equals(".mp3")))
                    {
                        var item = new ListViewItem { Content = fileInfo.Name };
                        FileList.Items.Add(item);
                    }
                }
            }
        }


        public void NextPlayEventHandler(int index, MediaInfo info)
        {
            /* 리스트 뷰 선택  해제 */
            foreach (ListViewItem i in FileList.Items)
            {
                i.IsSelected = false;
            }

            /* 다음 곡 리스트 뷰 아이템 선택  */
            var item = (ListViewItem)FileList.Items[index];
            item.IsSelected = true;

            /* 스크롤 진행 */
            FileList.ScrollIntoView(item);  // scroll 이동

            // 전체 시간 업데이트
            TotalTime = info.TotalTime;

            // 배경 업데이트
            UpdateBackground((BitmapSource)info.Image);

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

        private void PlayButtonHandler(object sender, RoutedEventArgs e)
        {
            try
            {
                SeekBar.Value = 0;
                CurrentTimeLb.Content = "00:00";
                if (FileList.SelectedIndex >= 0)
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                    }

                    if (!(_player.State is PauseState))
                    {
                        var info = _player.GetMediaInfo(FileList.SelectedIndex);

                        TotalTime = info.TotalTime;
                        Title = info.Title + "::" + info.Artist;

                        GetLyics(info.FilePath);

                        UpdateBackground((BitmapSource)info.Image);

                        SongInfoCtrl.Info = info;
                    }

                    TimerStart();

                    _player.Play(FileList.SelectedIndex);
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
            using (Stream tmpStream = new FileStream(path, FileMode.Open))
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
    }
}
