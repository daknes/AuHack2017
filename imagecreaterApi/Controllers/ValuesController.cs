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

namespace imagecreaterApi.Controllers
{
    public class ValuesController : ApiController
    {        
        public string Get()
        {            
            return "https://s3.eu-central-1.amazonaws.com/auhackimages/17742444_10211086890479658_1867142806.jpg";
        }

        // POST api/values
        public async Task Post([FromBody]string value)
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.Contents)
            {
                var filename = file.Headers.ContentDisposition.FileName.Trim('\"');
                var buffer = await file.ReadAsByteArrayAsync();                
            }

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

        private void GenerateImage(byte[] image)
        {
            var firstlinememe = "When you send nudes to your online gf";
            var secoundlineMeme = "And your uncle phone rings";
            var test = firstlinememe + " " + secoundlineMeme + " " + secoundlineMeme;

            MemoryStream ms = new MemoryStream(image);

            var memelines = DefindeNoOfLines(test);

            System.Drawing.Image bitmap = (System.Drawing.Image)Bitmap.FromStream(ms);
            Graphics graphicsImage = Graphics.FromImage(bitmap);
            StringFormat stringformat = new StringFormat();
            stringformat.Alignment = StringAlignment.Near;
            stringformat.LineAlignment = StringAlignment.Near;
            Color StringColor = System.Drawing.ColorTranslator.FromHtml("#ffffff");
            Font f = new Font("Impact", 25, FontStyle.Bold, GraphicsUnit.Pixel);
            Pen p = new Pen(ColorTranslator.FromHtml("#000000"), 8);
            p.LineJoin = LineJoin.Round; //prevent "spikes" at the path

            Rectangle fr = new Rectangle(0, bitmap.Height - f.Height, bitmap.Width, f.Height);
            LinearGradientBrush b = new LinearGradientBrush(fr, ColorTranslator.FromHtml("#ffffff"), ColorTranslator.FromHtml("#ffffff"), 90);

            for (int i = 0; i < memelines.Count; i++)
            {
                GraphicsPath gp = new GraphicsPath();
                Rectangle r = new Rectangle(25, 50 + (30 * i), bitmap.Width, bitmap.Height);

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

            bitmap.Save(@"C:\Users\1034553\Desktop\17742444_10211086890479658_1867142806.jpg");
        }

    }
}
