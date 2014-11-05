using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using WPFSoundVisualizationLib;

namespace MP3Testing.Player
{
    public class PlayOrder
    {
        public int CurrentIndex = -1;
        public int OldIndex = -1;
        public int NextIndex = -1;
    }
    public enum RepeatState
    {
        Repeat, UnRepeat
    }
    public class Mp3Player : IPlayer, IPlaybackContext, ISpectrumPlayer, IWaveformPlayer
    {
        private const int FftDataSize = (int)FFTDataSize.FFT2048;

        public event NextPlayDelegate NextPlayEvent;

        public Action<float> SetVolumeDelegate;

        private readonly Playlist _playlist;    // 재생 목록 리스트

        private readonly PlayOrder _order;  // 재생 순서 인덱스

        private const int RepeatThreshold = 200;

        /* NAudio 객체 */
        private IWavePlayer _wavePlayer;
        private WaveChannel32 _channel;
        private BlockAlignReductionStream _stream;

        /// <summary>
        /// playback 상태 객체
        /// </summary>
        private IPlaybackState _currentState;

        /// <summary>
        /// 반복 상태 객체
        /// </summary>
        private RepeatState _repeatState = RepeatState.UnRepeat;

        private float _volumn = 1.0f;

        #region Properties
        public IPlaybackState State
        {
            get { return _currentState; }
        }

        public RepeatState Repeat
        {
            set
            {
                _repeatState = value;
            }
        }
        public List<string> PlayFileList
        {
            get
            {
                return _playlist.Files;
            }
        }

        public Playlist PlayList
        {
            get
            {
                return _playlist;
            }
        }
        public TimeSpan TotalTime
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.TotalTime;
                }
                return TimeSpan.Zero;
            }
        }

        public long Position
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.Position;
                }
                return 0;
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.CurrentTime;
                }
                return TimeSpan.Zero;
            }
        }

        public long TotalLength
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.Length;
                }
                return 0;
            }
        }

        [DefaultValue(false)]
        public bool RandomState { get; set; }

        #endregion
        public Mp3Player()
        {
            _currentState = new StopState();
            _playlist = new Playlist();
            _order = new PlayOrder();
        }

        public void Close()
        {
            DisposeWave();
            _playlist.SaveFile();
        }

        public void AddFolderList(List<FileInfo> files)
        {
            foreach (var fileInfo in files.Where(fileInfo => fileInfo.Extension.Equals(".mp3")))
            {
                _playlist.InsertAudioFilePath(fileInfo.FullName);
            }
        }

        public void Play(string resourceName)
        {
            if (_currentState.CanPlay(resourceName, this))
            {
                _wavePlayer.Play();
            }
        }


        // 선택 재생 메소드
        public void Play(int index)
        {
            UpdatePlayIndex(index);

            var resource = _playlist.MediaList[index].FilePath;

            if (_currentState.CanPlay(resource, this))
            {
                if (_wavePlayer != null)
                    _wavePlayer.Play();
                IsPlaying = true;
            }
        }

        public void Stop()
        {
            if (_currentState.CanStop(this))
            {
                _wavePlayer.Stop();
            }
        }

        public void Pause()
        {
            if (_currentState.CanPause(this))
            {

                _wavePlayer.Pause();
            }
        }

        public int Forward()
        {
            int forwardIndex = _order.NextIndex;
            UpdatePlayIndex(forwardIndex);

            var resource = _playlist.MediaList[forwardIndex].FilePath;
            if (_currentState.CanPlay(resource, this))
            {
                if (_wavePlayer != null)
                    _wavePlayer.Play();
                IsPlaying = true;
            }

            return forwardIndex;
        }

        public int Backward()
        {
            int backIndex = _order.OldIndex;
            UpdatePlayIndex(backIndex);

            var resource = _playlist.MediaList[backIndex].FilePath;
            if (_currentState.CanPlay(resource, this))
            {
                if (_wavePlayer != null)
                    _wavePlayer.Play();
                IsPlaying = true;
            }

            return backIndex;
        }

        public void ChangePlaybackState(IPlaybackState newState)
        {
            _currentState = newState;
        }

        public void Open(string selectedFile)
        {
            if (_wavePlayer != null)
            {
                // 스트림 해제 시 PlaybackStopped 이벤트가 두번 발생 방지
                _wavePlayer.PlaybackStopped -= WavePlayerOnPlaybackStopped;
                _channel.Sample -= ChannelOnSample;
            }

            DisposeWave();

            WaveStream pcm;

            if (selectedFile.EndsWith(".mp3"))
            {
                pcm = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(selectedFile));
                _stream = new BlockAlignReductionStream(pcm);

            }
            else if (selectedFile.EndsWith(".wav"))
            {
                pcm = new WaveChannel32(new WaveFileReader(selectedFile));
                _stream = new BlockAlignReductionStream(pcm);
            }
            else
                throw new InvalidOperationException("Not a correct audio file type");

            _channel = new WaveChannel32(_stream)
            {
                PadWithZeroes = false
            };

            _channel.Sample += ChannelOnSample;

            // Volume 설정
            SetVolumeDelegate = vol =>
            {
                _volumn = vol;
                _channel.Volume = _volumn;
            };
            SetVolumeDelegate(_volumn);

            _sampleAggregator = new SampleAggregator(FftDataSize);

            _wavePlayer = new WasapiOut(AudioClientShareMode.Shared, false, 150);
            //_wavePlayer = new DirectSoundOut();
            _wavePlayer.Init(_channel);
            _wavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;
        }


        private void ChannelOnSample(object sender, SampleEventArgs sampleEventArgs)
        {
            if (_stream != null)
            {
                _sampleAggregator.Add(sampleEventArgs.Left, sampleEventArgs.Right);
                long repeatStopPosition = (long)((SelectionEnd.TotalSeconds / _stream.TotalTime.TotalSeconds) * _stream.Length);
                if (((SelectionEnd - SelectionBegin) >= TimeSpan.FromMilliseconds(RepeatThreshold)) &&
                    _stream.Position >= repeatStopPosition)
                {

                    _sampleAggregator.Clear();
                }
            }
        }

        public void Seek(long pos)
        {
            if (_stream != null)
            {
                if ((pos % _stream.BlockAlign) != 0)
                    pos -= pos % _stream.BlockAlign;
                // Force new position into valid range
                pos = Math.Max(0, Math.Min(_stream.Length, pos));
                // set position
                _stream.Position = pos;
            }
        }

        public MediaInfo GetMediaInfo(int index)
        {
            return _playlist.MediaList[index];
        }
        private void UpdatePlayIndex(int index)
        {
            if (index == 0)
            {
                _order.OldIndex = _playlist.FileCount - 1;
            }
            else
            {
                _order.OldIndex = index - 1;
            }
            _order.CurrentIndex = index;

            if (++index < _playlist.FileCount)
            {
                _order.NextIndex = index;
            }

            if (RandomState)
            {
                var r = new Random(index);

                int currentIndex = r.Next(0, _playlist.FileCount);
                int nextIndex = r.Next(0, _playlist.FileCount);

                _order.NextIndex = nextIndex;
                _order.CurrentIndex = currentIndex;
            }
        }
        /// <summary>
        /// 한곡 재생 종료시 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="stoppedEventArgs"></param>
        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            // 한 곡 플레이가 인터럽트 없이 끝나면 처리
            if (_order.NextIndex > 0)
            {
                if (NextPlayEvent != null)
                {
                    int playIndex = 0;

                    playIndex = _repeatState == RepeatState.Repeat ? _order.CurrentIndex : _order.NextIndex;

                    var resource = _playlist.MediaList[playIndex].FilePath;

                    UpdatePlayIndex(playIndex); // 인덱스 업데이트

                    if (_currentState.CanPlay(resource, this))
                    {
                        _wavePlayer.Play();
                    }

                    NextPlayEvent(playIndex, GetMediaInfo(playIndex));
                }
            }
        }
        private void DisposeWave()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            if (_channel != null)
            {
                _channel.Dispose();
            }
            if (_wavePlayer != null)
            {
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }
        }

        #region INotifyPropertyChanged

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        public bool GetFFTData(float[] fftDataBuffer)
        {
            _sampleAggregator.GetFFTResults(fftDataBuffer);
            return IsPlaying;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (_stream != null)
                maxFrequency = _stream.WaveFormat.SampleRate / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            return (int)((frequency / maxFrequency) * (FftDataSize / 2));
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get
            {
                if (_wavePlayer == null) return false;
                return _wavePlayer.PlaybackState == PlaybackState.Playing;
            }
            protected set
            {
                bool oldValue = _isPlaying;
                _isPlaying = value;
                if (oldValue != _isPlaying)
                    NotifyPropertyChanged("IsPlaying");
            }
        }

        private SampleAggregator _sampleAggregator;
        private double _channelLength;
        public double ChannelLength
        {
            get { return _channelLength; }
            protected set
            {
                double oldValue = _channelLength;
                _channelLength = value;
                if (oldValue != _channelLength)
                    NotifyPropertyChanged("ChannelLength");
            }
        }

        private double channelPosition;
        private bool inChannelTimerUpdate;

        public double ChannelPosition
        {
            get { return channelPosition; }
            set
            {
                if (!_inChannelSet)
                {
                    _inChannelSet = true; // Avoid recursion
                    double oldValue = channelPosition;
                    double position = Math.Max(0, Math.Min(value, ChannelLength));
                    if (!inChannelTimerUpdate && _stream != null)
                        _stream.Position = (long)((position / _stream.TotalTime.TotalSeconds) * _stream.Length);
                    channelPosition = position;
                    if (oldValue != channelPosition)
                        NotifyPropertyChanged("ChannelPosition");
                    _inChannelSet = false;
                }
            }
        }

        private TimeSpan _repeatStart;
        private TimeSpan _repeatStop;
        private bool _inRepeatSet;
        private bool _inChannelSet;
        private float[] _waveformData;

        public TimeSpan SelectionBegin
        {
            get { return _repeatStart; }
            set
            {
                if (!_inRepeatSet)
                {
                    _inRepeatSet = true;
                    TimeSpan oldValue = _repeatStart;
                    _repeatStart = value;
                    if (oldValue != _repeatStart)
                        NotifyPropertyChanged("SelectionBegin");
                    _inRepeatSet = false;
                }
            }

        }

        public TimeSpan SelectionEnd
        {
            get { return _repeatStop; }
            set
            {
                if (!_inChannelSet)
                {
                    _inRepeatSet = true;
                    TimeSpan oldValue = _repeatStop;
                    _repeatStop = value;
                    if (oldValue != _repeatStop)
                        NotifyPropertyChanged("SelectionEnd");
                    _inRepeatSet = false;
                }
            }

        }

        public float[] WaveformData
        {
            get { return _waveformData; }
            protected set
            {
                float[] oldValue = _waveformData;
                _waveformData = value;
                if (oldValue != _waveformData)
                    NotifyPropertyChanged("WaveformData");
            }
        }
    }
}
