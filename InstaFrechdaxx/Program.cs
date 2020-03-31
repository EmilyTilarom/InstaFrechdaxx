using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstaFrechdaxx
{
    class Program
    {

        #region Hidden
        private static string _username = "XXX";
        private static string _password = "XXX";
        #endregion

        private static IInstaApi _api;

        private static string settingFile = "settings.json";
        private static string followFile = "followFile";
        private static string stealFile = "stealingList.json";
        private static string interactionFile = "latestInteractions.json";
        private static string userDataFile = "userData.json";

        public static string path = Directory.GetCurrentDirectory();
        //public static string path = @"/home/adri/scripts/instagrambot/covid19memes4u";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!!");

            //load Settings
            Settings settings = Settings.LoadSettings(path, settingFile);

            try
            {
                string userdata = ReadStringFromFile(Path.Combine(path, userDataFile));
                var userJson = JObject.Parse(userdata);

                //login
                var loginSuccess = Login(userJson.GetValue("username").ToString(), userJson.GetValue("password").ToString());
                if (!loginSuccess)
                {
                    return;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            //download latest from user and upload on my account
            if (settings.StealPics)
            {
                if (!doesFolderExist(path) || !doesFileExist(path, stealFile))
                {
                    DoFolderAndFileExistPlusCreate(path, stealFile);
                    Console.WriteLine("stealnamesFile could not be found, it was created. pls restart program");
                }
                else
                {
                    string stealnamesjson = ReadStringFromFile(Path.Combine(path, stealFile));
                    var stealnames = JsonConvert.DeserializeObject<List<Stealing>>(stealnamesjson);

                    if (stealnames != null && stealnames.Count > 0)
                    {
                        foreach (var stealname in stealnames)
                        {
                            var posts = GetUserPostsInList(stealname.name, 1);
                            var latestpicid = GetLatestPictureOfUser(stealname.name);
                            if (latestpicid != stealname.lastPic)
                            {
                                string picname = DownloadPicture(latestpicid);
                                if (picname != "")
                                {
                                    string caption = GetCaptionOfPicture(latestpicid);
                                    UploadPicture(picname, caption);
                                    stealname.lastPic = latestpicid;
                                    break;
                                }

                                stealname.lastPic = latestpicid;
                            }
                            else
                            {
                                Console.WriteLine("last pic: " + latestpicid + " already stolen");
                            }
                        }
                    }
                    Stealing.saveStealingList(stealnames, Path.Combine(path, stealFile));
                }
            }


        }

        public static bool Login(string username, string password)
        {

            UserSessionData userSession = new UserSessionData
            {
                UserName = _username,
                Password = _password
            };

            _api = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .Build();

            const string stateFile = "state.bin";
            try
            {
                // load session file if exists
                if (File.Exists(stateFile))
                {
                    Console.WriteLine("Loading state from file");
                    using (var fs = File.OpenRead(stateFile))
                    {
                        //_api.LoadStateDataFromStream(fs);
                        // in .net core or uwp apps don't use LoadStateDataFromStream
                        // use this one:
                        _api.LoadStateDataFromString(new StreamReader(fs).ReadToEnd());
                        // you should pass json string as parameter to this function.
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (!_api.IsUserAuthenticated)
            {
                // login
                Console.WriteLine($"Logging in as {userSession.UserName}");
                var logInResult = _api.LoginAsync();
                if (!logInResult.Result.Succeeded)
                {
                    Console.WriteLine($"Unable to login: {logInResult.Result.Info.Message}");
                    return false;
                }
            }

            /*
            // save session in file
            //var state = _api.GetStateDataAsStream();
            // in .net core or uwp apps don't use GetStateDataAsStream.
            // use this one:
            var state = _api.GetStateDataAsString();
            // this returns you session as json string.
            using (var fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }
            */

            return true;
        }

        public static List<InstaMedia> GetUserPostsInList(string name, int max)
        {
            IResult<InstaMediaList> media = _api.UserProcessor.GetUserMediaAsync(name, PaginationParameters.MaxPagesToLoad(max)).Result;
            Console.Write(media.Info+"\n");
            List<InstaMedia> mediaList = new List<InstaMedia>();
            if (!media.Succeeded)
            {
                Console.WriteLine(" " + "failed");
            }
            else
            {
                mediaList = media.Value.ToList();
            }

            return mediaList;
        }

        public static string GetLatestPictureOfUser(string name)
        {
            //IResult<InstaUser> usersearch = await api.GetUserAsync(username);

            string id = "";
            IResult<InstaMediaList> media = _api.UserProcessor.GetUserMediaAsync(name, PaginationParameters.MaxPagesToLoad(1)).Result;
            //System.Threading.Thread.Sleep(1000);
            Console.Write(media.Info);
            if (!media.Succeeded)
            {
                Console.WriteLine(" " + "failed");
            }
            else
            {
                List<InstaMedia> mediaList = media.Value.ToList();

                InstaMedia firstInstaMedia = mediaList.First<InstaMedia>();
                id = firstInstaMedia.InstaIdentifier;
                Console.WriteLine(id);
            }


            return id;
        }

        public static string DownloadPicture(string id)
        {
            IResult<InstaMedia> pic = _api.MediaProcessor.GetMediaByIdAsync(id).Result;
            //System.Threading.Thread.Sleep(1000);
            InstaMedia m = pic.Value;
            if (m != null && m.MediaType == InstaMediaType.Image && !m.IsMultiPost)
            {
                //download
                string uri = m.Images[0].Uri;

                string imagefolder = Path.Combine(path, "images");
                bool folderExists = false;
                folderExists = doesFolderExist(imagefolder);
                if (!folderExists)
                {
                    folderExists = createFolder(imagefolder);
                }

                if (folderExists)
                {
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            client.DownloadFile(new Uri(uri), Path.Combine(imagefolder, id + ".jpg"));
                            return Path.Combine(imagefolder, id + ".jpg");
                        }
                        catch (Exception e)
                        {
                            //System.Net.WebException
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            return "";
        }

        public static string GetCaptionOfPicture(string id)
        {
            try
            {
                IResult<InstaMedia> media = _api.MediaProcessor.GetMediaByIdAsync(id).Result;
                //System.Threading.Thread.Sleep(1000);
                InstaMedia m = media.Value;

                if (!m.Caption.Text.Contains("@"))
                {
                    return m.Caption.Text + "\n.\nMake sure to follow @covid19memes4u :)\n.\n.\n.\n.\n.\n#coronavirus #covid19 #meme #corona #coronamemes #coronamemes4you #coronamemes4u #covid19memes4u #covid194u #virus #covid19memes #cov #covid_19 #covid19memes4you";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "Tag your friends :D\n\nMake sure to follow @covid19memes4u :)\n.\n.\n.\n.\n.\n#coronavirus #covid19 #meme #corona #coronamemes #coronamemes4you #coronamemes4u #covid19memes4u #covid194u #virus #covid19memes #cov #covid_19 #covid19memes4you";
        }

        public static bool UploadPicture(string path, string caption)
        {
            InstaImageUpload img = new InstaImageUpload()
            {
                Uri = path
            };

            InstaImage img2 = new InstaImage()
            {
                Uri = path
            };

            var iimg = _api.MediaProcessor.UploadPhotoAsync(img, caption).Result;
            var anotherResult = _api.StoryProcessor.UploadStoryPhotoAsync(img2, caption).Result;
            //System.Threading.Thread.Sleep(1000);
            InstaMedia im = iimg.Value;

            File.Delete(path);
            Console.WriteLine("New Image online");
            return true;
        }

        public static bool DoFolderAndFileExistPlusCreate(string path, string filename)
        {
            bool folderExist = false;
            folderExist = doesFolderExist(path);
            if (!folderExist)
            {
                folderExist = createFolder(path);
            }

            if (folderExist)
            {
                //schauen ob datei existiert, wenn nicht: erstellen
                bool fileExists = false;
                fileExists = doesFileExist(path, filename);
                if (!fileExists)
                {
                    fileExists = createFile(path, filename);
                }

                if (fileExists)
                {

                    return true;
                }
                else
                {
                    Console.WriteLine("Could not create Folder");
                }
            }
            return false;
        }

        public static bool doesFolderExist(string path)
        {
            if (Directory.Exists(path))
                return true;
            else
                return false;
        }

        public static bool createFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        public static bool doesFileExist(string path, string name)
        {
            string filepath = Path.Combine(path, name);

            if (File.Exists(filepath))
                return true;
            else
                return false;
        }

        public static bool createFile(string path, string name)
        {
            try
            {
                File.Create(Path.Combine(path, name)).Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        public static List<string> ReadLinesFromFile(string path)
        {
            List<string> ret = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(path);
                ret = lines.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return ret;
        }

        public static string ReadStringFromFile(string pathh)
        {
            string stringg = "";

            try
            {
                stringg = File.ReadAllText(pathh);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return stringg;
        }

        public static bool WriteToFile(string path, System.Collections.Generic.List<string> text)
        {
            StreamWriter sw = new StreamWriter(path);

            try
            {
                foreach (var line in text)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                sw.Close();
            }

            return true;
        }

        public static bool WriteStringToFile(string path, string str)
        {
            StreamWriter sw = new StreamWriter(path);

            try
            {
                sw.WriteLine(str);

                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                sw.Close();
            }

            return true;
        }



    }
}
