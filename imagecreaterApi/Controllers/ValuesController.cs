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

namespace imagecreaterApi.Controllers
{
    public class ValuesController : ApiController
    {
        // POST api/values
        public async Task<string> Post()
        {

            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            var url = "";
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.Contents)
            {
                var filename = Guid.NewGuid().ToString() +".png";
                var stream = await file.ReadAsStreamAsync();

                var mem = new MemoryStream(await file.ReadAsByteArrayAsync());
                var emo = getemotion(mem);
                var labels = GetLabels(mem);

                var newstream = GenerateImage(stream);
                url = saveToSThree(newstream, filename);
            }
            return url;

        }

        private List<string> DefindeNoOfLines(string meme)
        {
            List<string> memlines = new List<string>();
            var Maxlength = 28;
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
                    emos.Add(item.Type, item.Confidence);
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


        private Stream GenerateImage(Stream stream)
        {
            var firstlinememe = "When you send nudes to your online gf";
            var secoundlineMeme = "And your uncle phone rings";
            var test = firstlinememe + " " + secoundlineMeme + " " + secoundlineMeme;

            var memelines = DefindeNoOfLines(test);

            System.Drawing.Image bitmap = (System.Drawing.Image)Bitmap.FromStream(stream);
           // bitmap =  ScaleImage(bitmap, 360, 640);
           // bitmap = RotateImage(bitmap, 90);

            Graphics graphicsImage = Graphics.FromImage(bitmap);
            StringFormat stringformat = new StringFormat();
            stringformat.Alignment = StringAlignment.Near;
            stringformat.LineAlignment = StringAlignment.Near;
            Color StringColor = System.Drawing.ColorTranslator.FromHtml("#ffffff");
            Font f = new Font("Impact", 60, FontStyle.Bold, GraphicsUnit.Pixel);
            Pen p = new Pen(ColorTranslator.FromHtml("#000000"), 8);
            p.LineJoin = LineJoin.Round; //prevent "spikes" at the path

            Rectangle fr = new Rectangle(0, bitmap.Height - f.Height, bitmap.Width, f.Height);
            LinearGradientBrush b = new LinearGradientBrush(fr, ColorTranslator.FromHtml("#ffffff"), ColorTranslator.FromHtml("#ffffff"), 180);

            for (int i = 0; i < memelines.Count; i++)
            {
                GraphicsPath gp = new GraphicsPath();
                Rectangle r = new Rectangle(15, 10 + (63 * i), bitmap.Width, bitmap.Height);
                //Rectangle r = new Rectangle(0, (43 * i), bitmap.Width, bitmap.Height);

                gp.AddString(memelines[i], f.FontFamily, (int)f.Style, 60, r, stringformat);

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


        private static System.Drawing.Image ScaleImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }


        public static System.Drawing.Image RotateImage(System.Drawing.Image img, float rotationAngle)
        {
            //create an empty Bitmap image
            Bitmap bmp = new Bitmap(img.Width, img.Height);

            //turn the Bitmap into a Graphics object
            Graphics gfx = Graphics.FromImage(bmp);

            //now we set the rotation point to the center of our image
            gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);

            //now rotate the image
            gfx.RotateTransform(rotationAngle);

            gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);

            //set the InterpolationMode to HighQualityBicubic so to ensure a high
            //quality image once it is transformed to the specified size
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //now draw our new image onto the graphics object
            gfx.DrawImage(img, new Point(0, 0));

            //dispose of our Graphics object
            gfx.Dispose();

            //return the image
            return bmp;
        }

        private string saveToSThree(Stream image, string imageName)
        {
            //TransferUtility utility = new TransferUtility("", "");

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

    }
}
