using System;
using System.Drawing;
using System.IO;

namespace Emoticorg
{
    public class MemoryImage : IDisposable
    {
        private MemoryStream memStream;
        private Image image;

        public MemoryImage() { }

        public MemoryImage(byte[] data)
        {
            initWithData(data);
        }

        public MemoryImage(Stream stream)
        {
            initWithStream(stream);
        }

        public void initWithStream(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            initWithData(buffer);
        }

        public void initWithData(byte[] data)
        {
            Dispose();
            this.memStream = new MemoryStream(data);
            this.image = Image.FromStream(memStream, true, true);
        }

        public void Dispose()
        {
            if (memStream != null)
            {
                memStream.Dispose();
                memStream = null;
            }
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
        }

        public Image Image
        {
            get
            {
                return image;
            }
        }
    }
}
