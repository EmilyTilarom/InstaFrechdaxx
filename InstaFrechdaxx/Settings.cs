using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace InstaFrechdaxx
{
    class Settings
    {

        public bool CommentPictures { get; set; }
        public bool LikePictures { get; set; }
        public bool FollowAndUnfollow { get; set; }
        public bool StealPics { get; set; }

        public Settings()
        {
            this.CommentPictures = false;
            this.LikePictures = false;
            this.FollowAndUnfollow = false;
            this.StealPics = false;
        }

        public bool SaveSettings(string path)
        {
            Settings a = this;
            string jsonString = JsonConvert.SerializeObject(a);
            if (Program.WriteStringToFile(path, jsonString))
                return true;

            return false;
        }

        public static Settings LoadSettings(string path, string filename)
        {
            if (Program.doesFolderExist(path) && Program.doesFileExist(path, filename))
            {
                string jsonString = Program.ReadStringFromFile(Path.Combine(path, filename));
                //return new Settings(jsonString);
                return JsonConvert.DeserializeObject<Settings>(jsonString);
            }
            else
            {
                Settings neww = new Settings();
                neww.SaveSettings(Path.Combine(path, filename));
                return neww;
            }
        }
    }
}
