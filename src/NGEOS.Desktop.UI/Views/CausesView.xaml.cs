using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class CausesView : UserControl
    {
        public CausesView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is CausesViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
