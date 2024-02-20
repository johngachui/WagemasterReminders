// DatabaseConflictResolver.cs
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace YourProjectName.Services
{
    public class DatabaseConflictResolver
    {
        private readonly ILogger<DatabaseConflictResolver> _logger;
        private readonly Func<string, string> _getConnectionString;

        public DatabaseConflictResolver(ILogger<DatabaseConflictResolver> logger, Func<string, string> getConnectionString)
        {
            _logger = logger;
            _getConnectionString = getConnectionString;
        }

        public void ResolveAllConflicts(IEnumerable<string> databasePaths)
        {
            var potentialConflicts = ScanDatabasesForConflicts(databasePaths);
            foreach (var conflict in potentialConflicts)
            {
                var newDivisionKey = ComputeUniqueDivisionKey(conflict.CompanyKey, conflict.DatabasePath);
                UpdateDivisionKeyInDatabase(conflict.DatabasePath, newDivisionKey);
                _logger.LogInformation($"Updated DivisionKey for CompanyKey {conflict.CompanyKey} in database {conflict.DatabasePath} to resolve conflict.");
            }
        }

        private IEnumerable<Conflict> ScanDatabasesForConflicts(IEnumerable<string> databasePaths)
        {
            var companyDivisionKeyPairs = new Dictionary<(string CompanyKey, string DivisionKey), List<string>>();

            foreach (var databasePath in databasePaths)
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath); // Assuming GetConnectionString is a method that returns the connection string for a database path
                using (var connection = new OleDbConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        var command = new OleDbCommand("SELECT API_COMPANYKEY, API_DIVISIONKEY FROM COMPANY_FILES", connection);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var companyKey = reader["API_COMPANYKEY"].ToString();
                                var divisionKey = reader["API_DIVISIONKEY"].ToString();
                                var keyPair = (companyKey, divisionKey);

                                if (!companyDivisionKeyPairs.ContainsKey(keyPair))
                                {
                                    companyDivisionKeyPairs[keyPair] = new List<string>();
                                }
                                companyDivisionKeyPairs[keyPair].Add(databasePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error scanning database at {databasePath}: {ex.Message}");
                    }
                }
            }

            // Identify duplicates
            var conflicts = companyDivisionKeyPairs
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => new Conflict
                {
                    CompanyKey = kvp.Key.CompanyKey,
                    OriginalDivisionKey = kvp.Key.DivisionKey,
                    DatabasePath = string.Join(", ", kvp.Value) // This assumes you want to log all conflicting paths. Adjust as necessary.
                });

            return conflicts;
        }


        private string ComputeUniqueDivisionKey(string companyKey, string databasePath)
        {
            // Concatenate the input values
            string input = companyKey + databasePath;

            // Generate the salt
            string salt = GenerateSalt(input);

            // Concatenate input with the generated salt
            string inputWithSalt = input + salt;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the hash of the input with salt
                byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(inputWithSalt));

                // Convert byte array to a hexadecimal string
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

        private static string GenerateSalt(string input)
        {
            StringBuilder saltBuilder = new StringBuilder();
            for (int i = 2; i < input.Length; i += 3)
            {
                // Get ASCII value of each 3rd character
                int asciiValue = (int)input[i];

                // Concatenate absolute value of the length of input to ASCII value
                saltBuilder.Insert(0, Math.Abs(input.Length) + asciiValue);
            }

            return saltBuilder.ToString();
        }
        private void UpdateDivisionKeyInDatabase(string databasePath, string newDivisionKey)
        {
            string connectionString = _getConnectionString(databasePath); // Use the provided delegate to get the connection string.

            using (var connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // This SQL statement updates the division key for all records in the COMPANY_FILES table.
                    // Since all records have the same API_COMPANYKEY value, we don't need to filter by company key.
                    var commandText = "UPDATE COMPANY_FILES SET API_DIVISIONKEY = ?";
                    using (var command = new OleDbCommand(commandText, connection))
                    {
                        // Use parameters to prevent SQL injection.
                        command.Parameters.Add(new OleDbParameter("API_DIVISIONKEY", newDivisionKey));

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            _logger.LogInformation($"Successfully updated DivisionKey in database {databasePath}.");
                        }
                        else
                        {
                            // This path might indicate that there were no records to update, which could be unusual.
                            _logger.LogWarning($"No rows updated. The COMPANY_FILES table might be empty in database {databasePath}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating DivisionKey in database {databasePath}: {ex.Message}");
                }
            }
        }

    }

    // Placeholder for a conflict structure
    public struct Conflict
    {
        public string CompanyKey;
        public string OriginalDivisionKey;
        public string DatabasePath;
    }
}
