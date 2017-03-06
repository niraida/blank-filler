using BlankFiller.ViewModels;

namespace BlankFiller
{
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel = new MainWindowViewModel();       
        }

        private void BlankImageOnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {      
            var p = e.GetPosition(BlankImage);
            _viewModel.PointSelected(p.X, p.Y);
        }
    }
}
