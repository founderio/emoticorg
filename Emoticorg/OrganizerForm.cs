using GitHubUpdate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emoticorg
{
    public partial class OrganizerForm : Form
    {
        private EmoticonStore store;
        private UpdateChecker checker;

        private string filter = "";
        private string query;
        private int count;
        private int cacheOffset;
        private List<Emoticon> cache;
        private Brush fontBrush = Brushes.Black;
        private EditForm editForm;

        public OrganizerForm()
        {
            cache = new List<Emoticon>();
            InitializeComponent();
            editForm = new EditForm();
            fontBrush = new SolidBrush(listView1.ForeColor);
        }

        private void OrganizerForm_Load(object sender, EventArgs e)
        {
            checker = new UpdateChecker("founderio", "emoticorg");
            checker.CheckUpdate(UpdateType.Major).ContinueWith(continuation =>
            {
                Invoke(new Action(() =>
                {
                    if (continuation.Result != UpdateType.None)
                    {
                        var result = new UpdateNotifyDialog(checker).ShowDialog();
                        if (result == DialogResult.Yes)
                        {
                            checker.DownloadAsset("Emoticorg.zip");
                        }
                    }
                }));
            });

            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataDir = Path.Combine(appdata, "Emoticorg");
            try
            {
                Directory.CreateDirectory(dataDir);
                store = EmoticonStore.openStore(Path.Combine(dataDir, "store.sqlite"));
                //TODO: Handle store upgrade in a later version if required.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading database:\n" + ex.ToString(), "Error Loading Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void OrganizerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            store.Close();
            fontBrush.Dispose();
            editForm.Dispose();
        }

        private void tbClear_Click(object sender, EventArgs e)
        {
            tbSearch.Clear();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateQuery();
        }

        private void treeView1_Enter(object sender, EventArgs e)
        {
            listView1.Focus();
        }

        private void tbSearch_TextChanged(object sender, EventArgs e)
        {
            this.filter = tbSearch.Text;
            UpdateQuery();
        }

        private void UpdateQuery()
        {
            string special;
            string category;
            if (treeView1.SelectedNode == null)
            {
                special = "All";
                category = null;
            }
            else
            {
                special = treeView1.SelectedNode.Name;
                category = (string)treeView1.SelectedNode.Tag;
            }
            string query;
            bool useFilter = filter != null && filter.Length > 0;
            string filterString = "name LIKE @filter ";

            if (special == "Recent")
            {
                query = "WHERE ";
                if (useFilter)
                {
                    query += filterString + "AND ";
                }
                // 96 hours (for now)
                query += DateTime.Now.Ticks + " - lastUsed < 345600000000 ORDER BY lastUsed DESC";
            }
            else if (special == "All")
            {
                if (useFilter)
                {
                    query = "WHERE " + filterString + " ORDER BY name ASC";
                }
                else
                {
                    query = "ORDER BY name ASC";
                }
            }
            else
            {
                query = "WHERE ";
                if (useFilter)
                {
                    query += filterString + "AND ";
                }
                // 96 hours (for now)
                query += "category = '" + category + "' ORDER BY name ASC";
            }
            PopulateView(query);
        }


        private void PopulateView(string query)
        {
            this.query = query;
            bool useFilter = filter != null && filter.Length > 0;
            if (useFilter)
            {
                this.count = store.CountQueryEmoticons(query, filter);
            }
            else
            {
                this.count = store.CountQueryEmoticons(query, null);
            }
            
            this.cache.Clear();
            this.cacheOffset = 0;
            listView1.VirtualListSize = count;
            listView1.Refresh();
        }

        private void LoadCache(int offset, int count)
        {
            this.cacheOffset = offset;
            bool useFilter = filter != null && filter.Length > 0;
            if (useFilter)
            {
                cache = store.PartialQueryEmoticons(query, filter, offset, count);
            }
            else
            {
                cache = store.PartialQueryEmoticons(query, null, offset, count);
            }
        }

        private Emoticon RetrieveEmoticon(int index)
        {
            if (index < cacheOffset || index >= cacheOffset + cache.Count)
            {
                LoadCache(index, 50);
            }
            if (index >= cacheOffset + cache.Count)
            {
                return null;
            }
            else
            {
                return cache[index - cacheOffset];
            }
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            Emoticon emoticon = RetrieveEmoticon(e.ItemIndex);
            
            ListViewItem item = new ListViewItem();
            if (emoticon == null)
            {
                item.Text = "N/A";
            }
            else
            {
                item.Text = emoticon.name;
                item.ImageIndex = 1;
            }
            e.Item = item;
        }


        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Emoticon emoticon = RetrieveEmoticon(e.ItemIndex);
            if (emoticon != null)
            {
                if (emoticon.type == Emoticon.TYPE_ASCII)
                {
                    string text = UnicodeEncoding.UTF8.GetString(emoticon.data);
                    e.Graphics.DrawString(text, listView1.Font, fontBrush, this.listView1.GetItemRect(e.ItemIndex, ItemBoundsPortion.Icon));
                }
                else if (emoticon.type == Emoticon.TYPE_IMAGE)
                {
                    MemoryImage img = new MemoryImage(emoticon.data);
                    Clipboard.SetImage(img.Image);
                    e.Graphics.DrawImage(img.Image, this.listView1.GetItemRect(e.ItemIndex, ItemBoundsPortion.Icon));
                    img.Dispose();
                }
            }
            e.DrawFocusRectangle();
            if (listView1.View != View.Details)
            {
                e.DrawText(TextFormatFlags.Bottom);
                //e.DrawText(TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter);
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                int idx = listView1.SelectedIndices[0];
                Emoticon emot = RetrieveEmoticon(idx);
                editForm.ShowEdit(this, emot);
                store.UpdateEmoticon(emot);
            }
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
            {
                CopySelected();
            }
        }

        private void btnNewEmoticon_Click(object sender, EventArgs e)
        {
            Emoticon emoticon = editForm.ShowNew(this);
            if (emoticon != null)
            {
                store.UpdateEmoticon(emoticon);
                PopulateView(this.query);
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            CopySelected();
        }

        private void CopySelected()
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                int idx = listView1.SelectedIndices[0];
                Emoticon emot = RetrieveEmoticon(idx);
                if (emot.type == Emoticon.TYPE_ASCII)
                {
                    string text = UnicodeEncoding.UTF8.GetString(emot.data);
                    Clipboard.SetText(text);
                }
                else if (emot.type == Emoticon.TYPE_IMAGE)
                {
                    MemoryImage img = new MemoryImage(emot.data);
                    Clipboard.SetImage(img.Image);
                    img.Dispose();
                }
                store.UseEmoticon(emot.guid);
                PopulateView(query);
            }
        }
    }
}
