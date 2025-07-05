using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for TicketHistory.xaml
    /// </summary>
    public partial class TicketHistoryWindow : Window
    {
        private static int playerGUID;
        private static string playerName;
        private static bool isValidPlayer;

        private readonly DBConnector _dbConnectorCharacters;
        private readonly TicketManager _ticketManager;

        public TicketHistoryWindow()
        {
            InitializeComponent();

            isValidPlayer = false;

            var charactersConnectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.CharacterDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _dbConnectorCharacters = new DBConnector(charactersConnectionString);
            _ticketManager = new TicketManager();

            if (Properties.Settings.Default.PlayerGUID == 0)
            {
                MessageBox.Show("No player was selected, you will be prompted to specify a player's name.", "No Player Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                Properties.Settings.Default.PlayerName = InputDialog.ShowDialog("Please enter a player's name", false);
                Properties.Settings.Default.Save();

                if (Properties.Settings.Default.PlayerName?.Length == 0) { InvalidPlayer(); }
                else
                {
                    // Verify name given is actually a player
                    using (var connection = _dbConnectorCharacters.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            var command = new MySqlCommand("SELECT guid FROM characters WHERE name = @name LIMIT 1", connection);
                            playerName = Properties.Settings.Default.PlayerName;
                            command.Parameters.AddWithValue("@name", playerName);
                            var reader = command.ExecuteReader();

                            if (reader.Read() && reader.HasRows)
                            {
                                playerGUID = reader.GetInt32(0);
                            }

                            _dbConnectorCharacters.CloseConnection(command, reader);

                            if (playerGUID == 0)
                                InvalidPlayer();
                            else
                                isValidPlayer = true;
                        }
                        catch (MySqlException ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                isValidPlayer = true;
                playerGUID = Properties.Settings.Default.PlayerGUID;
                playerName = Properties.Settings.Default.PlayerName;
            }

            Loaded += TicketHistoryWindow_Loaded;
        }

        private void TicketHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isValidPlayer)
                Close();

            // Retrieve all tickets and display them on the UI
            var tickets = _ticketManager.GetTicketHistory();

            if (tickets == null)
                return;

            foreach (var ticket in tickets)
            {
                // Add the ticket to the left side list on the main window
                Ticket listItem = new Ticket
                {
                    id = ticket.id,
                    name = ticket.name
                };

                TicketList.Items.Add(listItem);
            }
        }

        private void TicketHisoryWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Reset PlayerGUID and PlayerName settings
            Properties.Settings.Default.PlayerGUID = 0;
            Properties.Settings.Default.PlayerName = "";
            Properties.Settings.Default.Save();
        }

        private void OnTicketSelected(object sender, RoutedEventArgs e)
        {
            // Retrieve selected ticket
            if (sender is ListBox listBox && listBox.SelectedItem is Ticket selectedTicket)
            {
                var ticketDetails = _ticketManager.GetTicket(selectedTicket.id);

                if (ticketDetails != null)
                {
                    // Convert Linux timestamp to readable date
                    var creationTime = DateTimeOffset.FromUnixTimeSeconds(ticketDetails.createTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    var lastModifiedTime = DateTimeOffset.FromUnixTimeSeconds(ticketDetails.lastModifiedTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                    // Update the UI ticket info
                    TicketID.Text = ticketDetails.id.ToString();
                    TicketName.Text = ticketDetails.name;
                    PlayerGUID.Text = ticketDetails.playerGUID.ToString();
                    ViewedCount.Text = ticketDetails.viewed.ToString();
                    Creation.Text = creationTime;
                    LastModified.Text = lastModifiedTime;
                    HandledBy.Text = ticketDetails.handledBy;
                    TicketDescription.Text = ticketDetails.description;
                }
            }
        }

        private void InvalidPlayer()
        {
            isValidPlayer = false;

            MessageBox.Show("Could not find a player associated with that name, the Account Tools window will now close.", "No Player Found", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnReadResponseClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ResponseTicketID = int.Parse(TicketID.Text);
            Properties.Settings.Default.Save();

            var readResponseWindow = new ReadResponseWindow();
            readResponseWindow.Show();
        }
    }
}
