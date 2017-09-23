using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using mshtml;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Threading;
using System.Data;
using System.Windows.Input;

namespace Tender_Glucauf
{
    public partial class MainWindow : Window
    {
        static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;
        private string result = "";
        private List<Tenders> custdata = new List<Tenders>();
        private List<Links> all_lnks = new List<Links>();
        private int count = 1;
        private int i = 1;

        public MainWindow()
        {
            InitializeComponent();
            Table.Visibility = Visibility.Hidden;
        }

        private void DG_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hyperlink link = (Hyperlink)e.OriginalSource;
                if (link != null && link is Hyperlink)
                {
                    Table.IsEnabled = false;
                    Process.Start(link.NavigateUri.AbsoluteUri);
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 3);
                    timer.Tick += (o, t) => { Table.IsEnabled = true; timer.Stop(); };
                    timer.Start();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка в методе DG_Hyperlink_Click");
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var displayName = GetPropertyDisplayName(e.PropertyDescriptor);

            if (!string.IsNullOrEmpty(displayName))
            {
                e.Column.Header = displayName;
                e.Column.MaxWidth = 500;
            }
        }

        private static string GetPropertyDisplayName(object descriptor)
        {
            var pd = descriptor as PropertyDescriptor;

            if (pd != null)
            {
                // Check for DisplayName attribute and set the column header accordingly
                var displayName = pd.Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;

                if (displayName != null && displayName != DisplayNameAttribute.Default)
                {
                    return displayName.DisplayName;
                }

            }
            else
            {
                var pi = descriptor as PropertyInfo;

                if (pi != null)
                {
                    // Check for DisplayName attribute and set the column header accordingly
                    object[] attributes = pi.GetCustomAttributes(typeof(DisplayNameAttribute), true);
                    for (int i = 0; i < attributes.Length; ++i)
                    {
                        var displayName = attributes[i] as DisplayNameAttribute;
                        if (displayName != null && displayName != DisplayNameAttribute.Default)
                        {
                            return displayName.DisplayName;
                        }
                    }
                }
            }

            return null;
        }


        private void search_textbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(search_textbox.Text))
            {
                if (search_textbox.Text.Length > 0)
                {
                    start_btn.IsEnabled = true;
                    start_btn.Cursor = Cursors.Hand;
                }
                else
                {
                    start_btn.IsEnabled = false;
                    start_btn.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                start_btn.IsEnabled = false;
                start_btn.Cursor = Cursors.Arrow;
            }
        }

        private async void start_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(search_textbox.Text)) return;
            stop_btn.IsEnabled = false;
            stop_btn.IsEnabled = true;
            stop_btn.Cursor = Cursors.Hand;
            clear_btn.Cursor = Cursors.Arrow;
            start_btn.Cursor = Cursors.Arrow;
            Table.IsEnabled = false;
            label_info.Content = "Идёт загрузка...";

            if (Table.Visibility != Visibility.Visible) Table.Visibility = Visibility.Visible;

            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;

            string url = "";

            if (count == 1) url = "http://www.zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=" + search_textbox.Text + "&morphology=on&pageNumber=1&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&fz223=on&priceFrom=&priceTo=&currencyId=1&regions=&af=true&ca=true&sortBy=UPDATE_DATE&openMode=USE_DEFAULT_PARAMS";
            else url = "http://www.zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=" + search_textbox.Text + "&morphology=on&pageNumber=" + count + "&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&fz223=on&priceFrom=&priceTo=&currencyId=1&regions=&af=true&ca=true&sortBy=UPDATE_DATE&openMode=USE_DEFAULT_PARAMS";
            count++;

            result = await GET(url);

            Parse(result);

            Table.ItemsSource = null;
            Table.ItemsSource = custdata;
            Table.IsEnabled = true;
            start_btn.Cursor = Cursors.Hand;
            stop_btn.Cursor = Cursors.Arrow;
            clear_btn.Cursor = Cursors.Hand;

        }

        private void Parse(string html)
        {
            start_btn.IsEnabled = false;
            clear_btn.IsEnabled = false;
            try
            {
                if (!html.Contains("registerBox registerBoxBank margBtm20"))
                {
                    stop_btn.IsEnabled = false;
                    start_btn.IsEnabled = true;
                    clear_btn.IsEnabled = true;
                    start_btn.Content = "Ещё";
                    label_info.Content = "Нету данных по данному запросу";
                    progress_bar.Value = 0;
                    return;
                }
                else
                {
                    HTMLDocument doc = new HTMLDocument();
                    IHTMLDocument2 doc2 = (IHTMLDocument2)doc;

                    doc2.write(html.Replace("http", "@@@ @@@"));

                    IHTMLDocument3 doc3 = (IHTMLDocument3)doc2;

                    var divs = doc3.getElementsByTagName("div");

                    foreach (var div in divs)
                    {
                        if (token.IsCancellationRequested)
                        {
                            Console.WriteLine("Операция прервана токеном");
                            goto M;
                        }
                        var t = (IHTMLElement)div;

                        if (t.className != null)
                        {
                            if (t.className.Contains("registerBox registerBoxBank margBtm20"))
                            {
                                var hrefs = Find(t.innerHTML);

                                HTMLDocument _doc = new HTMLDocument();
                                IHTMLDocument2 _doc2 = (IHTMLDocument2)_doc;
                                _doc2.write(t.innerHTML);
                                IHTMLDocument3 _doc3 = (IHTMLDocument3)_doc2;

                                List<string> strong = new List<string>();

                                foreach (IHTMLElement temp in _doc3.getElementsByTagName("strong"))
                                {
                                    if (temp.innerText != null && temp.innerText != "")
                                        strong.Add(temp.innerText.Replace("\n", "").Replace("  ", "").Replace("&nbsp;", " ").Replace(",", "."));
                                }

                                List<string> span = new List<string>();

                                foreach (IHTMLElement temp in _doc3.getElementsByTagName("span"))
                                {
                                    if (temp.innerText != null && temp.innerText != "")
                                        span.Add(temp.innerText.Replace("  ", "").Replace("\n", "").Replace("/", ""));
                                }

                                List<string> dd = new List<string>();

                                foreach (IHTMLElement temp in _doc3.getElementsByTagName("dd"))
                                {
                                    if (temp.innerText != null && temp.innerText != "")
                                        dd.Add(temp?.innerText.Replace("  ", "").Replace("\n", "").Replace("/", "").Replace("Заказчик:", "").Replace("&nbsp;", " ").Replace("закупки:", "закупки:\n").Replace("Идентификационный", "\nИдентификационный"));
                                }

                                List<string> li = new List<string>();

                                foreach (IHTMLElement temp in _doc3.getElementsByTagName("li"))
                                {
                                    li.Add(temp?.innerText.Replace("  ", "").Replace("\n", "").Replace("Размещено:", "").Replace("/", "").Replace("&nbsp;", " "));
                                }

                                string descript = "";

                                if (dd.Count >= 5)
                                {
                                    if (dd[4].Contains("function")) dd[4].Substring(dd[4].LastIndexOf("}}"), dd[4].Length - dd[4].LastIndexOf("}}"));
                                    else descript = dd[3]; //+ " " + dd[4];
                                }
                                else if (dd.Count == 4 || dd.Count == 3) descript = dd[2];//+"\n"+dd[3];
                                else if (dd.Count == 2) descript = dd[1];

                                Hyperlink link = new Hyperlink();
                                link.NavigateUri = new Uri(WebUtility.HtmlDecode("http://www.zakupki.gov.ru" + (from x in hrefs where x.Text == "Сведения" select x.Href).First().Replace("http://zakupki.gov.ru", "")));
                                link.TargetName = "Сведения";

                                Hyperlink link2 = new Hyperlink();
                                link2.NavigateUri = new Uri(WebUtility.HtmlDecode("http://www.zakupki.gov.ru" + (from x in hrefs where x.Href.Contains("organization") select x.Href.Replace("http://zakupki.gov.ru", "")).First()));
                                link2.TargetName = "Организация";

                                Hyperlink link3 = new Hyperlink();
                                link3.NavigateUri = new Uri(WebUtility.HtmlDecode("http://www.zakupki.gov.ru" + (from x in hrefs where x.Text == "Документы" select x.Href).First().Replace("http://zakupki.gov.ru", "")));
                                link3.TargetName = "Документация";

                                all_lnks.Add(new Links { url1 = link.NavigateUri, url2 = link2.NavigateUri, url3 = link3.NavigateUri });

                                string _sum = "";

                                string value = strong[1];
                                double number;

                                if (double.TryParse(value, out number))
                                {
                                    _sum = number.ToString();
                                }
                                else
                                {
                                    foreach (var _t in strong)
                                    {
                                        _sum += _t + " ";
                                    }
                                }
                                //_sum = _sum.Replace("Лот", "\nЛот ").Replace("аукцион", "аукцион\n ").Replace("форме", "форме\n ").Replace("конкурсе", "конкурсе\n ").Replace("котировок", "котировок\n ").Replace("отбор", "отбор\n ").Replace("закупка", "закупка\n ").Replace("закупки", "закупки\n ").Replace(" .", ".").Replace("поставщика", "поставщика\n ").Replace("предложений", "предложений\n ");

                                custdata.Add(new Tenders
                                {
                                    number = i,
                                    type = CheckLenght(strong[0] + " " + span[1]),
                                    description = CheckLenght(descript, 2),
                                    sum = CheckLenght(_sum, 2),// + "\n" + span[3],
                                    date_start = li[1],
                                    organizer = CheckLenght(li[0], 2),
                                    url = link.TargetName,
                                    contact = link2.TargetName,// WebUtility.HtmlDecode("http://www.zakupki.gov.ru" + (from x in hrefs where x.Href.Contains("organization") select x.Href.Replace("http://zakupki.gov.ru", "")).First()),
                                    documentation = link3.TargetName//WebUtility.HtmlDecode("http://www.zakupki.gov.ru" + (from x in hrefs where x.Text == "Документы" select x.Href).First().Replace("http://zakupki.gov.ru", ""))
                                });
                                progress_bar.Value++;
                                i++;
                            }
                        }
                    }
                M:
                    stop_btn.IsEnabled = false;
                    start_btn.IsEnabled = true;
                    clear_btn.IsEnabled = true;
                    start_btn.Content = "Ещё";
                    progress_bar.Value = 0;
                    label_info.Content = "Загрузка окончена.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка в методе Parse");
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private static string CheckLenght(string temp, int n = 1)
        {
            if (string.IsNullOrWhiteSpace(temp)) return temp;
            int interval = 22;
            switch (n)
            {
                case 2: interval = 45; break;
                default: break;
            }

            var result = new StringBuilder();

            int chek = interval; int ind = 2;

            for (int i = 0; i < temp.Length; i++)
            {
                if (temp[i] != ' ' && chek != i)
                {
                    result.Append(temp[i]);
                }
                else if (temp[i] != ' ' && chek == i)
                {
                    result.Append(temp[i]);
                    chek++;
                }
                else if (temp[i] == '\n' && chek == i)
                {
                    result.Append('\n');
                    chek = interval * ind;
                    ind++;
                }
                else if (temp[i] == ' ' && chek == i)
                {
                    result.Append(' ');
                    result.Append('\n');
                    chek = interval * ind;
                    ind++;
                }
                else if (temp[i] == ' ' && chek != i)
                {
                    result.Append(' ');
                }
            }
            return result.ToString();
        }

        public static List<LinkItem> Find(string file)
        {
            List<LinkItem> list = new List<LinkItem>();

            file = file.Replace("@@@ @@@", "http");

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<A.*?>.*?</A>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                RegexOptions.Singleline);
                i.Text = t;

                list.Add(i);
            }
            return list;
        }

        public struct LinkItem
        {
            public string Href;
            public string Text;

            public override string ToString()
            {
                return Href + "\n\t" + Text;
            }
        }

        private async Task<string> GET(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Get;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0";
                request.AllowAutoRedirect = false;
                request.ServicePoint.Expect100Continue = false;
                request.ProtocolVersion = HttpVersion.Version11;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Accept = "text/html,application/json,application/xml;q=0.9,*/*;q=0.8";
                return await Task.Run(() =>
                {
                    string resp = RESPONSE(request);
                    return resp;
                });
            }
            catch (Exception ex)
            {
                return ex.Message + "\n" + ex.StackTrace;
            }
        }

        private string RESPONSE(HttpWebRequest request)
        {
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string answer = "";
                var headers = response.Headers.ToString();

                if (Convert.ToInt32(response.StatusCode) == 302 || Convert.ToInt32(response.StatusCode) == 200)
                {
                    using (Stream rspStm = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(rspStm, Encoding.UTF8, true))
                        {
                            answer = string.Empty; answer = reader.ReadToEnd();
                        }
                    }
                    return answer;
                }
                else
                {
                    response.Close(); return WebUtility.HtmlDecode(response.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                return WebUtility.HtmlDecode(ex.Message) + "\n" + ex.StackTrace;
            }
        }

        private void stop_btn_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
        }

        private void search_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) start_btn_Click(null, null);
        }

        private void Table_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Table.CurrentCell.Column != null && Table.SelectedIndex >= 0)
            {
                int index0 = Table.SelectedIndex;
                int index = Table.CurrentCell.Column.DisplayIndex;

                Hyperlink link = new Hyperlink();

                switch (index)
                {
                    case 6: link.NavigateUri = all_lnks[index0].url1; break;
                    case 7: link.NavigateUri = all_lnks[index0].url2; break;
                    case 8: link.NavigateUri = all_lnks[index0].url3; break;
                    default: return;
                }

                if (link != null && link is Hyperlink)
                {
                    Table.IsEnabled = false;
                    Process.Start(link.NavigateUri.AbsoluteUri);
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 3);
                    timer.Tick += (o, t) => { Table.IsEnabled = true; timer.Stop(); };
                    timer.Start();
                }
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            if (Table.ItemsSource != null)
            {
                clear_btn.Cursor = Cursors.Arrow;
                label_info.Content = "Таблица очищена.";
                Table.ItemsSource = null;
                custdata.Clear();
                all_lnks.Clear();
                count = 1;
                i = 1;
                clear_btn.IsEnabled = false;
            }
        }
    }

    public class Tenders
    {

        [DisplayName("№")]
        public int number { get; set; } = 0;
        [DisplayName("Тип закупки")]
        public string type { get; set; } = "";
        [DisplayName("Наименование закупки")]
        public string description { get; set; } = "";
        [DisplayName("Сумма")]
        public string sum { get; set; } = "";
        [DisplayName("Опубликовано")]
        public string date_start { get; set; } = "";
        [DisplayName("Организатор")]
        public string organizer { get; set; } = "";
        [DisplayName("Источник")]
        public string url { get; set; } = "";
        [DisplayName("Учётная карточка")]
        public string contact { get; set; } = "";
        [DisplayName("Документация")]
        public string documentation { get; set; } = "";
    }

    public class Links
    {
        public Uri url1 { get; set; } = null;
        public Uri url2 { get; set; } = null;
        public Uri url3 { get; set; } = null;
    }
}
