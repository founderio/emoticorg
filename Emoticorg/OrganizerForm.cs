﻿using GitHubUpdate;
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

        public OrganizerForm()
        {
            InitializeComponent();
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            tbSearch.Clear();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string query;
            if (e.Node.Name == "Recent")
            {
               // 96 hours (for now)
                query = "WHERE " + DateTime.Now.Ticks + " - lastUsed < 345600000000 ORDER BY lastUsed DESC";
            }
            else if (e.Node.Name == "All")
            {
                query = "ORDER BY name ASC";
            }
            else
            {
                query = "WHERE category = '" + e.Node.Tag + "' ORDER BY name ASC";
            }
            PopulateView(query);
        }

        private string query;
        private int count;
        private int cacheOffset;
        private List<Emoticon> cache = new List<Emoticon>();

        private void PopulateView(string query)
        {
            this.query = query;
            this.count = store.CountQueryEmoticons(query);
            this.cache.Clear();
            this.cacheOffset = 0;
            listView1.VirtualListSize = count;
            listView1.Refresh();
        }

        private void LoadCache(int offset, int count)
        {
            this.cacheOffset = offset;
            cache = store.PartialQueryEmoticons(query, offset, count);
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
               return cache[cacheOffset + index];
            }
        }

        private void listView1_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            
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
            }
            e.Item = item;
        }

        private void listView1_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            
        }


        EditForm editForm = new EditForm();

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Emoticon emoticon = editForm.ShowNew(this);
            if (emoticon != null)
            {
                store.UpdateEmoticon(emoticon);
                PopulateView(this.query);
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
    }
}
