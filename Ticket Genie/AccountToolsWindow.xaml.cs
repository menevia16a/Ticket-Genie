using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Essy.Tools.InputBox;
using MySql.Data.MySqlClient;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for AccountToolsWindow.xaml
    /// </summary>
    public partial class AccountToolsWindow : Window
    {
        private enum PlayerFaction : byte
        {
            Alliance = 0,
            Horde = 1,
            Invalid = 2
        }

        // Declare a dictionary to map races to factions
        private Dictionary<int, PlayerFaction> raceToFaction = new Dictionary<int, PlayerFaction>
        {
            { 1, PlayerFaction.Alliance }, // Human
            { 3, PlayerFaction.Alliance }, // Dwarf
            { 4, PlayerFaction.Alliance }, // Night Elf
            { 7, PlayerFaction.Alliance }, // Gnome
            { 11, PlayerFaction.Alliance }, // Draenei
            { 2, PlayerFaction.Horde }, // Orc
            { 5, PlayerFaction.Horde }, // Undead
            { 6, PlayerFaction.Horde }, // Tauren
            { 8, PlayerFaction.Horde }, // Troll
            { 10, PlayerFaction.Horde }, // Blood Elf
        };

        // Declare dictionaries to map city names to PortLocation objects
        private Dictionary<string, PortLocation> alliancePortLocations = new Dictionary<string, PortLocation>
        {
            { "Stormwind", new PortLocation { name = "Stormwind", positionX = -8877.911133f, positionY = 671.453857f, positionZ = 104.950226f, map = 0, orientation = 0.583116f } },
            { "Ironforge", new PortLocation { name = "Ironforge", positionX = -4839.234863f, positionY = -871.912659f, positionZ = 510.246735f, map = 0, orientation = 4.900395f } },
            { "Darnassus", new PortLocation { name = "Darnassus", positionX = 10139.388672f, positionY = 2214.697510f, positionZ = 1329.984741f, map = 1, orientation = 2.186495f } },
            { "Exodar", new PortLocation { name = "Exodar", positionX = -3734.079102f, positionY = -11694.936523f, positionZ = -105.742058f, map = 530, orientation = 3.260193f } }
        };
        
        private Dictionary<string, PortLocation> hordePortLocations = new Dictionary<string, PortLocation>
        {
            { "Orgrimmar", new PortLocation { name = "Orgrimmar", positionX = 1636.935791f, positionY = -4443.699707f, positionZ = 15.633880f, map = 1, orientation = 2.622632f } },
            { "Undercity", new PortLocation {name = "Undercity", positionX = 1644.060913f, positionY = 218.970184f, positionZ = -43.102631f, map = 0, orientation = 2.709491f} },
            { "Thunder Bluff", new PortLocation {name = "Thunder Bluff", positionX = -1307.720215f, positionY = 35.120102f, positionZ = 129.208679f, map = 1, orientation = 0.451448f} },
            { "Silvermoon City", new PortLocation {name = "Silvermoon City", positionX = 9683.867188f, positionY = -7383.734863f, positionZ = 22.795446f, map = 530, orientation = 1.505019f} },
        };

        private static PlayerFaction playerFaction;
        private PortLocation portLocation;
        private static int playerGUID;
        private static string playerName;
        private static bool isValidPlayer;
        private readonly TCSOAPService _tcSoapService;
        private readonly DBConnector _dbConnectorCharacters;
        private readonly DBConnector _dbConnectorWorld;

        public AccountToolsWindow()
        {
            InitializeComponent();

            var charactersConnectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.CharacterDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            var worldConnectionString = new MySqlConnectionStringBuilder
            {
                Server = Properties.Settings.Default.SQLHost,
                Port = (uint)Properties.Settings.Default.SQLPort,
                Database = Properties.Settings.Default.WorldDB,
                UserID = Properties.Settings.Default.SQLUsername,
                Password = Properties.Settings.Default.SQLPassword
            }.ToString();

            _tcSoapService = new TCSOAPService();
            _dbConnectorCharacters = new DBConnector(charactersConnectionString);
            _dbConnectorWorld = new DBConnector(worldConnectionString);

            if (Properties.Settings.Default.PlayerGUID == 0)
            {
                MessageBox.Show("No player was selected, you will be prompted to specify a player's name.", "No Player Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                Properties.Settings.Default.PlayerName = InputBox.ShowInputBox("Please enter a player's name", false);
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
                            var command = new MySqlCommand("SELECT guid, name FROM characters WHERE name = @name LIMIT 1", connection);
                            command.Parameters.AddWithValue("@name", Properties.Settings.Default.PlayerName);
                            var reader = command.ExecuteReader();

                            if (reader.Read() && reader.HasRows)
                            {
                                playerGUID = reader.GetInt32(0);
                                playerName = reader.GetString(1);
                            }

                            _dbConnectorCharacters.CloseConnection(reader);

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
                playerGUID = Properties.Settings.Default.PlayerGUID;
                playerName = Properties.Settings.Default.PlayerName;
            }

            Loaded += AccountToolsWindow_Loaded;
        }

        private void AccountToolsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the player's faction and store it
            using (var connection = _dbConnectorCharacters.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT race FROM characters WHERE guid = @guid LIMIT 1", connection);
                    command.Parameters.AddWithValue("@guid", playerGUID);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // Check if the player is Alliance or Horde by their race id
                        int playerRace = reader.GetInt16(0);
                        playerFaction = GetPlayerFaction(playerRace);

                        if (playerFaction == PlayerFaction.Invalid)
                        {
                            MessageBox.Show("Invalid player faction.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else if (playerFaction == PlayerFaction.Alliance)
                        {
                            // Set the available porting cities to Alliance cities
                            PortComboBoxItem1.Content = alliancePortLocations.ElementAt(0).Key;
                            PortComboBoxItem2.Content = alliancePortLocations.ElementAt(1).Key;
                            PortComboBoxItem3.Content = alliancePortLocations.ElementAt(2).Key;
                            PortComboBoxItem4.Content = alliancePortLocations.ElementAt(3).Key;
                        }
                        else if (playerFaction == PlayerFaction.Horde)
                        {
                            // Set the available porting cities to Horde cities
                            PortComboBoxItem1.Content = hordePortLocations.ElementAt(0).Key;
                            PortComboBoxItem2.Content = hordePortLocations.ElementAt(1).Key;
                            PortComboBoxItem3.Content = hordePortLocations.ElementAt(2).Key;
                            PortComboBoxItem4.Content = hordePortLocations.ElementAt(3).Key;
                        }
                    }

                    _dbConnectorCharacters.CloseConnection(reader);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (!isValidPlayer)
                Close();
        }

        private void AccountToolsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Reset PlayerGUID and PlayerName settings
            Properties.Settings.Default.PlayerGUID = 0;
            Properties.Settings.Default.PlayerName = "";
            Properties.Settings.Default.Save();
        }

        private void OnBanClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Verify duration format
            string durationText = DurationTextBox.Text.Trim().ToLower();

            if (!Regex.IsMatch(durationText, @"^\d+d\d+h\d+m$") && !DurationTextBox.Text.StartsWith("-"))
            {
                MessageBox.Show("Invalid input format, negative value leads to permban, otherwise use a timestring like \"4d20h3s\".", "Wrong Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (DurationTextBox.Text.StartsWith("-"))
                DurationTextBox.Text = "-1";
            else
                DurationTextBox.Text = durationText;

            var banReason = ReasonTextBox.Text;
            var banDuration = DurationTextBox.Text;

            if (_tcSoapService.ExecuteSOAPCommand($"ban playeraccount {playerName} {banDuration} {banReason} by GM account {Properties.Settings.Default.AccountID}."))
                MessageBox.Show($"Player {playerName}'s account has been banned.\r\nReason: {banReason}", "Account Banned", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnUnbanClick(object sender, RoutedEventArgs e)
        {
            // Unban player account
            if (_tcSoapService.ExecuteSOAPCommand($"unban playeraccount {playerName}"))
                MessageBox.Show($"Player {playerName}'s account has been unbanned.", "Account Unbanned", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPortClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Check if player is logged in
            using (var connection = _dbConnectorCharacters.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT online FROM characters WHERE guid = @guid LIMIT 1", connection);
                    command.Parameters.AddWithValue("@guid", playerGUID);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // Check if the player is online
                        int playerOnline = reader.GetInt16(0);

                        if (playerOnline == 1)
                        {
                            MessageBox.Show("Player must be offline to be ported.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    _dbConnectorCharacters.CloseConnection(reader);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Get the selected port location
            string portName = PortComboBox.Text;

            if (portName?.Length == 0)
            {
                MessageBox.Show("Please select a port location.", "Porting Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the port location object
            switch (playerFaction)
            {
                case PlayerFaction.Alliance:
                    if (!alliancePortLocations.TryGetValue(portName, out portLocation))
                    {
                        MessageBox.Show("Invalid port location.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                case PlayerFaction.Horde:
                    if (!hordePortLocations.TryGetValue(portName, out portLocation))
                    {
                        MessageBox.Show("Invalid port location.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                case PlayerFaction.Invalid:
                    MessageBox.Show("Invalid player faction.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            // Port the player to the selected location
            using (var connection = _dbConnectorCharacters.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("UPDATE characters SET position_x = @positionX, position_y = @positionY, position_z = @positionZ, map = @map, orientation = @orientation WHERE guid = @guid", connection);
                    command.Parameters.AddWithValue("@positionX", portLocation.positionX);
                    command.Parameters.AddWithValue("@positionY", portLocation.positionY);
                    command.Parameters.AddWithValue("@positionZ", portLocation.positionZ);
                    command.Parameters.AddWithValue("@map", portLocation.map);
                    command.Parameters.AddWithValue("@orientation", portLocation.orientation);
                    command.Parameters.AddWithValue("@guid", playerGUID);
                    command.ExecuteNonQuery();

                    _dbConnectorCharacters.CloseConnection();

                    MessageBox.Show($"Player {playerName} has been ported to {portName}.", "Port Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMailClick(object sender, RoutedEventArgs e)
        {
            if (playerGUID == 0)
                return;

            // Verify fields have been filled
            if (SubjectTextBox.Text?.Length == 0 || MessageTextBox.Text?.Length == 0)
            {
                MessageBox.Show("Both the subject and the message are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check ItemIdTextBoxes for item ids
            int amountOfItems = 0;
            int invalidItems = 0;
            int itemID1 = 0;
            int itemID2 = 0;
            int itemID3 = 0;
            int itemID4 = 0;
            int itemID5 = 0;
            int itemID6 = 0;
            int itemCount1 = 0;
            int itemCount2 = 0;
            int itemCount3 = 0;
            int itemCount4 = 0;
            int itemCount5 = 0;
            int itemCount6 = 0;

            // Parse item ids
            if (ItemIdTextBox1.Text != string.Empty)
                int.TryParse(ItemIdTextBox1.Text, out itemID1);
            if (ItemIdTextBox2.Text != string.Empty)
                int.TryParse(ItemIdTextBox2.Text, out itemID2);
            if (ItemIdTextBox3.Text != string.Empty)
                int.TryParse(ItemIdTextBox3.Text, out itemID3);
            if (ItemIdTextBox4.Text != string.Empty)
                int.TryParse(ItemIdTextBox4.Text, out itemID4);
            if (ItemIdTextBox5.Text != string.Empty)
                int.TryParse(ItemIdTextBox5.Text, out itemID5);
            if (ItemIdTextBox6.Text != string.Empty)
                int.TryParse(ItemIdTextBox6.Text, out itemID6);

            // Parse the amount of each item
            if (ItemAmountTextBox1.Text != string.Empty)
                int.TryParse(ItemAmountTextBox1.Text, out itemCount1);
            if (ItemAmountTextBox2.Text != string.Empty)
                int.TryParse(ItemAmountTextBox2.Text, out itemCount2);
            if (ItemAmountTextBox3.Text != string.Empty)
                int.TryParse(ItemAmountTextBox3.Text, out itemCount3);
            if (ItemAmountTextBox4.Text != string.Empty)
                int.TryParse(ItemAmountTextBox4.Text, out itemCount4);
            if (ItemAmountTextBox5.Text != string.Empty)
                int.TryParse(ItemAmountTextBox5.Text, out itemCount5);
            if (ItemAmountTextBox6.Text != string.Empty)
                int.TryParse(ItemAmountTextBox6.Text, out itemCount6);

            // Validate items
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0:
                        if (itemID1 > 0)
                            if (IsValidItem(itemID1) && IsAmountPossible(itemID1, itemCount1))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                    case 1:
                        if (itemID2 > 0)
                            if (IsValidItem(itemID2) && IsAmountPossible(itemID2, itemCount2))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                    case 2:
                        if (itemID3 > 0)
                            if (IsValidItem(itemID3) && IsAmountPossible(itemID3, itemCount3))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                    case 3:
                        if (itemID4 > 0)
                            if (IsValidItem(itemID4) && IsAmountPossible(itemID4, itemCount4))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                    case 4:
                        if (itemID5 > 0)
                            if (IsValidItem(itemID5) && IsAmountPossible(itemID1, itemCount5))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                    case 5:
                        if (itemID6 > 0)
                            if (IsValidItem(itemID6) && IsAmountPossible(itemID6, itemCount6))
                                amountOfItems++;
                            else
                                invalidItems++;
                        break;
                }
            }

            // Send mail to the player, including items if there are any
            if (amountOfItems != 0 && invalidItems == 0)
            {
                switch (amountOfItems)
                {
                    case 1:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItem: {itemID1}:{itemCount1}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 2:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1} {itemID2}:{itemCount2}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}:{itemCount1}, {itemID2}:{itemCount2}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 3:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1} {itemID2}:{itemCount2} {itemID3}:{itemCount3}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}:{itemCount1}, {itemID2}:{itemCount2}, {itemID3}:{itemCount3}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 4:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1} {itemID2}:{itemCount2} {itemID3}:{itemCount3} {itemID4}:{itemCount4}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}:{itemCount1}, {itemID2}:{itemCount2}, {itemID3}:{itemCount3}, {itemID4}:{itemCount4}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 5:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1} {itemID2}:{itemCount2} {itemID3}:{itemCount3} {itemID4}:{itemCount4} {itemID5}:{itemCount5}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}:{itemCount1}, {itemID2}:{itemCount2}, {itemID3}:{itemCount3}, {itemID4}:{itemCount4}, {itemID5}:{itemCount5}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case 6:
                        if (_tcSoapService.ExecuteSOAPCommand($"send items {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\" {itemID1}:{itemCount1} {itemID2}:{itemCount2} {itemID3}:{itemCount3} {itemID4}:{itemCount4} {itemID5}:{itemCount5} {itemID6}:{itemCount6}"))
                            MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}\r\nItems: {itemID1}:{itemCount1}, {itemID2}:{itemCount2}, {itemID3}:{itemCount3}, {itemID4}:{itemCount4}, {itemID5}:{itemCount5}, {itemID6}:{itemCount6}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            else if (invalidItems > 0)
            {
                MessageBox.Show($"One or more of the items listed are invalid.", "Mail Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                if (_tcSoapService.ExecuteSOAPCommand($"send mail {playerName} \"{SubjectTextBox.Text}\" \"{MessageTextBox.Text}\""))
                    MessageBox.Show($"Mail has been sent to {playerName}.\r\nSubject: {SubjectTextBox.Text}\r\nMessage: {MessageTextBox.Text}", "Mail Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InvalidPlayer()
        {
            isValidPlayer = false;

            MessageBox.Show("Could not find a player associated with that name, the Account Tools window will now close.", "No Player Found", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private PlayerFaction GetPlayerFaction(int playerRace)
        {
            if (raceToFaction.TryGetValue(playerRace, out PlayerFaction faction))
                return faction;

            return PlayerFaction.Invalid;
        }

        private static bool IsTextNumeric(string text)
        {
            Regex reg = new Regex("[^0-9]");
            return reg.IsMatch(text);
        }

        private void NumericInputOnly(object sender, System.Windows.Input.TextCompositionEventArgs e) { e.Handled = IsTextNumeric(e.Text); }

        // Make sure the item id exists in the database
        private bool IsValidItem(int itemID)
        {
            bool hasRows = false;

            using (var connection = _dbConnectorWorld.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT * FROM item_template WHERE entry = @itemID LIMIT 1", connection);
                    command.Parameters.AddWithValue("@itemID", itemID);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                        hasRows = reader.HasRows;

                    _dbConnectorWorld.CloseConnection(reader);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return hasRows;
        }

        // Verify item count
        private bool IsAmountPossible(int itemID, int itemCount)
        {
            int maxCount = 0;
            int maxStackSize = 0;

            using (var connection = _dbConnectorWorld.GetConnection())
            {
                try
                {
                    connection.Open();
                    var command = new MySqlCommand("SELECT maxcount, stackable FROM item_template WHERE entry = @itemID LIMIT 1", connection);
                    command.Parameters.AddWithValue("@itemID", itemID);
                    var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        maxCount = reader.GetInt32(0);
                        maxStackSize = reader.GetInt32(1);
                    }

                    _dbConnectorWorld.CloseConnection(reader);

                    if (maxCount == 0 && maxStackSize >= itemCount)
                        return true;
                    else if (maxCount <= itemCount && maxStackSize >= itemCount)
                        return true;
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return false;
        }
    }
}
