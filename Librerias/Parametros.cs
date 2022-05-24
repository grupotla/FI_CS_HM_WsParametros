using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

public class Parametros
{
    public Parametros()
    {
    }


    public static Struct.Result send(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip)
    {

        Struct.Result res = new Struct.Result();

        try
        {
            Struct.ArrParams Params = Postgres_.EmpresaParametros(pais_iso, sistema, "", "", "");

            if (Params.error != null && Params.error != "")
            {
                res.stat = 4;
                res.msg = Params.error;
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }

            if (!Params.trackactivo)
            {
                res.stat = 3;
                res.msg = "Servicio SMTP Inactivo (" + pais_iso + ")";
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }


            if (fromName == null)
                fromName = "";

            fromName = fromName.ToUpper().Trim();
            if (fromName != "") fromName += " ";
            fromName += Params.firma;

            string base64Decoded = Utils.Base64Decode(body);

            base64Decoded = base64Decoded.Replace("#*logo*#", System.Convert.ToBase64String(Params.logo));
            base64Decoded = base64Decoded.Replace("#*home_page*#", Params.home_page);
            base64Decoded = base64Decoded.Replace("#*firma*#", Params.firma);
            //base64Decoded = base64Decoded.Replace("#*nombre_pais*#", string.IsNullOrEmpty(Params.nombre_pais) ? pais_iso : Params.nombre_pais);
            base64Decoded = base64Decoded.Replace("#*nombre_pais*#", string.IsNullOrEmpty(Params.firma) ? pais_iso : Params.firma);

            subject = subject.Replace("#*firma*#", Params.firma);

            var Email = new MailMessage()
            {
                From = new MailAddress(Params.trackfromaddress, fromName, System.Text.Encoding.UTF8),
                Subject = subject,
                IsBodyHtml = true,
                Body = base64Decoded
            };
            Email.To.Add(new MailAddress(to.Trim()));

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;

            var Cliente_Smtp = new SmtpClient(Params.trackmailserver, Params.trackpuerto)
            {
                Timeout = 3600000,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                EnableSsl = Params.trackauth == 1 ? true : false,
                Credentials = new System.Net.NetworkCredential() { UserName = Params.trackfromaddress, Password = Params.trackpassword }, //Credentials = CredentialCache.DefaultNetworkCredentials,
                //UseDefaultCredentials = false,
            };
            Cliente_Smtp.Send(Email);

            res.stat = 1;
            res.msg = "Tracking enviado con Exito.";

        }
        catch (Exception ex)
        {
            res.stat = 2;
            res.msg = ex.Message; //ex.InnerException.HResult + " - " + ex.InnerException.Message; // +" - " + ex.InnerException.InnerException.ToString();
        }

        Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
        return res;
    }




    public static Struct.Result sendattach(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip, string cc, string bc, string attachments)
    {

        Struct.Result res = new Struct.Result();

        try
        {
            Struct.ArrParams Params = Postgres_.EmpresaParametros(pais_iso, sistema, "", "", "");

            if (Params.error != null && Params.error != "")
            {
                res.stat = 4;
                res.msg = Params.error;
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }

            if (!Params.trackactivo)
            {
                res.stat = 3;
                res.msg = "Servicio SMTP Inactivo (" + pais_iso + ")";
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }


            if (fromName == null)
                fromName = "";

            fromName = fromName.ToUpper().Trim();
            if (fromName != "") fromName += " ";
            fromName += Params.firma;

            string base64Decoded = "";

            if (Utils.IsBase64String(body))
            {
                base64Decoded = Utils.Base64Decode(body);
                base64Decoded = base64Decoded.Replace("#*logo*#", System.Convert.ToBase64String(Params.logo));
                base64Decoded = base64Decoded.Replace("#*home_page*#", Params.home_page);
                base64Decoded = base64Decoded.Replace("#*firma*#", Params.firma);
                base64Decoded = base64Decoded.Replace("#*nombre_pais*#", Params.nombre_pais);
            }
            else
            {

                base64Decoded = body;
            }


            subject = subject.Replace("#*firma*#", Params.firma);

            var Email = new MailMessage()
            {
                From = new MailAddress(Params.trackfromaddress, fromName, System.Text.Encoding.UTF8),
                Subject = subject,
                IsBodyHtml = true,
                Body = base64Decoded
            };


            if (!String.IsNullOrEmpty(to))
            {
                string[] emails = to.Split(';');
                foreach (var email in emails)
                {
                    Email.To.Add(new MailAddress(email.Trim()));
                }
            }

            if (!String.IsNullOrEmpty(cc))
            {
                string[] copias = cc.Split(';');
                foreach (var copia in copias)
                {
                    Email.CC.Add(new MailAddress(copia.Trim()));
                }
            }

            if (!String.IsNullOrEmpty(bc))
            {
                string[] blinds = bc.Split(';');
                foreach (var blind in blinds)
                {
                    Email.Bcc.Add(new MailAddress(blind.Trim()));
                }
            }

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

                        var filename = System.IO.Path.GetFileName(row.Item(0).InnerText);
                        var file64 = row.Item(1).InnerText;

                        //.GetFileNameWithoutExtension(filename);
                        string extension = System.IO.Path.GetExtension(filename);

                        var bytes = Convert.FromBase64String(file64);
                        System.IO.MemoryStream strm = new System.IO.MemoryStream(bytes);
                        Attachment data = new Attachment(strm, filename);
                        System.Net.Mime.ContentDisposition disposition = data.ContentDisposition;
                        data.ContentId = filename;
                        data.ContentDisposition.Inline = true;

                        Email.Attachments.Add(data);
                    }
                }
            }

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;

            var Cliente_Smtp = new SmtpClient(Params.trackmailserver, Params.trackpuerto)
            {
                Timeout = 3600000,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,

                EnableSsl = Params.trackauth == 1 ? true : false,
                Credentials = new System.Net.NetworkCredential() { UserName = Params.trackfromaddress, Password = Params.trackpassword }, //Credentials = CredentialCache.DefaultNetworkCredentials,
                //UseDefaultCredentials = false,
            };
            Cliente_Smtp.Send(Email);

            res.stat = 1;
            res.msg = "Email enviado con Exito.";

            Email = null;
            Cliente_Smtp = null;

        }
        catch (Exception ex)
        {
            res.stat = 2;
            res.msg = ex.Message;
        }

        Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
        return res;
    }


    public static Struct.Result sendattachOne(string pais_iso, string to, string subject, string body, string fromName, string sistema, string user, string ip, string cc, string bc, string filename, string file64, string empresa)
    {

        Struct.Result res = new Struct.Result();

        try
        {
            Struct.ArrParams Params = Postgres_.EmpresaParametros(pais_iso, sistema, "", "", "");

            if (Params.error != null && Params.error != "")
            {
                res.stat = 4;
                res.msg = Params.error;
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }

            if (!Params.trackactivo)
            {
                res.stat = 3;
                res.msg = "Servicio SMTP Inactivo (" + pais_iso + ")";
                Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
                return res;
            }


            if (fromName == null)
                fromName = "";

            fromName = fromName.ToUpper().Trim();
            if (fromName != "") fromName += " ";
            fromName += Params.firma;

            string base64Decoded = "";

            if (Utils.IsBase64String(body))
            {
                base64Decoded = Utils.Base64Decode(body);
                base64Decoded = base64Decoded.Replace("#*logo*#", System.Convert.ToBase64String(Params.logo));
                base64Decoded = base64Decoded.Replace("#*home_page*#", Params.home_page);
                base64Decoded = base64Decoded.Replace("#*firma*#", Params.firma);
                base64Decoded = base64Decoded.Replace("#*nombre_pais*#", Params.nombre_pais);
            }
            else
            {

                base64Decoded = body;
            }


            subject = subject.Replace("#*firma*#", Params.firma);

            var Email = new MailMessage()
            {
                From = new MailAddress(Params.trackfromaddress, fromName, System.Text.Encoding.UTF8),
                Subject = subject,
                IsBodyHtml = true,
                Body = base64Decoded
            };


            string result = "";

            List<string> emailsStr = Postgres_.ContactosPaisCostos(empresa, "produccion", ref result);

            if (result != "")
            {
                res.stat = 2;
                res.msg = result;
                return res;
            }



            to = emailsStr[0];

            if (!String.IsNullOrEmpty(to))
            {
                string[] emails = to.Split(';');
                foreach (var email in emails)
                {
                    Email.To.Add(new MailAddress(email.Trim()));
                }
            }



            cc = emailsStr[1];

            if (!String.IsNullOrEmpty(cc))
            {
                string[] copias = cc.Split(';');
                foreach (var copia in copias)
                {
                    Email.CC.Add(new MailAddress(copia.Trim()));
                }
            }


            bc = emailsStr[2];

            if (!String.IsNullOrEmpty(bc))
            {
                string[] blinds = bc.Split(';');
                foreach (var blind in blinds)
                {
                    Email.Bcc.Add(new MailAddress(blind.Trim()));
                }
            }

            if (!String.IsNullOrEmpty(file64))
            {
                string extension = System.IO.Path.GetExtension(filename);

                var bytes = Convert.FromBase64String(file64);
                System.IO.MemoryStream strm = new System.IO.MemoryStream(bytes);
                Attachment data = new Attachment(strm, filename);
                System.Net.Mime.ContentDisposition disposition = data.ContentDisposition;
                data.ContentId = filename;
                data.ContentDisposition.Inline = true;

                Email.Attachments.Add(data);   
            }

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;

            var Cliente_Smtp = new SmtpClient(Params.trackmailserver, Params.trackpuerto)
            {
                Timeout = 3600000,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,

                EnableSsl = Params.trackauth == 1 ? true : false,
                Credentials = new System.Net.NetworkCredential() { UserName = Params.trackfromaddress, Password = Params.trackpassword }, //Credentials = CredentialCache.DefaultNetworkCredentials,
                //UseDefaultCredentials = false,
            };
            Cliente_Smtp.Send(Email);

            res.stat = 1;
            res.msg = "Email enviado con Exito.";

            Email = null;
            Cliente_Smtp = null;

        }
        catch (Exception ex)
        {
            res.stat = 2;
            res.msg = ex.Message;
        }

        Postgres_.log(res.stat, user, sistema, pais_iso, to, subject, body, ip, res.msg);
        return res;
    }

    public static Struct.Result upload_files(string ruta, string filename_old, string user, string ip, string attachments, Boolean erase)
    {

        Struct.Result res = new Struct.Result();

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

    }



}