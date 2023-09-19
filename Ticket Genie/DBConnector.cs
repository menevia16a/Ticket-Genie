using MySql.Data.MySqlClient;
using System.Windows;

public class DBConnector
{
    private MySqlConnection connection;

    public DBConnector(string connectionString)
    {
        try
        {
            connection = new MySqlConnection(connectionString);
        }
        catch (MySqlException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public MySqlConnection GetConnection() { return connection; }

    public void CloseConnection(MySqlDataReader reader = null)
    {
        if (reader != null && !reader.IsClosed)
            reader.Close();

        connection.Close();
    }
}
