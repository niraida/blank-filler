using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;

namespace BlankFiller.ViewModels
{
    class CreateEditNameForTextBlockDialogViewModel : ReactiveObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        private readonly CreateEditNameForTextBlock _view;

        private string _returnValue;

        public ReactiveCommand Save { get; set; }
        public ReactiveCommand Cancel { get; set; }

        public CreateEditNameForTextBlockDialogViewModel(string title, string value)
        {
            _view = new CreateEditNameForTextBlock();
            _view.Title = title;
            _view.DataContext = this;
            _returnValue = null;

            Name = value;
            Save = ReactiveCommand.Create(() =>
            {
                _returnValue = Name;
                _view.Close();
            }, this.WhenAnyValue(x => x.Name).Select(x => !string.IsNullOrEmpty(x)));

            Cancel = ReactiveCommand.Create(() => _view.Close());
        }

        public string Show()
        {
            _view.ShowDialog();
            return _returnValue;
        }



    }
}
