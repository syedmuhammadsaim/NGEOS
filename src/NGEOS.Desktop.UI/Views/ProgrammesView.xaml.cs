using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class ProgrammesView : UserControl
    {
        public ProgrammesView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ProgrammesViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
