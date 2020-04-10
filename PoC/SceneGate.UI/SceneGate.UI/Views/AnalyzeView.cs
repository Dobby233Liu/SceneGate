﻿
namespace SceneGate.UI.Views
{
    using Eto.Forms;
    using Lemon.Containers;
    using System;
    using System.Linq;
    using Yarhl;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;

    public class AnalyzeView : Panel
    {
        TreeGridView tree;
        ListBox converterList;
        ConverterMetadata[] converters;

        public AnalyzeView()
        {
            CreateControls();
        }

        void CreateControls()
        {
            var splitterRight = new Splitter {
                Position = 1000,
                FixedPanel = SplitterFixedPanel.Panel2,
                Panel2MinimumSize = 150,
                Panel1 = new DocumentControl(),
                Panel2 = CreateRightPanel(),
            };

            var splitter = new Splitter
            {
                Position = 300,
                FixedPanel = SplitterFixedPanel.Panel1,
                Panel1MinimumSize = 150,
                Panel1 = CreateLeftPanel(),
                Panel2 = splitterRight,
            };

            Content = splitter;
        }

        Panel CreateRightPanel()
        {
            converters = PluginManager.Instance.GetConverters().Select(x => x.Metadata).ToArray();
            converterList = new ListBox();
            converterList.Items.AddRange(converters.Select(x => new ListItem { Key = x.Type.FullName, Text = x.Name }));

            var stack = new DynamicLayout();
            stack.BeginHorizontal();
            stack.AddRow(converterList);
            stack.EndHorizontal();

            stack.Invalidate();

            return stack;
        }

        Panel CreateLeftPanel()
        {
            Button addBtn = new Button(AddRootNode);
            addBtn.Text = "Add";

            tree = new TreeGridView();
            tree.ShowHeader = false;
            tree.Border = BorderType.Line;
            tree.Columns.Add(
                new GridColumn
                {
                    DataCell = new TextBoxCell(1),
                    HeaderText = "Name",
                    AutoSize = true,
                    Resizable = true,
                    Editable = true
                });
            tree.DataStore = new TreeGridItem("root");

            if (Platform.Supports<ContextMenu>()) {
                var menu = new ContextMenu();
                var item = new ButtonMenuItem { Text = "Export to file" };
                item.Click += delegate
                {
                    if (tree.SelectedItems.Any())
                    {
                        var selected = tree.SelectedItem as TreeGridItem;
                        var node = selected.GetValue(0) as Node;
                        SaveFileDialog dialog = new SaveFileDialog();
                        if (dialog.ShowDialog(ParentWindow) == DialogResult.Ok)
                        {
                            node.Stream.WriteTo(dialog.FileName);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Click, no item selected");
                    }
                };
                menu.Items.Add(item);

                var item2 = new ButtonMenuItem { Text = "Convert" };
                item2.Click += delegate {
                    if (tree.SelectedItems.Any() && converterList.SelectedIndex != -1) {
                        var selected = tree.SelectedItem as TreeGridItem;
                        var node = selected.GetValue(0) as Node;

                        try {
                            node.TransformWith(converters[converterList.SelectedIndex].Type);
                        } catch (Exception ex) {
                            MessageBox.Show($"Failed to convert:\n{ex}");
                        }

                        AppendNodeToTree(node);
                    }
                };
                menu.Items.Add(item2);

                tree.ContextMenu = menu;
            }

            var headerLayout = new DynamicLayout();
            headerLayout.BeginHorizontal();
            headerLayout.AddRow(new Label { Text = "Nodes" }, null, addBtn, new Button { Text = "Collapse" });
            headerLayout.EndHorizontal();
            headerLayout.Padding = new Eto.Drawing.Padding(5);

            var stack = new DynamicLayout();
            stack.BeginHorizontal();
            stack.AddRow(headerLayout);
            stack.AddRow(tree);
            stack.EndHorizontal();

            return stack;
        }

        void AddRootNode(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog(ParentWindow) == DialogResult.Ok)
            {
                Node n = NodeFactory.FromFile(dialog.FileName);
                ContainerManager.Unpack3DSNode(n);
                AppendNodeToTree(n);
            }
        }

        void AppendNodeToTree(Node node)
        {
            var item = tree.SelectedItem as TreeGridItem;
            var parent = (item?.Parent ?? (ITreeGridItem)tree.DataStore) as TreeGridItem;
            AppendNode(parent, node);

            if (item != null) {
                parent.Children.Remove(item);
                tree.ReloadItem(parent);
            } else {
                tree.ReloadData();
            }
        }

        void AppendNode(TreeGridItem item, Node node)
        {
            var current = CreateTreeItem(node);
            item.Children.Add(current);
            foreach (var child in node.Children)
                AppendNode(current, child);
        }

        TreeGridItem CreateTreeItem(Node node)
        {
            string name = node.Name;
            if (node.Format != null & !node.IsContainer) {
                name += $" [{node.Format.GetType().Name}]";
            }

            return new TreeGridItem(node, name);
        }
    }
}
