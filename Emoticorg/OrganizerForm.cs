using GitHubUpdate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
		private List<TreeNode> categoryNodes;

        public OrganizerForm()
        {
            cache = new List<Emoticon>();
            InitializeComponent();
            editForm = new EditForm();
            fontBrush = new SolidBrush(listView1.ForeColor);
			categoryNodes = new List<TreeNode> ();
        }

        private void DisposeStuff()
        {
			if (store != null)
			{
				store.Close();
				store = null;
			}
			fontBrush.Dispose();
			editForm.Dispose();
			categoryNodes.Clear ();
        }

        private void OrganizerForm_Load(object sender, EventArgs e)
        {
            Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            lblVersion.Text = string.Format(lblVersion.Text, appVersion.Major, appVersion.Minor);

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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading database:\n" + ex.ToString(), "Error Loading Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                store = null;
                Application.Exit();
            }

            if (!store.IsReadable)
            {
                MessageBox.Show("This Emoticon store was created by a different application version and cannot be converted.\nSupported Version: " + EmoticonStore.VERSION + " Loaded Version: " + store.LoadedVersionString, "Error Loading Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                store = null;
                Application.Exit();
            }
            else if (store.NeedsUpgrade)
            {
                DialogResult result = MessageBox.Show("This Emoticon store needs be converted before usage.\nWARNING: This may break compatibility with lower application versions!\nTarget Version: " + EmoticonStore.VERSION + " Loaded Version: " + store.LoadedVersionString + "\n\nDo you want to convert?", "Conversion Needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        store.Upgrade();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error upgrading database:\n" + ex.ToString(), "Error Upgrading Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        store = null;
                        Application.Exit();
                    }
                }
            }

            if (treeView1.Nodes.Count > 0)
            {
                treeView1.SelectedNode = treeView1.Nodes[0];
            }
			RefreshCategories ();
        }

        private void OrganizerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
			DisposeStuff ();
        }

        private void tbClear_Click(object sender, EventArgs e)
        {
            tbSearch.Clear();
            tbSearch.Focus();
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
                query += "category = '" + category + "' ORDER BY name ASC";
            }
            PopulateView(query);
        }

		/// <summary>
		/// Populates the view with a new query, clearing the cache & refreshing the listView.
		/// </summary>
		/// <param name="query">Query.</param>
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

		private void RefreshCategories() {
			List<string> categories = store.GetCategories ();
			int diff = categoryNodes.Count - categories.Count;
			for(int i = 0; i < categories.Count; i++) {
				string cat = categories [i];
				TreeNode node;
				if(i == categoryNodes.Count) {
					node = new TreeNode (cat);
					categoryNodes.Add (node);
				} else {
					node = categoryNodes [i];
					node.Text = cat;
				}
				node.Tag = cat;
			}
			if(diff > 0) {
				int first = categoryNodes.Count - diff;
				for(int i = first; i < categoryNodes.Count; i++) {
					treeView1.Nodes.Remove (categoryNodes [i]);
				}
				categoryNodes.RemoveRange (first, diff);
			} else if(diff < 0) {
				int first = categoryNodes.Count + diff;
				treeView1.Nodes.AddRange (categoryNodes.GetRange (first, -diff).ToArray());
			}
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
                Rectangle itemRect = this.listView1.GetItemRect(e.ItemIndex, ItemBoundsPortion.Icon);
                if (emoticon.type == Emoticon.TYPE_ASCII)
                {
                    string text = UnicodeEncoding.UTF8.GetString(emoticon.data);
                    DrawTextScaled(e.Graphics, text, listView1.Font, fontBrush, itemRect, 0.5f, 1.0f);
                }
                else if (emoticon.type == Emoticon.TYPE_IMAGE)
                {
                    MemoryImage img = new MemoryImage(emoticon.data);
                    Clipboard.SetImage(img.Image);
                    DrawImageScaled(e.Graphics, img.Image, itemRect, 0.5f, 1.0f);
                    img.Dispose();
                }
            }
            e.DrawFocusRectangle();
            if (listView1.View != View.Details)
            {
                e.DrawText(TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
        }

        public static Rectangle DrawTextScaled(Graphics g, string txt, Font font, Brush brush, Rectangle targetRect, float centerH, float centerV)
        {
            SizeF textSize = g.MeasureString(txt, font);

            Rectangle drawRect = new Rectangle(targetRect.X, targetRect.Y,
                Math.Min((int)Math.Ceiling(textSize.Width), targetRect.Width), Math.Min((int)Math.Ceiling(textSize.Height), targetRect.Height));
            if (drawRect.Width < targetRect.Width)
            {
                int offset = targetRect.Width - drawRect.Width;
                drawRect.X += (int)(offset * centerH);
            }
            if (drawRect.Height < targetRect.Height)
            {
                int offset = targetRect.Height - drawRect.Height;
                drawRect.Y += (int)(offset * centerV);
            }
            g.DrawString(txt, font, brush, drawRect);
            return drawRect;
        }

        public static Rectangle DrawImageScaled(Graphics g, Image img, Rectangle targetRect, float centerH, float centerV)
        {
            float factorWidth = Math.Min(img.Width, targetRect.Width) / (float)img.Width;
            float factorHeight = Math.Min(img.Height, targetRect.Height) / (float)img.Height;
            float scaleFactor = Math.Min(factorHeight, factorWidth);

            Rectangle drawRect = new Rectangle(targetRect.X, targetRect.Y,
                (int)(img.Width * scaleFactor), (int)(img.Height * scaleFactor));
            if (drawRect.Width < targetRect.Width)
            {
                int offset = targetRect.Width - drawRect.Width;
                drawRect.X += (int)(offset * centerH);
            }
            if (drawRect.Height < targetRect.Height)
            {
                int offset = targetRect.Height - drawRect.Height;
                drawRect.Y += (int)(offset * centerV);
            }
            g.DrawImage(img, drawRect);
            return drawRect;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                int idx = listView1.SelectedIndices[0];
                Emoticon emot = RetrieveEmoticon(idx);
                editForm.ShowEdit(this, emot);
				store.UpdateEmoticon(emot);
				RefreshCategories ();
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
				RefreshCategories ();
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

                // Refresh the view if we are in the Recent Emoticons Tab
                if (treeView1.SelectedNode != null && treeView1.SelectedNode.Name == "Recent")
                {
                    PopulateView(query);
                    // Select the first item, as that is the last used one.
                    listView1.SelectedIndices.Clear();
                    if (count > 0)
                    {
                        listView1.SelectedIndices.Add(0);
                        listView1.FocusedItem = listView1.Items[0];
                        listView1.FocusedItem.EnsureVisible();
                    }
                }
            }
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            tbSearch.Focus();
        }

        private void tbSearch_Enter(object sender, EventArgs e)
        {
            tbSearch.SelectAll();
        }
    }
}
