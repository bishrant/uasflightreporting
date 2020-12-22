using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Survey123EmailNotification.Helpers
{
    public class ImageUtils
    {
        MainDocumentPart m_mainDocPart;
        WordprocessingDocument m_wordProcessingDocument;
        byte[] m_imageInBytes;

        public ImageUtils(string finalDocPath)
        {
            m_wordProcessingDocument = WordprocessingDocument.Open(finalDocPath, true);
            m_mainDocPart = m_wordProcessingDocument.MainDocumentPart;
        }


        public void replaceOriginalImages(Dictionary<string, string> imagesToChange)
        {

            foreach (string key in imagesToChange.Keys)
            {
                string newImagePath = key;
                string imageID = imagesToChange[newImagePath];

                // go through the document and pull out the inline image elements
                IEnumerable<Drawing> imageElements = from run in m_mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>()
                                                     where run.Descendants<Drawing>().First() != null
                                                     select run.Descendants<Drawing>().First();

                ImagePart imagePart = (ImagePart)m_mainDocPart.GetPartById(imageID);
                m_imageInBytes = File.ReadAllBytes(newImagePath);
                BinaryWriter writer = new BinaryWriter(imagePart.GetStream());
                writer.Write(m_imageInBytes);
                writer.Close();
            }


            m_wordProcessingDocument.Close();
        }

        public void replaceOriginalImage(string newImagePath, string imageID)
        {
            // go through the document and pull out the inline image elements
            IEnumerable<Drawing> imageElements = from run in m_mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>()
                                                 where run.Descendants<Drawing>().First() != null
                                                 select run.Descendants<Drawing>().First();

            ImagePart imagePart = (ImagePart)m_mainDocPart.GetPartById(imageID);
            m_imageInBytes = File.ReadAllBytes(newImagePath);
            BinaryWriter writer = new BinaryWriter(imagePart.GetStream());
            writer.Write(m_imageInBytes);
            writer.Close();

            m_wordProcessingDocument.Close();
        }
    }
}
