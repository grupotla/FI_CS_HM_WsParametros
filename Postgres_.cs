using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Npgsql;
using System.Data;


public class Postgres_
{
    public Postgres_()
    {
    }

    public static string CrossSendRoutingTTC(string json, string _table, string tipo)
    {




        string str_routing = (@"

INSERT INTO routings (
routing,
cotizacion_id,
vendedor_id,
order_no,
no_embarque,
fecha,
id_routing_type,
id_cliente,
id_shipper,
id_incoterms,
id_transporte,
tramite_aduanal,
observaciones,
id_pais,
import_export,
comodity_id,
id_tipo_paquete,
id_pais_origen,
id_pais_destino,
id_usuario_creacion,
hora_ingreso,
pais_origen_carga,
no_piezas,
peso,
volumen,
poliza_seguro,
carga_peligrosa,
tla_tipo_tramite,
id_facturar,
file,
referencia,
routing_no
) 

select 
	'AD-' || b.codigo || '-I-' || to_char(current_date, 'IW-IY') || '-' || 
(
select COALESCE(routing_no,0)+1 from routings 
where id_routing_type  = 2 and id_transporte = 8 and id_pais = b.codigo AND routing_no IS NOT NULL 
ORDER BY routing_no DESC LIMIT 1
),
	NULL,
	508,                     -- vendedor_id           
	a.cl_pedido,                -- order_no 
    a.cl_pedido,                -- no_embarque             
	CURRENT_DATE, 
	2,					        -- id_routing_type       
	CAST(a.cliente AS INT),
	1,      		            -- id_shipper                    
	NULL,
	8,                          -- c.id_transporte, fijo 8 aduana
	true,
	'',
	b.codigo,			        -- id_pais               
	true,
	82069,				        -- comodity_id  carga general         
	'4',                        -- id_tipo_paquete
	b.codigo,			        -- id_pais_origen        
	NULL,
	508,                        -- id_usuario_creacion
	NOW(),
	e.codigo,			        -- pais_origen_carga     
	'0',				        -- no_piezas             
	'0.00',				        -- peso                  
	'0',   				        -- volumen               
	'',
	false,
	d.codigo,
	CAST(a.cliente AS INT),     -- id_facturar
	a.file,
    a.referencia,
(
select COALESCE(routing_no,0)+1 from routings 
where id_routing_type  = 2 and id_transporte = 8 and id_pais = b.codigo AND routing_no IS NOT NULL 
ORDER BY routing_no DESC LIMIT 1
)

from tts_json a 
    left join paises b on b.tla_codigo = cast(a.cod_pais as int)
    -- left join transporte c on c.tla_medio_transporte = cast(a.medio_transporte as int)
    left join tla_tipo_tramite d on d.abreviatura = a.tipo_tramite 
    left join paises e on e.tla_codigo = cast(a.pais_origen as int)
where a.id_json = 0
");
        //returning id_routing;

        string str_cargos = (@"
INSERT INTO cargos_routing (
id_routing,
id_moneda,
id_rubro,
detalle,
local,
show,
activo,
id_servicio,
observacion,
valor,
costo,
fecha_registro,
prepaid,
tipo_documento
) VALUES (
360217,
4,
58,
'',
true,
true,
true,
9,
'',
0,
0,
NOW(),
false,
1
) 
");

        //returning id_cargos_routing;

        NpgsqlConnection conn;
        NpgsqlCommand comm;

        NpgsqlTransaction tran = null;

        conn = OpenPostgresConnection(tipo);


        List<Struct.tts_routing> json_response = new List<Struct.tts_routing>();

        Struct.tts_routing response = new Struct.tts_routing();

        try
        {
            var json_file = Utils.DeserializeJson<Struct.tts_file[]>(json);

            int id_json = 0, id_routing = 0, id_cargos_routing = 0;
            string query = "";

            foreach (Struct.tts_file json_row in (IEnumerable<Struct.tts_file>)json_file)
            {
                response = new Struct.tts_routing();
                response.file = json_row.file;
                response.fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffffff");
                response.refti = "-";
                response.stat = 0;

                tran = conn.BeginTransaction();

                comm = new NpgsqlCommand();
                comm.CommandType = CommandType.Text;
                comm.Connection = conn;

                try
                {
                    id_json = 0;
                    id_routing = 0;
                    id_cargos_routing = 0;

                    // insert json
                    response.msg = "Error al intentar guardar json file";
                    query = BuiltInsertQuery(json_row).Replace("_table", _table);
                    comm.CommandText = query;
                    comm.ExecuteNonQuery();

                    comm.CommandText = "select currval('tts_json_id_json_seq');";
                    var result = comm.ExecuteScalar();
                    id_json = result != null ? Convert.ToInt32(result) : 0;

                    //insert routings                     
                    if (id_json > 0)
                    {

                        response.msg = "Error al intentar guardar routing";
                        comm.CommandText = str_routing.Replace("a.id_json = 0", "a.id_json = " + id_json);
                        comm.ExecuteNonQuery();

                        comm.CommandText = "select currval('routings_id_routing_seq');";
                        result = comm.ExecuteScalar();
                        id_routing = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // cargos rutings
                    if (id_routing > 0)
                    {
                        response.msg = "Error al intentar guardar cargo";
                        comm.CommandText = str_cargos.Replace("360217", id_routing.ToString());
                        comm.ExecuteNonQuery();

                        comm.CommandText = "select currval('cargos_routing_id_cargos_routing_seq');";
                        result = comm.ExecuteScalar();
                        id_cargos_routing = result != null ? Convert.ToInt32(result) : 0;
                    }

                    // actualiza cruze de datos
                    if (id_cargos_routing > 0)
                    {
                        response.msg = "Error al intentar guardar cruze de datos";
                        query = (@"
                        UPDATE tts_json  
                        SET 
                        refti = (select routing from routings where id_routing = " + id_routing + @"), 
                        id_routing = " + id_routing + @",
                        creado_json ='" + response.fecha + @"'
                        WHERE id_json = " + id_json);
                        comm.CommandText = query;
                        comm.ExecuteNonQuery();

                        comm.CommandText = "select refti from tts_json where id_json = " + id_json;
                        result = comm.ExecuteScalar();
                        response.refti = result != null ? result.ToString() : "-";

                        if (response.refti != "-" && response.refti != "")
                        {
                            tran.Commit();
                            response.msg = "Datos Correctos";
                            response.stat = 1;
                        }

                    }


                    if (response.stat == 0)
                    {
                        //almacenar el error json_file

                        if (id_json > 0)
                        {
                            comm.CommandText = "update tts_json set msg = '" + response.msg + "' where id_json = " + id_json;
                            comm.ExecuteNonQuery();
                        }

                        tran.Rollback();
                    }


                }
                catch (Exception e) // insert json
                {
                    response.msg += " : " + e.Message;
                    //almacenar el error json_file

                    if (id_json > 0)
                    {
                        comm.CommandText = "update tts_json set msg = '" + response.msg + "' where id_json = " + id_json;
                        comm.ExecuteNonQuery();
                    }

                    tran.Rollback();

                }

                json_response.Add(response);

            }  // foreach

        }
        catch (Exception e)
        {

            response.msg += " : " + e.Message;
            json_response.Add(response);

            //comm.Dispose();
            conn.Close();
            conn.ClearPool();
            conn.Dispose();

        }






        string texto = "";

        texto = Utils.SerializeJson(json_response);

        try
        {

            //comm.Dispose();
            conn.Close();
            conn.ClearPool();
            conn.Dispose();

        }
        catch (Exception e)
        {

        }

        return texto;
    }





    public static string BuiltInsertQuery<TEntity>(TEntity row)
    {
        string query = "INSERT INTO _table (";

        foreach (var prop in row.GetType().GetProperties())
        {
            query += prop.Name + ",";
        }

        query = query.TrimEnd(new char[] { ',' });

        query += ") VALUES (";

        foreach (var prop in row.GetType().GetProperties())
        {
            query += "'" + prop.GetValue(row, null) + "',";
        }
        query = query.TrimEnd(new char[] { ',' });

        return query + ")";
    }

    public static Struct.ArrParams EmpresaParametros(string pais, string sistema, string docid, string titulo, string edicion)
    {
        NpgsqlConnection conn;
        NpgsqlCommand comm;
        NpgsqlDataReader reader;

        Struct.ArrParams Params = new Struct.ArrParams();

        string tipo = "produccion";    

        try
        {
            string default_image = "iVBORw0KGgoAAAANSUhEUgAAAHMAAAB8CAYAAABaFY8zAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAABMSSURBVHhe7Z13rBRFHMcHsKNREXtvIFbEgoolMSKW2LAQDWCLLZKIYCwgSFSsKLZEwBK7EZVEo+gfioANjCUq9gYIighYURThuZ8f+z2H9e7d3Xs39+727TfZ3O3s7OzM/OZXZ3a2TUMElyEVaBv/ZkgBMmKmCBkxU4SMmClCRswUISNmihDENaHINm3axGcrzlsr/H4IjSCcWc0GZPgPQYMGf/31l5s0aVJ8lqFnz55ulVVWic8qj6BiFmJ26tTJrbnmmvGV1oF//vnnf0SbM2eOW7hwoVt99dXjlMojOGfusccebt1113V///23a9eunaUvW7bMftMItTGJuXPnuu+++y6oCgpKTEborrvumiNmx44d4yutBwsWLHDt27d3X331VTqIiZg99NBD3TXXXBNfaR2AS2+55RY3YcIE48xvv/22fnXmH3/84bp16+bWXntt16tXLzdy5Mg4R+vB0KFD3cSJE92iRYvczJkzg3JmUNekkP7IEAZBI0AZMauLIMSU5K43q5V6B9A6VUMWAUqAur/zzjtu8ODB7sADD3Tdu3c3vffll1/a9VomdiZmPUDIJ5980vXu3ds9//zzbsmSJSZdnnjiCXfEEUe4N954o6YHalBi1hs++ugjd8kll7gNN9zQLHANxg022MCOs846y/3www+WVouoG50pfRZSzN1///32CxE5VH/9/vjjj27q1Kn2vxZRNzqTMlUuwYgQRCVK06FDByNeciByvtVWW7kvvvgiTqk91KXOJGCNOCQoUQkkB0ahev/555/xv9pEUGImR3e5yMd9dOh5553nbr31VjdlypQ4tXkQx++7777ut99+s/8+QWnHGmusYTMfe+65Z5xaHohNh0bN60yVJdF68cUXu+nTp7sePXq4YcOGuZ9++smuC/kGQKno16+f/dLx6nyIutpqq1nAfP/997cYc1OeQRmhEVRnNrcB0pN0HgHq66+/3r388sumuxYvXux+//13d/fdd8e5V0DPbgq23HJLc0Oo97x586z8X375xXzMnXbayT3yyCNVIUpTEVTMVgJwJAS655573J133mkuAhwPx+A+MCsh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";
            Params.logo = System.Convert.FromBase64String(default_image);
            Params.logo2 = default_image;
            Params.error = "";

            conn = OpenPostgresConnection(tipo);
            comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            string pais_iso = "";

            try
            {
                int pais_id = int.Parse(pais);
                comm.CommandText = "SELECT pais_iso FROM empresas WHERE id_empresa = " + pais_id + " LIMIT 1";
                reader = comm.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("pais_iso")))
                        pais_iso = reader["pais_iso"].ToString();
                }
            }
            catch (Exception e)
            {
                pais_iso = pais;
            }



            /*
            string QuerySelect = "SELECT b.edicion, b.titulo, b.observaciones, c.descripcion FROM empresas_plantillas b " +
            "INNER JOIN empresas_plantillas_docs c ON b.doc_id = c.doc_id  " +
            "WHERE b.country = ( " +
            "SELECT country FROM ( " +
            "SELECT * FROM empresas_plantillas b WHERE CAST(b.activo AS text) = 'true' AND UPPER(b.country) = '" + pais_iso.ToUpper() + "' " +
            "UNION " +
            "SELECT * FROM empresas_plantillas b WHERE CAST(b.activo AS text) = 'true' AND UPPER(substring('" + pais_iso + "',3,3)) = 'TLA' AND UPPER(b.country) = 'GTTLA' " +
            "UNION " +
            "SELECT * FROM empresas_plantillas b WHERE CAST(b.activo AS text) = 'true' AND UPPER(substring('" + pais_iso + "',3,3)) = 'LTF' AND UPPER(b.country) = 'GTLTF' " +
            "UNION " +
            "SELECT * FROM empresas_plantillas b WHERE CAST(b.activo AS text) = 'true' AND length('" + pais_iso + "') = 2 AND '" + pais_iso.ToUpper() + "' <> 'N1' AND UPPER(b.country) = 'GT' " +
            ") y LIMIT 1) " +
            " AND CAST(b.activo AS text) = 'true' AND b.doc_id = '" + docid + "' AND LOWER(b.sistema) = '" + sistema.ToLower() + "' ";
            */

            Params.titulo = "";
            Params.edicion = "";
            Params.observaciones = "";
            Params.descripcion = "";

            string QuerySelect = @"SELECT b.edicion, b.titulo, b.observaciones, c.descripcion 

            FROM empresas_plantillas b 

            INNER JOIN empresas_plantillas_docs c ON b.doc_id = c.doc_id  

            WHERE UPPER(b.country) =  '" + pais_iso.ToUpper() + "' AND b.activo = true AND b.doc_id = '" + docid + "' AND LOWER(b.sistema) = '" + sistema.ToLower().Trim() + "' ";

            comm.CommandText = QuerySelect;
            reader = comm.ExecuteReader();
            while (reader.Read())
            {

                if (!reader.IsDBNull(reader.GetOrdinal("titulo")))
                    Params.titulo = reader["titulo"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("edicion")))
                    Params.edicion = reader["edicion"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("observaciones")))
                    Params.observaciones = reader["observaciones"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("descripcion")))
                    Params.descripcion = reader["descripcion"].ToString();

            }

            /*
            QuerySelect = "SELECT b.descripcion as nombre_pais, y.* FROM ( " +
            "SELECT * FROM ( " +
            "SELECT 1 as n, * FROM empresas_parametros a WHERE CAST(a.activo AS text) = 'true' AND LOWER(a.country) = '" + pais_iso.ToLower() + "' " +
            "UNION " +
            "SELECT 2 as n, * FROM empresas_parametros a WHERE CAST(a.activo AS text) = 'true' AND UPPER(substring('" + pais_iso + "',3,3)) = 'TLA' AND UPPER(a.country) = 'GTTLA' " +
            "UNION " +
            "SELECT 3 as n, * FROM empresas_parametros a WHERE CAST(a.activo AS text) = 'true' AND UPPER(substring('" + pais_iso + "',3,3)) = 'LTF' AND UPPER(a.country) = 'GTLTF' " +
            "UNION " +
            "SELECT 4 as n, * FROM empresas_parametros a WHERE CAST(a.activo AS text) = 'true' AND length('" + pais_iso + "') = 2 AND '" + pais_iso.ToUpper() + "' <> 'N1' AND a.country = 'GT' " +
            ") x ) y " +  //") x LIMIT 1 ) y " +
            "LEFT JOIN paises ON codigo = substr(y.country,1,2) AND LOWER(y.country) = '" + pais_iso.ToLower() + "' ORDER BY n LIMIT 1";
            */

            QuerySelect = @"SELECT COALESCE(b.descripcion,'') as nombre_pais, a.* 

            FROM 

            empresas_parametros a 

            LEFT JOIN paises b ON b.codigo = substr(a.country,1,2) 

            WHERE a.activo = true AND UPPER(a.country) = '" + pais_iso.ToUpper() + @"' 

            LIMIT 1";



            comm.CommandText = QuerySelect;
            reader = comm.ExecuteReader();
            while (reader.Read())
            {

                if (!reader.IsDBNull(reader.GetOrdinal("country")))
                    Params.country = reader["country"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("logo")))
                    if (reader["logo"].ToString() != "")
                    {
                        Params.logo = System.Convert.FromBase64String(reader["logo"].ToString());
                        Params.logo2 = reader["logo"].ToString();
                    }

                if (!reader.IsDBNull(reader.GetOrdinal("nombre_empresa")))
                    Params.nombre_empresa = reader["nombre_empresa"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("nombre_pais")))
                    Params.nombre_pais = reader["nombre_pais"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("nit")))
                    Params.nit = reader["nit"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("direccion")))
                    Params.direccion = reader["direccion"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("telefonos")))
                    Params.telefonos = reader["telefonos"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("home_page")))
                    Params.home_page = reader["home_page"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("firma")))
                    Params.firma = reader["firma"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("fact_elect_codigo")))
                    Params.fact_elect_codigo = reader["fact_elect_codigo"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("fact_elect_user")))
                    Params.fact_elect_user = reader["fact_elect_user"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("fact_elect_pass")))
                    Params.fact_elect_pass = reader["fact_elect_pass"].ToString();

                int col = reader.GetOrdinal("trackactivo");
                if (!reader.IsDBNull(col))
                    Params.trackactivo = reader.GetBoolean(col);// ["trackactivo"];

                col = reader.GetOrdinal("trackpuerto");
                if (!reader.IsDBNull(col))
                    Params.trackpuerto = reader.GetInt32(col);

                if (!reader.IsDBNull(reader.GetOrdinal("trackmailserver")))
                    Params.trackmailserver = reader["trackmailserver"].ToString();

                col = reader.GetOrdinal("trackauth");
                if (!reader.IsDBNull(col))
                    Params.trackauth = reader.GetInt32(col);

                if (!reader.IsDBNull(reader.GetOrdinal("trackfromaddress")))
                    Params.trackfromaddress = reader["trackfromaddress"].ToString();

                if (!reader.IsDBNull(reader.GetOrdinal("trackpassword")))
                    Params.trackpassword = reader["trackpassword"].ToString();
            }

            CloseObj(reader, comm, conn);

            if (Params.titulo != null)
                if (Params.titulo.Trim() == "") Params.titulo = titulo.Trim();
            if (Params.edicion != null)
                if (Params.edicion.Trim() == "") Params.edicion = edicion.Trim();
            if (Params.titulo != null)
                if (Params.titulo.Trim() == "") Params.titulo = "Configurar titulo " + pais_iso.ToUpper() + " [" + docid + "]";

            //sistema.ToLower().Trim() 


        }
        catch (Exception e)
        {
            Params.error = Params.country + " " + e.Message;
        }
        return Params;
    }




    public static List<string> ContactosPaisCostos(string empresa, string tipo, ref string result)
    {
        NpgsqlConnection conn;
        NpgsqlCommand comm;
        NpgsqlDataReader reader = null;

        conn = OpenPostgresConnection(tipo);
        comm = new NpgsqlCommand();
        comm.CommandType = CommandType.Text;
        comm.Connection = conn;

        List<string> emails = new List<string>();
        try
        {
            conn = OpenPostgresConnection(tipo);

            string query = "", to = "", cc = "", bc = ""; ;

            query = @"

SELECT (pw_name||'@'||dominio) as email, pw_gecos as name, CAST(a.correo_cc_bc as text) as correo_cc_bc

FROM usuarios_empresas_exactus_correo a

INNER JOIN usuarios_empresas b ON a.id_usuario = b.id_usuario

WHERE a.activo = true AND a.pais_iso = '" + empresa + "'";

            comm.CommandText = query;
            reader = comm.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(reader.GetOrdinal("correo_cc_bc")))
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("email")))
                    {
                        switch (reader["correo_cc_bc"].ToString())
                        {
                            case "1":
                                if (to != "")
                                    to += ";";
                                to += reader["name"].ToString() + " <" + reader["email"].ToString() + ">";

                                break;

                            case "2":
                                if (cc != "")
                                    cc += ";";
                                cc += reader["name"].ToString() + " <" + reader["email"].ToString() + ">";

                                break;

                            case "3":
                                if (bc != "")
                                    bc += ";";
                                bc += reader["name"].ToString() + " <" + reader["email"].ToString() + ">";

                                break;
                        }
                    }
                }
            }

            CloseObj(reader, comm, conn);

            emails.Add(to);
            emails.Add(cc);
            emails.Add(bc);
        }
        catch (Exception e)
        {
            result = e.Message;
            conn.Close();
        }

        comm.Dispose();
        conn.ClearPool();
        conn.Dispose();

        return emails;
    }

	
	



    #region GetArrayPostgres

    public static IEnumerable<Dictionary<string, object>> GetArrayPostgres(string tipo, string query)
    {
        IEnumerable<Dictionary<string, object>> rows = null; // new IEnumerable<Dictionary<string, object>>();

        try
        {
            NpgsqlConnection conn;
            NpgsqlCommand comm;
            NpgsqlDataReader reader = null;

            conn = OpenPostgresConnection(tipo);
            comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            comm.CommandText = query;
            reader = comm.ExecuteReader();
            rows = Serialize(reader);

            CloseObj(reader, comm, conn);
        }
        catch (Exception e)
        {
            throw e;

        }

        return rows;
    }

    #endregion






    public static string GetScalar(string sqlString, string tipo)
    {
        string valor = "";

        try
        {
            NpgsqlConnection conn = OpenPostgresConnection(tipo);
            NpgsqlCommand comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            comm.CommandText = sqlString;

            valor = (string)comm.ExecuteScalar();

        }
        catch (Exception ex)
        {
            valor = ex.Message;
        }

        return valor;
    }



    #region get_clientes_data

    public static Struct.clientes_data get_clientes_data(Struct.bl_data tb_bl, string tipo)
    {
        Struct.clientes_data data = new Struct.clientes_data();

        try
        {

            string query = "SELECT id_cliente, nombre_cliente FROM clientes WHERE id_cliente IN (" + tb_bl.ConsignerID + "," + tb_bl.ShipperID + "," + tb_bl.id_coloader + "," + tb_bl.id_cliente_order + ")";

            int id;
            string names;

            NpgsqlConnection conn;
            NpgsqlCommand comm;
            NpgsqlDataReader reader = null;

            conn = OpenPostgresConnection(tipo);
            comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            comm.CommandText = query;
            reader = comm.ExecuteReader();

            if (reader.HasRows)
            {

                while (reader.Read())
                {
                    id = 0; names = "";

                    if (!reader.IsDBNull(0))
                        id = int.Parse(reader["id_cliente"].ToString());

                    if (!reader.IsDBNull(1))
                        names = reader["nombre_cliente"].ToString();

                    if (tb_bl.ConsignerID == id)
                        data.ConsignerName = names;

                    if (tb_bl.ShipperID == id)
                        data.ShipperName = names;

                    if (tb_bl.id_coloader == id)
                        data.ColoaderName = names;

                    if (tb_bl.id_cliente_order == id)
                        data.NotifyName = names;
                }
            }

            CloseObj(reader, comm, conn);
        }
        catch (Exception e)
        {
            throw e;
        }

        return data;
    }

    #endregion

    public static int EjecutaQuery(string sqlString, string tipo)
    {
        int result = 0;

        try
        {
            NpgsqlConnection conn = OpenPostgresConnection(tipo);
            NpgsqlCommand comm = new NpgsqlCommand();
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


    public static  System.Data.IDataReader GetDataReader(string sqlString, string tipo)
    {        
        NpgsqlConnection conn = OpenPostgresConnection(tipo);
        NpgsqlCommand comm = new NpgsqlCommand();
        comm.CommandType = CommandType.Text;
        comm.Connection = conn;
        comm.CommandText = sqlString;
        NpgsqlDataReader reader = comm.ExecuteReader();
        return reader;
    }

    public static IEnumerable<Dictionary<string, object>> Serialize(NpgsqlDataReader reader)
    {
        var results = new List<Dictionary<string, object>>();
        var cols = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            cols.Add(reader.GetName(i));

        while (reader.Read())
            results.Add(SerializeRow(cols, reader));

        return results;
    }

    private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, NpgsqlDataReader reader)
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


     public static TEntity GetRowPostgres<TEntity>(string tipo, string qry) where TEntity : class, new()
    {
        TEntity data = new TEntity();

        NpgsqlConnection conn = OpenPostgresConnection(tipo);
        NpgsqlCommand comm = new NpgsqlCommand();
        comm.CommandType = CommandType.Text;
        comm.Connection = conn;
        comm.CommandText = qry;
        NpgsqlDataReader reader = comm.ExecuteReader();

        if (reader.HasRows)
        {
            reader.Read();
            data = Utils.ReflectType<TEntity>(reader);
        }

        CloseObj(reader, comm, conn);

        return data;
    }



    public static void log(int user_id, string user_name, string sistema, string db, string accion, string before_txt, string after_txt, string ip, string tabla)
    {        
        try
        {
            NpgsqlConnection conn = OpenPostgresConnection("produccion");
            NpgsqlCommand comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            string sql = "INSERT INTO usuarios_empresas_log (user_id, user_name, sistema, db, accion, before_txt, after_txt, ip, tabla, tiempo) VALUES ( " +
            "'" + user_id + "', '" + user_name + "', '" + sistema + "', '" + db + "', '" + accion + "', '" + before_txt + "', '" + after_txt + "', '" + ip + "', '" + tabla + "', now() )";

            comm.CommandText = sql;
            comm.ExecuteNonQuery();

            comm.Dispose();
            conn.Close();
            conn.ClearPool();
            conn.Dispose();
        }
        catch (Exception e)
        {
            throw e;
        }
    }


   

    public static NpgsqlConnection OpenPostgresConnection(string tipo)
    {
        NpgsqlConnection conn = null;
        try
        {

            string db = "";

            switch (tipo)
            {
                case "produccion":
                case "master-aimar":
                    db = "master";
                    break;

                case "bk":
                    db = "bk_master";
                    break;

                case "pruebas_":
                    db = "pruebas_master";
                    break;

                case "local":
                    db = "local_master";
                    break;
            }

            db = "pruebas_master";

            string strconn = "";


            if (tipo.Length > 6)
            {

                if (tipo.Substring(0, 7) == "ventas_")
                {
                    db = "ventas";

                    strconn = System.Configuration.ConfigurationManager.ConnectionStrings[db].ConnectionString;

                    strconn += "" + tipo + ";";

                } else {
            
                    strconn = System.Configuration.ConfigurationManager.ConnectionStrings[db].ConnectionString;
            
                }

            }
            else {

                strconn = System.Configuration.ConfigurationManager.ConnectionStrings[db].ConnectionString;
            
            }
            
            

            conn = new NpgsqlConnection(strconn);
            conn.Open();

        }
        catch (Exception e)
        {
            throw e;
        }
        return conn;
    }





    public static void CloseObj(NpgsqlDataReader rd, NpgsqlCommand comm, NpgsqlConnection conn)
    {
        try
        {
            rd.Close();
            comm.Dispose();
            conn.Close();
            conn.ClearPool();
            conn.Dispose();
        }
        catch (Exception e)
        {
            throw e;
        }
    }



    /*
    public static int ExecuteQuery(string query) {

        NpgsqlConnection conn;
        NpgsqlCommand comm;

        try
        {
            conn = OpenPostgresConnection("");
            comm = new NpgsqlCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;

            string sql = "INSERT INTO usuarios_empresas_log (user_id, user_name, sistema, db, accion, before_txt, after_txt, ip, tabla, tiempo) VALUES ( " +

            "'" + user_id + "', '" + user_name + "', '" + sistema + "', '" + db + "', '" + accion + "', '" + before_txt + "', '" + after_txt + "', '" + ip + "', '" + tabla + "', now() )";

            comm.CommandText = sql;
            comm.ExecuteNonQuery();

 
            comm.Dispose();
            conn.Close();
            conn.ClearPool();
            conn.Dispose();

        }
        catch (Exception e)
        {

            throw e;

        }
    }
    */


    /*
public static NpgsqlConnection OpenVentasConnection(string schema)
{
    NpgsqlConnection conn = null;
    try
    {
        conn = new NpgsqlConnection("Server=10.10.1.20;Port=5432;User Id=dbmaster;Password=aimargt;Database=" + schema + ";POOLING=True;MINPOOLSIZE=2;MAXPOOLSIZE=1000");
        //conn = new NpgsqlConnection("Server=172.16.0.191;Port=5432;User Id=dbmaster;Password=aimargt;Database=" + schema + ";POOLING=True;MINPOOLSIZE=2;MAXPOOLSIZE=1000");
        // conn = new NpgsqlConnection("Server=localhost;Port=5432;User Id=dbmaster;Password=123456789;Database=" + schema + ";POOLING=True;MINPOOLSIZE=2;MAXPOOLSIZE=1000");
        conn.Open();
    }
    catch (Exception e)
    {
        throw e;

    }
    return conn;
}
*/

    /*
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DataFieldAttribute : Attribute
    {
        private readonly string _name;

        public DataFieldAttribute(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }
    */

}