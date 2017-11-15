using Lerp2Web;
using Newtonsoft.Json;
using System.IO;

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
}