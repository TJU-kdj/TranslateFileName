using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Data;
using RestSharp;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Threading;

namespace TranslateFileName
{
    class Program
    {
        static readonly string from = "zh";
        static readonly string to = "en";
        static readonly string appid = "20201213000646427";
        static readonly string salt = "1435660288";
        static readonly string key = "_pnU6TpaBpK9NMBIALJ3";
        static string q = "";
        static List<string> fileLocation = new List<string>();
        static List<string> folderLocation = new List<string>();
        static string folderName = "";
        [STAThread]
        static void Main(string[] args)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderBrowser.SelectedPath;
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                GetAllFilesName(directoryInfo);
                if(q!="")AnalysisFiles();
                Thread.Sleep(1000);
                GetAllFoldersName(directoryInfo);
                if(folderName!="") AnalysisFolders();
            }
            Console.WriteLine("Finsh");
            Console.ReadLine();
        }

        static void GetAllFoldersName(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Name == "TypeSort" || directoryInfo.Name == "TimeSort" || directoryInfo.Name == "0_Sort") return;
            try
            {
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    GetAllFoldersName(directory);
                }
                if(directoryInfo.Name.Contains(" "))
                {
                    directoryInfo.MoveTo(directoryInfo.Parent.FullName + "/" + directoryInfo.Name.Replace(' ', '_'));
                }
                if (IsHanZi(directoryInfo.Name))
                {
                    folderLocation.Add(directoryInfo.FullName);
                    folderName += (directoryInfo.Name+"\n");
                }
                
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void AnalysisFiles()
        {
            try
            {
                string file_translate = GetResult(q);
                string[] fileNewName = file_translate.Split(';');
                string[] fileLocation_array = fileLocation.ToArray();
                if (fileNewName.Length == fileLocation.Count)
                {
                    for (int i = 0; i < fileLocation.Count; i++)
                    {
                        FileInfo file = new FileInfo(fileLocation_array[i]);
                        file.MoveTo(file.DirectoryName + "/" + fileNewName[i].Replace(' ','_'));
                    }
                }
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void AnalysisFolders()
        {
            try
            {
                string folder_translate = GetResult(folderName);
                string[] folderNewName = folder_translate.Split(';');
                string[] folderLocation_array = folderLocation.ToArray();
                if (folderNewName.Length == folderLocation.Count)
                {
                    for (int i = 0; i < folderLocation.Count; i++)
                    {
                        DirectoryInfo directory = new DirectoryInfo(folderLocation_array[i]);
                        directory.MoveTo(directory.Parent.FullName + "/" + folderNewName[i].Replace(' ', '_'));
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void GetAllFilesName(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Name == "TypeSort" || directoryInfo.Name == "TimeSort" || directoryInfo.Name == "0_Sort") return;
            try
            {
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                foreach(DirectoryInfo directory in directories)
                {
                    GetAllFilesName(directory);
                }
                FileInfo[] files = directoryInfo.GetFiles();
                foreach(FileInfo file in files)
                {
                    if(file.Name.Contains(" "))
                    {
                        file.MoveTo(file.DirectoryName + "/" + file.Name.Replace(' ', '_'));
                    }
                    if (IsHanZi(file.Name))
                    {
                        fileLocation.Add(file.FullName);
                        q += (file.Name+"\n");
                    }
                }
            }
            catch(UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static bool IsHanZi(string ch)
        {
            char[] chars = ch.ToCharArray();
            foreach(char character in chars)
            {
                byte[] byte_len = System.Text.Encoding.Default.GetBytes(character.ToString());
                if (byte_len.Length == 2) { return true; }
            }
            return false;
        }

        static string sign(string old)
        {
            return string.Format("{0}{1}{2}{3}", appid, old, salt, key); 
        }

        static string getMd5(string old)
        {
            var md5 = new MD5CryptoServiceProvider();
            var result = Encoding.UTF8.GetBytes(sign(old));
            var output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        static string GetJson(string old)
        {
            var client = new RestClient("http://api.fanyi.baidu.com");
            var request = new RestRequest("/api/trans/vip/translate", Method.GET);
            request.AddParameter("q", old);
            request.AddParameter("from", from);
            request.AddParameter("to", to);
            request.AddParameter("appid", appid);
            request.AddParameter("salt", salt);
            request.AddParameter("sign", getMd5(old));
            IRestResponse response = client.Execute(request);
            return response.Content;
        }

        static string GetResult(string old)
        {
            var lst = new List<string>();
            var content = GetJson(old);
            dynamic json = JsonConvert.DeserializeObject(content);
            foreach (var item in json.trans_result)
            {
                lst.Add(item.dst.ToString());
            }
            return string.Join(";", lst);
        }
    }
}
