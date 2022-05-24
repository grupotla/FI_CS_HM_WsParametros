using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

public class Utils
{
    public Utils()
    {
    }






    /*
    public static List<TEntity> ParseJson<TEntity>(string json)
    {

        List<TEntity> result = new List<TEntity>();

        try
        {

            string temp = ""; //, json_exactus = "";

            json = json.Replace("[", "").Replace("]", "");

            List<string> lineas = json.Split('}').ToList();

            char[] MyChar = { ',' };

            foreach (string linea in lineas)
            {
                try
                {
                    temp = linea + "}";

                    temp = temp.Replace("\r", "").Replace("\n", "").Replace("\t", "");

                    if (temp.Substring(0, 1) == ",")
                    {
                        temp = temp.TrimStart(MyChar);
                    }

                    temp = temp.Replace("NULL", "\"\"").Replace("\"-\"", "\"\"");

                    TEntity row = (TEntity)DeserializeJson<TEntity>(temp);


                    result.Add(row);

                    //if (json_exactus != "") json_exactus += ",\n";

                    //json_exactus += SerializeJson(result);

                }
                catch (Exception ex)
                {
                    temp = ex.Message;

                    //log4net ErrLog = new log4net();
                    //ErrLog.ErrorLog(ex.Message);
                    //return null;
                }
            }

            //json_exactus = "[\n" + json_exactus + "\n]\n";



        }
        catch (Exception ex)
        {
            //Response.Write(ex.Message);
        }

        return result;

    }
    */

    public static object DeserializeJson<T>(string Json)
    {
        JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();

        return JavaScriptSerializer.Deserialize<T>(Json);
    }

    public static string SerializeJson(object obj)
    {
        JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();

        return JavaScriptSerializer.Serialize(obj);
    }

    //Encode
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    //Decode
    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static bool IsBase64String(string s)
    {
        s = s.Trim();
        return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);

    }


    public static List<TEntity> get_list_struct<TEntity>(System.Data.IDataReader ViewReporte) where TEntity : class, new()
    {
        List<TEntity> rows = new List<TEntity>();
        try
        {

            while (ViewReporte.Read())
            {
                TEntity data = ReflectType<TEntity>(ViewReporte);
                rows.Add(data);
            }

            /*
            foreach (System.Data.IDataRecord line in ViewReporte)
            {
                TEntity data = ReflectType<TEntity>(line);
                rows.Add(data);
            }
            */

        }
        catch (Exception e)
        {
            throw e;
        }
        return rows;
    }

    public static TEntity ReflectType<TEntity>(System.Data.IDataRecord dr) where TEntity : class, new()
    {
        TEntity instanceToPopulate = new TEntity();

        System.Reflection.PropertyInfo[] propertyInfos = typeof(TEntity).GetProperties
        (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        //for each public property on the original
        foreach (System.Reflection.PropertyInfo pi in propertyInfos)
        {
            var n = pi.Name;
            //var t = pi.GetType();

            try
            {

                object dbValue = dr[n];

                if (dbValue != null)
                {
                    pi.SetValue(instanceToPopulate, Convert.ChangeType
                    (dbValue, pi.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                }

            }
            catch (Exception e)
            {
                //throw e; aca no aplica, debe continuar
            }

            //var v = pi.


            /*
            DataFieldAttribute[] datafieldAttributeArray = pi.GetCustomAttributes
            (typeof(DataFieldAttribute), false) as DataFieldAttribute[];

            //this attribute is marked with AllowMultiple=false
            if (datafieldAttributeArray != null && datafieldAttributeArray.Length == 1)
            {
                DataFieldAttribute dfa = datafieldAttributeArray[0];

                //this will blow up if the datareader does not contain the item keyed dfa.Name
                object dbValue = dr[dfa.Name];

                if (dbValue != null)
                {
                    pi.SetValue(instanceToPopulate, Convert.ChangeType
                    (dbValue, pi.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                }
            }
            */

        }

        return instanceToPopulate;
    }


    /*
Function get_id_routing(EXType, EXDBCountry, EXID)
    Select Case EXType
        Case 0, 11
            OpenConnOcean Conn, "ventas_" & LCase(EXDBCountry)
                set rs = Conn.Execute("select coalesce(b.id_routing, 0) from contenedor_completo a inner join bl_completo b on b.bl_id=a.bl_id where a.contenedor_id = " & EXID)
                if not rs.EOF then
                    get_id_routing = rs(0)
                end if
            CloseOBJs Conn, rs
        Case 1, 12
            OpenConnOcean Conn, "ventas_" & LCase(EXDBCountry)
                set rs = Conn.Execute("select coalesce(id_routing, 0) from bill_of_lading where bl_id = " & EXID)
                if not rs.EOF then
                    get_id_routing = rs(0)
                end if
            CloseOBJs Conn, rs
    '    Case 2, 13
    '        OpenConnOcean Conn, "ventas_" & LCase(EXDBCountry)
    '            set rs = Conn.Execute("select coalesce(b.id_routing, 0) from divisiones_bl a inner join bill_of_lading b on b.bl_id=a.bl_asoc where a.division_id = " & EXID)
    '            if not rs.EOF then
    '                get_id_routing = rs(0)
    '            end if
    '        CloseOBJs Conn, rs
        Case 9, 10
            OpenConnAir Conn
                set rs = Conn.Execute("select RoutingID from Awbi where AwbID = " & EXID)
                if not rs.EOF then
                    get_id_routing = rs(0)
                end if
            CloseOBJs Conn, rs
    End Select
End Function
*/

    public static Struct.Scalar get_id_routing(int EXType, string EXDBCountry, int EXID)
    {
        Struct.Scalar data = null;

        string qry;

        try
        {
            switch (EXType)
            {
                case 0:
                case 11:
                    qry = "select coalesce(b.id_routing, 0) from contenedor_completo a inner join bl_completo b on b.bl_id=a.bl_id where a.contenedor_id = " + EXID;
                    data = Postgres_.GetRowPostgres<Struct.Scalar>("ventas_" + EXDBCountry.ToLower(), qry);
                    break;

                case 1:
                case 12:
                    qry = "select coalesce(id_routing, 0) from bill_of_lading where bl_id = " + EXID;
                    data = Postgres_.GetRowPostgres<Struct.Scalar>("ventas_" + EXDBCountry.ToLower(), qry);
                    break;

                case 2:
                case 13:
                    //qry = "select coalesce(b.id_routing, 0) from divisiones_bl a inner join bill_of_lading b on b.bl_id=a.bl_asoc where a.division_id = " & EXID)      
                    //data = GetRowPostgres<Struct.Scalar>("ventas_" + EXDBCountry.ToLower(), qry);
                    break;

                case 9:
                case 10:
                    qry = "select RoutingID from Awbi where AwbID = " + EXID;
                    data = MySql_.GetRowMysql<Struct.Scalar>("aereo", qry);
                    break;
            }

        }
        catch (Exception e)
        {
            throw e;

        }
        return data;
    }

    //using System.ComponentModel.DataAnnotations;

    public static bool IsValidEmail(string source)
    {
        return new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(source);
    }





    public static FileInfo[] ProcessDirectory(string targetDirectory)
    {

        DirectoryInfo info = new DirectoryInfo(targetDirectory);
        FileInfo[] files = info.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();
        
        /*
        
        foreach (FileInfo file in files)
        {
            // DO Something...
        }

        //ProcessFile(fileName);
        /*
        // Recurse into subdirectories of this directory.
        string [] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        foreach(string subdirectory in subdirectoryEntries)
            ProcessDirectory(subdirectory);
        */

        return files;
    }

}