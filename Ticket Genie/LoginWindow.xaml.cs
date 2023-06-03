using MySql.Data.MySqlClient;
using System.Windows;

namespace Ticket_Genie
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly DBConnector _dbConnector;
        private static bool loginSuccess = false;

        public LoginWindow()
        {
            InitializeComponent();
            _dbConnector = new DBConnector(Properties.Settings.Default.AuthDB);
        }

        public bool GetLoginSuccess()
        {
            return loginSuccess;
        }

        private void OnClickLogin(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.ToUpper();

            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, username FROM account WHERE username = @username", connection);
                command.Parameters.AddWithValue("@username", username);
                var reader = command.ExecuteReader();

                int accountID = 0;
                string usernameFromDB = "";

                if (reader.Read())
                {
                    accountID = reader.GetInt32(0);
                    usernameFromDB = reader.GetString(1).ToUpper();
                    reader.Close();
                }

                var command2 = new MySqlCommand("SELECT SecurityLevel FROM account_access WHERE AccountID = @id", connection);
                command2.Parameters.AddWithValue("@id", accountID);
                var reader2 = command2.ExecuteReader();
                if (reader2.HasRows && reader2.Read())
                {
                    // Store security level
                    int securityLevel = reader2.GetInt32(0);

                    if (usernameFromDB == username && securityLevel > 1) // Verify username and security level
                    {
                        Properties.Settings.Default.AccountID = accountID;
                        loginSuccess = true;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid username or insufficient security level", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    loginSuccess = false;
                    DialogResult = false;
                    Close();
                }    
            }
        }
    }
}
