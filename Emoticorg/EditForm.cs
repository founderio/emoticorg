using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emoticorg
{
    public partial class EditForm : Form
    {
        public EditForm()
        {
            InitializeComponent();
            pictureBox1.AllowDrop = true;
        }

        private void EditForm_Load(object sender, EventArgs e)
        {

        }

        private Emoticon emoticon;

        public Emoticon ShowNew(Form parent)
        {
            emoticon = new Emoticon();
            this.Text = "New Emoticon";
            return ShowInternal(parent);
        }

        public void ShowEdit(Form parent, Emoticon emoticon)
        {
            this.emoticon = emoticon;
            this.Text = "Edit Emoticon";
            ShowInternal(parent);
        }

        private Emoticon ShowInternal(Form parent)
        {
            tbName.Text = emoticon.name;
            tbCategory.Text = emoticon.category;
            if (emoticon.data != null && emoticon.data.Length > 0)
            {
                if (emoticon.type == Emoticon.TYPE_ASCII)
                {
                    TextData = UnicodeEncoding.UTF8.GetString(emoticon.data);
                }
                else if (emoticon.type == Emoticon.TYPE_IMAGE)
                {
                    SetImageFromBytes(emoticon.data);
                }
                else
                {
                    TextData = "";
                    ImageData = null;
                    UnlockType();
                }
            }
            else
            {
                TextData = "";
                ImageData = null;
                UnlockType();
            }
            if (emoticon.guid != null)
            {
                lblGuid.Text = emoticon.guid;
            }
            else
            {
                lblGuid.Text = "N/A";
            }
            if (emoticon.lastUsed > 0)
            {
                lblLastUsed.Text = new DateTime(emoticon.lastUsed).ToString();
            }
            else
            {
                lblLastUsed.Text = "N/A";
            }
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            DialogResult result = this.ShowDialog(parent);
            if (result == DialogResult.OK)
            {
                Store();
                return emoticon;
            }
            else
            {
                return null;
            }
        }

        private void Store()
        {
            emoticon.name = tbName.Text;
            emoticon.category = tbCategory.Text;
            emoticon.type = lockedType;
            if (emoticon.type == Emoticon.TYPE_ASCII)
            {
                emoticon.data = UnicodeEncoding.UTF8.GetBytes(TextData);
            }
            else if (emoticon.type == Emoticon.TYPE_IMAGE)
            {
                Image img = ImageData;
                if (img == null)
                {
                    emoticon.data = null;
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream()) {
                        try
                        {
                            ImageFormat imgRawFormat = img.RawFormat;
                            if (imgRawFormat == null)
                            {
                                img.Save(ms, ImageFormat.Png);
                            }
                            else
                            {
                                img.Save(ms, img.RawFormat);
                            }
                            emoticon.data = ms.ToArray();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Saving the image failed: " + e.Message, "Error saving the image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                SetImageFromBytes(emoticon.data);
            }
        }

        bool backupUsed;
        Image backup;
        int lockedType = -1;

        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
            string[] formats = e.Data.GetFormats();
            Console.WriteLine("Formats:");
            foreach (String format in formats)
            {
                Console.WriteLine(format);
            }
            foreach (String format in e.Data.GetFormats(true))
            {
                if (!formats.Contains(format))
                {
                    Console.WriteLine("Convertable:" + format);
                }
            }
            Console.WriteLine("Formats End.");
            if (!backupUsed)
            {
                backup = pictureBox1.Image;
                backupUsed = true;
                pictureBox1.Image = Properties.Resources.file_symlink_file_64_gray;
            }
        }

        private void resetPicture()
        {
            if (backupUsed)
            {
                pictureBox1.Image = backup;
                backupUsed = false;
                backup = null;
            }
        }

        private void pictureBox1_DragLeave(object sender, EventArgs e)
        {
            resetPicture();
        }

        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {

                resetPicture();

                IDataObject data = e.Data;

                if (data.GetDataPresent(typeof(Image)))
                {
                    Image image = (Image)data.GetData(typeof(Image));
                    ImageData = image;
                    return;
                }

                string[] formats = data.GetFormats();
                if (formats.Contains("text/html"))
                {

                    var obj = data.GetData("text/html");
                    string html = string.Empty;
                    if (obj is string)
                    {
                        html = (string)obj;
                    }
                    else if (obj is MemoryStream)
                    {
                        MemoryStream ms = (MemoryStream)obj;
                        byte[] buffer = new byte[ms.Length];
                        ms.Read(buffer, 0, (int)ms.Length);
                        ms.Dispose();
                        if (buffer[1] == (byte)0)  // Detecting unicode
                        {
                            html = System.Text.Encoding.Unicode.GetString(buffer);
                        }
                        else
                        {
                            html = System.Text.Encoding.ASCII.GetString(buffer);
                        }
                    }
                    // Using a regex to parse HTML, I know, bad boy.
                    // Go fix it right now if it bothers you!
                    var match = new Regex(@"<img[^/]src=""([^""]*)""").Match(html);
                    if (match.Success)
                    {
                        Uri uri = new Uri(match.Groups[1].Value);
                        SetImageFromUri(uri);
                        return;
                    }
                }

                if (formats.Contains("FileDrop"))
                {
                    var filePaths = (string[])data.GetData("FileDrop");
                    using (FileStream fs = File.OpenRead(filePaths[0]))
                    {
                        SetImageFromStream(fs);
                        return;
                    }
                }
                //TODO: Move loading stuff into background thread
            }
            catch
            {
                Console.WriteLine("ERR");
            }
        }

        private void SetImageFromUri(Uri uri)
        {
            string fileName = System.IO.Path.GetTempFileName();
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile(uri, fileName);
            }
            using (FileStream fs = File.OpenRead(fileName))
            {
                SetImageFromStream(fs);
            }
            File.Delete(fileName);
        }

        private void SetImageFromStream(Stream fs)
        {
            memImage.initWithStream(fs);
            ImageData = memImage.Image;
        }

        private void SetImageFromBytes(byte[] buffer)
        {
            memImage.initWithData(buffer);
            ImageData = memImage.Image;
        }

        private MemoryImage memImage = new MemoryImage();

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void LockType(int type)
        {
            splitContainer1.Panel1Collapsed = type != Emoticon.TYPE_ASCII;
            splitContainer1.Panel2Collapsed = type != Emoticon.TYPE_IMAGE;
            lockedType = type;
        }

        private void UnlockType()
        {
            lockedType = -1;
            splitContainer1.Panel1Collapsed = false;
            splitContainer1.Panel2Collapsed = false;
        }

        public string TextData
        {
            get
            {
                return textBox1.Text;
            }
            set
            {
                LockType(Emoticon.TYPE_ASCII);
                textBox1.Text = value;
            }
        }

        public Image ImageData
        {
            get
            {
                if (backupUsed)
                {
                    return backup;
                }
                else
                {
                    return pictureBox1.Image;
                }
            }
            set
            {
                LockType(Emoticon.TYPE_IMAGE);
                if (value != null)
                {
                    Guid[] frameDimensions = value.FrameDimensionsList;
                    foreach (Guid guid in frameDimensions)
                    {
                        FrameDimension dim = new FrameDimension(guid);
                        int count = value.GetFrameCount(dim);
                           
                        for (int idx = 0; idx < count; idx++)
                        {
                            // save each frame to a bytestream
                            value.SelectActiveFrame(dim, idx);
                        }
                        value.SelectActiveFrame(dim, 0);
                    }
                    
                }
                if (backupUsed)
                {
                    backup = value;
                }
                else
                {
                    pictureBox1.Image = value;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            LockType(Emoticon.TYPE_ASCII);
        }

        private void tsbCopyImage_Click(object sender, EventArgs e)
        {
            if (ImageData != null)
            {
                Clipboard.SetImage(ImageData);
            }
        }

        private void tsbPasteImage_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                ImageData = Clipboard.GetImage();
            }
        }

        private void tsbCopyText_Click(object sender, EventArgs e)
        {
            if (TextData != null)
            {
                Clipboard.SetText(TextData);
            }
        }

        private void tsbPasteText_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                TextData = Clipboard.GetText();
            }
        }
    }
}
