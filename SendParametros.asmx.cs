using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WsParametros
{
    /// <summary>
    /// Descripción breve de SendParametros
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]




    public class SendParametros : System.Web.Services.WebService
    {

        
        [WebMethod]
        public string InvokeService()
        {
            Exactus d = new Exactus();

            return d.InvokeService();
        }
        
         
        /*
        [WebMethod]
        public Struct._RESPUESTA Test2()
        {
            Exactus d = new Exactus();

            return d.InvokeService();
        }
        */


        public string path_files = "WsParametrosLogDocs";


        [WebMethod]
        public string ExactusCatalogos(string NombreCatalogo)
        {
            string select = "";

            List<Struct._RESPUESTA> result = new List<Struct._RESPUESTA>();

            var responseObject = Exactus.SendPedido(null, NombreCatalogo, 41, null);

            select = Newtonsoft.Json.JsonConvert.SerializeObject(responseObject.Result).ToString();

            try
            {
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Struct._RESPUESTA>>(select);

                select = "<option value='-1'>- Seleccione -</option>";

                foreach (Struct._RESPUESTA row in result)
                {
                    if (row.MENSAJE != "" && row.MENSAJE != null) { 

                        select = "<option value='-1'>" + row.MENSAJE.ToUpper() + "</option>";

                        break; 
                    }

                    select += "<option value='" + row.CODIGO + "'>" + row.CODIGO + " - " + row.DESCRIPCION.ToUpper() + "</option>";
                }

            }
            catch (Exception e)
            {
                select = e.Message;
            }
           
            return select;
            
        }



        [WebMethod]
        public Struct.Result TIPO_DOC_CP(string NombreCatalogo, string usuario)
        {
            string select = "", sql = "", dias = "";

            //TIPO_DOC_CP

            Struct.Result res = new Struct.Result();


            res.stat = 1;
            res.msg = "OK";
            res.text = "";

            sql = "SELECT CAST(EXTRACT(DAY FROM age(timestamp 'now()',date(fecha))) AS TEXT) as dias FROM tmp_tipo_documento_exactus LIMIT 1";
            dias = Postgres_.GetScalar(sql, "pruebas");

            if (dias != "0")
            {


                List<Struct._RESPUESTA> result = new List<Struct._RESPUESTA>();

                var responseObject = Exactus.SendPedido(null, NombreCatalogo, 41, null);

                select = Newtonsoft.Json.JsonConvert.SerializeObject(responseObject.Result).ToString();

                try
                {
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Struct._RESPUESTA>>(select);

                    foreach (Struct._RESPUESTA row in result)
                    {
                        if (row.MENSAJE != "" && row.MENSAJE != null)
                        {
                            //return row.MENSAJE.ToUpper();

                            res.stat = 2;
                            res.msg = row.MENSAJE.ToUpper();
                            return res;
                        }
                        break;
                    }

                    /*
                    sql = @"
CREATE TEMPORARY TABLE tmp_tipo_documento_exactus (
    tipo character varying(10),
    subtipo smallint,
    descripcion character varying(100)
);
";



sql = @"
CREATE TABLE tmp_tipo_documento_exactus (
    tipo character varying(10),
    subtipo smallint,
    descripcion character varying(100),
    usuario character varying(20)
);
";

                     
*/



                    sql = "DELETE FROM tmp_tipo_documento_exactus;";

                    foreach (Struct._RESPUESTA row in result)
                    {
                        //sql += "INSERT INTO tmp_tipo_documento_exactus (tipo, subtipo, descripcion) VALUES ('" + row.CODIGO.Split('|')[0] + "', '" + row.CODIGO.Split('|')[1] + "', '" + row.DESCRIPCION + "');";

                        sql += "INSERT INTO tmp_tipo_documento_exactus (tipo, subtipo, descripcion, usuario, fecha) VALUES ('" + row.CODIGO.Split('|')[0] + "', '" + row.CODIGO.Split('|')[1] + "', '" + row.DESCRIPCION + "', '" + usuario + "', CURRENT_DATE);";

                    }

                    select = Postgres_.EjecutaQuery(sql, "pruebas") > 0 ? "OK" : "ERROR";

                    res.msg = "CATALOGO FUE ACTUALIZADO EN ESTE MOMENTO";


                    /*
                    if (tipo != "")
                    {
                        sql += "SELECT subtipo as codigo, descripcion FROM tmp_tipo_documento_exactus WHERE tipo = '" + tipo + "' ORDER BY subtipo";
                    }

                    if (subtipo != "")
                    {

                        sql += "SELECT tipo as codigo, descripcion FROM tmp_tipo_documento_exactus WHERE subtipo = '" + subtipo + "' ORDER BY tipo";
                    }

                    System.Data.IDataReader reader = Postgres_.GetDataReader(sql, "local");

                    List<Struct._RESPUESTA> tipos = Utils.get_list_struct<Struct._RESPUESTA>(reader);


                    select = "<option value=''>- Seleccione -</option>";

                    foreach (Struct._RESPUESTA row in tipos)
                    {
                        select += "<option value='" + row.CODIGO + "'>" + row.CODIGO + " - " + row.DESCRIPCION.ToUpper() + "</option>";
                    }
                    */

                }
                catch (Exception e)
                {
                    res.stat = 3;
                    res.msg = e.Message;
                    return res;
                }

            }
            else {

                res.msg = "CATALOGO ESTA ACTUALIZADO";

            }

            return res;

        }


        [WebMethod]
        public Struct.Result InsertData(string query, string producto)
        {
            Struct.Result res = new Struct.Result();

            res.stat = 1;
            res.msg = "OK";
            res.text = "";

            try
            {

                res.stat = MySql_.EjecutaQuery(query, producto);

                res.msg = res.stat > 0 ? "OK" : "NO ACTUALIZO";

            }
            catch (Exception e)
            {
                res.stat = 3;
                res.msg = e.Message;
                return res;
            }


            return res;

        }



        

        [WebMethod]
        public Struct.arg_pedidos ExactusSetPedidos(string producto, string bodega, string actividad, string condicionpago, string observaciones, string impex, string bl_id, string user, string ip, string Countries, string abierto)
        {
            Struct.arg_pedidos data = new Struct.arg_pedidos();

            //aereo 1 = Export 2 = Import   //3 maritimo E   I

            data.PRODUCTO = producto.ToLower();

            data.BODEGA = string.IsNullOrEmpty(bodega) ? "BOSE" : bodega; 

            data.IMPEX = impex.ToUpper();

            data.BL_ID = string.IsNullOrEmpty(bl_id) ? 0 : int.Parse(bl_id);

            data.ACTIVIDAD_COMERCIAL = string.IsNullOrEmpty(actividad) ? "602001" : actividad;

            data.CONDICION_PAGO = string.IsNullOrEmpty(condicionpago) ? "075" : condicionpago;

            data.OBSERVACIONES = observaciones;
            
            data.user = user;

            data.ip = ip;

            data.COUNTRIES = Countries;

            data.abierto = abierto;

            return Exactus.pedidos(data);
        }


        public class AuthHeader : System.Web.Services.Protocols.SoapHeader
        {
            public string Username;
            public string Password;
        }

        public AuthHeader Authentication;


        /////////////////////////////////////////////////////////////////////  LOG //////////////////////////////////////////////////
        [System.Web.Services.Protocols.SoapHeader("Authentication")]

        [WebMethod] //LOG tipo = CXC / CXP
        public string ExactusGetDocumento_LOG()
        {
            var host = Context.Request.Url.Host; // will get www.mywebsite.com
            var port = Context.Request.Url.Port; // will get the port

            string text = "<table cellpadding=3 cellspacing=3>";

            if (Authentication == null) return "No se envio autenticacion";

            try
            {               
                Struct._exactus_webservices_users data = Exactus.GetUserWS("ExactusGetDocumento", Authentication.Username, Authentication.Password);
                if (Authentication.Username != data.wbus_user || Authentication.Password != data.wbus_pass) return "User or Password not valid";

                text += "<tr><th>FileName</th><th>Size</th><th>Created Date</th></tr>";

			    foreach (var file in Utils.ProcessDirectory(AppDomain.CurrentDomain.BaseDirectory + path_files)) 
                    text += "<tr><td><a href='http:/" + "/" + host + ":" + port + "/" + path_files + "/" + file.Name + "' target=_blank>" + file.Name + "</a></td><td>" + file.Length + "</td><td>" + file.CreationTime + "</td></tr>";                                                      
            }
            catch (Exception ex)
            {
                return ex.Message;                         
            }

            text += "</table>";

            return text;
        }




        /////////////////////////////////////////////////////////////////////  CUENTAS POR COBRAR //////////////////////////////////////////////////
        [System.Web.Services.Protocols.SoapHeader("Authentication")]

        [WebMethod] //CXC
        public Struct._RESPUESTA ExactusGetDocumento(string esquema, string id_pedido, string pedido_erp, string tipo_doc, string documento, string fecha, string valor, string impuesto, string accion, string fc_numero)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();


            if (Authentication == null)
            {
                res.CODIGO_ERROR = "101";
                res.ESTADO = "ERROR";
                res.MENSAJE = "No se autentico.";
                return res;
            }

            Struct._exactus_webservices_users credencial = Exactus.GetUserWS("ExactusGetDocumento", Authentication.Username, Authentication.Password);

            if (credencial == null)
            {
                res.CODIGO_ERROR = "102";
                res.ESTADO = "ERROR";
                res.MENSAJE = "User or Password not valid";
                return res;
            }


            if (string.IsNullOrEmpty(esquema))
            {
                res.CODIGO_ERROR = "103";
                res.ESTADO = "ERROR";
                res.MENSAJE = "El esquema es requerido.";
                return res;
            }

            try
            {

                res.PEDIDO = id_pedido;
                res.PEDIDO_EXACTUS = pedido_erp;

                //string esquema = "TRANSIT";

                Struct._ProcesoGetDocumento args = new Struct._ProcesoGetDocumento();
                args.esquema = esquema;
                args.id_pedido = id_pedido.Trim();
                args.pedido_erp = pedido_erp.Trim();
                args.tipo_doc = tipo_doc.Trim();
                args.documento = documento.Trim();
                args.fecha = fecha.Trim();
                args.valor = valor.Trim();
                args.impuesto = impuesto.Trim();
                args.accion = accion.Trim();
                args.fc_numero = fc_numero.Trim();
 
                args.server_user = Authentication.Username;
                args.server_pass = Authentication.Password;

                if (Authentication.Username == credencial.wbus_user && Authentication.Password == credencial.wbus_pass)
                {

                    res = Exactus.ProcesoGetDocumento("CXC", AppDomain.CurrentDomain.BaseDirectory + path_files, args);

                }
                else
                {
                    res.CODIGO_ERROR = "104";
                    res.ESTADO = "ERROR";
                    res.MENSAJE = "User or Password not valid";
                }
                

            }
            catch (Exception ex)
            {
                res.CODIGO_ERROR = "105";
                res.ESTADO = "ERROR";
                res.MENSAJE = ex.Message;
            }

            
            return res;
        }




        /////////////////////////////////////////////////////////////////////  CUETAS POR PAGAR /////////////////////////////////////////////////////
        [System.Web.Services.Protocols.SoapHeader("Authentication")]

        [WebMethod] //CXP
        public Struct._RESPUESTA ExactusGetDocumentoCXP(string esquema, string id_costo, string numero_erp, string tipo_doc, string documento, string fecha, string master, string proveedor, string valor, string user, string ip)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();

            if (Authentication == null) 
            {
                res.CODIGO_ERROR = "101";
                res.ESTADO = "ERROR";
                res.MENSAJE = "No se autentico.";
                return res;
            }

            Struct._exactus_webservices_users credencial = Exactus.GetUserWS("ExactusGetDocumento", Authentication.Username, Authentication.Password);

            if (credencial == null)
            {
                res.CODIGO_ERROR = "102";
                res.ESTADO = "ERROR";
                res.MENSAJE = "User or Password not valid";
                return res;
            }

            if (string.IsNullOrEmpty(esquema))
            {
                res.CODIGO_ERROR = "103";
                res.ESTADO = "ERROR";
                res.MENSAJE = "El esquema es requerido.";
                return res;
            }

            try
            {

                res.PEDIDO = id_costo;
                res.PEDIDO_EXACTUS = numero_erp;

                Struct._ProcesoGetDocumento args = new Struct._ProcesoGetDocumento();
                args.esquema = esquema;
                args.id_costo = id_costo.Trim();
                args.numero_erp = numero_erp.Trim();
                args.tipo_doc = tipo_doc.Trim();
                args.documento = documento.Trim();
                args.fecha = fecha.Trim();
                args.valor = valor.Trim();

                args.master = master.Trim();
                args.proveedor = proveedor.Trim();
                args.user = user.Trim();
                args.ip = ip.Trim();

                args.server_user = Authentication.Username;
                args.server_pass = Authentication.Password;

                if (Authentication.Username == credencial.wbus_user && Authentication.Password == credencial.wbus_pass)
                {

                    res = Exactus.ProcesoGetDocumento("CXP", AppDomain.CurrentDomain.BaseDirectory + path_files, args);

                }
                else
                {
                    res.CODIGO_ERROR = "103";
                    res.ESTADO = "ERROR";
                    res.MENSAJE = "User or Password not valid";
                }

            }
            catch (Exception ex)
            {
                res.CODIGO_ERROR = "104";
                res.ESTADO = "ERROR";
                res.MENSAJE = ex.Message;
            }

            return res;
        }




        /*
        [WebMethod] //CXC - NO AUTH
        public Struct._RESPUESTA ExactusGetDocumentoCXC_NOAUTH(string esquema, string id_pedido, string pedido_erp, string tipo_doc, string documento, string fecha, string valor, string impuesto, string accion, string fc_numero)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();

            Struct._ProcesoGetDocumento args = new Struct._ProcesoGetDocumento();
            args.esquema = esquema;
            args.id_pedido = id_pedido.Trim();
            args.pedido_erp = pedido_erp.Trim();
            args.tipo_doc = tipo_doc.Trim();
            args.documento = documento.Trim();
            args.fecha = fecha.Trim();
            args.valor = valor.Trim();
            args.impuesto = impuesto.Trim();
            args.accion = accion.Trim();
            args.fc_numero = fc_numero.Trim();
            args.server_user = "soporte7";
            args.server_pass = "";

            res = Exactus.ProcesoGetDocumento("CXC", AppDomain.CurrentDomain.BaseDirectory + path_files, args);

            //res = Exactus.ProcesoGetDocumento2(esquema.Trim(), "CXC", "soporte7", "", id_pedido.Trim(), pedido_erp.Trim(), tipo_doc.Trim(), documento.Trim(), fecha.Trim(), valor.Trim(), impuesto.Trim(), accion.Trim(), fc_numero.Trim());
        
            //res = Exactus.UpdateDocumentoCXC(esquema, id_pedido, pedido_erp, tipo_doc, documento, fecha, valor, impuesto, accion, fc_numero, "soporte7");

            return res;
        }
        */


        /*
        [WebMethod] //CXC - NO AUTH
        public Struct._RESPUESTA ExactusGetDocumentoCXP_NOAUTH(string esquema, string id_costo, string numero_erp, string tipo_doc, string documento, string fecha, string valor, string proveedor, string master, string user, string ip)
        {
            Struct._RESPUESTA res = new Struct._RESPUESTA();

            Struct._ProcesoGetDocumento args = new Struct._ProcesoGetDocumento();
            args.esquema = esquema;
            args.id_costo = id_costo.Trim();
            args.numero_erp = numero_erp.Trim();
            args.tipo_doc = tipo_doc.Trim();
            args.documento = documento.Trim();
            args.fecha = fecha.Trim();
            args.valor = valor.Trim();

            args.master = master.Trim();
            args.proveedor = proveedor.Trim();
            args.user = user.Trim();
            args.ip = ip.Trim();

            args.server_user = "soporte7";
            args.server_pass = "";


            res = Exactus.ProcesoGetDocumento("CXP", AppDomain.CurrentDomain.BaseDirectory + path_files, args);


            //res = Exactus.ProcesoGetDocumento(esquema.Trim(), "CXP", user.Trim(), ip.Trim(), id_costo.Trim(), numero_erp.Trim(), tipo_doc.Trim(), documento.Trim(), fecha.Trim(), valor.Trim(), "0", master.Trim(), proveedor.Trim());

            //recibe ProcesoGetDocumento(esquema, tipo, server_user, server_pass, id_pedido, pedido_erp, tipo_doc, documento, fecha, valor, impuesto, accion, fc_numero)     

            //pasa UpdateDocumentoCXP(esquema, id_pedido, pedido_erp, tipo_doc, documento, fecha, accion, fc_numero, valor, server_user, server_pass);

            //recibe UpdateDocumentoCXP(esquema, id_costo, numero_erp, tipo_doc, documento, fecha, master, proveedor, valor, user, ip)
        
            return res;
        }
        */


        [WebMethod]
        public Struct.Result SendAlertas(string tipo, string user, string from, string sistema, string pais_iso, string tipo_plantilla, string subject, string mensaje)
        {
                return Exactus.SendAlertas(tipo, user, from, sistema, pais_iso, tipo_plantilla, subject, mensaje);
        }



        [WebMethod]
        public Struct.Result SendMail(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip)
        {
            return Parametros.send(pais_iso, to, subject, body, fromName, sistema, user, ip);
        }

        [WebMethod]
        public Struct.Result SendMailAttach(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip, string cc, string bc, string attachments)
        {
            return Parametros.sendattach(pais_iso, to, subject, body, fromName, sistema, user, ip, cc, bc, attachments);
        }

        /*
        [WebMethod]
        public Struct.Result SendMailAttachOne(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip, string cc, string bc, string filename, string file64, string empresa)
        {
            return Parametros.sendattachOne(pais_iso, to, subject, body, fromName, sistema, user, ip, cc, bc, filename, file64, empresa);
        }
        */

        [WebMethod]
        public Struct.ArrParams GetLogoData(string pais_iso, string sistema, string doc_id, string titulo, string edicion)
        {
            return Postgres_.EmpresaParametros(pais_iso, sistema, doc_id, titulo, edicion);
        }

        [WebMethod]
//        public List<Struct.arg_data> Notification(string tracking_id, string product, string sub_product, string impex, string bl_id, string status_id, string produccion, string user, string ip)
        public Struct.arg_data Notification(string tracking_id, string product, string sub_product, string impex, string bl_id, string status_id, string produccion, string user, string ip)
        {
            Struct.arg_data data = new Struct.arg_data();
            
            //aereo 1 = Export 2 = Import   //maritimo E   I

            data.product = product.ToLower();

            data.sub_product = sub_product.ToLower();

            data.impex = impex.ToLower();

            data.bl_id = string.IsNullOrEmpty(bl_id) ? 0 : int.Parse(bl_id);

            data.status_id = string.IsNullOrEmpty(status_id) ? 0 : int.Parse(status_id);

            data.tracking_id = string.IsNullOrEmpty(tracking_id) ? 0 : int.Parse(tracking_id);

            data.produccion = produccion == "1" ? true : (produccion == "true" ? true : false);

            data.user = user;

            data.ip = ip;

            return Tracking.notification(data);
        }

        [WebMethod]
        public Struct.Result ModeDev2(string Sistema, string Countries)
        {
            Struct.Result res = new Struct.Result();
            res.msg = "-";
            string query = "SELECT case when copia = 'Si' then 1 else 0 end as wsNotif, case when rechazo = 'Si' then 1 else 0 end as sendNotif, case when status = 'Activo' then 1 else 0 end as Test, case when tipo_persona = 'Consulta' then 0 else 1 end as ws21 FROM contactos_divisiones WHERE id_catalogo = 1237 and nombre = 'TRACKING PRUEBAS' " +
                "AND area ILIKE '%" + Sistema + "%' " +
                "AND ( (catalogo = 'USUARIO' AND pais ILIKE '%\"" + Countries + "\"%') )";
            var data = Postgres_.GetRowPostgres<Struct.mode_data>("produccion", query);
            if (data.wsNotif == "1") res.msg += "wsNotif|";
            if (data.sendNotif == "1") res.msg += "sendNotif|";
            res.msg += "Test" + data.Test + "|";
            res.msg += "ws21" + data.ws21 + "|";
            res.stat = 1;
            return res;
        }

        [WebMethod]
        public Struct.Result ModeDev(string Sistema, string Countries)
        {
            Struct.Result x = new Struct.Result();
            int res = -1;
            string query = "SELECT copia as web, rechazo as cod, status as dev, case when tipo_persona = 'Consulta' then 0 else 1 end as ws21 FROM contactos_divisiones WHERE id_catalogo = 1237 and nombre = 'TRACKING PRUEBAS' " +
                "AND area ILIKE '%" + Sistema + "%' " +
                "AND ( (catalogo = 'USUARIO' AND pais ILIKE '%\"" + Countries + "\"%') )";

            var data = Postgres_.GetRowPostgres<Struct.dev_data>("produccion", query);
            if (data.web == "Si" && data.cod == "No") //codigo nuevo
            {
                if (data.dev == "Activo")
                    res = 1; //produccion 1
                else
                    res = 0; //produccion 0
            }
            else
                if (data.web == "No" && data.cod == "Si") //codigo anterior
                {
                    res = 4;  //solo codigo anterior
                }
                else
                    if (data.web == "Si" && data.cod == "Si") //ambos en paralelo
                    {
                        if (data.dev == "Activo")
                            res = 3;  //produccion 1
                        else
                            res = 2;  //produccion 0
                    }
            x.stat = res;
            x.msg = res.ToString();
            return x;
        }

        [WebMethod]
        public Struct.Result Upload_Files(string ruta, string filename_old, string user, string ip, string attachments, Boolean erase)
        {
            return Parametros.upload_files(ruta, filename_old, user, ip, attachments, erase);
            #region old
            /*

            string filename = "";

            try
            {
                if (!String.IsNullOrEmpty(attachments))
                {
                    System.Xml.XmlDocument responseXml = new System.Xml.XmlDocument();
                    responseXml.LoadXml(attachments);
                    System.Xml.XmlNodeList elemList = responseXml.GetElementsByTagName("row");
                    if (elemList.Count > 0)
                    {
                        foreach (System.Xml.XmlNode chldNode in elemList)
                        {
                            System.Xml.XmlNodeList row = chldNode.ChildNodes;

                            filename = System.IO.Path.GetFileName(row.Item(0).InnerText);
                            var file64 = row.Item(1).InnerText;

                            //.GetFileNameWithoutExtension(filename);
                            string extension = System.IO.Path.GetExtension(filename);

                            var bytes = Convert.FromBase64String(file64);
                            System.IO.MemoryStream strm = new System.IO.MemoryStream(bytes);

                            using (System.IO.FileStream fs = new System.IO.FileStream(ruta + filename, System.IO.FileMode.OpenOrCreate))
                            {
                                strm.CopyTo(fs);
                                fs.Flush();
                            }

                        }
                    }
                }

                filename_old = System.IO.Path.GetFileName(filename_old);

                if (filename_old != filename && erase)
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.IO.File.Delete(ruta + filename_old);
                }

                res.stat = 1;
                res.msg = "File uploaded.";

            }
            catch (Exception ex)
            {
                res.stat = 2;
                res.msg = ex.Message;
            }

            //log(res.stat, user, "uploadfile", "", "", "", "", ip, res.msg);

            return res;


            //return r;
            */
            #endregion
        }

        [WebMethod]
        public Struct.CSPedidosResult EvaluaPedidos(string HBLNumber, string ObjectID, string sistema, string CountryExactus, string pedido2str)
        {
            Struct.CSPedidosResult res = new Struct.CSPedidosResult();

            res = Exactus.EvaluaPedidos(HBLNumber, ObjectID, sistema, CountryExactus, pedido2str);

            return res;
        }


        /* este metodo ya no se utilizo desde que se crea la integracion cargo system a exactus

        [WebMethod]
        public string TTStoCargoSystem(string json)
        {
            string json_result = Postgres_.CrossSendRoutingTTC(json, "tts_json", "produccion");

            return json_result;
        } 
        
        */

    }
}
