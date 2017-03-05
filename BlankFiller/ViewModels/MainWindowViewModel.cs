using Microsoft.Win32;
using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Threading.Tasks;
using BlankFiller.Pdf;
using System.Collections.Generic;
using System;
using BlankFiller.UndoRedo;
using System.Reactive.Subjects;

namespace BlankFiller.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private const int ImageDefaultWidth = 1000;

        public int MinScale { get; set; } = 10;        
        public int MaxScale { get; set; } = 200;
        

        private List<PageViewModel> _pages;
        public List<PageViewModel> Pages
        {
            get { return _pages; }
            set { this.RaiseAndSetIfChanged(ref _pages, value); }
        }

        private int _imageScale;
        public int ImageScale
        {
            get { return _imageScale; }
            set { this.RaiseAndSetIfChanged(ref _imageScale, value); }
        }

        public ProgressObject Progress { get; set; }
        public UndoRedoStack UndoRedoStack { get; set; }

        public ReactiveCommand Open { get; set; }
        public ReactiveCommand Close { get; set; }
        public ReactiveCommand Save { get; set; }
        public ReactiveCommand ExitFromApplication { get; set; }

        public ReactiveCommand ScaleDown { get; set; }
        public ReactiveCommand ScaleUp { get; set; }


        private readonly ObservableAsPropertyHelper<Visibility> _fileNotSelectedVisibility;
        public Visibility FileNotSelectedVisibility => _fileNotSelectedVisibility.Value;

        private readonly ObservableAsPropertyHelper<Visibility> _noImageSelectedVisibility;
        public Visibility NoImageSelectedVisibility => _noImageSelectedVisibility.Value;
        private readonly ObservableAsPropertyHelper<Visibility> _oneImageSelectedVisibility;
        public Visibility OneImageSelectedVisibility => _oneImageSelectedVisibility.Value;
        private readonly ObservableAsPropertyHelper<Visibility> _multipleImageSelectedVisibility;
        public Visibility MultipleImageSelectedVisibility => _multipleImageSelectedVisibility.Value;
        private readonly ObservableAsPropertyHelper<PageViewModel> _selectedPageViewModel;
        public PageViewModel SelectedPageViewModel => _selectedPageViewModel.Value;
        private readonly ObservableAsPropertyHelper<int> _selectedPageDisplayedWidth;
        public int SelectedPageDisplayedWidth => _selectedPageDisplayedWidth.Value;
        //TODO: убрать плохо
        private IDisposable _subscriptionAtSelectedImagesCount;
        private readonly Subject<int> _selectedImagesCount = new Subject<int>();

        public MainWindowViewModel()
        {
            //начальные инициализации

            Progress = new ProgressObject();
            UndoRedoStack = new UndoRedoStack();
            Pages = new List<PageViewModel>();                                  

            //базовые команды

            var hasOpenedFile = this.WhenAnyValue(x => x.Pages).Select(_ => Pages.Any());            
            Open = ReactiveCommand.Create(OpenDocument);
            Close = ReactiveCommand.Create(CloseDocument, hasOpenedFile);
            Save = ReactiveCommand.Create(SaveSelectedPages, _selectedImagesCount.Select(x => x > 0));
            ExitFromApplication = ReactiveCommand.Create(Application.Current.Shutdown);

            //предупреждение о выбранном файле
            _fileNotSelectedVisibility = hasOpenedFile
                .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                .ToProperty(this, x => x.FileNotSelectedVisibility);

            //отображение правой части

            this.WhenAnyValue(x => x.Pages).Subscribe(images =>
            {
                if (_subscriptionAtSelectedImagesCount != null)
                    _subscriptionAtSelectedImagesCount.Dispose();
                _subscriptionAtSelectedImagesCount = images
                    .Select(x => x.WhenAnyValue(y => y.IsSelected))
                    .Merge()
                    .Subscribe(_ => _selectedImagesCount.OnNext(Pages.Count(x => x.IsSelected)));
            });

            _noImageSelectedVisibility = _selectedImagesCount
                .Select(x => x == 0 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.NoImageSelectedVisibility, Visibility.Collapsed);

            _oneImageSelectedVisibility = _selectedImagesCount
                .Select(x =>  x == 1 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.OneImageSelectedVisibility, Visibility.Collapsed);

            _multipleImageSelectedVisibility = _selectedImagesCount
                .Select(x => x > 1 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.MultipleImageSelectedVisibility, Visibility.Collapsed);

            _selectedPageViewModel = _selectedImagesCount
                .Select(x => x == 1 ? Pages.Single(y=>y.IsSelected) : null)
                .ToProperty(this, x => x.SelectedPageViewModel,null);


            ScaleDown = ReactiveCommand.Create(() => ImageScale -= 10, this.WhenAnyValue(x => x.ImageScale).Select(x => x > MinScale));
            ScaleUp = ReactiveCommand.Create(() => ImageScale += 10, this.WhenAnyValue(x => x.ImageScale).Select(x => x < MaxScale));
            _selectedPageDisplayedWidth = this
                .WhenAnyValue(x => x.ImageScale)
                .Select(x => x * ImageDefaultWidth / 100)
                .ToProperty(this, x => x.SelectedPageDisplayedWidth);
        }

        private void SaveSelectedPages()
        {
            
        }

        private void CloseDocument()
        {
            UndoRedoStack.Clear();
            Pages.Clear();
        }

        private async Task OpenDocument()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF файлы (*.pdf) | *.pdf"
            };
            var userSelectedFile = dialog.ShowDialog();
            if (userSelectedFile != true)
                return;

            Progress.Message = "Извлекаем изображения из файла";
            Progress.Progress = 0;
            Progress.Total = 100;
            Progress.Visibility = Visibility.Visible;

            List<string> files;
            try
            {
                files = await PdfImageSaver.ExtractFile(dialog.FileName, Progress);
            }
            catch (Exception ex)
            {
                MessageBox.Show("При открытии файла произошло исключение:" + ex.Message + "\n" + ex.StackTrace);
                Progress.Visibility = Visibility.Hidden;
                return;
            }

            if (!files.Any())
            {
                MessageBox.Show("Пустой список файлов");
                Progress.Visibility = Visibility.Hidden;
                return;
            }

            Progress.Visibility = Visibility.Hidden;
            var arr = new List<PageViewModel>();
            for (int i = 0; i < files.Count; i++)
            {
                arr.Add(new PageViewModel(this, files[i], i + 1));
            }
            Pages = arr;
            ImageScale = 100;
        }
    }
}
