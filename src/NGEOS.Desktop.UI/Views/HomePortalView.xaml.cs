using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class HomePortalView : UserControl
    {
        public HomePortalView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is HomePortalViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
