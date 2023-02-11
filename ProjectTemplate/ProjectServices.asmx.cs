using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Principal;

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
            string sqlSelect = "SELECT userid,IsAdmin FROM accounts WHERE userid=@idValue and pass=@passValue";
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
                Session["IsAdmin"] = sqlDt.Rows[0]["IsAdmin"];
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


        [WebMethod(EnableSession = true)]
        public void CreateCard(string uid, string desc, string category)
        {

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            
            string sqlSelect = "insert into Cards (cardcreator, carddesc, cardcategory) " +
                "values(@uid, @desc, @category); SELECT LAST_INSERT_ID();";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@uid", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@desc", HttpUtility.UrlDecode(desc));
            sqlCommand.Parameters.AddWithValue("@category", HttpUtility.UrlDecode(category));

            
            sqlConnection.Open();
            
            try
            {
                int cardID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        /**
         * UpgradetoAdmin will let admins upgrade regular user accounts to admin accounts
         */
        [WebMethod(EnableSession = true)]
        public void UpgradetoAdmin(string uid)
        {
            if (Convert.ToInt32(Session["IsAdmin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "update accounts set IsAdmin=1 where userid=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }

        //EXAMPLE OF A SELECT, AND RETURNING "COMPLEX" DATA TYPES
        [WebMethod(EnableSession = true)]
        public Card[] GetCards()
        {
            //check out the return type.  It's an array of Account objects.  You can look at our custom Account class in this solution to see that it's 
            //just a container for public class-level variables.  It's a simple container that asp.net will have no trouble converting into json.  When we return
            //sets of information, it's a good idea to create a custom container class to represent instances (or rows) of that information, and then return an array of those objects.  
            //Keeps everything simple.

            //WE ONLY SHARE ACCOUNTS WITH LOGGED IN USERS!
            if (Session["userid"] != null)
            {
                DataTable sqlDt = new DataTable("accounts");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect = "select cardid, cardcreator, carddesc, cardcategory from Cards order by cardid";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                //gonna use this to fill a data table
                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                //filling the data table
                sqlDa.Fill(sqlDt);

                //loop through each row in the dataset, creating instances
                //of our container class Account.  Fill each acciount with
                //data from the rows, then dump them in a list.
                List<Card> cards = new List<Card>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {
                    //only share user id and pass info with admins!
                    if (Convert.ToInt32(Session["admin"]) == 1)
                    {
                        cards.Add(new Card
                        {
                            cardid = Convert.ToInt32(sqlDt.Rows[i]["cardid"]),
                            cardcreator = sqlDt.Rows[i]["cardcreator"].ToString(),
                            carddesc = sqlDt.Rows[i]["carddesc"].ToString(),
                            cardcategory = sqlDt.Rows[i]["cardcategory"].ToString()
                            
                        });
                    }
                    else
                    {
                        cards.Add(new Card
                        {
                            cardid = Convert.ToInt32(sqlDt.Rows[i]["cardid"]),
                            cardcreator = sqlDt.Rows[i]["cardcreator"].ToString(),
                            carddesc = sqlDt.Rows[i]["carddesc"].ToString(),
                            cardcategory = sqlDt.Rows[i]["cardcategory"].ToString()
                        });
                    }
                }
                //convert the list of cards to an array and return!
                return cards.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new Card[0];
            }
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
				string testQuery = "select * from accounts";

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
