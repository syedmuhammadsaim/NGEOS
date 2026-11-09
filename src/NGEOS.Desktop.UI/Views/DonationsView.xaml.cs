using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class DonationsView : UserControl
    {
        public DonationsView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is DonationsViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
