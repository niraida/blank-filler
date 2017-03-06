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
using BlankFiller.Models;

using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace BlankFiller.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private const int ImageDefaultWidth = 2420;

        public int MinScale { get; set; } = 10;       

        public int MaxScale { get; set; } = 200;


        private List<PageViewModel> _pages;
        public List<PageViewModel> Pages
        {
            get { return _pages; }
            set { this.RaiseAndSetIfChanged(ref _pages, value); }
        }

        private TextBlock _selectedTextBlock;
        public TextBlock SelectedTextBlock
        {
            get { return _selectedTextBlock; }
            set { this.RaiseAndSetIfChanged(ref _selectedTextBlock, value); }
        }

        private int _imageScale;
        public int ImageScale
        {
            get { return _imageScale; }
            set { this.RaiseAndSetIfChanged(ref _imageScale, value); }
        }

        private bool _selectPointMode;
        public bool SelectPointMode
        {
            get { return _selectPointMode; }
            set { this.RaiseAndSetIfChanged(ref _selectPointMode, value); }
        }

        public ProgressObject Progress { get; set; }
        public UndoRedoStack UndoRedoStack { get; set; }

        public ReactiveCommand Open { get; set; }
        public ReactiveCommand Close { get; set; }
        public ReactiveCommand Save { get; set; }
        public ReactiveCommand ExitFromApplication { get; set; }

        public ReactiveCommand ScaleDown { get; set; }
        public ReactiveCommand ScaleUp { get; set; }


        public ReactiveCommand AddTextBlock { get; set; }
        public ReactiveCommand EditTextBlock { get; set; }
        public ReactiveCommand DeleteTextBlock { get; set; }

        public ReactiveCommand ImportTextBlocksFromFile { get; set; }
        public ReactiveCommand ExportTextBlocksToFile { get; set; }

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
        private readonly ObservableAsPropertyHelper<Visibility> _selectPointLabelVisibility;
        public Visibility SelectPointLabelVisibility => _selectPointLabelVisibility.Value;


        //TODO: убрать плохо

        private IDisposable _subscriptionAtSelectedImagesCount;
        private readonly Subject<int> _selectedImagesCount = new Subject<int>();
        private IDisposable _subscriptionAtHasFieldsOnSelectedSheet;
        private readonly Subject<bool> _hasFieldsOnSelectedSheet = new Subject<bool>();

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
                .Select(x => x == 1 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.OneImageSelectedVisibility, Visibility.Collapsed);

            _multipleImageSelectedVisibility = _selectedImagesCount
                .Select(x => x > 1 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.MultipleImageSelectedVisibility, Visibility.Collapsed);

            _selectedPageViewModel = _selectedImagesCount
                .Select(x => x == 1 ? Pages.Single(y => y.IsSelected) : null)
                .ToProperty(this, x => x.SelectedPageViewModel, null);


            ScaleDown = ReactiveCommand.Create(() => ImageScale -= 10, this.WhenAnyValue(x => x.ImageScale).Select(x => x > MinScale));
            ScaleUp = ReactiveCommand.Create(() => ImageScale += 10, this.WhenAnyValue(x => x.ImageScale).Select(x => x < MaxScale));
            _selectedPageDisplayedWidth = this
                .WhenAnyValue(x => x.ImageScale)
                .Select(x => x * ImageDefaultWidth / 100)
                .ToProperty(this, x => x.SelectedPageDisplayedWidth);


            //команды редактирования/создания полей

            AddTextBlock = ReactiveCommand.Create(AddTextBlockToBlank, this.WhenAnyValue(x => x.SelectedPageViewModel).Select(x => x != null));
            EditTextBlock = ReactiveCommand.Create(EditTextBlockAtBlank, this.WhenAnyValue(x => x.SelectedTextBlock).Select(x => x != null));
            DeleteTextBlock = ReactiveCommand.Create(DeleteTextBlockFromBlank, this.WhenAnyValue(x => x.SelectedTextBlock).Select(x => x != null));
            ImportTextBlocksFromFile = ReactiveCommand.Create(ImportTextBlocks, this.WhenAnyValue(x => x.SelectedPageViewModel).Select(x => x != null));

            this
                .WhenAnyValue(x => x.SelectedPageViewModel)
                .Subscribe(x =>
                {
                    if (x == null)
                    {
                        _hasFieldsOnSelectedSheet.OnNext(false);
                        return;
                    }
                    if (_subscriptionAtHasFieldsOnSelectedSheet != null)
                        _subscriptionAtHasFieldsOnSelectedSheet.Dispose();

                    _subscriptionAtHasFieldsOnSelectedSheet = x.WhenAnyValue(y => y.TextBlocks).Subscribe(y =>
                    {
                        _hasFieldsOnSelectedSheet.OnNext(y.Any());
                    });
                });

            ExportTextBlocksToFile = ReactiveCommand.Create(ExportTextBlocks, _hasFieldsOnSelectedSheet);
            SelectPointMode = false;

            _selectPointLabelVisibility = this.WhenAnyValue(x => x.SelectPointMode)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.SelectPointLabelVisibility, Visibility.Collapsed);
        }

        private void AddTextBlockToBlank()
        {
            SelectPointMode = true;
        }

        public void PointSelected(double x, double y)
        {
            SelectPointMode = false;
            if (SelectedPageViewModel == null)
                return;      

            var viewModel = new CreateEditNameForTextBlockDialogViewModel("Добавление текста", "");
            var name = viewModel.Show();
            if (string.IsNullOrEmpty(name))
                return;

            x -= 2;
            y -= 5;

            var textBlocks = SelectedPageViewModel.TextBlocks.Select(i => (TextBlock)i.Clone()).ToList();
            textBlocks.Add(new TextBlock(name, (int)x, (int)y));            
            UndoRedoStack.ExecuteNewCommand(new ChangeTextBlockListCommand(SelectedPageViewModel, textBlocks));
        }

        private void EditTextBlockAtBlank()
        {
            if (SelectedPageViewModel == null)
                return;
            if (SelectedTextBlock == null)
                return;

            var viewModel = new CreateEditNameForTextBlockDialogViewModel("Редактирование текста", SelectedTextBlock.Text);
            var newName = viewModel.Show();
            if (string.IsNullOrEmpty(newName))
                return;

            var textBlocks = new List<TextBlock>();
            foreach (var textBlock in SelectedPageViewModel.TextBlocks)
            {
                var text = textBlock == SelectedTextBlock ? newName : textBlock.Text;
                textBlocks.Add(new TextBlock(text, textBlock.X, textBlock.Y));                
            }
            UndoRedoStack.ExecuteNewCommand(new ChangeTextBlockListCommand(SelectedPageViewModel, textBlocks));

        }

        private void DeleteTextBlockFromBlank()
        {
            if (SelectedPageViewModel == null)
                return;
            if (SelectedTextBlock == null)
                return;

            var textBlocks = SelectedPageViewModel.TextBlocks.ToList();
            textBlocks.Remove(SelectedTextBlock);
            UndoRedoStack.ExecuteNewCommand(new ChangeTextBlockListCommand(SelectedPageViewModel, textBlocks));
        }

        private void ImportTextBlocks()
        {
            if (SelectedPageViewModel == null)
                return;

            var openFileDialog = new OpenFileDialog()
            {
                Filter = "JSON файлы (*.json) | *.json"
            };
            var res = openFileDialog.ShowDialog();
            if (res != true)
                return;

            List<TextBlock> textBlocks;
            try
            {
                var fileContent = File.ReadAllText(openFileDialog.FileName);
                textBlocks = JsonConvert.DeserializeObject<List<TextBlock>>(fileContent);
            }
            catch
            {
                MessageBox.Show("Ошибка чтения файла");
                return;
            }

            UndoRedoStack.ExecuteNewCommand(new ChangeTextBlockListCommand(SelectedPageViewModel, textBlocks));
        }

        private void ExportTextBlocks()
        {
            var saveDialog = new SaveFileDialog()
            {
                Filter = "JSON файлы (*.json) | *.json"
            };
            var res = saveDialog.ShowDialog();
            if (res != true)
                return;

            File.WriteAllText(saveDialog.FileName, JsonConvert.SerializeObject(SelectedPageViewModel.TextBlocks), Encoding.UTF8);
        }

        private void SaveSelectedPages()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            var filesToSave = Pages.Where(x => x.IsSelected).ToList(); ;


            for (var i = 0; i < filesToSave.Count; i++)
            {
                var page = filesToSave[i];
                var path = Path.Combine(dialog.SelectedPath, page.Number + ".png");
                File.Copy(page.ImagePath, path, true);
                Progress.Progress = i + 1;
            }
            MessageBox.Show("Файлы сохранены в " + dialog.SelectedPath);
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
