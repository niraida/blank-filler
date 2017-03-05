using BlankFiller.ViewModels;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace BlankFiller.Pdf
{
    class PdfImageSaver
    {
        public static Task<List<string>> ExtractFile(string fileName, ProgressObject progressObject)
        {
            if (progressObject == null)
                throw new ArgumentNullException(nameof(progressObject));
            return Task.Factory.StartNew(() =>
            {
                var result = new List<string>();                
                var reader = new PdfReader(fileName);
                progressObject.Total = reader.NumberOfPages;
                try
                {
                    for (var i = 1; i <= reader.NumberOfPages; i++)
                    {
                        var pg = reader.GetPageN(i);
                        var res = (PdfDictionary)PdfReader.GetPdfObject(pg.Get(PdfName.RESOURCES));
                        var xobj = (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));

                        foreach (PdfName name in xobj.Keys)
                        {
                            var obj = xobj.Get(name);
                            if (obj.IsIndirect())
                            {
                                var tg = (PdfDictionary)PdfReader.GetPdfObject(obj);

                                var type = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));

                                if (type.Equals(PdfName.IMAGE))
                                {
                                    float width = float.Parse(tg.Get(PdfName.WIDTH).ToString());
                                    float height = float.Parse(tg.Get(PdfName.HEIGHT).ToString());
                                    var matrix = new Matrix(width, height);

                                    var render = ImageRenderInfo.CreateForXObject(matrix, (PRIndirectReference)obj, tg);
                                    var image = render.GetImage();

                                    using (Bitmap dotnetImg = (Bitmap)image.GetDrawingImage())
                                    {
                                        dotnetImg.SetResolution(300f, 300f);

                                        var tempPath = System.IO.Path.GetTempFileName();
                                        dotnetImg.Save(tempPath);
                                        result.Add(tempPath);
                                    }
                                }
                            }
                        }
                        progressObject.Progress = i;
                    }
                    return result;
                }
                finally
                {
                    reader.Close();                    
                }


            });
        }
    }
}
