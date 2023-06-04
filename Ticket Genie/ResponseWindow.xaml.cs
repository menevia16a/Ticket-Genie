using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for ResponseWindow.xaml
    /// </summary>
    public partial class ResponseWindow : Window
    {
        public ResponseWindow()
        {
            InitializeComponent();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) { Close(); }

        private void OnFinalizeClick(object sender, RoutedEventArgs e)
        {
            // TODO: Handle Finalize button click
        }
    }
}
