using ReactiveUI;
using System.Windows;

namespace BlankFiller.ViewModels
{
    internal class ProgressObject : ReactiveObject
    {
        public ProgressObject()
        {
            Visibility = Visibility.Collapsed;
        }

        private Visibility _visibility;
        public Visibility Visibility
        {
            get { return _visibility; }
            set { this.RaiseAndSetIfChanged(ref _visibility, value); }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { this.RaiseAndSetIfChanged(ref _progress, value); }
        }

        private int _total;
        public int Total
        {
            get { return _total; }
            set { this.RaiseAndSetIfChanged(ref _total, value); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { this.RaiseAndSetIfChanged(ref _message, value); }
        }

    }
}
