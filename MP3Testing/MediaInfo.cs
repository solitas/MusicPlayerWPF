using System;
using System.ComponentModel;
using System.Windows.Media;
using Id3Lib;
using Mp3Lib;

namespace MP3Testing
{
    /// <summary>
    /// 현재 이미지를 메모리에 올림 (수정 요망)
    /// </summary>
    public class MediaInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string resource)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(resource));
            }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                var oldTitle = _title;
                _title = value;
                if (oldTitle != _title)
                {
                    OnPropertyChanged("Title");
                }
            }
        }
        private string _artist;

        public string Artist
        {
            get { return _artist; }
            set
            {
                var oldArtist = _artist;
                _artist = value;
                if (oldArtist != _artist)
                {
                    OnPropertyChanged("Artist");
                }
            }
        }
        private string _song;

        public string Song
        {
            get { return _song; }
            set { _song = value; }
        }
        private string _track;

        public string Track
        {
            get { return _track; }
            set { _track = value; }
        }
        private string _genre;

        public string Genre
        {
            get { return _genre; }
            set { _genre = value; }
        }
        private string _album;

        public string Album
        {
            get { return _album; }
            set
            {
                var oldAlbum = _album;
                _album = value;
                if (oldAlbum != _album)
                {
                    OnPropertyChanged("Album");
                }
            }
        }
        private string _year;

        public string Year
        {
            get { return _year; }
            set { _year = value; }
        }
        private string _disc;

        public string Disc
        {
            get { return _disc; }
            set { _disc = value; }
        }
        private string _comment;

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }
        private ImageSource _image;

        public ImageSource Image
        {
            get { return _image; }
            set { _image = value; }
        }

        private string _filePath;

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        private double _vbr;

        public double VBR
        {
            get { return _vbr; }
            set { _vbr = value; }
        }

        private TimeSpan _totalTime;

        public TimeSpan TotalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; }
        }

        public MediaInfo()
        {
            
        }

        public MediaInfo(string filePath)
        {
            SetData(filePath);
        }

        public void SetData(string filePath)
        {
            try
            {
                var file = new Mp3File(filePath);
                var handler = file.TagHandler;

                if (file.Audio.BitRateVbr != null)
                    _vbr = file.Audio.BitRateVbr.Value;
                _totalTime = TimeSpan.FromSeconds(file.Audio.Duration);

                _filePath = filePath;

                _artist = Utils.ConvertEncoding(handler.Artist);
                _album = Utils.ConvertEncoding(handler.Album);
                _title = Utils.ConvertEncoding(handler.Title);
                _song = Utils.ConvertEncoding(handler.Song);
                _track = handler.Track;
                _genre = handler.Genre;
                _disc = handler.Disc;
                _year = handler.Year;
                _comment = handler.Comment;
                _image = Utils.GetImageStream(handler.Picture);
            }
            catch (Exception e)
            {

            }
        }
    }
}
