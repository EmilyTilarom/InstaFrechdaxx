using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InstaFrechdaxx
{
    class Stealing
    {
        public string name { get; set; }
        public string lastPic { get; set; }

        public Stealing()
        {
            this.name = "";
            this.lastPic = "";
        }

        public Stealing(string jsonString)
        {
            JsonConvert.DeserializeObject<Stealing>(jsonString);
        }

        public static void saveStealingList(List<Stealing> list, string path)
        {
            string jsonString = JsonConvert.SerializeObject(list);
            Program.WriteStringToFile(path, jsonString);
        }
    }
}
