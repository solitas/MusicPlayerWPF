namespace MP3Testing.Control
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Shapes;

    [TemplatePart(Name = ThumbPartName, Type = typeof(Thumb))]
    public class SimpleSlider : Slider
    {
        private const string ThumbPartName = "PART_Thumb";
        private const string RectanglePartName = "PART_Rectangle";

        private Thumb _thumb;
        private Rectangle _rectangle;

        /// <summary>
        /// Initializes the <see cref="CustomSlider"/> class.
        /// </summary>
        static SimpleSlider()
        {
            // Links to the default style in Themes/Generic.xaml.
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SimpleSlider), new FrameworkPropertyMetadata(typeof(SimpleSlider)));
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(SimpleSlider), new UIPropertyMetadata(0.0));

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(SimpleSlider), new UIPropertyMetadata(0.0));

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SimpleSlider), new UIPropertyMetadata(0.0, OnValueChanged));

        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._thumb = this.Template.FindName(ThumbPartName, this) as Thumb;
            if (this._thumb != null)
            {
                this._thumb.DragDelta += this.Thumb_DragDelta;
            }

            this._rectangle = this.Template.FindName(RectanglePartName, this) as Rectangle;

            this.SizeChanged += new SizeChangedEventHandler(SimpleSlider_SizeChanged);
        }

        void SimpleSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                this.UpdateControls();
            }
        }

        /// <summary>
        /// Called when value changed.
        /// </summary>
        private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            // Simple version: no coercion involved.
            var customSlider = (SimpleSlider)dependencyObject;
            customSlider.UpdateControls();
        }

        /// <summary>
        /// Handles the DragDelta event of the Thumb control.
        /// </summary>
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var pixelDiff = e.HorizontalChange;
            var currentLeft = Canvas.GetLeft(this._thumb);

            // trying to drag too far left
            if ((currentLeft + pixelDiff) < 0)
            {

                this.Value = 0;
            }
            // trying to drag too far right
            else if ((currentLeft + pixelDiff + this._thumb.ActualWidth) > this.ActualWidth)
            {

                this.Value = this.Maximum;
            }
            else
            {
                var totalSize = this.ActualWidth;
                var ratioDiff = pixelDiff / totalSize;
                var rangeSize = this.Maximum - this.Minimum;
                var rangeDiff = rangeSize * ratioDiff;
                this.Value += rangeDiff;
            }
        }

        /// <summary>
        /// Updates the controls.
        /// </summary>
        private void UpdateControls()
        {
            double halfTheThumbWith = 0;

            if (this._thumb != null)
            {
                halfTheThumbWith = this._thumb.ActualWidth / 2;
            }

            double totalSize = this.ActualWidth - halfTheThumbWith * 2;

            double ratio = totalSize / (this.Maximum - this.Minimum);

            if (this._thumb != null)
            {
                Canvas.SetLeft(this._thumb, ratio * this.Value);
            }

            if (this._rectangle != null)
            {
                this._rectangle.Width = ratio * this.Value + halfTheThumbWith;
            }
        }
    }
}
