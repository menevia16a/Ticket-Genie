using MySql.Data.MySqlClient;

public class DBConnector
{
    private MySqlConnection connection;

    public DBConnector(string connectionString)
    {
        connection = new MySqlConnection(connectionString);
    }

    public MySqlConnection GetConnection()
    {
        return connection;
    }

    public void CloseConnection()
    {
        connection.Close();
    }
}
