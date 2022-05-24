using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Struct
{
    public Struct()
    {
    }

    public class tts_file
    {
        //id_routing  int 
        //public DateTime creado_json { get; set; } //	timestamp [now()]	

        public string file { get; set; } // character varying(30) NULL	
        public string cl_pedido { get; set; } // character varying(30) NULL	
        public string fecha_inclusion { get; set; } // date NULL	
        public string periodo { get; set; } // character(10) NULL	
        public string cod_pais { get; set; } // character(3) NULL	
        public string tipo_tramite { get; set; } // character(3) NULL	
        public string cliente { get; set; } // character(10) NULL	
        public string nombre_cliente { get; set; } // character varying(75) NULL	
        public string refti { get; set; } // character varying(30) NULL	
        public string consignatario { get; set; } // character varying(75) NULL	
        public string referencia { get; set; } // character varying(30) NULL	
        public string medio_transporte { get; set; } // character(10) NULL	
        public string producto { get; set; } // text NULL	
        public string pais_origen { get; set; } // character(3) NULL

        //user_id	integer NULL	
        //user_ingreso	timestamp NULL	
        //user_id_modifica	integer NULL	
        //user_modifica	timestamp NULL	
        //activo	smallint NULL [1]

    }

    public class tts_routing
    {
        public string file { get; set; }
        public string refti { get; set; }
        public string fecha { get; set; }
        public int stat { get; set; }
        public string msg { get; set; }

    }

    public class _exactus_webservices_users
    {
        public String wbus_nombre { get; set; }
        public String wbus_user { get; set; }
        public String wbus_pass { get; set; }
        public String wbus_usuario { get; set; }
        public String wbus_empresa { get; set; }
        public String wbus_url { get; set; }
        public String wbus_estado { get; set; }
        public String wbus_descripcion { get; set; }
    }

    public class ArrParams
    {
        public String country;
        public String nombre_empresa;
        public String nit;
        public String direccion;
        public String telefonos;
        public String nombre_pais;

        public String edicion;
        public String titulo;
        public String observaciones;
        public String descripcion;

        public String exactus_url { get; set; }
        public String exactus_url_username { get; set; }
        public String exactus_url_password { get; set; }




        public Boolean trackactivo;
        public int trackpuerto;
        public String trackmailserver;
        public int trackauth;
        public String trackfromaddress;
        public String trackpassword;

        public String home_page;
        public String firma;
        public String fact_elect_codigo;
        public String fact_elect_user;
        public String fact_elect_pass;
        public String error;
        public String logo2;
        public byte[] logo;
    }

    public class Result
    {
        public int stat;
        public String msg;
        public String text;
    }

    public class CSPedidosResult
    {
        public int stat;
        public String msg;
        public String pedido_erp;
        public String tipo_conta;
        public String esquema;
    }

    public class CSPedidosData
    {
        public int id_pedido { get; set; }
        public String tipo_conta { get; set; }
        public String pedido_erp { get; set; }
        public int estado { get; set; }

        public String comments { get; set; }
        public decimal valor { get; set; }
        public String pedido_datetime { get; set; }
        public String codigo_consecutivo { get; set; }
        public String fecha { get; set; }
        public String pedido_fecha { get; set; }
        public String pedido_hora { get; set; }
        public String exactus_schema { get; set; }
    }


    public class _ProcesoGetDocumento
    {
        public String id_pedido; 
	    public String pedido_erp; 
	    public String tipo_doc; 
	    public String documento; 
	    public String fecha; 
	    public String valor; 
	    public String impuesto; 
	    public String accion; 
	    public String fc_numero;

	
	    public String esquema; 
	    public String id_costo; 
	    public String numero_erp; 

	    //public String tipo_doc; 
	    //public String documento; 
	    //public String fecha; 

	    public String master; 
	    public String proveedor; 
	    //public String valor; 
	    public String user;
        public String ip;

        public String server_user;
        public String server_pass;        

    }

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

    public class status_data
    {
        public String estatus_en { get; set; }
        public String estatus_es { get; set; }
        public int notificar_agente { get; set; }
        public int notificar_cliente { get; set; }
        public int notificar_shipper { get; set; }
    }


    public class bl_data
    {
        public int BlId { get; set; }
        public String Mbl { get; set; }
        public String Hbl { get; set; }
        public int ConsignerID { get; set; }
        public int ShipperID { get; set; }
        public int AgentID { get; set; }
        public int id_coloader { get; set; }
        public int id_cliente_order { get; set; }
        public int no { get; set; }
        public String Countries { get; set; }
        public int RoutingID { get; set; }
        public int ExType { get; set; }
        public int BLType { get; set; }
        public String EXDBCountry { get; set; }
        public int EXID { get; set; }
        public String CountriesDes { get; set; }
        public String Contenedor { get; set; }
        public Boolean en_transito { get; set; }
        public int id_usuario { get; set; }
        public String dua { get; set; }
        public String referencia { get; set; }
    }


    public class tracking_data
    {
        public int tracking_id { get; set; }
        public int bl_id { get; set; }
        public int BLStatus { get; set; }
        public String BLStatusName { get; set; }
        public String Comment { get; set; }

        public int ClientID { get; set; }


        public int id_viaje { get; set; }
        public String viaje { get; set; }
        public String contenedor { get; set; }
        public String mbl { get; set; }
        public String bl { get; set; }
        public int id_tipoestatus { get; set; }
        public String id_pais { get; set; }
        public String tipo_contenedor { get; set; }

        public String impex { get; set; }

        public String Countries { get; set; }
        public String CountriesDep { get; set; }
        public String CountriesDes { get; set; }

    }


    public class routing_data
    {
        public String routing { get; set; }
        public String order_no { get; set; }
        public String no_embarque { get; set; }
    }


    public class clientes_data
    {
        public String ConsignerName { get; set; }
        public String ShipperName { get; set; }
        public String ColoaderName { get; set; }
        public String NotifyName { get; set; }
    }


    public class contact_data
    {
        public int id_contacto { get; set; }

        public String nombre { get; set; }
        public String email { get; set; }
        public String telefono { get; set; }
        public String error { get; set; }

        public String tipo_persona { get; set; }
        public String contactoxpais { get; set; }
        public String copia { get; set; }
        public String rechazo { get; set; }
    }


    public class email_data
    {
        public String subject { get; set; }
        public String body { get; set; }
        public String text { get; set; }
        public String error { get; set; }
    }



    public class Scalar
    {
        public int ide { get; set; }
    }


    public class arg_data
    {
        public int tracking_id { get; set; }
        public String product { get; set; }
        public String sub_product { get; set; }
        public String impex { get; set; }
        public int bl_id { get; set; }
        public int status_id { get; set; }
        public Boolean produccion { get; set; }
        public String user { get; set; }
        public String ip { get; set; }
        public String Countries { get; set; }
        public String CountriesDest { get; set; }

        public int stat;
        public String msg;
        public String sent_si;
        public String sent_no;

    }



    public class arg_pedidos
    {
        public String PRODUCTO { get; set; }
        public String IMPEX { get; set; }
        public int BL_ID { get; set; }

        public String BODEGA { get; set; }
        public String ACTIVIDAD_COMERCIAL { get; set; }
        public String CONDICION_PAGO { get; set; }
        public String OBSERVACIONES { get; set; }

        public String COUNTRIES { get; set; }

        public String user { get; set; }
        public String ip { get; set; }
        
        public int stat;
        public String msg;
        public String nombre_cliente { get; set; }
        public String abierto { get; set; }
        public String pedido_erp { get; set; }
        
    }



    public class lang_data
    {
        public String eng { get; set; }
        public String esp { get; set; }
    }




    public class dev_data
    {
        public String web { get; set; }
        public String cod { get; set; }
        public String dev { get; set; }
    }

    public class mode_data
    {
        public String wsNotif { get; set; }
        public String sendNotif { get; set; }
        public String Test { get; set; }
        public String ws21 { get; set; }
    }



    public class _RESPUESTA
    {
        public string ASIENTO { get; set; }
        public string COD_COMPANIA { get; set; }
        public string COD_PAIS { get; set; }
        public string CODIGO_ERROR { get; set; }
        public string ESTADO { get; set; }
        public string MENSAJE { get; set; }
        public string PEDIDO { get; set; }
        public string PEDIDO_EXACTUS { get; set; }
        public string PEDIDO_RESP { get; set; }

        public string CODIGO { get; set; }
        public string DESCRIPCION { get; set; }
    }


    /*
    public class _RESPUESTA2
    {
        public string CODIGO { get; set; }
        public string DESCRIPCION { get; set; }
    }
    */

}