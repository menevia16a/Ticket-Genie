using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Ticket_Genie
{
    public class TicketManager
    {
        private readonly DBConnector _dbConnector;

        public TicketManager() { _dbConnector = new DBConnector("Server=***REMOVED***;Database=characters;Uid=***REMOVED***;Pwd=***REMOVED***;Port=***REMOVED***;"); }

        public void UpdateTickets(Ticket ticket)
        {
            // Todo: Implement soap call to update tickets
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
