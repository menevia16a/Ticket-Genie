using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ticket_Genie
{
    public class TicketManager
    {
        private readonly TCSOAPService _tcSoapService;
        private readonly DBConnector _dbConnector;

        public TicketManager() { _dbConnector = new DBConnector(Properties.Settings.Default.CharactersDB); }

        public void UpdateTickets()
        {
            // Reloads the GM tickets in-game
            TCSOAPService _tcSoapService = new TCSOAPService();
            _tcSoapService.Call("reload gm_ticket");
        }

        public void AppendResponse(int ticketID, string response)
        {
            // Append the response to the ticket
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("UPDATE gm_ticket SET response = @response, completed = 1, closedBy = @accountID, resolvedBy = @accountID WHERE id = @ticketID", connection);
                command.Parameters.AddWithValue("@response", response);
                command.Parameters.AddWithValue("@accountID", Properties.Settings.Default.AccountID);
                command.Parameters.AddWithValue("@ticketID", ticketID);
                command.ExecuteNonQuery();
            }
        }

        public Ticket GetTicket(int id)
        {
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, name, description, response, completed, closedBy FROM gm_ticket WHERE id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // Check if the ticket is already closed, and don't list
                    if (reader.GetInt32(5) == 0)
                    {
                        var command2 = new MySqlCommand("UPDATE gm_ticket SET viewed = 1 WHERE id = @id", connection);
                        command2.Parameters.AddWithValue("@id", id);
                        command2.ExecuteNonQuery();

                        return new Ticket
                        {
                            id = reader.GetInt32(0),
                            name = reader.GetString(1),
                            description = reader.GetString(2),
                            response = reader.GetString(3),
                            completed = reader.GetInt32(4),
                            closedBy = reader.GetInt32(5)
                        };
                    }
                }
                return null;
            }
        }

        public IEnumerable<Ticket> GetAllTickets()
        {
            using (var connection = _dbConnector.GetConnection())
            {
                connection.Open();
                var command = new MySqlCommand("SELECT id, name, closedBy FROM gm_ticket", connection);
                var reader = command.ExecuteReader();

                var tickets = new List<Ticket>();
                while (reader.Read())
                {
                    // Check if the ticket is already closed, and don't list
                    if (reader.GetInt32(2) == 0)
                    {
                        tickets.Add(new Ticket
                        {
                            id = reader.GetInt32(0),
                            name = reader.GetString(1),
                            closedBy = reader.GetInt32(2)
                        });
                    }
                }
                return tickets;
            }
        }
    }
}
