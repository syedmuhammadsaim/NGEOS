using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is DashboardViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
