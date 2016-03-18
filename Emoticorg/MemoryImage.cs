using System;
using System.Drawing;
using System.IO;

namespace Emoticorg
{
	/// <summary>
	/// Wrapper class for images that holds the related date in a <see cref="System.IO.MemoryStream"/> as creating an image via
	/// <see cref="System.Drawing.Image.FromStream"/> requires the source stream to be left open until the image is disposed.
	/// </summary>
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

		/// <summary>
		/// Releases all resource used by the <see cref="Emoticorg.MemoryImage"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Emoticorg.MemoryImage"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Emoticorg.MemoryImage"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="Emoticorg.MemoryImage"/> so the garbage
		/// collector can reclaim the memory that the <see cref="Emoticorg.MemoryImage"/> was occupying.
		/// Alternatively, re-initiate the data using one of the initWith* methods. This is useful when buffering data.
		/// </remarks>
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
