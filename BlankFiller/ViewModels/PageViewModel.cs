using BlankFiller.Models;
using BlankFiller.UndoRedo;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Input;

namespace BlankFiller.ViewModels
{
    internal class PageViewModel : ReactiveObject
    {
        private string _original;

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
                
        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set { this.RaiseAndSetIfChanged(ref _imagePath, value); }
        }

        private ReadOnlyCollection<TextBlock> _textBlocks;
        public ReadOnlyCollection<TextBlock> TextBlocks
        {
            get { return _textBlocks; }
            set { this.RaiseAndSetIfChanged(ref _textBlocks, value); }
        }

        public ReactiveCommand SelectImage { get; set; }
        public ReactiveCommand SelectMoreOneImage { get; set; }

        public PageViewModel(MainWindowViewModel parent, string pathToImage, int number)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            _original = pathToImage;
            TextBlocks = new ReadOnlyCollection<TextBlock>(new List<TextBlock>());
            Number = number;
            SelectImage = ReactiveCommand.Create(() =>
            {
                parent.UndoRedoStack.ExecuteNewCommand(new SelectImage(parent.Pages, this));
            });
            SelectMoreOneImage = ReactiveCommand.Create(() =>
            {
                parent.UndoRedoStack.ExecuteNewCommand(new SelectMoreOneImage(this));
            });

            this.WhenAnyValue(x => x.TextBlocks).Subscribe(_ => RedrawImage());
        }

        private void RedrawImage()
        {
            var font = new Font("Arial", 16);
            using (var image = Image.FromFile(_original))
            {                
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    var stringFormat = new StringFormat()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    foreach (var textBlock in TextBlocks)
                    {

                        for (int i = 0; i < textBlock.Text.Length; i++)
                        {
                            var rect = new RectangleF(textBlock.X + i * 63, textBlock.Y, 52, 78);
                            g.DrawString(textBlock.Text.Substring(i, 1), font, Brushes.Black, rect, stringFormat);
                        }
                        
                    }
                    
                }

                var tempFileName = Path.GetTempFileName();
                image.Save(tempFileName);
                ImagePath = tempFileName;
            }
        }

    }
}
