using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for ReadResponseWindow.xaml
    /// </summary>
    public partial class ReadResponseWindow : Window
    {
        private readonly TicketManager _ticketManager;

        private static bool isValidTicket;

        public ReadResponseWindow()
        {
            InitializeComponent();
            _ticketManager = new TicketManager();

            isValidTicket = Properties.Settings.Default.ResponseTicketID != 0;

            Loaded += ReadResponseWindow_Loaded;
        }

        private void ReadResponseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isValidTicket)
                Close();

            // Retreive ticket response and display it in the UI
            var ticket = _ticketManager.GetTicket(Properties.Settings.Default.ResponseTicketID);

            if (ticket != null) { TicketResponse.Text = ticket.response; }
        }

        private void ReadResponseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ResponseTicketID = 0;
            Properties.Settings.Default.Save();
        }
    }
}
