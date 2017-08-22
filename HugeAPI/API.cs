using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace HugeAPI
{
    //This needs to go to the API
    public class FileData
    { //Throw exception if URL is not set (TODO)
        public static string RawUrl,
                             Author = "ikillnukes1",
                             RepoName = "HugeCraft-Client";

        public string Id,
                      CommitId,
                      FileRelPath;

        [JsonIgnore]
        public string FileName
        {
            get
            {
                return Path.GetFileName(FileRelPath);
            }
        }

        public int Size;

        //Github
        public FileData(string url, int siz)
        {
            RawUrl = url;
            Size = siz;
        }

        //Gitlab
        public FileData(string id, string cid, string path, int siz)
        {
            Id = id;
            CommitId = cid;
            FileRelPath = path;
            Size = siz;
        }

        public void SetUrl(string Url)
        {
            RawUrl = Url;
        }
    }

    public class ModpackData
    {
        public string Version;
        public FileData[] Files;
        public int TotalSize;
    }

    public class AppData
    {
        public string Name;
        public ModpackData[] Versions;

        public void AddVersion(ModpackData data)
        {
            ArrayExtensions.Append(ref Versions, data);
        }
    }

    public static class ArrayExtensions
    {
        public static void Append<T>(ref T[] array, T append)
        {
            if (array == null) array = new T[0];
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = append;    // < Adds an extra element to my array
        }
    }

    public static class UriExtensions
    {
        public static bool ValidUrl(this string Url)
        {
            Uri uriResult;
            return Uri.TryCreate(Url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    public static class WebExtensions
    {
        public static string DownloadString(string add)
        { //DownloadString for https
            try
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15");
                        return client.DownloadString(add);
                    }
                    catch
                    {
                        Console.WriteLine("File not found!");
                        return "";
                    }
                }
            }
            catch
            {
                Console.WriteLine("No internet connection!");
                return "";
            }
        }
    }
}