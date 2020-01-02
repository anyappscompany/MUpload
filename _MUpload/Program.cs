using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;

using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;
using Google.GData.Client;

namespace _MUpload
{
    class Program
    {
        //очередь адресов для закачки
        static Queue<string> videos = new Queue<string>();
        //список скачанных страниц
        static List<string> HTMLs = new List<string>();
        //локер для очереди адресов
        static object URLlocker = new object();
        //локер для списка скачанных страниц
        static object HTMLlocker = new object();
        //очередь ошибок
        static Queue<Exception> exceptions = new Queue<Exception>();
        public static string api;
        public static string user;
        public static string passwa;
        static void Main(string[] args)
        {
            Console.WriteLine("User:");
            user = Console.ReadLine();
            Console.WriteLine("ApiKey:");
            api = Console.ReadLine();
            Console.WriteLine("Pass:");
            passwa = Console.ReadLine();

            Console.Title = user;
            var reader = new StreamReader(File.OpenRead(@"upload.csv"));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine(); 
                videos.Enqueue(line);
            }

            //создаем массив хендлеров, для контроля завершения потоков
            ManualResetEvent[] handles = new ManualResetEvent[5];
            //создаем и запускаем 3 потока
            for (int i = 0; i < 5; i++)
            {
                handles[i] = new ManualResetEvent(false);
                (new Thread(new ParameterizedThreadStart(Download))).Start(handles[i]);
            }
            //ожидаем, пока все потоки отработают
            WaitHandle.WaitAll(handles);

            Console.WriteLine("Загрузка закончена");
            Console.ReadKey();
        }
        public static void Download(object handle)
        {
        
            //будем крутить цикл, пока не закончатся ULR в очереди
            while (true)
            {
            
                try
                {

                    string URL;
                    string title;
                    string desc;
                    string cat;
                    string kw;
                    //блокируем очередь URL и достаем оттуда один адрес
                    lock (URLlocker)
                    {
                        if (videos.Count == 0)
                        {
                            break;//адресов больше нет, выходим из метода, завершаем поток
                        }
                        else
                        {
                            string linon = videos.Dequeue();
                            var values = linon.Split(new string[] { "05452" }, StringSplitOptions.None);
                            URL = AppDomain.CurrentDomain.BaseDirectory + values[5];
                            //Console.WriteLine(URL);
                            //Console.ReadKey();
                            title = values[0];
                            desc = values[1];
                            cat = values[3];
                            kw = values[2];

                        }
                    }
                    //Console.WriteLine(URL + " - start downloading ...");
                    UploadVideo(user, api, URL, title, title, cat, false, kw);

                    //устанавливаем флажок хендла, что бы сообщить главному потоку о том, что мы отработали
                    ((ManualResetEvent)handle).Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    
                }
            }
        }
        public static void UploadVideo(string usernam, string apikey, string FilePath, string Title, string Description, string Category, bool privatevid, string Keywords)
        {
            //Console.WriteLine("Начало загрузки видео - " + Title);
            Console.WriteLine("Загрузка " + Title);
            YouTubeRequestSettings settings;
            YouTubeRequest request;
            string devkey = apikey;
            string username = usernam;
            string password = passwa;
            settings = new YouTubeRequestSettings(usernam, devkey, username, password) { Timeout = -1 }; 
            request = new YouTubeRequest(settings);
            ((GDataRequestFactory)request.Service.RequestFactory).Timeout = 9999999;
            ((GDataRequestFactory)request.Service.RequestFactory).KeepAlive = false;
            Video newVideo = new Video();


            newVideo.Title = Title;
            if (Title.Length > 100)
            {
                newVideo.Title = Title.Substring(0, 99);
            }
            newVideo.Tags.Add(new MediaCategory(Category, YouTubeNameTable.CategorySchema));
            newVideo.Keywords = Keywords.Replace("\"", "");
            newVideo.Description = Description;
            newVideo.YouTubeEntry.Private = privatevid;
            //newVideo.Tags.Add(new MediaCategory("mydevtag, anotherdevtag",
            //YouTubeNameTable.DeveloperTagSchema));

            //newVideo.YouTubeEntry.Location = new GeoRssWhere(37, -122);
            // alternatively, you could just specify a descriptive string
            // newVideo.YouTubeEntry.setYouTubeExtension("location", "Mountain View, CA");

            labs1:
            try
            {

                newVideo.YouTubeEntry.MediaSource = new MediaFileSource(FilePath, "video/wmv"); //video/wmv video/mp4
                
                Video createdVideo = request.Upload(newVideo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("*************************************");
                Console.WriteLine("***" + ex.Message + " - " + Title);
                Console.WriteLine("*************************************");
                Console.WriteLine("");
                if(ex.Message == "The operation has timed out")
                {
                    Console.WriteLine("Повторная загрузка " + Title);
                    goto labs1;
                }
                //
            }

            //Console.WriteLine("Конец загрузки видео - " + Title);
            //newVideo.Tags.Add(new MediaCategory("Autos", YouTubeNameTable.CategorySchema));

            //newVideo.Tags.Add(new MediaCategory("mydevtag, anotherdevtag", YouTubeNameTable.DeveloperTagSchema));

            //newVideo.YouTubeEntry.setYouTubeExtension("location", "Paris, FR");
            // You can also specify just a descriptive string ==>
            // newVideo.YouTubeEntry.Location = new GeoRssWhere(71, -111);
            // newVideo.YouTubeEntry.setYouTubeExtension("location", "Paris, France.");


        }
    }
}
