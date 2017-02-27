using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ShaderEffectLibrary
{
    public class EffectColoringImage : ShaderEffect
    {
        #region Constructors

        static EffectColoringImage()
        {
            _pixelShader.UriSource = Global.MakePackUri("EffectColoringImage.ps");
        }

        public EffectColoringImage()
        {
            this.PixelShader = _pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ColorFilterProperty);
            UpdateShaderValue(BackgroundFilterProperty);
        }

        #endregion

        #region Dependency Properties

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(EffectColoringImage), 0);



        public Color ColorFilter
        {
            get { return (Color)GetValue(ColorFilterProperty); }
            set { SetValue(ColorFilterProperty, value); }
        }

        // Scalar-valued properties turn into shader constants with the register
        // number sent into PixelShaderConstantCallback().
        public static readonly DependencyProperty ColorFilterProperty =
            DependencyProperty.Register("ColorFilter", typeof(Color), typeof(EffectColoringImage),
                    new UIPropertyMetadata(Colors.Black, PixelShaderConstantCallback(0)));

        public Color BackgroundFilter
        {
            get { return (Color)GetValue(BackgroundFilterProperty); }
            set { SetValue(BackgroundFilterProperty, value); }
        }

        // Scalar-valued properties turn into shader constants with the register
        // number sent into PixelShaderConstantCallback().
        public static readonly DependencyProperty BackgroundFilterProperty =
            DependencyProperty.Register("BackgroundFilter", typeof(Color), typeof(EffectColoringImage),
                    new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(1)));

        #endregion

        #region Member Data

        private static PixelShader _pixelShader = new PixelShader();

        #endregion

    }
}
