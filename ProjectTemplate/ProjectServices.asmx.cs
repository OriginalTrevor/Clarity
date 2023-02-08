using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
        ////////////////////////////////////////////////////////////////////////
        ///replace the values of these variables with your database credentials
        ////////////////////////////////////////////////////////////////////////
        private string dbID = "springa2023team4";
        private string dbPass = "springa2023team4";
        private string dbName = "springa2023team4";
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        ///call this method anywhere that you need the connection string!
        ////////////////////////////////////////////////////////////////////////
        private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
        ////////////////////////////////////////////////////////////////////////


        /**
		 * LogOn
		 */

        [WebMethod(EnableSession = true)]
        public bool LogOn(string uid, string pass)
        {
            bool success = false;
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "SELECT userid FROM accounts WHERE userid=@idValue and pass=@passValue";
            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));


            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

            DataTable sqlDt = new DataTable();

            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                Session["userid"] = sqlDt.Rows[0]["userid"];
                success = true;
            }
            //return the result!
            return success;
        }

        [WebMethod(EnableSession = true)]
        public bool LogOff()
        {
            Session.Abandon();
            return true;
        }

        /**
         * CreateAccount() allows users to make nonadmin accounts
         * If the username is already in use, do not create the account, otherwise,
         * can make the account
         */
        [WebMethod(EnableSession = true)]
        public void CreateAccount(string uid, string pass)
        {

            bool nameInUse = false;
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            // First we need to ensure that there is not an existing account with the same username

            string sqlSelect = "SELECT userid FROM accounts WHERE userid=@idValue";
            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));


            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

            DataTable sqlDt = new DataTable();

            sqlDa.Fill(sqlDt);

            // Name is found in table, so name is already in use
            if (sqlDt.Rows.Count > 0)
            {
                Session["userid"] = sqlDt.Rows[0]["userid"];
                nameInUse = true;

            }

            if (!nameInUse)
            {
                //the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
                //does is tell mySql server to return the primary key of the last inserted row.
                sqlSelect = "insert into accounts (userid, pass) " +
                    "values(@idValue, @passValue); SELECT LAST_INSERT_ID();";

                sqlConnection = new MySqlConnection(sqlConnectString);
                sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
                sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

                //this time, we're not using a data adapter to fill a data table.  We're just
                //opening the connection, telling our command to "executescalar" which says basically
                //execute the query and just hand me back the number the query returns (the ID, remember?).
                //don't forget to close the connection!
                sqlConnection.Open();
                //we're using a try/catch so that if the query errors out we can handle it gracefully
                //by closing the connection and moving on
                try
                {
                    int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                    //here, you could use this accountID for additional queries regarding
                    //the requested account.  Really this is just an example to show you
                    //a query where you get the primary key of the inserted row back from
                    //the database!
                }
                catch (Exception e)
                {
                }
            }

            sqlConnection.Close();
        }


        /////////////////////////////////////////////////////////////////////////
        //don't forget to include this decoration above each method that you want
        //to be exposed as a web service!
        [WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from test";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}
	}
}
