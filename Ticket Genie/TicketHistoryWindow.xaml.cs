using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for TicketHistory.xaml
    /// </summary>
    public partial class TicketHistoryWindow : Window
    {
        public TicketHistoryWindow()
        {
            InitializeComponent();

            Loaded += TicketHistoryWindow_Loaded;
        }

        private void TicketHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
