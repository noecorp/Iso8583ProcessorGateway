﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cerberus.ATC.GatewayV2Service.wsParametricas;
using Cerberus.ATC.GatewayV2Service.wsSeguridad;
using Cerberus.ATC.GatewayV2Service.wsVentas;
using NLog;

namespace Cerberus.ATC.GatewayV2Service
{
    public class MTI_ProcessingCodeRouter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static void ProcesarMensaje(byte[] byteRequest, out byte[] byteResponse)
        {
            logger.Info("Trama In" + new System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary(byteRequest).ToString());
            byteResponse = new byte[1];
            logger.Trace("len mensaje:" + byteRequest.Length);
            byte[] MTI100 = new byte[] { 0, 1 };
            byte[] MTI200 = new byte[] { 0, 2 };

            byte[] MTIconsulta = new byte[] { byteRequest[7], byteRequest[8] };
            byte[] bitmap = new byte[] { byteRequest[9], byteRequest[10], byteRequest[11], byteRequest[12], byteRequest[13], byteRequest[14], byteRequest[15], byteRequest[16] };
            byte[] processingCode = new byte[] { byteRequest[17], byteRequest[18], byteRequest[19], byteRequest[20], byteRequest[21], byteRequest[22] };
            byte[] header = MTIconsulta.Concat(bitmap).Concat(processingCode).ToArray<byte>();

            byte[] newArray = new byte[byteRequest.Length - 22];
            Buffer.BlockCopy(byteRequest, 21, newArray, 0, newArray.Length);
            //byte[] newArray = new byte[byteRequest.Length - 7];
            //Buffer.BlockCopy(byteRequest, 7, newArray, 0, newArray.Length);


            int i = newArray.Length - 1;
            while (newArray[i] == 0)
                --i;
            // now foo[i] is the last non-zero byte 
            byte[] bar = new byte[i + 1];
            Array.Copy(newArray, bar, i + 1);

            string mstrHeader = BitConverter.ToString(header).Replace("-", "");
            //string mstrHeader = Encoding.ASCII.GetString(header);
            string mstrBody = BitConverter.ToString(newArray).Replace("-", "").PadLeft(10, '0');

            string mstrMessage = mstrHeader + mstrBody;
            logger.Info("Mensaje Recibido ISO a procesar:" + mstrMessage);
            //mstrMessage = Encoding.ASCII.GetString(bar, 0, bar.Length);
            logger.Info("Mensaje Recibido ISO a procesar:" + mstrMessage);

            SOATProcessor sp = new SOATProcessor(mstrMessage);
            string[] contenidos6263 = Encoding.ASCII.GetString(newArray).Split('\0');
            sp.campo62 = contenidos6263[0];
            sp.campo63 = contenidos6263[1];
            string strResponseBody = sp.ProcesarMensaje();
            // Translate the passed message into ASCII and store it as a byte array.
            Byte[] responseWPOS = new Byte[1024];
            responseWPOS = System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary.Parse(strResponseBody).Value;
            byteResponse = responseWPOS;
            /*
             string strSegmentHeader = strResponseBody.Substring(1,30) ;
             byte[] headerLiteral = ConvertLiteralByteToString(strSegmentHeader);
             strResponseBody = strResponseBody.Substring(31);
             byte[] byteResponseBody = headerLiteral.Concat(Encoding.ASCII.GetBytes(strResponseBody)).ToArray(); 
             //byte[] byteHexedResBody = Encoding.Default.GetBytes(strResponseBody);
             //byte[] byteResponseBody = Encoding.ASCII.GetBytes(sp.ProcesarMensaje());
             //byteResponse = header.Concat(byteResponseBody).ToArray();
             byteResponse = byteResponseBody;
            //byteResponse = Encoding.ASCII.GetBytes(sp.ProcesarMensaje());
            */ //comentado 0412018


        }
        public static byte[] ConvertLiteralByteToString(string str)
        {
            char[] arrC = str.ToCharArray();
            List<byte> lstByte = new List<byte>();
            byte[] res = new byte[1024];
            foreach (char c in arrC)
            {
                if (Char.IsDigit(c))
                {
                    int convertC = int.Parse(c.ToString());
                    lstByte.Add(
                        BitConverter.GetBytes(convertC)[0]
                        );

                }
            }
            return (lstByte.ToArray());
        }
    }

    class SOATProcessor
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        string MsgReq { get; set; }
        string[] ParsedMsgReq { get; set; }
        string FlagMensaje { get; set; }
        string MTI { get; set; }
        public string campo62 { get; set; }
        public string campo63 { get; set; }

        public SOATProcessor(string inMsgReq)
        {
            ISO8583 msgIso8583 = new ISO8583();
            try
            {
                ParsedMsgReq = msgIso8583.Parse(inMsgReq);

                MsgReq = inMsgReq;
                MTI = ParsedMsgReq[0];
            }
            catch (Exception ex)
            {
                logger.Error("Error en parseado de la trama ISO:" + inMsgReq + " Detalle del error:" + ex.Message + ex.InnerException + ex.StackTrace);

            }

        }

        public string ProcesarMensaje()
        {

            string resMensaje = "";
            ISO8583 msgIso8583 = new ISO8583();
            string messageHex = "";
            //aca el processing code y el envio a UNIVIDA Y RESPUESTA
            switch (ParsedMsgReq[3])
            {
                case "010000": //autenticacion
                    FlagMensaje = "AUT_REQ";
                    logger.Info("Metodo de autenticacion invocado...");

                    logger.Info("Contenido62"+this.campo62);
                    string usuario = campo62.Split('/')[0].Trim();
                    string password = campo62.Split('/')[1].Trim();
                    string ip = campo63.Trim();
                    logger.Info(String.Format("Parametros:usuario:{0},password:{1},ip:{2}",usuario,password,ip)); 
                    string mensajeSoat=Autenticacion(usuario, password, ip);
                    string mensajeSoatHex = BitConverter.ToString(Encoding.Default.GetBytes(mensajeSoat)).Replace("-","");
                    Byte[] responseWPOS = new Byte[1024];
                    responseWPOS = System.Runtime.Remoting.Metadata.W3cXsd2001.SoapHexBinary.Parse(mensajeSoat).Value;


                    messageHex = "00d060000002330110200000000200000601000030300013313931393139313931393139310171447c424e7c42454e495c447c43427c434f43484142414d42415c447c43487c434855515549534143415c447c4c507c4c412050415a5c477c323031387c323031395c477c323031397c323031395c557c317c504152544943554c41525c557c327c5055424c49434f5c557c337c454a45524349544f5c557c347c504f4c494349415c557c357c4f46494349414c5c567c317c4d4f544f4349434c4554415c567c327c4155544f4d4f56494c";
                    // Translate the passed message into ASCII and store it as a byte array.

                    resMensaje = messageHex;

                    /*string[] DE = new string[130];
                    MTI = "0110";
                    DE[3] = "010000";
                    DE[39] = "010000";
                    DE[62] = "1919191919191"; //TOKEN
                    DE[63] = @"D|BN|BENI\D|CB|COCHABAMBA\D|CH|CHUQUISACA\D|LP|LA PAZ\D|OR|ORURO\D|PN|PANDO\D|PT|POTOSI\D|SC|SANTA CRUZ\D|TJ|TARIJA\U|1|PARTICULAR\U|2|PUBLICO\U|3|EJERCITO\U|4|POLICIA\U|5|OFICIAL\V|1|MOTOCICLETA\V|2|AUTOMOVIL\V|3|JEEP\V|4|CAMIONETA\V|5|VAGONETA\V|6|MICROBUS\V|7|COLECTIVO\V|8|OMNIBUS/FLOTA(MAS DE 39 oc)\V|9|TRACTO CAMION\V|10|MINIBUS(8 OCUPANTES)\V|11|MINIBUS(11 OCUPANTES)\V|12|MINIBUS(15 OCUPANTES)\V|13|CAMION(3 OCUPANTES)\V|14|CAMION(18 OCUPANTES)\V|15|CAMION(25 OCUPANTES)\";
                    resMensaje = msgIso8583.Build(DE, MTI);
                     */
                    logger.Info("Mensaje de repuesta:" + resMensaje);
                    break;
                case "020000": //validacion de placa
                               /*string[] DE1 = new string[130];
                               FlagMensaje = "VAL_REQ";
                               logger.Info("Metodo de Validacion de Placa invocado...");
                               MTI = "0110";
                               DE1[3] = "020000";
                               DE1[39] = "10"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE
                               resMensaje = msgIso8583.Build(DE1, MTI);
                               */
                    messageHex = "002f600000023301103000000002000002020000000000010000303000194b5253313031302f30312f30322f30352f4c50";
                    resMensaje = messageHex;
                    /*string token = ParsedMsgReq[62].Trim();
                    string placa = ParsedMsgReq[63].Trim();
                    logger.Info(String.Format("Parametros:token:{0},placa:{1}", token, placa));
                    resMensaje = ValidacionPlaca(token, placa);*/
                    break;
                case "040000": //enrolado y calculo de prima
                    string[] DE2 = new string[130];
                    FlagMensaje = "ENROL_REQ";
                    logger.Info("Calculo de prima con placa enrolada...");
                    MTI = "0110";
                    DE2[3] = "030000";
                    DE2[4] = "000000010000"; //PRIMA A COBRAR en este caso 100.00 Bs
                    DE2[39] = "00"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE
                    DE2[63] = "KRS1010/01/01/02/LP"; //PLACA/ID_GESTION/ID_VEHICULO/ID_USO/ID_DEPARTAMENTO_ EL CODIGO DE GESTION SIEMPRE 
                    resMensaje = msgIso8583.Build(DE2, MTI);
                    break;
                case "050000": //notificacion  de cobro de prima
                    string[] DE3 = new string[130];
                    FlagMensaje = "NOTIF_REQ";
                    logger.Info("Notificacion de cobro de prima...");
                    MTI = "0210";
                    DE3[3] = "050000";
                    DE3[39] = "00"; //CODIGO DE RESPUESTA , 00 EXITOSO, 01 ERROR
                    DE3[62] = @"000001\00002\426801800001849\12/11/2018\000021\Ley N° 453 Tienes derecho a un trato equitativo sin discriminacion en la oferta de servicios ESTA FACTURA CONTIBUYE AL DESARROLLO DEL PAIS, EL USO ILICITO DE ESTA SERÁ SANCIONADO DE ACUERDO A LEY/TIPO EMISION\FRANSISCO\3968971\SON: CIENTO VEINTISEIS CON 00/100 BOLIVIANOS\16-F2-81-21-1F\126.00\15/11/2018\426801800001849|21|15/11/2018|126.00|126.00|16-F2-81-21-1F|3968971|0.00|0.00|0.00|0.00\UNIVIDA S.A.\AV. CAMACHO ° 1425, EDIFICIO CRISPIERI NARDINI PLANTA BAJA - ZONA CENTRAL\TELEFONO 21510000 - 71561427\301204024\PLANES DE SEGUROS DE VIDA\SUCURSAL N°1\00000\LUGAR\LA PAZ- BOLIVIA\";
                    //DE[62] = NUMEROTRAMITE\NUMEROCOMPROBANTE\NUMEROAUTORIZACION\FECHA_LIMITE_EMISION\NUMERO_FACTURA\LEYENDA\TIPO_EMISION\RAZON_SOCIAL\NIT_CLIENTE\IMPORTE_LITERAL\CODIGO_CONTROL\IMPORTE_NUMERAL\FECHA_EMISION\CODIGOQR\RAZON_SOCIAL_UNIVIDA\DIRECCION_UNIVIDA\TELEFONOS_UNIVIDA\NIT_UNIVIDA\ACTIVIDAD_ECO_UNIVIDA\NOMBRE_SUCURSAL_UNIVIDA\DIRECCION_SUCURSAL_UNIVIDA\TELEFONO_SUCURSAL_UNIVIDA\NUMERO_SUCURSAL\LUGAR\MUNICIPIO_DEPTO
                    DE3[63] = @"SOAT (NUEVO)2019,  MINIBUS(8 OCUPANTES) PUBLICO PLACA 376XRI?1.00?126.00?126.00\";
                    //DE[63] = DETALLE DE LA FACTURA EN EL SIGUIENTE ORDEN: 
                    //DETALLE?CANTIDAD?PRECIO?SUBTOTAL
                    //CADA LINEA DIVIDA POR SALTO DE LINEA
                    resMensaje = msgIso8583.Build(DE3, MTI);
                    break;
                case "030000": //vendible y calculo de prima
                    string[] DE4 = new string[130];
                    FlagMensaje = "CALC_REQ";
                    logger.Info("Calculo de prima con placa nueva...");
                    MTI = "0110";
                    DE4[3] = "040000";
                    DE4[4] = "000000010000"; //PRIMA A COBRAR en este caso 100.00 Bs
                    DE4[39] = "00"; //CODIGO DE RESPUESTA , 00 exitoso, 01 error
                    resMensaje = msgIso8583.Build(DE4, MTI);
                    break;
                case "060000": //solicitud de listado de venta
                    string[] DE5 = new string[130];
                    FlagMensaje = "LISTA_REQ";
                    logger.Info("Solicitud de listado de venta");
                    MTI = "0210";
                    DE5[3] = "060000";
                    DE5[39] = "00"; //CODIGO DE RESPUESTA , 00 EXITOSO, 01 ERROR
                    DE5[62] = @"00002\00002\00000"; //SOAT TOTAL\SOAT VALIDOS\SOAT REVERTIDOS SUMARIZADORES
                    DE5[63] = @"000021|KCS2933|00002|21|15-01-2019|100.00|CON COBERTURA 0A 000022|XKC2933|00003|22|15-01-2019|120.00|CON COBERTURA";
                    resMensaje = msgIso8583.Build(DE5, MTI);
                    break;
                case "070000": //solicitud de factura
                    string[] DE6 = new string[130];
                    FlagMensaje = "FACT_REQ";
                    logger.Info("Solicitud factura");
                    MTI = "0210";
                    DE6[3] = "070000";
                    DE6[39] = "00"; //CODIGO DE RESPUESTA , 00 EXITOSO, 01 ERROR
                    DE6[62] = @"000001\00002\426801800001849\12/11/2018\000021\Ley N° 453 Tienes derecho a un trato equitativo sin discriminacion en la oferta de servicios ESTA FACTURA CONTIBUYE AL DESARROLLO DEL PAIS, EL USO ILICITO DE ESTA SERÁ SANCIONADO DE ACUERDO A LEY/TIPO EMISION\FRANSISCO\3968971\SON: CIENTO VEINTISEIS CON 00/100 BOLIVIANOS\16-F2-81-21-1F\126.00\15/11/2018\426801800001849|21|15/11/2018|126.00|126.00|16-F2-81-21-1F|3968971|0.00|0.00|0.00|0.00\UNIVIDA S.A.\AV. CAMACHO ° 1425, EDIFICIO CRISPIERI NARDINI PLANTA BAJA - ZONA CENTRAL\TELEFONO 21510000 - 71561427\301204024\PLANES DE SEGUROS DE VIDA\SUCURSAL N°1\00000\LUGAR\LA PAZ- BOLIVIA\";
                    DE6[63] = @"SOAT (NUEVO)2019,  MINIBUS(8 OCUPANTES) PUBLICO PLACA 376XRI?1.00?126.00?126.00\";
                    resMensaje = msgIso8583.Build(DE6, MTI);

                    break;

            }
            return resMensaje;
        }
        string EnroladoCalculoPrima(string token, string placa, int idGestion, int idTipo, int idUso, string idDepto)
        {
            string resMensaje = "";
            ISO8583 msgIso8583 = new ISO8583();
            IwsVentasClient client = new IwsVentasClient();
            CEVen02ObtenerPrima reqCEVen02ObtenerPrima = new CEVen02ObtenerPrima();
            reqCEVen02ObtenerPrima.SeguridadToken = Int32.Parse(token);
            reqCEVen02ObtenerPrima.SoatTIntermediarioFk = 0;
            reqCEVen02ObtenerPrima.SoatTParDepartamentoPcFk = idDepto;
            reqCEVen02ObtenerPrima.SoatTParGestionFk = idGestion;
            reqCEVen02ObtenerPrima.SoatTParVehiculoTipoFk = idTipo;
            reqCEVen02ObtenerPrima.SoatTParVehiculoUsoFk = idUso;
            reqCEVen02ObtenerPrima.SoatTParVentaCanalFk = 30;
            reqCEVen02ObtenerPrima.SoatVentaCajero = "EDWIN";
            reqCEVen02ObtenerPrima.SoatVentaVendedor = "EDWIN";
            reqCEVen02ObtenerPrima.Usuario = "EDWIN";
            reqCEVen02ObtenerPrima.VehiPlaca = placa;
            logger.Info("llamada metodo Ven02ObtenerPrima");
            CSVen02ObtenerPrima resCSVen02ObtenerPrima = client.Ven02ObtenerPrima(reqCEVen02ObtenerPrima);
            logger.Info("Respuesta metodo Ven02ObtenerPrima" + resCSVen02ObtenerPrima.Mensaje);
            if (resCSVen02ObtenerPrima.Exito)
            {
                string[] messageRespuesta = new string[130];
                MTI = "0110";
                messageRespuesta[3] = "030000";
                messageRespuesta[4] = resCSVen02ObtenerPrima.Prima.ToString();
                messageRespuesta[3] = "030000";
                messageRespuesta[39] = "00"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE


                resMensaje = msgIso8583.Build(messageRespuesta, MTI);
                logger.Info("mensaje enviado:" + resMensaje);
            }
            return resMensaje;

        }
        string ValidacionPlaca(string token, string placa)
        {
            string resMensaje = "";
            ISO8583 msgIso8583 = new ISO8583();
            IwsVentasClient client = new IwsVentasClient();
            try
            {

                CEVen01ValidarVendibleYObtenerDatos reqCeVen01ValidarVendibleYObtenerDatos = new CEVen01ValidarVendibleYObtenerDatos();
                reqCeVen01ValidarVendibleYObtenerDatos.SoatVentaCajero = "EDWIN"; //preguntar
                reqCeVen01ValidarVendibleYObtenerDatos.SeguridadToken = int.Parse(token);
                reqCeVen01ValidarVendibleYObtenerDatos.SoatTIntermediarioFk = 0; //siempre 0
                reqCeVen01ValidarVendibleYObtenerDatos.SoatTParGestionFk = 2018; //preguntar
                reqCeVen01ValidarVendibleYObtenerDatos.SoatTParVentaCanalFk = 30; //preguntar
                reqCeVen01ValidarVendibleYObtenerDatos.SoatVentaVendedor = "EDWIN"; //preguntar
                reqCeVen01ValidarVendibleYObtenerDatos.Usuario = "EDWIN"; //preguntar
                reqCeVen01ValidarVendibleYObtenerDatos.VehiPlaca = placa;
                logger.Info("llamada metodo Ven01ValidarVendibleYObtenerDatos");
                CSVen01ValidarVendibleYObtenerDatos resCsVen01ValidarVendibleYObtenerDatos =
                    client.Ven01ValidarVendibleYObtenerDatos(reqCeVen01ValidarVendibleYObtenerDatos);
                logger.Info("Respuesta metodo Ven01ValidarVendibleYObtenerDatos" + resCsVen01ValidarVendibleYObtenerDatos.Mensaje);
                if (resCsVen01ValidarVendibleYObtenerDatos.Exito)
                {
                    if (resCsVen01ValidarVendibleYObtenerDatos.oSoatDatosIniciales == null) //es vendible pero no esta enrolado
                    {
                        string[] messageRespuesta = new string[130];
                        MTI = "0110";
                        messageRespuesta[3] = "020000";
                        messageRespuesta[39] = "10"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE
                        resMensaje = msgIso8583.Build(messageRespuesta, MTI);
                    }
                    else //es vendible y esta enrolado
                    {
                        CSoatDatosIniciales datosIniciales = resCsVen01ValidarVendibleYObtenerDatos.oSoatDatosIniciales;
                        string[] messageRespuesta = new string[130];
                        MTI = "0110";
                        messageRespuesta[3] = "030000";
                        messageRespuesta[39] = "00"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE
                        messageRespuesta[63] = String.Format("{0}/{1}/{2}/{3}/{4}", datosIniciales.VehiPlaca, datosIniciales.SoatTParGestionFk, datosIniciales.SoatTParVehiculoTipoFk, datosIniciales.SoatTParVehiculoUsoFk, datosIniciales.SoatTParDepartamentoPcFk);
                        resMensaje = msgIso8583.Build(messageRespuesta, MTI);
                    }
                }
                else //no es vendible
                {
                    string[] messageRespuesta = new string[130];
                    MTI = "0110";
                    messageRespuesta[3] = "020000";
                    messageRespuesta[39] = "01"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE
                    resMensaje = msgIso8583.Build(messageRespuesta, MTI);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error metodo Ven01ValidarVendibleYObtenerDatos,detalle:" + ex.Message + ex.InnerException + ex.StackTrace);
                string[] messageRespuesta = new string[130];
                MTI = "0110";
                messageRespuesta[3] = "020000";
                messageRespuesta[39] = "99"; //CODIGO DE RESPUESTA,00=ENROLADO Y SI VENDIBLE,10= NO ENROLADO PERO SI VENDIBLE , 01 NO VENDIBLE, 99 ERROR
                resMensaje = msgIso8583.Build(messageRespuesta, MTI);
            }
            logger.Info("mensaje enviado:" + resMensaje);
            return resMensaje;
        }
        string Autenticacion(string usuario, string password, string ip)
        {
            //autenticar en BD
            usuario = "ESEGOBIANO";
            password = "123456";
            string resMensaje = "";
            ISO8583 msgIso8583 = new ISO8583();
            int token = 0;
            //llamada al metodo de seguridad
            IwsSeguridadClient client = new IwsSeguridadClient();
            CESeg01Autenticacion oCeSeg01Autenticacion = new CESeg01Autenticacion
            {
                Contrasenia = password,
                Ip = ip,
                SoatTIntermediarioFk = 0,
                Usuario = usuario
            };
            try
            {
                logger.Info("llamada metodo Seg01Autenticacion");
                CSSeg01Autenticacion res = client.Seg01Autenticacion(oCeSeg01Autenticacion);
                logger.Info("Respuesta metodo Seg01Autenticacion" + res.Mensaje);
                if (res.Exito)
                {
                    token = res.SeguridadToken;

                    IwsParametricasClient clientParametricasClient = new IwsParametricasClient();
                    CEParObtenerDepartamentos reqParObtenerDepartamentos = new CEParObtenerDepartamentos();
                    reqParObtenerDepartamentos.SeguridadToken = token;
                    reqParObtenerDepartamentos.SoatTIntermediarioFk = 0;
                    reqParObtenerDepartamentos.Usuario = usuario;
                    logger.Info("llamada metodo ParObtenerDepartamentos");
                    CSParObtenerDepartamentos resParObtenerDepartamentos = clientParametricasClient.ParObtenerDepartamentos(reqParObtenerDepartamentos);
                    logger.Info("Respuesta metodo ParObtenerDepartamentos" + resParObtenerDepartamentos.Mensaje);
                    if (resParObtenerDepartamentos.Exito)
                    {
                        string parametrosRes = "";
                        foreach (var depto in resParObtenerDepartamentos.lDepartamento)
                        {
                            parametrosRes += String.Format(@"D|{0}|{1}", depto.CodigoDepartamento, depto.Descripcion) + (char)10;

                        }
                        CEParObtenerVehiculoUsos reqCeParObtenerVehiculoUsos = new CEParObtenerVehiculoUsos();
                        reqCeParObtenerVehiculoUsos.SeguridadToken = token;
                        reqCeParObtenerVehiculoUsos.SoatTIntermediarioFk = 0;
                        reqCeParObtenerVehiculoUsos.Usuario = usuario;
                        logger.Info("llamada metodo ParObtenerVehiculoUsos");
                        CSParObtenerVehiculoUsos resCsParObtenerVehiculoUsos =
                            clientParametricasClient.ParObtenerVehiculoUsos(reqCeParObtenerVehiculoUsos);
                        logger.Info("Respuesta metodo ParObtenerVehiculoUsos" + resCsParObtenerVehiculoUsos.Mensaje);
                        if (resCsParObtenerVehiculoUsos.Exito)
                        {
                            foreach (var vehiUso in resCsParObtenerVehiculoUsos.lVehiculoUso)
                            {
                                parametrosRes += String.Format(@"U|{0}|{1}", vehiUso.Secuencial, vehiUso.Descripcion) + (char)10;

                            }
                            CEParObtenerVehiculoTipos reqCeParObtenerVehiculoTipos = new CEParObtenerVehiculoTipos();
                            reqCeParObtenerVehiculoTipos.SeguridadToken = token;
                            reqCeParObtenerVehiculoTipos.SoatTIntermediarioFk = 0;
                            reqCeParObtenerVehiculoTipos.Usuario = usuario;
                            logger.Info("llamada metodo ParObtenerVehiculoTipos");
                            CSParObtenerVehiculoTipos resCsParObtenerVehiculoTipos =
                                clientParametricasClient.ParObtenerVehiculoTipos(reqCeParObtenerVehiculoTipos);
                            logger.Info("Respuesta metodo ParObtenerVehiculoTipos" + resCsParObtenerVehiculoTipos.Mensaje);
                            if (resCsParObtenerVehiculoTipos.Exito)
                            {
                                foreach (var tipo in resCsParObtenerVehiculoTipos.lVehiculoTipo)
                                {
                                    parametrosRes += String.Format(@"V|{0}|{1}", tipo.Secuencial, tipo.Descripcion) + (char)10;
                                }
                                MTI = "0110";
                                string[] messageRespuesta = new string[130]; ;
                                messageRespuesta[3] = "010000";
                                messageRespuesta[39] = "00";
                                messageRespuesta[62] = token.ToString();
                                messageRespuesta[63] = parametrosRes;
                                //resMensaje = String.Join("", messageRespuesta);
                                resMensaje = msgIso8583.Build(messageRespuesta, MTI);
                            }


                        }
                        else
                        {
                            throw new Exception();

                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error metodo Autenticacion,detalle:" + ex.Message + ex.InnerException + ex.StackTrace);
                MTI = "0110";
                string[] messageRespuesta = new string[130]; ;
                messageRespuesta[3] = "010000";
                messageRespuesta[39] = "01"; //01 error 00 exito
                resMensaje = msgIso8583.Build(messageRespuesta, MTI);

            }
            logger.Info("mensaje enviado:" + resMensaje);
            return resMensaje;
        }
    }
}
