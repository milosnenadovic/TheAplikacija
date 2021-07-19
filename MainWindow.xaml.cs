using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace TheAplikacija
{

    public partial class MainWindow : Window
    {
        String putanja = "https://demo.moj-eracun.rs/apis/v2/";
        XDocument xmldoc = new XDocument();
        readonly XNamespace nscac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        readonly XNamespace nscbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        readonly XNamespace nsxsi = "http://www.w3.org/2001/XMLSchema-instance";
        readonly XNamespace nsxsd = "http://www.w3.org/2001/XMLSchema";
        readonly XNamespace xmlnsAD = "urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2";
        readonly XNamespace nsext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
        readonly XNamespace ns = "http://fina.hr/eracun/erp/OutgoingInvoicesData/v3.2";

        String electronicID = String.Empty;
        String norma = String.Empty;
        Dictionary<string, List<double>> obracunPoreza = new Dictionary<string, List<double>>();

        string SenderPIB = ConfigurationManager.AppSettings.Get("senderPIB");
        string RecipientPIB = ConfigurationManager.AppSettings.Get("recipientPIB");
        string SenderMail = ConfigurationManager.AppSettings.Get("mailSender");
        string RecipientMail = ConfigurationManager.AppSettings.Get("mailRecipient");
        string UsernameDemo = ConfigurationManager.AppSettings.Get("usernameDemo");
        string UsernameProd = ConfigurationManager.AppSettings.Get("usernameProd");
        string PasswordDemo = ConfigurationManager.AppSettings.Get("passwordDemo");
        string PasswordProd = ConfigurationManager.AppSettings.Get("passwordDemo");
        string SoftwareID = ConfigurationManager.AppSettings.Get("softwareIdEU");

        string posiljalacPIB = "";
        string primalacPIB = "";

        string valutaDokumenta = "";
        List<string> currencyCodes = new List<string> { "RSD", "HRK", "EUR", "USD", "CHF", "GBR" };

        public MainWindow()
        {
            InitializeComponent();
            OnemoguciIzmene();
            SetStatus(-1);


            senderPIB.Text = SenderPIB;
            recipientPIB.Text = RecipientPIB;
            senderMail.Text = SenderMail;
            recipientMail.Text = RecipientMail;
            username.Text = UsernameDemo;
            password.Password = PasswordDemo;
            softwareID.Text = SoftwareID;
        }

        private void SetStatus(int v)
        {
            switch (v)
            {
                case 0:
                    label_sendingStatus.Content = "Uspešno slanje, response preuzet";
                    label_sendingStatus.Foreground = Brushes.Green;
                    if (electronicID.Length > 0)
                    {
                        receive.Visibility = Visibility.Visible;
                        receive.IsEnabled = true;
                    }
                    break;
                case 1:
                    label_sendingStatus.Content = "Spremno za slanje";
                    label_sendingStatus.Foreground = Brushes.LightGreen;
                    break;
                case 2:
                    label_sendingStatus.Content = "Slanje u toku";
                    label_sendingStatus.Foreground = Brushes.Yellow;
                    break;
                case 3:
                    label_sendingStatus.Content = "Preuzimanje u toku";
                    label_sendingStatus.Foreground = Brushes.Yellow;
                    break;
                case 4:
                    label_sendingStatus.Content = "Greška pri pokušaju slanja";
                    label_sendingStatus.Foreground = Brushes.Red;
                    break;
                case 5:
                    label_sendingStatus.Content = "Greška pri pokušaju preuzimanja";
                    label_sendingStatus.Foreground = Brushes.Red;
                    break;
                case 6:
                    label_sendingStatus.Content = "Poslat zahtev ali nije pristigao odgovor";
                    label_sendingStatus.Foreground = Brushes.Red;
                    break;
                case 7:
                    label_sendingStatus.Content = "Slanje onemogućeno, XML nije ispravan";
                    label_sendingStatus.Foreground = Brushes.Red;
                    break;
                default:
                    label_sendingStatus.Content = "Slanje onemogućeno, fajl nije učitan";
                    label_sendingStatus.Foreground = Brushes.Black;
                    break;
            };
        }

        private void OmoguciIzmene()
        {
            senderPIB.IsEnabled = true;
            senderPJ.IsEnabled = true;
            senderMail.IsEnabled = true;
            recipientPIB.IsEnabled = true;
            recipientPJ.IsEnabled = true;
            recipientMail.IsEnabled = true;
            username.IsEnabled = true;
            password.IsEnabled = true;
            softwareID.IsEnabled = true;
            requestPutanja.IsEnabled = true;
            send.IsEnabled = true;
        }

        private void OnemoguciIzmene()
        {
            senderPIB.IsEnabled = false;
            senderPJ.IsEnabled = false;
            senderMail.IsEnabled = false;
            recipientPIB.IsEnabled = false;
            recipientPJ.IsEnabled = false;
            recipientMail.IsEnabled = false;
            username.IsEnabled = false;
            password.IsEnabled = false;
            softwareID.IsEnabled = false;
            requestPutanja.IsEnabled = false;
            send.IsEnabled = false;
        }

        private void IzmenaPutanje(object sender, RoutedEventArgs e)
        {
            username.Text = (bool)demo.IsChecked ? UsernameDemo : UsernameProd;
            password.Password = (bool)demo.IsChecked ? PasswordDemo : PasswordProd;
            putanja = (bool)demo.IsChecked ? "https://demo.moj-eracun.rs/apis/v2/" : "https://www.moj-eracun.rs/apis/v2/";
        }

        private void IzmeniPodatke(String norma)
        {
            XElement accountingSupplier = xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "AccountingSupplierParty");
            XElement accountingCustomer = xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "AccountingCustomerParty");
            XElement accountingSupplierParty = accountingSupplier.Element(nscac + "Party");
            XElement accountingCustomerParty = accountingCustomer.Element(nscac + "Party");
            XElement supplierPartyID = null;
            XElement customerPartyID = null;
            if (accountingSupplierParty.Element(nscac + "PartyIdentification") != null)
            {
                if (accountingSupplierParty.Element(nscac + "PartyIdentification").Element(nscbc + "ID") != null)
                {
                    supplierPartyID = accountingSupplierParty.Element(nscac + "PartyIdentification").Element(nscbc + "ID");
                }
            }
            if (accountingCustomerParty.Element(nscac + "PartyIdentification") != null)
            {
                if (accountingCustomerParty.Element(nscac + "PartyIdentification").Element(nscbc + "ID") != null)
                {
                    customerPartyID = accountingCustomerParty.Element(nscac + "PartyIdentification").Element(nscbc + "ID");
                }
            }
            XElement supplierCompanyID = accountingSupplierParty.Element(nscac + "PartyLegalEntity").Element(nscbc + "CompanyID");
            XElement customerCompanyID = accountingCustomerParty.Element(nscac + "PartyLegalEntity").Element(nscbc + "CompanyID");
            XElement supplierEnpointID = accountingSupplierParty.Element(nscbc + "EndpointID");
            XElement customerEnpointID = accountingCustomerParty.Element(nscbc + "EndpointID");
            if (norma == "OutgoingInvoicesData")
            {
                if (senderPIB.Text != null)
                {
                    xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "SupplierID").Value = senderPIB.Text;
                    supplierCompanyID.Value = senderPIB.Text;
                }
                if (recipientPIB != null)
                {
                    xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "BuyerID").Value = recipientPIB.Text;
                    customerCompanyID.Value = recipientPIB.Text;
                }
                if (senderMail.Text != null)
                {
                    accountingSupplier.Element(nscac + "AccountingContact").Element(nscbc + "ElectronicMail").Value = senderMail.Text;
                }
                if (recipientMail.Text != null)
                {
                    accountingCustomer.Element(nscac + "AccountingContact").Element(nscbc + "ElectronicMail").Value = recipientMail.Text;
                }
                //PJ posiljaoca - ako postoji menja ga sa textbox poljem (default je vec upisana vrednost, ako se izmeni onda ce novu ubaciti)
                if (supplierPartyID != null && senderPJ.Text.Length > 0)
                {
                    supplierPartyID.Value = senderPJ.Text;
                }
                else if (senderPJ.Text.Length > 0)
                {
                    accountingSupplierParty.Element(nscac + "PartyLegalEntity").AddAfterSelf(new XElement(nscac + "PartyIdentification", new XElement(nscbc + "ID", senderPJ.Text)));
                }
                else if (accountingSupplierParty.Element(nscac + "PartyIdentification") != null)
                {
                    accountingSupplierParty.Element(nscac + "PartyIdentification").Remove();
                }
                //PJ primaoca - ako postoji menja ga sa textbox poljem (default je vec upisana vrednost, ako se izmeni onda ce novu ubaciti)
                if (accountingCustomerParty.Element(nscac + "PartyIdentification") != null && recipientPJ.Text.Length > 0)
                {
                    customerPartyID.Value = recipientPJ.Text;
                }
                else if (recipientPJ.Text.Length > 0)
                {
                    accountingSupplierParty.Element(nscac + "PartyLegalEntity").AddAfterSelf(new XElement(nscac + "PartyIdentification", new XElement(nscbc + "ID", recipientPJ.Text)));
                }
                else if (accountingCustomerParty.Element(nscac + "PartyIdentification") != null)
                {
                    accountingCustomerParty.Element(nscac + "PartyIdentification").Remove();
                }
            }
            else if (norma == "Invoice")
            {
                String senderpartyid = supplierPartyID.Value;
                String recipientpartyid = customerPartyID.Value;
                if (senderPIB.Text != null)
                {
                    supplierCompanyID.Value = senderPIB.Text;
                    accountingSupplierParty.Element(nscac + "PartyTaxScheme").Element(nscbc + "CompanyID").Value = /*"RS" +*/ senderPIB.Text;
                    if (supplierEnpointID.Attribute("schemeID").Value == "9948")
                    {
                        supplierEnpointID.Value = senderPIB.Text.ToString();
                        if (senderpartyid.Contains("HR"))
                        {
                            supplierPartyID.Value = "9948:" + senderPIB.Text + senderpartyid.Substring(senderpartyid.IndexOf("::"));
                        }
                        else
                        {
                            supplierPartyID.Value = "9948:" + senderPIB.Text;
                        }
                    }
                }
                if (recipientPIB != null)
                {
                    customerCompanyID.Value = recipientPIB.Text;
                    accountingCustomerParty.Element(nscac + "PartyTaxScheme").Element(nscbc + "CompanyID").Value = /*"RS" + */recipientPIB.Text;
                    if (customerEnpointID.Attribute("schemeID").Value == "9948")
                    {
                        customerEnpointID.Value = recipientPIB.Text.ToString();
                        if (recipientpartyid.Contains("HR"))
                        {
                            customerPartyID.Value = "9948:" + recipientPIB.Text + recipientpartyid.Substring(recipientpartyid.IndexOf("::"));
                        }
                        else
                        {
                            customerPartyID.Value = "9948:" + recipientPIB.Text;
                        }
                    }
                }
                XElement supplierElectronicMail = accountingSupplierParty.Element(nscac + "Contact").Element(nscbc + "ElectronicMail");
                if (senderMail.Text != null)
                {
                    supplierElectronicMail.Value = senderMail.Text;
                }
                XElement customerElectronicMail = accountingCustomerParty.Element(nscac + "Contact").Element(nscbc + "ElectronicMail");
                if (recipientMail.Text != null)
                {
                    customerElectronicMail.Value = recipientMail.Text;
                }
                //PJ posiljaoca - ako postoji menja ga sa textbox poljem (default je vec upisana vrednost, ako se izmeni onda ce novu ubaciti)
                if (senderpartyid.Contains("HR") && senderPJ.Text.Length > 0)
                {
                    supplierPartyID.Value = senderpartyid.Substring(0, senderpartyid.IndexOf("HR99:") + 5) + senderPJ.Text.ToString();
                }
                else if (senderPJ.Text.Length > 0)
                {
                    supplierPartyID.Value = senderpartyid + "::HR99:" + senderPJ.Text;
                }
                else if (senderpartyid.Contains("HR"))
                {
                    supplierPartyID.Value = senderpartyid.Substring(0, senderpartyid.IndexOf("::"));
                }
                //PJ primaoca - ako postoji menja ga sa textbox poljem (default je vec upisana vrednost, ako se izmeni onda ce novu ubaciti)
                if (recipientpartyid.Contains("HR") && recipientPJ.Text.Length > 0)
                {
                    customerPartyID.Value = recipientpartyid.Substring(0, recipientpartyid.IndexOf("HR99:") + 5) + recipientPJ.Text.ToString();
                }
                else if (recipientPJ.Text.Length > 0)
                {
                    customerPartyID.Value = recipientpartyid + "::HR99:" + recipientPJ.Text;
                }
                else if (recipientpartyid.Contains("HR"))
                {
                    customerPartyID.Value = recipientpartyid.Substring(0, recipientpartyid.IndexOf("::"));
                }
            }
        }

        private void LoadXML_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            receive.Visibility = Visibility.Hidden;
            receive.IsEnabled = false;
            OnemoguciIzmene();
            SetStatus(-1);
            OpenFileDialog loadXML = new OpenFileDialog();
            loadXML.Filter = "XML Files (*.xml)|*.xml";
            loadXML.FilterIndex = 1;
            loadXML.Multiselect = false;
            label_validnost.Content = "";
            textBoxGreske.Text = "";
            canvas.Width = 910;

            bool? userClickedOK = loadXML.ShowDialog();
            if (userClickedOK == true)
            {
                //isenabled atribut textbox-ova se postavlja na true, ispis putanje do fajl u polje i load xml-a
                textBoxFileName.Text = loadXML.SafeFileName;
                try
                {
                    xmldoc = XDocument.Load(loadXML.FileName);
                    norma = xmldoc.Descendants().ToList()[0].Name.LocalName;

                    XElement accountingSupplierParty = xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "AccountingSupplierParty").Element(nscac + "Party");
                    XElement accountingCustomerParty = xmldoc.Descendants().SingleOrDefault(p => p.Name.LocalName == "AccountingCustomerParty").Element(nscac + "Party");
                    XElement supplierPartyID = null;
                    XElement customerPartyID = null;
                    if (norma == "OutgoingInvoicesData")
                    {
                        xmldoc.Save(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\OutgoingInvoicesData.xml");
                        label_norma.Content = "HRInvoice";
                        //ako postoji supplier PJ bice upisana u textbox
                        if (accountingSupplierParty.Element(nscac + "PartyIdentification") != null)
                        {
                            supplierPartyID = accountingSupplierParty.Element(nscac + "PartyIdentification");
                            senderPJ.Text = supplierPartyID.Element(nscbc + "ID").Value;
                        }
                        //ako postoji customer PJ bice upisana u textbox
                        if (accountingCustomerParty.Element(nscac + "PartyIdentification") != null)
                        {
                            customerPartyID = accountingCustomerParty.Element(nscac + "PartyIdentification");
                            recipientPJ.Text = customerPartyID.Element(nscbc + "ID").Value;
                        }
                    }
                    else if (norma == "Invoice")
                    {
                        xmldoc.Save(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\Invoice.xml");
                        label_norma.Content = "EU norma";
                        //ako postoji supplier PJ bice upisana u textbox
                        if (accountingSupplierParty.Element(nscac + "PartyIdentification") != null)
                        {
                            supplierPartyID = accountingSupplierParty.Element(nscac + "PartyIdentification");
                            if (supplierPartyID.Element(nscbc + "ID").Value.Contains("HR99"))
                            {
                                senderPJ.Text = supplierPartyID.Element(nscbc + "ID").Value.Substring(21);
                            }
                        }
                        //ako postoji customer PJ bice upisana u textbox
                        if (accountingCustomerParty.Element(nscac + "PartyIdentification") != null)
                        {
                            customerPartyID = accountingCustomerParty.Element(nscac + "PartyIdentification");
                            if (customerPartyID.Element(nscbc + "ID").Value.Contains("HR99"))
                            {
                                recipientPJ.Text = customerPartyID.Element(nscbc + "ID").Value.Substring(21);
                            }
                        }
                    }
                    else
                    {
                        label_norma.Content = "Nije račun";
                        xmldoc.Save(Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + @"\" + norma + ".xml");
                    }
                    SetStatus(1);
                    if (norma == "Invoice" || norma == "OutgoingInvoicesData") ValidateXML();
                }
                catch (System.Xml.XmlException ex)
                {
                    SetStatus(7);
                    SetGreska();
                    textBoxGreske.Text = "XML not well-formed: " + ex.Message;
                }
            }
            else
            {
                Reset();
            }
        }

        private void Reset()
        {
            textBoxFileName.Text = "";
            senderPJ.Text = "";
            recipientPJ.Text = "";
            textBoxRequest.Text = "Request";
            textBoxResponse.Text = "Response";
            textBoxResponse.HorizontalContentAlignment = HorizontalAlignment.Center;
            textBoxRequest.HorizontalContentAlignment = HorizontalAlignment.Center;
            obracunPoreza.Clear();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            OnemoguciIzmene();
            IzmeniPodatke((String)norma);
            SetStatus(2);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(putanja + "send"));
            request.KeepAlive = false;
            request.Method = "POST";
            request.ContentType = "application/json";
            String jsonXML = xmldoc.ToString();
            String jsonPostData = "{\"Username\":" + username.Text + ",\"Password\":\"" + password.Password + "\",\"CompanyId\":\"" + senderPIB.Text + "\",\"CompanyBu\":\"" + senderPJ.Text + "\",\"SoftwareId\":\"" + softwareID.Text + "\",\"File\":" + JsonConvert.ToString(jsonXML) + "}";
            textBoxRequest.Text = "{\"Username\":" + username.Text + ",\"Password\":\"";
            for (int i = 0; i < password.Password.Length; i++)
            {
                textBoxRequest.Text += "*";
            }
            textBoxRequest.TextAlignment = TextAlignment.Left;
            textBoxRequest.FontSize = 10;
            textBoxRequest.Text += "\",\"CompanyId\":\"" + senderPIB.Text + "\",\"CompanyBu\":\"" + senderPJ.Text + "\",\"SoftwareId\":\"" + softwareID.Text + "\",\"File\":" + JsonConvert.ToString(jsonXML) + "}";
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                SetStatus(4);
                OmoguciIzmene();
                streamWriter.Write(jsonPostData);
                streamWriter.Flush();
                streamWriter.Close();

                SetStatus(6);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd().ToString();
                    if (result.IndexOf("ElectronicId\":") >= 0)
                    {
                        int pos1 = result.IndexOf("ElectronicId\":") + 14;
                        int pos2 = result.IndexOf(",");
                        electronicID = result.Substring(pos1, pos2 - pos1);
                    }
                    string responsePrikaz = "";
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (result[i].ToString() == "}")
                        {
                            responsePrikaz += "\n";
                        }
                        responsePrikaz += result[i].ToString();
                        if (result[i].ToString() == "{")
                        {
                            responsePrikaz += "\n\t";
                        }
                        if (result[i].ToString() == ",")
                        {
                            responsePrikaz += "\n\t";
                        }
                    }
                    textBoxResponse.HorizontalContentAlignment = HorizontalAlignment.Left;
                    textBoxResponse.Text = responsePrikaz;
                    SetStatus(0);
                }
            }
        }

        private void Receive_Click(object sender, RoutedEventArgs e)
        {
            SetStatus(3);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(putanja + "receive"));
            request.KeepAlive = false;
            request.Method = "POST";
            request.ContentType = "application/json";
            String jsonPostData = "{\"Username\":" + username.Text + ",\"Password\":\"" + password.Password + "\",\"CompanyId\":\"" + recipientPIB.Text + "\",\"CompanyBu\":\"" + recipientPJ.Text + "\",\"SoftwareId\":\"" + softwareID.Text + "\",\"ElectronicId\":" + electronicID + "}";
            textBoxRequest.Text = "{\"Username\":" + username.Text + ",\"Password\":\"";
            for(int i=0; i< password.Password.Length; i++) {
                textBoxRequest.Text += "*";
            }
            textBoxRequest.TextAlignment = TextAlignment.Left;
            textBoxRequest.FontSize = 10;
            textBoxRequest.Text += "\",\"CompanyId\":\"" + recipientPIB.Text + "\",\"CompanyBu\":\"" + recipientPJ.Text + "\",\"SoftwareId\":\"" + softwareID.Text + "\",\"ElectronicId\":" + electronicID + "}";
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                SetStatus(5);
                streamWriter.Write(jsonPostData);
                streamWriter.Flush();
                streamWriter.Close();

                SetStatus(6);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    textBoxResponse.Text = result;
                    SetStatus(0);
                    File.WriteAllText("C:\\Users\\PC\\Desktop\\" + electronicID + ".xml", result);
                }
            }
        }

        private void ValidateXML()
        {
            XmlSchemaSet schema = new XmlSchemaSet();

            if (norma == "OutgoingInvoicesData")
            {
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-CommonAggregateComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-CommonBasicComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-CommonExtensionComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-CommonSignatureComponents-2.1.xsd")));
                schema.Add("urn:un:unece:uncefact:documentation:2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-CoreComponentParameters-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-ExtensionContentDatatype-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-QualifiedDataTypes-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-SignatureAggregateComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-SignatureBasicComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:UnqualifiedDataTypes-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-UnqualifiedDataTypes-2.1.xsd")));
                schema.Add("http://www.w3.org/2000/09/xmldsig#", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\UBL-xmldsig-core-schema-2.1.xsd")));
                schema.Add("urn:un:unece:uncefact:data:specification:CoreComponentTypeSchemaModule:2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\commonHR\CCTS_CCT_SchemaModule-2.1.xsd")));
                schema.Add("http://fina.hr/eracun/erp/OutgoingInvoicesData/v3.2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\OutgoingInvoicesData.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\HRInvoice.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\UBL-AttachedDocument-2.1.xsd")));
            }
            else if (norma == "Invoice")
            {
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-CommonAggregateComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-CommonBasicComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-CommonExtensionComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-CommonSignatureComponents-2.1.xsd")));
                schema.Add("urn:un:unece:uncefact:documentation:2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-CoreComponentParameters-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-ExtensionContentDatatype-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-QualifiedDataTypes-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-SignatureAggregateComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-SignatureBasicComponents-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:UnqualifiedDataTypes-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-UnqualifiedDataTypes-2.1.xsd")));
                schema.Add("http://www.w3.org/2000/09/xmldsig#", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\UBL-xmldsig-core-schema-2.1.xsd")));
                schema.Add("urn:un:unece:uncefact:data:specification:CoreComponentTypeSchemaModule:2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\common\CCTS_CCT_SchemaModule-2.1.xsd")));
                schema.Add("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", XmlReader.Create(new StreamReader(Directory.GetParent(Environment.CurrentDirectory).FullName + @"\UBL-Invoice-2.1.xsd")));
            }
            XDocument doc = new XDocument();
            bool ispravnost = true;
            try
            {
                doc = XDocument.Load(Directory.GetParent(Environment.CurrentDirectory).FullName + "\\" + norma + ".xml");
                //izmena schemaLocation
                if (norma == "Invoice")
                {
                    XElement invoice = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice");
                    if (invoice.Attribute(nsxsi + "schemaLocation") != null)
                    {
                        XAttribute schemeLocation = invoice.Attribute(nsxsi + "schemaLocation");
                        invoice.Attribute(nsxsi + "schemaLocation").SetValue("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-Invoice-2.1.xsd");
                    }
                    else if (invoice.Attribute("schemaLocation") != null)
                    {
                        XAttribute schemeLocation = invoice.Attribute("schemaLocation");
                        invoice.Attribute("schemaLocation").Remove();
                        invoice.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-Invoice-2.1.xsd"));
                    }
                    else
                    {
                        invoice.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-Invoice-2.1.xsd"));
                    }
                }
                if (norma == "OutgoingInvoicesData")
                {
                    XElement invoice = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice");
                    if (invoice.Attribute(nsxsi + "schemaLocation") != null)
                    {
                        XAttribute schemeLocation = invoice.Attribute(nsxsi + "schemaLocation");
                        invoice.Attribute(nsxsi + "schemaLocation").SetValue("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/HRInvoice.xsd");
                    }
                    else if (invoice.Attribute("schemaLocation") != null)
                    {
                        XAttribute schemeLocation = invoice.Attribute("schemaLocation");
                        invoice.Attribute("schemaLocation").Remove();
                        invoice.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/HRInvoice.xsd"));
                    }
                    else
                    {
                        invoice.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/HRInvoice.xsd"));
                    }
                    IEnumerable<XElement> attachedDocuments = doc.Descendants(xmlnsAD + "AttachedDocument");
                    foreach (XElement attachedDocument in attachedDocuments)
                    {
                        if (attachedDocument.Attribute(nsxsi + "schemaLocation") != null)
                        {
                            XAttribute schemaLocation = invoice.Attribute(nsxsi + "schemaLocation");
                            attachedDocument.Attribute(nsxsi + "schemaLocation").SetValue("urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-AttachedDocument-2.1.xsd");
                        }
                        else if (attachedDocument.Attribute("schemaLocation") != null)
                        {
                            XAttribute schemaLocation = attachedDocument.Attribute("schemaLocation");
                            attachedDocument.Attribute("schemaLocation").Remove();
                            attachedDocument.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-AttachedDocument-2.1.xsd"));
                        }
                        else
                        {
                            attachedDocument.Add(new XAttribute(nsxsi + "schemaLocation", "urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2 " + Directory.GetParent(Environment.CurrentDirectory).FullName + @"/UBL-AttachedDocument-2.1.xsd"));
                        }
                    }
                }
                //iznad izmena schemaLocation
                label_validnost.Content = "Ispravan ✔️";
                label_validnost.Foreground = Brushes.Green;
                doc.Validate(schema, (s, ex) =>
                {
                    ispravnost = false;
                    SetGreska();
                    textBoxGreske.Text = ex.Message;
                }, true);
            }
            catch (System.Xml.XmlException ex)
            {
                ispravnost = false;
                SetGreska();
                textBoxGreske.Text = "XSD greška: " + ex.Message;
            }
            if (ispravnost)
            {
                if (norma == "OutgoingInvoicesData")
                {
                    primalacPIB = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "OutgoingInvoice").Element(ns + "BuyerID").Value;
                    posiljalacPIB = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Header").Element(ns + "SupplierID").Value;
                }
                ispravnost = ValidacijaIznosaStavki(doc);
                if (ispravnost)
                {
                    ispravnost = ValidacijaPoreza(doc);
                    if (ispravnost)
                    {
                        ispravnost = ValidacijaUkupnihIznosa(doc);
                        if (ispravnost)
                        {
                            ispravnost = ValidacijaSpecijalnihPolja(doc);
                            if (ispravnost)
                            {
                                OmoguciIzmene();
                                SetStatus(1);
                            }
                            else
                            {
                                SetStatus(7);
                            }
                        }
                        else
                        {
                            SetStatus(7);
                        }
                    }
                    else
                    {
                        SetStatus(7);
                    }
                }
                else
                {
                    SetStatus(7);
                }
            }
            else
            {
                SetStatus(7);
            }
        }

        private bool ValidacijaSpecijalnihPolja(XDocument doc)
        {
            XElement invoice = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice");
            bool greskaPostoji = false;
            greskaPostoji = ValidacijaFiksnihPolja(invoice);
            if (!greskaPostoji)
            {
                greskaPostoji = ValidacijaTipaDokumenta(invoice);
                if (!greskaPostoji)
                {
                    greskaPostoji = ValidacijaPaymentMeans(invoice);
                    if (!greskaPostoji)
                    {
                        greskaPostoji = ValidacijaParty(invoice);
                        if (!greskaPostoji && norma == "OutgoingInvoicesData")
                        {
                            greskaPostoji = ValidacijaUBLExtensions(invoice.Element(nsext + "UBLExtensions"));
                        }
                    }
                }
            }
            return !greskaPostoji;
        }

        private bool ValidacijaPaymentMeans(XElement invoice)
        {
            bool greskaPostoji = false;
            List<XElement> paymentMeanss = invoice.Elements(nscac + "PaymentMeans").ToList();
            foreach (XElement paymentMeans in paymentMeanss)
            {
                XElement paymentMeansCode = paymentMeans.Element(nscbc + "PaymentMeansCode");
                List<string> paymentMeansCodes = new List<string> { "10", "20", "30", "42", "60" };
                if (!paymentMeansCodes.Contains(paymentMeansCode.Value))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPaymentMeansCode polje treba da ima ispravnu vrednost (primer: 42).\n";
                }
                if (norma == "OutgoingInvoicesData")
                {
                    if (paymentMeans.Element(nscbc + "PaymentChannelCode").Value != "Žiro račun")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPaymentChannelCode polje treba da ima ispravnu vrednost (primer: Žiro račun).\n";
                    }
                    if (!currencyCodes.Contains(paymentMeans.Element(nscac + "PayeeFinancialAccount").Element(nscbc + "CurrencyCode").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nValuta u kojoj se vrši isplata računa treba da bude ispravno uneta.\n";
                    }
                    else if (paymentMeans.Element(nscac + "PayeeFinancialAccount").Element(nscbc + "CurrencyCode").Value != valutaDokumenta)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nValuta u kojoj se vrši isplata računa treba da bude ista kao valuta dokumenta.\n";
                    }
                }
                if (invoice.Element(nscac + "PaymentTerms") != null)
                {
                    XElement paymentTerms = invoice.Element(nscac + "PaymentTerms");
                    if (paymentTerms.Element(nscbc + "Amount") != null)
                    {
                        if (!currencyCodes.Contains(paymentTerms.Element(nscbc + "Amount").Attribute("currencyID").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nValuta u kojoj je naveden iznos računa (PaymentTerms/Amount) treba da bude ispravna (Primer: " + valutaDokumenta + ").\n";
                        }
                    }
                }
            }
            return greskaPostoji;
        }

        private bool ValidacijaParty(XElement invoice)
        {
            bool greskaPostoji = false;
            XElement supplier = invoice.Element(nscac + "AccountingSupplierParty");
            XElement customer = invoice.Element(nscac + "AccountingCustomerParty");
            XElement customerContact = norma == "Invoice" ? customer.Element(nscac + "Party").Element(nscac + "Contact") : customer.Element(nscac + "AccountingContact");
            if (customerContact.Element(nscbc + "ElectronicMail") == null)
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nElectronicMail polje kod podataka o kupcu je obavezno.\n";
            }
            else
            {
                try
                {
                    string email = customerContact.Element(nscbc + "ElectronicMail").Value;
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nElectronicMail nije u formatu e-mail adrese.\n";
                    }
                }
                catch
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nElectronicMail nije u formatu e-mail adrese.\n";
                }
            }
            List<string> countryCodes = new List<string> { "RS", "HR", "SI", "BA", "MN" };
            if (!countryCodes.Contains(supplier.Element(nscac + "Party").Element(nscac + "PostalAddress").Element(nscac + "Country").Element(nscbc + "IdentificationCode").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nKod za državu u polju IdentificationCode kod dobavljača nije ispravan (primer: RS).\n";
            }
            if (!countryCodes.Contains(customer.Element(nscac + "Party").Element(nscac + "PostalAddress").Element(nscac + "Country").Element(nscbc + "IdentificationCode").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nKod za državu u polju IdentificationCode kod kupca nije ispravan (primer: RS).\n";
            }
            if (norma == "Invoice")
            {
                //partytaxscheme dobavljaca
                if (supplier.Element(nscac + "Party").Element(nscac + "PartyTaxScheme").Element(nscac + "TaxScheme").Element(nscbc + "ID").Value != "VAT")
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nOznaka za poresku shemu treba da bude uneta (Primer: VAT).\n";
                }
                //partytaxscheme kupca
                if (customer.Element(nscac + "Party").Element(nscac + "PartyTaxScheme").Element(nscac + "TaxScheme").Element(nscbc + "ID").Value != "VAT")
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nOznaka za poresku shemu treba da bude uneta (Primer: VAT).\n";
                }
            }
            greskaPostoji = ValidacijaPIB(supplier.Element(nscac + "Party"), customer.Element(nscac + "Party"));
            return greskaPostoji;
        }

        private bool ValidacijaPIB(XElement supplier, XElement customer)
        {
            bool greskaPostoji = false;
            string supplierPIB = supplier.Element(nscac + "PartyLegalEntity").Element(nscbc + "CompanyID").Value;
            string customerPIB = customer.Element(nscac + "PartyLegalEntity").Element(nscbc + "CompanyID").Value;
            if (norma == "Invoice")
            {
                string supplierSchemeID = supplier.Element(nscbc + "EndpointID").Attribute("schemeID").Value;
                string supplierPartyID = supplier.Element(nscac + "PartyIdentification").Element(nscbc + "ID").Value;
                string supplierEndpointID = supplier.Element(nscbc + "EndpointID").Value;
                string customerSchemeID = customer.Element(nscbc + "EndpointID").Attribute("schemeID").Value;
                string customerPartyID = customer.Element(nscac + "PartyIdentification").Element(nscbc + "ID").Value;
                string customerEndpointID = customer.Element(nscbc + "EndpointID").Value;
                if (supplierSchemeID == "9948")
                {
                    if (supplierEndpointID != supplierPIB)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'EndpointID' kod dobavljača.\n";
                    }
                    if (supplierPartyID.Substring(0, 5) != "9948:")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU polju 'PartyIdentification/ID' potrebno je uneti ispravan prefiks (Primer: '9948:') kod dobavljača.\n";
                    }
                    else if (supplierPartyID.Length == supplierPIB.Length + 5)
                    {
                        if (supplierPartyID.Substring(5) != supplierPIB)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod dobavljača.\n";
                        }
                    }
                    else if (supplierPartyID.Length < supplierPIB.Length + 5)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod dobavljača.\n";
                    }
                    else if (supplierPartyID.Substring(5).IndexOf("::HR99:") > 0)
                    {
                        if (supplierPartyID.IndexOf("::HR99:") + 7 >= supplierPartyID.Length)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPoslovna jedinica mora biti uneta ispravno (Primer: ::HR99:Poslovnica) kod dobavljača.\n";
                        }
                        else if (supplierPartyID.Substring(5, supplierPartyID.Substring(5).IndexOf("::HR99:")) != supplierPIB)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod dobavljača.\n";
                        }
                    }
                    else if (supplierPartyID.Substring(5, supplierPartyID.Length - 5) != supplierPIB)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod dobavljača.\n";
                    }
                }
                else if (supplierSchemeID == "0088")
                {
                    if (supplierEndpointID != supplierPartyID)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nGLN mora biti isti u poljima 'EndpointID' i 'PartyIdentification/ID' kod dobavljača.\n";
                    }
                }
                else
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nAtribut 'schemeID' u 'EndpointID' polju dobavljača nije ispravan (Primer: '9948').\n";
                }

                if (customerSchemeID == "9948")
                {
                    if (customerEndpointID != customerPIB)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'EndpointID' kod kupca.\n";
                    }
                    if (customerPartyID.Substring(0, 5) != "9948:")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU polju 'PartyIdentification/ID' potrebno je uneti ispravan prefiks (Primer: '9948:') kod kupca.\n";
                    }
                    else if (customerPartyID.Length == customerPIB.Length + 5)
                    {
                        if (customerPartyID.Substring(5) != customerPIB)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod kupca.\n";
                        }
                    }
                    else if (customerPartyID.Length < customerPIB.Length + 5)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod kupca.\n";
                    }
                    else if (customerPartyID.Substring(5).IndexOf("::HR99:") > 0)
                    {
                        if (customerPartyID.IndexOf("::HR99:") + 7 >= customerPartyID.Length)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPoslovna jedinica mora biti uneta ispravno (Primer: ::HR99:Poslovnica) kod kupca.\n";
                        }
                        else if (customerPartyID.Substring(5, customerPartyID.Substring(5).IndexOf("::HR99:")) != customerPIB)
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod kupca.\n";
                        }
                    }
                    else if (customerPartyID.Substring(5, customerPartyID.Length - 5) != customerPIB)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPIB iz polja 'PartyLegalEntity/CompanyID' se ne poklapa sa PIB-om iz polja 'PartyIdentification/ID' kod kupca.\n";
                    }
                }
                else if (customerSchemeID == "0088")
                {
                    if (customerEndpointID != customerPartyID)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nGLN mora biti isti u poljima 'EndpointID' i 'PartyIdentification/ID' kod dobavljača.\n";
                    }
                }
                else
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nAtribut 'schemeID' u 'EndpointID' polju kupca nije ispravan (Primer: '9948').\n";
                }
            }
            else if (norma == "OutgoingInvoicesData")
            {
                if (supplierPIB != posiljalacPIB)
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPIB iz polja 'SupplierID' se ne poklapa sa PIB-om iz polja 'PartyLegalEntity/CompanyID'.\n";
                }
                if (customerPIB != primalacPIB)
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPIB iz polja 'BuyerID' se ne poklapa sa PIB-om iz polja 'PartyLegalEntity/CompanyID'.\n";
                }
            }
            return greskaPostoji;
        }

        private bool ValidacijaTipaDokumenta(XElement invoice)
        {
            bool greskaPostoji = false;
            string[] invoiceTypeCodes = { /*"0", "4", "7", "82", "105", "226", "230", "231", "325", "326", "351",*/ "380"/*, "381", "383", "384", "386", "394", "632"*/ };
            XElement invoiceTypeCode = invoice.Element(nscbc + "InvoiceTypeCode");

            if (!invoiceTypeCodes.Contains(invoiceTypeCode.Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nInvoiceTypeCode polje treba da ima vrednost '380' u slučaju računa.\n";
            }

            string[] documentCurrencyCodes = { "RSD", "EUR", "HRK", "USD", "GBR", "USD" };
            XElement documentCurrencyCode = invoice.Element(nscbc + "DocumentCurrencyCode");

            if (!documentCurrencyCodes.Contains(documentCurrencyCode.Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nDocumentCurrencyCode polje treba da ima vrednost 'RSD' u slučaju računa.\n";
            }
            else
            {
                valutaDokumenta = documentCurrencyCode.Value;
            }

            return greskaPostoji;
        }

        private bool ValidacijaFiksnihPolja(XElement invoice)
        {
            bool greskaPostoji = false;
            if (invoice.Element(nscbc + "ProfileID").Value != "MojEracunInvoice")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje ProfileID treba da ima vrednost 'MojEracunInvoice'.\n";
            }
            if (norma == "OutgoingInvoicesData")
            {
                if (invoice.Element(nscbc + "UBLVersionID").Value != "2.1")
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPolje UBLVersionID treba da ima vrednost '2.1'.\n";
                }
                if (invoice.Element(nscbc + "CustomizationID").Value != "urn:invoice.hr:ubl-2.1-customizations:FinaInvoice")
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPolje CustomizationID treba da ima vrednost 'urn:invoice.hr:ubl-2.1-customizations:FinaInvoice'.\n";
                }
            }
            else if (norma == "Invoice")
            {
                if (invoice.Element(nscbc + "CustomizationID").Value != "urn:cen.eu:en16931:2017")
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPolje CustomizationID treba da ima vrednost 'urn:cen.eu:en16931:2017'.\n";
                }
            }
            return greskaPostoji;
        }

        private bool ValidacijaUBLExtensions(XElement UBLExtensions)
        {
            bool greskaPostoji = false;
            List<XElement> extensions = UBLExtensions.Elements(nsext + "UBLExtension").ToList();

            foreach (XElement extension in extensions)
            {
                if (extension.Element(nsext + "ExtensionContent") == null)
                {
                    if (extension.Element(nscbc + "ID").Value != "HRINVOICE1")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ID treba da ima vrednost 'HRINVOICE1'.\n";
                    }
                }
            }

            //validacija prve ekstenzije
            if (extensions[0].Element(nscbc + "Name").Value != "InvoiceIssuePlaceData")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/Name u prvoj elstenziji treba da ima vrednost 'InvoiceIssuePlaceData'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionAgencyID").Value != "Ministarstvo trgovine, turizma i telekomunikacije")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyID u prvoj elstenziji treba da ima vrednost 'Ministarstvo trgovine, turizma i telekomunikacije'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionAgencyName").Value != "Ministarstvo trgovine, turizma i telekomunikacije")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyName u prvoj elstenziji treba da ima vrednost 'Ministarstvo trgovine, turizma i telekomunikacije'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionAgencyURI").Value != "Ministarstvo trgovine, turizma i telekomunikacije")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyURI u prvoj elstenziji treba da ima vrednost 'Ministarstvo trgovine, turizma i telekomunikacije'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionURI").Value != "urn:invoice:hr:issueplace")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionURI u prvoj elstenziji treba da ima vrednost 'urn:invoice:hr:issueplace'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionReasonCode").Value != "MandatoryField")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionReasonCode u prvoj elstenziji treba da ima vrednost 'MandatoryField'.\n";
            }
            if (extensions[0].Element(nsext + "ExtensionReason").Value != "Mesto izdavanja računa prema Pravilniku o PDV-u")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionReason u prvoj ekstenziji treba da ima vrednost 'Mesto izdavanja računa prema Pravilniku o PDV-u'.\n";
            }

            //validacija druge ekstenzije
            if (extensions[1].Element(nscbc + "Name").Value != "InvoiceIssuerData")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/Name u drugoj elstenziji treba da ima vrednost 'InvoiceIssuerData'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionAgencyID").Value != "Uprava za trezor")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyID u drugoj elstenziji treba da ima vrednost 'Uprava za trezor'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionAgencyName").Value != "Uprava za trezor")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyName u drugoj elstenziji treba da ima vrednost 'Uprava za trezor'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionAgencyURI").Value != "Uprava za trezor")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionAgencyURI u drugoj elstenziji treba da ima vrednost 'Uprava za trezor'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionURI").Value != "urn:invoice:hr:issuer")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionURI u drugoj elstenziji treba da ima vrednost 'urn:invoice:hr:issuer'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionReasonCode").Value != "MandatoryField")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionReasonCode u drugoj elstenziji treba da ima vrednost 'MandatoryField'.\n";
            }
            if (extensions[1].Element(nsext + "ExtensionReason").Value != "Podaci o izdavatelju prema Zakonu o trgovačkim društvima" && extensions[1].Element(nsext + "ExtensionReason").Value != "Podaci o izdavaocu prema Zakonu o privrednim društvima")
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPolje UBLExtensions/UBLExtension/ExtensionReason u drugoj elstenziji treba da ima vrednost 'Podaci o izdavatelju prema Zakonu o trgovačkim društvima'.\n";
            }

            return greskaPostoji;
        }

        private bool ValidacijaUkupnihIznosa(XDocument doc)
        {
            bool greskaPostoji = false;
            double trosarina = 0;
            double popust = 0;
            double avans = 0;
            double iznosBezPoreza = 0;
            XElement invoice = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice");
            XElement legalMonetaryTotal = invoice.Element(nscac + "LegalMonetaryTotal");
            double allowanceTotalAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "AllowanceTotalAmount").Value);
            double chargeTotalAmount = 0;
            double prepaidAmount = 0;
            if (legalMonetaryTotal.Element(nscbc + "ChargeTotalAmount") != null)
            {
                chargeTotalAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "ChargeTotalAmount").Value);
                if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "ChargeTotalAmount").Attribute("currencyID").Value))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'ChargeTotalAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
                }
            }
            if (legalMonetaryTotal.Element(nscbc + "PrepaidAmount") != null)
            {
                prepaidAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "PrepaidAmount").Value);
                if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "PrepaidAmount").Attribute("currencyID").Value))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'PrepaidAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
                }
            }
            List<XElement> allowanceCharges = invoice.Elements(nscac + "AllowanceCharge").ToList();
            List<XElement> prepaidPayments = invoice.Elements(nscac + "PrepaidPayment").ToList();

            if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "LineExtensionAmount").Attribute("currencyID").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'LineExtensionAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
            }
            if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "TaxExclusiveAmount").Attribute("currencyID").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'TaxExclusiveAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
            }
            if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "TaxInclusiveAmount").Attribute("currencyID").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'TaxInclusiveAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
            }
            if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "AllowanceTotalAmount").Attribute("currencyID").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'AllowanceTotalAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
            }
            if (!currencyCodes.Contains(legalMonetaryTotal.Element(nscbc + "PayableAmount").Attribute("currencyID").Value))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'PayableAmount' ima upisanu neispravnu vrednost atributa 'currencyID' (Primer: RSD).\n";
            }
            //obracun allowance charge
            if (allowanceCharges.Count > 0)
            {
                List<object> validacijaAllowanceCharge = ValidacijaAllowanceCharge(allowanceCharges);
                greskaPostoji = (bool)validacijaAllowanceCharge[0];
                if (!greskaPostoji)
                {
                    popust = (double)validacijaAllowanceCharge[1];
                    trosarina = (double)validacijaAllowanceCharge[2];
                }
            }
            if (allowanceTotalAmount != popust)
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, 'AllowanceTotalAmount' se ne poklapa sa obračunom AllowanceCharge čvorova.\n";
            }
            if (chargeTotalAmount != trosarina)
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, ChargeTotalAmount se ne poklapa sa obračunom AllowanceCharge čvorova.\n";
            }

            //obracun prepaid payment
            if (prepaidPayments.Count > 0)
            {
                avans = 0;
                foreach (XElement prepaidPayment in prepaidPayments)
                {
                    if (!currencyCodes.Contains(prepaidPayment.Element(nscbc + "PaidAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nValuta u kojoj je naveden iznos avansne uplate (PrepaidPayment/PaidAmount) treba da bude ispravna (Primer: " + valutaDokumenta + ").\n";
                    }
                    avans += Convert.ToDouble(prepaidPayment.Element(nscbc + "PaidAmount").Value);
                }
            }
            if (avans != prepaidAmount)
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nU rekapitulaciji iznosa, PrepaidAmount se ne poklapa sa obračunom PrepaidPayment čvorova.\n";
            }

            if (!greskaPostoji)
            {
                double lineExtensionAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "LineExtensionAmount").Value);
                double taxExclusiveAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "TaxExclusiveAmount").Value);
                double taxInclusiveAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "TaxInclusiveAmount").Value);
                double taxAmount = Convert.ToDouble(doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice").Element(nscac + "TaxTotal").Element(nscbc + "TaxAmount").Value);
                double payableAmount = Convert.ToDouble(legalMonetaryTotal.Element(nscbc + "PayableAmount").Value);
                iznosBezPoreza = lineExtensionAmount + trosarina - popust;
                if (taxExclusiveAmount != iznosBezPoreza)
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji iznosa, TaxExclusiveAmount se ne poklapa sa obračunom LineExtensionAmount i popusta i trošarine na nivou računa.\n";
                }
                else if (taxInclusiveAmount != Math.Round(taxExclusiveAmount + taxAmount, 2))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji iznosa, TaxInclusiveAmount se ne poklapa sa obračunom TaxExclusiveAmount i poreza.\n";
                }
                else if (payableAmount != taxInclusiveAmount - avans)
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji iznosa, PayableAmount se ne poklapa sa obračunom TaxInclusiveAmount i uplaćenog avansa.\n";
                }

            }

            return !greskaPostoji;
        }

        private List<object> ValidacijaAllowanceCharge(List<XElement> allowanceCharges)
        {
            List<object> allowance = new List<object>();
            bool greskaPostoji = false;
            double popust = 0;
            double trosarina = 0;
            foreach (XElement allowanceCharge in allowanceCharges)
            {
                string chargeIndicator = allowanceCharge.Element(nscbc + "ChargeIndicator").Value;
                double iznos = Convert.ToDouble(allowanceCharge.Element(nscbc + "Amount").Value);
                if (allowanceCharge.Element(nscbc + "BaseAmount") != null)
                {
                    double osnovica = Convert.ToDouble(allowanceCharge.Element(nscbc + "BaseAmount").Value);
                    //provera obracuna iznosa prema osnovici i procentu
                    if (allowanceCharge.Element(nscbc + "MultiplierFactorNumeric") != null)
                    {
                        XElement multiplierFactorNumeric = allowanceCharge.Element(nscbc + "MultiplierFactorNumeric");
                        double procenat = Convert.ToDouble(multiplierFactorNumeric.Value);
                        if (procenat > 1)
                        {
                            if (iznos != osnovica / 100 * procenat)
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nU AllowanceCharge čvoru, obračun iznosa na osnovu osnovice i procenat nije u redu.\n";
                            }
                        }
                        else
                        {
                            if (iznos != osnovica * procenat)
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nU AllowanceCharge čvoru, obračun iznosa na osnovu osnovice i procenat nije u redu.\n";
                            }
                        }
                    }
                }
                if (chargeIndicator == "false")
                {
                    popust += iznos;
                }
                else if (chargeIndicator == "true")
                {
                    trosarina += iznos;
                }
                else
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text = "\nChargeIndicator polje u čvoru AllowanceCharge mora imati vrednost 'false' ili 'true'.\n";
                }

            }
            allowance.Add(greskaPostoji);
            allowance.Add(popust);
            allowance.Add(trosarina);
            return allowance;
        }

        private bool ValidacijaPoreza(XDocument doc)
        {
            bool greskaPostoji = false;
            XElement taxTotal = doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice").Element(nscac + "TaxTotal");
            double ukupanPorez = Convert.ToDouble(taxTotal.Element(nscbc + "TaxAmount").Value);
            Dictionary<string, double> rekapitulacijaPoreza = new Dictionary<string, double>();
            List<XElement> taxSubtotals = taxTotal.Elements(nscac + "TaxSubtotal").ToList();
            if (!currencyCodes.Contains((taxTotal.Element(nscbc + "TaxAmount").Attribute("currencyID").Value)))
            {
                greskaPostoji = true;
                SetGreska();
                textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu vrednost atributa 'currencyID' (primer: " + valutaDokumenta + ")!\n";
            }
            foreach (XElement taxSubtotal in taxSubtotals)
            {
                string poreskaKategorija = taxSubtotal.Element(nscac + "TaxCategory").Element(nscbc + "ID").Value;
                double procenat = Convert.ToDouble(taxSubtotal.Element(nscac + "TaxCategory").Element(nscbc + "Percent").Value);
                double iznosPoreza = Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").Value);
                double osnovica = Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxableAmount").Value);
                if (!currencyCodes.Contains((taxSubtotal.Element(nscbc + "TaxAmount").Attribute("currencyID").Value)))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nPorez u rekapitulaciji unutar 'TaxSubtotal' za poresku kategoriju " + poreskaKategorija + " nema ispravnu vrednost atributa 'currencyID' (primer: " + valutaDokumenta + ")!\n";
                }
                if (!currencyCodes.Contains((taxSubtotal.Element(nscbc + "TaxableAmount").Attribute("currencyID").Value)))
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nOsnovica za obračun poreza, u rekapitulaciji unutar 'TaxSubtotal' za poresku kategoriju " + poreskaKategorija + " nema ispravnu vrednost atributa 'currencyID' (primer: " + valutaDokumenta + ")!\n";
                }
                XElement taxCategory = taxSubtotal.Element(nscac + "TaxCategory");
                if (procenat > 0)
                {
                    if (taxCategory.Element(nscbc + "ID").Value != "S")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku (primer: S)!\n";
                    }
                    if (norma == "OutgoingInvoicesData")
                    {
                        if (taxCategory.Element(nscbc + "Name").Value != "PDV")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravan naziv (primer: PDV)!\n";
                        }
                        if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "VAT")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: VAT)!\n";
                        }
                        if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "TaxTypeCode").Value != "StandardRated")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: StandardRated)!\n";
                        }
                    }
                    else if (norma == "Invoice")
                    {
                        if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "ID").Value != "VAT")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: VAT)!\n";
                        }
                    }
                }
                else if (procenat == 0)
                {
                    if (norma == "OutgoingInvoicesData")
                    {

                        if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "TaxTypeCode").Value != "ZeroRated")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: ZeroRated)!\n";
                        }
                        else
                        {
                            switch (taxCategory.Element(nscbc + "ID").Value)
                            {
                                case "E":
                                    if (taxCategory.Element(nscbc + "Name").Value != "OSLOBOĐENO_POREZA")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravan naziv (primer: OSLOBOĐENO_POREZA)!\n";
                                    }
                                    if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "FRE")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: FRE)!\n";
                                    }
                                    break;
                                case "O":
                                    if (taxCategory.Element(nscbc + "Name").Value != "NEOPOREZIVO")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravan naziv (primer: NEOPOREZIVO)!\n";
                                    }
                                    if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "FRE")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: FRE)!\n";
                                    }
                                    break;
                                case "AE":
                                    if (taxCategory.Element(nscbc + "Name").Value != "PPO")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravan naziv (primer: PPO)!\n";
                                    }
                                    if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "VAT")
                                    {
                                        greskaPostoji = true;
                                        SetGreska();
                                        textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za shemu (primer: VAT)!\n";
                                    }
                                    break;
                                default:
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez u rekapitulaciji nema ispravnu oznaku za kategoriju (primer: E, O, AE)!\n";
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji poreza procenat nije unet dobro kod poreske kategorije " + poreskaKategorija + ".\n";
                }
                poreskaKategorija = poreskaKategorija == "S" ? procenat.ToString() : poreskaKategorija;
                if (obracunPoreza.ContainsKey(poreskaKategorija))
                {
                    if (!rekapitulacijaPoreza.ContainsKey(poreskaKategorija))
                    {
                        //double procenat = Convert.ToDouble(poreskaKategorija);
                        if (Math.Round(iznosPoreza, 2) != Math.Round(osnovica / 100 * procenat, 2))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU rekapitulaciji poreza se ne poklapaju vrednosti osnovice, procenta i iznosa poreza kod poreske kategorije " + poreskaKategorija + ".\n";
                        }
                        else
                        {
                            rekapitulacijaPoreza.Add(poreskaKategorija, iznosPoreza);
                        }
                    }
                    else
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU rekapitulaciji poreza se ponavlja poreska kategorija " + poreskaKategorija + ".\n";
                    }
                }
                else
                {
                    greskaPostoji = true;
                    SetGreska();
                    textBoxGreske.Text += "\nU rekapitulaciji poreza, poreska kategorija " + poreskaKategorija + " ne postoji kod stavki.\n";
                }
            }

            if (!greskaPostoji)
            {
                foreach (string kategorija in obracunPoreza.Keys)
                {
                    if (!rekapitulacijaPoreza.ContainsKey(kategorija))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPoreska kategorija " + kategorija + " koja postoji kod stavki ne postoji u rekapitulaciji.\n";
                    }
                }
                if (!greskaPostoji)
                {
                    double ukupanPorezRacuna = 0;
                    //poredjenje rekapitulacijaPoreza sa obracunPoreza
                    foreach (string kat in rekapitulacijaPoreza.Keys)
                    {
                        double sumaKat = 0;
                        foreach (double iznos in obracunPoreza[kat])
                        {
                            sumaKat += iznos;
                        }
                        double procenat = kat == "E" ? 0 : kat == "AE" ? 0 : kat == "O" ? 0 : Convert.ToDouble(kat);
                        if (norma == "Invoice") sumaKat = sumaKat / 100 * procenat;
                        //poredjenje ukupnih iznosa za subtotale
                        if (Math.Round(sumaKat, 2) != rekapitulacijaPoreza[kat])
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU rekapitulaciji poreza, ukupan iznos za poresku kategoriju " + kat + " ne odgovara ukupnom porezu za stavke sa istom poreskom kategorijom.\n";
                        }
                        else
                        {
                            ukupanPorezRacuna += sumaKat;
                        }
                    }
                    if (Math.Round(ukupanPorezRacuna, 2) != ukupanPorez)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nUkupan porez na računu u TaxTotal-u se ne podudara sa zbirom iznosa iz TaxSubtotal-a.\n";
                    }
                }
            }
            return !greskaPostoji;
        }

        private bool ValidacijaIznosaStavki(XDocument doc)
        {
            List<string> unitCodes = new List<string> { "H87", "LTR", "MTQ", "TNE", "KGM", "GRM", "KMT", "MTR", "MMT", "MTK", "MIN", "HUR", "DAY", "MON", "ANN" };
            bool greskaPostoji = false;
            double sumaIznosaStavki = 0;
            bool neoporezivo = false;
            bool oporezivo = false;
            if (norma == "Invoice")
            {
                IEnumerable<XElement> invoiceLines = doc.Descendants(nscac + "InvoiceLine");
                foreach (XElement invoiceLine in invoiceLines)
                {
                    double jedinicnaCena = Convert.ToDouble(invoiceLine.Descendants(nscbc + "PriceAmount").SingleOrDefault().Value) / Convert.ToDouble(invoiceLine.Descendants(nscbc + "BaseQuantity").SingleOrDefault().Value);
                    double kolicina = Convert.ToDouble(invoiceLine.Element(nscbc + "InvoicedQuantity").Value);
                    double iznosBezPopusta = Math.Round(jedinicnaCena * kolicina, 2);
                    double iznosPopusta = 0;
                    double iznos = Convert.ToDouble(invoiceLine.Element(nscbc + "LineExtensionAmount").Value);
                    if (invoiceLine.Element(nscbc + "InvoicedQuantity").Attribute("unitCode") != null)
                    {
                        if (!unitCodes.Contains(invoiceLine.Element(nscbc + "InvoicedQuantity").Attribute("unitCode").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU 'InvoicedQuantity' polju atribut 'unitCode' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                        }
                    }
                    else
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'InvoicedQuantity' neophodan je atribut 'unitCode'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                    }
                    if (invoiceLine.Element(nscac + "Price").Element(nscbc + "BaseQuantity").Attribute("unitCode") != null)
                    {
                        if (!unitCodes.Contains(invoiceLine.Element(nscac + "Price").Element(nscbc + "BaseQuantity").Attribute("unitCode").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU 'Price' čvoru atribut 'unitCode' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                        }
                    }
                    else
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'BaseQuantity' neophodan je atribut 'unitCode'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                    }
                    if (!currencyCodes.Contains(invoiceLine.Element(nscbc + "LineExtensionAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'LineExtensionAmount' polju atribut 'currencyID' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    if (!currencyCodes.Contains(invoiceLine.Element(nscac + "Price").Element(nscbc + "PriceAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'Price' čvoru atribut 'currencyID' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    if (invoiceLine.Element(nscac + "AllowanceCharge") != null)
                    {
                        List<XElement> allowanceCharges = invoiceLine.Elements(nscac + "AllowanceCharge").ToList();
                        foreach (XElement allowanceCharge in allowanceCharges)
                        {
                            if (!currencyCodes.Contains(allowanceCharge.Element(nscbc + "Amount").Attribute("currencyID").Value))
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nU 'AllowanceCharge' čvoru atribut 'currencyID' u tagu 'Amount' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            }
                            if (allowanceCharge.Element(nscbc + "BaseAmount") != null)
                            {
                                if (!currencyCodes.Contains(allowanceCharge.Element(nscbc + "BaseAmount").Attribute("currencyID").Value))
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nU 'AllowanceCharge' čvoru atribut 'currencyID' u tagu 'BaseAmount' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                            }
                            if (allowanceCharge.Element(nscbc + "ChargeIndicator").LastNode.ToString() == "false")
                            {
                                iznosPopusta += Convert.ToDouble(allowanceCharge.Element(nscbc + "Amount").LastNode.ToString());
                            }
                            else if (allowanceCharge.Element(nscbc + "ChargeIndicator").LastNode.ToString() == "true")
                            {
                                iznosPopusta -= Convert.ToDouble(allowanceCharge.Element(nscbc + "Amount").LastNode.ToString());
                            }
                            else
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nIndikator popusta na nivou stavke treba da bude 'true' ili 'false'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            }
                            if (allowanceCharge.Element(nscbc + "MultiplierFactorNumeric") != null)
                            {
                                double procenatPopusta = Convert.ToDouble(allowanceCharge.Element(nscbc + "MultiplierFactorNumeric").LastNode.ToString());
                                double osnovicaPopusta = Convert.ToDouble(allowanceCharge.Element(nscbc + "BaseAmount").LastNode.ToString());
                                if (ProveraPopustaPoStavkama(procenatPopusta, osnovicaPopusta, iznosPopusta, invoiceLine.Element(nscbc + "ID").Value.ToString())) greskaPostoji = true;
                            }
                        }
                        if (!greskaPostoji)
                        {
                            if (Math.Round(iznos, 2) != Math.Round(iznosBezPopusta - iznosPopusta, 2))
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nIznos u LineExtensionAmount na osnovu stavke ne odgovara iznosu bez popusta umanjenom za popust. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + "\n";
                            }
                        }
                    }
                    else if (iznos != iznosBezPopusta)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nIznos u LineExtensionAmount na osnovu stavke ne odgovara proizvodu jedinične cene i količine. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + "\n";
                    }
                    XElement classifiedTaxCategory = invoiceLine.Descendants(nscac + "ClassifiedTaxCategory").SingleOrDefault();
                    string procenatPoreza = classifiedTaxCategory.Element(nscbc + "Percent").Value;
                    double procenat = Convert.ToDouble(procenatPoreza);
                    if (classifiedTaxCategory.Element(nscac + "TaxScheme").Element(nscbc + "ID").Value != "VAT")
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: VAT)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    if (procenat > 0)
                    {
                        if (neoporezivo)
                        {
                            SetGreska();
                            textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            break;
                        }
                        oporezivo = true;
                        if (classifiedTaxCategory.Element(nscbc + "ID").Value != "S")
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku (primer: S)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                    }
                    else if (procenat == 0)
                    {
                        switch (classifiedTaxCategory.Element(nscbc + "ID").Value)
                        {
                            case "E":
                                if (neoporezivo)
                                {
                                    SetGreska();
                                    textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                    break;
                                }
                                oporezivo = true;
                                break;
                            case "O":
                                neoporezivo = true;
                                if (oporezivo)
                                {
                                    SetGreska();
                                    textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                    break;
                                }
                                break;
                            case "AE":
                                if (neoporezivo)
                                {
                                    SetGreska();
                                    textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                    break;
                                }
                                oporezivo = true;
                                break;
                            default:
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za kategoriju (primer: E, O, AE)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                break;
                        }
                    }
                    if (procenat != 0)
                    {
                        if (obracunPoreza.ContainsKey(procenat.ToString()))
                        {
                            obracunPoreza[procenat.ToString()].Add(iznos);
                        }
                        else
                        {
                            obracunPoreza.Add((procenat.ToString()), new List<double> { iznos });
                        }
                    }
                    else
                    {
                        if (obracunPoreza.ContainsKey(classifiedTaxCategory.Element(nscbc + "ID").Value))
                        {
                            obracunPoreza[classifiedTaxCategory.Element(nscbc + "ID").Value].Add(iznos);
                        }
                        else
                        {
                            obracunPoreza.Add(classifiedTaxCategory.Element(nscbc + "ID").Value, new List<double> { iznos });
                        }
                    }
                    sumaIznosaStavki += iznos;
                }
            }
            else if (norma == "OutgoingInvoicesData")
            {
                IEnumerable<XElement> invoiceLines = doc.Descendants(nscac + "InvoiceLine");
                foreach (XElement invoiceLine in invoiceLines)
                {
                    double jedinicnaCena = Convert.ToDouble(invoiceLine.Descendants(nscbc + "PriceAmount").SingleOrDefault().Value) / Convert.ToDouble(invoiceLine.Descendants(nscbc + "BaseQuantity").SingleOrDefault().Value);
                    double kolicina = Convert.ToDouble(invoiceLine.Element(nscbc + "InvoicedQuantity").LastNode.ToString());
                    double iznosBezPopusta = Math.Round(jedinicnaCena * kolicina, 2);
                    double iznos = Convert.ToDouble(invoiceLine.Element(nscbc + "LineExtensionAmount").LastNode.ToString());
                    double iznosUkupnogPopusta = 0;
                    if (invoiceLine.Element(nscbc + "InvoicedQuantity").Attribute("unitCode") != null)
                    {
                        if (!unitCodes.Contains(invoiceLine.Element(nscbc + "InvoicedQuantity").Attribute("unitCode").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU 'InvoicedQuantity' polju atribut 'unitCode' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                        }
                    }
                    else
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'InvoicedQuantity' neophodan je atribut 'unitCode'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                    }
                    if (invoiceLine.Element(nscac + "Price").Element(nscbc + "BaseQuantity").Attribute("unitCode") != null)
                    {
                        if (!unitCodes.Contains(invoiceLine.Element(nscac + "Price").Element(nscbc + "BaseQuantity").Attribute("unitCode").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nU 'Price' čvoru atribut 'unitCode' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                    }
                    else
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'BaseQuantity' neophodan je atribut 'unitCode'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value + ".\n";
                    }
                    if (!currencyCodes.Contains(invoiceLine.Element(nscbc + "LineExtensionAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'LineExtensionAmount' polju atribut 'currencyID' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    if (!currencyCodes.Contains(invoiceLine.Element(nscac + "Price").Element(nscbc + "PriceAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nU 'Price' čvoru atribut 'currencyID' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    if (invoiceLine.Element(nscac + "AllowanceCharge") != null)
                    {
                        List<XElement> allowanceCharges = invoiceLine.Elements(nscac + "AllowanceCharge").ToList();
                        foreach (XElement allowanceCharge in allowanceCharges)
                        {
                            double iznosPopusta = Convert.ToDouble(allowanceCharge.Element(nscbc + "Amount").LastNode.ToString());
                            if (!currencyCodes.Contains(allowanceCharge.Element(nscbc + "Amount").Attribute("currencyID").Value))
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nU 'AllowanceCharge' čvoru atribut 'currencyID' u tagu 'Amount' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            }
                            if (allowanceCharge.Element(nscbc + "AllowanceChargeReason") == null && allowanceCharge.Element(nscbc + "AllowanceChargeReasonCode") == null)
                            {
                                greskaPostoji = true;
                                SetGreska();
                                textBoxGreske.Text += "\nU 'AllowanceCharge' čvoru mora da postoji jedno od ponuđena 2: AllowanceChargeReason ili AllowacneChargeReasonCode. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            }
                            if (allowanceCharge.Element(nscbc + "BaseAmount") != null)
                            {
                                if (!currencyCodes.Contains(allowanceCharge.Element(nscbc + "BaseAmount").Attribute("currencyID").Value))
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nU 'AllowanceCharge' čvoru atribut 'currencyID' u tagu 'BaseAmount' nema ispravnu vrednost. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                            }
                            if (allowanceCharge.Element(nscbc + "MultiplierFactorNumeric") != null)
                            {
                                double procenatPopusta = Convert.ToDouble(allowanceCharge.Element(nscbc + "MultiplierFactorNumeric").LastNode.ToString());
                                if (allowanceCharge.Element(nscbc + "BaseAmount") != null)
                                {
                                    double osnovicaPopusta = Convert.ToDouble(allowanceCharge.Element(nscbc + "BaseAmount").LastNode.ToString());
                                    if (ProveraPopustaPoStavkama(procenatPopusta, osnovicaPopusta, iznosPopusta, invoiceLine.Element(nscbc + "ID").Value.ToString())) greskaPostoji = true;
                                }
                            }
                            if (!greskaPostoji)
                            {
                                if (allowanceCharge.Element(nscbc + "ChargeIndicator").LastNode.ToString() == "false")
                                {
                                    iznosUkupnogPopusta += iznosPopusta;
                                }
                                else if (allowanceCharge.Element(nscbc + "ChargeIndicator").LastNode.ToString() == "true")
                                {
                                    iznosUkupnogPopusta -= iznosPopusta;
                                }
                                else
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nIndikator popusta na nivou stavke treba da bude 'true' ili 'false'. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                            }
                        }
                        if (Math.Round(iznos, 2) != Math.Round(iznosBezPopusta - iznosUkupnogPopusta, 2))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nIznos u LineExtensionAmount na osnovu stavke ne odgovara iznosu bez popusta umanjenom za popust. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                    }
                    else if (iznos != iznosBezPopusta)
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nIznos u LineExtensionAmount na osnovu stavke ne odgovara proizvodu jedinične cene i količine. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    XElement taxTotal = invoiceLine.Element(nscac + "TaxTotal");
                    double taxAmount = Convert.ToDouble(taxTotal.Element(nscbc + "TaxAmount").LastNode.ToString());
                    double ukupanPorezStavke = 0;
                    if (!currencyCodes.Contains(taxTotal.Element(nscbc + "TaxAmount").Attribute("currencyID").Value))
                    {
                        greskaPostoji = true;
                        SetGreska();
                        textBoxGreske.Text += "\nPorez za stavku unutar 'TaxAmount' nema ispravnu vrednost atributa 'currencyID' (primer: RSD)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                    }
                    IEnumerable<XElement> taxSubtotals = taxTotal.Descendants(nscac + "TaxSubtotal");
                    foreach (XElement taxSubtotal in taxSubtotals)
                    {
                        if (!currencyCodes.Contains(taxSubtotal.Element(nscbc + "TaxAmount").Attribute("currencyID").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez za stavku unutar 'TaxSubtotal' za poresku kategoriju " + taxSubtotal.Element(nscac + "TaxCategory").Element(nscbc + "ID").Value + " nema ispravnu vrednost atributa 'currencyID' (primer: RSD)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                        if (!currencyCodes.Contains(taxSubtotal.Element(nscbc + "TaxableAmount").Attribute("currencyID").Value))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nOsnovica za stavku unutar 'TaxSubtotal' za poresku kategoriju " + taxSubtotal.Element(nscac + "TaxCategory").Element(nscbc + "ID").Value + " nema ispravnu vrednost atributa 'currencyID' (primer: RSD)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                        double procenatPoreza = Convert.ToDouble(taxSubtotal.Descendants(nscbc + "Percent").SingleOrDefault().Value.ToString());
                        if (Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString()) != Math.Round(Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxableAmount").LastNode.ToString()) / 100 * procenatPoreza, 2))
                        {
                            greskaPostoji = true;
                            SetGreska();
                            textBoxGreske.Text += "\nPorez na nivou stavke nije dobro obračunat. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                        }
                        else
                        {
                            XElement taxCategory = taxSubtotal.Element(nscac + "TaxCategory");
                            if (procenatPoreza > 0)
                            {
                                if (taxCategory.Element(nscbc + "Name").Value != "PDV")
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez na nivou stavke nema ispravan naziv (primer: PDV)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                                if (taxCategory.Element(nscbc + "ID").Value != "S")
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku (primer: S)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                                if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "VAT")
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: VAT)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                                if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "TaxTypeCode").Value != "StandardRated")
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: StandardRated)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                                if (neoporezivo)
                                {
                                    SetGreska();
                                    textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                    break;
                                }
                                oporezivo = true;
                            }
                            else if (procenatPoreza == 0)
                            {
                                if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "TaxTypeCode").Value != "ZeroRated")
                                {
                                    greskaPostoji = true;
                                    SetGreska();
                                    textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: ZeroRated)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                }
                                else
                                {
                                    switch (taxCategory.Element(nscbc + "ID").Value)
                                    {
                                        case "E":
                                            if (taxCategory.Element(nscbc + "Name").Value != "OSLOBOĐENO_POREZA")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravan naziv (primer: OSLOBOĐENO_POREZA)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "FRE")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: FRE)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (neoporezivo)
                                            {
                                                SetGreska();
                                                textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                                break;
                                            }
                                            oporezivo = true;
                                            break;
                                        case "O":
                                            if (taxCategory.Element(nscbc + "Name").Value != "NEOPOREZIVO")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravan naziv (primer: NEOPOREZIVO)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "FRE")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: FRE)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (oporezivo)
                                            {
                                                SetGreska();
                                                textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                                break;
                                            }
                                            neoporezivo = true;
                                            break;
                                        case "AE":
                                            if (taxCategory.Element(nscbc + "Name").Value != "PPO")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravan naziv (primer: PPO)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (taxCategory.Element(nscac + "TaxScheme").Element(nscbc + "Name").Value != "VAT")
                                            {
                                                greskaPostoji = true;
                                                SetGreska();
                                                textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za shemu (primer: VAT)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            }
                                            if (neoporezivo)
                                            {
                                                SetGreska();
                                                textBoxGreske.Text += "\nNije dozvoljeno imati stavke koje su oporezive na istom računu sa neoporezivim! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                                break;
                                            }
                                            oporezivo = true;
                                            break;
                                        default:
                                            greskaPostoji = true;
                                            SetGreska();
                                            textBoxGreske.Text += "\nPorez na nivou stavke nema ispravnu oznaku za kategoriju (primer: E, O, AE)! InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                                            break;
                                    }
                                }
                            }
                            if (!greskaPostoji)
                            {
                                ukupanPorezStavke += Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString());
                                if (obracunPoreza.ContainsKey(procenatPoreza.ToString()))
                                {
                                    if (procenatPoreza.ToString() != "0")
                                    {
                                        obracunPoreza[procenatPoreza.ToString()].Add(Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString()));
                                    }
                                    else
                                    {
                                        obracunPoreza[taxCategory.Element(nscbc + "ID").Value].Add(Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString()));
                                    }
                                }
                                else
                                {
                                    if (procenatPoreza.ToString() != "0")
                                    {
                                        obracunPoreza.Add(procenatPoreza.ToString(), new List<double> { Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString()) });
                                    }
                                    else
                                    {
                                        obracunPoreza.Add(taxCategory.Element(nscbc + "ID").Value, new List<double> { Convert.ToDouble(taxSubtotal.Element(nscbc + "TaxAmount").LastNode.ToString()) });
                                    }
                                }
                            }
                        }
                    }
                    if (!greskaPostoji)
                    {
                        if (taxAmount != ukupanPorezStavke)
                        {
                            SetGreska();
                            textBoxGreske.Text += "\nUkupan porez za stavku se ne poklapa sa zbirom TaxAmount-a svih TaxSubtotal-a te stavke. InvoiceLine/ID = " + invoiceLine.Element(nscbc + "ID").Value.ToString() + ".\n";
                            greskaPostoji = true;
                        }
                    }
                    sumaIznosaStavki += iznos;
                }
            }
            double lineExtensionAmount = Convert.ToDouble(doc.Descendants().SingleOrDefault(p => p.Name.LocalName == "Invoice").Element(nscac + "LegalMonetaryTotal").Element(nscbc + "LineExtensionAmount").Value);
            if (lineExtensionAmount != Math.Round(sumaIznosaStavki, 2))
            {
                SetGreska();
                textBoxGreske.Text += "\nSuma iznosa stavki se ne poklapa sa sumom u polju LegalMonetaryTotal/LineExtensionAmount.\n";
                greskaPostoji = true;
            }
            return !greskaPostoji;
        }

        private bool ProveraPopustaPoStavkama(double procenatPopusta, double osnovicaPopusta, double iznosPopusta, String ID)
        {
            if (procenatPopusta > 1)
            {
                if (iznosPopusta != Math.Round(osnovicaPopusta / 100 * procenatPopusta, 2))
                {
                    label_validnost.Content = "Neispravan ❌";
                    label_validnost.Foreground = Brushes.Red;
                    canvas.Width = 1230;
                    textBoxGreske.Visibility = Visibility.Visible;
                    textBoxGreske.IsEnabled = true;
                    textBoxGreske.Text += "\nPopust na nivou stavke nije dobro obračunat. InvoiceLine/ID = " + ID + ".\n";
                    return true;
                }
                else { return false; }
            }
            else
            {
                if (iznosPopusta != Math.Round(osnovicaPopusta * procenatPopusta, 2))
                {
                    label_validnost.Content = "Neispravan ❌";
                    label_validnost.Foreground = Brushes.Red;
                    canvas.Width = 1230;
                    textBoxGreske.Visibility = Visibility.Visible;
                    textBoxGreske.IsEnabled = true;
                    textBoxGreske.Text += "\nPopust na nivou stavke nije dobro obračunat. InvoiceLine/ID = " + ID + ".\n";
                    return true;
                }
                else { return false; }
            }
        }

        private void SetGreska()
        {
            label_validnost.Content = "Neispravan ❌";
            label_validnost.Foreground = Brushes.Red;
            canvas.Width = 1230;
            textBoxGreske.Visibility = Visibility.Visible;
            label_greske.Visibility = Visibility.Visible;
            textBoxGreske.IsEnabled = true;
        }
    }
}
