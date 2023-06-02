using System.Windows;
using System.Windows.Controls;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TicketManager _ticketManager;

        public MainWindow()
        {
            InitializeComponent();
            _ticketManager = new TicketManager();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve all tickets and display them on the UI
            var tickets = _ticketManager.GetAllTickets();

            if (tickets == null)
            {
                return;
            }

            foreach (var ticket in tickets)
            {
                //Add the ticket to the left side list on the main window
                Ticket listItem = new Ticket();
                listItem.id = ticket.id;
                listItem.name = ticket.name;
                listItem.closedBy = ticket.closedBy;
                TicketList.Items.Add(listItem);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        private void OnTicketSelected(object sender, RoutedEventArgs e)
        {
            // Retrieve selected ticket from list view
            if (sender is ListBox listBox && listBox.SelectedItem is Ticket selectedTicket)
            {
                var ticketDetails = _ticketManager.GetTicket(selectedTicket.id);

                if (ticketDetails != null)
                {
                    // update the UI ticket info
                    TextBlock ticketIDBlock = (TextBlock)FindName("TicketID");
                    TextBlock ticketNameBlock = (TextBlock)FindName("TicketName");
                    TextBlock ticketDescriptionBlock = (TextBlock)FindName("TicketDescription");
                    ticketIDBlock.Text = ticketDetails.id.ToString();
                    ticketNameBlock.Text = ticketDetails.name;
                    ticketDescriptionBlock.Text = ticketDetails.description;
                }
            }
        }
    }
}
