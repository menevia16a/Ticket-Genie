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
                // Add the ticket to the left side list on the main window
                var listItem = new ListViewItem();
                // Get the name property from the ticket object as the content for the list item
                listItem.Content = ticket.name;
                TicketList.Items.Add(listItem);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        private void OnTicketSelected(object sender, RoutedEventArgs e)
        {
            // Retrieve selected ticket from list view
            if (sender is ListView listView && listView.SelectedItem is Ticket selectedTicket)
            {
                var ticketDetails = _ticketManager.GetTicket(selectedTicket.id);

                if (ticketDetails != null)
                {
                    TextBlock ticketIDBlock = (TextBlock)FindName("TicketID");
                    TextBlock ticketNameBlock = (TextBlock)FindName("TicketName");
                    TextBlock ticketDescriptionBlock = (TextBlock)FindName("TicketDescription");
                    ticketIDBlock.Text = selectedTicket.id.ToString();
                    ticketNameBlock.Text = selectedTicket.name;
                    ticketDescriptionBlock.Text = ticketDetails.description;
                }
            }
        }
    }
}
