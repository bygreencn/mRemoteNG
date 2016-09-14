using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.Root.PuttySessions;
using mRemoteNG.Tree.Root;
using WeifenLuo.WinFormsUI.Docking;


namespace mRemoteNG.UI.Window
{
	public partial class ConnectionTreeWindow
	{
	    private ConnectionTreeModel _connectionTreeModel;
	    private ConnectionTreeDragAndDropHandler _dragAndDropHandler = new ConnectionTreeDragAndDropHandler();
        private NodeSearcher _nodeSearcher;

	    public ConnectionInfo SelectedNode => (ConnectionInfo) olvConnections.SelectedObject;

	    public ConnectionTreeModel ConnectionTreeModel
	    {
	        get { return _connectionTreeModel; }
	        set
	        {
	            _connectionTreeModel = value;
	            PopulateTreeView();
	        }
	    }

		public ConnectionTreeWindow(DockContent panel)
		{
			WindowType = WindowType.Tree;
			DockPnl = panel;
			InitializeComponent();

            FillImageList();
            LinkModelToView();
		    SetupDropSink();
            SetEventHandlers();
		}

        private void FillImageList()
        {
            try
            {
                imgListTree.Images.Add(Resources.Root);
                imgListTree.Images.SetKeyName(0, "Root");
                imgListTree.Images.Add(Resources.Folder);
                imgListTree.Images.SetKeyName(1, "Folder");
                imgListTree.Images.Add(Resources.Play);
                imgListTree.Images.SetKeyName(2, "Play");
                imgListTree.Images.Add(Resources.Pause);
                imgListTree.Images.SetKeyName(3, "Pause");
                imgListTree.Images.Add(Resources.PuttySessions);
                imgListTree.Images.SetKeyName(4, "PuttySessions");
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("FillImageList (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private void LinkModelToView()
	    {
            olvNameColumn.AspectGetter = item => ((ConnectionInfo)item).Name;
	        olvNameColumn.ImageGetter = ConnectionImageGetter;
            olvConnections.CanExpandGetter = item => item is ContainerInfo && ((ContainerInfo)item).Children.Count > 0;
            olvConnections.ChildrenGetter = item => ((ContainerInfo)item).Children;
        }

	    private void SetupDropSink()
	    {
	        var dropSink = (SimpleDropSink)olvConnections.DropSink;
	        dropSink.CanDropBetween = true;
	    }

	    private object ConnectionImageGetter(object rowObject)
	    {
            if (rowObject is RootNodeInfo) return "Root";
	        if (rowObject is ContainerInfo) return "Folder";
            if (rowObject is PuttySessionInfo) return "PuttySessions";
	        var connection = rowObject as ConnectionInfo;
	        if (connection == null) return "";
	        return connection.OpenConnections.Count > 0 ? "Play" : "Pause";
	    }

	    private void SetEventHandlers()
	    {
	        SetTreeEventHandlers();
	        SetMenuEventHandlers();
	    }

	    private void SetTreeEventHandlers()
	    {
	        olvConnections.Collapsed += (sender, args) =>
	        {
	            var container = args.Model as ContainerInfo;
                if (container != null)
                    container.IsExpanded = false;
	        };
            olvConnections.Expanded += (sender, args) =>
            {
                var container = args.Model as ContainerInfo;
                if (container != null)
                    container.IsExpanded = true;
            };
            olvConnections.BeforeLabelEdit += tvConnections_BeforeLabelEdit;
            olvConnections.AfterLabelEdit += tvConnections_AfterLabelEdit;
            olvConnections.SelectionChanged += tvConnections_AfterSelect;
            olvConnections.CellClick += tvConnections_NodeMouseSingleClick;
            olvConnections.CellClick += tvConnections_NodeMouseDoubleClick;
            olvConnections.CellToolTipShowing += tvConnections_CellToolTipShowing;
            olvConnections.ModelCanDrop += _dragAndDropHandler.HandleEvent_ModelCanDrop;
            olvConnections.ModelDropped += _dragAndDropHandler.HandleEvent_ModelDropped;
	        olvConnections.KeyDown += tvConnections_KeyDown;
	        olvConnections.KeyPress += tvConnections_KeyPress;
	    }

	    private void SetMenuEventHandlers()
	    {
            cMenTreeDuplicate.Click += (sender, args) => DuplicateSelectedNode();
            cMenTreeRename.Click += (sender, args) => RenameSelectedNode();
            cMenTreeDelete.Click += (sender, args) => DeleteSelectedNode();
            mMenViewExpandAllFolders.Click += (sender, args) => olvConnections.ExpandAll();
	        mMenViewCollapseAllFolders.Click += (sender, args) =>
	        {
	            olvConnections.CollapseAll();
                olvConnections.Expand(GetRootConnectionNode());
	        };
            cMenTree.Opening += (sender, args) => AddExternalApps();
            cMenTreeImport.Click += (sender, args) => Windows.Show(WindowType.ActiveDirectoryImport);
            cMenTreeImportPortScan.Click += (sender, args) => Windows.Show(WindowType.PortScan);
        }

	    private void PopulateTreeView()
	    {
            olvConnections.Roots = ConnectionTreeModel.RootNodes;
            _nodeSearcher = new NodeSearcher(ConnectionTreeModel);
	        ExpandPreviouslyOpenedFolders();
            ExpandRootConnectionNode();
	        OpenConnectionsFromLastSession();
	    }

	    private void ExpandRootConnectionNode()
	    {
            var rootConnectionNode = GetRootConnectionNode();
            olvConnections.Expand(rootConnectionNode);
        }

	    private RootNodeInfo GetRootConnectionNode()
	    {
            return (RootNodeInfo)olvConnections.Roots.Cast<ConnectionInfo>().First(item => item is RootNodeInfo);
        }

        #region Form Stuff
        private void Tree_Load(object sender, EventArgs e)
        {
            ApplyLanguage();
            Themes.ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();

            txtSearch.Multiline = true;
            txtSearch.MinimumSize = new Size(0, 14);
            txtSearch.Size = new Size(txtSearch.Size.Width, 14);
            txtSearch.Multiline = false;
        }

        private void ApplyLanguage()
        {
            Text = Language.strConnections;
            TabText = Language.strConnections;

            mMenAddConnection.ToolTipText = Language.strAddConnection;
            mMenAddFolder.ToolTipText = Language.strAddFolder;
            mMenView.ToolTipText = Language.strMenuView.Replace("&", "");
            mMenViewExpandAllFolders.Text = Language.strExpandAllFolders;
            mMenViewCollapseAllFolders.Text = Language.strCollapseAllFolders;
            mMenSortAscending.ToolTipText = Language.strSortAsc;

            cMenTreeConnect.Text = Language.strConnect;
            cMenTreeConnectWithOptions.Text = Language.strConnectWithOptions;
            cMenTreeConnectWithOptionsConnectToConsoleSession.Text = Language.strConnectToConsoleSession;
            cMenTreeConnectWithOptionsDontConnectToConsoleSession.Text = Language.strDontConnectToConsoleSessionMenuItem;
            cMenTreeConnectWithOptionsConnectInFullscreen.Text = Language.strConnectInFullscreen;
            cMenTreeConnectWithOptionsNoCredentials.Text = Language.strConnectNoCredentials;
            cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Text = Language.strChoosePanelBeforeConnecting;
            cMenTreeDisconnect.Text = Language.strMenuDisconnect;

            cMenTreeToolsExternalApps.Text = Language.strMenuExternalTools;
            cMenTreeToolsTransferFile.Text = Language.strMenuTransferFile;

            cMenTreeDuplicate.Text = Language.strDuplicate;
            cMenTreeRename.Text = Language.strRename;
            cMenTreeDelete.Text = Language.strMenuDelete;

            cMenTreeImport.Text = Language.strImportMenuItem;
            cMenTreeImportFile.Text = Language.strImportFromFileMenuItem;
            cMenTreeImportActiveDirectory.Text = Language.strImportAD;
            cMenTreeImportPortScan.Text = Language.strImportPortScan;
            cMenTreeExportFile.Text = Language.strExportToFileMenuItem;

            cMenTreeAddConnection.Text = Language.strAddConnection;
            cMenTreeAddFolder.Text = Language.strAddFolder;

            cMenTreeToolsSort.Text = Language.strSort;
            cMenTreeToolsSortAscending.Text = Language.strSortAsc;
            cMenTreeToolsSortDescending.Text = Language.strSortDesc;
            cMenTreeMoveUp.Text = Language.strMoveUp;
            cMenTreeMoveDown.Text = Language.strMoveDown;

            txtSearch.Text = Language.strSearchPrompt;
        }

        private void ApplyTheme()
        {
            msMain.BackColor = Themes.ThemeManager.ActiveTheme.ToolbarBackgroundColor;
            msMain.ForeColor = Themes.ThemeManager.ActiveTheme.ToolbarTextColor;
            olvConnections.BackColor = Themes.ThemeManager.ActiveTheme.ConnectionsPanelBackgroundColor;
            olvConnections.ForeColor = Themes.ThemeManager.ActiveTheme.ConnectionsPanelTextColor;
            //tvConnections.LineColor = Themes.ThemeManager.ActiveTheme.ConnectionsPanelTreeLineColor;
            BackColor = Themes.ThemeManager.ActiveTheme.ToolbarBackgroundColor;
            txtSearch.BackColor = Themes.ThemeManager.ActiveTheme.SearchBoxBackgroundColor;
            txtSearch.ForeColor = Themes.ThemeManager.ActiveTheme.SearchBoxTextPromptColor;
        }
        #endregion

        public void ExpandPreviouslyOpenedFolders()
        {
            var containerList = ConnectionTreeModel.GetRecursiveChildList(GetRootConnectionNode()).OfType<ContainerInfo>();
            var previouslyExpandedNodes = containerList.Where(container => container.IsExpanded);
            olvConnections.ExpandedObjects = previouslyExpandedNodes;
            olvConnections.RebuildAll(true);
        }

        public void OpenConnectionsFromLastSession()
        {
            if (!Settings.Default.OpenConsFromLastSession || Settings.Default.NoReconnect) return;
            var connectionInfoList = GetRootConnectionNode().GetRecursiveChildList().Where(node => !(node is ContainerInfo));
            var previouslyOpenedConnections = connectionInfoList.Where(item => item.PleaseConnect);
            foreach (var connectionInfo in previouslyOpenedConnections)
            {
                ConnectionInitiator.OpenConnection(connectionInfo);
            }
        }

        public void EnsureRootNodeVisible()
	    {
            olvConnections.EnsureModelVisible(GetRootConnectionNode());
	    }

        public void DuplicateSelectedNode()
        {
            var newNode = SelectedNode.Clone();
            newNode.Parent.SetChildBelow(newNode, SelectedNode);
            Runtime.SaveConnectionsBG();
            olvConnections.RefreshObject(SelectedNode);
        }

        public void RenameSelectedNode()
        {
            olvConnections.SelectedItem.BeginEdit();
            Runtime.SaveConnectionsBG();
        }

        public void DeleteSelectedNode()
        {
            if (!UserConfirmsDeletion()) return;
            ConnectionTreeModel.DeleteNode(SelectedNode);
            Runtime.SaveConnectionsBG();
            olvConnections.RefreshObject(SelectedNode);
        }

	    private bool UserConfirmsDeletion()
	    {
	        var selectedNodeAsContainer = SelectedNode as ContainerInfo;
	        if (selectedNodeAsContainer != null)
	            return selectedNodeAsContainer.HasChildren()
	                ? UserConfirmsNonEmptyFolderDeletion()
	                : UserConfirmsEmptyFolderDeletion();
	        return UserConfirmsConnectionDeletion();
	    }

        private bool UserConfirmsEmptyFolderDeletion()
        {
            var messagePrompt = string.Format(Language.strConfirmDeleteNodeFolder, SelectedNode.Name);
            return PromptUser(messagePrompt);
        }

        private bool UserConfirmsNonEmptyFolderDeletion()
        {
            var messagePrompt = string.Format(Language.strConfirmDeleteNodeFolderNotEmpty, SelectedNode.Name);
            return PromptUser(messagePrompt);
        }

        private bool UserConfirmsConnectionDeletion()
        {
            var messagePrompt = string.Format(Language.strConfirmDeleteNodeConnection, SelectedNode.Name);
            return PromptUser(messagePrompt);
        }

        private bool PromptUser(string promptMessage)
        {
            var msgBoxResponse = MessageBox.Show(promptMessage, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return (msgBoxResponse == DialogResult.Yes);
        }

        #region Private Methods
        private void tvConnections_BeforeLabelEdit(object sender, LabelEditEventArgs e)
		{
			cMenTreeDelete.ShortcutKeys = Keys.None;
		}

        private void tvConnections_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			try
			{
				cMenTreeDelete.ShortcutKeys = Keys.Delete;
                ConnectionTreeModel.RenameNode(SelectedNode, e.Label);
                Windows.configForm.pGrid_SelectedObjectChanged();
				ShowHideTreeContextMenuItems(SelectedNode);
                Runtime.SaveConnectionsBG();
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_AfterLabelEdit (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private void tvConnections_AfterSelect(object sender, EventArgs e)
		{
            try
            {
                Windows.configForm.SetPropertyGridObject(olvConnections.SelectedObject);
                ShowHideTreeContextMenuItems((ConnectionInfo)olvConnections.SelectedObject);
                Runtime.LastSelected = ((ConnectionInfo)olvConnections.SelectedObject)?.ConstantID;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_AfterSelect (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private void tvConnections_NodeMouseSingleClick(object sender, CellClickEventArgs e)
		{
            try
            {
                if (e.ClickCount > 1) return;
                var clickedNode = (ConnectionInfo)e.Model;
                ShowHideTreeContextMenuItems(SelectedNode);
                
                //if (e.Button != MouseButtons.Left) return;
                if (clickedNode.GetTreeNodeType() != TreeNodeType.Connection && clickedNode.GetTreeNodeType() != TreeNodeType.PuttySession) return;
                if (Settings.Default.SingleClickOnConnectionOpensIt)
                    ConnectionInitiator.OpenConnection(SelectedNode);

                if (Settings.Default.SingleClickSwitchesToOpenConnection)
                    ConnectionInitiator.SwitchToOpenConnection(SelectedNode);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_NodeMouseClick (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private void tvConnections_NodeMouseDoubleClick(object sender, CellClickEventArgs e)
        {
            if (e.ClickCount < 2) return;
            var clickedNode = e.Model as ConnectionInfo;
            
            if (clickedNode?.GetTreeNodeType() == TreeNodeType.Connection |
                clickedNode?.GetTreeNodeType() == TreeNodeType.PuttySession)
			{
                ConnectionInitiator.OpenConnection(SelectedNode);
			}
		}

        private void tvConnections_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			try
			{
			    var nodeProducingTooltip = (ConnectionInfo) e.Model;
			    e.Text = nodeProducingTooltip.Description;
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_MouseMove (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private static void EnableMenuItemsRecursive(ToolStripItemCollection items, bool enable = true)
		{
		    foreach (ToolStripItem item in items)
			{
				var menuItem = item as ToolStripMenuItem;
				if (menuItem == null)
				{
					continue;
				}
				menuItem.Enabled = enable;
				if (menuItem.HasDropDownItems)
				{
					EnableMenuItemsRecursive(menuItem.DropDownItems, enable);
				}
			}
		}

        private void ShowHideTreeContextMenuItems(ConnectionInfo connectionInfo)
		{
			if (connectionInfo == null)
				return ;
					
			try
			{
				cMenTree.Enabled = true;
				EnableMenuItemsRecursive(cMenTree.Items);
				if (connectionInfo is RootPuttySessionsNodeInfo)
                {
                    cMenTreeAddConnection.Enabled = false;
                    cMenTreeAddFolder.Enabled = false;
                    cMenTreeConnect.Enabled = false;
                    cMenTreeConnectWithOptions.Enabled = false;
                    cMenTreeDisconnect.Enabled = false;
                    cMenTreeToolsTransferFile.Enabled = false;
                    cMenTreeConnectWithOptions.Enabled = false;
                    cMenTreeToolsSort.Enabled = false;
                    cMenTreeToolsExternalApps.Enabled = false;
                    cMenTreeDuplicate.Enabled = false;
                    cMenTreeRename.Enabled = true;
                    cMenTreeDelete.Enabled = false;
                    cMenTreeMoveUp.Enabled = false;
                    cMenTreeMoveDown.Enabled = false;
                }
                else if (connectionInfo is RootNodeInfo)
                {
                    cMenTreeConnect.Enabled = false;
                    cMenTreeConnectWithOptions.Enabled = false;
                    cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
                    cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
                    cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Enabled = false;
                    cMenTreeDisconnect.Enabled = false;
                    cMenTreeToolsTransferFile.Enabled = false;
                    cMenTreeToolsExternalApps.Enabled = false;
                    cMenTreeDuplicate.Enabled = false;
                    cMenTreeDelete.Enabled = false;
                    cMenTreeMoveUp.Enabled = false;
                    cMenTreeMoveDown.Enabled = false;
                }
                else if (connectionInfo is ContainerInfo)
                {
                    cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
                    cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
                    cMenTreeDisconnect.Enabled = false;

                    var openConnections = ((ContainerInfo) connectionInfo).Children.Sum(child => child.OpenConnections.Count);
                    if (openConnections == 0)
                        cMenTreeDisconnect.Enabled = false;

                    cMenTreeToolsTransferFile.Enabled = false;
                    cMenTreeToolsExternalApps.Enabled = false;
                }
                else if (connectionInfo is PuttySessionInfo)
				{
					cMenTreeAddConnection.Enabled = false;
					cMenTreeAddFolder.Enabled = false;
							
					if (connectionInfo.OpenConnections.Count == 0)
						cMenTreeDisconnect.Enabled = false;
							
					if (!(connectionInfo.Protocol == ProtocolType.SSH1 | connectionInfo.Protocol == ProtocolType.SSH2))
						cMenTreeToolsTransferFile.Enabled = false;
							
					cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
					cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
					cMenTreeToolsSort.Enabled = false;
					cMenTreeDuplicate.Enabled = false;
					cMenTreeRename.Enabled = false;
					cMenTreeDelete.Enabled = false;
					cMenTreeMoveUp.Enabled = false;
					cMenTreeMoveDown.Enabled = false;
				}
                else
                {
                    if (connectionInfo.OpenConnections.Count == 0)
                        cMenTreeDisconnect.Enabled = false;

                    if (!(connectionInfo.Protocol == ProtocolType.SSH1 | connectionInfo.Protocol == ProtocolType.SSH2))
                        cMenTreeToolsTransferFile.Enabled = false;

                    if (!(connectionInfo.Protocol == ProtocolType.RDP | connectionInfo.Protocol == ProtocolType.ICA))
                    {
                        cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
                        cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
                    }

                    if (connectionInfo.Protocol == ProtocolType.IntApp)
                        cMenTreeConnectWithOptionsNoCredentials.Enabled = false;
                }
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("ShowHideTreeContextMenuItems (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}
        #endregion

        #region Tree Context Menu
        private void cMenTreeAddConnection_Click(object sender, EventArgs e)
		{
			AddConnection();
            Runtime.SaveConnectionsBG();
		}

        private void cMenTreeAddFolder_Click(object sender, EventArgs e)
		{
			AddFolder();
            Runtime.SaveConnectionsBG();
		}

        private void cMenTreeConnect_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.DoNotJump);
		}

        private void cMenTreeConnectWithOptionsConnectToConsoleSession_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.UseConsoleSession | ConnectionInfo.Force.DoNotJump);
		}

        private void cMenTreeConnectWithOptionsNoCredentials_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.NoCredentials);
		}

        private void cMenTreeConnectWithOptionsDontConnectToConsoleSession_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.DontUseConsoleSession | ConnectionInfo.Force.DoNotJump);
		}

        private void cMenTreeConnectWithOptionsConnectInFullscreen_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.Fullscreen | ConnectionInfo.Force.DoNotJump);
		}

        private void cMenTreeConnectWithOptionsChoosePanelBeforeConnecting_Click(object sender, EventArgs e)
		{
            ConnectionInitiator.OpenConnection(SelectedNode, ConnectionInfo.Force.OverridePanel | ConnectionInfo.Force.DoNotJump);
		}

	    private void cMenTreeDisconnect_Click(object sender, EventArgs e)
		{
			DisconnectConnection(SelectedNode);
		}

        private void cMenTreeToolsTransferFile_Click(object sender, EventArgs e)
		{
			SshTransferFile();
		}

        //TODO Fix for TreeListView
        private void mMenSortAscending_Click(object sender, EventArgs e)
		{
			tvConnections.BeginUpdate();
            ConnectionTree.Sort(tvConnections.Nodes[0], SortOrder.Ascending);
			tvConnections.EndUpdate();
            Runtime.SaveConnectionsBG();
		}

        //TODO Fix for TreeListView
        private void cMenTreeToolsSortAscending_Click(object sender, EventArgs e)
		{
			tvConnections.BeginUpdate();
            ConnectionTree.Sort(tvConnections.SelectedNode, SortOrder.Ascending);
			tvConnections.EndUpdate();
            Runtime.SaveConnectionsBG();
		}

        //TODO Fix for TreeListView
        private void cMenTreeToolsSortDescending_Click(object sender, EventArgs e)
		{
			tvConnections.BeginUpdate();
            ConnectionTree.Sort(tvConnections.SelectedNode, SortOrder.Descending);
			tvConnections.EndUpdate();
            Runtime.SaveConnectionsBG();
		}

        //TODO Fix for TreeListView
        private void cMenTreeImportFile_Click(object sender, EventArgs e)
		{
            Import.ImportFromFile(Windows.treeForm.tvConnections.Nodes[0], Windows.treeForm.tvConnections.SelectedNode, true);
		}

        //TODO Fix for TreeListView
        private void cMenTreeExportFile_Click(object sender, EventArgs e)
		{
            Export.ExportToFile(Windows.treeForm.tvConnections.Nodes[0], Windows.treeForm.tvConnections.SelectedNode, Runtime.ConnectionTreeModel);
		}

        private void cMenTreeMoveUp_Click(object sender, EventArgs e)
		{
            SelectedNode.Parent.PromoteChild(SelectedNode);
            olvConnections.RefreshObject(SelectedNode);
            Runtime.SaveConnectionsBG();
		}

        private void cMenTreeMoveDown_Click(object sender, EventArgs e)
		{
            SelectedNode.Parent.DemoteChild(SelectedNode);
            olvConnections.RefreshObject(SelectedNode);
            Runtime.SaveConnectionsBG();
		}
        #endregion

        #region Context Menu Actions
        public void AddConnection()
		{
			try
			{
			    var newConnectionInfo = new ConnectionInfo();
			    var selectedContainer = SelectedNode as ContainerInfo;
			    newConnectionInfo.SetParent(selectedContainer ?? SelectedNode.Parent);
                olvConnections.RebuildAll(true);
                Runtime.ConnectionList.Add(newConnectionInfo);
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("UI.Window.Tree.AddConnection() failed.", ex);
			}
		}

        public void AddFolder()
		{
			try
			{
				var newContainerInfo = new ContainerInfo();
                var selectedContainer = SelectedNode as ContainerInfo;
                newContainerInfo.SetParent(selectedContainer ?? SelectedNode.Parent);
                olvConnections.RebuildAll(true);
                Runtime.ContainerList.Add(newContainerInfo);
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace(Language.strErrorAddFolderFailed, ex);
			}
		}

        private void DisconnectConnection(ConnectionInfo connectionInfo)
		{
			try
			{
			    if (connectionInfo == null) return;
			    var nodeAsContainer = connectionInfo as ContainerInfo;
                if (nodeAsContainer != null)
                {
                    foreach (var child in nodeAsContainer.Children)
                    {
                        for (var i = 0; i <= child.OpenConnections.Count - 1; i++)
                        {
                            child.OpenConnections[i].Disconnect();
                        }
                    }
                }
			    else
                {
			        for (var i = 0; i <= connectionInfo.OpenConnections.Count - 1; i++)
			        {
                        connectionInfo.OpenConnections[i].Disconnect();
			        }
			    }
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("DisconnectConnection (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private void SshTransferFile()
		{
			try
			{
                Windows.Show(WindowType.SSHTransfer);                
                Windows.sshtransferForm.Hostname = SelectedNode.Hostname;
                Windows.sshtransferForm.Username = SelectedNode.Username;
                Windows.sshtransferForm.Password = SelectedNode.Password;
                Windows.sshtransferForm.Port = Convert.ToString(SelectedNode.Port);
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("SSHTransferFile (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private void AddExternalApps()
		{
			try
			{
			    ResetExternalAppMenu();

                foreach (Tools.ExternalTool extA in Runtime.ExternalTools)
				{
				    var menuItem = new ToolStripMenuItem
				    {
				        Text = extA.DisplayName,
				        Tag = extA,
				        Image = extA.Image
				    };

				    menuItem.Click += (sender, args) => StartExternalApp((Tools.ExternalTool)((ToolStripMenuItem)sender).Tag);
					cMenTreeToolsExternalApps.DropDownItems.Add(menuItem);
				}
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("cMenTreeTools_DropDownOpening failed (UI.Window.ConnectionTreeWindow)", ex);
			}
		}

	    private void ResetExternalAppMenu()
	    {
	        if (cMenTreeToolsExternalApps.DropDownItems.Count <= 0) return;
	        for (var i = cMenTreeToolsExternalApps.DropDownItems.Count - 1; i >= 0; i--)
	            cMenTreeToolsExternalApps.DropDownItems[i].Dispose();

	        cMenTreeToolsExternalApps.DropDownItems.Clear();
	    }

        private void StartExternalApp(Tools.ExternalTool externalTool)
		{
			try
			{
                if (SelectedNode.GetTreeNodeType() == TreeNodeType.Connection | SelectedNode.GetTreeNodeType() == TreeNodeType.PuttySession)
                    externalTool.Start(SelectedNode);
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("cMenTreeToolsExternalAppsEntry_Click failed (UI.Window.ConnectionTreeWindow)", ex);
			}
		}
        #endregion

        #region Search
        private void txtSearch_GotFocus(object sender, EventArgs e)
		{
			txtSearch.ForeColor = Themes.ThemeManager.ActiveTheme.SearchBoxTextColor;
			if (txtSearch.Text == Language.strSearchPrompt)
				txtSearch.Text = "";
		}

        private void txtSearch_LostFocus(object sender, EventArgs e)
		{
            if (txtSearch.Text != "") return;
            txtSearch.ForeColor = Themes.ThemeManager.ActiveTheme.SearchBoxTextPromptColor;
            txtSearch.Text = Language.strSearchPrompt;
		}

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Escape)
				{
					e.Handled = true;
				    olvConnections.Focus();
				}
				else if (e.KeyCode == Keys.Up)
				{
                    var match = _nodeSearcher.PreviousMatch();
                    JumpToNode(match);
                    e.Handled = true;
				}
				else if (e.KeyCode == Keys.Down)
				{
				    var match = _nodeSearcher.NextMatch();
				    JumpToNode(match);
                    e.Handled = true;
				}
				else
				{
					tvConnections_KeyDown(sender, e);
				}
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("txtSearch_KeyDown (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private void txtSearch_TextChanged(object sender, EventArgs e)
		{
            if (txtSearch.Text == "") return;
            _nodeSearcher.SearchByName(txtSearch.Text);
            JumpToNode(_nodeSearcher.CurrentMatch);
        }

	    private void JumpToNode(ConnectionInfo connectionInfo)
	    {
	        if (connectionInfo == null)
	        {
	            olvConnections.SelectedObject = null;
                return;
	        }
	        ExpandParentsRecursive(connectionInfo);
            olvConnections.SelectObject(connectionInfo);
            olvConnections.EnsureModelVisible(connectionInfo);
        }

	    private void ExpandParentsRecursive(ConnectionInfo connectionInfo)
	    {
            if (connectionInfo?.Parent == null) return;
	        olvConnections.Expand(connectionInfo.Parent);
            ExpandParentsRecursive(connectionInfo.Parent);
        }

        private void tvConnections_KeyPress(object sender, KeyPressEventArgs e)
		{
			try
			{
			    if (!char.IsLetterOrDigit(e.KeyChar)) return;
			    txtSearch.Text = e.KeyChar.ToString();
			    txtSearch.Focus();
			    txtSearch.SelectionStart = txtSearch.TextLength;
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_KeyPress (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}

        private void tvConnections_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Enter)
				{
					e.Handled = true;
                    ConnectionInitiator.OpenConnection(SelectedNode);
				}
				else if (e.Control && e.KeyCode == Keys.F)
				{
					txtSearch.Focus();
                    txtSearch.SelectAll();
				    e.Handled = true;
				}
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_KeyDown (UI.Window.ConnectionTreeWindow) failed", ex);
			}
		}
        #endregion
	}
}