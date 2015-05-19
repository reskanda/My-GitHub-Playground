using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace HubPasswordHasher
{
    public class Program
    {
        private static readonly int GlobalTimeout = 1000 * 60 * 15;
        private const string DeploymentEnvironmentChars = "dp";
        private const string ResponseChars = "yn";

        private enum DeploymentEnvironment
        {
            HubContextDev,
            HubContextProd,
            Undefined
        }

        private enum PasswordHashStatus
        {
            Aborted,
            ExceptionThrown,
            Success,
            Undefined
        }

        public static void Main(string[] args)
        {
            InitConsole();

            var environment = GetDeploymentEnvironment();
            var users = GetMigrationIdentifier(environment);

            Exception ex = null;
            var status = HashPasswords(environment, users, out ex);

            ShutOffConsole(status, ex);
        }

        private static void InitConsole()
        {
            double width = Console.WindowWidth;
            double height = Console.WindowHeight;

            Console.SetWindowSize(Convert.ToInt32(Math.Floor(width)), Convert.ToInt32(Math.Floor(height)));

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("====================================================================================");
            Console.WriteLine(" ******                      MyGK User Password Hasher                       ****** ");
            Console.WriteLine("====================================================================================");

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("\nThis tool creates SHA 256 bit hashes with random salts for newly migrated users that have plain text passwords.");
            Console.WriteLine("It uses a unique identifier allocated by migration to grab the desired users and apply the password hashing algorithm.\n");
        }

        private static DeploymentEnvironment GetDeploymentEnvironment()
        {
            DeploymentEnvironment e = DeploymentEnvironment.Undefined;
            Console.WriteLine("There are two available database environments you can connect to:\n");
            Console.WriteLine("\tD => Development");
            Console.WriteLine("\tP => Production\n");
            Console.WriteLine("Please select the environment you would like to connect to by entering the environment designated character:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            var key = Console.ReadKey().KeyChar.ToString().ToLower();
            Console.ForegroundColor = ConsoleColor.White;

            if (DeploymentEnvironmentChars.IndexOf(key) == -1)
            {
                do
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\nThe environment designated character you entered is incorrect.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\n\nThere are two available database environments you can connect to:\n");
                    Console.WriteLine("\tD => Development");
                    Console.WriteLine("\tP => Production\n");
                    Console.WriteLine("Please select the environment you would like to connect to by entering the environment designated character:");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    key = Console.ReadKey().KeyChar.ToString().ToLower();
                    Console.ForegroundColor = ConsoleColor.White;
                }
                while (DeploymentEnvironmentChars.IndexOf(key) == -1);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\nEnvironment selected successfully.\n\n");
            Console.ForegroundColor = ConsoleColor.White;

            switch (key)
            {
                case "d":
                    e = DeploymentEnvironment.HubContextDev;
                    break;
                case "p":
                    e = DeploymentEnvironment.HubContextProd;
                    break;
            }
            return e;
        }

        private static DataTable GetMigrationIdentifier(DeploymentEnvironment environment)
        {
            Console.WriteLine("Please type the migration identifier that was used to import users then hit Enter:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            var identifier = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;

            var users = GetUsersByCreator(environment, identifier);

            if (users.Rows.Count == 0)
            {
                do
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nThe migration identifier you entered is incorrect.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\n\nPlease type the migration identifier that was used to import users then hit Enter:");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    identifier = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.White;

                    users = GetUsersByCreator(environment, identifier);
                }
                while (users.Rows.Count == 0);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format("\n{0} user(s) were detected successfully based on the migration identifier you entered.\n", users.Rows.Count));
            Console.ForegroundColor = ConsoleColor.White;

            return users;
        }

        private static PasswordHashStatus HashPasswords(DeploymentEnvironment environment, DataTable users, out Exception exception)
        {
            PasswordHashStatus status = PasswordHashStatus.Undefined;
            exception = null;

            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("\n\nARE YOU SURE YOU'D LIKE TO HASH THE EXISTING PASSWORDS OF THE DETECTED {0} USERS?????\n", users.Rows.Count));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\tY => Yes");
                Console.WriteLine("\tN => No\n");
                Console.WriteLine("Please enter the designated character for your response:");
                Console.ForegroundColor = ConsoleColor.Cyan;
                var key = Console.ReadKey().KeyChar.ToString().ToLower();
                Console.ForegroundColor = ConsoleColor.White;

                if (ResponseChars.IndexOf(key) == -1)
                {
                    do
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n\nThe response designated character you entered is incorrect.");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(string.Format("\n\nARE YOU SURE YOU'D LIKE TO HASH THE EXISTING PASSWORDS OF THE DETECTED {0} USERS?????\n", users.Rows.Count));
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\tY => Yes");
                        Console.WriteLine("\tN => No\n");
                        Console.WriteLine("Please enter the designated character for your response:");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        key = Console.ReadKey().KeyChar.ToString().ToLower();
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    while (ResponseChars.IndexOf(key) == -1);
                }

                if (key == "y")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n\nTHERE IS NO GOING BACK -> ARE YOU REALLY REALLY REALLY SURE???\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\tY => Yes");
                    Console.WriteLine("\tN => No\n");
                    Console.WriteLine("Please enter the designated character for your response:");
                    Console.ForegroundColor = ConsoleColor.Cyan;

                    var confirmKey = Console.ReadKey().KeyChar.ToString().ToLower();
                    Console.ForegroundColor = ConsoleColor.White;

                    if (ResponseChars.IndexOf(confirmKey) == -1)
                    {
                        do
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n\nThe response designated character you entered is incorrect.");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\n\nTHERE IS NO GOING BACK -> ARE YOU REALLY REALLY REALLY SURE???\n");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\tY => Yes");
                            Console.WriteLine("\tN => No\n");
                            Console.WriteLine("Please enter the designated character for your response:");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            confirmKey = Console.ReadKey().KeyChar.ToString().ToLower();
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        while (ResponseChars.IndexOf(confirmKey) == -1);
                    }

                    if (confirmKey == "y")
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;

                        int index = 0;
                        Console.WriteLine(Environment.NewLine);
                        Console.WriteLine(Environment.NewLine);
                        foreach (DataRow dr in users.Rows)
                        {
                            string password = dr["PASSWORD"].ToString().Trim();
                            if (string.IsNullOrWhiteSpace(password))
                            {
                                password = dr["CONTACTID"].ToString();
                            }

                            string salt = SaltedHash.GenerateRandomSalt();
                            string hash = SaltedHash.GenerateHash(HashAlgorithmType.SHA256, password, salt);

                            dr["SALT"] = salt;
                            dr["PASSWORD"] = hash;
                            dr["CREATEUSER"] = "mygk_migrate";
                            dr["MODIFYUSER"] = "mygk_hash";
                            dr["MODIFYDATE"] = DateTime.Now;

                            ShowPercentProgress("Password hashing in progress ... ", index, users.Rows.Count);
                            index++;
                        }

                        Console.WriteLine("\nCommitting changes to the database ... This may take several minutes ... \n");
                        
                        
                        try
                        {
                            string connectionString = WebConfig.GetAppConnectionString(environment.ToString());

                            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.RequiresNew, new TimeSpan(0, 0, 0, 0, GlobalTimeout)))
                            {
                                using (SqlConnection connection = new SqlConnection(connectionString))
                                {
                                    connection.Open();

                                    SqlDataAdapter adapter = new SqlDataAdapter();

                                    adapter.UpdateCommand = new SqlCommand("UPDATE HVXUSER SET PASSWORD=@Password, SALT=@Salt, CREATEUSER=@CreateUser, MODIFYUSER=@ModifyUser, MODIFYDATE=@ModifyDate WHERE HVXUSERID=@HvxUserId;", connection);
                                    adapter.UpdateCommand.Parameters.Add("@Password", SqlDbType.VarChar, 256, "PASSWORD");
                                    adapter.UpdateCommand.Parameters.Add("@Salt", SqlDbType.VarChar, 24, "SALT");
                                    adapter.UpdateCommand.Parameters.Add("@CreateUser", SqlDbType.VarChar, 12, "CREATEUSER");
                                    adapter.UpdateCommand.Parameters.Add("@ModifyUser", SqlDbType.VarChar, 12, "MODIFYUSER");
                                    adapter.UpdateCommand.Parameters.Add("@ModifyDate", SqlDbType.DateTime, 256, "MODIFYDATE");
                                    adapter.UpdateCommand.Parameters.Add("@HvxUserId", SqlDbType.VarChar, 12, "HVXUSERID");
                                    adapter.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;

                                    adapter.UpdateCommand.CommandTimeout = GlobalTimeout;

                                    adapter.UpdateBatchSize = 0;

                                    adapter.Update(users);
                                    connection.Close();
                                }

                                ts.Complete();
                            }
                        }
                        catch (TransactionAbortedException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("TransactionAbortedException Message: {0}", ex.Message);
                        }
                        catch (ApplicationException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ApplicationException Message: {0}", ex.Message);
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("\n{0} user password(s) were hashed successfully.", users.Rows.Count));
                        Console.ForegroundColor = ConsoleColor.White;

                        status = PasswordHashStatus.Success;
                    }
                    else if (confirmKey == "n")
                    {
                        status = PasswordHashStatus.Aborted;
                    }
                }
                else if (key == "n")
                {
                    status = PasswordHashStatus.Aborted;
                }
            }
            catch (Exception ex)
            {
                status = PasswordHashStatus.ExceptionThrown;
                exception = ex;
            }

            return status;
        }

        private static void ShutOffConsole(PasswordHashStatus status, Exception ex)
        {
            switch (status)
            {
                case PasswordHashStatus.Aborted:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\nThe process has been aborted by you.\n\nHit any key to exit the application ... ");
                    break;
                case PasswordHashStatus.ExceptionThrown:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("\n\nThe process ran into an unexpected issue and the following exception was thrown:\n\n{0}\n{1}\n\nHit any key to exit the application ... ", ex.Message, ex.InnerException.Message));
                    break;
                case PasswordHashStatus.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n\nThe process has been completed successfully.\n\nHit any key to exit the application ... ");
                    break;
            }

            Console.ReadLine();
        }

        private static void ShowPercentProgress(string message, int currElementIndex, int totalElementCount)
        {
            if (currElementIndex < 0 || currElementIndex >= totalElementCount)
            {
                throw new InvalidOperationException("currElement out of range");
            }
            int percent = (100 * (currElementIndex + 1)) / totalElementCount;
            Console.Write("\r{0}{1}% complete", message, percent);
            if (currElementIndex == totalElementCount - 1)
            {
                Console.WriteLine(Environment.NewLine);
            }
        }

        private static DataTable GetUsersByCreator(DeploymentEnvironment environment, string identifier)
        {
            string connectionString = WebConfig.GetAppConnectionString(environment.ToString());
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter();

                adapter.SelectCommand = new SqlCommand(string.Format("SELECT * FROM HVXUSER WHERE CREATEUSER = '{0}'", identifier));
                adapter.SelectCommand.Connection = connection;

                SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

                connection.Open();

                DataTable users = new DataTable();
                adapter.Fill(users);

                return users;
            }
        }
    }
}
