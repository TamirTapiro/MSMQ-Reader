using System;
using System.Linq;
using System.Messaging;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace MSMQ_Reader
{
    public partial class Form1 : Form
    {
        MSMQ_Manager queueManager;
        public Form1()
        {
            InitializeComponent();
            queueManager = new MSMQ_Manager();
            backgroundWorker1.WorkerReportsProgress = true;
            progressBar1.Parent = treeView1;
            treeView1.AfterSelect += new TreeViewEventHandler(treeView1_AfterSelect);
            treeView1.ShowNodeToolTips = true;
            richTextBox1.TextChanged += new EventHandler(richTextBox1_TextChanged);
            treeView1.Parent = splitContainer1.Panel1;
            richTextBox1.Parent = splitContainer1.Panel2;
            splitContainer1.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            backgroundWorker1.RunWorkerAsync();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            XDocument xml;

            if (isXMLFormat(richTextBox1.Text, out xml))
            {
                richTextBox1.Text = xml.ToString();
            }
            else
            {
                try
                {
                    var token = JToken.Parse(richTextBox1.Text);
                    richTextBox1.Text = token.ToString();
                }
                catch (Exception ex)
                {
                    //if not xml or json just print the text to textbox
                }
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                TreeView node = (TreeView)sender;
                var parent = node.SelectedNode.Parent?.Text;
                if(parent != null)
                {
                    var msg = queueManager.GetQueueByName(parent).PeekById(node.SelectedNode.ToolTipText);                  
                    richTextBox1.Text = queueManager.ReadMessage(msg);
                }               
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void loadMessagesTree()
        {
            treeView1.Nodes.Clear();
            
            int index = 0;
            treeView1.Invoke((Action)delegate
            {
                foreach (var queue in queueManager.PrivateQueues)
                {
                    treeView1.Nodes.Add(TreeNodeFromQueue(queue));
                    index++;
                    progressBar1.Value = ((int)(index * 100 / queueManager.PrivateQueues.Length));
                }
            });
            
        }

        private TreeNode TreeNodeFromQueue(MessageQueue queue)
        {
            queue.MessageReadPropertyFilter.ArrivedTime = true;
            var queueNode = new TreeNode(queue.QueueName);

            var queueMessages =  queue.GetAllMessages();
            if (queueMessages.Length > 0)
            {
                queueNode.ToolTipText = queueMessages.Length.ToString();
            }
            foreach (var msg in queueMessages)
            {
                var msgNode = new TreeNode(msg.ArrivedTime.ToString());
                msgNode.ToolTipText = msg.Id;
                queueNode.Nodes.Add(msgNode);
            }
            return queueNode;
        }

        private void refreshQueue(string queueName,TreeNode node)
        {
            int index = treeView1.Nodes.IndexOf(node);
            treeView1.Nodes.RemoveAt(index);
            var queueToRefresh = queueManager.GetQueueByName(queueName);
            treeView1.Nodes.Insert(index, TreeNodeFromQueue(queueToRefresh));
        }

        private bool isXMLFormat(string xmlString, out XDocument res)
        {
            try
            {
                res = XDocument.Parse(xmlString);
                return true;
            }
            catch
            {
                res = null;
                return false;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            var queueNode = treeView1.SelectedNode;
            if (queueNode.Parent == null) //the node is a queue and not a message
            {
                queueManager.GetQueueByName(queueNode.Text).Refresh();
                refreshQueue(queueNode.Text,queueNode);
            }
        }

        private void btnPurge_Click(object sender, EventArgs e)
        {
            var queue = treeView1.SelectedNode;
            if (queue.Parent == null) //the node is a queue and not a message
            {
                DialogResult dialogResult = MessageBox.Show($"Are you sure you want to delete all of the messages from {queue.Text.Split('\\').Last()}?",
                                                            "Purge confirmation",
                                                            MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    queueManager.GetQueueByName(queue.Text).Purge();
                    btnRefresh.PerformClick();
                }
                else if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            loadMessagesTree();
        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if(progressBar1.Value == 100)
            {
                progressBar1.Dispose();
            }
        }
    }

}
