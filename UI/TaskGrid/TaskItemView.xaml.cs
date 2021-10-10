using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MyTaskSwitcher.UI.TaskGrid {
    /// <summary>
    /// TaskItemView
    /// </summary>
    public partial class TaskItemView : UserControl {

        #region Constructor
        public TaskItemView() {
            InitializeComponent();
        }

        static TaskItemView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskItemView), 
                new FrameworkPropertyMetadata(typeof(TaskItemView)));
        }
        #endregion

        #region Event
        /// <summary>
        /// task item click event argument
        /// </summary>
        public class TaskItemClickEventArgs : EventArgs {
            public long Id { private set; get; }
            public TaskItemClickEventArgs(long id) {
                this.Id = id;
            }
        }

        /// <summary>
        /// task item click event
        /// </summary>
        private EventHandler<TaskItemClickEventArgs> OnTaskItemClick;
        public event EventHandler<TaskItemClickEventArgs> TaskItemClick {
            add { OnTaskItemClick += value; }
            remove { OnTaskItemClick -= value; }
        }
        #endregion

        #region Property
        /// <summary>
        /// Key
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
                                                DependencyProperty.Register("Key",
                                                                            typeof(string),
                                                                            typeof(TaskItemView),
                                                                            new FrameworkPropertyMetadata());
        public string Key {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        /// <summary>
        /// title
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
                                                DependencyProperty.Register("Title",
                                                                            typeof(string),
                                                                            typeof(TaskItemView),
                                                                            new FrameworkPropertyMetadata());
        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// icon
        /// </summary>
        public static readonly DependencyProperty IconProperty =
                                                DependencyProperty.Register("Icon",
                                                                            typeof(BitmapSource),
                                                                            typeof(TaskItemView),
                                                                            new FrameworkPropertyMetadata());
        public BitmapSource Icon {
            get { return (BitmapSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty MyCommandProperty = DependencyProperty.Register("MyCommand",
                                                                        typeof(ICommand),
                                                                        typeof(TaskItemView),
                                                                        new PropertyMetadata(null));

        public ICommand MyCommand {
            get { return (ICommand)GetValue(MyCommandProperty); }
            set { SetValue(MyCommandProperty, value); }
        }

        public static readonly DependencyProperty MyCommandParamProperty = DependencyProperty.Register("MyCommandParam",
                                                                                typeof(object),
                                                                                typeof(TaskItemView),
                                                                                new PropertyMetadata(null));
        public object MyCommandParam {
            get { return GetValue(MyCommandParamProperty); }
            set { SetValue(MyCommandParamProperty, value); }
        }
        #endregion
    }
}
