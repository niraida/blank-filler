using BlankFiller.UndoRedo;
using ReactiveUI;
using System;
using System.Windows.Input;

namespace BlankFiller.ViewModels
{
    internal class PageViewModel : ReactiveObject
    {

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { this.RaiseAndSetIfChanged(ref _isSelected, value); }
        }


        private int _number;
        public int Number
        {
            get { return _number; }
            set { this.RaiseAndSetIfChanged(ref _number, value); }
        }

        private string _original;
        public string Original
        {
            get { return _original; }
            set { this.RaiseAndSetIfChanged(ref _original, value); }
        }


        public ReactiveCommand SelectImage { get; set; }
        public ReactiveCommand SelectMoreOneImage { get; set; }

        public PageViewModel(MainWindowViewModel parent, string pathToImage, int number)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            Original = pathToImage;
            Number = number;
            SelectImage = ReactiveCommand.Create(() =>
            {
                parent.UndoRedoStack.ExecuteNewCommand(new SelectImage(parent.Pages, this));
            });
            SelectMoreOneImage = ReactiveCommand.Create(() =>
            {
                parent.UndoRedoStack.ExecuteNewCommand(new SelectMoreOneImage(this));
            });
        }

    }
}
