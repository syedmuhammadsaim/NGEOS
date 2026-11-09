using System.Windows.Controls;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class PublicDonateView : UserControl
    {
        public PublicDonateView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is PublicDonateViewModel vm)
                {
                    await vm.LoadAsync();
                }
            };
        }
    }
}
