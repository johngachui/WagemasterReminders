//DatabaseService.cs
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Text;
using System.Windows.Forms.Design;
using YourProjectName.Models;

namespace YourProjectName.Services
{

    public interface IDatabaseService
    {
        List<string> ReadIniFile();
        List<Event> GetEvents(string username, string password);
        bool GetUser(string username, string password, string databasePath);

        bool UpdateEvent(int id, bool dismissed, string username, string databasePath, string password, int? ref_id, string reminderType);

        List<LeaveBals> GetLeaveBals(string num, string companykey, string divisionkey, string employeekey);
        bool AuthEmp(string num, string databasePath, string called_from);
        List<LeaveDays> GetLeaveDays(string num, string companykey, string divisionkey, string employeekey);

        List<HR_Master> GetHRMaster(string num,  string companykey, string divisionkey, string employeekey);
        bool CreateLeaveApplication(string num, DateTime startdate, DateTime stopdate, string? leavetype, string companykey, string divisionkey, string employeekey);
        
    }

    public interface ITaskService
    {
        void CreateRepeatingTasks(string databasePath, string connectionstr);
    }
    public interface IReminderService
    {
        //bool UpdateReminders(string username, string password);
        List<Event> UpdateReminders(string username, string password);
    }
    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;

        // Define the dictionary to map (CompanyKey, DivisionKey) to a list of database paths

        private Dictionary<(string CompanyKey, string DivisionKey), string> _companyDivisionDatabaseMappings = new Dictionary<(string CompanyKey, string DivisionKey), string>();

        private readonly ILoggerFactory _loggerFactory;

        public DatabaseService(ILogger<DatabaseService> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            InitializeDatabaseService();
        }

        private void InitializeDatabaseService()
        {
            var databasePaths = ReadIniFile();
            var conflictResolverLogger = _loggerFactory.CreateLogger<DatabaseConflictResolver>();
            var resolver = new DatabaseConflictResolver(conflictResolverLogger, DatabaseService.GetConnectionString);
            resolver.ResolveAllConflicts(databasePaths);
            LoadCompanyDivisionMappings();
        }


        public bool GetUser(string username, string password, string databasePath)

        {
            try
            {
                _logger.LogInformation($"databasePath = {databasePath}"); //SHOW ROWCOUNT 
                string connectionString = GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    // Check if the PASSWORDS table is empty
                    string countQuery = "SELECT COUNT(*) FROM PASSWORDS";
                    using (OleDbCommand countCommand = new OleDbCommand(countQuery, connection))
                    {
                        int rowCount = (int)countCommand.ExecuteScalar();
                        _logger.LogInformation($"rowCount = {rowCount}"); //SHOW ROWCOUNT                                        
                        if (rowCount == 0)
                        {
                            _logger.LogInformation($"return1 = true"); //SHOW ROWCOUNT 
                            return true;  // Return true if the PASSWORDS table is empty
                        }
                    }

                    // Query to find a user with the provided username and SHOW_REMINDERS permission
                    string userQuery = "SELECT [SHOW_REMINDERS] FROM PASSWORDS WHERE [USER NAME]=@UserName AND [PASSWORD]=@Password";
                    using (OleDbCommand userCommand = new OleDbCommand(userQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@UserName", username);
                        userCommand.Parameters.AddWithValue("@Password", password);

                        object result = userCommand.ExecuteScalar();
                        if (result != null)
                        {
                            return Convert.ToBoolean(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"GetUse error: {ex}");
            }
            return false;  // Return false if user was not found or if user does not have SHOW_REMINDERS permission

        }

        public bool AuthEmp(string num, string databasePath, string called_from)

        {
            try
            {
                _logger.LogInformation($"databasePath = {databasePath}"); //SHOW ROWCOUNT 
                string connectionString = GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    // Check if the leave API function is licensed
                    string countQuery = "SELECT COMPANY_FILES.API_COMPANYKEY FROM COMPANY_FILES WHERE (((COMPANY_FILES.LAST)=True) AND ((COMPANY_FILES.API_COMPANYKEY)>''));";
                    using (OleDbCommand countCommand = new OleDbCommand(countQuery, connection))
                    {
                        string? api_companykey = (string?)countCommand.ExecuteScalar();
                        _logger.LogInformation($"rowCountELM = {api_companykey}"); //SHOW ROWCOUNT                                        
                        if (!string.IsNullOrEmpty(api_companykey))
                        {
                            _logger.LogInformation($"return_elm1 = true"); //SHOW ROWCOUNT 
                            return true;  // Return true if the PASSWORDS table is empty
                        }
                    }

                    if (!num.StartsWith("*") && called_from != "leaveapplication")
                    {
                        // Query to find a user with the provided username and SHOW_REMINDERS permission
                        string userQuery = "SELECT [NUM] FROM HR_MASTER WHERE [NUM]=@num";
                        using (OleDbCommand userCommand = new OleDbCommand(userQuery, connection))
                        {
                            userCommand.Parameters.AddWithValue("@num", num);

                            object result = userCommand.ExecuteScalar();
                            if (result != null)
                            {
                                return Convert.ToBoolean(result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"AuthEmp error: {ex.Message}");
            }
            return false;  // Return false if user was not found or if user does not have SHOW_REMINDERS permission

        }

        public bool UpdateEvent(int id, bool dismissed, string databasePath, string username, string password, int? ref_id, string reminderType)
        {
            // Check if the user has permission to perform this operation
            if (!GetUser(username, password, databasePath))
            {
                _logger.LogInformation($"User {username} attempted to update event with id {id} in database {databasePath}, but does not have necessary permissions");
                return false;
            }

            // Proceed with the update operation if permission granted...
            string connectionString = GetConnectionString(databasePath);
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = $"UPDATE API_REMINDERS SET DISMISS = ?, USER_DISMISSED = ?, DATE_DISMISSED = ? WHERE ID = ?";

                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        DateTime currentDate = DateTime.Now.Date;

                        // Add parameters to prevent SQL Injection
                        command.Parameters.Add(new OleDbParameter("dismissed", dismissed));
                        command.Parameters.Add(new OleDbParameter("user_dismissed", username));
                        command.Parameters.Add(new OleDbParameter("date_dismissed", currentDate));
                        command.Parameters.Add(new OleDbParameter("id", id));

                        // Execute the query
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            _logger.LogInformation($"No rows were updated. Event with id {id} might not exist in database {databasePath}");
                            return false;
                        }
                        if (reminderType == "TASK")
                        {
                            try
                            {
                                string query2 = $"UPDATE TASKS SET COMPLETED = ?, USER_COMPLETED = ?, DATE_COMPLETED = ? WHERE ID = ? and MARK_COMPLETED_ON_DISMISSED = TRUE";
                                using (OleDbCommand command2 = new OleDbCommand(query2, connection))
                                {
                                    command2.Parameters.Add(new OleDbParameter("dismissed2", dismissed));
                                    command2.Parameters.Add(new OleDbParameter("user_completed", username));
                                    command2.Parameters.Add(new OleDbParameter("date_completed", currentDate));
                                    command2.Parameters.Add(new OleDbParameter("Refid", ref_id));

                                    // Execute the query
                                    int rowsAffected2 = command2.ExecuteNonQuery();
                                    if (rowsAffected2 == 0)
                                    {
                                        _logger.LogInformation($"No rows were updated. Task with id {ref_id} might not exist in database {databasePath}");
                                        return true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Error updating Task with ref_id {ref_id} and currentDate {currentDate} and username {username} and dismissed {dismissed}. Error: {ex.Message}");
                                return false;
                            }
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error updating event with id {id} in database {databasePath}. Error: {ex.Message}");
                    return false;
                }

            }
        }

        
        public List<Event> GetEvents(string username, string password)
        {
            // Read the INI file to get the database paths
            List<string> databasePaths = ReadIniFile();
            List<Event> events = new List<Event>();

            // Loop through each database path and fetch event data
            foreach (string path in databasePaths)
            {
                if (GetUser(username, password, path))
                {
                    _logger.LogInformation($"path1 = *******************"); //SHOW ROWCOUNT
                    events.AddRange(GetEventsFromDatabase(path,username));
                    _logger.LogInformation($"path2 = *******************"); //SHOW ROWCOUNT 
                }
            }
            return events;
        }

        public List<LeaveBals> GetLeaveBals(string num, string companykey, string divisionkey, string employeekey)
        {
            // Read the INI file to get the database paths
            //List<string> databasePaths = ReadIniFile();
            string path = ResolveDatabasePath(companykey, divisionkey);
            List<LeaveBals> leavebals = new List<LeaveBals>();

            
            if (AuthEmp(num, path,"leavebals"))
            {
                _logger.LogInformation($"path1 = *******************"); //SHOW ROWCOUNT
                leavebals.AddRange(GetLeaveBalsFromDatabase(path, num, companykey, divisionkey, employeekey));
                _logger.LogInformation($"path2 = *******************"); //SHOW ROWCOUNT 
            }
            
            return leavebals;
        }

        public List<HR_Master> GetHRMaster(string num, string companykey, string divisionkey, string employeekey)
        {
            // Read the INI file to get the database paths
            string path = ResolveDatabasePath(companykey, divisionkey);
            List<HR_Master> hrmasteremps = new List<HR_Master>();

            if (AuthEmp(num, path, "hrmaster"))
            {
                _logger.LogInformation($"path4 = *******************"); //SHOW ROWCOUNT
                hrmasteremps.AddRange(GetHRMasterFromDatabase(num, path, companykey, divisionkey, employeekey));
                _logger.LogInformation($"path4 = *******************"); //SHOW ROWCOUNT 
            }
            
            return hrmasteremps;
        }
        public List<LeaveDays> GetLeaveDays(string num, string companykey, string divisionkey, string employeekey)
        {
            // Read the INI file to get the database paths
            string path = ResolveDatabasePath(companykey, divisionkey);
            List<LeaveDays> leavedays = new List<LeaveDays>();
            
            if (AuthEmp(num, path,"leavedays"))
            {
                _logger.LogInformation($"path1 = *******************"); //SHOW ROWCOUNT
                leavedays.AddRange(GetLeaveDaysFromDatabase(path, num, employeekey));
                _logger.LogInformation($"path2 = *******************"); //SHOW ROWCOUNT 
            }
            
            return leavedays;
        }
        public List<string> ReadIniFile()
        {
            // Construct the INI file path
            string iniPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WagemasterPayroll",
                "WagemasterPayroll.ini"
            );

            // Read the contents of the INI file
            string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
            List<string> databasePaths = new List<string>();

            // Loop through each line and extract the database paths
            foreach (string line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string path = line.Substring(1, line.Length - 2);
                    path = Path.Combine(path, "Wagemaster_data.mdb");
                    // Log the path
                    _logger.LogInformation($"Log1: {path}");
                    databasePaths.Add(path);
                }
            }

            return databasePaths;
        }

        public static string GetConnectionString(string databasePath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password=!wage*master?;";
        }

        public void LoadCompanyDivisionMappings()
        {
            
            // Read database paths from the INI file
            var databasePaths = ReadIniFile();

            foreach (var databasePath in databasePaths)
            {
                // Connect to the database
                string connectionString = GetConnectionString(databasePath);
                using (var connection = new OleDbConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // Query the COMPANY_FILES table for API_COMPANYKEY and API_DIVISIONKEY
                        var command = new OleDbCommand("SELECT TOP 1 API_COMPANYKEY, API_DIVISIONKEY FROM COMPANY_FILES WHERE API_COMPANYKEY IS NOT NULL AND API_DIVISIONKEY IS NOT NULL", connection);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var companyKey = reader["API_COMPANYKEY"].ToString();
                                var divisionKey = reader["API_DIVISIONKEY"].ToString();

                                // Add to the dictionary
                                if (!string.IsNullOrEmpty(companyKey) && !string.IsNullOrEmpty(divisionKey))
                                {
                                    _companyDivisionDatabaseMappings.Add((companyKey, divisionKey), databasePath);

                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle any exceptions, such as logging errors or skipping to the next database path
                        Console.WriteLine($"Error loading mappings for database at path {databasePath}: {ex.Message}");
                    }
                }
            }
        }


        public string ResolveDatabasePath(string companyKey, string divisionKey)
        {
            if (_companyDivisionDatabaseMappings.TryGetValue((companyKey, divisionKey), out var databasePath))
            {
                return databasePath;
            }
            else
            {
                throw new KeyNotFoundException($"No database path found for company key {companyKey} and division key {divisionKey}.");
            }
        }




        private List<Event> GetEventsFromDatabase(string databasePath, string username)
        {
            List<Event> events = new List<Event>();
            try
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    
                    connection.Open();

                    string query = "SELECT ID, REF_NUM, REF_NAME, REMINDER_TYPE, REMINDER_MSG, REF_DATE, REMINDER_DATE, COMPANY, DISMISS, REF_ID FROM API_REMINDERS WHERE SHOW = TRUE AND ((REMINDER_TYPE <> 'TASK') OR (REMINDER_TYPE = 'TASK' AND (TASK_USER = ? OR TASK_USER IS NULL)))";
                    
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        _logger.LogInformation($"X1 username = {username}");
                        command.Parameters.AddWithValue("?", username);
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                _logger.LogInformation($"X1");
                                while (reader.Read())
                                {

                                    Event e = new Event
                                    {
                                        ID = (int)reader["ID"],
                                        RefNum = reader["REF_NUM"].ToString(),
                                        RefName = reader["REF_NAME"].ToString(),
                                        ReminderType = reader["REMINDER_TYPE"].ToString(),
                                        ReminderMsg = reader["REMINDER_MSG"].ToString(),
                                        RefDate = DateTime.Parse(reader["REF_DATE"].ToString()),
                                        ReminderDate = DateTime.Parse(reader["REMINDER_DATE"].ToString()),
                                        Company = reader["COMPANY"].ToString(),
                                        DatabasePath = databasePath,
                                        Dismissed = (bool)reader["DISMISS"],
                                        Ref_ID = reader["REF_ID"] == DBNull.Value ? (int?)null : (int)reader["REF_ID"],
                                       
                                    };
                                    events.Add(e);
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Log2a: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log2b: {ex.Message}");
            }
            return events;
        }

        private List<LeaveBals> GetLeaveBalsFromDatabase(string databasePath, string num, string companykey, string divisionkey, string employeekey)
        {
            List<LeaveBals> leavebals = new List<LeaveBals>();
            try
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {

                    connection.Open();
                    string query = "";
                    int query_type = 0;
                    
                    if (employeekey.StartsWith ("*"))
                    {
                        if (employeekey.Length == 1)
                        {
                            query = "SELECT MASTER.NUM, MASTER.LEAVE_BFWD, MASTER.LEAVE_CFWD, MASTER.THIS_MONTH, MASTER.TAKEN, MASTER.SOLD, MASTER.LEAVE_ABSENCE, MASTER.LEAVE_ADJUST, MASTER.MATERNITY_BFWD, MASTER.MATERNITY_CFWD, MASTER.PATERNITY_BFWD, MASTER.PATERNITY_CFWD, MASTER.FULL_DAYS_CFWD, MASTER.HALF_DAYS_CFWD, HR_MASTER.API_EMPLOYEEKEY, COMPANY_FILES.LAST FROM HR_MASTER INNER JOIN (COMPANY_FILES INNER JOIN MASTER ON (MASTER.MONTHZ = COMPANY_FILES.MONTHX) AND (COMPANY_FILES.YEARX = MASTER.YEARZ)) ON HR_MASTER.NUM = MASTER.NUM WHERE (((COMPANY_FILES.LAST)=True))";

                            query_type = 1;
                        }
                    }
                    else
                    {
                        query = "SELECT MASTER.NUM, MASTER.LEAVE_BFWD, MASTER.LEAVE_CFWD, MASTER.THIS_MONTH, MASTER.TAKEN, MASTER.SOLD, MASTER.LEAVE_ABSENCE, MASTER.LEAVE_ADJUST, MASTER.MATERNITY_BFWD, MASTER.MATERNITY_CFWD, MASTER.PATERNITY_BFWD, MASTER.PATERNITY_CFWD, MASTER.FULL_DAYS_CFWD, MASTER.HALF_DAYS_CFWD, COMPANY_FILES.LAST, HR_MASTER.API_EMPLOYEEKEY FROM (HR_MASTER INNER JOIN MASTER ON HR_MASTER.NUM = MASTER.NUM) INNER JOIN COMPANY_FILES ON (COMPANY_FILES.MONTHX = MASTER.MONTHZ) AND (MASTER.YEARZ = COMPANY_FILES.YEARX) WHERE (((COMPANY_FILES.LAST)=True) AND ((HR_MASTER.API_EMPLOYEEKEY)=?))";

                        query_type = 2;
                    }
                    
                    if (query_type == 0) { return leavebals; } //incorrect parameter

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        _logger.LogInformation($"Y1A employeekey = {employeekey} and query_type = {query_type}");
                        switch (query_type)
                        {
                            case 2:
                                command.Parameters.AddWithValue("?", employeekey);
                                break;
                            default:
                                break;
                        }
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                _logger.LogInformation($"Y1");
                                while (reader.Read())
                                {

                                    LeaveBals e = new LeaveBals
                                    {
                                        Num = reader["NUM"].ToString(),
                                        Annual_Bfwd = (decimal)reader["LEAVE_BFWD"],
                                        Annual_Cfwd = (decimal)reader["LEAVE_CFWD"],
                                        Maternity_Bfwd = (decimal)reader["MATERNITY_BFWD"],
                                        Maternity_Cfwd = (decimal)reader["MATERNITY_CFWD"],
                                        Paternity_Bfwd = (decimal)reader["PATERNITY_BFWD"],
                                        Paternity_Cfwd = (decimal)reader["PATERNITY_CFWD"],
                                        Full_Sick_Bal = (decimal)reader["FULL_DAYS_CFWD"],
                                        Half_Sick_Bal = (decimal)reader["HALF_DAYS_CFWD"],
                                        Earned = (decimal)reader["THIS_MONTH"],
                                        Taken = (decimal)reader["TAKEN"],
                                        Sold = (decimal)reader["SOLD"],
                                        Adjustment = (decimal)reader["LEAVE_ADJUST"],
                                        Absence = (decimal)reader["LEAVE_ABSENCE"],
                                        CompanyKey = companykey,
                                        DivisionKey = divisionkey,
                                        EmployeeKey = reader["API_EMPLOYEEKEY"].ToString(),
                                    };
                                    leavebals.Add(e);
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Log3a: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log3C: {ex.Message}");
            }
            return leavebals;
        }

        private List<LeaveDays> GetLeaveDaysFromDatabase(string databasePath, string num,string employeekey)
        {
            List<LeaveDays> leavedays = new List<LeaveDays>();
            try
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {

                    connection.Open();
                    string query = "";
                    int query_type = 0;
                    
                    if (employeekey.StartsWith("*"))
                    {
                        if (employeekey.Length == 1)
                        {
                            query = "SELECT [LEAVE DAYS TAKEN].NUM, [LEAVE DAYS TAKEN].START, [LEAVE DAYS TAKEN].STOP, [LEAVE DAYS TAKEN].TYPE, [LEAVE DAYS TAKEN].DAYS, [LEAVE DAYS TAKEN].APPROVED, [LEAVE DAYS TAKEN].NOT_APPROVED, [LEAVE DAYS TAKEN].NOTIFIED, [LEAVE DAYS TAKEN].TAKEN, [LEAVE DAYS TAKEN].RECALC_NEEDED, HR_MASTER.API_EMPLOYEEKEY FROM [LEAVE DAYS TAKEN] INNER JOIN HR_MASTER ON [LEAVE DAYS TAKEN].NUM = HR_MASTER.NUM";
                            query_type = 1;
                        }
                    }
                    else
                    {
                        query = "SELECT [LEAVE DAYS TAKEN].NUM, [LEAVE DAYS TAKEN].START, [LEAVE DAYS TAKEN].STOP, [LEAVE DAYS TAKEN].[TYPE], [LEAVE DAYS TAKEN].DAYS, [LEAVE DAYS TAKEN].APPROVED, [LEAVE DAYS TAKEN].NOT_APPROVED, [LEAVE DAYS TAKEN].NOTIFIED, [LEAVE DAYS TAKEN].TAKEN, [LEAVE DAYS TAKEN].RECALC_NEEDED, HR_MASTER.API_EMPLOYEEKEY FROM [LEAVE DAYS TAKEN] INNER JOIN HR_MASTER ON [LEAVE DAYS TAKEN].NUM = HR_MASTER.NUM WHERE HR_MASTER.API_EMPLOYEEKEY = ?";

                        query_type = 2;
                    }

                    if (query_type == 0) { return leavedays; } //incorrect parameter

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        _logger.LogInformation($"Z1 username = {num}");
                        switch (query_type)
                        {
                            case 2:
                                command.Parameters.AddWithValue("?", employeekey);
                                break;
                            default:
                                break;
                        }
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                _logger.LogInformation($"Z1");
                                while (reader.Read())
                                {
                                    string startDateString = reader["START"].ToString();
                                    DateTime? startDate;

                                    if (string.IsNullOrWhiteSpace(startDateString))
                                    {
                                        startDate = null;
                                    }
                                    else
                                    {
                                        startDate = DateTime.Parse(startDateString);
                                    }

                                    string stopDateString = reader["STOP"].ToString();
                                    DateTime? stopDate;

                                    if (string.IsNullOrWhiteSpace(stopDateString))
                                    {
                                        stopDate = null;
                                    }
                                    else
                                    {
                                        stopDate = DateTime.Parse(stopDateString);
                                    }

                                    LeaveDays e = new LeaveDays
                                    {
                                        Num = reader["NUM"].ToString(),
                                        StartDate = startDate,
                                        StopDate = stopDate,
                                        Days =(decimal)reader["DAYS"],
                                        LeaveType = (string)reader["TYPE"],
                                        Approved = (bool)reader["APPROVED"],
                                        NotApproved = (bool)reader["NOT_APPROVED"],
                                        Notified = (bool)reader["NOTIFIED"],
                                        Taken = (bool)reader["TAKEN"],
                                        RecalcNeeded = (bool)reader["RECALC_NEEDED"],
                                        DatabasePath = databasePath,
                                        EmployeeKey = reader["API_EMPLOYEEKEY"].ToString(),
                                    };
                                    leavedays.Add(e);
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Log4a: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log4b: {ex.Message}");
            }
            return leavedays;
        }

        public bool CreateLeaveApplication(string num, DateTime startdate, DateTime stopdate, string? leavetype, string companykey, string divisionkey, string employeekey)
        {

            string path = ResolveDatabasePath(companykey, divisionkey);
            // Check if the user has permission to perform this operation
            if (!AuthEmp(num, path,"leaveapplications"))
            {
                _logger.LogInformation($"employeekey {employeekey} attempted to update event in database {path}, but does not have necessary permissions");
                return false;
            }

            //
            int periodx = startdate.Month;
            int yearx = startdate.Year;

            // Proceed with the update operation if permission granted...
            string connectionString = GetConnectionString(path);
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                bool recalc_needed = true;
                string query = $"INSERT INTO [LEAVE DAYS TAKEN] (NUM, START, STOP, PERIOD, YEARX, [TYPE], RECALC_NEEDED) VALUES (num, startdate,stopdate,periodx,yearx,leavetype, recalc_needed)";

                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        
                        // Add parameters to prevent SQL Injection
                        command.Parameters.Add(new OleDbParameter("num", num));
                        command.Parameters.Add(new OleDbParameter("startdate", startdate));
                        command.Parameters.Add(new OleDbParameter("stopdate", stopdate));
                        command.Parameters.Add(new OleDbParameter("periodx", periodx));
                        command.Parameters.Add(new OleDbParameter("yearx", yearx));
                        command.Parameters.Add(new OleDbParameter("leavetype", leavetype));
                        command.Parameters.Add(new OleDbParameter("recalc_needed", recalc_needed));

                        // Execute the query
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            _logger.LogInformation($"No leave applications were made in database {path}");
                            return false;
                        }
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error in insert application query in database {path}. Error: {ex.Message}");
                    return false;
                }

            }
        }

        private List<HR_Master> GetHRMasterFromDatabase(string num, string databasePath, string companykey, string divisionkey,string employeekey)
        {
            List<HR_Master> hrmasteremps = new List<HR_Master>();
            try
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {

                    connection.Open();
                    string query = "";
                    int query_type = 0;
                    
                    if (employeekey.StartsWith("*"))
                    {
                        if (employeekey.Length == 1)
                        {
                            query = "SELECT HR_MASTER.NUM, HR_MASTER.[NAME],HR_MASTER.EMAIL, HR_MASTER.EMPLOYED, HR_MASTER.API_EMPLOYEEKEY FROM HR_MASTER ORDER BY HR_MASTER.NUM;";
                            query_type = 1;
                        }
                        
                    }
                    else
                    {
                        query = "SELECT HR_MASTER.NUM, HR_MASTER.[NAME],HR_MASTER.EMAIL, HR_MASTER.EMPLOYED, HR_MASTER.API_EMPLOYEEKEY FROM HR_MASTER WHERE HR_MASTER.API_EMPLOYEEKEY LIKE ?;";
                        query_type = 2;
                    }

                    if (query_type == 0) { return hrmasteremps; } //incorrect parameter

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        _logger.LogInformation($"Y1 username = {num}");
                        switch (query_type)
                        {
                            case 2:
                                command.Parameters.AddWithValue("?", employeekey);
                                break;
                            default:
                                break;
                        }
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                _logger.LogInformation($"Y1");
                                while (reader.Read())
                                {

                                    HR_Master e = new HR_Master
                                    {
                                        Num = reader["NUM"].ToString(),
                                        EmpName = reader["NAME"].ToString(),
                                        Email = reader["EMAIL"].ToString(),
                                        Employed = (bool)reader["EMPLOYED"],
                                        CompanyKey=companykey,
                                        DivisionKey=divisionkey,
                                        EmployeeKey = reader["API_EMPLOYEEKEY"].ToString(),

                                    };
                                    hrmasteremps.Add(e);
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Log3a: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log3b: {ex.Message}");
            }
            return hrmasteremps;
        }

        
        
    }
}

