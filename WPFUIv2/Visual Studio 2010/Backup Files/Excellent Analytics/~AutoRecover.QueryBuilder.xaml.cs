﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using System.Xml;

using Analytics.Data;
using UI.Controls;
using Analytics.Authorization;
using System.Windows.Interop;
using Analytics.Data.Enums;


using UI;

namespace UI
{
    /// <summary>
    /// Interaction logic for QueryBuilder.xaml
    /// </summary>
    public partial class QueryBuilder : Window 
    {
        #region Fields
        DoubleAnimation _animatePropertyHeight;
        DoubleAnimation _animatePropertyWidth;

        Query _query;
        UserAccount _currentUserAccount;

        public delegate void QueryComplete(Query query);
        public event QueryComplete queryComplete;

        public enum ListType { Dim, Met, Fil };
        bool hasInvokedDimSetCheck;
        bool hasInvokedMetSetCheck;

        private const int maxSupportedDimensions = 7;
        private const int maxSupportedMetrics = 10;

        private string descending = "Descending";
        private string ascending = "Ascending";

        SizeKeyType activeSize; 
        #endregion

        #region Properties

        public UserAccount CurrentUser
        {
            get { return _currentUserAccount; }
            set { _currentUserAccount = value; }
        }

        private List<RadioButton> TimeSpanBoxesColl
        {
            get
            {
                return new RadioButton[] { yearCheckBox, monthCheckBox, weekCheckBox, weekCheckBoxAnglosax, quarterCheckBox, periodNotSpecifiedCheckBox}.Where(p => p != null).ToList<RadioButton>();
            }
        }

        private DoubleAnimation AnimatePropertyHeight
        {
            get
            {
                return _animatePropertyHeight != null ? _animatePropertyHeight :
                new DoubleAnimation(0.0, 259.0, new Duration(TimeSpan.FromSeconds(0.2))) { DecelerationRatio = 0.2 };
            }
            set { _animatePropertyHeight = value; }
        }

        private DoubleAnimation AnimatePropertyWidth
        {
            get
            {
                return _animatePropertyWidth != null ? _animatePropertyWidth :
                new DoubleAnimation(0.0, 543.0, new Duration(TimeSpan.FromSeconds(0.2))) { DecelerationRatio = 0.9 };
            }
            set { _animatePropertyWidth = value; }
        } 
        #endregion

        public QueryBuilder(UserAccount userAccount, Query query)
        {
            InitializeComponent();
            this._query = query != null ? query : new Query();
            _currentUserAccount = userAccount;
            InitializeForm();
            SetTimePeriod(query);            
        }

        private void SetSelectedDates()
        {
            _query.StartDate = (DateTime)startDateCalendar.SelectedDate;
            _query.EndDate = (DateTime)endDateCalendar.SelectedDate;
        }

        private void SetTimePeriod(Query query)
        {
            if (!(query.Metrics.Values.Count.Equals(0)) && (_query.TimePeriod != TimePeriod.PeriodNotSpecified))
            {
                setCalendarToDefault();

                foreach (RadioButton itBox in TimeSpanBoxesColl)
                    itBox.IsChecked = query.TimePeriod.ToString() == itBox.Tag.ToString();

                timeSpanTab.IsSelected = true;
            }

        }

        private void setCalendarToDefault()
        {
            startDateCalendar.SelectedDate = DateTime.Now;
            startDateCalendar.DisplayDate = DateTime.Now;
            endDateCalendar.SelectedDate = DateTime.Now;
            endDateCalendar.DisplayDate = DateTime.Now;
        }

        #region Events

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        void Timespan_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton sendCheck = sender as RadioButton;
            foreach (RadioButton itBox in TimeSpanBoxesColl)
                itBox.IsChecked = itBox.Name == sendCheck.Name;

            setCalendarToDefault();

        }

        private void RetractQueryStartDate(int days)
        {
            if (startDateCalendar != null)
            {
                startDateCalendar.SelectedDate = DateTime.Now.AddDays(days * -1);
                startDateCalendar.DisplayDate = DateTime.Now.AddDays(days * -1); 
            }
        }

        private void Expand(object sender, RoutedEventArgs e)
        {
            Button callButton = sender as Button;
            Expander targetExpander = callButton.Parent as Expander;
            bool isExpanded = targetExpander.IsExpanded;
            VisualStateManager.GoToState(callButton, isExpanded ? "Normal"  : "Pressed", true);
            targetExpander.IsExpanded = !isExpanded;
            if (!isExpanded)
                DataBindSortByDropDown();
        }

        private void DimensionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            MetricsExpander.IsExpanded = false;
            FilterExpander.IsExpanded = false;
            DimensionsView.Visibility = Visibility.Visible;
            DimensionsView.BeginAnimation(TreeView.WidthProperty, AnimatePropertyWidth);
            DimensionsView.BeginAnimation(TreeView.HeightProperty, AnimatePropertyHeight); 
        }

        private void DimensionsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            _query.Dimensions.Clear();
            _query.Dimensions = GetCheckedItems(DimensionsView.tree.Items[0] as SizeViewModel);
            BindSizeList(ListType.Dim);
            DataBindSortByDropDown();
        }

        private void MetricsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            DimensionsExpander.IsExpanded = false;
            FilterExpander.IsExpanded = false;

            MetricsView.Visibility = Visibility.Visible;
            MetricsView.BeginAnimation(TreeView.WidthProperty, AnimatePropertyWidth);
            MetricsView.BeginAnimation(TreeView.HeightProperty, AnimatePropertyHeight);
            
        }

        private void MetricsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            _query.Metrics.Clear();
            _query.Metrics = GetCheckedItems(MetricsView.tree.Items[0] as SizeViewModel);
            BindSizeList(ListType.Met);
            DataBindSortByDropDown();
        }

        private void CancelBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void FilterExpander_Expanded(object sender, RoutedEventArgs e)
        {
            MetricsExpander.IsExpanded = false;
            DimensionsExpander.IsExpanded = false;

            FilterCanvas.BeginAnimation(Canvas.WidthProperty, AnimatePropertyWidth);
            FilterCanvas.BeginAnimation(Canvas.HeightProperty, AnimatePropertyHeight);

            BindFilterListBox();

            Binding dimBinding = new Binding();
            dimBinding.Source = _query.Dimensions;
            Binding metBinding = new Binding();
            metBinding.Source = _query.Metrics;
            comboBoxDimensions.SetBinding(ComboBox.ItemsSourceProperty, dimBinding);
            comboBoxMetrics.SetBinding(ComboBox.ItemsSourceProperty, metBinding);
        }


        private void FilterExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            BindSizeList(ListType.Fil);
        }

        private void comboBoxSites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxSites.SelectedItem != null)
            {
                this._query.Ids.Clear();
                this._query.Ids.Add((comboBoxSites.SelectedItem as Entry).Title, (comboBoxSites.SelectedItem as Entry).ProfileId); 
            }
        }

        private void cancelFilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterExpander.IsExpanded = false;
        }

        private void comboBoxMetrics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeSize = SizeKeyType.Metric;
            BindOperatorList(SizeKeyType.Metric);
        }

        private void comboBoxMetrics_DropDownOpened(object sender, EventArgs e)
        {
            comboBoxDimensions.SelectedIndex = -1;
            activeSize = SizeKeyType.Metric;
            BindOperatorList(SizeKeyType.Metric);
        }

        private void comboBoxDimensions_DropDownOpened(object sender, EventArgs e)
        {
            comboBoxMetrics.SelectedIndex = -1;
            activeSize = SizeKeyType.Dimension;
            BindOperatorList(SizeKeyType.Dimension);
        }

        private void comboBoxDimensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeSize = SizeKeyType.Dimension;
            BindOperatorList(SizeKeyType.Dimension);
        }

        private void addFilter_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxOperator.SelectedIndex != -1 && !String.IsNullOrEmpty(textBoxExpression.Text))
                AddFilter(activeSize);
        }

        private void removeFilter_Click(object sender, RoutedEventArgs e)
        {
            if (filterBox.SelectedIndex != -1)
            {
                _query.Filter.RemoveAt(filterBox.SelectedIndex);
                BindFilterListBox();
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                CompleteQuery();
                this.Close();
                SetSelectedDates();
                queryComplete(_query);
            }
        }

        private void KeyUniqueCheck(Query _query)
        { 
//            ((KeyValuePair<string, string>)sortBycomboBox.SelectedItem).Key
//            int numberOfKeys = _query.SortParams.Keys.Count;
            List<string> keyList = _query.SortParams.Keys.ToList();
                foreach(string key in keyList)
                {
                   //jämför varje nyckel i 
                    if ((((KeyValuePair<string, string>)sortBycomboBox.SelectedItem).Key).Equals(key))
                    {
                        _query.SortParams.Remove(((KeyValuePair<string, string>)sortBycomboBox.SelectedItem).Key);
                    }
                }            
        }


        private void CompleteQuery()
        {

            if (startDateCalendar.SelectedDate.Value.Date.Equals(DateTime.Today))
            {
                _query.TimePeriod = (TimePeriod)Enum.Parse(typeof(TimePeriod), TimeSpanBoxesColl.Where(p => (bool)p.IsChecked).First().Tag.ToString());
                _query.SelectDates = false;
            }
            else 
            {
                _query.EndDate = (DateTime)endDateCalendar.SelectedDate;
                _query.SelectDates = true;
           }


            _query.StartIndex = int.Parse(startIndexTextBox.Text);
            _query.MaxResults = int.Parse(maxResultsTextBox.Text);

            if (sortBycomboBox.SelectedIndex != -1)
            {
                KeyUniqueCheck(_query);
                _query.SortParams.Clear();
                _query.SortParams.Add(((KeyValuePair<string, string>)sortBycomboBox.SelectedItem).Key
                    , ((KeyValuePair<string, string>)sortBycomboBox.SelectedItem).Value);

                ListSortOrder();
            }

            _query.Ids.Clear();
            _query.Ids.Add((comboBoxSites.SelectedItem as Entry).Title, (comboBoxSites.SelectedItem as Entry).ProfileId);
        }

        private void ListSortOrder()
        {
            String tempKeyIncorrect = _query.SortParams.Keys.First();
            String tempValue = _query.SortParams.Values.First();
            String tempKey = _query.SortParams.Keys.First();

            if (tempValue.Contains("-"))
            {
                listOrder.Content = descending;
            }

            if (listOrder.Content.Equals(descending) && !(tempValue.Contains("-")))
            {
                tempValue = "-" + tempValue;
            }

            if (listOrder.Content.Equals(ascending) || _query.SortParams.Count != 0)
            {
                if (_query.SortParams.Values.First().Contains("-"))
                {
                    tempValue = tempValue.Substring(1, tempValue.Length - 1);
                }
            }

            if(tempKey.Contains("ga"))
            {
                tempKey = tempKey.Substring(4, tempKey.Length - 4);
            }

            _query.SortParams.Remove(tempKeyIncorrect);
            _query.SortParams.Add(tempKey, tempValue);
            sortBycomboBox.SelectedValue = _query.SortParams.First();
        }

        private void ExecuteButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _query.Metrics.Clear();
            _query.Metrics = GetCheckedItems(MetricsView.tree.Items[0] as SizeViewModel);

            _query.Dimensions.Clear();
            _query.Dimensions = GetCheckedItems(DimensionsView.tree.Items[0] as SizeViewModel);

            if (ValidateForm())
            {
                MainNotify.Visibility = Visibility.Collapsed;
                MainNotify.ErrorMessage = string.Empty;
            }
         }

        private void ExecuteButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExecuteButton.IsEnabled = true;
            MainNotify.Visibility = Visibility.Collapsed;
            MainNotify.ErrorMessage = string.Empty;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void logOPBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filterBox.Items.Count > 0)
            {
                ComboBox cBox = sender as ComboBox;
                filterBox.SelectedItem = (cBox.Parent as StackPanel).DataContext;
                if (filterBox.SelectedItem != null)
                {
                    this._query.Filter[filterBox.SelectedIndex].LOperator =
                    cBox.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                }
            }
        }

        private void TimeSpanTab_MouseEnter(object sender, MouseEventArgs e)
        {
            if (startDateCalendar.SelectedDate.Value.Date != DateTime.Today || endDateCalendar.SelectedDate.Value.Date != DateTime.Today)
            {
                weekCheckBox.IsChecked = false;
                weekCheckBoxAnglosax.IsChecked = false;
                monthCheckBox.IsChecked = false;
                quarterCheckBox.IsChecked = false;
                yearCheckBox.IsChecked = false;

            }        
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!hasInvokedDimSetCheck && _query.Metrics.Count > 0)
            {
                SetCheckedItems(_query.Metrics, MetricsView.tree.Items[0] as SizeViewModel);
                hasInvokedDimSetCheck = true;
            }
            if (!hasInvokedMetSetCheck && _query.Dimensions.Count > 0)
            {
                SetCheckedItems(_query.Dimensions, DimensionsView.tree.Items[0] as SizeViewModel);
                hasInvokedMetSetCheck = true;
            }
        }

        void DimensionsView_treeDatabound()
        {
            SetCheckedItems(_query.Dimensions, DimensionsView.tree.Items[0] as SizeViewModel);
        }
        #endregion

        #region Methods

        private void InitializeForm()
        {
            BindSizeList(ListType.Dim);
            BindSizeList(ListType.Met);
            BindSizeList(ListType.Fil);

            hasInvokedDimSetCheck = false;
            hasInvokedMetSetCheck = false;

            if (_query.SortParams != null && _query.SortParams.Count > 0)
                //Select the orderItem från query list.
                sortBycomboBox.SelectedValue = _query.SortParams.First().Value;

            if (_query.GetMetricsAndDimensions.Count() > 0)
                DataBindSortByDropDown();

            if (CurrentUser != null)
                DataBindSitesDropDown();

            if (this._query.Ids.Count > 0)
            {
                string pId = this._query.Ids.First().Value;
                if (_currentUserAccount.Entrys.Find(p => p.ProfileId == pId) != null)
                    comboBoxSites.SelectedValue = _currentUserAccount.Entrys.Find(p => p.ProfileId == pId);
                else
                    Notify("Your account lacks permission on the target profile");
            }
            else if (_currentUserAccount != null)
                comboBoxSites.SelectedIndex = _currentUserAccount.Entrys.Count > 0 ? 0 : -1;

            SetCalendars();

            activeSize = SizeKeyType.Dimension;

            startIndexTextBox.Text = _query.StartIndex.ToString();
            maxResultsTextBox.Text = _query.MaxResults.ToString();

            
        }

        private void DataBindSortByDropDown()
        {
            Binding sites = new Binding();
            sites.Source = _query.GetMetricsAndDimensions;
            sortBycomboBox.SetBinding(ComboBox.ItemsSourceProperty, sites);
            sortBycomboBox.SelectedIndex = _query.GetDimensionsAndMetricsCount() > 0 ? 0 : -1;
            
            if (_query.SortParams.Count != 0)
            {
                ListSortOrder();
            }
            
        }

        private void Notify(string message)
        {
            MainNotify.Visibility = Visibility.Visible;
            MainNotify.ErrorMessage = message;
        }

        private void DataBindSitesDropDown()
        {
            Binding sites = new Binding();
            sites.Source = CurrentUser.Entrys;
            comboBoxSites.SetBinding(ComboBox.ItemsSourceProperty, sites);
        }

        private void AddFilter(SizeKeyType size)
        {
            ComboBox sizeRefBox = size == SizeKeyType.Dimension ? comboBoxDimensions : comboBoxMetrics;
            if (sizeRefBox.SelectedItem != null)
            {
                KeyValuePair<string, string> item = (sizeRefBox.SelectedItem as Nullable<KeyValuePair<string, string>>).Value;
                KeyValuePair<string, string> selectedOperator = (comboBoxOperator.SelectedItem as Nullable<KeyValuePair<string, string>>).Value;
                FilterItem fItem = new FilterItem(item.Key, item.Value,
                         new SizeOperator(selectedOperator.Key, selectedOperator.Value), textBoxExpression.Text,
                         (SizeKeyType)size, _query.Filter.Count == 0 ?
                         LogicalOperator.None : LogicalOperator.And);
                _query.Filter.Add(fItem);
                BindFilterListBox();
            }
        }

        private void BindFilterListBox()
        {
            Filter f = new Filter();
            Binding filterBinding = new Binding();
            foreach (FilterItem item in _query.Filter)
                f.Add(item);
            if (f.Count == 1)
                f[0].LOperator = LogicalOperator.None;
            filterBinding.Source = f;
            filterBox.SetBinding(ListBox.ItemsSourceProperty, filterBinding);
        }

        private void BindSizeList(ListType type)
        {
            Binding binding = new Binding();
            switch (type)
            {
                case ListType.Dim:
                    binding.Source = _query.Dimensions;
                    dimensionsSelected.SetBinding(ListBox.ItemsSourceProperty, binding);
                    break;
                case ListType.Met:
                    binding.Source = _query.Metrics;
                    metricsSelected.SetBinding(ListBox.ItemsSourceProperty, binding);
                    break;
                case ListType.Fil:
                    binding.Source = _query.Filter.ToSimplifiedList();
                    activeFilters.SetBinding(ListBox.ItemsSourceProperty, binding);
                    break;
                default:
                    break;
            }
        }

        private void BindOperatorList(SizeKeyType activeSize)
        {
            Binding opBind = new Binding();
            opBind.Source = Query.GetOperatorCollection(activeSize);
            comboBoxOperator.SetBinding(ComboBox.ItemsSourceProperty, opBind);
        }

        private Dictionary<string, string> GetCheckedItems(SizeViewModel customTreeItems)
        {
            Dictionary<string, string> checkedSizes = new Dictionary<string, string>();
            foreach (SizeViewModel item in (customTreeItems).Children)
                foreach (SizeViewModel subItem in item.Children)
                    if (subItem.IsChecked == true)
                        checkedSizes.Add(subItem.Name, subItem.Value);

            return checkedSizes;
        }

        
        private void SetCalendars()
        {
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now;
            if (this._query != null && _query.StartDate.Year != 1 && _query.EndDate.Year != 1)
            {
                startDate = _query.StartDate;
                endDate = _query.EndDate;
            }
            startDateCalendar.SelectedDate = startDate;
            startDateCalendar.DisplayDate = startDate;
            endDateCalendar.SelectedDate = endDate;
            endDateCalendar.DisplayDate = endDate;

        }

        private void SetCheckedItems(Dictionary<string, string> sizeColl, SizeViewModel customTreeItems)
        {
            foreach (SizeViewModel category in customTreeItems.Children)
                foreach (SizeViewModel size in category.Children)
                    if (sizeColl.Keys.Contains(size.Name))
                        size.IsChecked = true;
        }

        private bool ValidateForm()
        {
            if (comboBoxSites.SelectedItem == null)
            {
                Notify("No profile is selected");
                return false;
            }
            if (startDateCalendar.SelectedDate > endDateCalendar.SelectedDate)
            {
                Notify("The start date can not be later than the end date");
                return false;
            }
            if (!(_query.Metrics.Count > 0))
            {
                Notify("Select atleast one metric");
                return false;
            }
            if (startDateCalendar.SelectedDate.Value.Day.Equals(DateTime.Today.Day) && !radioButtonChecked())
            {
                Notify("The start date can not be set to today");
                return false;
            }

            if (_query.Dimensions.Count > maxSupportedDimensions)
            {
                _query.Dimensions = _query.Dimensions.Take(maxSupportedDimensions).ToDictionary(k => k.Key, v => v.Value);
            }
            if (_query.Metrics.Count > maxSupportedMetrics)
            {
                _query.Metrics = _query.Metrics.Take(maxSupportedMetrics).ToDictionary(k => k.Key, v => v.Value);
            }
            return true;
        }

        private bool radioButtonChecked()
        {
            bool radioButton = false;

            foreach (RadioButton radio in TimeSpanBoxesColl)
            {
                if (radio.IsChecked.Value)
                {
                    radioButton = true;
                    break;
                }
            }

            return radioButton;
        }

       
        #endregion

        private void validate_int(object sender, TextChangedEventArgs e)
        {
            int i;
            if (! int.TryParse((sender as TextBox).Text , out i))
                (sender as TextBox).Text = string.Empty;
        }

        private void listOrder_Click(object sender, RoutedEventArgs e)
        {
            if (listOrder.Content.Equals(ascending))
            {
                listOrder.Content = descending;
            }
            else
            {
                listOrder.Content = ascending;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms
            AboutExcellentAnalytics about = new AboutExcellentAnalytics();
            // Configure the message box to be displayed
            string caption = "About Excellent Analytics";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;


            string messageBoxText = "Hejhej\n";
            System.Windows.Forms.LinkLabel link = new System.Windows.Forms.LinkLabel();
            link.Tag = "http://www.svd.se";
//            messageBoxText += link;

            
            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.OK:
                    break;
//                case 
//                this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);


              }
        }

        private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkLabel1.LinkVisited = true;

            // Navigate to a URL.
            System.Diagnostics.Process.Start("http://www.microsoft.com");
        }

    }
}