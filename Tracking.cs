using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Tracking
{
    public Tracking()
    {
    }

    #region notification

    //public static List<Struct.arg_data> notification(Struct.arg_data tb_arg)
    public static Struct.arg_data notification(Struct.arg_data tb_arg)
    {
        string query = "";


        //List<Struct.arg_data> data2 = new List<Struct.arg_data>();

        try
        {
            switch (tb_arg.product)
            {
                case "1": tb_arg.product = "aereo"; break;
                case "2": tb_arg.product = "terrestre"; break;
                case "3": tb_arg.product = "maritimo"; break;
                case "4": tb_arg.product = "preembarque"; break;
                case "5": tb_arg.product = "aduana"; break;
            }

            if (tb_arg.tracking_id > 0)
            {
                //get_tracking
                query = get_query(tb_arg, "tracking", "");
                var tb_tracking = get_tracking_data(tb_arg.product, query);

                if (tb_tracking.BLStatus > 0)
                    tb_arg.status_id = tb_tracking.BLStatus;

                if (tb_arg.status_id > 0)
                {
                    //get status           
                    query = get_query(tb_arg, "status", "");
                    var tb_status = Postgres_.GetRowPostgres<Struct.status_data>("produccion", query);
            
                    if (tb_arg.bl_id == 0 && tb_tracking.bl_id > 0)
                        tb_arg.bl_id = tb_tracking.bl_id;

                    tb_arg.impex = tb_tracking.impex;

                    if (!String.IsNullOrEmpty(tb_tracking.tipo_contenedor)) //fcl lcl
                        tb_arg.sub_product = tb_tracking.tipo_contenedor.ToLower();

                    if (tb_arg.bl_id > 0 || tb_arg.product == "maritimo")
                    {
                        var rows = get_bl_list(tb_arg, tb_tracking);

                        //data1 = new Struct.arg_data[rows.Count()];
                        //data2 = new Struct.arg_data[rows.Count()];

                        int c = 0, tb_arg_bl_id = tb_arg.bl_id;

                        ///////////////////////////////////////inicia del ciclo de bls///////////////////////////////////////

                        #region foreach rows bls
                        foreach (Dictionary<string, object> row in rows)
                        {

                            Struct.bl_data tb_bl = new Struct.bl_data();

                            #region set struct tb_bl

                            foreach (KeyValuePair<string, object> campo in row)
                            {
                                try
                                {
                                    switch (campo.Key.ToLower())
                                    {
                                        case "bl_id": tb_bl.BlId = int.Parse(campo.Value.ToString()); break;
                                        case "mbl": tb_bl.Mbl = campo.Value.ToString(); break;
                                        case "hbl": tb_bl.Hbl = campo.Value.ToString(); break;
                                        case "consignerid": tb_bl.ConsignerID = int.Parse(campo.Value.ToString()); break;
                                        case "shipperid": tb_bl.ShipperID = int.Parse(campo.Value.ToString()); break;
                                        case "agentid": tb_bl.AgentID = int.Parse(campo.Value.ToString()); break;
                                        case "id_coloader": tb_bl.id_coloader = int.Parse(campo.Value.ToString()); break;
                                        case "id_cliente_order": tb_bl.id_cliente_order = int.Parse(campo.Value.ToString()); break;
                                        case "no": tb_bl.no = int.Parse(campo.Value.ToString()); break;
                                        case "countries": tb_bl.Countries = campo.Value.ToString(); break;
                                        case "routingid": tb_bl.RoutingID = int.Parse(campo.Value.ToString()); break;
                                        case "extype": tb_bl.ExType = int.Parse(campo.Value.ToString()); break;
                                        case "bltype": tb_bl.BLType = int.Parse(campo.Value.ToString()); break;
                                        case "exdbcountry": tb_bl.EXDBCountry = campo.Value.ToString(); break;
                                        case "exid": tb_bl.EXID = int.Parse(campo.Value.ToString()); break;
                                        //case "mbls": tb_bl.MBls = campo.Value.ToString(); break;
                                        case "countriesdes": tb_bl.CountriesDes = campo.Value.ToString(); break;
                                        case "contenedor": tb_bl.Contenedor = campo.Value.ToString(); break;
                                        case "id_usuario": tb_bl.id_usuario = int.Parse(campo.Value.ToString()); break;
                                        case "id_dochijo": tb_arg.stat = int.Parse(campo.Value.ToString()); break; //se usa temporal para leer bls hijos
                                        case "sub_product": tb_arg.sub_product = campo.Value.ToString(); break;
                                        case "product": tb_arg.msg = campo.Value.ToString(); break; //se usa temporal para AE TE OC AD

                                    }
                                }
                                catch (Exception ex)
                                {
                                    tb_arg.msg = ex.Message;
                                }


                            }

                            #endregion
                            
                            Struct.bl_data tb_hija = null;

                            tb_arg.Countries = tb_tracking.id_pais;
                            tb_arg.CountriesDest = tb_tracking.id_pais;

                            if (!String.IsNullOrEmpty(tb_bl.Countries))
                                tb_arg.Countries = tb_bl.Countries;

                            if (!String.IsNullOrEmpty(tb_bl.CountriesDes))
                                tb_arg.CountriesDest = tb_bl.CountriesDes;

                            query = "";

                            switch (tb_arg.product)
                            {
                                case "terrestre":
                                    ///////////////////////////////////////////////////////////
                                    //////// inicia modulo terrestre ///////////////////////////
                                    #region proceso terrestre CountriesDes / get query routing




                                    string country_dest = "", cou_temp = "";
                                    int tipo = 1;

                                    if (tb_status.notificar_shipper == 1)
                                        switch (tb_bl.ExType)
                                        {
                                            //Solo cuando es RO se envia al Shipper: 4=RO-Consolidado,5=RO-Express,6=RO-Recoleccion,7=RO-Entrega
                                            case 0:
                                            case 1:
                                            case 2:
                                            case 4:
                                            case 5:
                                            case 6:
                                            case 7:
                                            case 11:
                                            case 12:
                                            case 13:
                                            case 14:
                                                tipo = 2;
                                                break;
                                        }

                                    if (tipo == 2)
                                    {
                                        //cou_temp = tb_bl.Hbl.Substring(1, 2);

                                        //country_dest = cou_temp;
                                        /* se deja asi como esta el codigo viejito
                                        cou_temp = tb_bl.EXDBCountry.Substring(0, 2);

                                        //switch (country_dest)
                                        switch (cou_temp)
                                        {
                                            case "N1":
                                            case "BZ":
                                            case "MX":
                                            case "GT":
                                            case "SV":
                                            case "HN":
                                            case "NI":
                                            case "CR":
                                            case "PA":

                                                country_dest = tb_bl.EXDBCountry;

                                                //cou_temp = tb_bl.EXDBCountry.Substring(2, 3);

                                                //if (cou_temp != "")
                                                    //country_dest += cou_temp;

                                                break;

                                            default:
                                                //country_dest = "";
                                                break;
                                        }
                                        */

                                    }


                                    if (String.IsNullOrEmpty(country_dest))
                                        country_dest = tb_bl.CountriesDes;

                                    cou_temp = "";
                                    if (tb_bl.Countries.Length == 5)
                                        cou_temp = tb_bl.Countries.Substring(2, 3);

                                    if (country_dest.Length == 2 && !String.IsNullOrEmpty(cou_temp))
                                        country_dest += cou_temp;

                                    tb_arg.CountriesDest = country_dest;



                                    switch (tb_bl.ExType)
                                    {  // iextype
                                        case 4:
                                        case 5:
                                        case 6:
                                        case 7:
                                            tb_bl.RoutingID = tb_bl.EXID;
                                            break;

                                        case 0:
                                        case 1:
                                        case 2:
                                        case 9:
                                        case 10:
                                        case 11:
                                        case 12:
                                        case 13:
                                            var data = Utils.get_id_routing(tb_bl.ExType, tb_bl.EXDBCountry, tb_bl.EXID);
                                            tb_bl.RoutingID = data.ide;
                                            break;
                                    }

                                    ///////////////////////// fin modulo terrestre ///////////////////////
                                    //////////////////////////////////////////////////////////////////////


                                    #endregion
                                    break;

                                case "maritimo":
                                    tb_arg.CountriesDest = tb_tracking.id_pais; //reinicia pais origen
                                    if (tb_arg.impex == "import" && tb_bl.en_transito == true && !String.IsNullOrEmpty(tb_bl.CountriesDes))
                                        tb_arg.CountriesDest = tb_bl.CountriesDes;
                                    break;

                                case "aduana":
                                    #region datos aduana


                                    switch (tb_arg.msg)
                                    {
                                        case "AE":

                                            query = get_query(tb_arg, "bl", (tb_arg.impex == "export" ? "Awb" : "Awbi") + " WHERE AWBID = " + tb_arg.stat);

                                            tb_hija = MySql_.GetRowMysql<Struct.bl_data>(tb_arg.sub_product.ToLower(), query);

                                            break;

                                        case "TE":

                                            tb_arg.sub_product = tb_arg.sub_product.Replace("Terrestre ", "").ToLower();

                                            query = get_query(tb_arg, "bl", "WHERE BLID = " + tb_arg.stat + " " + (tb_tracking.ClientID > 0 ? " and ClientsID=" + tb_tracking.ClientID : ""));

                                            tb_hija = MySql_.GetRowMysql<Struct.bl_data>(tb_arg.sub_product.ToLower(), query);

                                            break;

                                        case "OC":

                                            tb_arg.sub_product = tb_arg.sub_product.Replace("Maritimo ", "").ToLower();

                                            var ventas = "ventas_" + tb_arg.Countries.ToLower();

                                            query = get_query(tb_arg, "bl", " and b.bl_id = " + tb_arg.stat);

                                            tb_hija = Postgres_.GetRowPostgres<Struct.bl_data>(ventas, query);

                                            break;

                                        case "AD":

                                            break;

                                    }


                                    tb_bl.en_transito = tb_hija.en_transito;
                                    tb_bl.ConsignerID = tb_hija.ConsignerID;
                                    tb_bl.Contenedor = tb_hija.Contenedor;
                                    tb_bl.CountriesDes = tb_hija.CountriesDes;
                                    tb_bl.id_cliente_order = tb_hija.id_cliente_order;
                                    tb_bl.id_coloader = tb_hija.id_coloader;
                                    tb_bl.Mbl = string.IsNullOrEmpty(tb_bl.Mbl) ? tb_hija.Mbl : "";
                                    tb_bl.Hbl = string.IsNullOrEmpty(tb_bl.Hbl) ? tb_hija.Hbl : "";
                                    tb_bl.AgentID = tb_hija.AgentID;


                                    #endregion
                                    break;
                            }

                            if (tb_arg.bl_id == 0 && tb_bl.BlId > 0)
                                tb_arg.bl_id = tb_bl.BlId;

                            //get routing
                            query = get_query(tb_arg, "routing", tb_bl.RoutingID.ToString());
                            var tb_routing = Postgres_.GetRowPostgres<Struct.routing_data>("produccion", query);

                            // get clientes names
                            var tb_clientes = Postgres_.get_clientes_data(tb_bl,"produccion");

                            //get_contacts_list
                            var tb_contacts = get_contacts_list(tb_arg, tb_bl, tb_status);

                            // get clean contacts
                            tb_contacts = get_clean_contacts(tb_contacts, tb_arg.CountriesDest);

                            if (tb_contacts.Count() > 0)
                            {
                                // get contact name, email, phone
                                var tb_contac = get_contact_data(tb_contacts, tb_arg, tb_bl.id_usuario);

                                if (String.IsNullOrEmpty(tb_contac.error))
                                {
                                    // send email to contacts
                                    tb_arg = send_contacts(tb_arg, tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, tb_contacts, tb_hija);

                                    //data2[c] = send_contacts(data1[c], tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, tb_contacts, tb_hija); 


                                    //data2.Add(send_contacts(tb_arg, tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, tb_contacts, tb_hija));
                                    
                                }
                                else
                                {
                                    tb_arg.stat = 3;
                                    tb_arg.msg = "No se pudo enviar la notificacion, " + tb_contac.error;
                                }
                            }
                            else
                            {
                                tb_arg.stat = 4;
                                tb_arg.msg = "No se pudo enviar la notificacion, No se encontraron contactos asignados al cliente";
                            }

                            c++;

                            tb_arg.bl_id = tb_arg_bl_id;

                        }
                        ///////////////////////////////////////fin del ciclo de bls///////////////////////////////////////

                        #endregion

                        if (c == 0)
                        {
                            tb_arg.stat = 5;
                            tb_arg.msg = "No se pudo enviar la notificacion, No se encontraron bls";
                        }
                    }
                    else
                    {
                        tb_arg.stat = 6;
                        //if (tb_arg.bl_id == 0)
                        tb_arg.msg += "No se pudo enviar la notificacion, No trae id de bl ";
                    }

                }
                else
                {
                    tb_arg.stat = 7;
                    tb_arg.msg = "No se pudo enviar la notificacion, No trae id de estatus";
                }
            }
            else
            {
                tb_arg.stat = 8;
                tb_arg.msg = "No se pudo enviar la notificacion, No trae id de tracking";
            }
        }
        catch (Exception ex)
        {
            tb_arg.stat = 9;
            tb_arg.msg = "No se pudo enviar la notificacion, " + ex.Message;
        }

        //if (data2 == null)
        {
            //data2 = new Struct.arg_data[1];
            //data2[0] = tb_arg;
        }

        //Utils.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);

        return tb_arg;

    }


    #endregion

    #region send_contacts

    public static Struct.arg_data send_contacts(Struct.arg_data tb_arg, Struct.status_data tb_status, Struct.bl_data tb_bl, Struct.tracking_data tb_tracking, Struct.routing_data tb_routing, Struct.clientes_data tb_clientes, Struct.contact_data tb_contac, IEnumerable<Dictionary<string, object>> tb_contacts, Struct.bl_data tb_hija)
    {

        try
        {
            tb_arg.sent_no = "";
            tb_arg.sent_si = "";

            //string repetidos = "";

            if (tb_contacts.Count() > 0)
            {

                string Countries = tb_arg.Countries;
                string language = "es";

                if (Countries.Substring(0, 2) == "BZ")
                    language = "en";


                Struct.email_data tb_email_cliente = new Struct.email_data();
                Struct.email_data tb_email_agente = new Struct.email_data();
                Struct.email_data tb_email_rechazo = new Struct.email_data();

                tb_email_cliente = get_email(tb_arg, tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, tb_hija, language, "1"); //1 = clientes


                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///////////////////////// envia notificacion
                #region foreach contacts

                foreach (Dictionary<string, object> row in tb_contacts)
                {

                    Struct.contact_data data = new Struct.contact_data();
                    #region persona
                    foreach (KeyValuePair<string, object> campo in row)
                    {
                        switch (campo.Key)
                        {
                            case "id_contacto": data.id_contacto = int.Parse(campo.Value.ToString()); break;
                            case "tipo_persona": data.tipo_persona = campo.Value.ToString(); break;
                            case "nombre": data.nombre = campo.Value.ToString(); break;
                            case "contactoxpais": data.contactoxpais = campo.Value.ToString(); break;
                            case "email": data.email = campo.Value.ToString(); break;
                            case "telefono": data.telefono = campo.Value.ToString(); break;
                            case "copia": data.copia = campo.Value.ToString(); break;
                            case "rechazo": data.rechazo = campo.Value.ToString(); break;
                        }
                    }
                    #endregion

                    Struct.Result result = new Struct.Result();
                    result.stat = -10;
                    result.msg = result.stat + (tb_arg.produccion == true ? "No proceso el email del contacto." : "Ambiente de Pruebas.");

                    #region esta es la anterior limpieza de contactos
                    /*
                    if (!string.IsNullOrEmpty(data.contactoxpais) && data.contactoxpais != "0")
                    {
                        data.email = contactoXpais(data.contactoxpais, data.email, tb_arg.CountriesDest); //tb_bl.CountriesDes);
                    }
                    
                    data.email = data.email.ToLower().Trim();

                    //if (!String.IsNullOrEmpty(data.tipo_persona) && !String.IsNullOrEmpty(data.email))
                    if (String.IsNullOrEmpty(data.tipo_persona))
                    {
                        result.stat = -20;
                        result.msg = result.stat + " : No hay tipo de persona";
                    }                   

                    string x = repetidos.IndexOf(data.email).ToString();
                    if (result.stat == -10)
                    if (x != "-1")
                    {
                        result.stat = -90;
                        result.msg = result.stat + " : Email Repetido";
                    }*/
                    #endregion

                    ///////////////////////// envia notificacion
                    #region validacion de email
                    if (data.copia == "Si")
                    {

                        if (String.IsNullOrEmpty(data.email))
                        {
                            result.stat = -30;
                            result.msg = result.stat + " : No tiene cuenta de correo, favor de revisar contactos y actualizar.";
                        }

                        if (!Utils.IsValidEmail(data.email))
                        {
                            result.stat = -40;
                            result.msg = result.stat + " : No es un email valido según los estándares.";
                        }


                        if (result.stat == -10)
                        {

                            if (tb_arg.produccion == true)
                            {  ///////////// produccion
                                if (data.tipo_persona == "Agente")
                                {
                                    if (data.email.IndexOf("aimargroup") > -1)
                                    {
                                        result.stat = -70;
                                        result.msg = result.stat + " : No hay configuración para envió a Agentes.";

                                        //if (String.IsNullOrEmpty(tb_email_agente.body))
                                        //    tb_email_agente = get_email(product, sub_product, impex, tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, lang, "2"); //2 = agentes
                                    }
                                }
                                else
                                {
                                    result.stat = -80;
                                    result.msg = result.stat + " : OK";
                                }
                            }
                            else
                            {
                                result.stat = -50;
                                result.msg = result.stat + " : Ambiente Pruebas";

                                ///////////////////////////// localhost
                                if (data.tipo_persona == "Desarrollo")
                                {
                                    result.stat = -80;
                                    result.msg = result.stat + " : OK";
                                }
                            }
                        }
                    } //copia si
                    #endregion

                    if (result.stat == -80) //envio de tracking valido
                    {
                        result.msg = result.stat + " : No se pudo configurar plantilla de email.";

                        ////////////////////////////////// send email CLIENTE
                        if (data.tipo_persona != "Agente" && !String.IsNullOrEmpty(tb_email_cliente.subject) && !String.IsNullOrEmpty(tb_email_cliente.body))
                        {
                            tb_email_cliente.body = tb_email_cliente.body.Replace("#*consignee*#", GetLang("cliente", language)); //data.tipo_persona);
                            result = Parametros.send(tb_arg.CountriesDest, data.email, (tb_arg.produccion ? "" : "TEST2 ") + tb_email_cliente.subject, Utils.Base64Encode(tb_email_cliente.body), "TRACKING", tb_arg.product, "", "");
                        }

                        ////////////////////////////////// send email AGENTE
                        if (data.tipo_persona == "Agente" && !String.IsNullOrEmpty(tb_email_agente.subject) && !String.IsNullOrEmpty(tb_email_agente.body))
                        {
                            tb_email_agente.body = tb_email_agente.body.Replace("#*consignee*#", GetLang("agente", language));
                            result = Parametros.send(tb_arg.CountriesDest, data.email, (tb_arg.produccion ? "" : "TEST2 ") + tb_email_agente.subject, Utils.Base64Encode(tb_email_agente.body), "TRACKING", tb_arg.product, "", "");
                        }
                    }
                    else
                    {
                        //result.msg = result.stat + " : No pudo encontrar un email para envió";
                    }

                    tb_arg.stat = result.stat;
                    tb_arg.msg = result.msg;

                    if (result.stat == 1)
                        tb_arg.sent_si += " id_contacto:'" + data.id_contacto + ", nombre:'" + data.nombre + "', email:'" + data.email + "'||";
                    else
                        tb_arg.sent_no += " id_contacto:'" + data.id_contacto + ", nombre:'" + data.nombre + "', email:'" + data.email + "', error:'" + result.msg + "'||";


                    if (result.stat != 1)

                        switch (data.tipo_persona)
                        {
                            case "Agente":
                            case "Consigneer":
                            case "Shipper":
                            case "Coloader":
                            case "Notify":
                                tb_email_rechazo.body += "<font color=red>IMPORTANTE:</font> No se pudo enviar estatus al " + data.tipo_persona +
                                "<br>" + "<b>Contacto ID : " + data.id_contacto + " - " + data.nombre + "</b> E-mail : (" + data.email + ")<br>";
                                tb_email_rechazo.body += result.msg + "<br><br>";
                                break;
                        }

                } // foreach notificacion
                #endregion

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///////////////////////// envia aviso a supervisores / desarrollo de los emails que tuvieron error
                #region evalua sent_00
                if (!String.IsNullOrEmpty(tb_email_rechazo.body))
                {
                    tb_email_rechazo.body += "A continuacion el mensaje original: <br><hr><br>" + tb_email_cliente.body;

                    //if (String.IsNullOrEmpty(tb_email_rechazo.body))
                    //tb_email_rechazo = get_email(product, sub_product, impex, tb_status, tb_bl, tb_tracking, tb_routing, tb_clientes, tb_contac, "es", "cliente");

                    ///////////////////////// envia notificacion
                    foreach (Dictionary<string, object> contact in tb_contacts)
                    {
                        Struct.contact_data person = new Struct.contact_data();

                        foreach (KeyValuePair<string, object> campo in contact)
                        {
                            switch (campo.Key)
                            {
                                case "id_contacto": person.id_contacto = int.Parse(campo.Value.ToString()); break;
                                case "tipo_persona": person.tipo_persona = campo.Value.ToString(); break;
                                case "nombre": person.nombre = campo.Value.ToString(); break;
                                case "contactoxpais": person.contactoxpais = campo.Value.ToString(); break;
                                case "email": person.email = campo.Value.ToString(); break;
                                case "telefono": person.telefono = campo.Value.ToString(); break;
                                case "copia": person.copia = campo.Value.ToString(); break;
                                case "rechazo": person.rechazo = campo.Value.ToString(); break;
                            }
                        }


                        ///////////////////////// envia rechazOs
                        if (person.rechazo == "Si" && !String.IsNullOrEmpty(tb_email_rechazo.body))
                        {

                            if (tb_arg.produccion == true)
                            {
                                ///////////// produccion

                                Parametros.send(tb_arg.Countries, person.email, (tb_arg.produccion ? "" : "TEST ") + "NO ENVIO : " + tb_email_cliente.subject, Utils.Base64Encode(tb_email_rechazo.body), "TRACKING", tb_arg.product, "", "");

                            }
                            else
                            {
                                /////////// pruebas
                                if (person.tipo_persona == "Desarrollo")
                                {

                                    Parametros.send(tb_arg.Countries, person.email, (tb_arg.produccion ? "" : "TEST ") + "NO ENVIO : " + tb_email_cliente.subject, Utils.Base64Encode(tb_email_rechazo.body), "TRACKING", tb_arg.product, "", "");

                                }
                            }
                        }

                    }

                }
                else
                {

                    if (!String.IsNullOrEmpty(tb_arg.sent_no))
                    {


                        tb_arg.msg = "Hubieron trackings sin enviar, pero no encontro contactos externo";

                        tb_arg.msg = "Tracking Enviado";

                    }


                }
                #endregion

            }
            else
            {
                tb_arg.stat = -20;
                tb_arg.msg = "No encontro contactos";
            }

        }
        catch (Exception ex)
        {
            if (tb_arg.stat != 1)
                tb_arg.stat = 2;

            if (string.IsNullOrEmpty(tb_arg.msg))
                tb_arg.msg = ex.Message;
            else
                tb_arg.msg += " Errores : " + ex.Message;
        }

        return tb_arg;

    }

    #endregion

    #region get_email

    public static Struct.email_data get_email(Struct.arg_data tb_arg, Struct.status_data tb_status, Struct.bl_data tb_bl, Struct.tracking_data tb_tracking, Struct.routing_data tb_routing, Struct.clientes_data tb_clientes, Struct.contact_data tb_contac, Struct.bl_data tb_hija, string language, string tipo) //1 = clientes     2 = agentes
    {
        Struct.email_data data = new Struct.email_data();

        try
        {

            string headers = "<img src='data:image/jpeg;base64,#*logo*#'>#*nombre_pais*# " + (string.IsNullOrEmpty(tb_arg.sub_product) ? GetLang(tb_arg.product, language).ToUpper() : tb_arg.sub_product.ToUpper()) + " " + GetLang(tb_arg.impex.ToUpper(), language);

            string body = get_template(tb_tracking.Comment, tb_contac, tipo, language, tb_arg, tb_bl.BlId);

            switch (tb_arg.product)
            {
                case "aereo":

                    #region datos aereo

                    #endregion

                    break;

                case "terrestre":

                    #region datos terrestre

                    #endregion

                    break;

                case "maritimo":

                    #region datos maritimo
                    body = body.Replace("#*carga*#", tb_arg.sub_product);
                    #endregion

                    break;

                case "preembarque":

                    #region datos preembarque
                    body = body.Replace("#*carga*#", tb_arg.sub_product);
                    #endregion

                    break;

                case "aduana":

                    #region datos aduana
                    body = body.Replace("#*customs*#", GetLang("Aduana", language));
                    #endregion

                    break;
            }

            body = body.Replace("#*carga*#", "");


            // subject / datos / body
            Struct.email_data tb_datos = get_header_datos(tb_clientes, tb_bl, tb_arg, tb_status, tb_routing, tb_hija, language);

            tb_datos.text += "<b>STATUS : </b>" + tb_status.estatus_en + "<i> (" + tb_status.estatus_es + ")</i><br>";

            //body += tb_datos.body;          

            body = body.Replace("#*datos*#", tb_datos.text);

            data.subject = tb_datos.subject;

            data.body = headers + body;

        }
        catch (Exception ex)
        {
            data.error = ex.Message;

        }

        return data;
    }

    #endregion

    #region get_header_datos

    public static Struct.email_data get_header_datos(Struct.clientes_data tb_clientes, Struct.bl_data tb_bl, Struct.arg_data tb_arg, Struct.status_data tb_status, Struct.routing_data tb_routing, Struct.bl_data tb_hija, string language)
    {
        Struct.email_data row = new Struct.email_data();

        int SendCoLoader = 0, SendNotify = 0, SendAgent = 0, SendConsigner = 0, SendShipper = 0, AgentID = 0, ShipperID = 0, ColoaderID = 0, ConsignerID = 0, NotifyID = 0;

        SendAgent = tb_status.notificar_agente;
        SendConsigner = tb_status.notificar_cliente;
        SendShipper = tb_status.notificar_shipper;

        ConsignerID = tb_bl.ConsignerID;
        ShipperID = tb_bl.ShipperID;
        AgentID = tb_bl.AgentID;
        ColoaderID = tb_bl.id_coloader;
        SendCoLoader = tb_bl.id_coloader > 0 ? 1 : 0;
        NotifyID = tb_bl.id_cliente_order;
        SendNotify = tb_bl.id_cliente_order > 0 ? 1 : 0;

        //string body = "";
        string datos = "";
        string BLSubject = "";

        try
        {
            ////////////////////////////////////////////////////// subject
            string subject = "Status Notification #*customs*# ";

            if (!string.IsNullOrEmpty(tb_routing.order_no))
            {
                subject += "PO : " + tb_routing.order_no + " / ";

                datos += "<b>PO : </b>" + tb_routing.order_no + "<br>"; //2021-09-01
            }


            if (!string.IsNullOrEmpty(tb_routing.routing) && tb_arg.product != "terrestre")
                subject += "RO : " + tb_routing.routing + " / ";

            if (tb_arg.product == "terrestre" && !string.IsNullOrEmpty(tb_bl.Mbl))
                subject += "BL : " + tb_bl.Mbl + " / ";

            if (!string.IsNullOrEmpty(tb_clientes.ShipperName))
                subject += "S: " + tb_clientes.ShipperName + " / ";

            if (!string.IsNullOrEmpty(tb_clientes.ConsignerName))
                subject += "C: " + tb_clientes.ConsignerName + " / ";

            switch (tb_arg.product)
            {
                case "aereo":

                    #region datos aereo

                    if (string.IsNullOrEmpty(tb_bl.Hbl))
                        subject += "AWB : " + tb_bl.Mbl;
                    else
                        subject += "HAWB : " + tb_bl.Hbl;

                    if (string.IsNullOrEmpty(tb_bl.Hbl))
                        datos += "<b>AWB : </b>" + tb_bl.Mbl + "<br>";
                    else
                        datos += "<b>HAWB : </b>" + tb_bl.Hbl + "<br>";

                    datos += "<b>Consignee : </b>" + tb_clientes.ConsignerName + "<br>";

                    datos += "<b>Shipper : </b>" + tb_clientes.ShipperName + "<br>";

                    if (SendCoLoader == 1)
                        datos += "<b>Coloader : </b>" + tb_clientes.ColoaderName + "<br>";

                    if (SendCoLoader == 1 || SendShipper == 1)
                        if (SendNotify == 1)
                            datos += "<b>Notify Party : </b>" + tb_clientes.NotifyName + "<br>";

                    #endregion

                    break;

                case "terrestre":

                    #region datos terrestre

                    switch (tb_bl.ExType)
                    {
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            if (tb_bl.Hbl.Substring(0, 3) == "CMX")
                                BLSubject = tb_bl.Mbl;
                            else
                                BLSubject = tb_bl.Hbl;
                            break;

                        case 8:
                            if (tb_bl.Hbl.Substring(0, 3) == "CMX")
                                BLSubject = tb_bl.Mbl;
                            else
                                BLSubject = tb_bl.Hbl;
                            break;

                        default:
                            BLSubject = tb_bl.Mbl;
                            break;
                    }

                    if (!string.IsNullOrEmpty(tb_bl.Hbl))
                        subject += "CP : " + tb_bl.Hbl;

                    datos += "<b>RO / BL : </b>" + tb_bl.Mbl + "<br>";

                    datos += "<b>" + GetLang("cp", language) + " : </b>" + tb_bl.Hbl + "<br>";

                    if (tb_bl.BLType == 2)
                        if (tb_bl.ExType == 14)
                            datos += "<b>" + GetLang("cpi", language) + " : </b>" + tb_bl.Mbl + "<br>";
                        else
                            datos += "<b>HBL : </b>" + tb_bl.Mbl + "<br>";

                    datos += "<b>Consignee : </b>" + tb_clientes.ConsignerName + "<br>";

                    datos += "<b>Shipper : </b>" + tb_clientes.ShipperName + "<br>";

                    if (SendCoLoader == 1)
                        datos += "<b>Coloader : </b>" + tb_clientes.ColoaderName + "<br>";

                    if (SendCoLoader == 1 || SendShipper == 1)
                        if (SendNotify == 1)
                            datos += "<b>Notify Party : </b>" + tb_clientes.NotifyName + "<br>";

                    #endregion

                    break;

                case "maritimo":

                    #region datos maritimo

                    subject += tb_arg.sub_product.ToUpper() + " " + tb_arg.impex.ToUpper().Substring(0, 1) + " / ";

                    subject += "HBL : " + tb_bl.Hbl;

                    if (!string.IsNullOrEmpty(tb_bl.Contenedor))
                        subject += " / Cont : " + tb_bl.Contenedor;

                    //body = body.Replace("#*carga*#", tb_arg.sub_product);

                    datos += "<b>HBL : </b>" + tb_bl.Hbl + "<br>";

                    datos += "<b>" + GetLang("contenedor", language) + " : </b>" + tb_bl.Contenedor + "<br>";

                    datos += "<b>Consignee : </b>" + tb_clientes.ConsignerName + "<br>";

                    datos += "<b>Shipper : </b>" + tb_clientes.ShipperName + "<br>";

                    #endregion

                    break;

                case "preembarque":

                    #region datos preembarque

                    subject += GetLang("embarque", language) + " : " + (string.IsNullOrEmpty(tb_routing.no_embarque) ? GetLang("pendiente", language) : tb_routing.no_embarque) + "";

                    datos += "<b>Consignee : </b>" + tb_clientes.ConsignerName + "<br>";

                    datos += "<b>" + GetLang("embarque", language) + " : </b>" + (string.IsNullOrEmpty(tb_routing.no_embarque) ? GetLang("pendiente", language) : tb_routing.no_embarque) + "<br>";

                    #endregion

                    break;

                case "aduana":

                    #region datos aduana

                    Struct.email_data tb_datos = null;

                    switch (tb_arg.sub_product.ToLower())
                    {
                        case "aereo":

                            tb_datos = get_header_datos(tb_clientes, tb_hija, tb_arg, tb_status, tb_routing, null, language);

                            break;

                        case "terrestre":

                            tb_datos = get_header_datos(tb_clientes, tb_hija, tb_arg, tb_status, tb_routing, null, language);

                            break;

                        case "fcl":
                        case "lcl":

                            tb_datos = get_header_datos(tb_clientes, tb_hija, tb_arg, tb_status, tb_routing, null, language);

                            break;
                    }

                    subject += tb_datos.subject;
                    datos += tb_datos.text;


                    if (!string.IsNullOrEmpty(tb_bl.dua))
                    {

                        subject += " / " + GetLang("dua", language) + " : " + tb_bl.dua;

                        datos += "<b>" + GetLang("dua", language) + ": </b>" + tb_bl.dua + "<br>";
                    }

                    if (!string.IsNullOrEmpty(tb_bl.referencia))
                    {
                        subject += " / " + GetLang("referencia", language) + " : " + tb_bl.referencia;

                        datos += "<b>" + GetLang("referencia", language) + " : </b>" + tb_bl.referencia + "<br>";
                    }

                    #endregion


                    break;
            }

            subject = subject.Replace("#*customs*#", "");

            row.subject = subject;

            row.text = datos;

        }
        catch (Exception e)
        {
            throw e;

        }



        return row;
    }

    #endregion

    #region get_bl_list

    public static IEnumerable<Dictionary<string, object>> get_bl_list(Struct.arg_data tb_arg, Struct.tracking_data tb_tracking)
    {
        IEnumerable<Dictionary<string, object>> rows = null;

        try
        {
            string filter = "";
            string query = "";

            switch (tb_arg.product)
            {
                case "aereo":
                    filter = (tb_arg.impex == "export" ? "Awb" : "Awbi") + " WHERE AWBID = " + tb_arg.bl_id;
                    query = get_query(tb_arg, "bl", filter);
                    rows = MySql_.get_mysql_list(tb_arg.product, query);
                    break;

                case "terrestre":

                    switch (tb_tracking.ClientID)
                    {
                        case -2:
                            filter = "WHERE a.BLDetailID = " + tb_arg.bl_id;
                            break;

                        case -1: //grupos
                            filter = @" 
                            INNER JOIN BLs b ON a.BLID=b.BLID 
                            INNER JOIN BLGroupDetail f ON b.BLID=f.BLID 
                            INNER JOIN BLGroups e ON e.BLGroupID=f.BLGroupID
                            WHERE e.BLGroupID = " + tb_arg.bl_id;
                            break;

                        case 0:
                            filter = "WHERE a.BLID = " + tb_arg.bl_id;
                            break;

                        default:
                            filter = "WHERE a.BLID = " + tb_arg.bl_id + " and a.ClientsID = " + tb_tracking.ClientID;
                            break;
                    }

                    /*2021-10-20
                    if (tb_tracking.ClientID == 0)
                        filter = "WHERE BLID = " + tb_arg.bl_id;
                    else
                        if (tb_tracking.ClientID > 0)

                            filter = "WHERE BLID = " + tb_arg.bl_id + " and ClientsID = " + tb_tracking.ClientID;

                        else
                            if (tb_tracking.ClientID == -2)
                                filter = "WHERE BLDetailID = " + tb_arg.bl_id;
                    */

                    query = get_query(tb_arg, "bl", filter); // "WHERE BLID = " + tb_arg.bl_id + " " + (tb_tracking.ClientID > 0 ? " and ClientsID=" + tb_tracking.ClientID : ""));

                    rows = MySql_.get_mysql_list(tb_arg.product, query);

                    break;

                case "maritimo":

                    filter = " WHERE b.activo = true AND c.activo = true ";

                    if (tb_arg.sub_product == "lcl")
                    {
                        filter += " AND 'LCL' = 'LCL' AND v.viaje_id = " + tb_tracking.id_viaje;

                        if (tb_tracking.id_tipoestatus > 0)
                            filter += " AND c.no_contenedor = '" + tb_tracking.contenedor + "' ";

                        if (tb_tracking.id_tipoestatus > 1)
                            filter += " AND c.mbl = '" + tb_tracking.mbl + "' ";

                        if (tb_tracking.id_tipoestatus > 2)
                            filter += " AND b.bl_id = " + tb_tracking.bl_id;

                        if (tb_tracking.id_tipoestatus > 3)
                            filter += " AND b.id_cliente = " + tb_tracking.ClientID;

                        if (tb_arg.impex == "import") filter += " AND v.import_export = 't' ";
                        if (tb_arg.impex == "export") filter += " AND v.import_export = 'f' ";
                    }


                    if (tb_arg.sub_product == "fcl")
                    {
                        if (!string.IsNullOrEmpty(tb_tracking.viaje))
                            filter += " AND b.no_viaje = '" + tb_tracking.viaje + "'";

                        //if (tb_tracking.id_tipoestatus > 0)
                        filter += " AND 'FCL' = 'FCL' AND b.mbl = '" + tb_tracking.contenedor + "' ";

                        if (tb_tracking.id_tipoestatus > 1)
                            filter += " AND c.no_contenedor = '" + tb_tracking.mbl + "' ";

                        if (tb_tracking.id_tipoestatus > 2)
                            filter += " AND b.bl_id = " + tb_tracking.bl_id;

                        if (tb_tracking.id_tipoestatus > 3)
                            filter += " AND b.id_cliente = " + tb_tracking.ClientID;

                        if (tb_arg.impex == "import") filter += " AND b.import_export = 't' ";
                        if (tb_arg.impex == "export") filter += " AND b.import_export = 'f' ";

                    }

                    query = get_query(tb_arg, "bl", filter);

                    var ventas = "ventas_" + tb_tracking.id_pais.ToLower();

                    rows = Postgres_.GetArrayPostgres(ventas, query);
                    break;

                case "preembarque":
                    filter = "WHERE id_routing = " + tb_arg.bl_id;
                    query = get_query(tb_arg, "bl", filter);
                    rows = Postgres_.GetArrayPostgres("produccion", query);
                    break;

                case "aduana":
                    query = get_query(tb_arg, "bl", "");
                    rows = MySql_.get_mysql_list(tb_arg.product, query);
                    break;
            }


        }
        catch (Exception e)
        {
            throw e;

        }

        return rows;
    }

    #endregion

    #region get_clean_contacts

    public static IEnumerable<Dictionary<string, object>> get_clean_contacts(IEnumerable<Dictionary<string, object>> tb_contacts, string CountriesDest)
    {


        var rows = new List<Dictionary<string, object>>();

        try
        {
            string repetidos = "";



            foreach (Dictionary<string, object> row in tb_contacts)
            {

                Struct.contact_data data = new Struct.contact_data();

                foreach (KeyValuePair<string, object> campo in row)
                {
                    switch (campo.Key)
                    {
                        case "id_contacto": data.id_contacto = int.Parse(campo.Value.ToString()); break;
                        case "tipo_persona": data.tipo_persona = campo.Value.ToString(); break;
                        case "nombre": data.nombre = campo.Value.ToString(); break;
                        case "contactoxpais": data.contactoxpais = campo.Value.ToString(); break;
                        case "email": data.email = campo.Value.ToString(); break;
                        case "telefono": data.telefono = campo.Value.ToString(); break;
                        case "copia": data.copia = campo.Value.ToString(); break;
                        case "rechazo": data.rechazo = campo.Value.ToString(); break;
                    }
                }

                if (!string.IsNullOrEmpty(data.contactoxpais) && data.contactoxpais != "0")
                {
                    data.email = contactoXpais(data.contactoxpais, data.email, CountriesDest); //tb_bl.CountriesDes);
                }

                data.email = data.email.ToLower().Trim();

                Struct.Result result = new Struct.Result();

                result.stat = -10;
                result.msg = result.stat + " : No proceso contacto.";

                //if (!String.IsNullOrEmpty(data.tipo_persona) && !String.IsNullOrEmpty(data.email))
                if (String.IsNullOrEmpty(data.tipo_persona))
                {
                    result.stat = -20;
                    result.msg = result.stat + " : No hay tipo de persona";
                }

                /*
                if (String.IsNullOrEmpty(data.email))
                {
                    result.stat = -30;
                    result.msg = result.stat + " : No tiene cuenta de correo, favor de revisar y actualizar.";
                }

                if (!Utils.IsValidEmail(data.email))
                {
                    result.stat = -40;
                    result.msg = result.stat + " : No es un email valido segun los estandares.";
                }
                */

                if (data.email.Length > 3)
                {

                    string x = repetidos.IndexOf(data.email).ToString();
                    if (x != "-1")
                    {
                        result.stat = -50;
                        result.msg = result.stat + " : Email Repetido";
                    }
                }

                if (result.stat == -10)
                {
                    repetidos += data.email + "|";

                    row["email"] = data.email;

                    rows.Add(row);
                }

            }

        }
        catch (Exception e)
        {
            throw e;

        }

        return rows;
    }

    #endregion

    #region get_contacts_list

    public static IEnumerable<Dictionary<string, object>> get_contacts_list(Struct.arg_data tb_arg, Struct.bl_data tb_bl, Struct.status_data tb_status)
    {
        IEnumerable<Dictionary<string, object>> rows = null; // new IEnumerable<Dictionary<string, object>>();

        try
        {

            int SendAgent = tb_status.notificar_agente;
            int SendConsigner = tb_status.notificar_cliente;
            int SendShipper = tb_status.notificar_shipper;
            int SendCoLoader = tb_bl.id_coloader > 0 ? 1 : 0;
            int SendNotify = tb_bl.id_cliente_order > 0 ? 1 : 0;

            int AgentID = tb_bl.AgentID;
            int ConsignerID = tb_bl.ConsignerID;
            int ShipperID = tb_bl.ShipperID;
            int ColoaderID = tb_bl.id_coloader;
            int NotifyID = tb_bl.id_cliente_order;


            ////////////////////////////////////// dudas si debe ser countries o final dest
            string Countries = tb_arg.CountriesDest;

            string query = "";

            query = "SELECT nombre, email, telefono, pais, area, impexp, carga, tranship, tipo_persona, copia, rechazo, contactoxpais, id_catalogo, id_contacto, " +
            "case when tipo_persona = 'Desarrollo' then 10 " +
            "when tipo_persona = 'Contacto' then 9 " +
            "when tipo_persona = 'Soporte' then 8 " +
            "when tipo_persona = 'Shipper' then 6 " +
            "when tipo_persona = 'Coloader' then 5 " +
            "when tipo_persona = 'Consigneer' then 4 " +
            "when tipo_persona = 'Agente' then 3 else 0 end as sort " +
                "FROM contactos_divisiones " +
                "WHERE status = 'Activo' " +
                "AND area ILIKE '%" + tb_arg.product + "%' " +
                "AND ( (catalogo = 'USUARIO' AND pais ILIKE '%\"" + Countries + "\"%') ) AND nombre <> 'TRACKING PRUEBAS' ";

            if (!string.IsNullOrEmpty(tb_arg.sub_product))
            {
                query += "AND carga ILIKE '%" + tb_arg.sub_product + "%' ";
            }

            if (!string.IsNullOrEmpty(tb_arg.impex))
            {
                query += " AND impexp ILIKE '%" + tb_arg.impex + "%'";
                /*
                switch (tb_arg.impex.ToLower())
                {
                    case "1":
                    case "e":
                        query += " AND impexp ILIKE '%Export%'";
                        break;

                    case "2":
                    case "i":
                        query += " AND impexp ILIKE '%Import%'"; 
                        break;
                }*/
            }

            if (SendAgent == 1)
            {
                if (query != "")
                    query += " UNION ";

                query += get_externos(tb_arg, 3, AgentID, "Agente", Countries);
            }


            if (SendCoLoader == 1 && (SendShipper == 1 || SendConsigner == 1))
            {
                if (query != "")
                    query += " UNION ";

                query += get_externos(tb_arg, 5, ColoaderID, "Coloader", Countries);
            }
            else
            {

                if (SendShipper == 1)
                {
                    if (query != "")
                        query += " UNION ";

                    query += get_externos(tb_arg, 6, ShipperID, "Shipper", Countries);
                }

                if (SendConsigner == 1)
                {
                    if (query != "")
                        query += " UNION ";

                    query += get_externos(tb_arg, 4, ConsignerID, "Consigneer", Countries);
                }

            }

            if (SendShipper == 1)
            {

                if (SendNotify == 1)
                {
                    if (query != "")
                        query += " UNION ";

                    query += get_externos(tb_arg, 7, NotifyID, "Notify", Countries);
                }
            }

            if (query != "")
                query = "select * from (" + query + ") x where nombre <> '' order by sort, id_contacto";


            rows = Postgres_.GetArrayPostgres("produccion", query);

        }
        catch (Exception e)
        {
            throw e;

        }

        return rows;
    }

    #endregion

    #region get_tracking_data

    public static Struct.tracking_data get_tracking_data(string product, string query)
    {

        Struct.tracking_data tb_tracking = null;

        switch (product)
        {
            case "aereo":

                tb_tracking = MySql_.GetRowMysql<Struct.tracking_data>(product, query);

                break;

            case "terrestre":

                tb_tracking = MySql_.GetRowMysql<Struct.tracking_data>(product, query);

                break;

            case "maritimo":

                tb_tracking = Postgres_.GetRowPostgres<Struct.tracking_data>("produccion", query);

                break;

            case "preembarque":

                tb_tracking = Postgres_.GetRowPostgres<Struct.tracking_data>("produccion", query);

                break;

            case "aduana":

                tb_tracking = MySql_.GetRowMysql<Struct.tracking_data>(product, query);

                break;

        }

        return tb_tracking;

    }

    #endregion

    #region get_externos

    public static string get_externos(Struct.arg_data tb_arg, int sort, int id, string titulo, string country)
    {
        string query = "";

        if (titulo == "Agente")
        {
            //no habilitado por el momento
            //query = "SELECT nombres as nombre, trim(email) as email, telefono, '' as pais, '' as area, '' as impexp, '' as carga, '' as tranship, '" + titulo + "' as tipo_persona, 'Si' as copia, '' as rechazo, '' as contactoxpais, agente_id as id_catalogo, id_contacto, " + sort + " as sort FROM agentes_contactos WHERE agente_id = " + id + " AND activo = 't'";
            query = "SELECT '' as nombre, '' as email, '' as telefono, '' as pais, '' as area, '' as impexp, '' as carga, '' as tranship, '" + titulo + "' as tipo_persona, '' as copia, '' as rechazo, '' as contactoxpais, 0 as id_catalogo, 0 as id_contacto, " + sort + " as sort ";
        }
        else
        {
            query = "SELECT nombres as nombre, trim(email) as email, '' as telefono, '' as pais, area, impexp, carga, '' as tranship, '" + titulo + "' as tipo_persona, 'Si' as copia, '' as rechazo, '' as contactoxpais, id_cliente as id_catalogo, contacto_id as id_contacto, " + sort + " as sort FROM contactos " +
            "WHERE id_cliente = " + id + " AND activo = 't' ";

            query += "AND (area ILIKE '%" + tb_arg.product + "%' OR area IS NULL OR area = '')  ";

            if (!string.IsNullOrEmpty(tb_arg.sub_product))
            {
                query += "AND (carga ILIKE '%" + tb_arg.sub_product + "%' OR carga IS NULL OR carga = '')  ";

            }

            if (!string.IsNullOrEmpty(tb_arg.impex))
            {
                query += " AND (impexp ILIKE '%" + tb_arg.impex + "%' OR impexp IS NULL OR impexp = '') ";
                /*switch (tb_arg.impex.ToLower())
                {
                    case "1":
                    case "e":
                        query += " AND (impexp ILIKE '%Export%' OR impexp IS NULL OR impexp = '') ";
                        break;

                    case "2":
                    case "i":
                        query += " AND (impexp ILIKE '%Import%' OR impexp IS NULL OR impexp = '') ";
                        break;
                }*/
            }

        }

        return query;
    }

    #endregion

    #region   get_contact_data

    public static Struct.contact_data get_contact_data(IEnumerable<Dictionary<string, object>> rows, Struct.arg_data tb_arg, int usuario)
    {
        Struct.contact_data data = new Struct.contact_data();

        try
        {
            string contactoxpais = "", email = "", tipo_persona = "", telefono = "";

            if (tb_arg.product == "preembarque")
            {

                string query = "SELECT id_usuario, pw_gecos as nombre, pais, pw_name || '@' ||  dominio as email from usuarios_empresas where id_usuario = " + usuario + " AND pw_activo = 1";

                data = Postgres_.GetRowPostgres<Struct.contact_data>("produccion", query);

            }
            else
            {

                foreach (Dictionary<string, object> row in rows)
                {
                    foreach (KeyValuePair<string, object> campo in row)
                    {
                        switch (campo.Key)
                        {
                            case "tipo_persona": tipo_persona = campo.Value.ToString(); break;
                            case "nombre": data.nombre = campo.Value.ToString(); break;
                            case "contactoxpais": contactoxpais = campo.Value.ToString(); break;
                            case "email": email = campo.Value.ToString(); break;
                            case "telefono": telefono = campo.Value.ToString(); break;
                        }
                    }

                    if (tipo_persona == "Contacto")
                    {

                        data.telefono = telefono;

                        if (!string.IsNullOrEmpty(contactoxpais) && contactoxpais != "0")
                        {
                            data.email = contactoXpais(contactoxpais, email, tb_arg.CountriesDest);
                        }
                        else
                        {
                            data.email = email;
                        }
                        break;
                    }

                }

            }



            if (string.IsNullOrEmpty(data.email) && String.IsNullOrEmpty(data.telefono))
                data.error = "<font color=red> Se requiere informacion del contacto principal (" + tb_arg.CountriesDest + ") (" + data.email + ")(" + data.telefono + "). </font><br>";

        }
        catch (Exception ex)
        {
            data.error = ex.Message;
        }

        return data;
    }

    #endregion

    #region get_query

    public static string get_query(Struct.arg_data tb_arg, string tipo, string filter)
    {

        string query = "";

        try
        {

            switch (tipo)
            {

                case "tracking":

                    switch (tb_arg.product)
                    {
                        case "aereo":
                            query = "select AWBID as bl_id, BLStatus, BLStatusName, Comment, CASE WHEN DocTyp = 2 THEN 'import' ELSE 'export' END as impex  from Tracking WHERE TrackingID = " + tb_arg.tracking_id;
                            //1=Export, 2=Import
                            break;

                        case "terrestre":
                            query = "select BLID as bl_id, BLStatus, BLStatusName, Comment, ClientID, '' as impex from Tracking where TrackingID = " + tb_arg.tracking_id;
                            break;

                        case "maritimo":
                            
                        //query = "SELECT tracking_details.id_bl as bl_id, id_estatus_pg as BLStatus, name_es as BLStatusName, name_en, comentario as Comment, tracking_details.id_viaje, tracking_details.viaje, tracking_details.contenedor, tracking_details.mbl, tracking_details.bl, id_cliente as ClientID, id_tipoestatus, tracking_details.id_pais, tipo_contenedor, CASE WHEN ubicacion = 'I' THEN 'import' ELSE 'export' END  as impex, tracking_details.id_pais FROM tracking_details INNER JOIN  tracking_header ON tracking_header.numero = id_numero WHERE tracking_details.numero = " + tb_arg.tracking_id + " and tracking_details.borrado = 0";
                       
                            //tipo 3 estatus fue a bl sino fue a cont / via / mbl en esos casos no sirve el numero

                            query = "SELECT CASE WHEN id_tipoestatus = 3 THEN a.id_bl ELSE 0 END as bl_id, id_estatus_pg as BLStatus, name_es as BLStatusName, name_en, comentario as Comment, a.id_viaje, a.viaje, a.contenedor, a.mbl, bl, id_cliente as ClientID, id_tipoestatus, a.id_pais, tipo_contenedor, CASE WHEN ubicacion = 'I' THEN 'import' ELSE 'export' END as impex, a.id_pais " + 
"FROM tracking_details a INNER JOIN tracking_header b ON b.numero = id_numero WHERE a.numero = " + tb_arg.tracking_id + " and a.borrado = 0 ";

                            if (tb_arg.impex != "") 
                                query += " AND ubicacion = '" + tb_arg.impex.ToUpper() + "' ";
                            
                            if (tb_arg.sub_product != "") 
                                query += " AND tipo_contenedor = '" + tb_arg.sub_product.ToUpper() + "' ";

                            break;

                        case "preembarque":

                            query = "SELECT routing_cli, id_cliente as ClientID, id_routing as bl_id, cotizacion_id, id_estatus as BLStatus, comentario as Comment, id_pais, usuario, CASE WHEN import_export = 0 THEN 'import' ELSE 'export' END as impex, routing, activo, borrado FROM tracking_routings WHERE id = " + tb_arg.tracking_id;

                            break;

                        case "aduana":

                            query = "SELECT numero, id_estatus as BLStatus, comentario as Comment, id_bitacora as bl_id, id_user, id_pais, id_bitacora, user_name, estatus_es, estatus_en, num_dua, '' as impex FROM estatus_bitacora_dua WHERE numero = " + tb_arg.tracking_id;

                            break;
                    }

                    break;


                case "status":

                    query = "SELECT estatus as estatus_en, estatus_es, notificar_agente, notificar_cliente, notificar_shipper FROM aimartrackings a LEFT JOIN tracking_comentarios b ON a.id = b.id_estatus_pg WHERE a.id = " + tb_arg.status_id;

                    break;

                case "routing":

                    query = "select routing, order_no, no_embarque, id_pais as Countries, id_pais_origen as CountriesDep, id_pais_destino as CountriesDes FROM routings WHERE id_routing = " + filter;

                    break;

                case "bl":

                    switch (tb_arg.product)
                    {
                        case "aereo":
                            query = "select AWBNumber as Mbl, HAWBNumber as Hbl, ConsignerID, ShipperID, AgentID, Countries, id_coloader, id_cliente_order, 1 as no, RoutingID, Countries as CountriesDes from " + filter;
                            break;

                        case "terrestre":

                            query = "select a.BLs as Mbl, a.HBLNumber as Hbl, a.ClientsID as ConsignerID, a.AgentsID as ShipperID, a.ShippersID as AgentID, a.Countries, a.ColoadersID as id_coloader, a.ExType, a.Clients, a.Agents, a.CountriesFinalDes as CountriesDes, a.Shippers, a.Coloaders, a.MBls, a.BLType, a.EXDBCountry, a.NotifyPartyID as id_cliente_order, a.EXID, a.Container from BLDetail a " + filter;

                            break;

                        case "maritimo":

                            if (tb_arg.sub_product == "lcl")
                            {
                                query = "SELECT distinct(b.bl_id) as bl_id, " +
                                    "v.viaje_id, v.no_viaje, c.viaje_contenedor_id, c.no_contenedor as Contenedor, c.mbl as Mbl, b.no_bl as Hbl, b.id_cliente as ConsignerID, " +
                                    "v.id_naviera, v.vapor, v.agente_id as AgentID, b.id_shipper as ShipperID, b.id_almacen, v.etd, v.eta, v.fecha_arribo, v.id_puerto_origen, " +
                                    "v.id_puerto_desembarque, a.fecha_generada, c.fecha_descarga, c.no_aduana, b.id_destino_final, b.id_puerto_embarque, " +
                                    "to_date(to_char(b.fecha_ingreso_sistema,'yyyy-mm-dd'),'yyyy-mm-dd') as fecha_s, b.fecha_ingreso_sistema, b.en_intermodal as en_transito, b.id_pais_final2 as CountriesDes, b.id_coloader, b.id_cliente_order, b.id_routing as RoutingID " +
                                "FROM " +
                                    "bill_of_lading as b " +
                                    "left join viaje_contenedor as c on c.viaje_contenedor_id = b.viaje_contenedor_id " +
                                    "left join viajes as v on v.viaje_id = c.viaje_id " +
                                    "left join arrival_notices as a on a.bl_id = b.bl_id " + filter +
                                "ORDER BY b.no_bl";
                            }


                            if (tb_arg.sub_product == "fcl")
                            {
                                query = "SELECT b.no_viaje as viaje_id, b.no_viaje, b.mbl as Mbl, b.bl_id, b.no_bl as Hbl, b.id_cliente as ConsignerID, c.contenedor_id as viaje_contenedor_id, c.no_contenedor as Contenedor, b.id_naviera, b.vapor, b.agente_id as AgentID, b.id_shipper as ShipperID, b.id_almacen, CURRENT_DATE as etd, b.eta, b.fecha_arribo, b.id_puerto_origen, b.id_puerto_desembarque, to_date(to_char(b.fecha_ingreso_sistema,'yyyy-mm-dd'),'yyyy-mm-dd') as fecha_generado, b.fecha_descarga,  b.no_aduana, b.id_destino_final, b.id_puerto_embarque, to_date(to_char(b.fecha_ingreso_sistema,'yyyy-mm-dd'),'yyyy-mm-dd') as fecha_s, b.fecha_ingreso_sistema, b.en_transito, b.id_pais_final as CountriesDes, b.id_coloader, b.id_cliente_order, b.id_routing as RoutingID " +
                               "FROM bl_completo b INNER JOIN contenedor_completo c ON b.bl_id = c.bl_id " + filter +
                               "ORDER BY b.no_bl";

                            }
                            break;

                        case "preembarque":

                            query = "select routing as Mbl, order_no as Hbl, no_embarque, id_pais, id_pais_origen as Countries, id_pais_destino as CountriesDes, id_cliente as ConsignerID, id_shipper as ShipperID, coalesce(id_notify,0) as id_cliente_order, id_coloader, agente_id as AgentID, id_usuario_creacion as id_usuario FROM routings " + filter;

                            break;

                        case "aduana":

                            query = "SELECT " +
                                //"id_bitacora as bl_id, " +
                                "id_pais as Countries, " +
                                "transito_det as sub_product, " +
                                "id_dochijo, " +
                                "id_cliente as ConsignerID, " +
                                "id_proveedor as ShipperID, " +
                                "num_viaje as no_viaje, " +
                                "contenedor as Contenedor, " +
                                "mbl as Mbl, " +
                                "case when hbl = '' or hbl is null then documento_externo else hbl end as Hbl, " +
                                "transito as product, " +
                                "documento_externo, " +
                                "id_user as id_usuario, " +
                                "id_pais as CountriesDes," +
                                "cast(grupo_empresa as signed) as no" +

                                /*"bitacora, " + 
                                "dua, " + 
                                "awb, " + 
                                "carta_porte, " + 
                                "viaje_id, " + 
                                "num_recibo, " + 
                                "id_routing, " + 
                                "id_customer, " + 
                                "id_shipper, " + */

                            " FROM bitacora_aduanas WHERE id = " + tb_arg.bl_id;

                            break;
                    }

                    break;


            }
        }
        catch (Exception e)
        {
            throw e;
        }

        return query;
    }

    #endregion

    #region contactoXpais

    public static string contactoXpais(string contactoxpais, string email, string country)
    {
        //dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(contactoxpais);

        string mail = email;

        try
        {

            if (!String.IsNullOrEmpty(contactoxpais))
            {
                contactoxpais = contactoxpais.Replace("{", "");
                contactoxpais = contactoxpais.Replace("}", "");
                contactoxpais = contactoxpais.Replace("/", "");

                contactoxpais = contactoxpais.Replace("\"", "");

                String[] lineElements;
                String[] dat;

                lineElements = contactoxpais.Split(',');
                foreach (var row in lineElements)
                {
                    dat = row.Split(':');

                    var a = dat[0];
                    var b = dat[1];

                    if (a == country)
                    {
                        mail = b;
                    }
                }

                /*
                TestArray = Split(contactoxpais,",")
                For Each dato In TestArray
                    temp2 = Split(dato,":")
                    'response.write("*(" & dato & ")(" & Replace(temp2(0),"""","") & ")(" & pais & ")<br>")
                    if Replace(temp2(0),"""","") = country then
                        temp3 = temp2(1)
                        temp3 = Replace(temp3,"""","")                            
                        'response.write(temp2(0) & " " & email & "<br>")
                    end if               
                next 
                */
            }

        }
        catch (Exception e)
        {
            throw e;

        }

        return mail;
    }


    #endregion

    #region get_template

    public static string get_template(string Comment, Struct.contact_data tb_contac, string tipo, string lang, Struct.arg_data tb_arg, int bl_id)
    {
        string body = "";

        if (String.IsNullOrEmpty(tb_contac.email))
            tb_contac.email = "";

        if (String.IsNullOrEmpty(tb_contac.telefono))
            tb_contac.telefono = "";

        string tla_en =
            "<p>Dear #*consignee*# : </p><p>Here we present the current status of your shipment #*carga*# with the following information : </p>" +
            "#*datos*#" +
            "<b>Comments : </b><font color='green'>" + Comment + "</font></p>" +
            "<p style='text-align:justify'>If you need any additional information please contact our Operations Department by e-mail: <a href='mailto:" + tb_contac.email.ToLower() + "'>" + tb_contac.email.ToLower() + "</a> or by Phone: " + tb_contac.telefono + ".</p>" +
            "<p>Cordially,</p>#*firma*#" +
            "<p><b>IMPORTANT:<br>Please do not reply to this email, it is sent from an automated system, there will be no response from this address. For assistance contact customer service department.</b></p>";

        string tla_es =
            "<p>Estimado #*consignee*# : </p><p>A continuaci&oacute;n le damos a conocer el status actual de su mercaderia #*carga*# amparada con la siguiente informaci&oacute;n : </p>" +
            "#*datos*#" +
            "<b>Observaciones : </b><font color='green'>" + Comment + "</font></p>" +
            "<p style='text-align:justify'>Si necesita mayor informaci&oacute;n de su carga puede consultar con nuestro departamento de Operaciones <a href='mailto:" + tb_contac.email.ToLower() + "'>" + tb_contac.email.ToLower() + "</a> o al tel&eacute;fono: " + tb_contac.telefono + ".</p>" +
            "<p>Estamos para servirle,</p><p>Atentamente,</p>#*firma*#" +
            "<p><b>IMPORTANTE:<br>Favor no responder este email ya que fue enviado desde un sistema automaticamente y no tendr&aacute; respuesta desde esta direcci&oacute;n de correo.</b></p>";

        string aimar_en =
            "<p>Dear #*consignee*# : </p><p>Here we present the current status of your shipment #*carga*# with the following information : </p>" +
            "#*datos*#" +
            "<b>Comments : </b><font color='green'>" + Comment + "</font></p>" +
            "<p style='text-align:justify'>If you need any additional information please visit our tracking on web page <a href='http://#*home_page*#'>#*home_page*#</a> or you can also contact our Customer Service Department e-mail: <a href='mailto:" + tb_contac.email.ToLower() + "'>" + tb_contac.email.ToLower() + "</a> Phone: " + tb_contac.telefono + ".</p>" +
            "<p>To request a username and password to access to the tracking please contact a customer service representative.</p>" +
            "<p>Cordially,</p>#*firma*#" +
            "<p><b>IMPORTANT:<br>Please do not reply to this email, it is sent from an automated system, there will be no response from this address. For assistance contact customer service department.</b></p>";

        string aimar_es =
            "<p>Estimado #*consignee*# : </p><p>A continuaci&oacute;n le damos a conocer el status actual de su mercaderia #*carga*# amparada con la siguiente informaci&oacute;n : </p>" +
            "#*datos*#" +
            "<b>Observaciones : </b><font color='green'>" + Comment + "</font></p>" +
            "<p style='text-align:justify'>Si necesita mayor informaci&oacute;n de su carga puede visitar nuestro tracking en la pagina web: <a href='http://#*home_page*#'>#*home_page*#</a> o bien consultar con nuestro departamento de Servicio al Cliente <a href='mailto:" + tb_contac.email.ToLower() + "'>" + tb_contac.email.ToLower() + "</a> Telefono: " + tb_contac.telefono + ".</p>" +
            "<p>Para solicitar un usuario y password para acceso al tracking puede comunicarse con nuestro representante de Servicio al Cliente.</p>" +
            "<p>Estamos para servirle,</p><p>Atentamente,</p>#*firma*#" +
            "<p><b>IMPORTANTE:<br>Favor no responder este email ya que fue enviado desde un sistema automaticamente y no tendr&aacute; respuesta desde esta direcci&oacute;n de correo.</b></p>";


        if (tipo == "1") //clientes
        {
            if (tb_arg.Countries.IndexOf("TLA") > 0)
            {
                if (lang == "en") body = tla_en;
                if (lang == "es") body = tla_es;
            }
            else //aimar latin grh 
            {
                if (lang == "en") body = aimar_en;
                if (lang == "es") body = aimar_es;
            }



        }

        if (tipo == "2") //agentes
        {

            if (tb_arg.Countries.IndexOf("TLA") > 0)
            {
                if (lang == "en") body = tla_en;
                if (lang == "es") body = tla_es;
            }
            else //aimar latin grh
            {
                if (lang == "en") body = aimar_en;
                if (lang == "es") body = aimar_es;
            }
        }


        body += "<p><font color=white>" + tb_arg.user + " " + tb_arg.ip + " Tracking Id :" + tb_arg.tracking_id + "</font></p>";
        body += "<p><font color=white>From : " + tb_arg.Countries + " To :" + tb_arg.CountriesDest + "</font></p>";

        body += "<p><font color=white>sub-product : " + tb_arg.sub_product + "</font></p>";
        body += "<p><font color=white>bl_id : " + bl_id + "</font></p>";

        return body;
    }

    #endregion

    #region GetLang

    public static string GetLang(string codigo, string lang)
    {

        string str_cod = (@"
Aereo,
Terrestre,
Maritimo,
Preembarque,
Aduana,
Cliente,
CP,
CPI,
Contenedor,
DUA,
Referencia,
Embarque,
Pendiente,
Import,
Export,
Agente");

        string str_spa = (@"
Aereo,
Terrestre,
Maritimo,
Preembarque,
Aduana,
Cliente,
Carta de Porte,
Carta Porte Importación,
Contenedor,
No. de DUA,
Referencia,
Embarque,
Pendiente,
Import,
Export,
Agente");

        string str_eng = (@"
Air,
Land,
Ocean,
Coordination,
Custom,
Consignee,
WayBill,
Import WayBill,
Container,
DUA Number,
Reference,
Voyage,
Pending,
Import,
Export,
Agent");



        //IDictionary<string, Struct.lang_data> dict = new Dictionary<string, Struct.lang_data>();

        List<string> list_eng = str_eng.Replace("\n", "").Replace("\r", "").Split(',').ToList();
        List<string> list_esp = str_spa.Replace("\n", "").Replace("\r", "").Split(',').ToList();
        List<string> list_cod = str_cod.Replace("\n", "").Replace("\r", "").Split(',').ToList();

        string word = "";

        int i = -1;

        foreach (string cod in list_cod)
        {
            i++;
            //dict[cod].eng = list_eng[i];
            //dict[cod].esp = list_esp[i];

            if (cod.ToLower() == codigo.ToLower())
            {
                word = lang == "en" ? list_eng[i] : list_esp[i];
            }
        }

        //string word = lang == "en" ? dict[codigo].eng : dict[codigo].esp;


        i = 0;

        /*

        foreach (string cod in list_cod)
        {
            i++;

            string ncod = cod.Replace("\n", "").Replace("\r", "").ToLower();

            if (ncod == codigo.ToLower()) {

                foreach (string lan in list_lan)
                {
                    j++;
                    if (i == j)
                    {
                        word = lan.Replace("\n", "").Replace("\r", "");
                        break;
                       
                    }
                }
            }

            if (word != "")
                break;
            j = -1;

        }

        */


        return word;
    }
    #endregion

}