//--------------------------------------------------------------------------------------
// 
// WPF ShaderEffect HLSL -- EffectColoringImage
//
//	using grayscale value, blend with colorLow replacing black and colorHigh replacing white value
//
//--------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------

float4 colorLow : register(C0);
float4 colorHigh : register(C1);

//--------------------------------------------------------------------------------------
// Sampler Inputs (Brushes, including ImplicitInput)
//--------------------------------------------------------------------------------------

sampler2D input : register(S0);


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 Color;
	Color = tex2D(input, uv.xy);

	float gray = (Color.r + Color.g + Color.b) / 3.0;

	Color.rgb = colorHigh.rgb * gray + colorLow.rgb * (1.0-gray);

	return Color;
}

