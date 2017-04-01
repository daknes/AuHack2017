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

namespace imagecreaterApi.Controllers
{
    public class ValuesController : ApiController
    {
        public string Get()
        {
            return "https://s3.eu-central-1.amazonaws.com/auhackimages/17742444_10211086890479658_1867142806.jpg";
        }

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
                var newstream = GenerateImage(stream);
                url = saveToSThree(newstream, filename);
            }
            return url;

        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        private List<string> DefindeNoOfLines(string meme)
        {
            List<string> memlines = new List<string>();
            var Maxlength = 37;
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

                    if ((line.Length + item.Length + 1) < 37)
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

        private Stream GenerateImage(Stream stream)
        {
            var firstlinememe = "When you send nudes to your online gf";
            var secoundlineMeme = "And your uncle phone rings";
            var test = firstlinememe + " " + secoundlineMeme + " " + secoundlineMeme;

            var memelines = DefindeNoOfLines(test);

            System.Drawing.Image bitmap = (System.Drawing.Image)Bitmap.FromStream(stream);
            bitmap = RotateImage(bitmap, 90);
            bitmap =  ScaleImage(bitmap, 460, 720);

            Graphics graphicsImage = Graphics.FromImage(bitmap);
            StringFormat stringformat = new StringFormat();
            stringformat.Alignment = StringAlignment.Near;
            stringformat.LineAlignment = StringAlignment.Near;
            Color StringColor = System.Drawing.ColorTranslator.FromHtml("#ffffff");
            Font f = new Font("Impact", 15, FontStyle.Bold, GraphicsUnit.Pixel);
            Pen p = new Pen(ColorTranslator.FromHtml("#000000"), 8);
            p.LineJoin = LineJoin.Round; //prevent "spikes" at the path

            Rectangle fr = new Rectangle(0, bitmap.Height - f.Height, bitmap.Width, f.Height);
            LinearGradientBrush b = new LinearGradientBrush(fr, ColorTranslator.FromHtml("#ffffff"), ColorTranslator.FromHtml("#ffffff"), 90);

            for (int i = 0; i < memelines.Count; i++)
            {
                GraphicsPath gp = new GraphicsPath();
                Rectangle r = new Rectangle(25, 50 + (20 * i), bitmap.Width, bitmap.Height);

                gp.AddString(memelines[i], f.FontFamily, (int)f.Style, 25, r, stringformat);

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


        private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
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


        public static Image RotateImage(Image img, float rotationAngle)
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
