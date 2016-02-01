﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ClassOutline.ControlLibrary
{
    class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cp = (ContentPresenter  )value;
            var item = (OutlineItem) cp.Content;
           
            if (item.Parent == null) return true;
            var siblingCount = item.Parent.Children.Count-1;

            if (item.Parent.Children.IndexOf(item) == siblingCount) return true;
            return false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
    /// <summary>
    /// Interaction logic for OutlineControl.xaml
    /// </summary>
    public partial class OutlineControl : UserControl
    {
        private int? _tooltipDuration=null ;

        public delegate void ItemDoubleClickHandler(object sender, MouseButtonEventArgs args);

          public event ItemDoubleClickHandler ItemDoubleClick;
      
        public OutlineControl()
        {
            InitializeComponent();
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void OnPreviewRightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }

        }

        public OutlineItem SelectedItem
        {
            get
            {
                return (OutlineItem) treeView.SelectedItem;
                
            }
        }
        public void Select(OutlineItem itm)
        {
            
            var tvi = GetTreeViewItem(treeView, itm);
            if (tvi != null)
            {
                  tvi.IsSelected = true;
            tvi.BringIntoView();
                tvi.ExpandSubtree();
                tvi.IsExpanded = true;
            }
          
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem) sender;
            
           // enableTooltips(tvi, false );

            var dc = tvi.DataContext as OutlineItem;
            if (dc != null)
            {
                
                if (dc.MenuItems == null || !dc.MenuItems.Any())
                {
                    e.Handled = true;

                }
            }
        }

        private void enableTooltips(Control control, bool isEnabled)
        {
            if(_tooltipDuration==null) _tooltipDuration = ToolTipService.GetShowDuration(control);
            var tooltip = (ToolTip) control.ToolTip;
            if(!isEnabled && tooltip!=null ) tooltip.IsOpen = false;
           
           ToolTipService.SetShowDuration(control, isEnabled ? _tooltipDuration.Value  : 100);
         ToolTipService.SetIsEnabled(control, isEnabled);

        }
        

        /// <summary>
        /// Recursively search for an item in this subtree.
        /// </summary>
        /// <param name="container">
        /// The parent ItemsControl. This can be a TreeView or a TreeViewItem.
        /// </param>
        /// <param name="item">
        /// The item to search for.
        /// </param>
        /// <returns>
        /// The TreeViewItem that contains the specified item.
        /// </returns>
        private TreeViewItem GetTreeViewItem(ItemsControl container, object item)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }

                // Expand the current container
                if (container is TreeViewItem && !((TreeViewItem)container).IsExpanded)
                {
                    container.SetValue(TreeViewItem.IsExpandedProperty, true);
                }

                // Try to generate the ItemsPresenter and the ItemsPanel.
                // by calling ApplyTemplate.  Note that in the
                // virtualizing case even if the item is marked
                // expanded we still need to do this step in order to
                // regenerate the visuals because they may have been virtualized away.

                container.ApplyTemplate();
                ItemsPresenter itemsPresenter =
                    (ItemsPresenter)container.Template.FindName("ItemsHost", container);
                if (itemsPresenter != null)
                {
                    itemsPresenter.ApplyTemplate();
                }
                else
                {
                    // The Tree template has not named the ItemsPresenter,
                    // so walk the descendents and find the child.
                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    if (itemsPresenter == null)
                    {
                        container.UpdateLayout();

                        itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    }
                }

                Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);


                // Ensure that the generator for this panel has been created.
                UIElementCollection children = itemsHostPanel.Children;

                MyVirtualizingStackPanel virtualizingPanel =
                    itemsHostPanel as MyVirtualizingStackPanel;

                for (int i = 0, count = container.Items.Count; i < count; i++)
                {
                    TreeViewItem subContainer;
                    if (virtualizingPanel != null)
                    {
                        // Bring the item into view so
                        // that the container will be generated.
                        virtualizingPanel.BringIntoView(i);

                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(i);
                    }
                    else
                    {
                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(i);

                        // Bring the item into view to maintain the
                        // same behavior as with a virtualizing panel.
                        subContainer.BringIntoView();
                    }

                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem resultContainer = GetTreeViewItem(subContainer, item);
                        if (resultContainer != null)
                        {
                            return resultContainer;
                        }
                        else
                        {
                            // The object is not under this TreeViewItem
                            // so collapse it.
                            subContainer.IsExpanded = false;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Search for an element of a certain type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="visual">The parent element.</param>
        /// <returns></returns>
        private T FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        return correctlyTyped;
                    }

                    T descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Handle double-click
        /// Swallow bubbled events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemDoubleClick != null)
            {
                if (e.Source is TreeViewItem && (e.Source as TreeViewItem).IsSelected)
                {
                    ItemDoubleClick(this, e);
                    e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void OnSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

        }


        private void OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                e.Handled = true; // don't buble the event
                OnItemDoubleClick(sender, e);
            }
        }
    }

  

    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        /// <summary>
        /// Publically expose BringIndexIntoView.
        /// </summary>
        public void BringIntoView(int index)
        {

            this.BringIndexIntoView(index);
        }
    }
}
