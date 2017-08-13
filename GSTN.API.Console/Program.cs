﻿using eSignASPLib;
using eSignASPLib.DTO;
using Risersoft.API.GSTN;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.OpenPgp.Examples;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GSTN.API;

namespace Risersoft.API.GSTN.Console
{
    class Program
    {
        static eSign eSignObj;
        static void Main(string[] args)
        {
            string gstin = "", userid="", fp = "", filename = "", ctin = "",etin="";
       
            if ((args != null) && (System.IO.File.Exists(args[0])))
            {
                filename = args[0];
            }
            else
            {
                System.Console.Write("Enter input filename [Press Enter for None]:");
                filename = System.Console.ReadLine();
            }


            if (!String.IsNullOrEmpty(filename) && (System.IO.File.Exists(filename)))
            {
                var fileContents = File.ReadAllText(filename);
                string[] arr = Strings.Split(fileContents, Constants.vbCrLf);
                GSTNConstants.client_id = arr[0];
                GSTNConstants.client_secret = arr[1];
                userid = arr[2];
                gstin = arr[3];
                fp = arr[4];
                ctin = arr[5];
                etin = arr[6];

            }
            else
            {
                System.Console.Write("Enter ClientId:");
                GSTNConstants.client_id = System.Console.ReadLine();

                System.Console.Write("Enter Client Secret:");
                GSTNConstants.client_secret = System.Console.ReadLine();

                System.Console.Write("Enter UserID:");
                userid = System.Console.ReadLine();

                System.Console.Write("Enter GSTIN:");
                gstin = System.Console.ReadLine();

                System.Console.Write("Enter FP:");
                fp = System.Console.ReadLine();
            }

            try
            {
                GSTNConstants.publicip = new WebClient().DownloadString("http://ipinfo.io/ip").Trim();
            }
            catch
            {
                GSTNConstants.publicip = "11.10.1.1";
            }


            System.Console.WriteLine("1=GSTR1 Get, 2=GSTR2 Get, 3=GSTR3 Get, 4=Ledger, " +
                "5=File with eSign, 6=CSV conversion, 7=PGP, 8=File With DSC, " +
                "9=GSTR1 Save, 10=GSTR2 Save, 11=GSTR3 Save, 12=Refresh Token, 13=Register DSC, 14=Search");
            string selection = System.Console.ReadLine();

            switch (selection)
            {
                case "1":
                    TestGSTR1Get(gstin, userid, fp);
                    break;
                case "2":
                    TestGSTR2Get(gstin, userid, fp);
                    break;
                case "3":
                    TestGSTR3(gstin, userid, fp);
                    break;
                case "4":
                    TestLedger(gstin, userid, "19-08-2016", "20-09-2016");
                    break;
                case "5":
                    System.Console.Write("Enter path to license file:");
                    string path = System.Console.ReadLine();
                    System.Console.Write("Enter your Aadhar Num:");
                    string aadhaarnum = System.Console.ReadLine();
                    string transactionid = GetUIDAIOtp(path, aadhaarnum);
                    System.Console.Write("Enter OTP:");
                    string otp = System.Console.ReadLine();
                    FileGSTR1WithESign(gstin, userid, fp, aadhaarnum, transactionid, otp);
                    break;
                case "6":
                    TestCSV(gstin, userid, fp);
                    break;
                case "7":
                    TestPGP("the quick brown fox jumped over the lazy dog");
                    break;
                case "8":
                    System.Console.Write("Enter your PAN:");
                    string pan = System.Console.ReadLine();
                    FileGSTR1WithDSC(gstin, userid, fp, pan);
                    break;
                case "9":
                    TestGSTR1Save(gstin, userid, fp, ctin,etin);
                    break;
                case "10":
                    TestGSTR2Save(gstin, userid, fp, ctin);
                    break;
                case "12":
                    GSTNAuthClient client = GetAuth(gstin, userid);
                    client.RefreshToken();
                    break;
                case "13":
                    System.Console.Write("Enter your PAN:");
                    string pan2 = System.Console.ReadLine();
                    RegisterDSC(gstin, userid, pan2);
                    break;
                case "14":
                    Search(gstin);
                    break;

            }

            System.Console.WriteLine("Press any key to end this program");
            System.Console.ReadKey(false);
        }

        private static void TestPGP(string message)
        {
            string pwd = "mypassword";
            System.IO.Directory.CreateDirectory(@"D:\Keys");
            PGPSnippet.KeyGeneration.PGPKeyGenerator.GenerateKey("GSTNUser", pwd, @"D:\Keys\");
            System.Console.WriteLine("Keys Generated Successfully");

            String encoded = DetachedSignatureProcessor.CreateSignature(message, @"D:\Keys\PGPPrivateKey.asc", "signature.asc", pwd.ToCharArray(), true);
            System.Console.WriteLine("Obtained Signature = " + encoded);
            bool verified = DetachedSignatureProcessor.VerifySignature(message, encoded, @"D:\Keys\PGPPublicKey.asc");
            if (verified)
            {
                System.Console.WriteLine("signature verified.");
            }
            else
            {
                System.Console.WriteLine("signature verification failed.");
            }


        }
        private static string GetUIDAIOtp(string path, string aadhaarnum)
        {
            eSignObj = new eSign(path);    //Get your own license file from e-Mudhra
            Settings.PfxPath = "resources\\Docsigntest.pfx";
            Settings.PfxPassword = "emudhra";
            Settings.UIDAICertificatePath = "resources\\uidai_auth_prod.cer";
            Settings.AuthMode = AuthMode.OTP;
            string guid = System.Guid.NewGuid().ToString();
            Response OTPResponse = eSignObj.GetOTP(aadhaarnum, guid);
            return guid;
        }
        private static void FileGSTR1WithESign(string gstin, string userid, string fp, string aadhaarnum, string transactionId, string Otp)
        {
            GSTNAuthClient client = GetAuth(gstin,userid);
            GSTR1ApiClient client2 = new GSTR1ApiClient(client, gstin, fp);
            var model2 = client2.GetSummary().Data;

            //https://groups.google.com/forum/#!searchin/gst-suvidha-provider-gsp-discussion-group/authorized|sort:relevance/gst-suvidha-provider-gsp-discussion-group/9-_Mk7LatDs/eQ6_1kHTBAAJ
            //https://groups.google.com/forum/#!searchin/gst-suvidha-provider-gsp-discussion-group/authorized|sort:relevance/gst-suvidha-provider-gsp-discussion-group/acd-F7XPYz4/7z83KM4IBgAJ

            var Base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(client2.dicParams["ResponsePayload"]));
            var Base64Hash = Convert.ToBase64String(EncryptionUtils.sha256_hash(Base64Payload));

            AuthMetaDetails MetaDetails = new AuthMetaDetails();
            MetaDetails.fdc = "NA";
            MetaDetails.udc = "NA";//Unique device code. 
            MetaDetails.pip = "NA";
            MetaDetails.lot = "P";
            MetaDetails.lov = "560103";
            MetaDetails.idc = "NA";

            var json4 = eSignObj.SignText(aadhaarnum, Otp, transactionId, Base64Hash, MetaDetails);
            var result4 = client2.File(model2, json4.SignedText, "Esign", aadhaarnum);

        }
        private static void FileGSTR1WithDSC(string gstin, string userid, string fp, string pan)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            GSTR1ApiClient client2 = new GSTR1ApiClient(client, gstin, fp);
            var model2 = client2.GetSummary().Data;

            var base64PayLoad = Convert.ToBase64String(Encoding.UTF8.GetBytes(client2.dicParams["ResponsePayload"]));
            var PayLoadHash = EncryptionUtils.sha256_hash(base64PayLoad);

            var cert = DSCUtils.getCertificate();

            var json4 = Convert.ToBase64String(DSCUtils.SignCms(PayLoadHash, cert));
            var result4 = client2.File(model2, json4, "DSC", pan);

        }
        private static void Search(string gstin)
        {
            GSTNPublicAuthClient client = new GSTNPublicAuthClient();
            var result2 = client.RequestToken();
            GSTNPublicApiClient client2 = new GSTNPublicApiClient(client);
            var output = client2.Search(gstin);
        }
        private static void RegisterDSC(string gstin, string userid, string pan)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            GSTNDSClient client2 = new GSTNDSClient(client, gstin);

            var cert = DSCUtils.getCertificate();
            byte[] data = Encoding.UTF8.GetBytes(pan);
            var sign =Convert.ToBase64String(DSCUtils.SignCms(data, cert));
            var result = client2.RegisterDSC(pan, sign);
        }

        private static GSTNAuthClient GetAuth(string gstin, string userid)
        {

            GSTNAuthClient client = new GSTNAuthClient(gstin, userid);
            var result = client.RequestOTP();

            System.Console.Write("Enter OTP:");
            string otp = System.Console.ReadLine();

            var result2 = client.RequestToken(otp);
            return client;
        }

        private static void TestGSTR1Get(string gstin, string userid, string fp)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);

            GSTR1.GSTR1Total model = new GSTR1.GSTR1Total();
            GSTR1ApiClient client2 = new GSTR1ApiClient(client, gstin, fp);
            model.b2b = client2.GetB2B("","").Data;

        }
        private static void TestGSTR2Get(string gstin, string userid, string fp)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            GSTR2.GSTR2Total model = new GSTR2.GSTR2Total();
            GSTR2ApiClient client2 = new GSTR2ApiClient(client, gstin, fp);
            System.Console.Write("Action Required? Y/N/Enter");
            string action = System.Console.ReadLine();
            model.b2b = client2.GetB2B(action,"").Data;
            var model2 = client2.GetSummary().Data;
        }
        private static void TestGSTR3(string gstin, string userid, string fp)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            GSTR3.GSTR3Total model = new GSTR3.GSTR3Total();
            GSTR3ApiClient client2 = new GSTR3ApiClient(client, gstin, fp);
            var info = client2.Generate(fp).Data;
            model = client2.GetDetails(fp).Data;
        }
        private static void TestLedger(string gstin, string userid, string fr_dt, string to_dt)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            LedgerApiClient client2 = new LedgerApiClient(client, gstin);
            var info = client2.GetCashDtl(gstin, fr_dt, to_dt).Data;
        }
        private static string TestCSV(string gstin, string userid, string fp)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            GSTR1.GSTR1Total model = new GSTR1.GSTR1Total();
            GSTR1ApiClient client2 = new GSTR1ApiClient(client, gstin, fp);
            model.b2b = client2.GetB2B("Y","").Data;

            var client3 = new MxApiClient("http://www.maximprise.com/api/gst");
            string str1 = client3.Json2CSV(client2.dicParams["ResponsePayload"], "gstr1", "b2b").Data;
            return str1;
        }
        private static void TestGSTR1Save(string gstin, string userid, string fp, string ctin, string etin )
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            var filename = "sampledata\\b2bout.json";
            if (String.IsNullOrEmpty(ctin)) {
                System.Console.Write("Enter CTIN:");
                ctin = System.Console.ReadLine();
            }
            if (String.IsNullOrEmpty(etin))
            {
                System.Console.Write("Enter ETIN:");
                etin = System.Console.ReadLine();
            }

            var str1 = File.ReadAllText(filename).Replace("%ctin%", ctin).Replace("%etin%",etin);
            GSTR1.GSTR1Total model = JsonConvert.DeserializeObject<GSTR1.GSTR1Total>(str1);
            model.gstin = gstin;
            model.fp = fp;
            GSTR1ApiClient client2 = new GSTR1ApiClient(client, gstin, fp);
            var info = client2.Save(model);
            GetStatus(client2, info.Data, fp);

          
        }
        private static void TestGSTR2Save(string gstin, string userid, string fp, string ctin)
        {
            GSTNAuthClient client = GetAuth(gstin, userid);
            var filename = "sampledata\\b2bin.json";
            if (String.IsNullOrEmpty(ctin))
            {
                System.Console.Write("Enter CTIN:");
                ctin = System.Console.ReadLine();
            }

            var str1 = File.ReadAllText(filename).Replace("%ctin%", ctin);
            GSTR2.GSTR2Total model = JsonConvert.DeserializeObject<GSTR2.GSTR2Total>(str1);
            model.gstin = gstin;
            model.fp = fp;
            GSTR2ApiClient client2 = new GSTR2ApiClient(client, gstin, fp);
            var info = client2.Save(model);
            GetStatus(client2, info.Data, fp);

        }

        private static void GetStatus(GSTNReturnsClient client2, SaveInfo info, string fp)
        {
            System.Console.WriteLine("Reference_ID: "+info.reference_id);
            var status = client2.GetStatus(fp, info.reference_id);
            System.Console.WriteLine(JsonConvert.SerializeObject(status.Data));

        }


    }
}
