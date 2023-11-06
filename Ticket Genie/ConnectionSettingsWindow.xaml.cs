using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for ConnectionSettingsWindow.xaml
    /// </summary>
    public partial class ConnectionSettingsWindow : Window
    {
        public ConnectionSettingsWindow()
        {
            InitializeComponent();
            Loaded += ConnectionSettingsWindow_Loaded;
        }

        private void ConnectionSettingsWindow_Loaded(object sender, RoutedEventArgs e) { LoadConnectionSettings(); }

        private void ConnectionType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Call the UpdateVisibility function to update the panel visibility
            if (IsLoaded)
            {
                int selectedIndex = ConnectionTypeComboBox.SelectedIndex;
                SaveSettings(selectedIndex);
                UpdateVisibility(selectedIndex);
            }
        }

        private void LoadConnectionSettings()
        {
            // Load SQL connection settings
            SQLHostnameTextBox.Text = Properties.Settings.Default.SQLHost;
            SQLPortTextBox.Text = Properties.Settings.Default.SQLPort.ToString();
            CharacterDatabaseTextBox.Text = Properties.Settings.Default.CharacterDB;
            AuthDatabaseTextBox.Text = Properties.Settings.Default.AuthDB;
            WorldDatabaseTextBox.Text = Properties.Settings.Default.WorldDB;
            SQLUsernameTextBox.Text = Properties.Settings.Default.SQLUsername;
            SQLPasswordTextBox.Text = Properties.Settings.Default.SQLPassword;
            // Load SOAP connection settings
            SOAPHostnameTextBox.Text = Properties.Settings.Default.SOAPHost;
            SOAPPortTextBox.Text = Properties.Settings.Default.SOAPPort.ToString();
            SOAPUsernameTextBox.Text = Properties.Settings.Default.SOAPUsername;
            SOAPPasswordTextBox.Text = Properties.Settings.Default.SOAPPassword;
        }

        private void UpdateVisibility(int type) // Type 0 = SQL, Type 1 = SOAP
        {
            // Update panel visibility based on selected database type
            switch (type)
            {
                case 0:
                    // Set the SQL panels to visible
                    SQLHostnamePanel.Visibility = Visibility.Visible;
                    SQLPortPanel.Visibility = Visibility.Visible;
                    CharacterDatabasePanel.Visibility = Visibility.Visible;
                    AuthDatabasePanel.Visibility = Visibility.Visible;
                    WorldDatabasePanel.Visibility = Visibility.Visible;
                    SQLUsernamePanel.Visibility = Visibility.Visible;
                    SQLPasswordPanel.Visibility = Visibility.Visible;
                    // Set the SOAP panels to hidden
                    SOAPHostnamePanel.Visibility = Visibility.Hidden;
                    SOAPPortPanel.Visibility = Visibility.Hidden;
                    SOAPUsernamePanel.Visibility = Visibility.Hidden;
                    SOAPPasswordPanel.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    // Set the SQL panels to hidden
                    SQLHostnamePanel.Visibility = Visibility.Hidden;
                    SQLPortPanel.Visibility = Visibility.Hidden;
                    CharacterDatabasePanel.Visibility = Visibility.Hidden;
                    AuthDatabasePanel.Visibility = Visibility.Hidden;
                    WorldDatabasePanel.Visibility = Visibility.Hidden;
                    SQLUsernamePanel.Visibility = Visibility.Hidden;
                    SQLPasswordPanel.Visibility = Visibility.Hidden;
                    // Set the SOAP panels to visible
                    SOAPHostnamePanel.Visibility = Visibility.Visible;
                    SOAPPortPanel.Visibility = Visibility.Visible;
                    SOAPUsernamePanel.Visibility = Visibility.Visible;
                    SOAPPasswordPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SaveSettings(int type) // Type 0 = SQL, Type 1 = SOAP, Type 2 = Both
        {
            int sqlPort = int.Parse(SQLPortTextBox.Text);
            int soapPort = int.Parse(SOAPPortTextBox.Text);

            SQLConnectionSettingsInfo sqlSettings = new SQLConnectionSettingsInfo
            {
                hostname = SQLHostnameTextBox.Text,
                port = sqlPort,
                characterDatabase = CharacterDatabaseTextBox.Text,
                authDatabase = AuthDatabaseTextBox.Text,
                worldDatabase = WorldDatabaseTextBox.Text,
                username = SQLUsernameTextBox.Text,
                password = SQLPasswordTextBox.Text
            };
            SOAPConnectionSettingsInfo soapSettings = new SOAPConnectionSettingsInfo
            {
                hostname = SOAPHostnameTextBox.Text,
                port = soapPort,
                username = SOAPUsernameTextBox.Text,
                password = SOAPPasswordTextBox.Text
            };

            switch (type)
            {
                case 0:
                    JsonTools.SerializeSQLConnectionSettingsJSON<SQLConnectionSettingsInfo>(sqlSettings);
                    break;
                case 1:
                    JsonTools.SerializeSOAPConnectionSettingsJSON<SOAPConnectionSettingsInfo>(soapSettings);
                    break;
                case 2:
                    JsonTools.SerializeSQLConnectionSettingsJSON<SQLConnectionSettingsInfo>(sqlSettings);
                    JsonTools.SerializeSOAPConnectionSettingsJSON<SOAPConnectionSettingsInfo>(soapSettings);
                    break;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) { Close(); }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveSettings(2);
            JsonTools.UpdateConnectionSettings();
            Close();
        }
    }
}
