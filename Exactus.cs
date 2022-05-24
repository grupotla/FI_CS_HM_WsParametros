using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace WsParametros
{
    public class Exactus
    {
        public Exactus()
        {
        }

        public static string dbStr = "bk";
       
        //ESTILO DISPLAY MENSAJES
        public static string FRow1 = @"<div style=""display:block"">";
        public static string FRow2 = "</div>";

        public static string FReg1 = @"<div style=""font: 15px Arial, sans-serif;background-color:WHITE;color:NAVY;display:inline"">";
        public static string FReg2 = "</div>";

        public static string FBold1 = @"<div style=""font: 15px Arial, sans-serif;background-color:SILVER;color:NAVY;display:inline"">";
        public static string FBold2 = "</div>";



        #region pedidos - MAIN PROGRAM

        public static Struct.arg_pedidos pedidos(Struct.arg_pedidos tb_arg)
        {
            string res = "0", mensajes = "", errores = "";

            tb_arg.stat = 120;
            tb_arg.msg = "";

            try
            {
                /*
                switch (tb_arg.PRODUCTO)
                {
                    case "1": tb_arg.PRODUCTO = "aereo"; break;
                    case "2": tb_arg.PRODUCTO = "terrestre"; break;
                    case "3": tb_arg.PRODUCTO = "maritimo"; break;
                    case "4": tb_arg.PRODUCTO = "preembarque"; break;
                    case "5": tb_arg.PRODUCTO = "aduana"; break;
                }
                */

                //mensajes = Postgres_.GetScalar("select CAST(CURRENT_DATE as text)", "ventas_cr");

                List<_PEDIDO> pedidos_obj = new List<_PEDIDO>();

                _PEDIDO pedido_obj = new _PEDIDO();

                string pedido_id = "0";

                int C = 0; string single_resp = "";
                    
                List<string> charges = new List<string>(); 

                //errores = "|BuildHeaderConf"; no debe salir este mensaje en pantalla para el user 2021-08-11

                errores = "";

                _PARAMETROS data_conf = new _PARAMETROS();

                ///////////////////////////////// BUILT PEDIDOS /////////////////////////////////////////////////////////////////////////////

                List<string> pedidos = BuildHeaderConf(ref tb_arg, "PEDIDO", ref pedido_id, ref data_conf, ref charges);

                if (pedidos != null && pedidos.Count > 0 && int.Parse(pedido_id) > 0)
                {
                    //List<_PEDIDO> pedidos = GetDocumentos(); //  genera query pruebas

                    errores += "|TraeTextos";

                    foreach (string texto in pedidos)
                    {
                        pedido_obj = JsonConvert.DeserializeObject<_PEDIDO>(texto);
                        
                        pedidos_obj.Add(pedido_obj);
                    }

                    errores += "|ConvertResult";

                    ///////////////////// SendPedido /////////////////////////////////////////////////////////////////////////////////////////

                    var responseObject = SendPedido(pedidos_obj, null, int.Parse (data_conf.id_pais), data_conf);

                    List<Struct._RESPUESTA> results_objets = new List<Struct._RESPUESTA>();

                    single_resp = Newtonsoft.Json.JsonConvert.SerializeObject(responseObject.Result).ToString();

                    try
                    {
                        results_objets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Struct._RESPUESTA>>(single_resp);

                    }
                    catch (Exception e)
                    {
                        errores += "|" + e.Message;
                    }

                    errores += "|SendPedido";

                    Struct._RESPUESTA result_obj = new Struct._RESPUESTA();

                    errores += "|CicloTextos";

                    tb_arg.stat = 119;


                    string query;

                    foreach (string texto in pedidos)
                    {

                        string charge = charges[C];
                        
                        
                        errores += "|C=" + C;

                        result_obj = results_objets[C];

                        errores += "|pedido_obj";

                        pedido_obj = JsonConvert.DeserializeObject<_PEDIDO>(texto);

                        errores += "|result_obj.PEDIDO=" + pedido_obj.PEDIDO;

                        result_obj.PEDIDO = pedido_obj.PEDIDO;

                        if (result_obj.PEDIDO_EXACTUS == null) result_obj.PEDIDO_EXACTUS = "PENDIENTE";

                        if (result_obj.CODIGO_ERROR != null) //2022-03-22
                        tb_arg.stat = int.Parse(result_obj.CODIGO_ERROR);

                        result_obj.PEDIDO_RESP = "";


                        tb_arg.pedido_erp = result_obj.PEDIDO_EXACTUS;

                        ///////////////////// VALIDA RESPUESTA /////////////////////////////////////////////////////////////////////////////////////////
                        
                        string msg = FRow1;

                        if (result_obj.CODIGO_ERROR == "99")
                        {
                            tb_arg.stat = 0;
                            msg += FReg1 + result_obj.PEDIDO.Split('|')[2] + " " + pedido_obj.MONEDA + " " + FReg2 + " " + FBold1 + result_obj.PEDIDO_EXACTUS + FBold2 + " " + FReg1 + result_obj.MENSAJE;
                        }
                        else
                        {
                            tb_arg.stat = 118;
                            msg += FBold1 + result_obj.PEDIDO.Replace("|", "&#124;") + " " + pedido_obj.MONEDA + " " + FBold2 + " " + FReg1 + result_obj.MENSAJE + FReg2;
                        }

                        switch(result_obj.CODIGO_ERROR) {
                            case "99":


                                if (tb_arg.abierto == "")
                                { // solo los actualiza cuando es pedido cerrado



                                    query = "SELECT COALESCE(array_to_string(array_agg(id_pedido ), ','),'') FROM exactus_pedidos WHERE pedido_erp = '" + result_obj.PEDIDO_EXACTUS + "' AND estado IN (3)";
                                
                                    string pedidos_str = Postgres_.GetScalar(query, dbStr);

                                    if (pedidos_str != null && pedidos_str != "")
                                    {

                                        /////////////////////// cancela pedidos anterioes  //////////////////////////////////////////////

                                        query = "UPDATE exactus_pedidos SET estado = 5 WHERE id_pedido IN (" + pedidos_str + ") AND estado IN (3)";

                                        res = Postgres_.EjecutaQuery(query, dbStr).ToString();


                                        switch (tb_arg.PRODUCTO)
                                        {
                                            case "1":
                                                query = "UPDATE ChargeItems SET InvoiceID = 0, DocType = 0  WHERE InvoiceID IN (" + pedidos_str + ") AND DocType = 9 AND DocTyp = " + (tb_arg.IMPEX == "IMPORT" ? "1" : "0");
                                                MySql_.EjecutaQuery(query, "aereo");
                                                break;

                                            case "2":
                                                query = "UPDATE ChargeItems SET InvoiceID = 0, DocType = 0  WHERE InvoiceID IN (" + pedidos_str + ") AND DocType = 9";
                                                MySql_.EjecutaQuery(query, "terrestre");
                                                break;
                                        }
                                    }

                                    tb_arg.stat = UpdateRubrosOK(ref tb_arg, pedido_obj.PEDIDO, charge); //envia un single pedido
                                
                                }

                                result_obj.PEDIDO_RESP = tb_arg.msg;

                                msg += tb_arg.msg + FReg2 + FRow2;
                                break;

                            case "101": //EL CLIENTE NO ESTA HOMOLOGADO

                                if (data_conf.id_cliente.IndexOf('|') + 1 < 1)
                                    data_conf.id_cliente = "|" + data_conf.id_cliente;

                                msg += " " + FBold1 + data_conf.id_cliente.Split('|')[1] + FBold2 + " " + FReg1 + data_conf.nombre_cliente + FReg2 + FRow2;
 
                                break;

                            case "102": //EL PAIS NO ESTA HOMOLOGADO

                                if ( pedido_obj.PAIS.IndexOf('|')+1 < 1) 
                                    pedido_obj.PAIS = "|" + pedido_obj.PAIS;

                                msg += " " + FBold1 + pedido_obj.PAIS.Split('|')[1] + FBold2 + " " + FRow2;

                                break;

                            case "103": //LA MONEDA NO ESTA HOMOLOGADA

                                if (pedido_obj.MONEDA.IndexOf('|') + 1 < 1)
                                    pedido_obj.MONEDA = "|" + pedido_obj.PAIS;

                                msg += " " + FBold1 + pedido_obj.MONEDA.Split('|')[1] + FBold2 + " " + FRow2;
                                break;

                            case "104": //LA CONDICION CONDICION_PAGO NO EXISTE PARA EL CLIENTE
                                msg += " " + FBold1 + pedido_obj.CONDICION_PAGO + FBold2 + " " + FReg1 + pedido_obj.CLIENTE.Split('|')[1] + " " + data_conf.nombre_cliente + FReg2 + FRow2;
                                break;

                            case "106": //EL ARTICULO NO ESTA HOMOLOGADO
                                msg = FReg1 + msg.Replace("09|", "") + FReg2 + FRow2;
                                break;
                            
                            case "113": //EL CODIGO DE CONSECUTIVO NO ESTA HOMOLOGADO
                                msg += FRow2;
                                break;

                            default:
                                if (result_obj.CODIGO_ERROR != null)
                                    msg += " " + FBold1 + result_obj.CODIGO_ERROR + FBold2 + " " + FRow2;
                                break;
                        }

                        single_resp = JsonConvert.SerializeObject(result_obj);

                        errores += "|single_resp=" + single_resp;


                        /////////////////////// almacena json pedido y respuesta //////////////////////////////////////////////
                        query = "UPDATE exactus_pedidos SET estado = " + (result_obj.ESTADO == "ERROR" ? 2 : 3) + ", json_cargo_system = '[" + texto + "]', json_exactus = '" + single_resp + "', pedido = '" + msg + "', codigo_consecutivo = '" + pedido_obj.PEDIDO + "', pedido_erp='" + result_obj.PEDIDO_EXACTUS + "', esquema = '" + data_conf.esquema + "' WHERE id_pedido = " + pedido_obj.PEDIDO.Split('|')[2];

                        errores += "|query=" + query;

                        res = Postgres_.EjecutaQuery(query, dbStr).ToString();

                        errores += "|Postgres_.EjecutaQuery=" + res;

                        //results_objets.Add(result_obj);

                        C++;
                     
                        if (result_obj.CODIGO_ERROR != "99")
                        {
                            msg = msg.Replace("NAVY", "RED");
                        }

                        if (mensajes != "") mensajes += "|";
                        mensajes += msg;

                    }


                    //////////////////////////////////// no se requiere retornar todo el json de la respuesta, solo los mensajes clave

                    errores = JsonConvert.SerializeObject(results_objets);

                    errores = ""; //limpia debug si finalizo la tarea

                    //string pedidos_ = JsonConvert.SerializeObject(pedidos);
                    // works pero solo a produccion
                    //Postgres_.log(0, tb_arg.user, tb_arg.PRODUCTO, "exactus", "log", tb_arg.msg, pedidos_, tb_arg.ip, "exactus_pedidos");

                }

            }
            catch (Exception ex)
            {
                tb_arg.stat = 9;
                tb_arg.msg += "|" + ex.Message;
            }


            if (tb_arg.msg != "")
            {
                if (mensajes != "") mensajes += "|";
                mensajes += tb_arg.msg;
            }

            if (errores != "")
            {
                if (mensajes != "") mensajes += "|";
                mensajes += errores;
            }

            tb_arg.msg = mensajes;

            if (tb_arg.pedido_erp == null) tb_arg.pedido_erp = "";

            return tb_arg;
        }

        #endregion





        #region UpdateRubrosOK

        public static int UpdateRubrosOK(ref Struct.arg_pedidos tb_arg, string pedido, string charge)
        {
            tb_arg.stat = -1;
            tb_arg.msg = "";

            int res = 0;

            string query = "", producto = "";

            List<string> ArrayQ = new List<string>();

       
            
                string[] rows = charge.Split('|');

                try
                {
                    //foreach (_PEDIDO_LINEA line in pedido.LINEAS)

                    foreach (string chargeid in rows)
                    {
                        if (chargeid.Trim() != "")
                        {

                            res++;
                            switch (tb_arg.PRODUCTO)
                            {
                                case "1": // aereo
                                    producto = "aereo";
                                    query = "UPDATE ChargeItems SET InvoiceID = " + pedido.Split('|')[2] + ", DocType = 9 WHERE ChargeID = " + chargeid + " AND AWBID = " + tb_arg.BL_ID + "  AND Expired = 0 AND DocTyp = " + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + ";";
                                    //MySql_.EjecutaQuery(query, "aereo");
                                    break;
                                case "2": // terrestre
                                    producto = "terrestre";
                                    query = "UPDATE ChargeItems SET InvoiceID = " + pedido.Split('|')[2] + ", DocType = 9 WHERE ChargeID = " + chargeid + " AND SBLID = " + tb_arg.BL_ID + " AND Expired = 0;";
                                    //MySql_.EjecutaQuery(query, "terrestre");
                                    break;
                            }
                            ArrayQ.Add(query);
                        }
                    }

                    res = MySql_.EjecutaQueryArray(ArrayQ, producto);

                    tb_arg.stat = res;

                } catch(Exception ex) {
                    tb_arg.stat = -2;
                    tb_arg.msg = ex.Message;
                }

                if (tb_arg.stat <= 0 && tb_arg.msg == "") {
                    tb_arg.msg = " - Problemas al bloquear rubros cargo system, verifique " + charge;
                }

            

            return res;
        }


        #endregion

        #region Build Header Conf

        public static List<string> BuildHeaderConf(ref Struct.arg_pedidos tb_arg, string documento, ref string pedido_id, ref _PARAMETROS data_conf, ref List<string> charges)
        {
            string query = "", producto = "";

            tb_arg.stat = 1;
            tb_arg.msg = "";




            if (tb_arg.PRODUCTO == "2")
            { //terrestre

                if (tb_arg.IMPEX == null)
                {
                    /* se espera que ya no entre aca 2021-12-09
                    //////////////////////////////////////////////////// IMPORT / EXPORT 2021-08-05

                    query = @"select 'EXPORT' as tipo from BLDetail where BLDetailID = " + tb_arg.BL_ID + @" and Countries in ('" + tb_arg.COUNTRIES + @"') and CountriesFinalDes not in ('" + tb_arg.COUNTRIES + @"') and BLType in (0,1) 
union 

select 'IMPORT' as tipo from BLDetail where BLDetailID = " + tb_arg.BL_ID + @" and Intransit IN (" + (tb_arg.COUNTRIES.Substring(0, 2) == "NI" ? "1,2" : "2") + @") and CountriesFinalDes in ('" + tb_arg.COUNTRIES + @"') and BLType in (0,1) 

union 

select 'IMPORT' as tipo from BLDetail where BLDetailID = " + tb_arg.BL_ID + @" and Countries in ('" + tb_arg.COUNTRIES + @"') and CountriesFinalDes in ('" + tb_arg.COUNTRIES + @"') and BLType in (2)

";
                    tb_arg.IMPEX = MySql_.GetScalar(query, producto);
                    */
                }


                if (tb_arg.IMPEX == null)
                {
                    //tb_arg.msg = " " + tb_arg.BL_ID + " - " + (pedido.RUBRO4 == "" ? pedido.RUBRO2 : pedido.RUBRO4) + " : No se puede facturar esta carga en oficina actual " + tb_arg.COUNTRIES + ".";
                    tb_arg.msg = " " + tb_arg.BL_ID + " : No se puede facturar esta carga en oficina actual " + tb_arg.COUNTRIES + ".";
                    
                    tb_arg.stat = 2;
                    return null;
                }

            }




            List<_CONFIG> data = BuildQuery(tb_arg, documento, ref query);

            switch (tb_arg.PRODUCTO)
            {
                case "1": // aereo
                    //CreatedDate
                    query += " FROM Awb" + (tb_arg.IMPEX == "IMPORT" ? "i" : "") + " WHERE AwbID = " + tb_arg.BL_ID;
                    producto = "aereo";
                    break;

                case "2": // terrestre
                    query += " FROM BLDetail WHERE BLDetailID = " + tb_arg.BL_ID;
                    producto = "terrestre";
                    break;
            }


            _PEDIDO pedido = MySql_.GetRowMysql<_PEDIDO>(producto, query);



            if (tb_arg.PRODUCTO == "2") { //2021-10-21 esto debido a los cambios en terrestre para la validacion de paises oficinas ya sea origen o destino

                pedido.EMPRESA = tb_arg.COUNTRIES;

            }



            //////////////////////////////////   DATA CONF

            ///  SE DESCARTAN LAS TABLAS exactus_cargo_system & servicios_combinaciones

            string tla_medio_transporte = "0";  //1 : aereo  2 : terrestre

            if (tb_arg.PRODUCTO == "1") tla_medio_transporte = "3";
            if (tb_arg.PRODUCTO == "2") tla_medio_transporte = "2";

            // COALESCE(d.id_pais,'" + pedido.EMPRESA + @"') as id_pais, 

            query = @"SELECT DISTINCT exactus_url, exactus_url_username, exactus_url_password, exactus_usuario, c.id as id_pais, c.exactus_schema as esquema,

SUBSTR(UPPER(a.descripcion),1,2) as cs_producto, UPPER(a.letra) as cs_sub_producto, 

CASE WHEN COALESCE(f.id_facturar,0) > 0 THEN '01' ELSE '" + (tb_arg.IMPEX == "IMPORT" ? pedido.CLIENTE.Split('|')[0] : pedido.U_CLIENTE_TI.Split('|')[0]) + @"' END || '|' ||

d.id_cliente AS id_cliente, d.nombre_cliente, 

COALESCE(e.tca_tcambio,0) as tca_tcambio, COALESCE(f.routing,'') as routing_no, COALESCE(f.fecha,CURRENT_DATE) as routing_fecha, COALESCE(f.id_facturar,0) as id_facturar, 

COALESCE(g.name,'') as transportista, COALESCE(h.nombre_cliente,'') as U_AGENTE_TI, a.u_servicio_ti as U_SERVICIO_TI,

i.id_pedido, COALESCE(i.pedido_erp ,'') as pedido_erp, COALESCE(i.estado,0) as estado, COALESCE(i.codigo_consecutivo,'') as codigo_consecutivo, COALESCE(to_char(i.pedido_fecha, 'DD/MM/YYYY HH24:MI:SS'),'') as pedido_fecha 

FROM transporte a

INNER JOIN empresas_parametros c ON c.country = '" + tb_arg.COUNTRIES + @"'

LEFT JOIN routings f ON id_routing = '" + pedido.RUBRO5.Trim() + @"'

LEFT JOIN clientes d ON d.id_cliente = CASE WHEN COALESCE(f.id_facturar,0) > 0 THEN COALESCE(f.id_facturar,0) ELSE " + (tb_arg.IMPEX == "IMPORT" ? pedido.CLIENTE.Split('|')[1] : pedido.U_CLIENTE_TI.Split('|')[1]) + @" END 

LEFT JOIN v_baw_tipocambio e ON e.pai_iso = '" + pedido.EMPRESA + @"' AND e.tca_fecha = '" + pedido.FECHA_HORA + @"'

LEFT JOIN carriers g ON g.carrier_id = '" + (tb_arg.PRODUCTO == "1" ? pedido.U_TRANSPORTISTA_TI : "0") + @"'

LEFT JOIN clientes h ON h.id_cliente = '" + (tb_arg.PRODUCTO == "1" ? pedido.U_AGENTE_TI : "0") + @"' 

LEFT JOIN exactus_pedidos i ON  i.documento = '" + pedido.COMENTARIO_CXC + @"' AND i.id_documento = '" + tb_arg.BL_ID + @"' AND i.id_cargo_system = " + tb_arg.PRODUCTO + @" AND estado = 3 

WHERE a.tla_medio_transporte = " + tla_medio_transporte + @" AND UPPER(a.letra) = '" + pedido.U_SERVICIO_TI.ToUpper() + @"'
            
ORDER BY i.id_pedido DESC LIMIT 1
";

/// tipo cambio CS debe leerse por pais_id, pais_iso SV se repite en sv sv2

            //U_TRANSPORTISTA_TI        aereo : CarrierID      terrestre : Shippers
             
            data_conf = Postgres_.GetRowPostgres<_PARAMETROS>(dbStr, query);

            //pedido.COMENTARIO_CXC = data_conf.routing_no; 2021-09-13 ahora viaja en RUBRO5 por si se necesita el dato de routing 

            pedido.U_SERVICIO_TI = data_conf.U_SERVICIO_TI.ToUpper().Trim(); //debe venir nueva columna dato de 3 caracteres

            //pedido.FECHA_PEDIDO = data_conf.routing_fecha.Substring(0,10);


            pedido.PEDIDO_ERP = data_conf.pedido_erp; //2021-11-25

            if (data_conf.id_cliente == null || data_conf.id_cliente == "")
            {
                tb_arg.msg = "Cliente " + pedido.CLIENTE.Split('|')[1] + " no fue encontrado en db actual";
                tb_arg.stat = 2;
                return null;
            }

            if (data_conf.exactus_url == "" || data_conf.exactus_url == null)
            {               
                tb_arg.stat = 3;
                tb_arg.msg = "Configuracion Url sin valor";
                return null;
            }

            if (data_conf.exactus_url_username == "" || data_conf.exactus_url_username == null)
            {
                tb_arg.stat = 4;
                tb_arg.msg = "Configuracion Usuario (credenciales) sin valor";
                return null;
            }

            if (data_conf.exactus_url_password == "" || data_conf.exactus_url_password == null)
            {
                tb_arg.stat = 5;
                tb_arg.msg = "Configuracion Password (credenciales) sin valor";
                return null;
            }

            #region LEER ROUTINGS ANTIGUO METODO
            /////////////////////////////////// ROUTINGS
            /*
            if (pedido.RUBRO4 != "0" && pedido.RUBRO4 != "" && pedido.RUBRO4 != null)  //RoutingID
            {
                query = "SELECT";

                foreach (_CONFIG row in data)
                {
                    valor = "";
                    p = 0;
                    if (tb_arg.PRODUCTO == "1") //aereo
                    {
                        if (row.exconf_campo_aereo != "")
                        {
                            if (row.exconf_campo_aereo.Contains("."))
                            {
                                p = row.exconf_campo_aereo.IndexOf(".");
                                tabla = row.exconf_campo_aereo.Substring(0, p);
                                campo = row.exconf_campo_aereo.Remove(0, p + 1);

                                if (tabla == "routings")
                                {
                                    if (campo == "routing") valor = row.exconf_campo_aereo;
                                    if (campo.Contains("fecha")) valor = row.exconf_campo_aereo;
                                }

                            }

                        }

                    }

                    if (tb_arg.PRODUCTO == "2") //terrestre
                    {

                        if (row.exconf_campo_terrestre != "")
                        {
                            if (row.exconf_campo_terrestre.Contains("."))
                            {
                                p = row.exconf_campo_terrestre.IndexOf(".");
                                tabla = row.exconf_campo_terrestre.Substring(0, p);
                                campo = row.exconf_campo_terrestre.Remove(0, p + 1);

                                if (tabla == "routings")
                                {
                                    //if (campo == "routing") valor = campo;
                                    //if (campo.Contains("fecha")) valor = campo;
                                    valor = campo;
                                }

                            }

                        }

                    }

                    if (valor != "")
                    {

                        if (query != "SELECT")
                            query += ",";

                        query += " " + (valor.Trim() == "" ? "''" : valor) + " as " + row.exconf_campo.Replace(" ", "_") + "";
                    }
                }

                if (query != "SELECT")
                {

                    query += " FROM routings WHERE id_routing = " + pedido.RUBRO4;

                    _PEDIDO routing = Postgres_.GetRowPostgres<_PEDIDO>(dbStr, query);

                    if (routing != null)
                    {

                        ////////////////////////////////////////// MERGE ROUTINGS

                        pedido.COMENTARIO_CXC = routing.COMENTARIO_CXC == null ? "" : routing.COMENTARIO_CXC;
                        //pedido.FECHA_PEDIDO = routing.FECHA_PEDIDO;
                    }

                }

            }
            */
            #endregion


            int bl_abierto = tb_arg.BL_ID;

            if (tb_arg.abierto == "1") //2021-11-25
            {
                bl_abierto = 0;
            }



            //if (tb_arg.IMPEX == "") tb_arg.IMPEX = pedido.MOVIMIENTO; 2021-08-06

            ///////////////////////////////  DISTINCT DETALLES MONEDAS


            switch (tb_arg.PRODUCTO) {
                case "1": //aereo
                    query = @"SELECT Count(a.AWBID) as TOTAL_UNIDADES, a.CurrencyID as MONEDA, 0 as CLIENTE, '' AS NOMBRE_CLIENTE

,(SELECT COALESCE(Sum(b.Value),0) FROM ChargeItems b WHERE b.AWBID = a.AWBID AND b.Expired = '0' AND b.DocTyp = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + @"' AND b.ServiceID <> 14 AND a.CurrencyID = b.CurrencyID) as TOTAL_MERCADERIA 

,(SELECT COALESCE(Sum(b.Value),0) FROM ChargeItems b WHERE b.AWBID = a.AWBID AND b.Expired = '0' AND b.DocTyp = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + @"' AND b.ServiceID = 14 AND a.CurrencyID = b.CurrencyID) as MONTO_OTRO_CARGO

, '" + data_conf.transportista.Trim() + @"' as U_TRANSPORTISTA_TI

FROM ChargeItems a WHERE a.AWBID = " + bl_abierto + @" AND a.Expired = '0' AND (a.InvoiceID = 0 OR a.DocType = 9) AND a.DocTyp = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + @"' AND a.CurrencyID <> ''

GROUP BY a.CurrencyID;";

                    pedido.U_AGENTE_TI = data_conf.U_AGENTE_TI;
                    break;

                case "2": //terrestre
                    query = @"SELECT Count(a.SBLID) as TOTAL_UNIDADES, a.Currency as MONEDA, 0 as CLIENTE, '' AS NOMBRE_CLIENTE

,(SELECT COALESCE(Sum(b.Value),0) FROM ChargeItems b WHERE b.SBLID = a.SBLID AND b.Expired = '0' AND b.ServiceID <> 14 AND a.Currency = b.Currency) as TOTAL_MERCADERIA 

,(SELECT COALESCE(Sum(b.Value),0) FROM ChargeItems b WHERE b.SBLID = a.SBLID AND b.Expired = '0' AND b.ServiceID = 14 AND a.Currency = b.Currency) as MONTO_OTRO_CARGO

, '" + pedido.U_TRANSPORTISTA_TI.Trim() + @"' as U_TRANSPORTISTA_TI

FROM ChargeItems a WHERE a.SBLID = " + bl_abierto + @" AND a.Expired = '0' AND (a.InvoiceID = 0 OR a.DocType = 9) AND a.Currency <> ''

GROUP BY a.Currency;";
                    break;
            }


            if (tb_arg.abierto == "1") //2021-11-30 //2022-02-22 se agrega filtro prepaid collect porque en terrestre total unidades 2 al tomar ambos registros
            {
                query = query.Replace("AND a.Expired = '0'", "AND a.PrepaidCollect = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + "' AND a.Expired = '0' AND a.UserID = '" + data_conf.id_pais + "'"); //amarra los rubros comodines a una empresa
                query = query.Replace("AND b.Expired = '0'", "AND b.PrepaidCollect = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + "' AND b.Expired = '0' AND b.UserID = '" + data_conf.id_pais + "'"); //amarra los rubros comodines a una empresa
            }

            System.Data.IDataReader reader = MySql_.GetDataReader(query, producto);

            List<_PEDIDO> monedas = Utils.get_list_struct<_PEDIDO>(reader);

            if (monedas.Count != 1) //2021-12-09 solo debe permitir un tipo de moneda por la nueva modalidad de pedido abierto
            {
                if (monedas.Count == 0)
                    tb_arg.msg = "No hay rubros para facturar";
                else
                    tb_arg.msg = "Se ha ingresado mas de una moneda, no se podra procesar";
                tb_arg.stat = 6;
                return null;
            }



            if ((data_conf.pedido_erp == null || data_conf.pedido_erp == "") && tb_arg.abierto == "")
            {

                if (monedas.Count != 1)
                {
                    tb_arg.msg = "No hay PEDIDO ERP para adjuntar mas datos";
                    tb_arg.stat = 2;
                    return null;
                }
            }


            List<_PEDIDO> clientes = new List<_PEDIDO>();

            if (tb_arg.abierto != "1") //2022-05-19 solo si es pedido no abierto
            {

                switch (tb_arg.PRODUCTO)
                {
                    case "1": //aereo
                        query = @"SELECT Count(a.AWBID) as TOTAL_UNIDADES, a.CurrencyID as MONEDA, a.id_cliente as CLIENTE, cliente_nombre AS NOMBRE_CLIENTE

,(SELECT COALESCE(Sum(b.Value), 0) FROM ChargeItems b WHERE b.AWBID = a.AWBID AND b.Expired = '0' AND b.ServiceID <> 14 AND a.id_cliente = b.id_cliente) as TOTAL_MERCADERIA

,(SELECT COALESCE(Sum(b.Value), 0) FROM ChargeItems b WHERE b.AWBID = a.AWBID AND b.Expired = '0' AND b.ServiceID = 14 AND a.id_cliente = b.id_cliente) as MONTO_OTRO_CARGO

, '" + data_conf.transportista.Trim() + @"' as U_TRANSPORTISTA_TI

FROM ChargeItems a WHERE a.AWBID = " + bl_abierto + @" AND a.Expired = '0' AND COALESCE(a.id_cliente,0) > 0 AND(a.InvoiceID = 0 OR a.DocType = 9) AND a.DocTyp = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + @"' 

GROUP BY a.id_cliente;";
                        break;

                    case "2": //terrestre
                        query = @"SELECT Count(a.SBLID) as TOTAL_UNIDADES, a.Currency as MONEDA, a.id_cliente as CLIENTE, cliente_nombre AS NOMBRE_CLIENTE

,(SELECT COALESCE(Sum(b.Value), 0) FROM ChargeItems b WHERE b.SBLID = a.SBLID AND b.Expired = '0' AND b.ServiceID <> 14 AND a.id_cliente = b.id_cliente) as TOTAL_MERCADERIA

,(SELECT COALESCE(Sum(b.Value), 0) FROM ChargeItems b WHERE b.SBLID = a.SBLID AND b.Expired = '0' AND b.ServiceID = 14 AND a.id_cliente = b.id_cliente) as MONTO_OTRO_CARGO

, '" + pedido.U_TRANSPORTISTA_TI.Trim() + @"' as U_TRANSPORTISTA_TI

FROM ChargeItems a WHERE a.SBLID = " + bl_abierto + @" AND a.Expired = '0' AND COALESCE(a.id_cliente,0) > 0 -- AND(a.InvoiceID = 0 OR a.DocType = 9)

GROUP BY a.id_cliente;";
                        break;
                }

                reader = MySql_.GetDataReader(query, producto);

                clientes = Utils.get_list_struct<_PEDIDO>(reader);

                if (clientes.Count > 0) //2022-05-19
                {
                    monedas = clientes; //monedas llevaran los grupos de clientes
                }
            }

            List<string> pedidos = new List<string>();

            _PEDIDO pedido_tmp = new _PEDIDO();

            string RUBRO1 = pedido.RUBRO1, RUBRO2 = pedido.RUBRO2, RUBRO5 = pedido.RUBRO5;

            foreach (_PEDIDO moneda in monedas) /////// GENERA UN PEDIDO POR CADA MONEDA QUE CONTENGAN LOS DETALLES
            {
                pedido_tmp = new _PEDIDO();

                pedido_tmp = pedido;

                pedido_tmp.FECHA_HORA = "";

                if (clientes.Count == 0) //si no hay datos nuevos rubros por clientes, realiza lo tradicional
                {
                    pedido_tmp.CLIENTE = data_conf.id_cliente;
                    pedido_tmp.U_CLIENTE_TI = data_conf.nombre_cliente;
                } else
                {
                    pedido_tmp.CLIENTE = moneda.CLIENTE;
                    pedido_tmp.U_CLIENTE_TI = moneda.NOMBRE_CLIENTE;
                }


                if (pedido_tmp.U_CLIENTE_TI.Length > 35)                
                    pedido_tmp.U_CLIENTE_TI = pedido_tmp.U_CLIENTE_TI.Substring(0, 34).Trim();

                pedido_tmp.U_TRANSPORTISTA_TI = moneda.U_TRANSPORTISTA_TI;

                if (pedido_tmp.U_TRANSPORTISTA_TI.Length > 35)
                    pedido_tmp.U_TRANSPORTISTA_TI = pedido_tmp.U_TRANSPORTISTA_TI.Substring(0, 34).Trim();

                if (pedido_tmp.U_AGENTE_TI.Length > 80)                
                    pedido_tmp.U_AGENTE_TI = pedido_tmp.U_AGENTE_TI.Substring(0,79).Trim();


                pedido_tmp.MOVIMIENTO = tb_arg.IMPEX;
                pedido_tmp.BODEGA = tb_arg.BODEGA;
                pedido_tmp.ACTIVIDAD_COMERCIAL = tb_arg.ACTIVIDAD_COMERCIAL;
                pedido_tmp.CONDICION_PAGO = tb_arg.CONDICION_PAGO;
                pedido_tmp.OBSERVACIONES = tb_arg.OBSERVACIONES;

                pedido_tmp.TOTAL_MERCADERIA = moneda.TOTAL_MERCADERIA;
                pedido_tmp.TOTAL_A_FACTURAR = moneda.TOTAL_MERCADERIA;
                pedido_tmp.TOTAL_UNIDADES = moneda.TOTAL_UNIDADES;
                pedido_tmp.MONEDA = "08 |" + moneda.MONEDA;
                pedido_tmp.MONEDA_PEDIDO = "08|" + moneda.MONEDA;
                pedido_tmp.NIVEL_PRECIO = "08|" + moneda.MONEDA;
                pedido_tmp.MONTO_OTRO_CARGO = moneda.MONTO_OTRO_CARGO;

                pedido_tmp.PEDIDO = "12|" + data_conf.cs_producto + "-" + tb_arg.IMPEX.ToUpper().Substring(0, 2) + (data_conf.cs_sub_producto == "" ? "" : "-" + data_conf.cs_sub_producto);

                pedido_tmp.TIPO_CAMBIO = data_conf.tca_tcambio;

                decimal PrepaidCollect = 0;

                /////////////////////////// INSERT PEDIDO Ó VALIDA EXISTENCIA


                pedido_tmp.RUBRO1 = RUBRO1; 
                pedido_tmp.RUBRO2 = RUBRO2;
                pedido_tmp.RUBRO5 = RUBRO5;


                pedido_id = SavePedido(pedido_tmp, ref tb_arg, data_conf);

                if (tb_arg.stat == 1 && int.Parse(pedido_id) > 0)
                {
                    pedido_tmp.PEDIDO += "|" + pedido_id;

                    pedido_tmp.RUBRO1 = ""; //bl id
                    pedido_tmp.RUBRO2 = ""; //master
                    //pedido_tmp.RUBRO3 = ""; nada
                    //pedido_tmp.RUBRO4 = ""; CP / GUIA
                    pedido_tmp.RUBRO5 = ""; //routing id

                    List<_PEDIDO_LINEA> ped_lineas = BuildDetailConf(tb_arg, "PEDIDO_LINEA", pedido_tmp, data_conf, ref charges, ref PrepaidCollect); //, ref total, ref c, ref moneda);

                    if (PrepaidCollect > 0) //2021-07-28
                    {
                        pedido_tmp.LINEAS = ped_lineas;

                        pedidos.Add(JsonConvert.SerializeObject(pedido_tmp));

                    }
                    else
                    {
                        if (tb_arg.IMPEX.ToUpper() == "IMPORT") //2021-07-28
                        {
                            tb_arg.msg = "Para carga IMPORT no hay valores Collect";
                        }
                        else
                        {
                            tb_arg.msg = "Para carga EXPORT no hay valores Prepaid";
                        }

                        tb_arg.stat = 8;
                        return null;
                    }
                }
                else
                {
                    tb_arg.msg = "Id de pedido fallo : " + pedido_id;
                    tb_arg.stat = 7;
                    return null;
                }
            }

            return pedidos;
        }

        #endregion

        #region Build Detail Conf

        public static List<_PEDIDO_LINEA> BuildDetailConf(Struct.arg_pedidos tb_arg, string documento, _PEDIDO pedido, _PARAMETROS data_conf, ref List<string> charges, ref decimal PrepaidCollect) //, ref decimal total, ref int c, ref string moneda)
        {
            string query = "";

            List<_PEDIDO_LINEA> new_lineas = new List<_PEDIDO_LINEA>();

            int bl_abierto = tb_arg.BL_ID;

            if (tb_arg.abierto == "1") //2021-11-25
            {
                bl_abierto = 0;
            }

			

            List<_CONFIG> data = BuildQuery(tb_arg, documento, ref query);

            System.Data.IDataReader reader = null;
            switch (tb_arg.PRODUCTO)
            {
                case "1":
                    query += " FROM ChargeItems WHERE Expired = 0 AND AWBID = " + bl_abierto + " AND DocTyp = '" + (tb_arg.IMPEX == "IMPORT" ? "1" : "0") + "' ";
                    query += " AND CurrencyID = '" + pedido.MONEDA.Split('|')[1] + "' AND (InvoiceID = 0 OR DocType = 9)";
                    break;

                case "2":
                    query += " FROM ChargeItems WHERE Expired = 0 AND SBLID = " + bl_abierto + " ";
                    query += " AND Currency = '" + pedido.MONEDA.Split('|')[1] + "' AND (InvoiceID = 0 OR DocType = 9)";
                    break;
            }


            if (tb_arg.abierto == "1") //2021-11-30
            {
                query = query.Replace(" AND Expired = 0", "AND Expired = 0 AND UserID = '" + data_conf.id_pais + "'"); //amarra los rubros comodines a una empresa
            }
            else
            {
                query += " AND id_cliente = CASE WHEN id_cliente IS NULL THEN id_cliente ELSE " + pedido.CLIENTE + " END ";
            }


            switch (tb_arg.PRODUCTO)
            {
                case "1":
                    reader = MySql_.GetDataReader(query, "aereo");
                    break;

                case "2":
                    reader = MySql_.GetDataReader(query, "terrestre");
                    break;
            }

            List<_PEDIDO_LINEA> lineas = Utils.get_list_struct<_PEDIDO_LINEA>(reader);

            int c = 0; string charges_str = ""; decimal precio_u = 0;

            PrepaidCollect = 0;

            foreach (_PEDIDO_LINEA line in lineas)
            {
                line.LINEA_USUARIO = c.ToString();

                charges_str += "|" + line.ROWPOINTER.ToString();

                c++;

                line.LINEA_ORDEN_COMPRA = c.ToString();
                line.BODEGA = pedido.BODEGA;
                line.PEDIDO = pedido.PEDIDO;
                line.PEDIDO_LINEA = c.ToString();
                line.ARTICULO = "09|" + data_conf.cs_producto + line.LOCALIZACION + "-" + tb_arg.IMPEX.ToUpper().Substring(0, 2) + "-" + (data_conf.cs_sub_producto == "" ? "A-" : data_conf.cs_sub_producto + "-") + line.LOTE;

                line.ES_OTRO_CARGO = line.LOCALIZACION == "14" ? "S" : "N"; // 2021-07-07 surgio en reunion TRANSIT

                line.UNIDAD_DISTRIBUCIO = "";   //service name
                line.LOCALIZACION = "";         //service id
                line.LOTE = "";                 //rubro id
                line.ROWPOINTER = "";           //charge_id
                

                //PrepaidCollect    0=Prepaid, 1=Collect
                //segun lo conversado con Carlos, solo los articulos validos deben aparecen en el detalle, el total de articulos ya fue asignado en la sumatoria de la moneda

                precio_u = decimal.Parse(line.PRECIO_UNITARIO);

                if (tb_arg.abierto == "1") //2021-12-09
                {
                    precio_u = 1;
                }


                if (tb_arg.IMPEX.ToUpper() == "IMPORT") //2021-07-28
                {
                    if (line.PEDIDO_LINEA_BONIF == "1") // COLLECT
                    {
                        PrepaidCollect += precio_u;

                        line.PEDIDO_LINEA_BONIF = "";           //PrepaidCollect 2021-07-28

                        new_lineas.Add(line);
                    }

                } else {

                    if (line.PEDIDO_LINEA_BONIF == "0") // PREPAID
                    {
                        PrepaidCollect += precio_u;

                        line.PEDIDO_LINEA_BONIF = "";           //PrepaidCollect 2021-07-28

                        new_lineas.Add(line);
                    }

                }
    
            }

            charges.Add(charges_str);

            return new_lineas;
        }

        #endregion

        #region BuildQuery - CONTRUYE EL QUERY DE LA TABLA CONFIGURACIONES

        public static List<_CONFIG> BuildQuery(Struct.arg_pedidos tb_arg, string documento, ref string query)
        {
            query = @"SELECT exconf_campo, exconf_nulo, exconf_valor_default, exconf_campo_aereo, exconf_catalogo, exconf_campo_terrestre  FROM exactus_configuraciones WHERE exconf_documento = '" + documento + "' ORDER BY exconf_order";

            System.Data.IDataReader reader = Postgres_.GetDataReader(query, dbStr);

            var data = Utils.get_list_struct<_CONFIG>(reader);

            string valor = "";

            /////////////////////////////////// principal
            query = "SELECT";

            foreach (_CONFIG row in data)
            {
                valor = "";

                if (row.exconf_nulo == "Y")
                    valor = "";

                if (row.exconf_valor_default != "")
                    valor = "'" + row.exconf_valor_default + "'";

                if (tb_arg.PRODUCTO == "1") //aereo
                {
                    if (row.exconf_campo_aereo != "")
                    {
                        if (row.exconf_campo_aereo.Contains("movimiento"))
                        {
                            valor = "'" + tb_arg.IMPEX.ToUpper() + "'";
                        }
                        else
                            if (!row.exconf_campo_aereo.Contains("."))
                            {

                                if (row.exconf_catalogo != "") // concatena catalogo para homologacion
                                {
                                    valor = "CONCAT('" + row.exconf_catalogo + "','|'," + row.exconf_campo_aereo + ")";
                                }
                                else
                                    valor = row.exconf_campo_aereo;
                            }

                    }

                }

                if (tb_arg.PRODUCTO == "2") //terrestre
                {
                    //validar empresa a partir del impex


                    if (row.exconf_campo == "EMPRESA") //2021-09-17
                    {

                        if (tb_arg.IMPEX == "IMPORT")
                        {
                            row.exconf_campo_terrestre = row.exconf_campo_terrestre.Split(',')[1]; // "CountriesFinalDes";
                        }

                        if (tb_arg.IMPEX == "EXPORT")
                        {
                            row.exconf_campo_terrestre = row.exconf_campo_terrestre.Split(',')[0]; // "CountryOrigen";
                        }                   
                    }
                    

                    if (row.exconf_campo_terrestre != "")
                    {
                        if (row.exconf_campo_terrestre.Contains("movimiento"))
                        {
                            valor = "'" + tb_arg.IMPEX.ToUpper() + "'";
                        }
                        else
                            if (!row.exconf_campo_terrestre.Contains("."))
                            {

                                if (row.exconf_catalogo != "") // concatena catalogo para homologacion
                                {
                                    valor = "CONCAT('" + row.exconf_catalogo + "','|'," + row.exconf_campo_terrestre + ")";
                                }
                                else
                                    valor = row.exconf_campo_terrestre;
                            }

                    }

                }

                if (query != "SELECT")
                    query += ",";

                query += " " + (valor.Trim() == "" ? "''" : valor) + " as " + row.exconf_campo.Replace(" ", "_") + "";
            }

            return data;
        }

        #endregion

        #region SavePedido

        public static string SavePedido(_PEDIDO pedido, ref Struct.arg_pedidos tb_arg, _PARAMETROS data_conf)
        {
            string id_pedido = "0";

            string query = "";

            tb_arg.stat = 1;
            tb_arg.msg = "";

            try
            {
                /* AL PARECER NO SE DEBE VALIDAR POR PEDIDO EXISTENTE				
                query = "SELECT id_pedido, estado FROM exactus_pedidos WHERE documento = '" + pedido.RUBRO2 + "' AND id_documento = '" + pedido.RUBRO1 + "' AND pais = '" + pedido.PAIS + "' AND id_cargo_system = " + tb_arg.PRODUCTO + " AND estado > 2";
                _exactus_pedidos data = Postgres_.GetRowPostgres<_exactus_pedidos>(dbStr, query);
                if (data != null)
                {
                    if (int.Parse(data.id_pedido) > 0)
                    {
                        if (int.Parse(data.estado) > 2) // firmados
                            id_pedido = data.id_pedido;
                    }
                }
                */


                if (id_pedido == "0")
                {
                    query = @"
					INSERT INTO exactus_pedidos(
						id_empresa_parametros,
						fecha_solicitud,
						documento,
						id_documento,
						pais,
						id_cargo_system,
						id_usuario,
						pedido,
						pedido_fecha,
						estado,
						valor,
						moneda,
						movimiento,
                        codigo_consecutivo,
                        tipo_carga
						) VALUES (
						"  + data_conf.id_pais + @",
						now(),
						'" + (pedido.RUBRO4 == "" ? pedido.RUBRO2 : pedido.RUBRO4) + @"',
						'" + pedido.RUBRO1 + @"',
						'" + pedido.EMPRESA + @"',
						" + tb_arg.PRODUCTO + @",
						'" + tb_arg.user + @"',
						'',
						NOW(),
						1,
						'" + pedido.TOTAL_MERCADERIA + @"',
						'" + pedido.MONEDA.Split('|')[1] + @"',
						'" + pedido.MOVIMIENTO + @"',
						'" + pedido.PEDIDO + @"',
						'" + pedido.TIPO_CARGA + @"'
						);
						SELECT CAST(currval('exactus_pedidos_id_pedido_seq') as text) as _id;";

                    id_pedido = Postgres_.GetScalar(query, dbStr);
                }

            }
            catch (Exception ex)
            {
                //return ex.Message;
                tb_arg.stat = 2;
                tb_arg.msg = ex.Message;
            }

            try
            {
                int t = int.Parse(id_pedido);
            }
            catch (Exception ex)
            {

                tb_arg.stat = 2;
                tb_arg.msg = id_pedido;
            }

            return id_pedido;
        }

        #endregion

        #region SendPedido

        public async static Task<List<Struct._RESPUESTA>> SendPedido(List<_PEDIDO> pedidos, string NombreCatalogo, int id, _PARAMETROS data_conf) //, ref string responseBody)
        {
            string responseBody = "";

            List<Struct._RESPUESTA> responseObject = new List<Struct._RESPUESTA>();

            Struct._RESPUESTA des = new Struct._RESPUESTA();

            try
            {
                try
                {
                    if (data_conf == null)
                    {
                        //id 41 CRTLA credenciales
                        string query = "SELECT exactus_url, exactus_url_username, exactus_url_password, exactus_usuario FROM empresas_parametros WHERE id = " + id; //	country = CRTLA

                        data_conf = Postgres_.GetRowPostgres<_PARAMETROS>(dbStr, query);
                    }

                    //data_conf.exactus_url = "http://wstlatest.grupopasqui.com/WSAccounting/api/Accounting/Pedidos";
                    //data_conf.exactus_url = "http://wstlatest/WSAccounting/api/Accounting/Pedidos";

                    if (data_conf.exactus_url == "" || data_conf.exactus_url == null)
                    {
                        des.MENSAJE = "Configuracion Url sin valor";
                        responseObject.Add(des);
                        return responseObject;
                    }

                    if (data_conf.exactus_url_username == "" || data_conf.exactus_url_username == null)
                    {
                        des.MENSAJE = "Configuracion Usuario (credenciales) sin valor";
                        responseObject.Add(des);
                        return responseObject;
                    }

                    if (data_conf.exactus_url_password == "" || data_conf.exactus_url_password == null)
                    {
                        des.MENSAJE = "Configuracion Password (credenciales) sin valor";
                        responseObject.Add(des);
                        return responseObject;
                    }

                    if (NombreCatalogo != null)
                        data_conf.exactus_url = data_conf.exactus_url.Replace("Pedidos", "Catalogos");

                    string sendingText = "";

                    if (pedidos != null)
                        sendingText = JsonConvert.SerializeObject(pedidos);

                    if (NombreCatalogo != null)
                        sendingText = "[{\"NombreCatalogo\": \"" + NombreCatalogo + "\"}]";

                    using (HttpClient Client = new HttpClient())
                    {

                        var url = new Uri(data_conf.exactus_url);

                        var byteArray = Encoding.ASCII.GetBytes(data_conf.exactus_url_username + ":" + data_conf.exactus_url_password);

                        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                        var content = new StringContent(sendingText, Encoding.UTF8, "application/json");

                        var response = Client.PostAsync(url, content).Result;

                        responseBody = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var temp = responseBody.Replace("[\"", "").Replace("\"]", "").Replace("null", "\"\"").Replace("\\", "");

                            responseObject = JsonConvert.DeserializeObject<List<Struct._RESPUESTA>>(temp);

                            responseBody = JsonConvert.SerializeObject(responseObject);
                        }
                        catch (JsonReaderException e)
                        {
                            responseBody = e.Message;
                        }

                    }

                }
                catch (HttpRequestException e)
                {
                    des.MENSAJE = e.Message;
                    responseObject.Add(des);
                }

            } catch(Exception e) {

                //string hostName = System.Net.Dns.GetHostName(); // Retrive the Name of HOST
 
                // Get the IP
                //string myIP = System.Net.Dns.GetHostByName(hostName).AddressList[0].ToString();

                Uri originalUri = System.Web.HttpContext.Current.Request.Url;

                des.MENSAJE = "No hay acceso desde [" + originalUri + "] hacia [" + data_conf.exactus_url + "] " + e.Message + ".";
                responseObject.Add(des);           
            }

            return responseObject;

        }

        #endregion

        #region GetDocumentos GENERA QUERY PRUEBA SOLO COMO TEST
        /// ///////////////////////////// GENERA QUERY PRUEBA SOLO COMO TEST
        /*
        public static List<_PEDIDO> GetDocumentos()
        {

            ///////////////////// PEDIDOS 

            System.Data.IDataReader reader = Postgres_.GetDataReader(QryPedido, dbStr);

            var data = Utils.get_list_struct<_PEDIDO>(reader);

            ///////////////////// LINEAS

            List<_PEDIDO> pedidos = new List<_PEDIDO>();

            System.Data.IDataReader reader_line = null;

            foreach (_PEDIDO row in data)
            {
                reader_line = Postgres_.GetDataReader(QryLinea, dbStr);

                var data_line = Utils.get_list_struct<_PEDIDO_LINEA>(reader_line);

                row.LINEAS = new List<_PEDIDO_LINEA>();

                foreach (_PEDIDO_LINEA row_line in data_line)
                {
                    row.LINEAS.Add(row_line);
                }

                pedidos.Add(row);
            }

            return pedidos;
        }
        */
        #endregion


        ////////////////////////////////////////////////// METODO GET DOCUMENT C X C  ////////////////////////////////////////////////////////////////////////
        // desde exactus envian el numero de factura principalmente y notas de credito
        public static Struct._RESPUESTA UpdateDocumentoCXC(string esquema, string id_pedido, string pedido_erp, string tipo_doc, string documento, string fecha, string valor, string impuesto, string accion, string fc_numero, string user)
        {
            Struct._RESPUESTA result = new Struct._RESPUESTA();

            int _res = 0, factura_id = 0;
            //string res = "0";
            string query = "";
            string estado = "";
            string msg = "";
            string tipo_doc_standar = "";
            Boolean retorna_rubros = false;
            Boolean factura_rubros = false;
        
            result.ASIENTO = "";
            result.CODIGO_ERROR = "";
            result.MENSAJE = "";

            try
            {



                switch (tipo_doc)
                {

                    case "C": //cancelacion pedidos directo 
                        tipo_doc_standar = "CANCELA";
                        break;

                    case "FAC": //factura
                    case "P": //pedido 2021-06-22
                    case "F": //pedido 2021-06-28
                        tipo_doc_standar = "FACTURA";
                        break;

                    case "NCP": // nota credito parcial
                    case "NCT": //nota credito total
                    case "NC": // 2021-06-15
                    case "D": // 2021-06-28 devoluciones exactus
                        tipo_doc_standar = "DEVUELVE";

                        break;

                    default:
                        result.CODIGO_ERROR = "96";
                        result.MENSAJE = "Tipo de documento no clasificado : " + tipo_doc;
                        return result;
                        break;
                }



                query = "SELECT id_pedido, estado, json_exactus, pais, id_empresa_parametros, id_cargo_system, movimiento FROM exactus_pedidos WHERE id_pedido = " + id_pedido;

                _exactus_pedidos data = Postgres_.GetRowPostgres<_exactus_pedidos>(dbStr, query);

                if (data != null)
                {
                    int _id_pedido = 0;

                    estado = data.estado;

                    try
                    {
                        _id_pedido = int.Parse(data.id_pedido);

                    }
                    catch (Exception ex)
                    {
                        result.CODIGO_ERROR = "93";
                        result.MENSAJE = "id_pedido no es valido o no existe en cargo system";
                        return result;
                    }

                    if (_id_pedido > 0)
                    {
                        //"CODIGO_ERROR\":\"99\",\"ESTADO\":\"CORRECTO\",\"MENSAJE\":\"PROCESO CORRECTO

                        if (data.json_exactus.Contains("99") && data.json_exactus.Contains("CORRECTO") && data.json_exactus.Contains("PROCESO CORRECTO") && (data.estado == "3" || data.estado == "4"))
                        {





                            id_pedido = _id_pedido.ToString();

                            result.COD_COMPANIA = data.id_empresa_parametros;
                            result.COD_PAIS = data.pais;

                            string[] split_date = fecha.Split('/');

                            if (split_date.Length == 3)
                            {

                                string new_date = split_date[2] + "-" + split_date[0].PadLeft(2, '0') + "-" + split_date[1].PadLeft(2, '0');

                                if (documento.Trim() == "") documento = "--";


                                

                                try
                                {

                                    //ambos tipos insertan en tabla nc
                                    switch (tipo_doc_standar)
                                    {
                                        case "CANCELA":
                                        case "DEVUELVE":


                                            try {

                                                //busca la factura obligatorio
                                                if (fc_numero == "" && tipo_doc_standar == "CANCELA")
                                                    query = "SELECT CAST(fc_id as text) FROM exactus_pedidos_fc WHERE fc_estado = 1 AND id_pedido = " + id_pedido + " LIMIT 1";
                                                else
                                                    query = "SELECT CAST(fc_id as text) FROM exactus_pedidos_fc WHERE fc_estado = 1 AND id_pedido = " + id_pedido + " AND fc_numero = '" + fc_numero + "' LIMIT 1";

                                                factura_id = Int32.Parse(Postgres_.GetScalar(query, dbStr));

                                            }
                                            catch (Exception ex)
                                            {
                                                //result.CODIGO_ERROR = "98";
                                                //result.MENSAJE = "Error " + tipo_doc + " " + tipo_doc_standar + " : " + ex.Message;
                                            }
                                  
                                            if (tipo_doc_standar == "CANCELA" && factura_id > 0) //data.estado == "4" || 
                                            {
                                                result.CODIGO_ERROR = "93";
                                                result.MENSAJE = "id_pedido tiene factura, no es posible cancelar directo";
                                                return result;
                                            }


                                        
                                            //si hay registros identicos anteriores los anula
                                            query = @"UPDATE exactus_pedidos_nc SET nc_estado = 5, nc_comentarios = NOW() WHERE nc_estado < 5 AND id_pedido = '" + id_pedido + "' AND pedido_erp = '" + pedido_erp + "' AND nc_numero = '" + documento + "' AND nc_fecha = '" + new_date + "' ";
                                            Postgres_.EjecutaQuery(query, dbStr);

                                            //inserta las cancelaciones y notas de credito
                                            query = @"INSERT INTO exactus_pedidos_nc (
										        id_pedido,
										        pedido_erp,
										        nc_tipo,
										        nc_numero,
										        nc_fecha,
										        nc_valor,
										        nc_impuesto,
										        fc_numero,
										        nc_estado,
                                                nc_user			
								            ) VALUES ( 
								            " + id_pedido + ", '" + pedido_erp + "', '" + tipo_doc + "', '" + documento + "', '" + new_date + "', " + valor + ", " + impuesto + ", '" + fc_numero + @"', 1, '" + user + @"');

									        SELECT CAST(currval('exactus_pedidos_nc_nc_id_seq') as text) as _id;";

                                            result.ASIENTO = Postgres_.GetScalar(query, dbStr);

                                            _res = int.Parse(result.ASIENTO);
                                            if (_res < 1)
                                            {
                                                result.CODIGO_ERROR = "93";
                                                result.MENSAJE = "Problemas para insertar \"" + tipo_doc_standar + "\"";
                                                return result;
                                            }
                                            break;
                                    }




                                

                                    switch (tipo_doc_standar)
                                    {
                                        case "CANCELA":

                                            //ESTILO DISPLAY MENSAJES
                                            msg = ", pedido = pedido || '|" + FRow1 + FReg1 + " CANCELACION : " + FReg2 + FBold1 + pedido_erp + FBold2 + FReg1 + " FECHA : " + new_date + " (' || NOW() || ') " + FReg2 + FRow2 + "'";

                                            //cuando es cancelacion, anula directo el pedido
                                            query = "UPDATE exactus_pedidos SET esquema = '" + esquema + "', estado = 5 " + msg + " WHERE id_pedido = " + id_pedido + " AND estado IN (3,4) ";
                                            _res = Postgres_.EjecutaQuery(query, dbStr);

                                            if (_res > 0)
                                                retorna_rubros = true;

                                            break;

                                        case "FACTURA":

                                            //si hay registros identicos anteriores los anula
                                            query = @"UPDATE exactus_pedidos_fc SET fc_estado = 5, fc_comentarios = NOW() WHERE id_pedido = '" + id_pedido + "' AND fc_estado = 1 AND pedido_erp = '" + pedido_erp + "' AND fc_numero = '" + documento + "' AND fc_fecha = '" + new_date + "' "; 
                                            Postgres_.EjecutaQuery(query, dbStr);

                                            query = @"INSERT INTO exactus_pedidos_fc (
										        id_pedido,
										        pedido_erp,
										        fc_numero,
										        fc_fecha,
										        fc_valor,
										        fc_impuesto,
										        fc_saldo,
										        fc_estado,
                                                fc_user
								            ) VALUES ( 
								            " + id_pedido + ", '" + pedido_erp + "', '" + documento + "', '" + new_date + "', " + valor + ", " + impuesto + @", " + valor + @", 1, '" + user + @"');

								            SELECT CAST(currval('exactus_pedidos_fc_fc_id_seq') as text) as _id; ";

                                            result.ASIENTO = Postgres_.GetScalar(query, dbStr);

                              
                                            _res = int.Parse(result.ASIENTO);

                                            //ESTILO DISPLAY MENSAJES
                                            msg = ", pedido = pedido || '|" + FRow1 + FReg1 + id_pedido + " FACTURA : " + FReg2 + FBold1 + documento + FBold2 + FReg1 +  " FECHA : ' || to_char(CURRENT_DATE, 'DD/MM/YYYY') || '" + FReg2 + FRow2 + "'";

                                            /////////////////////// ACTUALIZA EL MENSAJE CON LA FACTURA
                                            query = "UPDATE exactus_pedidos SET esquema = '" + esquema + "', estado = 4 " + msg + " WHERE id_pedido = " + id_pedido + " AND estado = 3";  
                            
                                            _res = Postgres_.EjecutaQuery(query, dbStr);

                                            factura_rubros = true;

                                            break;

                                        case "DEVUELVE":

                                            //busca la factura obligatorio
                                            //query = "SELECT CAST(fc_id as text) FROM exactus_pedidos_fc WHERE fc_estado = 1 AND id_pedido = " + id_pedido + " AND fc_numero = '" + fc_numero + "' LIMIT 1";

                                            //_res = Int32.Parse(Postgres_.GetScalar(query, dbStr));

                                            if (factura_id > 0) //debe existir la factura
                                            {
                                                //////////////////// actualiza saldo
                                                query = "UPDATE exactus_pedidos_fc SET fc_saldo = fc_saldo - " + valor + ", fc_estado = 4 WHERE fc_estado < 5 AND fc_id = '" + factura_id + "'";
                                                Postgres_.EjecutaQuery(query, dbStr);


                                                /////////////////////// anula factura si saldo es cero
                                                query = "UPDATE exactus_pedidos_fc SET fc_estado = 5 WHERE fc_estado < 5 AND fc_id = '" + factura_id + "' AND fc_saldo <= 0";
                                                Postgres_.EjecutaQuery(query, dbStr);


                                                /////////////////////// anula pedido si saldo es cero
                                                query = @"UPDATE exactus_pedidos SET estado = 5

											    FROM exactus_pedidos_fc

											    WHERE exactus_pedidos.id_pedido = exactus_pedidos_fc.id_pedido AND

											    exactus_pedidos.id_pedido = " + id_pedido + " AND fc_id = '" + factura_id + "' AND fc_saldo <= 0 AND estado IN (3,4)";

                                                _res = Postgres_.EjecutaQuery(query, dbStr);

                                                if (_res > 0)
                                                {
                                                    msg = ", pedido = pedido || '|" + FRow1 + FReg1 + id_pedido + " DEVOLUCION : " + FReg2 + FBold1 + documento + FBold2 + FReg1 + " FECHA : ' || to_char(CURRENT_DATE, 'DD/MM/YYYY') || '" + FReg2 + FRow2 + "'";

                                                    /////////////////////// ACTUALIZA EL MENSAJE CON LA NC
                                                    query = "UPDATE exactus_pedidos SET esquema = '" + esquema + "' " + msg + " WHERE id_pedido = " + id_pedido + " AND estado = 5 ";
                                                    _res = Postgres_.EjecutaQuery(query, dbStr);

                                                    if (_res > 0)
                                                        retorna_rubros = true;
                                                }

                                            }
                                            else {
                                                result.CODIGO_ERROR = "94";
                                                result.MENSAJE = "Factura no encontrada " + fc_numero;
                                                return result;                                       
                                            }


                                            break;
                                    }


                                    if (retorna_rubros == true)
                                    {

                                        switch (data.id_cargo_system)
                                        {
                                            case "1":
                                                query = "UPDATE ChargeItems SET InvoiceID = 0, DocType = 0  WHERE InvoiceID = " + id_pedido + " AND DocType IN (9,10) AND DocTyp = " + (data.movimiento == "IMPORT" ? "1" : "0");
                                                MySql_.EjecutaQuery(query, "aereo");
                                                break;

                                            case "2":
                                                query = "UPDATE ChargeItems SET InvoiceID = 0, DocType = 0  WHERE InvoiceID = " + id_pedido + " AND DocType IN (9,10)";
                                                MySql_.EjecutaQuery(query, "terrestre");
                                                break;
                                        }
                                    }


                                    if (factura_rubros == true)
                                    {

                                        switch (data.id_cargo_system)
                                        {
                                            case "1":
                                                query = "UPDATE ChargeItems SET DocType = 10  WHERE InvoiceID = " + id_pedido + " AND DocType = 9 AND DocTyp = " + (data.movimiento == "IMPORT" ? "1" : "0");
                                                MySql_.EjecutaQuery(query, "aereo");
                                                break;

                                            case "2":
                                                query = "UPDATE ChargeItems SET DocType = 10  WHERE InvoiceID = " + id_pedido + " AND DocType = 9";
                                                MySql_.EjecutaQuery(query, "terrestre");
                                                break;
                                        }
                                    }


                                }
                                catch (Exception ex)
                                {
                                    result.CODIGO_ERROR = "98";
                                    result.MENSAJE = "Error " + tipo_doc + " " + tipo_doc_standar + " : " + ex.Message;
                                }


                            
                            }
                            else
                            {
                                result.CODIGO_ERROR = "95";
                                //res = "Verifique formato de fecha";
                                result.MENSAJE = "Verifique formato de fecha";
                            }


                        }
                        else
                        {

                            result.CODIGO_ERROR = "92";

                            switch (tipo_doc_standar)
                            {
                                case "CANCELA":
                                    result.MENSAJE = "El pedido al que requiere enviar \"CANCELACION\", no tiene el registro completo en exactus";
                                    break;

                                case "FACTURA":
                                    result.MENSAJE = "El pedido al que requiere enviar \"FACTURA\", no tiene el registro completo en exactus";
                                    break;

                                case "DEVUELVE":
                                    result.MENSAJE = "El pedido al que requiere \"DEVOLUCION\", no tiene el registro completo en exactus";
                                    break;
                            }

                            return result;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                result.CODIGO_ERROR = "94";
                result.MENSAJE = ex.Message;
            }



            if (result.CODIGO_ERROR == "" || result.MENSAJE == "") {

                result.CODIGO_ERROR = "99";
                result.ESTADO = "CORRECTO";
                result.MENSAJE = "Operacion registrada correctamente";           
            }



            return result;
        }


        public class _plantilla
        {
            public string tipo_plantilla { get; set; }
            public string pais_iso { get; set; }
            public string subject_ { get; set; }
            public string body_ { get; set; }
            public string to_ { get; set; }
            public string cc_ { get; set; }
            public string bc_ { get; set; }
        }

        /*
        public class Result1
        {
            public int stat;
            public String msg;
            public _plantilla datos;
        }
        */

        
        public static Struct.CSPedidosResult EvaluaPedidos(string HBLNumber, string ObjectID, string sistema, string CountryExactus, string pedido2str)
        {
            Struct.CSPedidosResult res = new Struct.CSPedidosResult();

            res.pedido_erp = "";
            res.msg = "";
            res.tipo_conta = "";
            res.stat = -1;

            string Msg = "", Pedido_Msg  = "";
            int Distinto = -1;

            Pedido_Msg = pedido2str;
                                                  //0                                        1                                             2                              3                                 4                                   5                                                                                    6                                                       7                               8                                                                    9                                                   10
            string QuerySelect = @"SELECT COALESCE(a.tipo_conta,'BAW') as tipo_conta, COALESCE(b.id_pedido,0) as id_pedido, COALESCE(b.pedido_erp ,'') as pedido_erp, COALESCE(b.estado,0) as estado, COALESCE(pedido,'') as comments, COALESCE(b.valor,-1) as valor, COALESCE(to_char(b.pedido_fecha, 'DD/MM/YYYY HH24:MI:SS'),'') as pedido_datetime, COALESCE(b.codigo_consecutivo,'') as codigo_consecutivo, COALESCE(to_char(b.pedido_fecha, 'DD/MM/YYYY'),'') pedido_fecha, to_char(CURRENT_DATE, 'DD/MM/YYYY') as fecha, COALESCE(to_char(b.pedido_fecha, 'HH24:MI:SS'),'') as pedido_hora, 
            COALESCE(c.fc_numero,'') as fc_numero, COALESCE(to_char(c.fc_current, 'DD/MM/YYYY HH24:MI:SS'),'') as fc_current, 
            COALESCE(d.nc_numero,'') as nc_numero, COALESCE(to_char(d.nc_current, 'DD/MM/YYYY HH24:MI:SS'),'') as nc_current, exactus_schema 
            FROM empresas_parametros a 
            LEFT JOIN exactus_pedidos b ON b.documento = '" + HBLNumber + @"' AND b.id_documento = '" + ObjectID + @"' AND b.pais = a.country AND b.id_cargo_system = " + sistema + @" 
            LEFT JOIN exactus_pedidos_fc c ON c.pedido_erp = b.pedido_erp 
            LEFT JOIN exactus_pedidos_nc d ON d.pedido_erp = b.pedido_erp 
            WHERE a.country = '" + CountryExactus + @"' ORDER BY b.id_pedido DESC, c.fc_id DESC " ;

            System.Data.IDataReader reader = Postgres_.GetDataReader(QuerySelect, dbStr);

            var data = Utils.get_list_struct<Struct.CSPedidosData>(reader);

            foreach (Struct.CSPedidosData row in data)
            {
                res.tipo_conta = row.tipo_conta;
                res.esquema = row.exactus_schema;

                res.stat = 1;

                if (row.id_pedido > 0 && res.pedido_erp == "") {

                    if (row.estado == 3){                    
                        res.pedido_erp = row.pedido_erp;                    
                    }

                    row.comments = row.comments.ToString().Replace(">|<",">°<");

                    string[] rows = row.comments.Split('°');

                    foreach (string str in rows)
                    {
                            if (Pedido_Msg.Replace("RED","") == str.Replace("NAVY",""))  
                                Distinto = 0;

                            Msg = str.Replace("\r\n", "").Replace("\t", "");
    
                            if (row.valor == 0 && row.pedido_erp != "" && (row.estado == 3 || row.estado == 5))
                                Msg = Msg.Replace("PROCESO CORRECTO", "SOLICITUD REALIZADA");

                            if (Msg.IndexOf("</div></div>") > 0)
                                Msg = Msg.Replace("</div></div>", " " + (row.pedido_fecha == row.fecha ? row.pedido_hora : row.pedido_datetime) + "</div></div>");
                            else
                                Msg = Msg + "<font face=verdana color=blue style='display:inline'>" + (row.pedido_fecha == row.fecha ? row.pedido_hora : row.pedido_datetime) + "</font><br>";

                            if (Distinto == -1 || Pedido_Msg == "")
                                res.msg = res.msg + Msg; 

                    }
                }
            }
             
            if (Distinto == 0 && Pedido_Msg != "") {
               
                if (Pedido_Msg.ToLower().IndexOf("<br>") > 0)
                    Pedido_Msg = Pedido_Msg.Replace("<br>","");

                if (Pedido_Msg.ToLower().IndexOf("font") == 0) 
                    Pedido_Msg = "<font face=verdana color=blue>" + Pedido_Msg + "</font><br>";

                //res.stat = 0;

                res.msg = res.msg + Pedido_Msg;
            }

            if (res.msg != "")
            {

                res.msg = "<div style='overflow:auto;width:100%;min-height:30px;max-height:60px;border:1px solid silver'>" + res.msg + "</div>";
            }

            return res;

        }

        public static Struct.Result SendAlertas(string tipo, string user, string from, string sistema, string pais_iso, string tipo_plantilla, string subject, string mensaje)
        {
            string query = "";

            if (pais_iso == null)
                pais_iso = "";

            if (pais_iso == "")
                pais_iso = "CRTLA";

            query = @"SELECT 
CASE WHEN (to_ = '' AND cc_  = '' AND bc_ = '') THEN (select a.pw_name||'@'||a.dominio from usuarios_empresas as a where a.pw_name = '" + user + @"') ELSE to_ END as to_, cc_, bc_, subject_, body_, tipo_plantilla, pais_iso 
 
FROM (
        SELECT b.id_correo_grupo, b.tipo_plantilla, 
        string_agg(distinct CASE WHEN correo_cc_bc = 1 THEN d.pw_name || '@' || d.dominio ELSE '' END, CASE WHEN correo_cc_bc = 1 THEN ';' ELSE '' END) as to_ ,
        string_agg(distinct CASE WHEN correo_cc_bc = 2 THEN d.pw_name || '@' || d.dominio ELSE '' END, CASE WHEN correo_cc_bc = 2 THEN ';' ELSE '' END) as cc_ ,
        string_agg(distinct CASE WHEN correo_cc_bc = 3 THEN d.pw_name || '@' || d.dominio ELSE '' END, CASE WHEN correo_cc_bc = 3 THEN ';' ELSE '' END) as bc_ , 
        CASE WHEN b.tipo_plantilla = 1 THEN 'EXCEL' WHEN b.tipo_plantilla = 2 THEN 'ALERTA' WHEN b.tipo_plantilla = 3 THEN 'NOTIFICACION' END b.tipo_plantilla || ' - ' || '' as tipo_plantilla,  
        c.pais_iso, a.siglas, SUBSTR(c.pais_iso,3,3), b.id_correo_grupo, b.subject as subject_, b.body as body_

        FROM exactus_costos_correos_plantillas b 

        INNER JOIN exactus_costos_correos_grupos a ON a.activo = true AND a.id_correo_grupo = b.id_correo_grupo AND a.siglas = SUBSTR('" + pais_iso + @"',3,3)

        LEFT JOIN usuarios_empresas_exactus_correo c ON c.accion = b.tipo_plantilla AND c.activo = true AND c.pais_iso = '" + pais_iso + @"'

        LEFT JOIN usuarios_empresas d ON pw_activo = 1 AND (c.id_usuario = d.id_usuario OR d.pw_name = '" + user + @"')

        WHERE b.activo = true AND b.tipo_plantilla = " + tipo_plantilla + @"

        GROUP BY a.id_correo_grupo, b.tipo_plantilla, c.pais_iso, b.subject, b.body, b.id_correo_grupo, a.siglas, SUBSTR(c.pais_iso,3,3)
) x ";

            // accion 1:excel 2:alerta 3:notificaiones 
            // correo_cc_bc 1:to 2:cc 3:bc

            _plantilla data1 = Postgres_.GetRowPostgres<_plantilla>(dbStr, query);

            data1.to_ = data1.to_.Trim(';');
            data1.cc_ = data1.cc_.Trim(';');
            data1.bc_ = data1.bc_.Trim(';');

            if (data1.to_ == "" && data1.cc_ == "")
            {
                data1.to_ = "hmeckler@grupotla.com";
                mensaje += "<p>No se encontraron contactos para notificar</p>";
            }

            string str = Utils.Base64Encode(data1.body_ + "<p>" + mensaje.Replace("[", "<").Replace("]", ">") + "</p>");

            Struct.Result res = new Struct.Result();
                                        //sendattach( pais_iso, to,         subject,     body,  fromName,  sistema,  user,  ip,  cc,        bc,     attachments)

            if (tipo == "1") { //live
                res = Parametros.sendattach(pais_iso, data1.to_, subject + data1.subject_, str, from,  sistema, user, "", data1.cc_, data1.bc_, "");
               
                //res.stat = res1.stat;
                //res.msg = res1.msg;
            }

            if (tipo == "0") {     // test          
                res.stat = 1;
                res.msg = "Resultado test de contactos";
                res.text = "PAIS_ISO : " + pais_iso + "|USER : " + user + "|PLANTILLA : " + tipo_plantilla + "|TO : " + data1.to_ + "|CC : " + data1.cc_ + "|BC : " + data1.bc_ + "|SQL : " + query;
            }


            return res;

        }








        ////////////////////////////////////////////////// METODO GET DOCUMENT C X P  ////////////////////////////////////////////////////////////////////////
        // desde exactus envian respuesta del envio de costos
        public static Struct._RESPUESTA UpdateDocumentoCXP(string esquema, string id_costo, string numero_erp, string tipo_doc, string documento, string fecha, string master, string proveedor, string valor, string user, string ip)
        {
            Struct._RESPUESTA result = new Struct._RESPUESTA();

            int _res = 0;
    
            string query = "";
            string estado = "";
            string msg = "";
            decimal valor_erp = 0;
        
            result.ASIENTO = "";
            result.CODIGO_ERROR = "";
            result.MENSAJE = "";

            if (id_costo == null) id_costo = "0";
            if (id_costo == "") id_costo = "0";

            numero_erp = numero_erp.Trim();

            _exactus_costos data = null;

            try
            {
                query = "SELECT id_costo, estado, valor, pais_id, pais_iso, id_cargo_system, servicio FROM exactus_costos WHERE id_costo = " + id_costo + " OR (blmaster = '" + master + "' AND proveedor_erp = '" + proveedor + "' AND documento = '" + documento + "')";
                
                data = Postgres_.GetRowPostgres<_exactus_costos>(dbStr, query);

                if (data.id_costo == null)
                {
                        result.CODIGO_ERROR = "91";
                        result.MENSAJE = "Registro no encontrado en cargo system id_costo:" + id_costo + " Master:" + master + " Proveedor ERP:" + proveedor + " Documento:" + documento;

                        Struct.Result res = SendAlertas("1","hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
                        
                       return result;            
                }


                int _id_costo = 0;

                estado = data.estado;

                try
                {
                    _id_costo = int.Parse(data.id_costo);

                }
                catch (Exception ex)
                {
                    result.CODIGO_ERROR = "93";
                    result.MENSAJE = "id_costo " + data.id_costo + " no es valido o no existe en cargo system";

                    Struct.Result res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
                        
                    return result;
                }



                if (decimal.Parse(valor) != decimal.Parse(data.valor)) {

                    result.CODIGO_ERROR = "92";
                    result.MENSAJE = "El valor en este documento " + valor + " no coincide con los costos de excel " + data.valor;

                    Struct.Result res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);

                    return result;
                }



                id_costo = _id_costo.ToString();

                result.COD_COMPANIA = data.pais_id;
                result.COD_PAIS = data.pais_iso;

                string[] split_date = fecha.Split('/');

                if (split_date.Length == 3)
                {

                    string new_date = split_date[2] + "-" + split_date[0].PadLeft(2, '0') + "-" + split_date[1].PadLeft(2, '0');


                    query = @"UPDATE exactus_costos_respuesta SET cr_estado = 5, cr_comentarios = NOW() WHERE id_costo = " + id_costo; //cr_tipo_doc = '" + tipo_doc + "' AND cr_estado < 5 AND cr_blmaster_cs = '" + master + "' AND cr_proveedor_erp = '" + proveedor + "' AND cr_documento_cs = '" + documento + "' AND cr_numero_erp = '" + numero_erp + "' ";

                    Postgres_.EjecutaQuery(query, dbStr);

                    query = @"INSERT INTO exactus_costos_respuesta (
                        cr_tipo_doc, 
						id_costo,
						cr_numero_erp,
						cr_numero_fec,
						cr_blmaster_cs,
						cr_proveedor_erp,
						cr_documento_cs,
						cr_valor,
						cr_estado,
						cr_comentarios,
						cr_usuario_erp,
						cr_usuario_ip	
					) VALUES ( 
'" + tipo_doc + "', " +  id_costo + ", '" + numero_erp + "', '" + new_date + "', '" + master + "', '" + proveedor + "', '" + documento + "', " + valor + ", 1, '', '" + user + "', '" + ip + @"');

					SELECT CAST(currval('exactus_costos_respuesta_cr_id_seq') as text) as _id; ";

                    result.ASIENTO = Postgres_.GetScalar(query, dbStr);

                    if (documento.Trim() == "") documento = "--";

                    Struct.Result res = null;

                    switch (tipo_doc)
                    {

                        case "C": //respuesta cancelacion

                            try
                            {

                                //ESTILO DISPLAY MENSAJES
                                //msg = ", pedido = pedido || '|" + FRow1 + FReg1 + " CANCELACION : " + FReg2 + FBold1 + numero_erp + FBold2 + FReg1 + " FECHA : " + new_date + " (' || NOW() || ') " + FReg2 + FRow2 + "'";
										
								msg = "";

                                //cancelacion direct
                                query = "UPDATE exactus_costos SET estado = 5, numero_erp = '" + numero_erp + "', esquema_erp = '" + esquema + "' " + msg + " WHERE id_costo = " + id_costo + " AND estado < 5";
                                _res = Postgres_.EjecutaQuery(query, dbStr);

                                if (_res > 0)
                                {
                                    switch (data.id_cargo_system)
                                    {
                                        case "1":
                                            query = "UPDATE Costs SET cxp_exactus_id = 0, Expired = 0 WHERE cxp_exactus_id = " + id_costo + " AND DocTyp = " + (data.servicio == "AE" ? "1" : "2");
                                            _res = MySql_.EjecutaQuery(query, "aereo");
                                            break;

                                        case "2":
                                            query = "UPDATE Costs SET cxp_exactus_id = 0, Expired = 0 WHERE cxp_exactus_id = " + id_costo + " ";
                                            _res = MySql_.EjecutaQuery(query, "terrestre");
                                            break;
                                    }
                                } else {
                                    result.CODIGO_ERROR = "98";
                                    result.MENSAJE = "error al intentar cancelar costo CS : " + "El documento en CS ya esta cancelado o no fue encontrado";
                                }

                            }
                            catch (Exception ex)
                            {
                                result.CODIGO_ERROR = "97";
                                result.MENSAJE = "Error al intentar cancelar costo CS : " + ex.Message;

                                res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
                            }

                            break;



                        case "R": //respuesta 
                        case "FAC": //factura

                            try
                            {
                                _res = int.Parse(result.ASIENTO);

                                //ESTILO DISPLAY MENSAJES
                                //msg = ", pedido = pedido || '|" + FRow1 + FReg1 + id_costo + " FACTURA : " + FReg2 + FBold1 + documento + FBold2 + FReg1 +  " FECHA : ' || to_char(CURRENT_DATE, 'DD/MM/YYYY') || '" + FReg2 + FRow2 + "'";
										
								msg = "";

                                /////////////////////// ACTUALIZA EL MENSAJE CON ESTE REGISTRO
                                query = "UPDATE exactus_costos SET estado = 3, numero_erp = '" + numero_erp + "', esquema_erp = '" + esquema + "' " + msg + " WHERE id_costo = " + id_costo + " AND estado < 5";
                                _res = Postgres_.EjecutaQuery(query, dbStr);
                            }
                            catch (Exception ex)
                            {
                                result.CODIGO_ERROR = "98";
                                result.MENSAJE = "Error al intentar guardar el registro : " + ex.Message;

                                res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);

                            }

                            break;


                        default:
                            result.CODIGO_ERROR = "96";
                            result.MENSAJE = "Tipo de documento no clasificado " + tipo_doc;
                            res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
                            break;
                    }
                }
                else
                {
                    result.CODIGO_ERROR = "95";
                    result.MENSAJE = "Verifique formato de fecha " + fecha;

                    Struct.Result res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
                }
             
                
            }
            catch (Exception ex)
            {
                result.CODIGO_ERROR = "94";
                result.MENSAJE = ex.Message;


                Struct.Result res = SendAlertas("1", "hmeckler", "CS", "Integracion CS-Exactus", "", "3", "CXP", result.MENSAJE);
            }



            if (result.CODIGO_ERROR == "" || result.MENSAJE == "") {

                result.CODIGO_ERROR = "99";
                result.ESTADO = "CORRECTO";
                result.MENSAJE = "Operacion registrada correctamente";           
            }



            return result;
        }








        ////////////////////////////////////////////////// SEGMENTO PARA TEST DEL WEB SERVICE PUBLICADO ////////////////////////////////////////////////////////////////////////


        public System.Net.HttpWebRequest CreateSOAPWebRequest()
        {

            string url = "";

            //url = @"http://10.10.1.21:9093/SendParametros.asmx";

            url = @"http://localhost:4343/SendParametros.asmx";

            //Making Web Request    
            System.Net.HttpWebRequest Req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            //SOAPAction    
            //Req.Headers.Add(@"SOAPAction:http://tempuri.org/Addition");
            //Content_type    
            Req.ContentType = "text/xml;charset=\"utf-8\"";
            //Req.Accept = "text/xml";
            //HTTP method    
            Req.Method = "POST";
            //return HttpWebRequest    
            return Req;
        }



        public string InvokeService()
        {
            string ServiceResult = "";


            try
            {

                string str = @"<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">  <soap:Header>    <AuthHeader xmlns=""http://tempuri.org/""><Username>soportetla01</Username><Password>ald847fhe637</Password></AuthHeader></soap:Header><soap:Body><ExactusGetDocumento xmlns=""http://tempuri.org/""><id_pedido>411</id_pedido><pedido_erp>AFRI000027</pedido_erp><tipo_doc>F</tipo_doc><documento>00100001010000073402</documento><fecha>12/06/2021</fecha><valor>2720.83</valor><impuesto>65.61</impuesto><accion>1</accion><fc_numero></fc_numero></ExactusGetDocumento></soap:Body></soap:Envelope>";


/*                
str = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">			 
<soap:Body>
<ExactusSetPedidos xmlns=""http://tempuri.org/"">
<product>2</product>
<sub_product></sub_product>
<impex></impex>
<bl_id>99488</bl_id>
<status_id>0</status_id>
<produccion>0</produccion>
<user>soporte7</user>
<ip>0:0:1</ip>
</ExactusSetPedidos>
</soap:Body>
</soap:Envelope>";
*/

                /*
                str = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">			 
<soap:Body>
<ExactusCatalogos xmlns=""http://tempuri.org/"">
<NombreCatalogo>BODEGA</NombreCatalogo>
</ExactusCatalogos>
</soap:Body>
</soap:Envelope>";




str = @"<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">  <soap:Header>    <AuthHeader xmlns=""http://tempuri.org/""><Username>soportetla01</Username><Password>ald847fhe637</Password></AuthHeader></soap:Header><soap:Body><ExactusGetDocumento xmlns=""http://tempuri.org/""><id_pedido>204</id_pedido><pedido_erp>TIIM019210</pedido_erp><tipo_doc>D</tipo_doc><documento>00100001030000002432</documento><fecha>06/25/2021</fecha><valor>400</valor><impuesto>0</impuesto><accion>1</accion><fc_numero> </fc_numero></ExactusGetDocumento></soap:Body></soap:Envelope>";


str = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
<soap:Header>
<AuthHeader xmlns=""http://tempuri.org/"">
<Username>soportetla01</Username>
<Password>ald847fhe637</Password>
</AuthHeader>
</soap:Header>
<soap:Body>
<ExactusGetDocumento xmlns=""http://tempuri.org/"">
<id_pedido>1</id_pedido>
<pedido_erp>PED0001</pedido_erp>
<tipo_doc>NCP</tipo_doc>
<documento>NC888</documento>
<fecha>06/22/2021</fecha>
<valor>120.01</valor>
<impuesto>12.84</impuesto>
<accion>1</accion>
<fc_numero>XXX0001</fc_numero>
</ExactusGetDocumento>
</soap:Body>
</soap:Envelope>";
                */


                /*
//////////////// NOTA DE CREDITO PARCIAL
str = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
<soap:Header>
<AuthHeader xmlns=""http://tempuri.org/"">
<Username>soportetla01</Username>
<Password>ald847fhe637</Password>
</AuthHeader>
</soap:Header>
<soap:Body>
<ExactusGetDocumento xmlns=""http://tempuri.org/"">
<id_pedido>94</id_pedido>
<pedido_erp>00100001080000000029</pedido_erp>
<tipo_doc>D</tipo_doc>
<documento>00200001030000002433</documento>
<fecha>06/28/2021</fecha>
<valor>100</valor>
<impuesto>0</impuesto>
<accion>1</accion>
<fc_numero></fc_numero>
</ExactusGetDocumento>
</soap:Body>
</soap:Envelope>";
                
                 */

/////////////////// CATALOGO BODEGAS
//str = @"<?xml version=""1.0"" encoding=""utf-8""?><soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""><soap:Body><ExactusCatalogos xmlns=""http://tempuri.org/""><NombreCatalogo>BODEGA</NombreCatalogo></ExactusCatalogos></soap:Body></soap:Envelope>";


                str = str.Replace("\r\n", "").Replace("\t", "");


                Struct._RESPUESTA responseObject = new Struct._RESPUESTA();

                //Calling CreateSOAPWebRequest method    
                System.Net.HttpWebRequest request = CreateSOAPWebRequest();

                System.Xml.XmlDocument SOAPReqBody = new System.Xml.XmlDocument();
                //SOAP Body Request    
                SOAPReqBody.LoadXml(str);

                using (System.IO.Stream stream = request.GetRequestStream())
                {
                    SOAPReqBody.Save(stream);
                }

                System.Xml.Linq.XDocument xml;

                //Geting response from request    
                using (System.Net.WebResponse Serviceres = request.GetResponse())
                {
                    using (System.IO.StreamReader rd = new System.IO.StreamReader(Serviceres.GetResponseStream()))
                    {
                        //reading stream    
                        ServiceResult = rd.ReadToEnd();

                        xml = System.Xml.Linq.XDocument.Parse(ServiceResult);

                        /* FUNCIONALIDAD OK PARA ExactusGetDocumento
                        responseObject = xml.Descendants().Where(x => x.Name.LocalName == "ExactusSetPedidosResult").Select(x => new Struct._RESPUESTA()
                        {
                            ASIENTO = (string)x.Element(x.Name.Namespace + "ASIENTO"),
                            COD_COMPANIA = (string)x.Element(x.Name.Namespace + "COD_COMPANIA"),
                            COD_PAIS = (string)x.Element(x.Name.Namespace + "COD_PAIS"),
                            ESTADO = (string)x.Element(x.Name.Namespace + "ESTADO"),
                            MENSAJE = (string)x.Element(x.Name.Namespace + "MENSAJE")
                        }).FirstOrDefault();
                        */

                        ServiceResult = xml.ToString();

                    }
                }
            }
            catch (Exception ex)
            {
                ServiceResult = ex.Message;

            }

            return ServiceResult;
        }


        public static Struct._exactus_webservices_users GetUserWS(string ws, string usr, string pass)
        {

            string query = "SELECT wbus_id, wbus_nombre, wbus_user, wbus_pass, wbus_usuario, wbus_empresa, wbus_url, wbus_estado, wbus_descripcion FROM exactus_webservices_users WHERE wbus_nombre = '" + ws + "' AND wbus_estado = 1 AND wbus_user = '" + usr + "' AND wbus_pass = '" + pass + "'";

            Struct._exactus_webservices_users data = Postgres_.GetRowPostgres<Struct._exactus_webservices_users>(dbStr, query);

            return data;
        }





        public Struct._RESPUESTA InvokeService2()
        {
            string ServiceResult = "";

            Struct._RESPUESTA responseObject = new Struct._RESPUESTA();

            //Calling CreateSOAPWebRequest method    
            System.Net.HttpWebRequest request = CreateSOAPWebRequest();

            string str = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Header>
    <AuthHeader xmlns=""http://tempuri.org/"">
      <Username>soportetla01</Username>
      <Password>ald847fhe637</Password>
    </AuthHeader>
  </soap:Header>
  <soap:Body>
    <ExactusGetDocumento xmlns=""http://tempuri.org/"">
      <id_pedido>1</id_pedido>
      <tipo_doc>NCP</tipo_doc>
      <documento>NC888</documento>
      <fecha>06/22/2021</fecha>
      <valor>20.01</valor>
      <accion>1</accion>
    </ExactusGetDocumento>
  </soap:Body>
</soap:Envelope>";


            str = str.Replace("\r\n", "");



            System.Xml.XmlDocument SOAPReqBody = new System.Xml.XmlDocument();
            //SOAP Body Request    
            SOAPReqBody.LoadXml(str);

            using (System.IO.Stream stream = request.GetRequestStream())
            {
                SOAPReqBody.Save(stream);
            }

            //Geting response from request    
            using (System.Net.WebResponse Serviceres = request.GetResponse())
            {
                using (System.IO.StreamReader rd = new System.IO.StreamReader(Serviceres.GetResponseStream()))
                {
                    //reading stream    
                    ServiceResult = rd.ReadToEnd();

                    System.Xml.Linq.XDocument xml = System.Xml.Linq.XDocument.Parse(ServiceResult);

                    // FUNCIONALIDAD OK PARA ExactusGetDocumento
                    responseObject = xml.Descendants().Where(x => x.Name.LocalName == "ExactusSetPedidosResult").Select(x => new Struct._RESPUESTA()
                    {
                        ASIENTO = (string)x.Element(x.Name.Namespace + "ASIENTO"),
                        COD_COMPANIA = (string)x.Element(x.Name.Namespace + "COD_COMPANIA"),
                        COD_PAIS = (string)x.Element(x.Name.Namespace + "COD_PAIS"),
                        ESTADO = (string)x.Element(x.Name.Namespace + "ESTADO"),
                        MENSAJE = (string)x.Element(x.Name.Namespace + "MENSAJE")
                    }).FirstOrDefault();
                }
            }

            return responseObject;
        }





        //////////////////// PROCESA DOCUMENTOS CXC CXP


        public static Struct._RESPUESTA ProcesoGetDocumento(string tipo, string path, Struct._ProcesoGetDocumento dato)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();

            res.PEDIDO = "-";
            res.PEDIDO_EXACTUS = "-";

            string ip = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[1].MapToIPv4().ToString();

            string str = "";

            try
            {

                Postgres_.EjecutaQuery("UPDATE empresas_parametros SET fecha_marca_endpoint = NOW(), modulo_endpoint = '" + tipo + "' WHERE exactus_schema = '" + dato.esquema + "'", dbStr);
            

                str = @"
DOCUMENTO RECIBIDO
-------------------------------------" + tipo + " - " + ip + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"--------------------------------------

<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Header>
    <AuthHeader xmlns=""http://tempuri.org/"">
      <Username>" + dato.server_user + @"</Username>
      <Password>" + dato.server_pass + @"</Password>
    </AuthHeader>
  </soap:Header>
  <soap:Body>";

                if (tipo == "CXC") {

                    res.PEDIDO = dato.id_pedido;
                    res.PEDIDO_EXACTUS = dato.pedido_erp;

                    str += @"
    <ExactusGetDocumento xmlns=""http://tempuri.org/"">
      <esquema>" + dato.esquema + @"</esquema>
      <id_pedido>" + dato.id_pedido + @"</id_pedido>
      <pedido_erp>" + dato.pedido_erp + @"</pedido_erp>
      <tipo_doc>" + dato.tipo_doc + @"</tipo_doc>
      <documento>" + dato.documento + @"</documento>
      <fecha>" + dato.fecha + @"</fecha>
      <valor>" + dato.valor + @"</valor>
      <impuesto>" + dato.impuesto + @"</impuesto>
      <accion>" + dato.accion + @"</accion>
      <fc_numero>" + dato.fc_numero + @"</fc_numero>
    </ExactusGetDocumento>";            
            }

            if (tipo == "CXP") {

                res.PEDIDO = dato.id_costo;
                res.PEDIDO_EXACTUS = dato.numero_erp;

                    str += @"
    <ExactusGetDocumentoCXP xmlns=""http://tempuri.org/"">
      <esquema>" + dato.esquema + @"</esquema>
      <id_costo>" + dato.id_costo + @"</id_costo>
      <numero_erp>" + dato.numero_erp + @"</numero_erp>
      <tipo_doc>" + dato.tipo_doc + @"</tipo_doc>
      <documento>" + dato.documento + @"</documento>
      <fecha>" + dato.fecha + @"</fecha>
      <valor>" + dato.valor + @"</valor>
      <proveedor>" + dato.proveedor + @"</impuesto>
      <master>" + dato.master + @"</accion>
      <user>" + dato.user + @"</fc_numero>
      <ip>" + dato.ip + @"</fc_numero>
    </ExactusGetDocumentoCXP>";
               
            }


    str += @"
  </soap:Body>
</soap:Envelope>
";


                res.DESCRIPCION = Utils.EscribirEnDirectorio(path, dato.esquema + "_" + tipo + "_" + ip + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", str);

                if (tipo == "CXC")
                    res = UpdateDocumentoCXC(dato.esquema, dato.id_pedido, dato.pedido_erp, dato.tipo_doc, dato.documento, dato.fecha, dato.valor, dato.impuesto, dato.accion, dato.fc_numero, dato.server_user);

                if (tipo == "CXP")
                    res = UpdateDocumentoCXP(dato.esquema, dato.id_costo, dato.numero_erp, dato.tipo_doc, dato.documento, dato.fecha, dato.master, dato.proveedor, dato.valor, dato.user, dato.ip);
                    //recibe UpdateDocumentoCXP(esquema, id_costo, numero_erp, tipo_doc, documento, fecha, master, proveedor, valor, user, ip)
        
            }
            catch (Exception ex)
            {
                res.CODIGO_ERROR = "104";
                res.ESTADO = "ERROR";
                res.MENSAJE = ex.Message;
            }

            //str = Newtonsoft.Json.JsonConvert.DeserializeObject<Struct._RESPUESTA>(res);

            str = Newtonsoft.Json.JsonConvert.SerializeObject(res).ToString();

            str = @"

RESPUESTA                 
-------------------------------------" + tipo + " - " + ip + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"--------------------------------------

" + str;

            res.DESCRIPCION = Utils.EscribirEnDirectorio(path, dato.esquema + "_" + tipo + "_" + ip + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", str);

            return res;
        }


        /* ProcesoGetDocumento2
        public static Struct._RESPUESTA ProcesoGetDocumento2(string esquema, string tipo, string server_user, string server_pass, string id_pedido, string pedido_erp, string tipo_doc, string documento, string fecha, string valor, string impuesto, string accion, string fc_numero)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();

            res.PEDIDO = id_pedido;
            res.PEDIDO_EXACTUS = pedido_erp;

            Postgres_.EjecutaQuery("UPDATE empresas_parametros SET fecha_marca_endpopint = NOW(), modulo_endpoint = '" + tipo + "' WHERE exactus_schema = '" + esquema + "'", dbStr);

            string ip = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[1].MapToIPv4().ToString();

            string str = "";

            try
            {

                str = @"
DOCUMENTO RECIBIDO
-------------------------------------" + tipo + " - " + ip + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"--------------------------------------

<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Header>
    <AuthHeader xmlns=""http://tempuri.org/"">
      <Username>" + server_user + @"</Username>
      <Password>" + server_pass + @"</Password>
    </AuthHeader>
  </soap:Header>
  <soap:Body>
    <ExactusGetDocumento xmlns=""http://tempuri.org/"">
      <esquema>" + esquema + @"</esquema>
      <id_pedido>" + id_pedido + @"</id_pedido>
      <pedido_erp>" + pedido_erp + @"</pedido_erp>
      <tipo_doc>" + tipo_doc + @"</tipo_doc>
      <documento>" + documento + @"</documento>
      <fecha>" + fecha + @"</fecha>
      <valor>" + valor + @"</valor>
      <impuesto>" + impuesto + @"</impuesto>
      <accion>" + accion + @"</accion>
      <fc_numero>" + fc_numero + @"</fc_numero>
    </ExactusGetDocumento>

    <ExactusGetDocumentoCXP xmlns=""http://tempuri.org/"">
      <esquema>" + esquema + @"</esquema>
      <id_costo>" + id_pedido + @"</id_costo>
      <numero_erp>" + pedido_erp + @"</numero_erp>
      <tipo_doc>" + tipo_doc + @"</tipo_doc>
      <documento>" + documento + @"</documento>
      <fecha>" + fecha + @"</fecha>
      <valor>" + valor + @"</valor>
      <proveedor>" + fc_numero + @"</impuesto>
      <master>" + accion + @"</accion>
      <user>" + server_user + @"</fc_numero>
      <ip>" + fc_numero + @"</fc_numero>
    </ExactusGetDocumentoCXP>


  </soap:Body>
</soap:Envelope>
";




                System.IO.File.AppendAllText(@"C:\Logs\UpdateDocumentoExactus\" + tipo + "_" + ip + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", str + Environment.NewLine);

                if (tipo == "CXC")
                    res = UpdateDocumentoCXC(esquema, id_pedido, pedido_erp, tipo_doc, documento, fecha, valor, impuesto, accion, fc_numero, server_user);

                if (tipo == "CXP")
                    res = UpdateDocumentoCXP(esquema, id_pedido, pedido_erp, tipo_doc, documento, fecha, accion, fc_numero, valor, server_user, server_pass);
                //recibe UpdateDocumentoCXP(esquema, id_costo, numero_erp, tipo_doc, documento, fecha, master, proveedor, valor, user, ip)

            }
            catch (Exception ex)
            {
                res.CODIGO_ERROR = "104";
                res.ESTADO = "ERROR";
                res.MENSAJE = ex.Message;
            }

            //str = Newtonsoft.Json.JsonConvert.DeserializeObject<Struct._RESPUESTA>(res);

            str = Newtonsoft.Json.JsonConvert.SerializeObject(res).ToString();

            str = @"

RESPUESTA                 
-------------------------------------" + tipo + " - " + ip + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"--------------------------------------

" + str;


            System.IO.File.AppendAllText(@"C:\Logs\UpdateDocumentoExactus\" + tipo + "_" + ip + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", str + Environment.NewLine);


            return res;
        }

        */
        #region para query de pruebas



        /*
        public static string QryPedido = @"
		SELECT 
	'PRUEBA' as EMPRESA, 
	'PRUEBA' as MOVIMIENTO, 
	'PRUEBA' as TIPO_CARGA, 
	'PRUEBA' as PEDIDO, 
	'PRUEBA' as BODEGA, 
	'PRUEBA' as CLIENTE, 
	'PRUEBA' as RUTA, 
	'PRUEBA' as ZONA, 
	'PRUEBA' as PAIS, 
	'PRUEBA' as NIVEL_PRECIO, 
	'PRUEBA' as MONEDA, 
	'PRUEBA' as VENDEDOR, 
	'PRUEBA' as COBRADOR, 
	'PRUEBA' as CONDICION_PAGO, 
	'PRUEBA' as ESTADO, 
	'PRUEBA' as FECHA_PEDIDO, 
	'PRUEBA' as FECHA_PROMETIDA, 
	'PRUEBA' as FECHA_PROX_EMBARQU, 
	'PRUEBA' as FECHA_ULT_EMBARQUE, 
	'PRUEBA' as FECHA_ULT_CANCELAC, 
	'PRUEBA' as ORDEN_COMPRA, 
	'PRUEBA' as FECHA_ORDEN, 
	'PRUEBA' as TARJETA_CREDITO, 
	'PRUEBA' as EMBARCAR_A, 
	'PRUEBA' as DIREC_EMBARQUE, 
	'PRUEBA' as DIRECCION_FACTURA, 
	'PRUEBA' as RUBRO1, 
	'PRUEBA' as RUBRO2, 
	'PRUEBA' as RUBRO3, 
	'PRUEBA' as RUBRO4, 
	'PRUEBA' as RUBRO5, 
	'PRUEBA' as OBSERVACIONES, 
	'PRUEBA' as COMENTARIO_CXC, 
	'PRUEBA' as TOTAL_MERCADERIA, 
	'PRUEBA' as MONTO_ANTICIPO, 
	'PRUEBA' as MONTO_FLETE, 
	'PRUEBA' as MONTO_SEGURO, 
	'PRUEBA' as MONTO_DOCUMENTACIO, 
	'PRUEBA' as TIPO_DESCUENTO1, 
	'PRUEBA' as TIPO_DESCUENTO2, 
	'PRUEBA' as MONTO_DESCUENTO1, 
	'PRUEBA' as MONTO_DESCUENTO2, 
	'PRUEBA' as PORC_DESCUENTO1, 
	'PRUEBA' as PORC_DESCUENTO2, 
	'PRUEBA' as TOTAL_IMPUESTO1, 
	'PRUEBA' as TOTAL_IMPUESTO2, 
	'PRUEBA' as TOTAL_A_FACTURAR, 
	'PRUEBA' as PORC_COMI_VENDEDOR, 
	'PRUEBA' as PORC_COMI_COBRADOR, 
	'PRUEBA' as TOTAL_CANCELADO, 
	'PRUEBA' as TOTAL_UNIDADES, 
	'PRUEBA' as IMPRESO, 
	'PRUEBA' as USUARIO, 
	'PRUEBA' as FECHA_HORA, 
	'PRUEBA' as DESCUENTO_VOLUMEN, 
	'PRUEBA' as TIPO_PEDIDO, 
	'PRUEBA' as MONEDA_PEDIDO, 
	'PRUEBA' as CLASE_PEDIDO, 
	'PRUEBA' as TIPO_DOC_CXC, 
	'PRUEBA' as SUBTIPO_DOC_CXC, 
	'PRUEBA' as VERSION_NP, 
	'PRUEBA' as AUTORIZADO, 
	'PRUEBA' as DOC_A_GENERAR, 
	'PRUEBA' as CLIENTE_ORIGEN, 
	'PRUEBA' as CLIENTE_CORPORAC, 
	'PRUEBA' as CLIENTE_DIRECCION, 
	'PRUEBA' as f, 
	'PRUEBA' as DESCUENTO_CASCADA, 
	'PRUEBA' as CONTRATO, 
	'PRUEBA' as PORC_INTCTE, 
	'PRUEBA' as NOTEEXISTSFLAG, 
	'PRUEBA' as RECORDDATE, 
	'PRUEBA' as CREATEDBY, 
	'PRUEBA' as CREATEDATE, 
	'PRUEBA' as UPDATEDBY, 
	'PRUEBA' as TIPO_CAMBIO, 
	'PRUEBA' as FIJAR_TIPO_CAMBIO, 
	'PRUEBA' as ROWPOINTER, 
	'PRUEBA' as ORIGEN_PEDIDO, 
	'PRUEBA' as DESC_DIREC_EMBARQUE, 
	'PRUEBA' as DIVISION_GEOGRAFICA1, 
	'PRUEBA' as DIVISION_GEOGRAFICA2, 
	'PRUEBA' as BASE_IMPUESTO1, 
	'PRUEBA' as BASE_IMPUESTO2, 
	'PRUEBA' as NOMBRE_CLIENTE, 
	'PRUEBA' as FECHA_PROYECTADA, 
	'PRUEBA' as FECHA_APROBACION, 
	'PRUEBA' as TIPO_DOCUMENTO, 
	'PRUEBA' as VERSION_COTIZACION, 
	'PRUEBA' as RAZON_CANCELA_COTI, 
	'PRUEBA' as DES_CANCELA_COTI, 
	'PRUEBA' as CAMBIOS_COTI, 
	'PRUEBA' as COTIZACION_PADRE, 
	'PRUEBA' as TASA_IMPOSITIVA, 
	'PRUEBA' as TASA_IMPOSITIVA_PORC, 
	'PRUEBA' as TASA_CREE1, 
	'PRUEBA' as TASA_CREE1_PORC, 
	'PRUEBA' as TASA_CREE2, 
	'PRUEBA' as TASA_CREE2_PORC, 
	'PRUEBA' as TASA_GAN_OCASIONAL_PORC, 
	'PRUEBA' as CONTRATO_AC, 
	'PRUEBA' as TIPO_CONTRATO_AC, 
	'PRUEBA' as PERIODICIDAD_CONTRATO_AC, 
	'PRUEBA' as FECHA_CONTRATO_AC, 
	'PRUEBA' as FECHA_INICIO_CONTRATO_AC, 
	'PRUEBA' as FECHA_PROXFAC_CONTRATO_AC, 
	'PRUEBA' as FECHA_FINFAC_CONTRATO_AC, 
	'PRUEBA' as FECHA_ULTAUMENTO_CONTRATO_AC, 
	'PRUEBA' as FECHA_PROXFACSIST_CONTRATO_AC, 
	'PRUEBA' as DIFERIDO_CONTRATO_AC, 
	'PRUEBA' as TOTAL_CONTRATO_AC, 
	'PRUEBA' as CONTRATO_REVENTA, 
	'PRUEBA' as USR_NO_APRUEBA, 
	'PRUEBA' as FECHA_NO_APRUEBA, 
	'PRUEBA' as RAZON_DESAPRUEBA, 
	'PRUEBA' as MODULO, 
	'PRUEBA' as CORREOS_ENVIO, 
	'PRUEBA' as CONTRATO_VIGENCIA_DESDE, 
	'PRUEBA' as CONTRATO_VIGENCIA_HASTA, 
	'PRUEBA' as USO_CFDI, 
	'PRUEBA' as FORMA_PAGO, 
	'PRUEBA' as CLAVE_REFERENCIA_DE, 
	'PRUEBA' as FECHA_REFERENCIA_DE, 
	'PRUEBA' as U_ENVIADO_TLA, 
	'PRUEBA' as TIPO_OPERACION, 
	'PRUEBA' as INCOTERMS, 
	'PRUEBA' as U_AD_WM_NUMERO_VENDEDOR, 
	'PRUEBA' as U_AD_WM_ENVIAR_GLN, 
	'PRUEBA' as U_AD_WM_NUMERO_RECEPCION, 
	'PRUEBA' as U_AD_WM_NUMERO_RECLAMO, 
	'PRUEBA' as U_AD_WM_FECHA_RECLAMO, 
	'PRUEBA' as U_AD_PC_NUMERO_VENDEDOR, 
	'PRUEBA' as U_AD_PC_ENVIAR_GLN, 
	'PRUEBA' as U_AD_GS_NUMERO_VENDEDOR, 
	'PRUEBA' as U_AD_GS_ENVIAR_GLN, 
	'PRUEBA' as U_AD_GS_NUMERO_RECEPCION, 
	'PRUEBA' as U_AD_GS_FECHA_RECEPCION, 
	'PRUEBA' as U_AD_AM_NUMERO_PROVEEDOR, 
	'PRUEBA' as U_AD_AM_ENVIAR_GLN, 
	'PRUEBA' as U_AD_AM_NUMERO_RECEPCION, 
	'PRUEBA' as U_AD_AM_NUMERO_RECLAMO, 
	'PRUEBA' as U_AD_AM_FECHA_RECLAMO, 
	'PRUEBA' as U_AD_AM_FECHA_RECEPCION, 
	'PRUEBA' as U_AD_CC_REMISION, 
	'PRUEBA' as U_AD_CC_FECHA_CONSUMO, 
	'PRUEBA' as U_AD_CC_HOJA_ENTRADA, 
	'PRUEBA' as U_IVA_CATEGORIA, 
	'PRUEBA' as ACTIVIDAD_COMERCIAL, 
	'PRUEBA' as MONTO_OTRO_CARGO, 
	'PRUEBA' as CODIGO_REFERENCIA_DE, 
	'PRUEBA' as TIPO_REFERENCIA_DE, 
	'PRUEBA' as TIENE_RELACIONADOS, 
	'PRUEBA' as ES_FACTURA_REEMPLAZO, 
	'PRUEBA' as FACTURA_ORIGINAL_REEMPLAZO, 
	'PRUEBA' as CONSECUTIVO_FTC, 
	'PRUEBA' as NUMERO_FTC, 
	'PRUEBA' as NIT_TRANSPORTADOR, 
	'PRUEBA' as NUM_OC_EXENTA, 
	'PRUEBA' as NUM_CONS_REG_EXO, 
	'PRUEBA' as NUM_IRSEDE_AGR_GAN, 
	'PRUEBA' as U_AD_GS_NUMERO_ORDEN, 
	'PRUEBA' as U_AD_GS_FECHA_RECLAMO, 
	'PRUEBA' as U_AD_GS_NUMERO_RECLAMO, 
	'PRUEBA' as U_AD_GS_FECHA_ORDEN, 
	'PRUEBA' as U_AD_WM_NUMERO_ORDEN, 
	'PRUEBA' as U_AD_WM_FECHA_ORDEN, 
	'PRUEBA' as U_AD_PM_ENVIAR_GLN, 
	'PRUEBA' as U_AD_MS_NUMERO_VENDEDOR, 
	'PRUEBA' as U_AD_MS_ENVIAR_GLN, 
	'PRUEBA' as U_AD_MS_NUMERO_RECEPCION, 
	'PRUEBA' as U_AD_MS_NUMERO_RECLAMO, 
	'PRUEBA' as U_AD_MS_FECHA_RECLAMO, 
	'PRUEBA' as TIPO_PAGO, 
	'PRUEBA' as TIPO_DESCUENTO_GLOBAL, 
	'PRUEBA' as TIPO_FACTURA, 
	'PRUEBA' as U_FECHA_DUA_AA, 
	'PRUEBA' as U_PAIS_ORIGEN_AA, 
	'PRUEBA' as U_DIAS_AF, 
	'PRUEBA' as U_EQUIPO_AI, 
	'PRUEBA' as U_CIF_AF, 
	'PRUEBA' as U_PESO_AF, 
	'PRUEBA' as U_IMPUESTOS_AF, 
	'PRUEBA' as U_VOLUMEN_AF, 
	'PRUEBA' as U_BULTOS_DUA_AF, 
	'PRUEBA' as U_TC_AF, 
	'PRUEBA' as U_BL_AF, 
	'PRUEBA' as U_MOVIMIENTO_AF, 
	'PRUEBA' as U_LIQUIDACION_TI, 
	'PRUEBA' as U_DUA_AF, 
	'PRUEBA' as U_SERVICIO_TI, 
	'PRUEBA' as U_RED, 
	'PRUEBA' as U_AGENTE_TI, 
	'PRUEBA' as U_TARIFA_AF, 
	'PRUEBA' as U_TRAMITE_AA, 
	'PRUEBA' as U_ADUANA_AA, 
	'PRUEBA' as U_ASOCIAR_A_PEDIDO_AA, 
	'PRUEBA' as U_LIQUIDACION_AA, 
	'PRUEBA' as U_CLIENTE_TI, 
	'PRUEBA' as U_TRANSPORTISTA_TI, 
	'PRUEBA' as U_REC_ANT_ORIGEN_AA, 
	'PRUEBA' as U_FECHA_MOVIMIENTO_AF, 
	'PRUEBA' as U_CLIENTE_AF, 
	'PRUEBA' as U_AGENCIA, 
	'PRUEBA' as U_AGENCIA_NOM, 
	'PRUEBA' as U_AGENCIA_NIT, 
	'PRUEBA' as U_CONSIG, 
	'PRUEBA' as U_CONSIG_NIT, 
	'PRUEBA' as NUMERO_REGISTRO_IVA, 
	'PRUEBA' as U_COPIA_PAIS,
	null as LINEAS
";

        public static string QryLinea = @"
		SELECT
			'PRUEBA' as PEDIDO, 
			'PRUEBA' as PEDIDO_LINEA, 
			'PRUEBA' as ARTICULO, 
			'PRUEBA' as BODEGA, 
			'PRUEBA' as ESTADO, 
			'PRUEBA' as FECHA_ENTREGA, 
			'PRUEBA' as LINEA_USUARIO, 
			'PRUEBA' as PRECIO_UNITARIO, 
			'PRUEBA' as CANTIDAD_PEDIDA, 
			'PRUEBA' as CANTIDAD_A_FACTURA, 
			'PRUEBA' as CANTIDAD_FACTURADA, 
			'PRUEBA' as CANTIDAD_RESERVADA, 
			'PRUEBA' as CANTIDAD_BONIFICAD, 
			'PRUEBA' as CANTIDAD_CANCELADA, 
			'PRUEBA' as TIPO_DESCUENTO, 
			'PRUEBA' as MONTO_DESCUENTO, 
			'PRUEBA' as PORC_DESCUENTO, 
			'PRUEBA' as DESCRIPCION, 
			'PRUEBA' as COMENTARIO, 
			'PRUEBA' as PEDIDO_LINEA_BONIF, 
			'PRUEBA' as LOTE, 
			'PRUEBA' as LOCALIZACION, 
			'PRUEBA' as UNIDAD_DISTRIBUCIO, 
			'PRUEBA' as FECHA_PROMETIDA, 
			'PRUEBA' as LINEA_ORDEN_COMPRA, 
			'PRUEBA' as NOTEEXISTSFLAG, 
			'PRUEBA' as RECORDDATE, 
			'PRUEBA' as CREATEDBY, 
			'PRUEBA' as CREATEDATE, 
			'PRUEBA' as UPDATEDBY, 
			'PRUEBA' as ROWPOINTER, 
			'PRUEBA' as PROYECTO, 
			'PRUEBA' as FASE, 
			'PRUEBA' as CENTRO_COSTO, 
			'PRUEBA' as CUENTA_CONTABLE, 
			'PRUEBA' as RAZON_PERDIDA, 
			'PRUEBA' as TIPO_DESC, 
			'PRUEBA' as TIPO_IMPUESTO1, 
			'PRUEBA' as TIPO_TARIFA1, 
			'PRUEBA' as TIPO_IMPUESTO2, 
			'PRUEBA' as TIPO_TARIFA2, 
			'PRUEBA' as PORC_EXONERACION, 
			'PRUEBA' as MONTO_EXONERACION, 
			'PRUEBA' as PORC_IMPUESTO1, 
			'PRUEBA' as PORC_IMPUESTO2, 
			'PRUEBA' as ES_OTRO_CARGO, 
			'PRUEBA' as ES_CANASTA_BASICA, 
			'PRUEBA' as PORC_EXONERACION2, 
			'PRUEBA' as MONTO_EXONERACION2, 
			'PRUEBA' as PORC_IMP1_BASE, 
			'PRUEBA' as PORC_IMP2_BASE, 
			'PRUEBA' as TIPO_DESCUENTO_LINEA
			
";
         
        */

        #endregion



        public class _PEDIDO
        {
            public string EMPRESA { get; set; }
            public string MOVIMIENTO { get; set; }
            public string TIPO_CARGA { get; set; }
            public string PEDIDO { get; set; }
            public string PEDIDO_ERP { get; set; }
            public string BODEGA { get; set; }
            public string CLIENTE { get; set; }
            public string RUTA { get; set; }
            public string ZONA { get; set; }
            public string PAIS { get; set; }
            public string NIVEL_PRECIO { get; set; }
            public string MONEDA { get; set; }
            public string VENDEDOR { get; set; }
            public string COBRADOR { get; set; }
            public string CONDICION_PAGO { get; set; }
            public string ESTADO { get; set; }
            public string FECHA_PEDIDO { get; set; }
            public string FECHA_PROMETIDA { get; set; }
            public string FECHA_PROX_EMBARQU { get; set; }
            public string FECHA_ULT_EMBARQUE { get; set; }
            public string FECHA_ULT_CANCELAC { get; set; }
            public string ORDEN_COMPRA { get; set; }
            public string FECHA_ORDEN { get; set; }
            public string TARJETA_CREDITO { get; set; }
            public string EMBARCAR_A { get; set; }
            public string DIREC_EMBARQUE { get; set; }
            public string DIRECCION_FACTURA { get; set; }
            public string RUBRO1 { get; set; }
            public string RUBRO2 { get; set; }
            public string RUBRO3 { get; set; }
            public string RUBRO4 { get; set; }
            public string RUBRO5 { get; set; }
            public string OBSERVACIONES { get; set; }
            public string COMENTARIO_CXC { get; set; }
            public string TOTAL_MERCADERIA { get; set; }
            public string MONTO_ANTICIPO { get; set; }
            public string MONTO_FLETE { get; set; }
            public string MONTO_SEGURO { get; set; }
            public string MONTO_DOCUMENTACIO { get; set; }
            public string TIPO_DESCUENTO1 { get; set; }
            public string TIPO_DESCUENTO2 { get; set; }
            public string MONTO_DESCUENTO1 { get; set; }
            public string MONTO_DESCUENTO2 { get; set; }
            public string PORC_DESCUENTO1 { get; set; }
            public string PORC_DESCUENTO2 { get; set; }
            public string TOTAL_IMPUESTO1 { get; set; }
            public string TOTAL_IMPUESTO2 { get; set; }
            public string TOTAL_A_FACTURAR { get; set; }
            public string PORC_COMI_VENDEDOR { get; set; }
            public string PORC_COMI_COBRADOR { get; set; }
            public string TOTAL_CANCELADO { get; set; }
            public string TOTAL_UNIDADES { get; set; }
            public string IMPRESO { get; set; }
            public string USUARIO { get; set; }
            public string FECHA_HORA { get; set; }
            public string DESCUENTO_VOLUMEN { get; set; }
            public string TIPO_PEDIDO { get; set; }
            public string MONEDA_PEDIDO { get; set; }
            public string CLASE_PEDIDO { get; set; }
            public string TIPO_DOC_CXC { get; set; }
            public string SUBTIPO_DOC_CXC { get; set; }
            public string VERSION_NP { get; set; }
            public string AUTORIZADO { get; set; }
            public string DOC_A_GENERAR { get; set; }
            public string CLIENTE_ORIGEN { get; set; }
            public string CLIENTE_CORPORAC { get; set; }
            public string CLIENTE_DIRECCION { get; set; }
            public string f { get; set; }
            public string DESCUENTO_CASCADA { get; set; }
            public string CONTRATO { get; set; }
            public string PORC_INTCTE { get; set; }
            public string NOTEEXISTSFLAG { get; set; }
            public string RECORDDATE { get; set; }
            public string CREATEDBY { get; set; }
            public string CREATEDATE { get; set; }
            public string UPDATEDBY { get; set; }
            public string TIPO_CAMBIO { get; set; }
            public string FIJAR_TIPO_CAMBIO { get; set; }
            public string ROWPOINTER { get; set; }
            public string ORIGEN_PEDIDO { get; set; }
            public string DESC_DIREC_EMBARQUE { get; set; }
            public string DIVISION_GEOGRAFICA1 { get; set; }
            public string DIVISION_GEOGRAFICA2 { get; set; }
            public string BASE_IMPUESTO1 { get; set; }
            public string BASE_IMPUESTO2 { get; set; }
            public string NOMBRE_CLIENTE { get; set; }
            public string FECHA_PROYECTADA { get; set; }
            public string FECHA_APROBACION { get; set; }
            public string TIPO_DOCUMENTO { get; set; }
            public string VERSION_COTIZACION { get; set; }
            public string RAZON_CANCELA_COTI { get; set; }
            public string DES_CANCELA_COTI { get; set; }
            public string CAMBIOS_COTI { get; set; }
            public string COTIZACION_PADRE { get; set; }
            public string TASA_IMPOSITIVA { get; set; }
            public string TASA_IMPOSITIVA_PORC { get; set; }
            public string TASA_CREE1 { get; set; }
            public string TASA_CREE1_PORC { get; set; }
            public string TASA_CREE2 { get; set; }
            public string TASA_CREE2_PORC { get; set; }
            public string TASA_GAN_OCASIONAL_PORC { get; set; }
            public string CONTRATO_AC { get; set; }
            public string TIPO_CONTRATO_AC { get; set; }
            public string PERIODICIDAD_CONTRATO_AC { get; set; }
            public string FECHA_CONTRATO_AC { get; set; }
            public string FECHA_INICIO_CONTRATO_AC { get; set; }
            public string FECHA_PROXFAC_CONTRATO_AC { get; set; }
            public string FECHA_FINFAC_CONTRATO_AC { get; set; }
            public string FECHA_ULTAUMENTO_CONTRATO_AC { get; set; }
            public string FECHA_PROXFACSIST_CONTRATO_AC { get; set; }
            public string DIFERIDO_CONTRATO_AC { get; set; }
            public string TOTAL_CONTRATO_AC { get; set; }
            public string CONTRATO_REVENTA { get; set; }
            public string USR_NO_Af { get; set; }
            public string FECHA_NO_Af { get; set; }
            public string RAZON_DESAf { get; set; }
            public string MODULO { get; set; }
            public string CORREOS_ENVIO { get; set; }
            public string CONTRATO_VIGENCIA_DESDE { get; set; }
            public string CONTRATO_VIGENCIA_HASTA { get; set; }
            public string USO_CFDI { get; set; }
            public string FORMA_PAGO { get; set; }
            public string CLAVE_REFERENCIA_DE { get; set; }
            public string FECHA_REFERENCIA_DE { get; set; }
            public string U_ENVIADO_TLA { get; set; }
            public string TIPO_OPERACION { get; set; }
            public string INCOTERMS { get; set; }
            public string U_AD_WM_NUMERO_VENDEDOR { get; set; }
            public string U_AD_WM_ENVIAR_GLN { get; set; }
            public string U_AD_WM_NUMERO_RECEPCION { get; set; }
            public string U_AD_WM_NUMERO_RECLAMO { get; set; }
            public string U_AD_WM_FECHA_RECLAMO { get; set; }
            public string U_AD_PC_NUMERO_VENDEDOR { get; set; }
            public string U_AD_PC_ENVIAR_GLN { get; set; }
            public string U_AD_GS_NUMERO_VENDEDOR { get; set; }
            public string U_AD_GS_ENVIAR_GLN { get; set; }
            public string U_AD_GS_NUMERO_RECEPCION { get; set; }
            public string U_AD_GS_FECHA_RECEPCION { get; set; }
            public string U_AD_AM_NUMERO_PROVEEDOR { get; set; }
            public string U_AD_AM_ENVIAR_GLN { get; set; }
            public string U_AD_AM_NUMERO_RECEPCION { get; set; }
            public string U_AD_AM_NUMERO_RECLAMO { get; set; }
            public string U_AD_AM_FECHA_RECLAMO { get; set; }
            public string U_AD_AM_FECHA_RECEPCION { get; set; }
            public string U_AD_CC_REMISION { get; set; }
            public string U_AD_CC_FECHA_CONSUMO { get; set; }
            public string U_AD_CC_HOJA_ENTRADA { get; set; }
            public string U_IVA_CATEGORIA { get; set; }
            public string ACTIVIDAD_COMERCIAL { get; set; }
            public string MONTO_OTRO_CARGO { get; set; }
            public string CODIGO_REFERENCIA_DE { get; set; }
            public string TIPO_REFERENCIA_DE { get; set; }
            public string TIENE_RELACIONADOS { get; set; }
            public string ES_FACTURA_REEMPLAZO { get; set; }
            public string FACTURA_ORIGINAL_REEMPLAZO { get; set; }
            public string CONSECUTIVO_FTC { get; set; }
            public string NUMERO_FTC { get; set; }
            public string NIT_TRANSPORTADOR { get; set; }
            public string NUM_OC_EXENTA { get; set; }
            public string NUM_CONS_REG_EXO { get; set; }
            public string NUM_IRSEDE_AGR_GAN { get; set; }
            public string U_AD_GS_NUMERO_ORDEN { get; set; }
            public string U_AD_GS_FECHA_RECLAMO { get; set; }
            public string U_AD_GS_NUMERO_RECLAMO { get; set; }
            public string U_AD_GS_FECHA_ORDEN { get; set; }
            public string U_AD_WM_NUMERO_ORDEN { get; set; }
            public string U_AD_WM_FECHA_ORDEN { get; set; }
            public string U_AD_PM_ENVIAR_GLN { get; set; }
            public string U_AD_MS_NUMERO_VENDEDOR { get; set; }
            public string U_AD_MS_ENVIAR_GLN { get; set; }
            public string U_AD_MS_NUMERO_RECEPCION { get; set; }
            public string U_AD_MS_NUMERO_RECLAMO { get; set; }
            public string U_AD_MS_FECHA_RECLAMO { get; set; }
            public string TIPO_PAGO { get; set; }
            public string TIPO_DESCUENTO_GLOBAL { get; set; }
            public string TIPO_FACTURA { get; set; }
            public string U_FECHA_DUA_AA { get; set; }
            public string U_PAIS_ORIGEN_AA { get; set; }
            public string U_DIAS_AF { get; set; }
            public string U_EQUIPO_AI { get; set; }
            public string U_CIF_AF { get; set; }
            public string U_PESO_AF { get; set; }
            public string U_IMPUESTOS_AF { get; set; }
            public string U_VOLUMEN_AF { get; set; }
            public string U_BULTOS_DUA_AF { get; set; }
            public string U_TC_AF { get; set; }
            public string U_BL_AF { get; set; }
            public string U_MOVIMIENTO_AF { get; set; }
            public string U_LIQUIDACION_TI { get; set; }
            public string U_DUA_AF { get; set; }
            public string U_SERVICIO_TI { get; set; }
            public string U_RED { get; set; }
            public string U_AGENTE_TI { get; set; }
            public string U_TARIFA_AF { get; set; }
            public string U_TRAMITE_AA { get; set; }
            public string U_ADUANA_AA { get; set; }
            public string U_ASOCIAR_A_PEDIDO_AA { get; set; }
            public string U_LIQUIDACION_AA { get; set; }
            public string U_CLIENTE_TI { get; set; }
            public string U_TRANSPORTISTA_TI { get; set; }
            public string U_REC_ANT_ORIGEN_AA { get; set; }
            public string U_FECHA_MOVIMIENTO_AF { get; set; }
            public string U_CLIENTE_AF { get; set; }
            public string U_AGENCIA { get; set; }
            public string U_AGENCIA_NOM { get; set; }
            public string U_AGENCIA_NIT { get; set; }
            public string U_CONSIG { get; set; }
            public string U_CONSIG_NIT { get; set; }
            public string NUMERO_REGISTRO_IVA { get; set; }
            public string U_COPIA_PAIS { get; set; }

            public List<_PEDIDO_LINEA> LINEAS { get; set; }
        }

        public class _PEDIDO_LINEA
        {
            public string PEDIDO { get; set; }
            public string PEDIDO_LINEA { get; set; }
            public string ARTICULO { get; set; }
            public string BODEGA { get; set; }
            public string ESTADO { get; set; }
            public string FECHA_ENTREGA { get; set; }
            public string LINEA_USUARIO { get; set; }
            public string PRECIO_UNITARIO { get; set; }
            public string CANTIDAD_PEDIDA { get; set; }
            public string CANTIDAD_A_FACTURA { get; set; }
            public string CANTIDAD_FACTURADA { get; set; }
            public string CANTIDAD_RESERVADA { get; set; }
            public string CANTIDAD_BONIFICAD { get; set; }
            public string CANTIDAD_CANCELADA { get; set; }
            public string TIPO_DESCUENTO { get; set; }
            public string MONTO_DESCUENTO { get; set; }
            public string PORC_DESCUENTO { get; set; }
            public string DESCRIPCION { get; set; }
            public string COMENTARIO { get; set; }
            public string PEDIDO_LINEA_BONIF { get; set; }
            public string LOTE { get; set; }
            public string LOCALIZACION { get; set; }
            public string UNIDAD_DISTRIBUCIO { get; set; }
            public string FECHA_PROMETIDA { get; set; }
            public string LINEA_ORDEN_COMPRA { get; set; }
            public string NOTEEXISTSFLAG { get; set; }
            public string RECORDDATE { get; set; }
            public string CREATEDBY { get; set; }
            public string CREATEDATE { get; set; }
            public string UPDATEDBY { get; set; }
            public string ROWPOINTER { get; set; }
            public string PROYECTO { get; set; }
            public string FASE { get; set; }
            public string CENTRO_COSTO { get; set; }
            public string CUENTA_CONTABLE { get; set; }
            public string RAZON_PERDIDA { get; set; }
            public string TIPO_DESC { get; set; }
            public string TIPO_IMPUESTO1 { get; set; }
            public string TIPO_TARIFA1 { get; set; }
            public string TIPO_IMPUESTO2 { get; set; }
            public string TIPO_TARIFA2 { get; set; }
            public string PORC_EXONERACION { get; set; }
            public string MONTO_EXONERACION { get; set; }
            public string PORC_IMPUESTO1 { get; set; }
            public string PORC_IMPUESTO2 { get; set; }
            public string ES_OTRO_CARGO { get; set; }
            public string ES_CANASTA_BASICA { get; set; }
            public string PORC_EXONERACION2 { get; set; }
            public string MONTO_EXONERACION2 { get; set; }
            public string PORC_IMP1_BASE { get; set; }
            public string PORC_IMP2_BASE { get; set; }
            public string TIPO_DESCUENTO_LINEA { get; set; }

        }

        public class _CONFIG
        {
            public string exconf_documento { get; set; }
            public string exconf_order { get; set; }
            public string exconf_campo { get; set; }
            //public string exconf_separado { get; set; }
            public string exconf_nulo { get; set; }
            public string exconf_tipo_dato { get; set; }
            public string exconf_valor_default { get; set; }
            //public string exconf_comentarios { get; set; }
            //public string exconf_cs { get; set; }
            public string exconf_lista_seleccion { get; set; }
            public string exconf_campo_aereo { get; set; }
            public string exconf_campo_terrestre { get; set; }
            public string exconf_catalogo { get; set; }
        }

        public class _exactus_pedidos // EXACTUS CXC
        {
            public string id_pedido { get; set; }
            public string id_empresa_parametros { get; set; }
            public string fecha_solicitud { get; set; }
            public string documento { get; set; }
            public string id_documento { get; set; }
            public string pais { get; set; }
            public string id_cargo_system { get; set; }
            public string id_usuario { get; set; }
            public string pedido { get; set; }
            public string pedido_erp { get; set; }
            public string pedido_fecha { get; set; }
            public string json_cargo_system { get; set; }
            public string json_exactus { get; set; }
            public string estado { get; set; }
            public string movimiento { get; set; }
            public string codigo_consecutivo { get; set; }
            public string tipo_carga { get; set; }
            public string valor { get; set; }
        }


        public class _exactus_costos //EXACTUS CXP
        {
            public string id_costo { get; set; }
            public string fecha { get; set; }
            public string pais_id { get; set; }
            public string pais_iso { get; set; }
            public string blmaster { get; set; }
            public string proveedor { get; set; }
            public string documento { get; set; }
            public string doc_fecha { get; set; }
            public string moneda { get; set; }
            public string valor { get; set; }
            public string esquema_erp { get; set; }
            public string tipodoc_erp { get; set; }
            public string subtipo_erp { get; set; }
            public string numero_erp { get; set; }
            public string proveedor_erp { get; set; }
            public string moneda_erp { get; set; }
            public string id_cargo_system { get; set; }
            public string estado { get; set; }
            public string usuario_cs { get; set; }
            public string usuario_ip { get; set; }
            public string blid { get; set; }
            public string servicio { get; set; }
        }


        public class _PARAMETROS
        {
            public String exactus_url { get; set; }
            public String exactus_url_username { get; set; }
            public String exactus_url_password { get; set; }
            public String exactus_usuario { get; set; }
            public String cs_producto { get; set; }
            public String cs_sub_producto { get; set; }
            public String id_cliente { get; set; }
            public String id_pais { get; set; }
            public String nombre_cliente { get; set; }
            public String tca_tcambio { get; set; }
            public String routing_no { get; set; }
            public String routing_fecha { get; set; }
            public String id_facturar { get; set; }
            public String transportista { get; set; }
            public String U_AGENTE_TI { get; set; }
            public String U_SERVICIO_TI { get; set; }
            public String pedido_erp { get; set; }
            public String esquema { get; set; }
        }





    }




}