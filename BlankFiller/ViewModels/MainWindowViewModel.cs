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

namespace BlankFiller.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private List<PageViewModel> _pages;
        public List<PageViewModel> Pages
        {
            get { return _pages; }
            set { this.RaiseAndSetIfChanged(ref _pages, value); }
        }

        public ProgressObject Progress { get;  set; }
        public UndoRedoStack UndoRedoStack { get; set; }

        public ReactiveCommand Open { get; set; }
        public ReactiveCommand Close { get; set; }
        public ReactiveCommand Save { get; set; }
        public ReactiveCommand SaveSelected { get; set; }
        public ReactiveCommand ExitFromApplication { get; set; }

        private readonly ObservableAsPropertyHelper<Visibility> _fileNotSelectedVisibility;
        public Visibility FileNotSelectedVisibility => _fileNotSelectedVisibility.Value;

        public MainWindowViewModel()
        {
            Pages = new List<PageViewModel>();
            var hasOpenedFile = this.WhenAnyValue(x => x.Pages).Select(_ => Pages.Any());

            Progress = new ProgressObject();
            UndoRedoStack = new UndoRedoStack();

            Open = ReactiveCommand.Create(OpenDocument);            
            Close = ReactiveCommand.Create(() => { MessageBox.Show("Открываем документ"); }, hasOpenedFile);
            Save = ReactiveCommand.Create(() => { MessageBox.Show("Открываем документ"); }, hasOpenedFile);
            SaveSelected = ReactiveCommand.Create(() => { MessageBox.Show("Открываем документ"); }, hasOpenedFile);
            ExitFromApplication = ReactiveCommand.Create(() => { Application.Current.Shutdown(); });

           

            _fileNotSelectedVisibility = hasOpenedFile
                .Select(x => x ? Visibility.Collapsed : Visibility.Visible)
                .ToProperty(this, x => x.FileNotSelectedVisibility);
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
        }
    }
}
