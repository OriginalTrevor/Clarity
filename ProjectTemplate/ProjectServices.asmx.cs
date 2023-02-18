using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Principal;
using System.Security.Cryptography;

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
            string sqlSelect = "SELECT accountid, userid,IsAdmin FROM accounts WHERE userid=@idValue and pass=@passValue";
            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));


            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

            DataTable sqlDt = new DataTable();

            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                Session["accountid"] = sqlDt.Rows[0]["accountid"];
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

		[WebMethod(EnableSession = true)]
		public bool isAdmin()
		{
			
			if (Convert.ToInt32(Session["IsAdmin"]) == 1)
            {
				return true;
			}

            return false;
				
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
                // Session["userid"] = sqlDt.Rows[0]["userid"];
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
        public void CreateCard(string desc, string category)
        {

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            
            string sqlSelect = "insert into Cards (cardcreator, carddesc, cardcategory) " +
                "values(@uid, @desc, @category); SELECT LAST_INSERT_ID();";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@uid", Session["accountid"]);
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
        /**
        * MarkasDone will allow admins to marks cards completed once done 
        */

        [WebMethod(EnableSession = true)]
        public void MarkasDone(string cardid)
        {
            if (Convert.ToInt32(Session["IsAdmin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "update Cards set IsDone=1 where cardid=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(cardid));

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

        /**
        * DeleteCard will allow the administrators to delete unnecessary cards
        */

        [WebMethod(EnableSession = true)]
        public void DeleteCard(string cardid)
        {
            if (Convert.ToInt32(Session["IsAdmin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "delete from Cards where cardid=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(cardid));

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
            
            //WE ONLY DISPLAY CARDS WITH LOGGED IN USERS!
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

                    cards.Add(new Card
                    {
                        cardid = Convert.ToInt32(sqlDt.Rows[i]["cardid"]),
                        cardcreator = sqlDt.Rows[i]["cardcreator"].ToString(),
                        carddesc = sqlDt.Rows[i]["carddesc"].ToString(),
                        cardcategory = sqlDt.Rows[i]["cardcategory"].ToString()

                    });
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

        /**
         * GetAccounts will return all accounts and where or not they are admins. Only usable by admins
         */

        [WebMethod(EnableSession = true)]
        public Account[] GetAccounts()
        {
        
            if (Convert.ToInt32(Session["IsAdmin"]) == 1)
            {
                DataTable sqlDt = new DataTable("accounts");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect = "select accountid, userid, isAdmin from accounts";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                //gonna use this to fill a data table
                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                //filling the data table
                sqlDa.Fill(sqlDt);

                
                List<Account> accounts = new List<Account>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {
                    accounts.Add(new Account
                    {
                        accountid = Convert.ToInt32(sqlDt.Rows[i]["accountid"]),
                        userid = sqlDt.Rows[i]["userid"].ToString(),
                        isAdmin = Convert.ToBoolean(sqlDt.Rows[i]["isAdmin"])
                    });
                   
                }
                //convert the list of accounts to an array and return!
                return accounts.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new Account[0];
            }
        }

        /**
         * GiveUpvote will give the card an upvote if there isn't one already or take it away
         */
        [WebMethod(EnableSession = true)]
        public void GiveUpvote(string accountid, string cardid)
        {
            if (Session["userid"] != null)
            {
                bool voteAlreadyGiven = false;
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

                // First we need to ensure that there is not an existing vote for this account and card

                string sqlSelect = "SELECT accouontid, cardid, isUpvote FROM Votes WHERE accouontid=@aid AND cardid=@cid";
                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


                sqlCommand.Parameters.AddWithValue("@aid", HttpUtility.UrlDecode(accountid));
                sqlCommand.Parameters.AddWithValue("@cid", HttpUtility.UrlDecode(cardid));


                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

                DataTable sqlDt = new DataTable();

                sqlDa.Fill(sqlDt);

                // Record found in table, so a vote was already given
                if (sqlDt.Rows.Count > 0)
                {
                    //bool isUpvote= (bool)sqlDt.Rows[0]["isUpvote"];
                    voteAlreadyGiven = true;

                }

                if (!voteAlreadyGiven)
                {
                    sqlSelect = "insert into Votes (accouontid, cardid, isUpvote) " +
                    "values(@aid, @cid, true); SELECT LAST_INSERT_ID();";

                    sqlConnection = new MySqlConnection(sqlConnectString);
                    sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                    sqlCommand.Parameters.AddWithValue("@aid", HttpUtility.UrlDecode(accountid));
                    sqlCommand.Parameters.AddWithValue("@cid", HttpUtility.UrlDecode(cardid));


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
            }


        }


        /**
        * GiveDownvote will give the card a downvote if there isn't one already or take it away
        */
        [WebMethod(EnableSession = true)]
        public void GiveDownvote(string accountid, string cardid)
        {
            if (Session["userid"] != null)
            {
                bool voteAlreadyGiven = false;
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

                // First we need to ensure that there is not an existing vote for this account and card

                string sqlSelect = "SELECT accouontid, cardid, isUpvote FROM Votes WHERE accouontid=@aid AND cardid=@cid";
                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


                sqlCommand.Parameters.AddWithValue("@aid", HttpUtility.UrlDecode(accountid));
                sqlCommand.Parameters.AddWithValue("@cid", HttpUtility.UrlDecode(cardid));


                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

                DataTable sqlDt = new DataTable();

                sqlDa.Fill(sqlDt);

                // Record found in table, so a vote was already given
                if (sqlDt.Rows.Count > 0)
                {
                    //bool isUpvote= (bool)sqlDt.Rows[0]["isUpvote"];
                    voteAlreadyGiven = true;

                }

                if (!voteAlreadyGiven)
                {
                    sqlSelect = "insert into Votes (accouontid, cardid, isUpvote) " +
                    "values(@aid, @cid, false); SELECT LAST_INSERT_ID();";

                    sqlConnection = new MySqlConnection(sqlConnectString);
                    sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                    sqlCommand.Parameters.AddWithValue("@aid", HttpUtility.UrlDecode(accountid));
                    sqlCommand.Parameters.AddWithValue("@cid", HttpUtility.UrlDecode(cardid));


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
