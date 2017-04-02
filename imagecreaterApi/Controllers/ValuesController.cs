using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Threading.Tasks;
using System.IO;
using Amazon.S3;
using Amazon;
using Amazon.S3.Transfer;
using System.Drawing.Imaging;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using System.Threading;

namespace imagecreaterApi.Controllers
{
    public class ValuesController : ApiController
    {
        private SqlConnection _connection;

        public ValuesController()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            _connection = new SqlConnection("Server=auhack2017.c2hsrbdochzn.eu-central-1.rds.amazonaws.com;Database=#MEMEMAGIC;User Id=admin;Password = auhack2017;");
            _connection.Open();
        }
        // POST api/values
        public async Task<IHttpActionResult> Post()
        {

            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            var url = "";
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            Dictionary<string, float> emo = null;
            Dictionary<string, float> labels = null;
            foreach (var file in provider.Contents)
            {
                var filename = Guid.NewGuid().ToString() + ".png";
                var stream = await file.ReadAsStreamAsync();

                var mem = new MemoryStream(await file.ReadAsByteArrayAsync());
                emo = getemotion(mem);


                string memetext;
                if (emo.Any())
                {
                    memetext = GetMemeText(emo);
                }
                else
                {
                    labels = GetLabels(mem);
                    if (labels.Any())
                    {
                        memetext = GetMemeTextByLabel(labels);
                    }
                    else
                    {
                        return NotFound();
                    }
                }

                var newstream = GenerateImage(stream, memetext);
                url = saveToSThree(newstream, filename);
            }
            return Ok(new
            {
                URL = url,
                Emotion = emo?.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "",
                Tag = labels?.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? ""
            });
        }

        private List<string> DefindeNoOfLines(string meme, int Maxlength)
        {
            List<string> memlines = new List<string>();
            if (meme.Length < Maxlength)
            {
                memlines.Add(meme);
            }
            else
            {
                var list = meme.Split(' ');
                var line = "";
                foreach (var item in list)
                {

                    if ((line.Length + item.Length + 1) < Maxlength)
                    {
                        line += " " + item;
                    }
                    else
                    {
                        memlines.Add(line);
                        Console.WriteLine(line);
                        line = item;
                    }
                }
                if (!memlines.Contains(line))
                {
                    memlines.Add(line);
                }

                Console.WriteLine(line);

            }

            return memlines;
        }


        private Dictionary<string, float> getemotion(MemoryStream image)
        {

            Dictionary<string, float> emos = new Dictionary<string, float>();
            IAmazonRekognition reg = new AmazonRekognitionClient(ConfigurationManager.AppSettings["AWSAccessKey"], ConfigurationManager.AppSettings["AWSSecretKey"], RegionEndpoint.EUWest1);

            var request = new DetectFacesRequest()
            {
                Image = new Amazon.Rekognition.Model.Image { Bytes = image },
                Attributes = new List<string>
                {
                    "ALL"
                }
            };

            var respFace = reg.DetectFaces(request);
            foreach (var detail in respFace.FaceDetails)
            {
                foreach (var item in detail.Emotions)
                {
                    if (!emos.ContainsKey(item.Type))
                    {
                        emos.Add(item.Type, item.Confidence);
                    }
                }
            }
            return emos;
        }

        private Dictionary<string, float> GetLabels(MemoryStream image)
        {
            Dictionary<string, float> labels = new Dictionary<string, float>();

            IAmazonRekognition reg = new AmazonRekognitionClient(ConfigurationManager.AppSettings["AWSAccessKey"], ConfigurationManager.AppSettings["AWSSecretKey"], RegionEndpoint.EUWest1);
            var lbreq = new DetectLabelsRequest()
            {
                Image = new Amazon.Rekognition.Model.Image { Bytes = image },
                MaxLabels = 100,
                MinConfidence = 0
            };

            var tagRepo = reg.DetectLabels(lbreq);
            foreach (var item in tagRepo.Labels)
            {
                labels.Add(item.Name, item.Confidence);
            }
            return labels;
        }


        private Stream GenerateImage(Stream stream, string text)
        {
            System.Drawing.Image bitmap = (System.Drawing.Image)Bitmap.FromStream(stream);

            var memelines = DefindeNoOfLines(text, bitmap.Width - 20);
            var fontSize = (bitmap.Height / 100) * 5;
            Graphics graphicsImage = Graphics.FromImage(bitmap);
            StringFormat stringformat = new StringFormat();
            stringformat.Alignment = StringAlignment.Center;
            stringformat.LineAlignment = StringAlignment.Near;
            Color StringColor = System.Drawing.ColorTranslator.FromHtml("#ffffff");
            Font f = new Font("Impact", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            Pen p = new Pen(ColorTranslator.FromHtml("#000000"), 8);
            p.LineJoin = LineJoin.Round; //prevent "spikes" at the path

            Rectangle fr = new Rectangle(0, bitmap.Height - f.Height, bitmap.Width, f.Height);
            LinearGradientBrush b = new LinearGradientBrush(fr, ColorTranslator.FromHtml("#ffffff"), ColorTranslator.FromHtml("#ffffff"), 180);

            for (int i = 0; i < memelines.Count; i++)
            {
                GraphicsPath gp = new GraphicsPath();
                Rectangle r = new Rectangle(0, 0 + ((fontSize + 3) * i), bitmap.Width, bitmap.Height);

                gp.AddString(memelines[i], f.FontFamily, (int)f.Style, fontSize, r, stringformat);

                graphicsImage.SmoothingMode = SmoothingMode.AntiAlias;
                graphicsImage.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsImage.DrawPath(p, gp);
                graphicsImage.FillPath(b, gp);
            }



            b.Dispose();
            b.Dispose();
            f.Dispose();
            stringformat.Dispose();
            graphicsImage.Dispose();

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        private string GetMemeText(Dictionary<string, float> emotions)
        {
            KeyValuePair<string, float> strongest = emotions.OrderByDescending(x => x.Value).FirstOrDefault();
            string sql = $@"select  * from emotions
                                        where Name = '{strongest.Key}' 
                                        order by ABS(Rating - {strongest.Value})";

            var result = _connection.Query<dynamic>(sql);

            dynamic theMEME = null;
            bool done = false;
            while (!done)
            {
                var theFinalOne = result.OrderBy(x => new Random().Next()).FirstOrDefault();
                string theBestSqlEver = $"select * from memes where id = {theFinalOne.MemeID} and LEN(MemeText) >= 80";
                theMEME = _connection.QueryFirstOrDefault<dynamic>(theBestSqlEver);
                done = theMEME != null;
            }
            var awesomeText = (string)theMEME.MemeText;

            var superAweSomeText = awesomeText.Replace("\r\n", " ");
            return superAweSomeText.Replace("imgflip.com", "").ToUpper();

        }

        private string GetMemeTextByLabel(Dictionary<string, float> labels)
        {
            dynamic theMEME = null;
            bool done = false;
            int i = -1;
            while (!done)
            {
                i++;
                KeyValuePair<string, float> strongest = labels.OrderByDescending(x => x.Value).ElementAt(i);
                string sql = $@"select  * from tags
                                        where Name = '{strongest.Key}' 
                                        order by ABS(Rating - {strongest.Value})";

                var result = _connection.Query<dynamic>(sql);

                if (result == null || result.Count() == 0)
                {
                    continue;
                }

                bool doneFindingMeme = false;
                while (doneFindingMeme)
                {
                    var theFinalOne = result.FirstOrDefault();

                    string theBestSqlEver = $"select * from memes where id = {theFinalOne.MemeID} and LEN(MemeText) >= 80";
                    theMEME = _connection.QueryFirstOrDefault<dynamic>(theBestSqlEver);

                }
                done = doneFindingMeme = theMEME != null;
            }
            var awesomeText = (string)theMEME.MemeText;

            var superAweSomeText = awesomeText.Replace("\r\n", " ");
            return superAweSomeText.Replace("imgflip.com", "").ToUpper();

        }


        private string saveToSThree(Stream image, string imageName)
        {
            IAmazonS3 client;
            client = new AmazonS3Client(Amazon.RegionEndpoint.EUCentral1);
            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = "auhackimages",
                Key = imageName,
                InputStream = image,
                CannedACL = S3CannedACL.PublicReadWrite
            };
            PutObjectResponse response2 = client.PutObject(request);
            return "https://s3.eu-central-1.amazonaws.com/auhackimages/" + imageName;
        }

        protected override void Dispose(bool disposing)
        {
            _connection.Dispose();
            base.Dispose(disposing);
        }

    }
}
