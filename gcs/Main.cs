using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gcs
{
    public partial class Main : Form
    {
        List<string> keywords = new List<string>();
        List<ActiveKeyword> activeKeywords = new List<ActiveKeyword>();
        Dictionary<string, List<ActiveKeyword>> dictKeys = new Dictionary<string, List<ActiveKeyword>>();
        /// <summary>
        /// 
        /// </summary>
        public Main()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtApiKey.Text)
                && !string.IsNullOrEmpty(txtSearchID.Text))
            {
                int keywordsCount = keywords.Count;
                prgSearch.Visible = true;
                prgSearch.Minimum = 1;
                prgSearch.Maximum = keywordsCount;
                prgSearch.Value = 1;
                prgSearch.Step = 1;
                if (keywordsCount > 0)
                {
                    for (int i = 0; i < keywordsCount; i++)
                    {
                        string query = keywords[i].Trim();
                        if (!string.IsNullOrEmpty(query))
                        {
                            bool isSuccess = DoSearch(txtApiKey.Text.Trim(), txtSearchID.Text.Trim(), query, txtNumberOfResult.Text);
                            if (isSuccess)
                            {
                                prgSearch.PerformStep();
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Keywords file is empty. Please check it again.");
                }
                if (prgSearch.Value == keywordsCount)
                {
                    MessageBox.Show("Searching done, please click to button Export to export data.");
                }
            }
            else
            {
                MessageBox.Show("APIs key or Search Engine Id do not allow empty.");
            }
        }

        /// <summary>
        /// Export to CSV file
        /// Only CSV comma delimited files are supported 
        /// </summary>
        private void Export2Csv(List<ActiveKeyword> keywords)
        {
            string noData = "No data available";
            string filePath = ConfigurationManager.AppSettings["FilePath"];
            string fileName = string.Empty;
            StringBuilder sb = new StringBuilder();
            if (keywords.Count > 0)
            {
                //add CSV file header
                sb.Append("KEY WORDS");
                sb.Append(",");
                sb.Append("URL");
                sb.Append(",");
                sb.Append("TOTAL RESULTS");
                sb.Append(",");
                sb.Append("TITLE");
                sb.Append(",");
                sb.Append("STATUS");
                sb.AppendLine();

                foreach (ActiveKeyword item in keywords)
                {
                    sb.Append(EncodeCsvString(item.Keyword ?? ""));
                    sb.Append(",");
                    sb.Append(EncodeCsvString(item.Url ?? ""));
                    sb.Append(",");
                    sb.Append(EncodeCsvString(item.TotalResult ?? ""));
                    sb.Append(",");
                    sb.Append(EncodeCsvString(item.Title ?? ""));
                    sb.Append(",");
                    sb.Append(item.Status);
                    sb.AppendLine();
                }
            }
            else
            {
                sb.Append(noData);
            }

            string csvData = sb.ToString();
            if (!string.IsNullOrEmpty(csvData))
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                fileName = string.Concat(ConfigurationManager.AppSettings["FileName"], "_", DateTime.Now.ToString("yyyyMMddHHmmss"), ".csv");
                string strFileLocation = Path.Combine(filePath, fileName);
                File.WriteAllText(strFileLocation, csvData);
                MessageBox.Show("Export to CSV file had been done, The CSV file was stored following " + strFileLocation + "!");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExport_Click(object sender, EventArgs e)
        {
            Export2Csv(activeKeywords);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_Load(object sender, EventArgs e)
        {
            string keywordFilePath = ConfigurationManager.AppSettings["KeywordFilePath"];
            keywords = LoadFiles(keywordFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> LoadFiles(string filePath)
        {
            string[] lines;
            var list = new List<string>();
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }
                lines = list.ToArray();
            }
            else
            {
                WriteLog("KeywordFilePath is not empty.");
            }
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string EncodeCsvString(string input)
        {
            input = System.Text.RegularExpressions.Regex.Replace(input, @"(?:\r\n|\n|\r)+", " ", System.Text.RegularExpressions.RegexOptions.Multiline);

            string output = string.Join("\"\"", input.Split('"'));
            if (output.IndexOf(',') != -1)
                output = "\"" + output + "\"";
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="searchEngineId"></param>
        /// <param name="query"></param>
        private bool DoSearch(string apiKey, string searchEngineId, string query, string numberOfResult)
        {
            bool isSuccess = false;
            long pageResults = 1;
            if (string.IsNullOrEmpty(numberOfResult))
            {
                numberOfResult = "1";
            }
            long.TryParse(numberOfResult, out pageResults);
            try
            {
                long? starPage = 1;
                long? totalResults = 0;
                decimal numberOfPage = 0;
                isSuccess = Scraper(apiKey, searchEngineId, query, starPage, out totalResults);
                if (isSuccess && totalResults > 0)
                {
                    numberOfPage = Math.Ceiling((decimal)totalResults / 10);
                    if (starPage < numberOfPage)
                    {
                        for (int i = 1; i < pageResults; i++)
                        {
                            List<ActiveKeyword> lstBeginPage = dictKeys[query.ToLower()];
                            if (lstBeginPage != null)
                            {
                                starPage = lstBeginPage.Count + 1;
                            }
                            isSuccess = Scraper(apiKey, searchEngineId, query, starPage, out totalResults);
                        }
                    }
                    else
                    {
                        WriteLog("Start page bigger than number of page");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                isSuccess = false;
            }
            return isSuccess;
        }

        private bool Scraper(string apiKey, string searchEngineId, string query, long? starPage, out long? totalResults)
        {
            if (!dictKeys.ContainsKey(query.ToLower()))
                dictKeys.Add(query.ToLower(), new List<ActiveKeyword>());

            bool isSuccess = false;
            totalResults = 0;
            CustomsearchService customSearchService = new CustomsearchService(new Google.Apis.Services.BaseClientService.Initializer() { ApiKey = apiKey });
            CseResource.ListRequest listRequest = customSearchService.Cse.List(query);
            listRequest.Cx = searchEngineId;
            listRequest.Start = starPage;
            Search search = listRequest.Execute();
            if (search != null)
            {
                string formattedTotalResults = search.SearchInformation.FormattedTotalResults;
                totalResults = search.SearchInformation.TotalResults;
                foreach (var item in search.Items)
                {
                    ActiveKeyword activeKey = new ActiveKeyword();
                    activeKey.Keyword = query;
                    activeKey.Url = item.Link;
                    activeKey.TotalResult = formattedTotalResults;
                    activeKey.Title = item.Title;
                    activeKey.Status = "Activated";
                    activeKeywords.Add(activeKey);
                    if (dictKeys.ContainsKey(query.ToLower()))
                        dictKeys[query.ToLower()].Add(activeKey);
                }

            }
            else
            {
                WriteLog("No result");
            }
            isSuccess = true;
            return isSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void WriteLog(string message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\GCS.txt", true);
                sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": " + message);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
