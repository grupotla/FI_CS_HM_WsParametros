using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Data;

public class MySql_
{
    public MySql_()
    {
    }



    public static System.Data.IDataReader GetDataReader(string sqlString, string product)
    {
        MySqlConnection conn = OpenMysqlConnection(product);
        MySqlCommand comm = new MySqlCommand();
        comm.CommandType = CommandType.Text;
        comm.Connection = conn;
        comm.CommandText = sqlString;
        MySqlDataReader reader = comm.ExecuteReader();
        return reader;
    }




    public static string GetScalar(string sqlString, string product)
    {
        string valor = "";

        try
        {
            MySqlConnection conn = OpenMysqlConnection(product);
            MySqlCommand comm = new MySqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;
            comm.CommandText = sqlString;
            valor = (string) comm.ExecuteScalar();

        }
        catch (Exception ex)
        {
            valor = ex.Message;
        }

        return valor;
    }






    #region get_mysql_list

    public static IEnumerable<Dictionary<string, object>> get_mysql_list(string product, string query)
    {
        IEnumerable<Dictionary<string, object>> rows = null; // new IEnumerable<Dictionary<string, object>>();

        try
        {
            MySqlConnection conn = OpenMysqlConnection(product);
            MySqlCommand comm = new MySqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;
            comm.CommandText = query;
            MySqlDataReader reader = comm.ExecuteReader();
            rows = Serialize(reader);

            CloseMySQLObj(reader, comm, conn);
        }
        catch (Exception e)
        {
            throw e;

        }

        return rows;
    }

    #endregion

    #region OpenPostgresConnection_BK
    /*
    public static MySqlConnection OpenMysqlConnection_BK(string product)
    {
        MySqlConnection conn = null;
        try
        {

            string strconn = "";
            switch (product)
            {
                case "aereo":
                    strconn = "SERVER=10.10.1.18;UID=DbAereo;PWD=aereoaimar;DATABASE=bk_db_aereo";
                    break;

                case "terrestre":
                    strconn = "SERVER=10.10.1.18;UID=DbTerrestre;PWD=terrestreaimar;DATABASE=bk_db_terrestre";
                    //strconn = "SERVER=localhost;UID=root;PWD=123456;DATABASE=db_terrestre";
                    break;

                case "aduana":

                    strconn = "SERVER=10.10.1.18;UID=us3r_cUstomer;PWD=cUst0m3R;DATABASE=customer";

                    break;
            }

            conn = new MySqlConnection(strconn);
            conn.Open();
        }
        catch (Exception e)
        {
            throw e;

        }
        return conn;
    }

    */  
    #endregion


    public static MySqlConnection OpenMysqlConnection(string product)
    {
        MySqlConnection conn = null;
        try
        {
            string db = "";
            switch (product)
            {
                case "aereo":
                    db = "aereo";
                    break;

                case "terrestre":
                    db = "terrestre";
                    break;
            }

            string strconn = System.Configuration.ConfigurationManager.ConnectionStrings[db].ConnectionString;

            conn = new MySqlConnection(strconn);

            conn.Open();
        }
        catch (Exception e)
        {
            throw e;

        }
        return conn;
    }


    public static void CloseMySQLObj(MySqlDataReader rd, MySqlCommand comm, MySqlConnection conn)
    {
        try
        {
            rd.Close();
            comm.Dispose();
            conn.Close();
            conn.Dispose();
        }
        catch (Exception e)
        {
            throw e;

        }
    }



    public static IEnumerable<Dictionary<string, object>> Serialize(MySqlDataReader reader)
    {
        var results = new List<Dictionary<string, object>>();
        var cols = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            cols.Add(reader.GetName(i));

        while (reader.Read())
            results.Add(SerializeRow(cols, reader));

        return results;
    }

    private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, MySqlDataReader reader)
    {
        var result = new Dictionary<string, object>();
        int i = 0;
        // DateTime date1;
        string s = "";
        foreach (var col in cols)
        {

            var v = reader[col];
            var t = reader.GetFieldType(i);  //col.GetType();

            if (t.Name == "DateTime")
            {
                s = String.Format("{0:yyyy-MM-dd HH:mm:ss}", v).Replace(" 00:00:00", "");
                result.Add(col, s);
            }
            else
                result.Add(col, v);

            i++;
        }

        return result;
    }



    public static int EjecutaQuery(string sqlString, string product)
    {
        int result = 0;

        try
        {
            MySqlConnection conn = OpenMysqlConnection(product);
            MySqlCommand comm = new MySqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;
            comm.CommandText = sqlString;
            result = comm.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            throw e;
        }

        return result;
    }


    public static int EjecutaQueryArray(List<string> queryArray, string product)
    {
        int result = 0;

        try
        {
            MySqlConnection conn = OpenMysqlConnection(product);
            MySqlCommand comm = new MySqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            comm.CommandText = "SET autocommit = OFF; START TRANSACTION;";
            result = comm.ExecuteNonQuery();

            int c = 0;

            foreach (string query in queryArray)
            {
                comm.CommandText = query;
                result = comm.ExecuteNonQuery();

                if (result <= 0)
                {
                    comm.CommandText = " ROLLBACK;";
                    result = comm.ExecuteNonQuery();
                    break;
                }

                c++;
            }

            result = 0;

            if (c == queryArray.Count)
            {
                comm.CommandText = "COMMIT;";
                result = comm.ExecuteNonQuery();
                result = 1;
            }


        }
        catch (Exception e)
        {
            throw e;
        }

        return result;
    }


    /*
    SET autocommit = OFF;

    START TRANSACTION;

    UPDATE ChargeItems SET InvoiceID = 222  WHERE ChargeID = 2 AND AWBID = 4241 AND DocTyp = 1 AND Expired = 0;
    UPDATE ChargeItems SET InvoiceID = 222 WHERE ChargeID = 3 AND AWBID = 4242 AND DocTyp = 1 AND Expired = 9;
    UPDATE ChargeItems SET InvoiceID = 222  WHERE ChargeID = 4 AND AWBID = 4241 AND DocTyp = 1 AND Expired = 0;
							
-- COMMIT;
    */



    public static TEntity GetRowMysql<TEntity>(string product, string qry) where TEntity : class, new()
    {

        TEntity data = new TEntity();

        try
        {
            MySqlConnection conn = OpenMysqlConnection(product);
            MySqlCommand comm = new MySqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;
            comm.CommandText = qry;
            MySqlDataReader reader = comm.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                data = Utils.ReflectType<TEntity>(reader);
            }

            CloseMySQLObj(reader, comm, conn);

        }
        catch (Exception e)
        {
            throw e;

        }


        return data;
    }

}