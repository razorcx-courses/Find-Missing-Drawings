using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RazorCX.FindMissingDrawings
{
    public class DataGridSelectedItemsBlendBehavior : Behavior<DataGrid>
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(name: "SelectedItems", propertyType: typeof(IList<object>),
                ownerType: typeof(DataGridSelectedItemsBlendBehavior),
                typeMetadata: new FrameworkPropertyMetadata(propertyChangedCallback: null)
                {
                    BindsTwoWayByDefault = true
                });

        public IList<object> SelectedItems
        {
            get => (IList<object>)GetValue(dp: SelectedItemProperty);
            set => SetValue(dp: SelectedItemProperty, value: value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (this.AssociatedObject != null)
                this.AssociatedObject.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0 && this.SelectedItems != null)
            {
                foreach (object obj in e.AddedItems)
                    this.SelectedItems.Add(item: obj);
            }

            if (e.RemovedItems != null && e.RemovedItems.Count > 0 && this.SelectedItems != null)
            {
                foreach (var obj in e.RemovedItems)
                    this.SelectedItems.Remove(item: obj);
            }
        }
    }
}
