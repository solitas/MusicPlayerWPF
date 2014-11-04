using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Mp3Lib;
using MP3Testing.Player;
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
            foreach (ListViewItem i in FileList.Items)
            {
                i.IsSelected = false;
            }

            var item = (ListViewItem)FileList.Items[index];
            item.IsSelected = true;

            FileList.ScrollIntoView(item);  // scroll 이동

            _totalTime = DateTime.MinValue.Add(info.TotalTime);

            TitleName.Content = info.Title;
            AlbumImage.Source = info.Image;
            TotalTimeLb.Content = _totalTime.ToString("mm:ss");
   
            TimerStart();
        }

        private void TimerStart()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }

            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 1) };
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            var currentTime = DateTime.MinValue.Add(_player.CurrentTime); // 현재 시간을 가져온다
            SeekBar.Value = _player.Position;   // Seekbar 의 포지션을 옮긴다
            CurrentTime.Content = currentTime.ToString("mm:ss");
        }

        private void PlayButtonHandler(object sender, RoutedEventArgs e)
        {
            try
            {
                SeekBar.Value = 0;
                CurrentTime.Content = "00:00";
                if (FileList.SelectedIndex >= 0)
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                    }

                    if (!(_player.State is PauseState))
                    {
                        var info = _player.GetMediaInfo(FileList.SelectedIndex);
                        _totalTime = DateTime.MinValue.Add(info.TotalTime);

                        TitleName.Content = info.Title;
                        AlbumImage.Source = info.Image;
                        TotalTimeLb.Content = _totalTime.ToString("mm:ss");
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

        private void StopButtonHandler(object sender, RoutedEventArgs e)
        {
            _player.Stop();
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

        private void SeekBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void SeekBar_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_player != null && _timer != null)
            {
                _player.Seek((long)SeekBar.Value);
            }
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }


}
