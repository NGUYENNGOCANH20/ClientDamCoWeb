using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;

namespace client_damco
{
    internal class Program
    {
        static int iloldv = 6229;
        static void Main(string[] args)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClientHandler hd = new HttpClientHandler();
            CookieContainer ck = new CookieContainer();
            hd.CookieContainer = ck;
            HttpClient clint = new HttpClient(hd);
            clint.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            clint.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.0.0 Safari/537.36");
            var sortlist = _getobj_data(clint);
            string title = File.ReadAllText(Directory.GetCurrentDirectory()+"\\table.html");
            string o1v = title.Split('*')[0];
            foreach (var kvl in sortlist)
            {
                string Sonumber = kvl.Key.Split('^')[0];
                Console.WriteLine($"Checking INV number :{Sonumber}");
                string mklstring = "<tr>";
                string Gacdate = "";
                string Bkconf = "";
                for (int i = 0; i < kvl.Value.Count; i++)
                {
                    if (kvl.Value[i].Key.Contains("so_number"))
                    {
                        mklstring = mklstring + "<td>" + kvl.Value[i].Value.Split('^')[0] + "</td>";
                    }
                    else
                    {
                        if (kvl.Value[i].Key.Contains("po_number")|| kvl.Value[i].Key.Contains("skuItem"))
                        {
                            mklstring = mklstring + "<td>" + kvl.Value[i].Value.Split('*')[kvl.Value[i].Value.Split('*').GetLength(0)-1] + "</td>";
                        }
                        else
                        {
                            if (kvl.Value[i].Key.Contains("estimated_delivery"))
                            {
                                Gacdate = ($"{kvl.Value[i].Value.Split('-')[0]}-{int.Parse(kvl.Value[i].Value.Split('-')[1])}");
                            }
                            else
                            {
                                if (kvl.Value[i].Key.Contains("so_confirmation"))
                                {
                                    Bkconf = kvl.Value[i].Value;
                                }
                                
                            }
                            mklstring = mklstring + "<td>" + kvl.Value[i].Value + "</td>";
                        }
                    }

                }
                if (Gacdate == ($"{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}"))
                {
                    HttpRequestMessage mgdownload = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://booking.damco.com/ShipperPortalWeb/GenerateSoReport.action?soId={Sonumber}"));
                    var rejk = clint.SendAsync(mgdownload).Result;
                    rejk.EnsureSuccessStatusCode();
                    File.WriteAllBytes($"{Directory.GetCurrentDirectory()}\\Booking# {Bkconf} _ Tracking# {Sonumber}.pdf", rejk.Content.ReadAsByteArrayAsync().Result);
                    string detail = _getInformationdetail(Sonumber, clint, iloldv);
                    foreach (string item in detail.Split('\n'))
                    {
                        if (item.Contains("Invoice") && int.Parse(item.Split(':')[1]) == 0)
                        {
                            mklstring = mklstring + "<td>" + detail + "</td>";
                            mklstring = mklstring + "<td>Missing INV number</td>";
                            Console.WriteLine($"Missing INV number");
                        }
                    }
                    mklstring = mklstring + "</tr>";
                    o1v = o1v + mklstring;
                }
            }
            o1v = o1v + title.Split('*')[1];
            File.WriteAllText(Directory.GetCurrentDirectory()+"\\Ouput.html", o1v);
            Console.ReadKey();
        }
        public static SortedList<string, List<KeyValuePair<string, string>>> _getobj_data(HttpClient client)
        {
            Login(client);
            HttpRequestMessage mgs = new HttpRequestMessage();
            mgs.Method = HttpMethod.Get;
            mgs.RequestUri = new Uri("https://booking.damco.com/ShipperPortalWeb/searchSO.action?csrfPreventionSalt=LBKfkIk7NjiYWBsro6Ub&searchSO_Status=All&searchSO_Export_License=&searchSO_PO_NO=&searchSO_SKU=&searchSO_SO_NO=&searchSO_Shipper=All&searchSO_FOB_Point=&searchSO_Categories=&searchSO_SO_Reference=&searchSO_Consignee=All&searchSO_Place_of_delivery=&searchSO_Carrier=&searchSO_SO_Confirmation_NO=&searchSO_Manufacturer=&searchSO_FCR_Placed=All&searchSO_Vessel=&searchSO_Submission_Date_From=&searchSO_Submission_Date_To=&searchSO_HBL_HSWB_Placed=All&searchSO_Voyage=&searchSO_Earliest_Ship_Date=&searchSO_Latest_Ship_Date=&searchSO_CLR_Placed=All&searchSO_Cargo_Type=BOTH&searchSO_viewfactoryrecords=false&searchSO_Include_Historic_Data=false&searchSO_Submission_Date_Options=Between&searchSO_Earliest_ship_date_Options=Between&searchSO_Earliest_ship_date_To=&searchSO_Latest_ship_date_Options=Between&searchSO_Latest_ship_date_To=&searchSO_refLabel=All&searchSO_refText=&searchSO_Transportation_Mode=All&isAllDocumentSelected=false&recordToShowSO=10");
            var req = client.SendAsync(mgs).Result;
            req.EnsureSuccessStatusCode();
            string mlc = req.Content.ReadAsStringAsync().Result;
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < mlc.Split('\"').GetLength(0) - 2; i++)
            {
                if (mlc.Split('\"')[i].Contains("SAMLResponse"))
                {
                    list.Add(new KeyValuePair<string, string>("SAMLResponse", req.Content.ReadAsStringAsync().Result.Split('\"')[i + 2]));
                }
                if (mlc.Split('\"')[i].Contains("RelayState"))
                {
                    list.Add(new KeyValuePair<string, string>("RelayState", req.Content.ReadAsStringAsync().Result.Split('\"')[i + 2]));
                }
            }
            mgs = new HttpRequestMessage();
            mgs.Method = HttpMethod.Post;
            mgs.RequestUri = new Uri("https://booking.damco.com/ShipperPortalWeb/ADFSSSOEntry");
            mgs.Content = new FormUrlEncodedContent(list);
            req = client.SendAsync(mgs).Result;
            req.EnsureSuccessStatusCode();
            mgs = new HttpRequestMessage();
            mgs.Method = HttpMethod.Get;
            mgs.RequestUri = new Uri("https://booking.damco.com/ShipperPortalWeb/searchSO.action?csrfPreventionSalt=awuM4RoC5GCCu0RfeH8q&searchSO_Status=All&searchSO_Export_License=&searchSO_PO_NO=&searchSO_SKU=&searchSO_SO_NO=&searchSO_Shipper=All&searchSO_FOB_Point=&searchSO_Categories=&searchSO_SO_Reference=&searchSO_Consignee=All&searchSO_Place_of_delivery=&searchSO_Carrier=&searchSO_SO_Confirmation_NO=&searchSO_Manufacturer=&searchSO_FCR_Placed=All&searchSO_Vessel=&searchSO_Submission_Date_From=&searchSO_Submission_Date_To=&searchSO_HBL_HSWB_Placed=All&searchSO_Voyage=&searchSO_Earliest_Ship_Date=&searchSO_Latest_Ship_Date=&searchSO_CLR_Placed=All&searchSO_Cargo_Type=BOTH&searchSO_viewfactoryrecords=false&searchSO_Include_Historic_Data=false&searchSO_Submission_Date_Options=Between&searchSO_Earliest_ship_date_Options=Between&searchSO_Earliest_ship_date_To=&searchSO_Latest_ship_date_Options=Between&searchSO_Latest_ship_date_To=&searchSO_refLabel=All&searchSO_refText=&searchSO_Transportation_Mode=All&isAllDocumentSelected=false&recordToShowSO=10");
            req = client.SendAsync(mgs).Result;
            string valuedata = req.Content.ReadAsStringAsync().Result;
            SortedList<string, List<KeyValuePair<string, string>>> sortlist = new SortedList<string, List<KeyValuePair<string, string>>>();
            string keyadding = "";
            for (int i = 2; i < valuedata.Split('{').GetLength(0); i++)
            {
                if (!valuedata.Split('{')[i].Contains("CANCELLED")&& valuedata.Split('{')[i].Contains("NIKE"))
                {
                    int ml = 0;
                    keyadding = valuedata.Split('{')[i].Split('\'')[1];
                    Console.WriteLine("Loading Tracking number# " + keyadding.Split('^')[0]);
                    List<KeyValuePair<string, string>> mvalue = new List<KeyValuePair<string, string>>();
                    while (2 * ml < valuedata.Split('{')[i].Split('\'').GetLength(0))
                    {
                        if (!valuedata.Split('{')[i].Split('\'')[2 * ml].Contains("shipper_id"))
                        {
                            if (2 * ml == 0)
                            {
                                mvalue.Add(new KeyValuePair<string, string>(valuedata.Split('{')[i].Split('\'')[2 * ml].Split(':')[0], valuedata.Split('{')[i].Split('\'')[2 * ml + 1]));
                            }
                            else
                            {
                                mvalue.Add(new KeyValuePair<string, string>(valuedata.Split('{')[i].Split('\'')[2 * ml].Split(':')[0].Split(',')[1], valuedata.Split('{')[i].Split('\'')[2 * ml + 1]));
                            }
                            ml++;
                        }
                        else
                        {
                            sortlist.Add(keyadding, mvalue);
                            break;
                        }
                    }
                }

            }
            Console.Clear();
            return sortlist;
        }
        public static string _getInformationdetail(string Sonumber,HttpClient client,int ilold)
        {
            HttpRequestMessage mgs = new HttpRequestMessage();
            mgs.RequestUri = new Uri($"https://booking.damco.com/ShipperPortalWeb/ViewSOAction.action?so_number={Sonumber}&searchByShipper_id=");
            var req = client.SendAsync(mgs).Result;
            req.EnsureSuccessStatusCode();
            string valuedata = req.Content.ReadAsStringAsync().Result;
            if (!valuedata.Split('>')[ilold].Contains("soDto.soLineDtoList[0].description"))
            {
                int mk = 0;
                while (mk < valuedata.Split('>').GetLength(0))
                {
                    if (valuedata.Split('>')[mk].Contains("soDto.soLineDtoList[0].description"))
                    {
                        iloldv = mk;
                        return valuedata.Split('>')[mk+1].Split('<')[0];
                    }
                    if (valuedata.Split('>')[valuedata.Split('>').GetLength(0) - 1 - mk].Contains("soDto.soLineDtoList[0].description"))
                    {
                        iloldv = valuedata.Split('>').GetLength(0) - 1 - mk;
                        return valuedata.Split('>')[valuedata.Split('>').GetLength(0) - mk].Split('<')[0];
                    }
                    if (valuedata.Split('>').GetLength(0) / 2 < mk)
                    {
                        if (valuedata.Split('>')[valuedata.Split('>').GetLength(0) / 2 + mk].Contains("soDto.soLineDtoList[0].description"))
                        {
                            iloldv = valuedata.Split('>').GetLength(0) / 2 + mk;
                            return valuedata.Split('>')[valuedata.Split('>').GetLength(0) / 2 + mk + 1].Split('<')[0];
                        }
                        if (valuedata.Split('>')[valuedata.Split('>').GetLength(0) / 2 - mk].Contains("soDto.soLineDtoList[0].description"))
                        {
                            iloldv = valuedata.Split('>').GetLength(0) / 2 - mk;
                            return valuedata.Split('>')[valuedata.Split('>').GetLength(0) / 2 - mk + 1].Split('<')[0];
                        }
                    }
                    mk++;
                }
            }
            return valuedata.Split('>')[ilold + 1].Split('<')[0]; ;
        }
        public static void Login(HttpClient client)
        {
            Console.WriteLine("Loading Cracking System");
            Console.ForegroundColor = ConsoleColor.Green;
            HttpRequestMessage mgs = new HttpRequestMessage();
            mgs.Method = HttpMethod.Get;
            mgs.RequestUri = new Uri("https://portal.damco.com/");
            var req = client.SendAsync(mgs).Result;
            req.EnsureSuccessStatusCode();
            Console.WriteLine("Get_information");
            string tokenlogin = req.Content.ReadAsStringAsync().Result;
            string ulrPost = "";
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$UsernameTextBox", "HHDELT"));
            list.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$PasswordTextBox", File.ReadAllText(Directory.GetCurrentDirectory()+"\\Pass.txt")));
            list.Add(new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$SubmitButton", "Sign in"));
            foreach (string item in tokenlogin.Split('<'))
            {
                if (item.Contains("aspnetForm"))
                {
                    foreach (string keyvalue in item.Split(' '))
                    {
                        if (keyvalue.Contains("action"))
                        {
                            ulrPost = $"https://auth.damco.com{Regex.Replace(string.Join("", keyvalue.Split('\"')).Substring(string.Join("", keyvalue.Split('\"')).Split('=')[0].Length + 1, string.Join("", keyvalue.Split('\"')).Length - string.Join("", keyvalue.Split('\"')).Split('=')[0].Length - 1), "&amp;", "&")}";
                        }
                    }
                }
                if (item.Contains("__VIEWSTATE"))
                {
                    for (int i = 0; i < item.Split('\"').GetLength(0) - 1; i++)
                    {
                        if (item.Split('\"')[i].Contains("value"))
                        {
                            Console.WriteLine($"Loading Token " + item.Split('\"')[i + 1]);
                            list.Add(new KeyValuePair<string, string>("__VIEWSTATE", item.Split('\"')[i + 1]));
                        }
                    }

                }
                if (item.Contains("__EVENTVALIDATION"))
                {
                    for (int i = 0; i < item.Split('\"').GetLength(0) - 1; i++)
                    {
                        if (item.Split('\"')[i].Contains("value"))
                        {
                            Console.WriteLine($"Loading Cookie " + item.Split('\"')[i + 1]);
                            list.Add(new KeyValuePair<string, string>("__EVENTVALIDATION", item.Split('\"')[i + 1]));
                        }
                    }
                }
                if (item.Contains("__db"))
                {
                    for (int i = 0; i < item.Split('\"').GetLength(0) - 1; i++)
                    {
                        if (item.Split('\"')[i].Contains("value"))
                        {
                            Console.WriteLine($"Loading Sesion " + item.Split('\"')[i + 1]);
                            list.Add(new KeyValuePair<string, string>("__db", item.Split('\"')[i + 1]));
                        }
                    }
                }
            }
            Console.WriteLine("UsernameTextBox:HHDELT\tPassword:*****");
            mgs = new HttpRequestMessage();
            mgs.Method = HttpMethod.Post;
            mgs.RequestUri = new Uri(ulrPost);
            mgs.Content = new FormUrlEncodedContent(list);
            req = client.SendAsync(mgs).Result;
            req.EnsureSuccessStatusCode();
            Console.WriteLine($"Login Status:[{req.StatusCode}]");
        }

        public class ConnectNetW
        {
            [Flags]
            enum ConnectionInternetState : int
            {
                INTERNET_CONNECTION_MODEM = 0x1, INTERNET_CONNECTION_LAN = 0x2, INTERNET_CONNECTION_PROXY = 0x4, INTERNET_RAS_INSTALLED = 0x10, INTERNET_CONNECTION_OFFLINE = 0x20, INTERNET_CONNECTION_CONFIGURED = 0x40
            }
            [DllImport("wininet.dll", CharSet = CharSet.Auto)]
            static extern bool InternetGetConnectedState(ref ConnectionInternetState lpdwFlags, int dwReserved);
            public static bool IsConnectedToInternet()
            {
                ConnectionInternetState Description = 0;
                bool conn = InternetGetConnectedState(ref Description, 0);
                return conn;
            }
        }
    }

}
