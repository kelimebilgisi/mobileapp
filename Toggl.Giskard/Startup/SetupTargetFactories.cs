using Android.Views;
using MvvmCross.Binding.Bindings.Target.Construction;
using Toggl.Giskard.Bindings;
using Toggl.Giskard.Views;
using Android.Widget;
using Android.Support.Design.Widget;
using Android.Support.V4.View;

namespace Toggl.Giskard
{
    public partial class Setup
    {
        protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            base.FillTargetFactories(registry);
            registry.RegisterCustomBindingFactory<View>(
                $"Left{ViewMarginTargetBinding.BindingName}",
                view => new ViewMarginTargetBinding(view, ViewMarginTargetBinding.BoundMargin.Left)
            );
            registry.RegisterCustomBindingFactory<View>(
                $"Top{ViewMarginTargetBinding.BindingName}",
                view => new ViewMarginTargetBinding(view, ViewMarginTargetBinding.BoundMargin.Top)
            );
            registry.RegisterCustomBindingFactory<View>(
                $"Right{ViewMarginTargetBinding.BindingName}",
                view => new ViewMarginTargetBinding(view, ViewMarginTargetBinding.BoundMargin.Right)
            );
            registry.RegisterCustomBindingFactory<View>(
                $"Bottom{ViewMarginTargetBinding.BindingName}",
                view => new ViewMarginTargetBinding(view, ViewMarginTargetBinding.BoundMargin.Bottom)
            );

            registry.RegisterCustomBindingFactory<View>(
                DrawableColorTargetBinding.BindingName,
                view => new DrawableColorTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<EditText>(
                EditTextFocusTargetBinding.BindingName,
                view => new EditTextFocusTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<FloatingActionButton>(
                FabVisibilityTargetBinding.BindingName,
                view => new FabVisibilityTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<TextInputLayout>(
                TextInputLayoutErrorTextTargetBinding.BindingName,
                view => new TextInputLayoutErrorTextTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<TextView>(
                TextViewFontWeightTargetBinding.BindingName,
                view => new TextViewFontWeightTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<View>(
                ViewAlphaTargetBinding.BindingName,
                view => new ViewAlphaTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<View>(
                ViewHeightTargetBinding.BindingName,
                view => new ViewHeightTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<View>(
                ViewMarginTargetBinding.BindingName,
                view => new ViewMarginTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<ViewPager>(
                ViewPagerCurrentPageTargetBinding.BindingName,
                view => new ViewPagerCurrentPageTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<View>(
                ViewUpsideDownTargetBinding.BindingName,
                view => new ViewUpsideDownTargetBinding(view)
            );

            registry.RegisterCustomBindingFactory<View>(
                ViewWidthPercentageTargetBinding.BindingName,
                view => new ViewWidthPercentageTargetBinding(view)
            );
        }
    }
}
