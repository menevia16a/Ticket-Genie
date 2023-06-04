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
            var loginWindow = new LoginWindow();
            Properties.Settings.Default.AccountID = 0;
            Properties.Settings.Default.CharacterGUID = 0;
            Properties.Settings.Default.CurrentTicketID = 0;
            Properties.Settings.Default.Save();

            if (!loginWindow.GetLoginSuccess())
            {
                if (loginWindow.ShowDialog() == true)
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
                else
                    Application.Current.Shutdown();
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        private void OnTicketSelected(object sender, RoutedEventArgs e)
        {
            // Check if a character has been selected, if not then send a message
            if (Properties.Settings.Default.CharacterGUID == 0)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Retrieve selected ticket
            if (sender is ListBox listBox && listBox.SelectedItem is Ticket selectedTicket)
            {
                var ticketDetails = _ticketManager.GetTicket(selectedTicket.id);

                if (ticketDetails != null)
                {
                    // update the UI ticket info
                    TicketID.Text = ticketDetails.id.ToString();
                    TicketName.Text = ticketDetails.name;
                    TicketDescription.Text = ticketDetails.description;
                    Properties.Settings.Default.CurrentTicketID = ticketDetails.id;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void OnCharacterManagerClick(object sender, RoutedEventArgs e)
        {
            // Open the Character Manager window to select a character
            var characterManager = new CharacterManagerWindow();
            characterManager.ShowDialog();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            // Open the AboutWindow
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            // Trigger ticket list updating
            TicketList.Items.Clear();
            var tickets = _ticketManager.GetAllTickets();

            if (tickets == null)
            {
                return;
            }

            foreach ( var ticket in tickets)
            {
                Ticket listItem = new Ticket();
                listItem.id = ticket.id;
                listItem.name = ticket.name;
                listItem.closedBy = ticket.closedBy;
                TicketList.Items.Add(listItem);
            }

            MessageBox.Show("The list of tickets has been updated.", "Ticket List Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnRespondClick(object sender, RoutedEventArgs e)
        {
            // If no ticket is selected, send a message
            if (TicketID.Text?.Length == 0)
            {
                MessageBox.Show("Please select a ticket first.", "No Ticket Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Display GM Response window
            var gmResponseWindow = new ResponseWindow();
            gmResponseWindow.ShowDialog();
        }
    }
}
